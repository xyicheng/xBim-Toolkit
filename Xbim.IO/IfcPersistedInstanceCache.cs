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
using System.Diagnostics;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.GeometryResource;

namespace Xbim.IO
{
    
    public class IfcPersistedInstanceCache : IDisposable
    {
        #region ESE Database 

        internal static Instance JetInstance;

        /// <summary>
        /// Holds the session and transaction state
        /// </summary>
        private readonly object lockObject;
        private readonly XbimEntityTable[] _entityTables;
        private readonly XbimGeometryTable[] _geometryTables;
        private const int MaxCachedEntityTables = 32;
        private const int MaxCachedGeometryTables = 32;


        const int _transactionBatchSize = 100;


       
        #endregion
        #region Cached data
        protected Dictionary<int, IPersistIfcEntity> read = new Dictionary<int, IPersistIfcEntity>();
        protected HashSet<IPersistIfcEntity> modified = new HashSet<IPersistIfcEntity>();
        //protected HashSet<IPersistIfcEntity> ToDelete = new HashSet<IPersistIfcEntity>();
        //protected HashSet<IPersistIfcEntity> ToCreate = new HashSet<IPersistIfcEntity>();
        protected int CacheDefaultSize = 5000;

        #endregion

        private string _databaseName;
        private XbimModel _model;
        private bool disposed = false;
       
        public IfcPersistedInstanceCache(XbimModel model)
        {
            this.lockObject = new Object();
            _model = model;
            _entityTables = new XbimEntityTable[MaxCachedEntityTables];
            _geometryTables = new XbimGeometryTable[MaxCachedGeometryTables];
        }
        static IfcPersistedInstanceCache()
        {
            JetInstance = new Instance("XbimDBInstance");
            JetInstance.Parameters.BaseName = "XBM";
            // _jetInstance.Parameters.TempDirectory = Path.GetRandomFileName();
            JetInstance.Parameters.CreatePathIfNotExist = true;
            JetInstance.Parameters.CircularLog = true;
            JetInstance.Parameters.CleanupMismatchedLogFiles = true;
            //JetInstance.Parameters.Recovery = false; //By default its True, only set this if we plan to write
            SystemParameters.CacheSizeMin = 16 * 1024;
            JetInstance.Parameters.LogFileSize = 16 * 1024;
            JetInstance.Parameters.LogBuffers = 8 * 1024;
           // JetInstance.Parameters.MaxTemporaryTables = 0;
            JetInstance.Init();
        }
       
        /// <summary>
        /// Creates an empty xbim file, overwrites any existing file of the same name
        /// </summary>
        /// <returns></returns>
        public bool CreateDatabase(string fileName)
        {
            _databaseName = fileName;
            return IfcPersistedInstanceCache.CreateDB(fileName);
        }

        private static bool CreateDB(string fileName)
        {
            using (var session = new Session(JetInstance))
            {
                JET_DBID dbid;

                Api.JetCreateDatabase(session, fileName, null, out dbid, CreateDatabaseGrbit.OverwriteExisting);
                try
                {
                    using (var transaction = new Microsoft.Isam.Esent.Interop.Transaction(session))
                    {
                        XbimDBTable.CreateGlobalsTable(session, dbid); //create the gobals table
                        XbimEntityTable.CreateTable(session, dbid);
                        XbimGeometryTable.CreateTable(session, dbid);
                        transaction.Commit(CommitTransactionGrbit.LazyFlush);
                    }
                    return true;
                }
                catch
                {
                    Api.JetCloseDatabase(session, dbid, CloseDatabaseGrbit.None);
                    Api.JetDetachDatabase(session, fileName);
                    File.Delete(fileName);
                    throw;
                }
            }
        }


        #region Table functions

        /// <summary>
        /// Returns a cached or new entity table, assumes the database filename has been specified
        /// </summary>
        /// <returns></returns>
        internal XbimEntityTable GetEntityTable()
        {
            Debug.Assert(!string.IsNullOrEmpty(_databaseName));
            lock (this.lockObject)
            {
                for (int i = 0; i < this._entityTables.Length; ++i)
                {
                    if (null != this._entityTables[i])
                    {
                        var table = this._entityTables[i];
                        this._entityTables[i] = null;
                        return table;
                    }
                }
            }
            return new XbimEntityTable(JetInstance, _databaseName); ;
        }

