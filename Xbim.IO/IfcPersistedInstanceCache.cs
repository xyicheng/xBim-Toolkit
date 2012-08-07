using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions.Interfaces;
using Microsoft.Isam.Esent.Interop;
using Xbim.IO.Parser;
using System.IO;
using Xbim.Common.Exceptions;
using System.Xml;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Transactions.Extensions;
using Xbim.XbimExtensions.SelectTypes;
using System.Linq.Expressions;
using System.Reflection;
using Xbim.Ifc2x3.Kernel;

namespace Xbim.IO
{
    
    internal class IfcPersistedInstanceCache : Dictionary<long, IPersistIfcEntity>, IIfcInstanceCache, IDisposable
    {
        #region ESE Database fields

        static private Instance _jetInstance;
        private Session _jetSession;
        private JET_DBID _jetDatabaseId;
        //tables open read only
        private XbimEntityTable _jetTypeCursor;
        private XbimEntityTable _jetEntityCursor;
        const int _transactionBatchSize = 100;


       
        #endregion
        #region Cached data

        protected HashSet<IPersistIfcEntity> ToUpdate = new HashSet<IPersistIfcEntity>();
        protected HashSet<IPersistIfcEntity> ToDelete = new HashSet<IPersistIfcEntity>();
        protected HashSet<IPersistIfcEntity> ToCreate = new HashSet<IPersistIfcEntity>();
        protected int CacheDefaultSize = 5000;

        #endregion

        private long _highestLabel = -1;
        private string _databaseName;
        private XbimModel _model;
        private bool disposed = false;


        public IfcPersistedInstanceCache(XbimModel model)
        {
            _model = model;
        }

        static IfcPersistedInstanceCache()
        {
            if (_jetInstance == null) //if we have never created an instance do it now
            {
                _jetInstance = new Instance("XbimInstance");
                _jetInstance.Parameters.Recovery = false; //By default its True, only set this if we plan to write
                SystemParameters.CacheSizeMin = 16 * 1024;
                _jetInstance.Parameters.LogFileSize = 16 * 1024;
                _jetInstance.Parameters.LogBuffers = 8 * 1024;
                _jetInstance.Init();
            }
        }
        /// <summary>
        /// Creates an empty xbim file, overwrites any existing file of the same name
        /// </summary>
        /// <returns></returns>
        public bool CreateDatabase(string fileName)
        {

            // _filename = Path.ChangeExtension(fileName, "xBIM");

            using (var session = new Session(_jetInstance))
            {
                JET_DBID dbid;
                Api.JetCreateDatabase(session, fileName, null, out dbid, CreateDatabaseGrbit.OverwriteExisting);
                using (var transaction = new Microsoft.Isam.Esent.Interop.Transaction(session))
                {
                    XbimEntityTable.CreateTable(session, dbid);
                    XbimGeometryTable.CreateTable(session, dbid);
                    XbimHeaderTable.CreateTable(session, dbid);
                    transaction.Commit(CommitTransactionGrbit.LazyFlush);
                }
                if (dbid == JET_DBID.Nil)
                {
                    return false;
                }
                else
                {
                    _databaseName = fileName;
                    return true;
                }
            }
        }

       

       
        
        /// <summary>
        ///  Opens an xbim model server file, exception is thrown if errors are encountered
        /// </summary>
        /// <param name="filename"></param>
        internal void Open(string filename)
        {
            try
            {
                //reset the instances cache to clear any previous loads
                Clear();
                _jetSession = new Session(_jetInstance);
                JET_wrn warning = Api.JetAttachDatabase(_jetSession, filename, AttachDatabaseGrbit.None);
                warning = Api.JetOpenDatabase(_jetSession, filename, null, out _jetDatabaseId, OpenDatabaseGrbit.None);

                _jetEntityCursor = new XbimEntityTable(_jetSession, _jetDatabaseId);
                _jetEntityCursor.Open(OpenTableGrbit.ReadOnly);
                _jetTypeCursor = new XbimEntityTable(_jetSession, _jetDatabaseId);
                _jetTypeCursor.Open(OpenTableGrbit.ReadOnly);

                XbimHeaderTable headerTable = new XbimHeaderTable(_jetSession, _jetDatabaseId);
                headerTable.Open(OpenTableGrbit.ReadOnly);

                _model.Header = headerTable.IfcFileHeader;
                headerTable.Close();
                //set up the geometry table
               
                _databaseName = filename; //success store the name of the DB file

            }
            catch (Exception e)
            {
                Clear();
                throw new XbimException("Failed to open " + filename, e);
            }
        }

