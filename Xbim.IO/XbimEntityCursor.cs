using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Isam.Esent.Interop;
using Xbim.XbimExtensions.Interfaces;

using System.IO;
using Microsoft.Isam.Esent.Interop.Windows7;
using Xbim.Common.Exceptions;
using System.Collections;

namespace Xbim.IO
{
    public class XbimEntityCursor : XbimCursor, IEnumerator<int>, IEnumerator
    {

        private  const string ifcEntityTableName = "IfcEntities";
        private const string entityTableTypeLabelIndex = "EntByTypeLabel";
        //private const string entityTableTypeIndex = "EntByType";
        private const string entityTableLabelIndex = "EntByLabel"; 
        private const string colNameEntityLabel = "EntityLabel";
        private const string colNameSecondaryKey = "SecondaryKey";
        private const string colNameIfcType = "IfcType";
        private const string colNameEntityData = "EntityData";
        private const string colNameIsIndexedClass = "IsIndexedClass";
        private JET_COLUMNID _colIdEntityLabel;
        private JET_COLUMNID _colIdSecondaryKey;
        private JET_COLUMNID _colIdIfcType;
        private JET_COLUMNID _colIdEntityData;
        private JET_COLUMNID _colIdIsIndexedClass;
        Int32ColumnValue _colValEntityLabel;
        Int32ColumnValue _colValSecondaryKey;
        Int16ColumnValue _colValTypeId;
        BytesColumnValue _colValData;
        BoolColumnValue _colValIsIndexedClass;
        ColumnValue[] _colValues;
        int current;


        public ColumnValue[] ColumnValues
        {
            get
            {
                return _colValues;
            }
        }

        public static implicit operator JET_TABLEID (XbimEntityCursor table)
        {
            return table;
        }



        internal static void CreateTable(JET_SESID sesid, JET_DBID dbid)
        {
            JET_TABLEID tableid;
           

            using (var transaction = new Microsoft.Isam.Esent.Interop.Transaction(sesid))
            {
                Api.JetCreateTable(sesid, dbid, ifcEntityTableName, 8, 100, out tableid);
                JET_COLUMNID columnid;
                var columndef = new JET_COLUMNDEF
                {
                    coltyp = JET_coltyp.Long,
                    grbit = ColumndefGrbit.ColumnNotNULL
                    
                };

                Api.JetAddColumn(sesid, tableid, colNameEntityLabel, columndef, null, 0, out columnid);
             
                columndef.grbit = ColumndefGrbit.ColumnTagged | ColumndefGrbit.ColumnMultiValued;
                // Name of the secondary key : for lookup by a property value of the object that is a foreign object
                Api.JetAddColumn(sesid, tableid, colNameSecondaryKey, columndef, null, 0, out columnid);
                // Identity of the type of the object : 16-bit integer looked up in IfcType Table
                columndef = new JET_COLUMNDEF
                {
                    coltyp = JET_coltyp.Short,
                    grbit = ColumndefGrbit.ColumnMaybeNull
                };
                Api.JetAddColumn(sesid, tableid, colNameIfcType, columndef, null, 0, out columnid);

                
                columndef = new JET_COLUMNDEF
                {
                    coltyp = JET_coltyp.LongBinary,
                    grbit = ColumndefGrbit.ColumnMaybeNull
                };
              
                if(EsentVersion.SupportsWindows7Features)
                    columndef.grbit |= Windows7Grbits.ColumnCompressed;
                
                Api.JetAddColumn(sesid, tableid, colNameEntityData, columndef, null, 0, out columnid);
                columndef = new JET_COLUMNDEF
                {
                    coltyp = JET_coltyp.Bit,
                    grbit = ColumndefGrbit.None
                };
                Api.JetAddColumn(sesid, tableid, colNameIsIndexedClass, columndef, null, 0, out columnid);

                //write an initial header record
                
                string labelIndexDef = string.Format("+{0}\0\0", colNameEntityLabel);
                Api.JetCreateIndex(sesid, tableid, entityTableLabelIndex, CreateIndexGrbit.IndexPrimary, labelIndexDef, labelIndexDef.Length,100);
                Api.JetCloseTable(sesid, tableid);
                transaction.Commit(CommitTransactionGrbit.LazyFlush);
            }
            Api.JetOpenTable(sesid, dbid, ifcEntityTableName, null, 0, OpenTableGrbit.DenyRead, out tableid);

            string typeIndexDef = string.Format("+{0}\0{1}\0{2}\0\0", colNameIfcType, colNameSecondaryKey, colNameEntityLabel);
           
            JET_INDEXCREATE[] indexes = new[]
                {
                    new JET_INDEXCREATE
                    {
                        szIndexName = entityTableTypeLabelIndex,
                        szKey = typeIndexDef,
                        cbKey = typeIndexDef.Length,
                        rgconditionalcolumn = new[]
                        {
                            new JET_CONDITIONALCOLUMN
                            {
                                szColumnName = colNameIsIndexedClass,
                                grbit = ConditionalColumnGrbit.ColumnMustBeNonNull
                            }
                        },
                        cConditionalColumn = 1,
                        ulDensity=100,
                        grbit = CreateIndexGrbit.IndexUnique
                    }
                };

            Api.JetCreateIndex2(sesid, tableid, indexes, indexes.Length);
            Api.JetCloseTable(sesid, tableid);
        }
        
