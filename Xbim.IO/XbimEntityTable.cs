using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Isam.Esent.Interop;
using Xbim.XbimExtensions.Interfaces;

using System.IO;

namespace Xbim.IO
{
    public class XbimEntityTable : XbimDBTable, IDisposable
    {

        private  const string ifcEntityTableName = "IfcEntities";
        private const string entityTablePrimaryIndex = "EntPrimary";
        private const string entityTableTypeIndex = "EntSecondary";

        private const string colNameEntityLabel = "EntityLabel";
        private const string colNameSecondaryKey = "SecondaryKey";
        private const string colNameIfcType = "IfcType";
        private const string colNameEntityData = "EntityData";

        private JET_COLUMNID _colIdEntityLabel;
        private JET_COLUMNID _colIdSecondaryKey;
        private JET_COLUMNID _colIdIfcType;
        private JET_COLUMNID _colIdEntityData;

        Int64ColumnValue _colValEntityLabel;
        Int64ColumnValue _colValSecondaryKey;
        UInt16ColumnValue _colValTypeId;
        BytesColumnValue _colValData;
        ColumnValue[] _colValues;


        public ColumnValue[] ColumnValues
        {
            get
            {
                return _colValues;
            }
        }

        public static implicit operator JET_TABLEID (XbimEntityTable table)
        {
            return table;
        }


        internal XbimLazyDBTransaction BeginLazyTransaction()
        {
            return new XbimLazyDBTransaction(this.sesid);
        }

        /// <summary>
        /// Begin a new transaction for this cursor. This is the cheapest
        /// transaction type because it returns a struct and no separate
        /// commit call has to be made.
        /// </summary>
        /// <returns>The new transaction.</returns>
        internal XbimReadOnlyDBTransaction BeginReadOnlyTransaction()
        {
            return new XbimReadOnlyDBTransaction(this.sesid);
        }

        internal static void CreateTable(JET_SESID sesid, JET_DBID dbid)
        {
            JET_TABLEID tableid;
            Api.JetCreateTable(sesid, dbid, ifcEntityTableName, 8, 100, out tableid);

            using (var transaction = new Microsoft.Isam.Esent.Interop.Transaction(sesid))
            {
                JET_COLUMNID columnid;

                // Stock symbol : text column
                var columndef = new JET_COLUMNDEF
                {
                    coltyp = JET_coltyp.Currency,
                    grbit = ColumndefGrbit.ColumnNotNULL
                };

                Api.JetAddColumn(sesid, tableid, colNameEntityLabel, columndef, null, 0, out columnid);

                columndef.grbit = ColumndefGrbit.ColumnTagged;
                // Name of the secondary key : for lookup by a property value of the object that is a foreign object
                Api.JetAddColumn(sesid, tableid, colNameSecondaryKey, columndef, null, 0, out columnid);
                // Identity of the type of the object : 16-bit integer looked up in IfcType Table
                columndef = new JET_COLUMNDEF
                {
                    coltyp = JET_coltyp.Short,
                    grbit = ColumndefGrbit.ColumnNotNULL
                };
                Api.JetAddColumn(sesid, tableid, colNameIfcType, columndef, null, 0, out columnid);
                columndef = new JET_COLUMNDEF
                {
                    coltyp = JET_coltyp.LongBinary,
                    grbit = ColumndefGrbit.ColumnMaybeNull
                };
                Api.JetAddColumn(sesid, tableid, colNameEntityData, columndef, null, 0, out columnid);

                // Now add indexes. An index consists of several index segments (see
                // EsentVersion.Capabilities.ColumnsKeyMost to determine the maximum number of
                // segments). Each segment consists of a sort direction ('+' for ascending,
                // '-' for descending), a column name, and a '\0' separator. The index definition
                // must end with "\0\0". The count of characters should include all terminators.

                // The primary index is the type and the entity label.
                string indexDef = string.Format("+{0}\0\0", colNameEntityLabel);
                //string indexDef = string.Format("+{0}\0\0",  XbimModel.colNameEntityLabel);
                Api.JetCreateIndex(sesid, tableid, entityTablePrimaryIndex, CreateIndexGrbit.IndexPrimary, indexDef, indexDef.Length, 100);

                // An index on the type and secondary key. For quick access to IfcRelation entities and the like
                indexDef = string.Format("+{0}\0{1}\0\0", colNameIfcType, colNameSecondaryKey);
                Api.JetCreateIndex(sesid, tableid, entityTableTypeIndex, CreateIndexGrbit.IndexIgnoreFirstNull, indexDef, indexDef.Length, 100);

                transaction.Commit(CommitTransactionGrbit.LazyFlush);
            }
        }
       