        /// <summary>
        /// Clears all contents fromthe cache and closes any connections
        /// </summary>
        public void Close()
        {

            CloseDatabase();

            base.Clear();
            _highestLabel = -1;
        }

        /// <summary>
        /// Closes and nulls any ables that are open
        /// </summary>
        private void CloseDatabase()
        {
            if (_jetEntityCursor != null)
            {
                _jetEntityCursor.Close();
                _jetEntityCursor = null;
            }
           
            if (_jetTypeCursor != null)
            {
                _jetTypeCursor.Close();
                _jetTypeCursor = null;
            }

            if (_jetSession != null)
            {
                if (!string.IsNullOrEmpty(_databaseName))
                {
                    Api.JetCloseDatabase(_jetSession, _jetDatabaseId, CloseDatabaseGrbit.None);
                    Api.JetDetachDatabase(_jetSession, _databaseName);
                    _databaseName = null;
                }
                _jetSession.End();
                _jetSession = null;
            }
        }

        /// <summary>
        /// Imports the contents of the ifc file into the named database
        /// </summary>
        /// <param name="progressHandler"></param>
        /// <returns></returns>
        public void ImportIfc(string toImportIfcFilename, ReportProgressDelegate progressHandler = null)
        {

            using (var jetSession = new Session(_jetInstance))
            {
                JET_DBID dbid;

                Api.JetAttachDatabase(jetSession, _databaseName, AttachDatabaseGrbit.None);
                Api.JetOpenDatabase(jetSession, _databaseName, null, out dbid, OpenDatabaseGrbit.None);

                using (var table = new XbimEntityTable(jetSession, dbid, OpenTableGrbit.None))
                {
                    using (var transaction = new Transaction(jetSession))
                    {
                        using (FileStream reader = new FileStream(toImportIfcFilename, FileMode.Open, FileAccess.Read))
                        {
                            using (P21toIndexParser part21Parser = new P21toIndexParser(reader, jetSession, table))
                            {
                                try
                                {
                                    if (progressHandler != null) part21Parser.ProgressStatus += progressHandler;
                                    part21Parser.Parse();
                                    _model.Header = part21Parser.Header;
                                    WriteHeader(jetSession, dbid);
                                }
                                catch (Exception e)
                                {
                                    transaction.Rollback();
                                    throw new XbimException("Error importing Ifc File " + toImportIfcFilename, e);
                                }
                                finally
                                {
                                    if (progressHandler != null) part21Parser.ProgressStatus -= progressHandler;
                                }
                            }
                        }
                        transaction.Commit(CommitTransactionGrbit.None);
                    }
                }
                Api.JetCloseDatabase(jetSession, dbid, CloseDatabaseGrbit.None);
                Api.JetDetachDatabase(jetSession, _databaseName);
            }

        }

        private void WriteHeader(Session jetSession, JET_DBID dbid)
        {
            using (var table = new XbimHeaderTable(jetSession, dbid, OpenTableGrbit.None))
            {
                table.UpdateHeader(_model.Header);
            }
        }

        /// <summary>
        ///   Imports an Xml file memory model into the model server, only call when the database instances table is empty
        /// </summary>

