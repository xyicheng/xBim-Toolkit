using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Isam.Esent.Interop;
using Xbim.Common.Exceptions;
using Xbim.Ifc2x3.Kernel;
using Xbim.XbimExtensions;
using Microsoft.Isam.Esent.Interop.Windows7;

namespace Xbim.IO
{
    public class XbimGeometryCursor : XbimCursor
    {
       
    
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

     
        private JET_COLUMNID _colIdProductLabel;
        private JET_COLUMNID _colIdGeomType;
        private JET_COLUMNID _colIdProductIfcTypeId;
        private JET_COLUMNID _colIdRepItem;
        private JET_COLUMNID _colIdShapeData;
        private JET_COLUMNID _colIdSubPart;
        private JET_COLUMNID _colIdTransformMatrix;
        Int32ColumnValue _colValGeometryProductLabel;
        ByteColumnValue _colValGeomType;
        Int16ColumnValue _colValProductIfcTypeId;
        Int16ColumnValue _colValSubPart;
        BytesColumnValue _colValTransformMatrix;  
        BytesColumnValue _colValShapeData;
        Int32ColumnValue _colValRepItem;

        ColumnValue[] _colValues;
        

       
        internal static void CreateTable(JET_SESID sesid, JET_DBID dbid)
        {
            JET_TABLEID tableid;
            Api.JetCreateTable(sesid, dbid, GeometryTableName, 8, 80, out tableid);

            using (var transaction = new Microsoft.Isam.Esent.Interop.Transaction(sesid))
            {
                JET_COLUMNID columnid;

                var columndef = new JET_COLUMNDEF
                {
                    coltyp = JET_coltyp.Long,
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
                if (EsentVersion.SupportsWindows7Features)
                    columndef.grbit |= Windows7Grbits.ColumnCompressed;
                Api.JetAddColumn(sesid, tableid, colNameShapeData, columndef, null, 0, out columnid);

                columndef.coltyp = JET_coltyp.Long;
                columndef.grbit = ColumndefGrbit.ColumnMaybeNull;
                Api.JetAddColumn(sesid, tableid, colNameRepItem, columndef, null, 0, out columnid);

                // The primary index is the type and the entity label.
                string indexDef = string.Format("+{0}\0{1}\0{2}\0\0", colNameGeomType, colNameProductLabel, colNameSubPart);
                Api.JetCreateIndex(sesid, tableid, geometryTablePrimaryIndex, CreateIndexGrbit.IndexPrimary, indexDef, indexDef.Length, 100);
                Api.JetCloseTable(sesid, tableid);
                transaction.Commit(CommitTransactionGrbit.LazyFlush);
            }
           
        }
        public string PrimaryIndex { get { return geometryTablePrimaryIndex; } }

        private void InitColumns()
        {
            //this call causes temprary databases t be created
           //IDictionary<string, JET_COLUMNID> columnids = Api.GetColumnDictionary(_jetSession, _jetCursor);
            _colIdGeomType = Api.GetTableColumnid(sesid, table, colNameGeomType);
            _colIdProductIfcTypeId = Api.GetTableColumnid(sesid, table, colNameProductIfcTypeId);
            _colIdProductLabel = Api.GetTableColumnid(sesid, table, colNameProductLabel);
            _colIdSubPart = Api.GetTableColumnid(sesid, table, colNameSubPart);
            _colIdTransformMatrix = Api.GetTableColumnid(sesid, table, colNameTransformMatrix);
            _colIdShapeData = Api.GetTableColumnid(sesid, table, colNameShapeData);
            _colIdRepItem = Api.GetTableColumnid(sesid, table, colNameRepItem);
           

            _colValGeomType = new ByteColumnValue { Columnid = _colIdGeomType };
            _colValProductIfcTypeId = new Int16ColumnValue { Columnid = _colIdProductIfcTypeId };
            _colValGeometryProductLabel = new Int32ColumnValue { Columnid = _colIdProductLabel };
            _colValSubPart = new Int16ColumnValue { Columnid = _colIdSubPart };
            _colValTransformMatrix = new BytesColumnValue { Columnid = _colIdTransformMatrix };
            _colValShapeData = new BytesColumnValue { Columnid = _colIdShapeData };
            _colValRepItem = new Int32ColumnValue { Columnid = _colIdRepItem };
            _colValues = new ColumnValue[] { _colValGeomType, _colValGeometryProductLabel, _colValProductIfcTypeId, _colValSubPart, _colValTransformMatrix, _colValShapeData, _colValRepItem };

        }
        public XbimGeometryCursor(JET_INSTANCE instance, string database)
            : this(instance, database, OpenDatabaseGrbit.None)
        {
        }
        public XbimGeometryCursor(JET_INSTANCE instance, string database, OpenDatabaseGrbit mode)
            : base(instance, database, mode)
        {
            Api.JetOpenTable(this.sesid, this.dbId, GeometryTableName, null, 0, mode == OpenDatabaseGrbit.ReadOnly ? OpenTableGrbit.ReadOnly :
                                                                                mode == OpenDatabaseGrbit.Exclusive ? OpenTableGrbit.DenyWrite : OpenTableGrbit.None,
                                                                                out this.table);
            InitColumns();
        }
       
        public void AddGeometry(int prodLabel, XbimGeometryType type, short ifcType, byte[] transform, byte[] shapeData, int repItemLabel=0, short subPart = 0)
        {

            using (var update = new Update(sesid, table, JET_prep.Insert))
            {
                _colValGeometryProductLabel.Value = prodLabel;
                _colValGeomType.Value = (Byte)type;
                _colValProductIfcTypeId.Value = ifcType;
                _colValSubPart.Value = subPart;
                _colValTransformMatrix.Value = transform;
                _colValShapeData.Value = shapeData;
                _colValRepItem.Value = repItemLabel;
                Api.SetColumns(sesid, table, _colValues);
                UpdateCount(1);
                update.Save();
                
            }
        }

        internal XbimGeometryData GeometryData(IfcProduct product, XbimGeometryType geomType)
        {
            int posLabel = Math.Abs(product.EntityLabel);
            Api.JetSetCurrentIndex(sesid, table, geometryTablePrimaryIndex);
            Api.MakeKey(sesid, table, (byte)geomType, MakeKeyGrbit.NewKey);
            Api.MakeKey(sesid, table, posLabel, MakeKeyGrbit.None);
            if (Api.TrySeek(sesid, table, SeekGrbit.SeekEQ))
            {
                Api.RetrieveColumns(sesid, table, _colValues);
                return new XbimGeometryData(posLabel, (XbimGeometryType) _colValGeomType.Value, _colValProductIfcTypeId.Value.Value,_colValShapeData.Value,_colValTransformMatrix.Value, _colValRepItem.Value.Value);
            }
            else
                return null;
        }

        internal IEnumerable<XbimGeometryData> Shapes(XbimGeometryType ofType)
        {

            Api.JetSetCurrentIndex(sesid, table, geometryTablePrimaryIndex);
            Api.MakeKey(sesid, table, (byte)ofType, MakeKeyGrbit.NewKey);

            if (Api.TrySeek(sesid, table, SeekGrbit.SeekGE))
            {
                do
                {
                    Api.RetrieveColumns(sesid, table, _colValues);
                    yield return new XbimGeometryData(_colValGeometryProductLabel.Value.Value, (XbimGeometryType)_colValGeomType.Value, _colValProductIfcTypeId.Value.Value, _colValShapeData.Value, _colValTransformMatrix.Value, _colValRepItem.Value.Value);
                } while (Api.TryMoveNext(sesid, table));

            }
        }

        /// <summary>
        /// Retrieve the count of geometry items in the database from the globals table.
        /// </summary>
        /// <returns>The number of items in the database.</returns>
        override internal int RetrieveCount()
        {
            return (int)Api.RetrieveColumnAsInt32(this.sesid, this.globalsTable, this.geometryCountColumn);
        }

        /// <summary>
        /// Update the count of geometry entities in the globals table. This is done with EscrowUpdate
        /// so that there won't be any write conflicts.
        /// </summary>
        /// <param name="delta">The delta to apply to the count.</param>
        override protected void UpdateCount(int delta)
        {
            Api.EscrowUpdate(this.sesid, this.globalsTable, this.geometryCountColumn, delta);
        }
    }
}
