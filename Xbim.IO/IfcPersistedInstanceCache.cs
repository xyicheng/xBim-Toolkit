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

namespace Xbim.IO
{
    internal class IfcPersistedInstanceCache : Dictionary<long, IPersistIfcEntity>, IIfcInstanceCache, IDisposable
    {
        #region ESE Database fields

        static private Instance _jetInstance;
        private Session _jetSession;
        private JET_DBID _jetDatabaseId;
        private Table _jetEntityCursor;
        private Table _jetTypeCursor;
        private Table _jetActivateCursor;
        private JET_COLUMNID _columnidEntityLabel;
        private JET_COLUMNID _columnidSecondaryKey;
        private JET_COLUMNID _columnidIfcType;
        private JET_COLUMNID _columnidEntityData;
        Int64ColumnValue _colValEntityLabel;
        Int16ColumnValue _colValTypeId;
        BytesColumnValue _colValData;
        ColumnValue[] _colValues;

        const int _transactionBatchSize = 100;


        private const string _EntityTablePrimaryIndex = "primary";
        private const string _EntityTableTypeIndex = "secondary";

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
                    JET_TABLEID tableid;
                    Api.JetCreateTable(session, dbid, XbimModel.IfcInstanceTableName, 16, 80, out tableid);
                    CreateEntityTable(session, tableid);
                    Api.JetCloseTable(session, tableid);
                    JET_TABLEID headerTableid;
                    Api.JetCreateTable(session, dbid, XbimModel.IfcHeaderTableName, 1, 80, out headerTableid);
                    CreateHeaderTable(session, headerTableid);
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

        private void CreateHeaderTable(Session session, JET_TABLEID headerTableid)
        {
            using (var transaction = new Microsoft.Isam.Esent.Interop.Transaction(session))
            {
                JET_COLUMNID columnid;


                var columndef = new JET_COLUMNDEF
                {
                    coltyp = JET_coltyp.LongBinary,
                    grbit = ColumndefGrbit.ColumnNotNULL
                };

                Api.JetAddColumn(session, headerTableid, XbimModel.headerData, columndef, null, 0, out columnid);
                columndef = new JET_COLUMNDEF
                {
                    coltyp = JET_coltyp.Long,
                    grbit = ColumndefGrbit.ColumnAutoincrement
                };
                Api.JetAddColumn(session, headerTableid, XbimModel.headerId, columndef, null, 0, out columnid);

                transaction.Commit(CommitTransactionGrbit.LazyFlush);
            }
        }