        public void ImportIfcXml(string xmlFilename, ReportProgressDelegate progressHandler = null)
        {

            using (_jetSession = new Session(_jetInstance))
            {
                JET_DBID dbid;
                Api.JetAttachDatabase(_jetSession, _databaseName, AttachDatabaseGrbit.None);
                Api.JetOpenDatabase(_jetSession, _databaseName, null, out dbid, OpenDatabaseGrbit.None);
                using (_jetEntityCursor = new XbimEntityTable(_jetSession, dbid, OpenTableGrbit.None))
                {
                    
                    using (var transaction = new Transaction(_jetSession))
                    {
                        XmlReaderSettings settings = new XmlReaderSettings() { IgnoreComments = true, IgnoreWhitespace = false };
                        using (Stream xmlInStream = new FileStream(xmlFilename, FileMode.Open, FileAccess.Read))
                        {
                            using (XmlReader xmlReader = XmlReader.Create(xmlInStream, settings))
                            {
                                IfcXmlReader reader = new IfcXmlReader();
                                _model.Header = reader.Read(this, xmlReader);
                                WriteHeader(_jetSession, dbid);
                            }
                        }
                        transaction.Commit(CommitTransactionGrbit.None);
                    }
                }
                _jetEntityCursor = null;
                Api.JetCloseDatabase(_jetSession, dbid, CloseDatabaseGrbit.None);
                Api.JetDetachDatabase(_jetSession, _databaseName);
            }
            _jetSession = null;
        }

        private void SetDatabaseColumns(Session jetSession, Table jetEntityCursor)
        {
           
        }

       

        /// <summary>
        /// Writes the properties of the entity to the database and calls commit in batch mode
        /// </summary>
        /// <param name="toWrite"></param>
        /// <param name="entitiesParsed"></param>
        public void UpdateEntity(IPersistIfcEntity toWrite, int entitiesParsed = 0)
        {

            using (var update = new Update(_jetSession, _jetEntityCursor, JET_prep.Insert))
            {

                
                _jetEntityCursor.SetColumnValues(toWrite);

                Api.SetColumns(_jetSession, _jetEntityCursor, _jetEntityCursor.ColumnValues);
                update.Save();
                if (entitiesParsed % _transactionBatchSize == (_transactionBatchSize - 1))
                {
                    Api.JetCommitTransaction(_jetSession, CommitTransactionGrbit.LazyFlush);
                    Api.JetBeginTransaction(_jetSession);
                }
            }
        }

        public bool Contains(IPersistIfcEntity instance)
        {
            return Contains(Math.Abs(instance.EntityLabel));
        }

        public bool Contains(long posLabel)
        {
            if (base.ContainsKey(posLabel)) //check if it is cached
                return true;
            else //look in the database
            {

                Api.JetSetCurrentIndex(_jetSession, _jetEntityCursor, _jetEntityCursor.PrimaryIndex);
                Api.MakeKey(_jetSession, _jetEntityCursor, posLabel, MakeKeyGrbit.NewKey);
                return Api.TrySeek(_jetSession, _jetEntityCursor, SeekGrbit.SeekEQ);
            }
        }

        public new long Count
        {
            get { throw new NotImplementedException(); }
        }

        public long HighestLabel
        {
            get { throw new NotImplementedException(); }
        }



        public void SetHighestLabel_Reversable(long nextLabel)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Creates a new instance this is a reversable action and should be used typically
        /// </summary>
        /// <param name="t"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        public IPersistIfcEntity CreateNew_Reversable(Type t, long label)
        {
            long posLabel = Math.Abs(label);
            IPersistIfcEntity entity = (IPersistIfcEntity)Activator.CreateInstance(t);
            entity.Bind(_model, posLabel); //bind it, the object is new and empty so the label is positive
            this.Add_Reversible(new KeyValuePair<long, IPersistIfcEntity>(posLabel, entity));
            ToCreate.Add_Reversible(entity);
            return entity;
        }