        /// <summary>
        /// Returns a cached or new Geometry Table, assumes the database filename has been specified
        /// </summary>
        /// <returns></returns>
        internal XbimGeometryTable GetGeometryTable()
        {
            Debug.Assert(!string.IsNullOrEmpty(_databaseName));
            lock (this.lockObject)
            {
                for (int i = 0; i < this._geometryTables.Length; ++i)
                {
                    if (null != this._geometryTables[i])
                    {
                        var table = this._geometryTables[i];
                        this._geometryTables[i] = null;
                        return table;
                    }
                }
            }
            return new XbimGeometryTable(JetInstance, _databaseName); ;
        }

        /// <summary>
        /// Free a table. This will cache the table if the cache isn't full
        /// and dispose of it otherwise.
        /// </summary>
        /// <param name="table">The cursor to free.</param>
        internal void FreeTable(XbimEntityTable table)
        {
            Debug.Assert(null != table, "Freeing a null table");

            lock (this.lockObject)
            {
                for (int i = 0; i < this._entityTables.Length; ++i)
                {
                    if (null == this._entityTables[i])
                    {
                        this._entityTables[i] = table;
                        return;
                    }
                }
            }

            // Didn't find a slot to cache the cursor in, throw it away
            table.Dispose();
        }

        /// <summary>
        /// Free a table. This will cache the table if the cache isn't full
        /// and dispose of it otherwise.
        /// </summary>
        /// <param name="table">The cursor to free.</param>
        public void FreeTable(XbimGeometryTable table)
        {
            Debug.Assert(null != table, "Freeing a null table");

            lock (this.lockObject)
            {
                for (int i = 0; i < this._geometryTables.Length; ++i)
                {
                    if (null == this._geometryTables[i])
                    {
                        this._geometryTables[i] = table;
                        return;
                    }
                }
            }

            // Didn't find a slot to cache the cursor in, throw it away
            table.Dispose();
        }
        #endregion


        /// <summary>
        ///  Opens an xbim model server file, exception is thrown if errors are encountered
        /// </summary>
        /// <param name="filename"></param>
        internal void Open(string filename)
        {
            Close();
            _databaseName = filename; //success store the name of the DB file
            XbimEntityTable entTable = GetEntityTable();
            try
            {
                using (var transaction = entTable.BeginReadOnlyTransaction())
                {
                    _model.Header = entTable.ReadHeader();
                }
            }
            catch (Exception e)
            {
                Close();
                throw new XbimException("Failed to open " + filename, e);
            }
            finally
            {
                FreeTable(entTable);
            }
        }

        /// <summary>
        /// Clears all contents fromthe cache and closes any connections
        /// </summary>
        public void Close()
        {
            for (int i = 0; i < this._entityTables.Length; ++i)
            {
                if (null != this._entityTables[i])
                {
                    this._entityTables[i].Dispose();
                    this._entityTables[i] = null;
                }
            }
            for (int i = 0; i < this._geometryTables.Length; ++i)
            {
                if (null != this._geometryTables[i])
                {
                    this._geometryTables[i].Dispose();
                    this._geometryTables[i] = null;
                }
            }
            this.read.Clear();

        }


        

        /// <summary>
        /// Imports the contents of the ifc file into the named database
        /// </summary>
        /// <param name="progressHandler"></param>
        /// <returns></returns>
        public void ImportIfc(string toImportIfcFilename, ReportProgressDelegate progressHandler = null)
        {

            var table = GetEntityTable();
            try
            {
                using (var transaction = table.BeginLazyTransaction())
                {
                    using (FileStream reader = new FileStream(toImportIfcFilename, FileMode.Open, FileAccess.Read))
                    {
                        using (P21toIndexParser part21Parser = new P21toIndexParser(reader, table, transaction))
                        {
                            if (progressHandler != null) part21Parser.ProgressStatus += progressHandler;
                            part21Parser.Parse();
                            _model.Header = part21Parser.Header;
                            table.WriteHeader(part21Parser.Header);
                            if (progressHandler != null) part21Parser.ProgressStatus -= progressHandler;
                        }
                    } 
                    transaction.Commit();
                    
                }
            }
            catch (Exception e)
            {
                throw new XbimException("Error importing Ifc File " + toImportIfcFilename, e);
            }
            finally
            { 
                this.FreeTable(table);
            }
           
        }