        private void InitColumns()
        {

           // IDictionary<string, JET_COLUMNID> columnids = Api.GetColumnDictionary(_jetSession, _jetCursor);
            _colIdEntityLabel = Api.GetTableColumnid(sesid, table, colNameEntityLabel);
            _colIdSecondaryKey = Api.GetTableColumnid(sesid, table, colNameSecondaryKey);
            _colIdIfcType = Api.GetTableColumnid(sesid, table, colNameIfcType);
            _colIdEntityData = Api.GetTableColumnid(sesid, table, colNameEntityData);
            _colIdIsIndexedClass = Api.GetTableColumnid(sesid, table, colNameIsIndexedClass);
            _colValEntityLabel = new Int32ColumnValue { Columnid = _colIdEntityLabel };
            _colValTypeId = new Int16ColumnValue { Columnid = _colIdIfcType };
            _colValSecondaryKey = new Int32ColumnValue { Columnid = _colIdSecondaryKey };
            _colValData = new BytesColumnValue { Columnid = _colIdEntityData };
            _colValIsIndexedClass = new BoolColumnValue { Columnid = _colIdIsIndexedClass };
            _colValues = new ColumnValue[] { _colValEntityLabel, _colValSecondaryKey, _colValTypeId, _colValData, _colValIsIndexedClass };

        }

        public XbimEntityCursor(Instance instance, string database):this(instance,database,OpenDatabaseGrbit.None)
        {
        }
        /// <summary>
        /// Constructs a table and opens it
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="database"></param>
        public XbimEntityCursor(JET_INSTANCE instance, string database, OpenDatabaseGrbit mode)
            : base(instance, database, mode)
        {
            Api.JetOpenTable(this.sesid, this.dbId, ifcEntityTableName, null, 0, 
                mode == OpenDatabaseGrbit.ReadOnly ? OpenTableGrbit.ReadOnly :
                mode == OpenDatabaseGrbit.Exclusive ? OpenTableGrbit.DenyWrite : OpenTableGrbit.None, out this.table);         
            InitColumns();
        }


        /// <summary>
        /// Sets the values of the fields, no update is performed
        /// </summary>
        /// <param name="primaryKey">The label of the entity</param>
        /// <param name="type">The index of the type of the entity</param>
        /// <param name="indexKey">specify null if no secondary key is required</param>
        /// <param name="data">The property data</param>
        internal void SetColumnValues(int primaryKey, short type, int? indexKey, byte[] data, bool? index)
        {
            _colValEntityLabel.Value = primaryKey;
            _colValTypeId.Value = type;
            _colValSecondaryKey.Value=indexKey;
            _colValData.Value = data;
            _colValIsIndexedClass.Value = index;
        }



        public string PrimaryIndex { get { return entityTableTypeLabelIndex; } }

