using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Isam.Esent.Interop;
using Xbim.Common.Exceptions;

namespace Xbim.IO
{
    public class XbimGeometryTable : IDisposable
    {
        const int _transactionBatchSize = 100;
        Session _jetSession;
        JET_DBID _jetDatabaseId;
        //geometry fields
        public static string GeometryTableName = "Geometry";

        const string geometryTablePrimaryIndex = "GeomPrimaryIndex";
        const string colNameProductLabel = "GeomProductLabel";
        const string colNameGeomType = "GeomType";
        const string colNameTransformMatrix = "GeomTransformMatrix";
        const string colNameShapeData = "GeomShapeData";

        private Table _jetCursor;
        private JET_COLUMNID _colIdProductLabel;
        private JET_COLUMNID _colIdGeomType;
        private JET_COLUMNID _colIdShapeData;
        private JET_COLUMNID _colIdTransformMatrix;
        UInt64ColumnValue _colValGeometryProductLabel;
        ByteColumnValue _colValGeomType;      
        BytesColumnValue _colValTransformMatrix;  
        BytesColumnValue _colValXbimGeometryData;
       
        ColumnValue[] _colValues;
        private Transaction _transaction;
        private int _added ;
        internal static void CreateTable(JET_SESID sesid, JET_DBID dbid)
        {
            JET_TABLEID tableid;
            Api.JetCreateTable(sesid, dbid, GeometryTableName, 8, 80, out tableid);

            using (var transaction = new Microsoft.Isam.Esent.Interop.Transaction(sesid))
            {
                JET_COLUMNID columnid;

                var columndef = new JET_COLUMNDEF
                {
                    coltyp = JET_coltyp.Currency,
                    grbit = ColumndefGrbit.ColumnNotNULL
                };
                Api.JetAddColumn(sesid, tableid, colNameProductLabel, columndef, null, 0, out columnid);

                columndef.coltyp = JET_coltyp.UnsignedByte;
                Api.JetAddColumn(sesid, tableid, colNameGeomType, columndef, null, 0, out columnid);
                
               
                columndef.coltyp = JET_coltyp.Binary;
                Api.JetAddColumn(sesid, tableid, colNameTransformMatrix, columndef, null, 0, out columnid);
               
                columndef.coltyp = JET_coltyp.LongBinary;
                Api.JetAddColumn(sesid, tableid, colNameShapeData, columndef, null, 0, out columnid);

                // The primary index is the type and the entity label.
                string indexDef = string.Format("+{0}\0{1}\0\0", colNameGeomType, colNameProductLabel);
                Api.JetCreateIndex(sesid, tableid, geometryTablePrimaryIndex, CreateIndexGrbit.IndexPrimary, indexDef, indexDef.Length, 100);
                transaction.Commit(CommitTransactionGrbit.LazyFlush);
            }
            Api.JetCloseTable(sesid, tableid);
        }

        private void InitColumns()
        {
            IDictionary<string, JET_COLUMNID> columnids = Api.GetColumnDictionary(_jetSession, _jetCursor);
            _colIdGeomType = columnids[colNameGeomType];
            _colIdProductLabel = columnids[colNameProductLabel];
            _colIdTransformMatrix = columnids[colNameTransformMatrix];
            _colIdShapeData = columnids[colNameShapeData];
            _colValGeomType = new ByteColumnValue { Columnid = _colIdGeomType };
            _colValGeometryProductLabel = new UInt64ColumnValue { Columnid = _colIdProductLabel };
            _colValTransformMatrix = new BytesColumnValue { Columnid = _colIdTransformMatrix };
            _colValXbimGeometryData = new BytesColumnValue { Columnid = _colIdShapeData };
            _colValues = new ColumnValue[] { _colValGeomType, _colValGeometryProductLabel, _colValTransformMatrix, _colValXbimGeometryData };

        }
        public XbimGeometryTable(Session jetSession, JET_DBID jetDatabaseId, OpenTableGrbit openTableGrbit)
        {
            _jetSession = jetSession;
            _jetDatabaseId = jetDatabaseId;
            Open(openTableGrbit);
        } 
        
        public XbimGeometryTable(Session jetSession, JET_DBID jetDatabaseId)
        {
            _jetSession = jetSession;
            _jetDatabaseId = jetDatabaseId;
        }
        public void Open(OpenTableGrbit mode)
        {
            _jetCursor = new Table(_jetSession, _jetDatabaseId, GeometryTableName, mode);
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

        internal void BeginTransaction()
        {
            if (_transaction != null) throw new XbimException("Cannot start a transaction, one is already active");
            _transaction = new Transaction(_jetSession);
            _added = 0;
        }
        public void AddGeometry(ulong prodLabel, XbimGeometryType type, byte[] transform, byte[] shapeData)
        {

            using (var update = new Update(_jetSession, _jetCursor, JET_prep.Insert))
            {
                _colValGeometryProductLabel.Value = prodLabel;
                _colValGeomType.Value = (Byte)type;
                _colValTransformMatrix.Value = transform;
                _colValXbimGeometryData.Value = shapeData;
                Api.SetColumns(_jetSession, _jetCursor, _colValues);
                update.Save();
                _added++;
                if (_added % _transactionBatchSize == (_transactionBatchSize - 1))
                {
                    Api.JetCommitTransaction(_jetSession, CommitTransactionGrbit.LazyFlush);
                    Api.JetBeginTransaction(_jetSession);
                }
            }
        }

        internal void CommitTransaction()
        {
            _transaction.Commit(CommitTransactionGrbit.None);
            _transaction.Dispose();
            _transaction = null;
        }
    }
}