        /// <summary>
        ///   Imports an Xml file memory model into the model server, only call when the database instances table is empty
        /// </summary>

        public void ImportIfcXml(string xmlFilename, ReportProgressDelegate progressHandler = null)
        {
            var entityTable = GetEntityTable();
            try
            {
                using (var transaction = entityTable.BeginLazyTransaction())
                {
                    XmlReaderSettings settings = new XmlReaderSettings() { IgnoreComments = true, IgnoreWhitespace = false };
                    using (Stream xmlInStream = new FileStream(xmlFilename, FileMode.Open, FileAccess.Read))
                    {
                        using (XmlReader xmlReader = XmlReader.Create(xmlInStream, settings))
                        {
                            IfcXmlReader reader = new IfcXmlReader();
                            _model.Header = reader.Read(this, xmlReader);
                            entityTable.WriteHeader(_model.Header);
                        }
                    }

                    transaction.Commit();
                }
            }
            finally
            {
                FreeTable(entityTable);
            }
        }


        public bool Contains(IPersistIfcEntity instance)
        {
            return Contains(Math.Abs(instance.EntityLabel));
        }

        public bool Contains(int posLabel)
        {
            if (this.read.ContainsKey(posLabel)) //check if it is cached
                return true;
            else //look in the database
            {
                var entityTable = GetEntityTable();
                try
                {
                    return entityTable.TrySeekEntityLabel(posLabel);
                }
                finally
                {
                    FreeTable(entityTable);
                }
            }
        }

       
       /// <summary>
        /// returns the number of instances of the specified type and its sub types
       /// </summary>
       /// <typeparam name="TIfcType"></typeparam>
       /// <returns></returns>
        public long CountOf<TIfcType>() where TIfcType : IPersistIfcEntity
        {
            return CountOf(typeof(TIfcType));
           
        }
        /// <summary>
        /// returns the number of instances of the specified type and its sub types
        /// </summary>
        /// <param name="theType"></param>
        /// <returns></returns>
        private long CountOf(Type theType)
        {
            long count = 0;
            IfcType ifcType = IfcMetaData.IfcType(theType);
            var entityTable = GetEntityTable();
            try
            {
                entityTable.SetOrderByType();
                foreach (Type t in ifcType.NonAbstractSubTypes)
                {
                    short? typeId = IfcMetaData.IfcTypeId(t);
                    if (typeId.HasValue && entityTable.TrySeekEntityType(typeId.Value))
                    {
                        do
                        {
                            count++;
                        } while (entityTable.TryMoveNext());
                    }
                }
            }
            finally
            {
                FreeTable(entityTable);
            }
            return count;
        }

        public bool Any<TIfcType>() where TIfcType : IPersistIfcEntity
        {
            IfcType ifcType = IfcMetaData.IfcType(typeof(TIfcType));
            var entityTable = GetEntityTable();
            try
            {
                entityTable.SetOrderByType();
                foreach (Type t in ifcType.NonAbstractSubTypes)
                {
                    short? typeId = IfcMetaData.IfcTypeId(t);
                    if (typeId.HasValue && entityTable.TrySeekEntityType(typeId.Value))
                        return true;
                }
            }
            finally
            {
                FreeTable(entityTable);
            }
            return false;
        }
        /// <summary>
        /// returns the number of instances in the model
        /// </summary>
        /// <returns></returns>
        public long Count 
        {
            get
            {
                var entityTable = GetEntityTable();
                try
                {
                    return entityTable.RetrieveCount();
                }
                finally
                {
                    FreeTable(entityTable);
                }
            }
        }

        /// <summary>
        /// returns the value of the highest current entity label
        /// </summary>
        public int HighestLabel
        {
            get
            {
                var entityTable = GetEntityTable();
                try
                {
                    return entityTable.RetrieveHighestLabel();
                }
                finally
                {
                    FreeTable(entityTable);
                }
                
            }
        }