        /// <summary>
        /// Creates a new instance, this is not a reversable action, and the instance is not cached
        /// It is for performance in import and export routines and should not be used in normal code
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public IPersistIfcEntity CreateNew(Type type, long label)
        {
            long posLabel = Math.Abs(label);
            IPersistIfcEntity entity = (IPersistIfcEntity)Activator.CreateInstance(type);
            entity.Bind(_model, posLabel); //bind it, the object is new and empty so the label is positive
            //this.Add(posLabel, entity);
            //ToCreate.Add(entity);
            return entity;
        }

        public long InstancesOfTypeCount(Type t)
        {
            throw new NotImplementedException();
        }
        /// <summary>
        /// Returns an enumeration of handles to all instances in the database and in the cache
        /// </summary>
        public IEnumerable<IfcInstanceHandle> InstanceHandles
        {
            get
            {
                foreach (var ent in this.Values)
                {
                    yield return new IfcInstanceHandle(ent.EntityLabel, ent.GetType());
                }
                Api.JetSetCurrentIndex(_jetSession, _jetEntityCursor, _jetEntityCursor.PrimaryIndex);
                if (Api.TryMoveFirst(_jetSession, _jetEntityCursor))
                {
                    List<IfcInstanceHandle> entities = new List<IfcInstanceHandle>();
                    do
                    {
                        long label = (long)Api.RetrieveColumnAsInt64(_jetSession, _jetEntityCursor, _jetEntityCursor.ColIdEntityLabel);
                        if (!this.ContainsKey(label)) //we have already returned the entity from the cache
                        {
                            short typeId = (short)Api.RetrieveColumnAsInt16(_jetSession, _jetEntityCursor, _jetEntityCursor.ColIdIfcType);
                            entities.Add(new IfcInstanceHandle(label, IfcInstances.IfcIdIfcTypeLookup[typeId].Type));
                        }
                    }
                    while (Api.TryMoveNext(_jetSession, _jetEntityCursor));
                    foreach (var item in entities)
                        yield return item;
                }
            }
        }
        /// <summary>
        /// Returns an enumeration of handles to all instances in the database or the cache of specified type
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IfcInstanceHandle> InstanceHandlesOfType<TIfcType>()
        {
            Type reqType = typeof(TIfcType);
            foreach (var ent in this.Values)
            {
                if (ent is TIfcType)
                    yield return new IfcInstanceHandle(ent.EntityLabel, ent.GetType());
            }
            IfcType ifcType = IfcInstances.IfcEntities[reqType];
            foreach (Type t in ifcType.NonAbstractSubTypes)
            {
                short typeId = IfcInstances.IfcEntities[t].TypeId;
                Api.JetSetCurrentIndex(_jetSession, _jetTypeCursor, _jetTypeCursor.TypeIndex);
                Api.MakeKey(_jetSession, _jetTypeCursor, typeId, MakeKeyGrbit.NewKey);
                if (Api.TrySeek(_jetSession, _jetTypeCursor, SeekGrbit.SeekGE))
                {
                    Api.MakeKey(_jetSession, _jetTypeCursor, typeId, MakeKeyGrbit.NewKey);
                    if (Api.TrySetIndexRange(_jetSession, _jetTypeCursor, SetIndexRangeGrbit.RangeUpperLimit | SetIndexRangeGrbit.RangeInclusive))
                    {
                        List<IfcInstanceHandle> entities = new List<IfcInstanceHandle>();
                        do
                        {
                            Int64 posLabel = (Int64)Api.RetrieveColumnAsInt64(_jetSession, _jetTypeCursor, _jetTypeCursor.ColIdEntityLabel); //it is a non null db value so just cast
                            if (!this.ContainsKey(posLabel)) //we have already returned the entity from the cache
                                entities.Add( new IfcInstanceHandle(posLabel, t));
                        }
                        while (Api.TryMoveNext(_jetSession, _jetTypeCursor));
                        foreach (var item in entities)
                             yield return item;
                    }
                }
            }
        }