        public JET_COLUMNID ColIdEntityLabel { get { return _colIdEntityLabel; } }

        public JET_COLUMNID ColIdIfcType { get { return _colIdIfcType; } }

        

        public JET_COLUMNID ColIdEntityData { get { return _colIdEntityData; } }

        internal void WriteHeader(IIfcFileHeader ifcFileHeader)
        {
            MemoryStream ms = new MemoryStream(4096);
            BinaryWriter bw = new BinaryWriter(ms);
            ifcFileHeader.Write(bw);    
            if (Api.TryMoveFirst(sesid, globalsTable)) 
            {
                using (var update = new Update(sesid, globalsTable, JET_prep.Replace))
                {
                    Api.SetColumn(sesid, globalsTable, ifcHeaderColumn, ms.ToArray());
                    update.Save(); 
                }
            }
           
        }

        internal IIfcFileHeader ReadHeader()
        {
            
            if (Api.TryMoveFirst(sesid, globalsTable)) 
            {
                byte[] hd = Api.RetrieveColumn(sesid, globalsTable, ifcHeaderColumn);
                if (hd == null) return null;//there is nothing in at the moment
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
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            toWrite.WriteEntity(bw);
            IfcType ifcType = IfcMetaData.IfcType(toWrite);
            AddEntity(toWrite.EntityLabel, ifcType.TypeId, ifcType.GetIndexedValues(toWrite), ms.ToArray(), ifcType.IndexedClass);
        }

        /// <summary>
        /// Adds an entity, assumes a valid transaction is running
        /// </summary>
        /// <param name="currentLabel">Primary key/label</param>
        /// <param name="typeId">Type identifer</param>
        /// <param name="indexKeys">Search keys to use specifiy null if no indices</param>
        /// <param name="data">property data</param>
        internal void AddEntity(int currentLabel, short typeId, List<int> indexKeys, byte[] data, bool? indexed)
        {
            if (indexed.HasValue && indexed.Value == false) indexed = null;
            using (var update = new Update(sesid, table, JET_prep.Insert))
            {

                if (indexKeys != null && indexKeys.Count > 0)
                {
                    IEnumerable<int> uniqueKeys = indexKeys.Distinct();
                    int i = 1;
                    JET_SETINFO setinfo = new JET_SETINFO();
                    foreach (var item in uniqueKeys)
                    {
                        if (i == 1)
                        {
                            SetColumnValues(currentLabel, typeId, item, data.ToArray(), indexed.Value);
                            Api.SetColumns(sesid, table, _colValues);
                        }
                        else
                        {
                            byte[] bytes = BitConverter.GetBytes(item);
                            setinfo.itagSequence = i + 1;
                            Api.JetSetColumn(sesid, table, _colIdSecondaryKey, bytes, bytes.Length, SetColumnGrbit.None, setinfo);
                        }
                        i++;
                    }
                }
                else
                {
                    SetColumnValues(currentLabel, typeId, null, data.ToArray(), indexed);
                    Api.SetColumns(sesid, table, _colValues);
                }
                update.Save();
                UpdateCount(1);
            }

        }

        /// <summary>
        /// Create a new entity of the specified type, the entity will be blank, all properties with default values
        /// </summary>
        /// <param name="type">Type of entity to create, this must support IPersistIfcEntity</param>
        /// <returns>A handle to the entity</returns>
        internal XbimInstanceHandle AddEntity(Type type)
        {
            System.Diagnostics.Debug.Assert(typeof(IPersistIfcEntity).IsAssignableFrom(type));
            int highest = RetrieveHighestLabel();
            IfcType ifcType = IfcMetaData.IfcType(type);
            XbimInstanceHandle h = new XbimInstanceHandle(highest + 1, ifcType.TypeId);
            AddEntity(h.EntityLabel, h.EntityTypeId, null, null, ifcType.IndexedClass);
            return h;
        }

        /// <summary>
        /// Returns true if the specified entity label is present in the table, assumes the current index has been set to by primary key (SetPrimaryIndex)
        /// </summary>
        /// <param name="key">The entity label to lookup</param>
        /// <returns></returns>
        public bool TrySeekEntityLabel(int key)
        {
           
            Api.MakeKey(sesid, table, key, MakeKeyGrbit.NewKey);
            return Api.TrySeek(this.sesid, this.table, SeekGrbit.SeekEQ);
        }
        /// <summary>
        /// Trys to move to the first entity of the specified type, assumes the current index has been set to order by type (SetOrderByType)
        /// </summary>
        /// <param name="typeId"></param>
        /// <returns></returns>
        public bool TrySeekEntityType(short typeId, out XbimInstanceHandle ih)
        {
            Api.MakeKey(sesid, table, typeId, MakeKeyGrbit.NewKey);
            if(Api.TrySeek(sesid, table, SeekGrbit.SeekGE))
            {
                Api.MakeKey(sesid, table, typeId, MakeKeyGrbit.NewKey | MakeKeyGrbit.FullColumnEndLimit);
                if( Api.TrySetIndexRange(sesid, table, SetIndexRangeGrbit.RangeUpperLimit | SetIndexRangeGrbit.RangeInclusive))
                {
                    ih = new XbimInstanceHandle(Api.RetrieveColumnAsInt32(sesid, table, _colIdEntityLabel, RetrieveColumnGrbit.RetrieveFromIndex), Api.RetrieveColumnAsInt16(sesid, table, _colIdIfcType, RetrieveColumnGrbit.RetrieveFromIndex));
                    return true;
                }
            }
            ih = new XbimInstanceHandle();
            return false;

        }

        /// <summary>
        /// Trys to move to the first entity of the specified type, assumes the current index has been set to order by type (SetOrderByType)
        /// Secondar keys are specific t the type and defined as IfcAttributes in the class declaration
        /// </summary>
        /// <param name="typeId">the type of entity to look up</param>
        /// <param name="lookupKey">Secondary indexes on the search</param>
        /// <returns>Returns an instance handle to the first or an empty handle if not found</returns>
        public  bool TrySeekEntityType(short typeId, out XbimInstanceHandle ih, long lookupKey = -1 )
        {
            if (lookupKey > 0) 
            {
                Api.MakeKey(sesid, table, typeId, MakeKeyGrbit.NewKey);
                Api.MakeKey(sesid, table, lookupKey, MakeKeyGrbit.None);
                if (Api.TrySeek(sesid, table, SeekGrbit.SeekGE))
                {                    
                    Api.MakeKey(sesid, table, typeId, MakeKeyGrbit.NewKey);
                    Api.MakeKey(sesid, table, lookupKey, MakeKeyGrbit.FullColumnEndLimit);
                    if (Api.TrySetIndexRange(sesid, table, SetIndexRangeGrbit.RangeUpperLimit | SetIndexRangeGrbit.RangeInclusive))
                    {
                        ih = new XbimInstanceHandle(Api.RetrieveColumnAsInt32(sesid, table, _colIdEntityLabel, RetrieveColumnGrbit.RetrieveFromIndex), Api.RetrieveColumnAsInt16(sesid, table, _colIdIfcType, RetrieveColumnGrbit.RetrieveFromIndex));
                        return true;
                    }
                }
                ih = new XbimInstanceHandle();
                return false;
            }
            else
                return TrySeekEntityType(typeId, out ih);
        }
        /// <summary>
        /// Sets the order to be by entity type and then label 
        /// </summary>
        internal void SetOrderByType()
        {
            Api.JetSetCurrentIndex(this.sesid, this.table, entityTableTypeLabelIndex);
        }

        /// <summary>
        /// Sets the order to be by entity label and type
        /// </summary>
        internal void SetOrderByLabel()
        {
            Api.JetSetCurrentIndex(this.sesid, this.table, entityTableLabelIndex);
        }
        

        
        /// <summary>
        /// returns the instance handle for the object at the current cursor position. Assumes the index has been set to the correct position
        /// and the current index is SetOrderByType
        /// </summary>
        /// <returns></returns>
        internal XbimInstanceHandle GetInstanceHandle()
        {
            int? label = Api.RetrieveColumnAsInt32(sesid, table, _colIdEntityLabel, RetrieveColumnGrbit.RetrieveFromIndex);
            short? typeId = Api.RetrieveColumnAsInt16(sesid, table, _colIdIfcType, RetrieveColumnGrbit.RetrieveFromIndex);
            return new XbimInstanceHandle(label.Value, typeId.Value);
            
        }
        /// <summary>
        /// Gets the property values of the entity from the current record
        /// </summary>
        /// <returns>byte array of the property data in binary ifc format</returns>
        internal byte[] GetProperties()
        {
            return Api.RetrieveColumn(sesid, table, _colIdEntityData);
           
        }

        
        /// <summary>
        /// Retrieve the count of entity items in the database from the globals table.
        /// </summary>
        /// <returns>The number of items in the database.</returns>
        override internal int RetrieveCount()
        {
            return (int)Api.RetrieveColumnAsInt32(this.sesid, this.globalsTable, this.entityCountColumn);
        }

        /// <summary>
        /// Update the count of entity in the globals table. This is done with EscrowUpdate
        /// so that there won't be any write conflicts.
        /// </summary>
        /// <param name="delta">The delta to apply to the count.</param>
        override protected void UpdateCount(int delta)
        {
            Api.EscrowUpdate(this.sesid, this.globalsTable, this.entityCountColumn, delta);
        }

        internal int RetrieveHighestLabel()
        {
            SetOrderByLabel();
            if (TryMoveLast())
            {
                int? val = Api.RetrieveColumnAsInt32(sesid, table, _colIdEntityLabel, RetrieveColumnGrbit.RetrieveFromIndex);
                if (val.HasValue) return val.Value;
            }
            return 0;
        }


        /// <summary>
        /// Returns the id of the current ifc type
        /// </summary>
        /// <returns></returns>
        public short GetIfcType()
        { 
            short? typeId = Api.RetrieveColumnAsInt16(sesid, table, _colIdIfcType);
            if (typeId.HasValue) 
                return typeId.Value;
            else
                return 0;
        }

        /// <summary>
        /// Returns the current enity label
        /// </summary>
        /// <returns></returns>
        public int GetLabel()
        {
            int? label = Api.RetrieveColumnAsInt32(sesid, table, _colIdEntityLabel);
            if (label.HasValue)
                return label.Value;
            else
                return 0;
        }



        public bool TryMoveNextEntityType(out XbimInstanceHandle ih)
        {
            if (TryMoveNext())
            {
                ih = new XbimInstanceHandle(Api.RetrieveColumnAsInt32(sesid, table, _colIdEntityLabel, RetrieveColumnGrbit.RetrieveFromIndex), Api.RetrieveColumnAsInt16(sesid, table, _colIdIfcType, RetrieveColumnGrbit.RetrieveFromIndex));
                return true;
            }
            else
            {
                ih = new XbimInstanceHandle();
                return false;
            }
        }

        internal bool TryMoveFirstLabel(out int label)
        {
            if (TryMoveFirst())
            {
                label = Api.RetrieveColumnAsInt32(sesid, table, _colIdEntityLabel, RetrieveColumnGrbit.RetrieveFromIndex).Value;
                return true;
            }
            else
            {
                label = 0;
                return false;
            }
        }

        internal bool TryMoveNextLabel(out int label)
        {
            if (TryMoveNext())
            {
                label = Api.RetrieveColumnAsInt32(sesid, table, _colIdEntityLabel, RetrieveColumnGrbit.RetrieveFromIndex).Value;
                return true;
            }
            else
            {
                label = 0;
                return false;
            }
        }

        public int Current
        {
            get { return current; }
        }

        object System.Collections.IEnumerator.Current
        {
            get { return current; }
        }

        public bool MoveNext()
        {
            int label;
            if (TryMoveNextLabel(out label))
            {
                current = label;
                return true;
            }
            else
                return false;
        }

        public void Reset()
        {
            this.SetOrderByLabel();
            Api.MoveBeforeFirst(sesid, table);
            current = 0;
        }

      
    }
}