        /// <summary>
        /// Creates a new instance this is a reversable action and should be used typically
        /// </summary>
        /// <param name="t"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        public IPersistIfcEntity CreateNew_Reversable(Type t, int label)
        {
            int posLabel = Math.Abs(label);
            IPersistIfcEntity entity = (IPersistIfcEntity)Activator.CreateInstance(t);
            entity.Bind(_model, posLabel); //bind it, the object is new and empty so the label is positive
            this.read.Add_Reversible(new KeyValuePair<int, IPersistIfcEntity>(posLabel, entity));
           // ToCreate.Add_Reversible(entity);
            return entity;
        }

        /// <summary>
        /// Creates a new instance, this is not a reversable action, and the instance is not cached
        /// It is for performance in import and export routines and should not be used in normal code
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal IPersistIfcEntity CreateNew(Type type, int label)
        {
            int posLabel = Math.Abs(label);
            IPersistIfcEntity entity = (IPersistIfcEntity)Activator.CreateInstance(type);
            entity.Bind(_model, posLabel); //bind it, the object is new and empty so the label is positive
            //this.Add(posLabel, entity);
            //ToCreate.Add(entity);
            return entity;
        }

        /// <summary>
        /// Deprecated. Use CountOf, returns the number of instances of the specified type
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        public long InstancesOfTypeCount(Type t)
        {
            return CountOf(t);
        }

       
        /// <summary>
        /// Returns an enumeration of handles to all instances in the database and in the cache
        /// </summary>
        public IEnumerable<XbimInstanceHandle> InstanceHandles
        {
            get
            {
                var entityTable = GetEntityTable();
                try
                {
                    entityTable.SetOrderByType();
                    if (entityTable.TryMoveFirst()) // we have something
                    {
                        do
                        {
                            yield return entityTable.GetInstanceHandle();
                        }
                        while (entityTable.TryMoveNext());
                    }
                }
                finally
                {
                    FreeTable(entityTable);
                }
            }
        }
        /// <summary>
        /// Returns an enumeration of handles to all instances in the database or the cache of specified type
        /// </summary>
        /// <returns></returns>
        public IEnumerable<XbimInstanceHandle> InstanceHandlesOfType<TIfcType>()
        {
            Type reqType = typeof(TIfcType);
            IfcType ifcType = IfcMetaData.IfcType(reqType);
            var entityTable = GetEntityTable();
            try
            {

                entityTable.SetOrderByType();
                foreach (Type t in ifcType.NonAbstractSubTypes)
                {
                    short? typeId = IfcMetaData.IfcTypeId(t);

                    if (typeId.HasValue && entityTable.TrySeekEntityType(typeId.Value))
                    {
                        do
                        {
                            XbimInstanceHandle ih = entityTable.GetInstanceHandle();
                            if (!this.read.ContainsKey(ih.EntityLabel)) //we have already returned the entity from the cache
                                yield return ih;
                        }
                        while (entityTable.TryMoveNext());
                    }
                }
            }
            finally
            {
                FreeTable(entityTable);
            }
        }