        /// <summary>
        /// Returns an instance of the entity with the specified label,
        /// if the instance has alrady been loaded it is returned from the caache
        /// if it has not been loaded a blank instance is loaded, i.e. will not have been activated
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        public IPersistIfcEntity GetInstance(long label, bool loadProperties = false, bool unCached = false)
        {
            long posLabel = Math.Abs(label);
            IPersistIfcEntity entity;
            if (this.TryGetValue(posLabel, out entity))
                return entity;
            else
                return GetInstanceFromStore(posLabel, loadProperties, unCached);
        }

        /// <summary>
        /// Loads a blank instance from the database, do not call this before checking that the instance is in the instances cache
        /// If the entity has already been cached it will throw an exception
        /// This is not a undoable/reversable operation
        /// </summary>
        /// <param name="posLabel">Must be a positive value of the label</param>
        /// <param name="loadProperties">if true the properties of the object are loaded  at the same time</param>
        /// <param name="unCached">if true the object is not cached, this is dangerous and can lead to object duplicates</param>
        /// <returns></returns>
        private IPersistIfcEntity GetInstanceFromStore(long posLabel, bool loadProperties = false, bool unCached = false)
        {
            Api.JetSetCurrentIndex(_jetSession, _jetEntityCursor, _jetEntityCursor.PrimaryIndex);
            Api.MakeKey(_jetSession, _jetEntityCursor, posLabel, MakeKeyGrbit.NewKey);
            if (Api.TrySeek(_jetSession, _jetEntityCursor, SeekGrbit.SeekEQ))
            {
                short? typeId = Api.RetrieveColumnAsInt16(_jetSession, _jetEntityCursor, _jetEntityCursor.ColIdIfcType);
                if (typeId.HasValue)
                {
                    IfcType ifcType = IfcInstances.IfcIdIfcTypeLookup[typeId.Value];
                    IPersistIfcEntity entity = (IPersistIfcEntity)Activator.CreateInstance(ifcType.Type);

                    if (loadProperties)
                    {
                        byte[] properties = Api.RetrieveColumn(_jetSession, _jetEntityCursor, _jetEntityCursor.ColIdEntityData);
                        entity.ReadEntityProperties(this, new BinaryReader(new MemoryStream(properties)), unCached);
                        entity.Bind(_model, posLabel); //a positive handle determines that the attributes of this entity have been loaded yet
                    }
                    else
                        entity.Bind(_model, -posLabel); //a negative handle determines that the attributes of this entity have not been loaded yet
                    if (!unCached) this.Add(posLabel, entity);
                    return entity;
                }
            }
            return null;
        }

        public void Print()
        {
            //Api.JetSetCurrentIndex(_jetSession, _jetTypeCursor, _jetTypeCursor.TypeIndex);
            //Api.MakeKey(_jetSession, _jetTypeCursor, (short)17200, MakeKeyGrbit.NewKey);
            //Api.MakeKey(_jetSession, _jetTypeCursor, (long)48790, MakeKeyGrbit.None);
            //if (Api.TrySeek(_jetSession, _jetTypeCursor, SeekGrbit.SeekGE))
            //{
            //    Api.MakeKey(_jetSession, _jetTypeCursor, (short)17200, MakeKeyGrbit.NewKey );
            //    Api.MakeKey(_jetSession, _jetTypeCursor, (long)48790, MakeKeyGrbit.None);
            //    if (Api.TrySetIndexRange(_jetSession, _jetTypeCursor,  SetIndexRangeGrbit.RangeInclusive|SetIndexRangeGrbit.RangeUpperLimit))
            //    {
            //        do
            //        {
            //            Int64 firLabel = (Int64)Api.RetrieveColumnAsInt64(_jetSession, _jetTypeCursor, _jetTypeCursor.ColIdEntityLabel); //it is a non null db value so just cast
            //            Int64? secLabel = Api.RetrieveColumnAsInt64(_jetSession, _jetTypeCursor, _colIdSecondaryKey); //it is a non null db value so just cast
            //            Int16 typeId = (Int16)Api.RetrieveColumnAsInt16(_jetSession, _jetTypeCursor, _colIdIfcType); //it is a non null db value so just cast
            //            System.Diagnostics.Debug.WriteLine("#{0}={1}, Key = {2}", firLabel, IfcInstances.IfcIdIfcTypeLookup[typeId].Type.Name, secLabel.HasValue ? secLabel.Value : -1);
            //        }
            //        while (Api.TryMoveNext(_jetSession, _jetTypeCursor));
            //    }
            //}
        }



