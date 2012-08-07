using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Isam.Esent.Interop;
using Xbim.XbimExtensions.Interfaces;
using System.IO;

namespace Xbim.IO
{
    public class XbimEntityTable:IDisposable
    {
    
        Session _jetSession;
        JET_DBID _jetDatabaseId;

        private  const string ifcEntityTableName = "IfcEntities";
        
        private const string entityTablePrimaryIndex = "EntPrimary";
        private const string entityTableTypeIndex = "EntSecondary";
        
     

        private const string colNameEntityLabel = "EntityLabel";
        private const string colNameSecondaryKey = "SecondaryKey";
        private const string colNameIfcType = "IfcType";
        private const string colNameEntityData = "EntityData";

        private Table _jetCursor;

        private JET_COLUMNID _colIdEntityLabel;
        private JET_COLUMNID _colIdSecondaryKey;
        private JET_COLUMNID _colIdIfcType;
        private JET_COLUMNID _colIdEntityData;

        Int64ColumnValue _colValEntityLabel;
        Int64ColumnValue _colValSecondaryKey;
        Int16ColumnValue _colValTypeId;
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
            return (JET_TABLEID)(table._jetCursor);
        }
        

        internal static void CreateTable(JET_SESID sesid, JET_DBID dbid)
        {
            JET_TABLEID tableid;
            Api.JetCreateTable(sesid, dbid, ifcEntityTableName, 8, 80, out tableid);

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
            IDictionary<string, JET_COLUMNID> columnids = Api.GetColumnDictionary(_jetSession, _jetCursor);
            _colIdEntityLabel = columnids[colNameEntityLabel];
            _colIdSecondaryKey = columnids[colNameSecondaryKey];
            _colIdIfcType = columnids[colNameIfcType];
            _colIdEntityData = columnids[colNameEntityData];
            _colValEntityLabel = new Int64ColumnValue { Columnid = _colIdEntityLabel };
            _colValTypeId = new Int16ColumnValue { Columnid = _colIdIfcType };
            _colValSecondaryKey = new Int64ColumnValue { Columnid = _colIdSecondaryKey };
            _colValData = new BytesColumnValue { Columnid = _colIdEntityData };
            _colValues = new ColumnValue[] { _colValEntityLabel, _colValTypeId, _colValData };

        }
        /// <summary>
        /// Constructs a table but does not open it
        /// </summary>
        /// <param name="jetSession"></param>
        /// <param name="jetDatabaseId"></param>
        public XbimEntityTable(Session jetSession, JET_DBID jetDatabaseId)
        {
            _jetSession = jetSession;
            _jetDatabaseId = jetDatabaseId;
        }

        /// <summary>
        /// Constructs a table and opens in the specified mode
        /// </summary>
        /// <param name="jetSession"></param>
        /// <param name="dbid"></param>
        /// <param name="openTableGrbit"></param>
        public XbimEntityTable(Session jetSession, JET_DBID dbid, OpenTableGrbit openTableGrbit)
        {
            // TODO: Complete member initialization
            _jetSession = jetSession;
            _jetDatabaseId = dbid;
            Open(openTableGrbit);
        }

        /// <summary>
        /// Opens the table in the desired mode
        /// </summary>
        /// <param name="mode"></param>
        public void Open(OpenTableGrbit mode)
        {
            _jetCursor = new Table(_jetSession, _jetDatabaseId, ifcEntityTableName, mode);
            InitColumns();
        }
        public void Close()
        {
            Api.JetCloseTable(_jetSession, _jetCursor);
            _jetCursor = null;
        }

        public void Dispose()
        {
            if (_jetCursor != null) Close();
        }

        /// <summary>
        /// Sets the values of the fields, no update is performed
        /// </summary>
        /// <param name="primaryKey">The label of the entity</param>
        /// <param name="type">The index of the type of the entity</param>
        /// <param name="secondaryKey">specify a value less than 0 if no secondary key is required</param>
        /// <param name="data">The property data</param>
        internal void SetColumnValues(long primaryKey, short type, long secondaryKey, byte[] data)
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
        internal void SetColumnValues(long primaryKey, short type, byte[] data)
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
    }
}