        /// <summary>
        /// Returns an instance of the entity with the specified label,
        /// if the instance has alrady been loaded it is returned from the caache
        /// if it has not been loaded a blank instance is loaded, i.e. will not have been activated
        /// </summary>
        /// <param name="label"></param>
        /// <returns></returns>
        public IPersistIfcEntity GetInstance(int label, bool loadProperties = false, bool unCached = false)
        {
            int posLabel = Math.Abs(label);
            IPersistIfcEntity entity;
            if (this.read.TryGetValue(posLabel, out entity))
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
        private IPersistIfcEntity GetInstanceFromStore(int posLabel, bool loadProperties = false, bool unCached = false)
        {
            var entityTable = GetEntityTable();
            try
            {
                using (var transaction = entityTable.BeginReadOnlyTransaction())
                {
                    entityTable.SetOrderByLabel();
                    if (entityTable.TrySeekEntityLabel(posLabel))
                    {
                        short currentIfcTypeId = entityTable.GetIfcType();
                        IPersistIfcEntity entity = (IPersistIfcEntity)Activator.CreateInstance(IfcMetaData.GetType(currentIfcTypeId));
                        if (loadProperties)
                        {
                            byte[] properties = entityTable.GetProperties();
                            entity.ReadEntityProperties(this, new BinaryReader(new MemoryStream(properties)), unCached);
                            entity.Bind(_model, posLabel); //a positive handle determines that the attributes of this entity have been loaded yet
                        }
                        else
                            entity.Bind(_model, -posLabel); //a negative handle determines that the attributes of this entity have not been loaded yet
                        if (!unCached)
                            this.read.Add(posLabel, entity);
                        return entity;
                    }
                }
            }
            finally
            {
                FreeTable(entityTable);
            }
            return null;
            
        }

        public void Print()
        {
            Debug.WriteLine(InstanceHandles.Count());
                
            Debug.WriteLine(HighestLabel);
            Debug.WriteLine(Count);
            Debug.WriteLine(GeometriesCount());
           // Debug.WriteLine(Any<Xbim.Ifc2x3.SharedBldgElements.IfcWall>());
            //Debug.WriteLine(Count<Xbim.Ifc2x3.SharedBldgElements.IfcWall>());
            //IEnumerable<IfcElement> elems = OfType<IfcElement>();
            //foreach (var elem in elems)
            //{
            //    IEnumerable<IfcRelVoidsElement> rels = elem.HasOpenings;
            //    bool written = false;
            //    foreach (var rel in rels)
            //    {
            //        if (!written) { Debug.Write(elem.EntityLabel + " = "); written = true; }
            //        Debug.Write(rel.EntityLabel +", ");
            //    }
            //    if (written) Debug.WriteLine(";");
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
            IfcType ifcType = IfcMetaData.IfcType(typeof(TIfcType));
            var entityTable = GetEntityTable();
            try
            {
                using (var transaction = entityTable.BeginReadOnlyTransaction())
                {
                    if (activate)
                        entityTable.SetOrderByType(); //use the lookup order if we plan to load the objects properties, slower than just reading the index but we need to go to the cursor for the properties
                    else
                        entityTable.SetOrderByType(); //use the primary order if we only want empty object, these can be read from the index which is faster
                    foreach (Type t in ifcType.NonAbstractSubTypes)
                    {
                        short? typeId = IfcMetaData.IfcTypeId(t);
                        if (typeId.HasValue && entityTable.TrySeekEntityType(typeId.Value, secondaryKey))
                        {
                            do
                            {
                                
                                IPersistIfcEntity entity;
                                
                                XbimInstanceHandle ih;
                                if (activate) 
                                    ih= new XbimInstanceHandle(entityTable.GetLabel(),entityTable.GetIfcType());             
                                else
                                {
                                    ih = entityTable.GetInstanceHandle(); //use handles for faster lighter return
                                   
                                }

                                if (this.read.TryGetValue(ih.EntityLabel, out entity))
                                {
                                    if (activate && !entity.Activated) //activate if required and not already done
                                    {
                                        byte[] properties = entityTable.GetProperties();
                                        entity.ReadEntityProperties(this, new BinaryReader(new MemoryStream(properties)), false);
                                        entity.Bind(_model, ih.EntityLabel); //a positive handle determines that the attributes of this entity have been loaded yet
                                    }
                                    yield return (TIfcType)entity;
                                }
                                else
                                {

                                    entity = (IPersistIfcEntity)Activator.CreateInstance(ih.EntityType);
                                    if (activate)
                                    {
                                        byte[] properties = entityTable.GetProperties();
                                        entity.ReadEntityProperties(this, new BinaryReader(new MemoryStream(properties)), false);
                                        entity.Bind(_model, ih.EntityLabel); //a positive handle determines that the attributes of this entity have been loaded yet
                                    }
                                    else
                                        entity.Bind(_model, -ih.EntityLabel); //a negative handle determines that the attributes of this entity have not been loaded yet

                                    this.read.Add(ih.EntityLabel, entity);
                                    yield return (TIfcType)entity;
                                }
                            } while (entityTable.TryMoveNext());
                        }
                    }
                }
            }
            finally
            {
                FreeTable(entityTable);
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
                    Close();
                    GC.SuppressFinalize(this);
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
            var entityTable = GetEntityTable();
            try
            {
                using (var transaction = entityTable.BeginReadOnlyTransaction())
                {
                    entityTable.SetOrderByLabel();
                    int posLabel = Math.Abs(entity.EntityLabel);
                    if (entityTable.TrySeekEntityLabel(posLabel))
                        return entityTable.GetProperties();
                }
            }
            finally
            {
                FreeTable(entityTable);
            }
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

        /// <summary>
        /// Creates a copy of the current model, the storage of the new version is compressed to remove unused space in the model file
        /// </summary>
        /// <param name="storageFileName"></param>
        private void SaveAsXbim(string storageFileName)
        {
            
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



        internal void Commit()
        {
            throw new NotImplementedException();
        }


        public void Delete_Reversable(IPersistIfcEntity instance)
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
            bool indexFound = false;
            Type type = typeof(T);
            IfcType ifcType = IfcMetaData.IfcType(type);
            PropertyInfo pKey = ifcType.PrimaryIndex;
            Func<T, bool> predicate = expr.Compile();
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
                            {
                                indexFound=true;
                                foreach (var item in OfType<T>(true, Math.Abs(entity.EntityLabel)))
                                {
                                    if(predicate(item)) yield return item;
                                }
                            }
                        }
                    }
                }
            }

