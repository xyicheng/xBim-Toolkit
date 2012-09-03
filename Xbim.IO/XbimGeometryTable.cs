using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Isam.Esent.Interop;
using Xbim.Common.Exceptions;
using Xbim.Ifc2x3.Kernel;
using Xbim.XbimExtensions;

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
        const string colNameProductIfcTypeId = "GeomIfcType";
        const string colNameSubPart = "GeomSubPart";     
        const string colNameTransformMatrix = "GeomTransformMatrix";
        const string colNameShapeData = "GeomShapeData";
        const string colNameRepItem = "GeomRepItemLabel";

        private Table _jetCursor;
        private JET_COLUMNID _colIdProductLabel;
        private JET_COLUMNID _colIdGeomType;
        private JET_COLUMNID _colIdProductIfcTypeId;
        private JET_COLUMNID _colIdRepItem;
        private JET_COLUMNID _colIdShapeData;
        private JET_COLUMNID _colIdSubPart;
        private JET_COLUMNID _colIdTransformMatrix;
        UInt64ColumnValue _colValGeometryProductLabel;
        ByteColumnValue _colValGeomType;
        UInt16ColumnValue _colValProductIfcTypeId;
        Int16ColumnValue _colValSubPart;
        BytesColumnValue _colValTransformMatrix;  
        BytesColumnValue _colValShapeData;
        UInt64ColumnValue _colValRepItem;

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
                
                columndef.coltyp = JET_coltyp.Short;
                Api.JetAddColumn(sesid, tableid, colNameProductIfcTypeId, columndef, null, 0, out columnid);
                Api.JetAddColumn(sesid, tableid, colNameSubPart, columndef, null, 0, out columnid);
                
               

                columndef.coltyp = JET_coltyp.Binary;
                Api.JetAddColumn(sesid, tableid, colNameTransformMatrix, columndef, null, 0, out columnid);
               
                columndef.coltyp = JET_coltyp.LongBinary;
                Api.JetAddColumn(sesid, tableid, colNameShapeData, columndef, null, 0, out columnid);

                columndef.coltyp = JET_coltyp.Currency;
                columndef.grbit = ColumndefGrbit.ColumnMaybeNull;
                Api.JetAddColumn(sesid, tableid, colNameRepItem, columndef, null, 0, out columnid);

                // The primary index is the type and the entity label.
                string indexDef = string.Format("+{0}\0{1}\0{2}\0\0", colNameGeomType, colNameProductLabel, colNameSubPart);
                Api.JetCreateIndex(sesid, tableid, geometryTablePrimaryIndex, CreateIndexGrbit.IndexPrimary, indexDef, indexDef.Length, 100);
                transaction.Commit(CommitTransactionGrbit.LazyFlush);
            }
            Api.JetCloseTable(sesid, tableid);
        }
        public string PrimaryIndex { get { return geometryTablePrimaryIndex; } }

        private void InitColumns()
        {
            //this call causes temprary databases t be created
           //IDictionary<string, JET_COLUMNID> columnids = Api.GetColumnDictionary(_jetSession, _jetCursor);
            _colIdGeomType = Api.GetTableColumnid(_jetSession, _jetCursor, colNameGeomType);
            _colIdProductIfcTypeId = Api.GetTableColumnid(_jetSession, _jetCursor, colNameProductIfcTypeId);
            _colIdProductLabel = Api.GetTableColumnid(_jetSession, _jetCursor, colNameProductLabel);
            _colIdSubPart = Api.GetTableColumnid(_jetSession, _jetCursor, colNameSubPart);
            _colIdTransformMatrix = Api.GetTableColumnid(_jetSession, _jetCursor, colNameTransformMatrix);
            _colIdShapeData = Api.GetTableColumnid(_jetSession, _jetCursor, colNameShapeData);
            _colIdRepItem = Api.GetTableColumnid(_jetSession, _jetCursor, colNameRepItem);
           

            _colValGeomType = new ByteColumnValue { Columnid = _colIdGeomType };
            _colValProductIfcTypeId = new UInt16ColumnValue { Columnid = _colIdProductIfcTypeId };
            _colValGeometryProductLabel = new UInt64ColumnValue { Columnid = _colIdProductLabel };
            _colValSubPart = new Int16ColumnValue { Columnid = _colIdSubPart };
            _colValTransformMatrix = new BytesColumnValue { Columnid = _colIdTransformMatrix };
            _colValShapeData = new BytesColumnValue { Columnid = _colIdShapeData };
            _colValRepItem = new UInt64ColumnValue { Columnid = _colIdRepItem };
            _colValues = new ColumnValue[] { _colValGeomType, _colValGeometryProductLabel, _colValProductIfcTypeId, _colValSubPart, _colValTransformMatrix, _colValShapeData, _colValRepItem };

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
        public void AddGeometry(ulong prodLabel, XbimGeometryType type, UInt16 ifcType, byte[] transform, byte[] shapeData, ulong repItemLabel=0, short subPart = 0)
        {

            using (var update = new Update(_jetSession, _jetCursor, JET_prep.Insert))
            {
                _colValGeometryProductLabel.Value = prodLabel;
                _colValGeomType.Value = (Byte)type;
                _colValProductIfcTypeId.Value = ifcType;
                _colValSubPart.Value = subPart;
                _colValTransformMatrix.Value = transform;
                _colValShapeData.Value = shapeData;
                _colValRepItem.Value = repItemLabel;
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

        internal XbimGeometryData GeometryData(IfcProduct product, XbimGeometryType geomType)
        {
            long posLabel = Math.Abs(product.EntityLabel);
            Api.JetSetCurrentIndex(_jetSession, _jetCursor, geometryTablePrimaryIndex);
            Api.MakeKey(_jetSession, _jetCursor, (byte)geomType, MakeKeyGrbit.NewKey);
            Api.MakeKey(_jetSession, _jetCursor, posLabel, MakeKeyGrbit.None);
            if (Api.TrySeek(_jetSession, _jetCursor, SeekGrbit.SeekEQ))
            {
                Api.RetrieveColumns(_jetSession, _jetCursor, _colValues);
                return new XbimGeometryData(posLabel, (XbimGeometryType) _colValGeomType.Value, _colValProductIfcTypeId.Value.Value,_colValShapeData.Value,_colValTransformMatrix.Value, (long)_colValRepItem.Value);
            }
            else
                return null;
        }

        internal IEnumerable<XbimGeometryData> Shapes(XbimGeometryType ofType)
        {

            Api.JetSetCurrentIndex(_jetSession, _jetCursor, geometryTablePrimaryIndex);
            Api.MakeKey(_jetSession, _jetCursor, (byte)ofType, MakeKeyGrbit.NewKey);

            if (Api.TrySeek(_jetSession, _jetCursor, SeekGrbit.SeekGE))
            {
                do
                {
                    Api.RetrieveColumns(_jetSession, _jetCursor, _colValues);
                    yield return new XbimGeometryData((long)_colValGeometryProductLabel.Value, (XbimGeometryType)_colValGeomType.Value, _colValProductIfcTypeId.Value.Value, _colValShapeData.Value, _colValTransformMatrix.Value, (long)_colValRepItem.Value);
                } while (Api.TryMoveNext(_jetSession, _jetCursor));

            }
        }
        
    }
}