        private void InitColumns()
        {

           // IDictionary<string, JET_COLUMNID> columnids = Api.GetColumnDictionary(_jetSession, _jetCursor);
            _colIdEntityLabel = Api.GetTableColumnid(sesid, table, colNameEntityLabel);
            _colIdSecondaryKey = Api.GetTableColumnid(sesid, table, colNameSecondaryKey);
            _colIdIfcType = Api.GetTableColumnid(sesid, table, colNameIfcType);
            _colIdEntityData = Api.GetTableColumnid(sesid, table, colNameEntityData);
            
            _colValEntityLabel = new Int64ColumnValue { Columnid = _colIdEntityLabel };
            _colValTypeId = new UInt16ColumnValue { Columnid = _colIdIfcType };
            _colValSecondaryKey = new Int64ColumnValue { Columnid = _colIdSecondaryKey };
            _colValData = new BytesColumnValue { Columnid = _colIdEntityData };
            _colValues = new ColumnValue[] { _colValEntityLabel,_colValSecondaryKey, _colValTypeId, _colValData };

        }
        /// <summary>
        /// Constructs a table and opens it
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="database"></param>
        public XbimEntityTable(Instance instance, string database)
            : base(instance, database)
        {
            Api.JetOpenTable(this.sesid, this.dbId, ifcEntityTableName, null, 0, OpenTableGrbit.None, out this.table);
            InitColumns();
        }