        /// <summary>
        /// Enumerates of all instances of the specified type. The values are cached, if activate is true all the properties of the entity are loaded
        /// </summary>
        /// <typeparam name="TIfcType"></typeparam>
        /// <param name="activate">if true loads the properties of the entity</param>
        /// <param name="secondaryKey">if the entity has a key object, optimises to search for this handle</param>
        /// <returns></returns>
        public IEnumerable<TIfcType> OfType<TIfcType>(bool activate = false, long secondaryKey = -1)
        {
            IfcType ifcType = IfcInstances.IfcEntities[typeof(TIfcType)];
            foreach (Type t in ifcType.NonAbstractSubTypes)
            {
                short typeId = IfcInstances.IfcEntities[t].TypeId;
                Api.JetSetCurrentIndex(_jetSession, _jetTypeCursor, _jetTypeCursor.TypeIndex);
                Api.MakeKey(_jetSession, _jetTypeCursor, typeId, MakeKeyGrbit.NewKey);
                
                if (secondaryKey > -1)
                    Api.MakeKey(_jetSession, _jetTypeCursor, secondaryKey, MakeKeyGrbit.None);

                if (Api.TrySeek(_jetSession, _jetTypeCursor, SeekGrbit.SeekGE))
                {

                    if (secondaryKey > -1)
                    {
                        Api.MakeKey(_jetSession, _jetTypeCursor, typeId, MakeKeyGrbit.NewKey);
                        Api.MakeKey(_jetSession, _jetTypeCursor, secondaryKey, MakeKeyGrbit.None);
                    }
                    else
                        Api.MakeKey(_jetSession, _jetTypeCursor, typeId, MakeKeyGrbit.NewKey | MakeKeyGrbit.FullColumnEndLimit);
                    if (Api.TrySetIndexRange(_jetSession, _jetTypeCursor, SetIndexRangeGrbit.RangeUpperLimit | SetIndexRangeGrbit.RangeInclusive))
                    {
                        List<TIfcType> entities = new List<TIfcType>(); //get them all in one go to avoid problems with cursor scope when yield is called
                        do
                        {
                            Int64 posLabel = (Int64)Api.RetrieveColumnAsInt64(_jetSession, _jetTypeCursor, _jetTypeCursor.ColIdEntityLabel); //it is a non null db value so just cast
                            IPersistIfcEntity entity;
                            if (!this.TryGetValue(posLabel, out entity))//if already in the cache just return it, else create a blank
                            {
                                entity = (IPersistIfcEntity)Activator.CreateInstance(t);
                                if (activate)
                                {
                                    byte[] properties = Api.RetrieveColumn(_jetSession, _jetTypeCursor, _jetTypeCursor.ColIdEntityData);
                                    entity.ReadEntityProperties(this, new BinaryReader(new MemoryStream(properties)), false);
                                    entity.Bind(_model, posLabel); //a positive handle determines that the attributes of this entity have been loaded yet
                                }
                                else
                                    entity.Bind(_model, -posLabel); //a negative handle determines that the attributes of this entity have not been loaded yet
                                base.Add(posLabel, entity);
                            }
                            entities.Add( (TIfcType)entity);
                        }
                        while (Api.TryMoveNext(_jetSession, _jetTypeCursor));
                        foreach (var item in entities)
                            yield return item;
                    }
                }
            }
            
        }