        /// <summary>
        /// Creates the entity table
        /// </summary>
        /// <param name="sesid">The session to use.</param>
        /// <param name="tableid">
        /// The table to add the columns/indexes to. This table must be opened exclusively.
        /// </param>
        private static void CreateEntityTable(JET_SESID sesid, JET_TABLEID tableid)
        {
            using (var transaction = new Microsoft.Isam.Esent.Interop.Transaction(sesid))
            {
                JET_COLUMNID columnid;

                // Stock symbol : text column
                var columndef = new JET_COLUMNDEF
                {

                    coltyp = JET_coltyp.Currency,
                    grbit = ColumndefGrbit.ColumnNotNULL
                };

                Api.JetAddColumn(sesid, tableid, XbimModel.colNameEntityLabel, columndef, null, 0, out columnid);

                columndef.grbit = ColumndefGrbit.ColumnTagged;
                // Name of the secondary key : for lookup by a property value of the object that is a foreign object
                Api.JetAddColumn(sesid, tableid, XbimModel.colNameSecondaryKey, columndef, null, 0, out columnid);
                // Identity of the type of the object : 16-bit integer looked up in IfcType Table
                columndef = new JET_COLUMNDEF
                {
                    coltyp = JET_coltyp.Short,
                    grbit = ColumndefGrbit.ColumnNotNULL
                };
                Api.JetAddColumn(sesid, tableid, XbimModel.colNameIfcType, columndef, null, 0, out columnid);
                columndef = new JET_COLUMNDEF
                {
                    coltyp = JET_coltyp.LongBinary,
                    grbit = ColumndefGrbit.ColumnMaybeNull
                };
                Api.JetAddColumn(sesid, tableid, XbimModel.colNameEntityData, columndef, null, 0, out columnid);

                // Now add indexes. An index consists of several index segments (see
                // EsentVersion.Capabilities.ColumnsKeyMost to determine the maximum number of
                // segments). Each segment consists of a sort direction ('+' for ascending,
                // '-' for descending), a column name, and a '\0' separator. The index definition
                // must end with "\0\0". The count of characters should include all terminators.

                // The primary index is the type and the entity label.
                string indexDef = string.Format("+{0}\0\0", XbimModel.colNameEntityLabel);
                //string indexDef = string.Format("+{0}\0\0",  XbimModel.colNameEntityLabel);
                Api.JetCreateIndex(sesid, tableid, _EntityTablePrimaryIndex, CreateIndexGrbit.IndexPrimary, indexDef, indexDef.Length, 100);

                // An index on the type and secondary key. For quick access to IfcRelation entities and the like
                indexDef = string.Format("+{0}\0{1}\0\0", XbimModel.colNameIfcType,XbimModel.colNameSecondaryKey);
                Api.JetCreateIndex(sesid, tableid, _EntityTableTypeIndex, CreateIndexGrbit.IndexIgnoreAnyNull, indexDef, indexDef.Length, 100);

                transaction.Commit(CommitTransactionGrbit.LazyFlush);
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
                JET_wrn warning = Api.JetAttachDatabase(_jetSession, filename, AttachDatabaseGrbit.ReadOnly);
                warning = Api.JetOpenDatabase(_jetSession, filename, null, out _jetDatabaseId, OpenDatabaseGrbit.ReadOnly);

                _jetEntityCursor = new Table(_jetSession, _jetDatabaseId, XbimModel.IfcInstanceTableName, OpenTableGrbit.ReadOnly);
                _jetTypeCursor = new Table(_jetSession, _jetDatabaseId, XbimModel.IfcInstanceTableName, OpenTableGrbit.ReadOnly);
                _jetActivateCursor = new Table(_jetSession, _jetDatabaseId, XbimModel.IfcInstanceTableName, OpenTableGrbit.ReadOnly);
                IDictionary<string, JET_COLUMNID> columnids = Api.GetColumnDictionary(_jetSession, _jetEntityCursor);
                _columnidEntityLabel = columnids[XbimModel.colNameEntityLabel];
                _columnidSecondaryKey = columnids[XbimModel.colNameSecondaryKey];
                _columnidIfcType = columnids[XbimModel.colNameIfcType];
                _columnidEntityData = columnids[XbimModel.colNameEntityData];
                _colValEntityLabel = new Int64ColumnValue { Columnid = _columnidEntityLabel };
                _colValTypeId = new Int16ColumnValue { Columnid = _columnidIfcType };
                _colValData = new BytesColumnValue { Columnid = _columnidEntityData };
                _colValues = new ColumnValue[] { _colValEntityLabel, _colValTypeId, _colValData };
                // we have _header of the opened file, set that header to the Header property of XbimModelServer
                using (Table headerCursor = new Table(_jetSession, _jetDatabaseId, XbimModel.IfcHeaderTableName, OpenTableGrbit.None))
                {
                    Api.JetSetCurrentIndex(_jetSession, headerCursor, null);
                    Api.TryMoveFirst(_jetSession, headerCursor); //this should never fail
                    IDictionary<string, JET_COLUMNID> headerColIds = Api.GetColumnDictionary(_jetSession, headerCursor);
                    JET_COLUMNID dataId = headerColIds[XbimModel.headerData];
                    byte[] hd = Api.RetrieveColumn(_jetSession, headerCursor, dataId);
                    BinaryReader br = new BinaryReader(new MemoryStream(hd));
                    IfcFileHeader hdr = new IfcFileHeader();
                    hdr.Read(br);
                    _model.Header = hdr;
                }
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
            if (_jetActivateCursor != null)
            {
                _jetActivateCursor.Close();
                _jetActivateCursor = null;
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
        /// <param name="databaseName"></param>
        /// <param name="progressHandler"></param>
        /// <returns></returns>
        public void ImportIfc(string toImportIfcFilename, ReportProgressDelegate progressHandler = null)
        {

            using (var jetSession = new Session(_jetInstance))
            {
                JET_DBID dbid;

                Api.JetAttachDatabase(jetSession, _databaseName, AttachDatabaseGrbit.None);
                Api.JetOpenDatabase(jetSession, _databaseName, null, out dbid, OpenDatabaseGrbit.None);

                using (var table = new Table(jetSession, dbid, XbimModel.IfcInstanceTableName, OpenTableGrbit.None))
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
            using (var table = new Table(jetSession, dbid, XbimModel.IfcHeaderTableName, OpenTableGrbit.None))
            {
                IDictionary<string, JET_COLUMNID> columnids = Api.GetColumnDictionary(jetSession, table);
                JET_COLUMNID dataId = columnids[XbimModel.headerData];
                MemoryStream ms = new MemoryStream(4096);
                BinaryWriter bw = new BinaryWriter(ms);
                _model.Header.Write(bw);
                if (!Api.TryMoveFirst(jetSession, table)) //there is nothing in
                {
                    using (var update = new Update(jetSession, table, JET_prep.Insert))
                    {
                        Api.SetColumn(jetSession, table, dataId, ms.ToArray());
                        update.Save();
                    }
                }
                else
                {
                    using (var update = new Update(jetSession, table, JET_prep.Replace))
                    {
                        Api.SetColumn(jetSession, table, dataId, ms.ToArray());
                        update.Save();
                    }
                }

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
                using (_jetEntityCursor = new Table(_jetSession, dbid, XbimModel.IfcInstanceTableName, OpenTableGrbit.None))
                {
                    SetDatabaseColumns(_jetSession, _jetEntityCursor);
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
            IDictionary<string, JET_COLUMNID> columnids = Api.GetColumnDictionary(jetSession, jetEntityCursor);
            _columnidEntityLabel = columnids[XbimModel.colNameEntityLabel];
            _columnidSecondaryKey = columnids[XbimModel.colNameSecondaryKey];
            _columnidIfcType = columnids[XbimModel.colNameIfcType];
            _columnidEntityData = columnids[XbimModel.colNameEntityData];
            _colValEntityLabel = new Int64ColumnValue { Columnid = _columnidEntityLabel };
            _colValTypeId = new Int16ColumnValue { Columnid = _columnidIfcType };
            _colValData = new BytesColumnValue { Columnid = _columnidEntityData };
            _colValues = new ColumnValue[] { _colValEntityLabel, _colValTypeId, _colValData };
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

                MemoryStream ms = new MemoryStream(4096);
                toWrite.WriteEntity(new BinaryWriter(ms));
                _colValEntityLabel.Value = toWrite.EntityLabel;
                _colValTypeId.Value = toWrite.TypeId();
                _colValData.Value = ms.ToArray();
                Api.SetColumns(_jetSession, _jetEntityCursor, _colValues);
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
                Api.JetSetCurrentIndex(_jetSession, _jetEntityCursor, _EntityTablePrimaryIndex);
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
        /// <param name="id"></param>
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
                using (var enumCursor = new Table(_jetSession, _jetDatabaseId, XbimModel.IfcInstanceTableName, OpenTableGrbit.ReadOnly))
                {
                    Api.JetSetCurrentIndex(_jetSession, enumCursor, _EntityTablePrimaryIndex);
                    if (Api.TryMoveFirst(_jetSession, enumCursor))
                    {
                        do
                        {
                            long label = (long)Api.RetrieveColumnAsInt64(_jetSession, enumCursor, _columnidEntityLabel);
                            if (!this.ContainsKey(label)) //we have already returned the entity from the cache
                            {
                                short typeId = (short)Api.RetrieveColumnAsInt16(_jetSession, enumCursor, _columnidIfcType);

                                yield return new IfcInstanceHandle(label, IfcInstances.IfcIdIfcTypeLookup[typeId].Type);
                            }
                        }
                        while (Api.TryMoveNext(_jetSession, enumCursor));
                    }
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
                Api.JetSetCurrentIndex(_jetSession, _jetTypeCursor, _EntityTableTypeIndex);
                Api.MakeKey(_jetSession, _jetTypeCursor, typeId, MakeKeyGrbit.NewKey);
                if (Api.TrySeek(_jetSession, _jetTypeCursor, SeekGrbit.SeekGE))
                {
                    Api.MakeKey(_jetSession, _jetTypeCursor, typeId, MakeKeyGrbit.NewKey);
                    if (Api.TrySetIndexRange(_jetSession, _jetTypeCursor, SetIndexRangeGrbit.RangeUpperLimit | SetIndexRangeGrbit.RangeInclusive))
                    {
                        do
                        {
                            Int64 posLabel = (Int64)Api.RetrieveColumnAsInt64(_jetSession, _jetTypeCursor, _columnidEntityLabel); //it is a non null db value so just cast
                            if (!this.ContainsKey(posLabel)) //we have already returned the entity from the cache
                                yield return new IfcInstanceHandle(posLabel, t);
                        }
                        while (Api.TryMoveNext(_jetSession, _jetTypeCursor));
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
            Api.JetSetCurrentIndex(_jetSession, _jetEntityCursor, _EntityTablePrimaryIndex);
            Api.MakeKey(_jetSession, _jetEntityCursor, posLabel, MakeKeyGrbit.NewKey);
            if (Api.TrySeek(_jetSession, _jetEntityCursor, SeekGrbit.SeekEQ))
            {
                short? typeId = Api.RetrieveColumnAsInt16(_jetSession, _jetEntityCursor, _columnidIfcType);
                if (typeId.HasValue)
                {
                    IfcType ifcType = IfcInstances.IfcIdIfcTypeLookup[typeId.Value];
                    IPersistIfcEntity entity = (IPersistIfcEntity)Activator.CreateInstance(ifcType.Type);

                    if (loadProperties)
                    {
                        byte[] properties = Api.RetrieveColumn(_jetSession, _jetEntityCursor, _columnidEntityData);
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

        /// <summary>
        /// Enumerates of all instances of the specified type. The values are cached
        /// </summary>
        /// <typeparam name="TIfcType"></typeparam>
        /// <returns></returns>
        public IEnumerable<TIfcType> OfType<TIfcType>(long secondaryKey = -1)
        {
            IfcType ifcType = IfcInstances.IfcEntities[typeof(TIfcType)];
            foreach (Type t in ifcType.NonAbstractSubTypes)
            {
                short typeId = IfcInstances.IfcEntities[t].TypeId;
                Api.JetSetCurrentIndex(_jetSession, _jetTypeCursor, _EntityTableTypeIndex);
                Api.MakeKey(_jetSession, _jetTypeCursor, typeId, MakeKeyGrbit.NewKey);
                if (secondaryKey > -1) Api.MakeKey(_jetSession, _jetTypeCursor, null, MakeKeyGrbit.None);
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
                        do
                        {
                            Int64 posLabel = (Int64)Api.RetrieveColumnAsInt64(_jetSession, _jetTypeCursor, _columnidEntityLabel); //it is a non null db value so just cast
                            IPersistIfcEntity entity;
                            if (!this.TryGetValue(posLabel, out entity))//if already in the cache just return it, else create a blank
                            {
                                entity = (IPersistIfcEntity)Activator.CreateInstance(t);
                                entity.Bind(_model, -posLabel); //a negative handle determines that the attributes of this entity have not been loaded yet
                                base.Add(posLabel, entity);
                            }
                            yield return (TIfcType)entity;
                        }
                        while (Api.TryMoveNext(_jetSession, _jetTypeCursor));
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
                    if (_jetActivateCursor != null) { _jetActivateCursor.Close(); _jetActivateCursor.Dispose(); };
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
            Api.JetSetCurrentIndex(_jetSession, _jetEntityCursor, _EntityTablePrimaryIndex);
            Api.MakeKey(_jetSession, _jetEntityCursor, posLabel, MakeKeyGrbit.NewKey);
            if (Api.TrySeek(_jetSession, _jetEntityCursor, SeekGrbit.SeekEQ))
                return Api.RetrieveColumn(_jetSession, _jetEntityCursor, _columnidEntityData);
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
                            if(entity!=null)
                                return OfType<T>(Math.Abs(entity.EntityLabel));
                            //IEnumerable<long> values = indexColl.GetValues(property, hashRight);
                            //if (values != null)
                            //{
                            //    foreach (T item in values.Cast<T>())
                            //    {
                            //        yield return item;
                            //    }
                            //    noIndex = false;
                            //}
                        }
                    }
            }
            else if (expr.Body.NodeType == ExpressionType.Call) //this is for the contains calls
            {
                MethodCallExpression callExp = (MethodCallExpression)expr.Body;
                if (callExp.Method.Name == "Contains")
                {
                    Expression keyExpr = callExp.Arguments[0];
                    if (keyExpr.NodeType == ExpressionType.Constant)
                    {
                        ConstantExpression constExp = (ConstantExpression)keyExpr;
                        object key = constExp.Value;
                        if (callExp.Object.NodeType == ExpressionType.MemberAccess)
                        {
                            MemberExpression memExp = (MemberExpression)callExp.Object;

                            //string property = memExp.Member.Name;
                            //if (indexColl.HasIndex(property))
                            //{
                            //    IEnumerable<long> values = indexColl.GetValues(property, key);
                            //    if (values != null)
                            //    {
                            //        foreach (T item in values.Cast<T>())
                            //        {
                            //            yield return item;
                            //        }
                            //        noIndex = false;
                            //    }
                            //}
                        }
                    }
                }
            }
            }
            return null;
        }
        #endregion
    }
}