            //we cannot optimise so just do it
            if (!indexFound)
            {
                foreach (var item in OfType<T>(true))
                {
                    if (predicate(item)) yield return item;
                }
            }
        }
        #endregion

      

        



        public XbimGeometryData GetGeometry(IfcProduct product, XbimGeometryType geomType)
        {
           
            XbimGeometryTable geomTable = GetGeometryTable();
            using (var transaction = geomTable.BeginReadOnlyTransaction())
            {
                return geomTable.GeometryData(product, geomType);
            }
        }


        /// <summary>
        /// Iterates over all the shape geoemtry
        /// This is a thread safe operation and can be accessed in background threads
        /// </summary>
        /// <param name="ofType"></param>
        /// <returns></returns>
        public IEnumerable<XbimGeometryData> Shapes(XbimGeometryType ofType)
        {
            //Get a cached or open a new Table
            XbimGeometryTable geometryTable = GetGeometryTable();
            foreach (var shape in geometryTable.Shapes(ofType))
                yield return shape;
            FreeTable(geometryTable);
        }

        internal long GeometriesCount()
        {
            var geomTable = GetGeometryTable();
            try
            {
                return geomTable.RetrieveCount();
            }
            finally
            {
                FreeTable(geomTable);
            }
        }

        internal static bool Compact(string source, string target, out string error)
        {
            error = "";
            XbimModel sourceModel = new XbimModel();
            sourceModel.Open(source);
            if (CreateDB(target))
            {
                XbimModel targetModel = new XbimModel();
                targetModel.Open(target);
                sourceModel.Compact(targetModel);
            }
            return false;
            //try
            //{
            //    if(File.Exists(target)) File.Delete(target);
            //    Api.JetCompact(new Session(JetInstance), source,target, null, null, CompactGrbit.Stats);
            //    error = "";
            //    return true;
            //}
            //catch (Exception e)
            //{
            //    error = e.Message;
            //    return false;
            //} 
        }