        public void ImportXbim(string importFrom, ReportProgressDelegate progressHandler = null)
        {
            throw new NotImplementedException();
        }

        public void ImportIfcZip(string importFrom, ReportProgressDelegate progressHandler = null)
        {
            throw new NotImplementedException();
        }


        public void Activate(IPersistIfcEntity entity)
        {
            byte[] bytes = GetEntityBinaryData(entity);
            if (bytes != null)
                entity.ReadEntityProperties(this, new BinaryReader(new MemoryStream(bytes)));
        }

        public void Dispose()
        {
            Dispose(true);
            // Take yourself off the Finalization queue 
            // to prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        ~IfcPersistedInstanceCache()
        {
            Dispose(false);
        }

        protected void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed 
                // and unmanaged resources.
                if (disposing)
                {
                    // Dispose managed resources.
                    
                    if (_jetTypeCursor != null) { _jetTypeCursor.Close(); _jetTypeCursor.Dispose(); };
                    if (_jetEntityCursor != null) { _jetEntityCursor.Close(); _jetEntityCursor.Dispose(); };
                    if (_jetSession != null) _jetSession.End();
                }

            }
            disposed = true;
        }


        /// <summary>
        /// Gets the entities propertyData on binary stream
        /// </summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        internal byte[] GetEntityBinaryData(IPersistIfcEntity entity)
        {
            long posLabel = Math.Abs(entity.EntityLabel);
            Api.JetSetCurrentIndex(_jetSession, _jetEntityCursor, _jetEntityCursor.PrimaryIndex);
            Api.MakeKey(_jetSession, _jetEntityCursor, posLabel, MakeKeyGrbit.NewKey);
            if (Api.TrySeek(_jetSession, _jetEntityCursor, SeekGrbit.SeekEQ))
                return Api.RetrieveColumn(_jetSession, _jetEntityCursor, _jetEntityCursor.ColIdEntityData);
            else
                return null;
        }




        public void SaveAs(XbimStorageType _storageType, string _storageFileName, ReportProgressDelegate progress = null)
        {
            switch (_storageType)
            {
                case XbimStorageType.IFCXML:
                    SaveAsIfcXml(_storageFileName);
                    break;
                case XbimStorageType.IFC:
                    SaveAsIfc(_storageFileName);
                    break;
                case XbimStorageType.IFCZIP:
                    SaveAsIfcZip(_storageFileName);
                    break;
                case XbimStorageType.XBIM:
                    SaveAsXbim(_storageFileName);
                    break;
                case XbimStorageType.INVALID:
                default:
                    break;
            }

        }

        private void SaveAsIfcZip(string storageFileName)
        {
            throw new NotImplementedException();
        }

        private void SaveAsXbim(string storageFileName)
        {
            throw new NotImplementedException();
        }

        private void SaveAsIfc(string storageFileName)
        {

            try
            {
                using (TextWriter tw = new StreamWriter(storageFileName))
                {
                    Part21FileWriter p21 = new Part21FileWriter();
                    p21.Write(_model, tw);
                }
            }
            catch (Exception e)
            {
                throw new XbimException("Failed to write Ifc file " + storageFileName, e);
            }

        }

        private void SaveAsIfcXml(string storageFileName)
        {
            FileStream xmlOutStream = null;
            try
            {
                xmlOutStream = new FileStream(storageFileName, FileMode.Create, FileAccess.ReadWrite);
                XmlWriterSettings settings = new XmlWriterSettings { Indent = true };
                using (XmlWriter xmlWriter = XmlWriter.Create(xmlOutStream, settings))
                {
                    IfcXmlWriter writer = new IfcXmlWriter();
                    writer.Write(_model, xmlWriter);
                }
            }
            catch (Exception e)
            {
                throw new XbimException("Failed to write IfcXml file " + storageFileName, e);
            }
            finally
            {
                if (xmlOutStream != null) xmlOutStream.Close();
            }
        }



        internal void Save()
        {
            throw new NotImplementedException();
        }


