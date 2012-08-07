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
        const string colNameRepresentationLabel = "GeomRepresentationLabel";
        const string colNameBoundingBoxData = "GeomBoundindBoxData";
        const string colNameShapeData = "GeomShapeData";

        private Table _jetCursor;
        private JET_COLUMNID _colIdProductLabel;
        private JET_COLUMNID _colIdRepresentationLabel;
        private JET_COLUMNID _colIdShapeData;
        private JET_COLUMNID _colIdBoundingBoxData;
        Int64ColumnValue _colValGeometryProductLabel;
        Int64ColumnValue _colValGeometryRepLabel;        
        BytesColumnValue _colValXbimGeometryData;
        BytesColumnValue _colValBBGeometryData;
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

                Api.JetAddColumn(sesid, tableid, colNameRepresentationLabel, columndef, null, 0, out columnid);
                
                Api.JetAddColumn(sesid, tableid, colNameProductLabel, columndef, null, 0, out columnid);
                columndef.coltyp = JET_coltyp.UnsignedByte;
                columndef.grbit = ColumndefGrbit.ColumnMaybeNull;

                // Name of the secondary key : for lookup by a property value of the object that is a foreign object
                Api.JetAddColumn(sesid, tableid, colNameBoundingBoxData, columndef, null, 0, out columnid);
                // Identity of the type of the object : 16-bit integer looked up in IfcType Table

                columndef.coltyp = JET_coltyp.LongBinary;
                columndef.grbit = ColumndefGrbit.ColumnMaybeNull;

                Api.JetAddColumn(sesid, tableid, colNameShapeData, columndef, null, 0, out columnid);

                // Now add indexes. An index consists of several index segments (see
                // EsentVersion.Capabilities.ColumnsKeyMost to determine the maximum number of
                // segments). Each segment consists of a sort direction ('+' for ascending,
                // '-' for descending), a column name, and a '\0' separator. The index definition
                // must end with "\0\0". The count of characters should include all terminators.

                // The primary index is the type and the entity label.
                string indexDef = string.Format("+{0}\0\0", colNameRepresentationLabel);

                Api.JetCreateIndex(sesid, tableid, geometryTablePrimaryIndex, CreateIndexGrbit.IndexPrimary, indexDef, indexDef.Length, 100);

                //// An index on the type and secondary key. For quick access to IfcRelation entities and the like
                //indexDef = string.Format("+{0}\0{1}\0\0", XbimModel.colNameIfcType,XbimModel.colNameSecondaryKey);
                //Api.JetCreateIndex(sesid, tableid, _EntityTableTypeIndex, CreateIndexGrbit.IndexIgnoreFirstNull, indexDef, indexDef.Length, 100);

                transaction.Commit(CommitTransactionGrbit.LazyFlush);
            }
            Api.JetCloseTable(sesid, tableid);
        }

        private void InitColumns()
        {
            IDictionary<string, JET_COLUMNID> columnids = Api.GetColumnDictionary(_jetSession, _jetCursor);
            _colIdRepresentationLabel = columnids[colNameRepresentationLabel];
            _colIdProductLabel = columnids[colNameProductLabel];
            _colIdBoundingBoxData = columnids[colNameBoundingBoxData];
            _colIdShapeData = columnids[colNameShapeData];
            _colValGeometryRepLabel = new Int64ColumnValue { Columnid = _colIdRepresentationLabel };
            _colValGeometryProductLabel = new Int64ColumnValue { Columnid = _colIdProductLabel };
            _colValBBGeometryData = new BytesColumnValue { Columnid = _colIdBoundingBoxData };
            _colValXbimGeometryData = new BytesColumnValue { Columnid = _colIdShapeData };
            _colValues = new ColumnValue[] { _colValGeometryRepLabel, _colValGeometryProductLabel, _colValBBGeometryData, _colValXbimGeometryData };

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
        public void AddGeometry(long prodLabel, long repLabel, short type, byte[] shapeData, byte[] boundingBoxData)
        {

            using (var update = new Update(_jetSession, _jetCursor, JET_prep.Insert))
            {
                _colValGeometryProductLabel.Value = prodLabel;
                _colValGeometryRepLabel.Value = repLabel;
                _colValXbimGeometryData.Value = shapeData;
                _colValBBGeometryData.Value = boundingBoxData;
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