        internal T InsertCopy<T>(T toCopy, XbimInstanceHandleMap mappings, bool includeInverses) where T : IPersistIfcEntity
        {
            XbimInstanceHandle toCopyHandle;
            if (mappings.TryGetValue(toCopy.GetHandle(), out toCopyHandle))
                return (T)this.GetInstance(toCopyHandle);
           
            IfcType ifcType = IfcMetaData.IfcType(toCopy);
            int copyLabel = Math.Abs(toCopy.EntityLabel);
            XbimInstanceHandle copyHandle = InsertNew(ifcType.Type);
            mappings.Add(toCopyHandle, copyHandle);
            if (typeof(IfcCartesianPoint) == ifcType.Type || typeof(IfcDirection) == ifcType.Type)//special cases for cartesian point and direction for efficiency
            {
                IPersistIfcEntity v = (IPersistIfcEntity)Activator.CreateInstance(ifcType.Type, new object[] { toCopy });  
                return (T)v;
            }
            else
            {
                
                IPersistIfcEntity theCopy = (IPersistIfcEntity)Activator.CreateInstance(copyHandle.EntityType);
                theCopy.Bind(_model, copyHandle.EntityLabel);
                IfcRoot rt = theCopy as IfcRoot;
                IEnumerable<IfcMetaProperty> props = ifcType.IfcProperties.Values.Where(p => !p.IfcAttribute.IsDerivedOverride);
                if (includeInverses)
                    props = props.Union(ifcType.IfcInverses);
                foreach (IfcMetaProperty prop in props)
                {
                    if (rt != null && prop.PropertyInfo.Name == "OwnerHistory") //don't add the owner history in as this will be changed later
                        continue;
                    object value = prop.PropertyInfo.GetValue(toCopy, null);
                    if (value != null)
                    {
                        bool isInverse = (prop.IfcAttribute.Order == -1); //don't try and set the values for inverses
                        Type theType = value.GetType();
                        //if it is an express type or a value type, set the value
                        if (theType.IsValueType || typeof(ExpressType).IsAssignableFrom(theType))
                        {
                            prop.PropertyInfo.SetValue(theCopy, value, null);
                        }
                        //else 
                        else if (!isInverse && typeof(IPersistIfcEntity).IsAssignableFrom(theType))
                        {
                            prop.PropertyInfo.SetValue(theCopy, InsertCopy((IPersistIfcEntity)value, mappings, includeInverses), null);
                        }
                        else if (!isInverse && typeof(ExpressEnumerable).IsAssignableFrom(theType))
                        {
                            Type itemType = theType.GetItemTypeFromGenericType();

                            ExpressEnumerable copyColl;
                            if (!theType.IsGenericType) //we have a class that inherits from a generic type
                                copyColl = (ExpressEnumerable)Activator.CreateInstance(theType, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { theCopy }, null);
                            else
                            {
                                Type genericType = theType.GetGenericTypeDefinition();
                                Type gt = genericType.MakeGenericType(new Type[] { itemType });
                                copyColl = (ExpressEnumerable)Activator.CreateInstance(gt, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, new object[] { theCopy }, null);
                            }
                            prop.PropertyInfo.SetValue(theCopy, copyColl, null);
                            foreach (var item in (ExpressEnumerable)value)
                            {
                                Type actualItemType = item.GetType();
                                if (actualItemType.IsValueType || typeof(ExpressType).IsAssignableFrom(actualItemType))
                                    copyColl.Add(item);
                                else if (typeof(IPersistIfcEntity).IsAssignableFrom(actualItemType))
                                    copyColl.Add(InsertCopy((IPersistIfcEntity)item, mappings, includeInverses));
                                else
                                    throw new XbimException(string.Format("Unexpected collection item type ({0}) found", itemType.Name));
                            }
                        }
                        else if (isInverse && value is IEnumerable<IPersistIfcEntity>) //just an enumeration of IPersistIfcEntity
                        {
                            foreach (var ent in (IEnumerable<IPersistIfcEntity>)value)
                                InsertCopy(ent, mappings, includeInverses);
                        }
                        else if (isInverse && value is IPersistIfcEntity) //it is an inverse and has a single value
                            InsertCopy((IPersistIfcEntity)value, mappings, includeInverses);
                        else
                            throw new XbimException(string.Format("Unexpected item type ({0})  found", theType.Name));
                    }
                }
              //  if (rt != null) rt.OwnerHistory = this.OwnerHistoryAddObject;
                return (T)theCopy;
            }
        }

        private IPersistIfcEntity GetInstance(XbimInstanceHandle map)
        {
            return GetInstance(map.EntityLabel);
        }


        private XbimInstanceHandle InsertNew(Type type)
        {

            XbimEntityTable table = GetEntityTable();

            try
            {
                using (var txn = table.BeginLazyTransaction())
                {
                    XbimInstanceHandle handle = table.AddEntity(type);
                    txn.Commit();
                    return handle;
                }

            }
            catch (Exception e)
            {
                throw;
            }
            finally
            {

                FreeTable(table);
            }
            
        }

        private int NextLabel()
        {
            return HighestLabel + 1;
        }

        internal void AddModified(IPersistIfcEntity entity)
        {
            modified.Add(entity);
        }
    }
}