        public void Delete_Reversable(IPersistIfcEntity instance)
        {
            throw new NotImplementedException();
        }

        public void Update_Reversible(IPersistIfcEntity entity)
        {
            throw new NotImplementedException();
        }




        public bool Saved
        {
            get
            {
                throw new NotImplementedException();
            }

        }
        #region Support for Linq based indexed searching


        private static MemberExpression GetIndexablePropertyOnLeft<T>(Expression leftSide)
        {
            MemberExpression mex = leftSide as MemberExpression;
            if (leftSide.NodeType == ExpressionType.Call)
            {
                MethodCallExpression call = leftSide as MethodCallExpression;
                if (call.Method.Name == "CompareString")
                {
                    mex = call.Arguments[0] as MemberExpression;
                }
            }

            return mex;
        }


        private static object GetRight(Expression leftSide, Expression rightSide)
        {
            if (leftSide.NodeType == ExpressionType.Call)
            {
                MethodCallExpression call = leftSide as MethodCallExpression;
                if (call.Method.Name == "CompareString")
                {
                    LambdaExpression evalRight = Expression.Lambda(call.Arguments[1], null);
                    //Compile it, invoke it, and get the resulting hash
                    return (evalRight.Compile().DynamicInvoke(null));
                }
            }
            //rightside is where we get our hash...
            switch (rightSide.NodeType)
            {
                //shortcut constants, dont eval, will be faster
                case ExpressionType.Constant:
                    ConstantExpression constExp
                        = (ConstantExpression)rightSide;
                    return (constExp.Value);

                //if not constant (which is provably terminal in a tree), convert back to Lambda and eval to get the hash.
                default:
                    //Lambdas can be created from expressions... yay
                    LambdaExpression evalRight = Expression.Lambda(rightSide, null);
                    //Compile and invoke it, and get the resulting hash
                    return (evalRight.Compile().DynamicInvoke(null));
            }
        }

        public IEnumerable<T> Where<T>(Expression<Func<T, bool>> expr)
        {
            Type type = typeof(T);
            IfcType ifcType = IfcInstances.IfcEntities[type];
            PropertyInfo pKey = ifcType.PrimaryIndex;
            if (pKey != null) //we can use a secondary index to look up
            {
                //our indexes work from the hash values of that which is indexed, regardless of type
                object hashRight = null;

                //indexes only work on equality expressions here
                //this  matches "Property" = "Value"
                if (expr.Body.NodeType == ExpressionType.Equal)
                {
                    //Equality is a binary expression
                    BinaryExpression binExp = (BinaryExpression)expr.Body;
                    //Get some aliases for either side
                    Expression leftSide = binExp.Left;
                    Expression rightSide = binExp.Right;

                    hashRight = GetRight(leftSide, rightSide);

                    //if we were able to create a hash from the right side (likely)
                    MemberExpression returnedEx = GetIndexablePropertyOnLeft<T>(leftSide);
                    if (returnedEx != null)
                    {
                        //cast to MemberExpression - it allows us to get the property
                        MemberExpression propExp = returnedEx;
                        string property = propExp.Member.Name;

                        if (pKey.Name == property) //we have a primary key match
                        {
                            IPersistIfcEntity entity = hashRight as IPersistIfcEntity;
                            if (entity != null)
                                return OfType<T>(true, Math.Abs(entity.EntityLabel));
                        }
                    }
                }
            }

            //we cannot optimise so just do it
            return OfType<T>(true).Where<T>(expr.Compile());

        }
        #endregion

        public XbimGeometryTable BeginGeometryUpdate()
        {
            XbimGeometryTable table = new XbimGeometryTable(_jetSession,_jetDatabaseId,OpenTableGrbit.Updatable);
            table.BeginTransaction();
            return table;
        }

        public void EndGeometryUpdate(XbimGeometryTable table)
        {
            table.CommitTransaction();
            table.Close();
        }

        
    }
}