        public void Dispose()
        {
            Api.JetEndSession(this.sesid, EndSessionGrbit.None);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Sets the values of the fields, no update is performed
        /// </summary>
        /// <param name="primaryKey">The label of the entity</param>
        /// <param name="type">The index of the type of the entity</param>
        /// <param name="secondaryKey">specify a value less than 0 if no secondary key is required</param>
        /// <param name="data">The property data</param>
        internal void SetColumnValues(long primaryKey, ushort type, long secondaryKey, byte[] data)
        {
            _colValEntityLabel.Value = primaryKey;
            _colValTypeId.Value = type;
            if (secondaryKey > -1)
                _colValSecondaryKey.Value = secondaryKey;
            else
                _colValSecondaryKey.Value = null;
            _colValData.Value = data.ToArray();
        }
        /// <summary>
        /// Sets the values of the fields, no update is performed
        /// </summary>
        /// <param name="primaryKey">The label of the entity</param>
        /// <param name="type">The index of the type of the entity</param>
        /// <param name="data">The property data</param>
        internal void SetColumnValues(long primaryKey, ushort type, byte[] data)
        {
            SetColumnValues(primaryKey, type, -1, data);
        }

        internal void SetColumnValues(IPersistIfcEntity toWrite)
        {
            MemoryStream ms = new MemoryStream(4096);
            toWrite.WriteEntity(new BinaryWriter(ms));

            IfcType ifcType = toWrite.IfcType();
            int secKeyIdx = ifcType.PrimaryKeyIndex;
            SetColumnValues(toWrite.EntityLabel, toWrite.TypeId(), -1, ms.ToArray());
        }

        public string PrimaryIndex { get { return entityTablePrimaryIndex; } }

        public JET_COLUMNID ColIdEntityLabel { get { return _colIdEntityLabel; } }

        public JET_COLUMNID ColIdIfcType { get { return _colIdIfcType; } }

        public string TypeIndex { get { return entityTableTypeIndex; } }

        public JET_COLUMNID ColIdEntityData { get { return _colIdEntityData; } }

        internal void WriteHeader(IIfcFileHeader ifcFileHeader, long _count)
        {
            MemoryStream ms = new MemoryStream(4096);
            BinaryWriter bw = new BinaryWriter(ms);
            ifcFileHeader.Write(bw);
            _colValEntityLabel.Value = 0;
            _colValTypeId.Value = 0;
            _colValData.Value = ms.GetBuffer();
            Api.JetSetCurrentIndex(sesid, table, entityTablePrimaryIndex);
            Api.MakeKey(sesid, table, _colValEntityLabel.Value.Value, MakeKeyGrbit.NewKey);
            if (!Api.TrySeek(sesid, table,SeekGrbit.SeekEQ)) //there is nothing in at the moment
            {
                using (var update = new Update(sesid, table, JET_prep.Insert))
                {
                    Api.SetColumns(sesid, table, _colValues);
                    update.Save();
                }
            }
            else
            {
                using (var update = new Update(sesid, table, JET_prep.Replace))
                {
                    Api.SetColumns(sesid, table, _colValues);
                    update.Save();
                }
            }
        }

        internal IIfcFileHeader ReadHeader()
        {
            Api.JetSetCurrentIndex(sesid, table, entityTablePrimaryIndex);
            Api.MakeKey(sesid, table, 0L, MakeKeyGrbit.NewKey);
            if (Api.TrySeek(sesid, table, SeekGrbit.SeekEQ)) //there is nothing in at the moment
            {
                byte[] hd = Api.RetrieveColumn(sesid, table, _colIdEntityData);
                BinaryReader br = new BinaryReader(new MemoryStream(hd));
                IfcFileHeader hdr = new IfcFileHeader();
                hdr.Read(br);
                return hdr;
            }
            else
                return null;
           
        }
        /// <summary>
        /// Adds an entity, assumes a valid transaction is running
        /// </summary>
        /// <param name="toWrite"></param>
        internal void AddEntity(IPersistIfcEntity toWrite)
        {
            using (var update = new Update(sesid, table, JET_prep.Insert))
            {
                _colValTypeId.Value = toWrite.TypeId();
                _colValEntityLabel.Value = toWrite.EntityLabel;
                MemoryStream ms = new MemoryStream();
                BinaryWriter bw = new BinaryWriter(ms);
                toWrite.WriteEntity(bw);
                _colValData.Value = ms.ToArray();
                Api.SetColumns(sesid, table, _colValues);
                update.Save();
            }
        }

        /// <summary>
        /// Adds an entity, assumes a valid transaction is running
        /// </summary>
        /// <param name="_currentLabel">Primary key/label</param>
        /// <param name="typeId">Type identifer</param>
        /// <param name="secondaryKeyValue">Secondary key</param>
        /// <param name="data">property data</param>
        internal void AddEntity(long _currentLabel, ushort typeId, long secondaryKeyValue, byte[] data)
        {
            using (var update = new Update(sesid, table, JET_prep.Insert))
            {
                SetColumnValues(_currentLabel, typeId, secondaryKeyValue, data.ToArray());
                Api.SetColumns(sesid, table, _colValues);
                update.Save();
            }
        }

        /// <summary>
        /// Returns true if the specified entity label is present in the table, assumes the current index has been set to by primary key (SetPrimaryIndex)
        /// </summary>
        /// <param name="key">The entity label to lookup</param>
        /// <returns></returns>
        public bool TrySeekEntityLabel(long key)
        {
           
            Api.MakeKey(sesid, table, key, MakeKeyGrbit.NewKey);
            return Api.TrySeek(this.sesid, this.table, SeekGrbit.SeekEQ);
        }
        /// <summary>
        /// Trys to move to the first entity of the specified type, assumes the current index has been set to order by type (SetOrderByType)
        /// </summary>
        /// <param name="typeId"></param>
        /// <returns></returns>
        public bool TrySeekEntityType(ushort typeId)
        {
            Api.MakeKey(sesid, table, typeId, MakeKeyGrbit.NewKey);
            if(Api.TrySeek(sesid, table, SeekGrbit.SeekGE))
            {
                Api.MakeKey(sesid, table, typeId, MakeKeyGrbit.NewKey);
                return Api.TrySetIndexRange(sesid, table, SetIndexRangeGrbit.RangeUpperLimit | SetIndexRangeGrbit.RangeInclusive);
            }
            else
                return false;

        }

        /// <summary>
        /// Trys to move to the first entity of the specified type, assumes the current index has been set to order by type (SetOrderByType)
        /// Secondar keys are specific t the type and defined as IfcAttributes in the class declaration
        /// </summary>
        /// <param name="typeId">the type of entity to look up</param>
        /// <param name="lookupKey">Secondary indexes on the search</param>
        /// <returns></returns>
        public bool TrySeekEntityType(ushort typeId, long lookupKey)
        {
            Api.MakeKey(sesid, table, typeId, MakeKeyGrbit.NewKey);
            Api.MakeKey(sesid, table, lookupKey, MakeKeyGrbit.None);
            if (Api.TrySeek(sesid, table, SeekGrbit.SeekGE))
            {
                Api.MakeKey(sesid, table, typeId, MakeKeyGrbit.NewKey);
                Api.MakeKey(sesid, table, lookupKey, MakeKeyGrbit.None);
                return Api.TrySetIndexRange(sesid, table, SetIndexRangeGrbit.RangeUpperLimit | SetIndexRangeGrbit.RangeInclusive);
            }
            else 
                return false;
        }


        /// <summary>
        /// Sets the order to be by entity label and type
        /// </summary>
        internal void SetPrimaryIndex()
        {
            Api.JetSetCurrentIndex(this.sesid, this.table, entityTablePrimaryIndex);
        }
        /// <summary>
        /// Sets the order to be by entity type
        /// </summary>
        internal void SetOrderByType()
        {
            Api.JetSetCurrentIndex(this.sesid, this.table, entityTableTypeIndex);
        }

        

        internal IfcInstanceHandle GetCurrentInstanceHandle()
        {
            long? label = Api.RetrieveColumnAsInt64(sesid, table, _colIdEntityLabel);
            ushort? typeId = Api.RetrieveColumnAsUInt16(sesid, table, _colIdIfcType);
            if (label.HasValue && typeId.HasValue)
                return new IfcInstanceHandle(label.Value, IfcInstances.IfcIdIfcTypeLookup[typeId.Value].Type);
            else
                return IfcInstanceHandle.Empty;
        }
        /// <summary>
        /// Gets the property values of the entity from the current record
        /// </summary>
        /// <returns>byte array of the property data in binary ifc format</returns>
        internal byte[] GetProperties()
        {
            return Api.RetrieveColumn(sesid, table, _colIdEntityData);
           
        }

       
    }
}
