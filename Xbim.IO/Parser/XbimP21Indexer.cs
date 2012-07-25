#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    P21toIndexParser.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Xbim.XbimExtensions.Transactions.Extensions;
using Xbim.XbimExtensions.Transactions;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;
using Microsoft.Isam.Esent.Interop;
#endregion

namespace Xbim.IO.Parser
{
   
   
    public class XbimP21Indexer
    {
        private readonly XbimIndex _entityOffsets = new XbimIndex();
        private readonly Dictionary<Type, List<long>> _entityTypes = new Dictionary<Type, List<long>>();

        public XbimIndex EntityOffsets
        {
            get { return _entityOffsets; }
        }

        public Dictionary<Type, List<long>> EntityTypes
        {
            get { return _entityTypes; }
        }

        public void Add(long entityLabel, string entityType, long fileOffset)
        {
            try
            {
                Type t = IfcInstances.IfcTypeLookup[entityType].Type;
                _entityOffsets.Add(new XbimIndexEntry(entityLabel, fileOffset, t));
                List<long> offsets;
                if (!_entityTypes.TryGetValue(t, out offsets))
                {
                    offsets = new List<long>();
                    _entityTypes.Add(t, offsets);
                }
                offsets.Add(fileOffset);
            }
            catch (Exception e)
            {
                throw new Exception("Unsupported Ifc Type found " + entityType + "in entity #" + entityLabel, e);
            }
        }


        internal void Write(BinaryWriter binaryWriter)
        {
            long start = binaryWriter.BaseStream.Position;
            binaryWriter.Write((long)_entityOffsets.Count);
            binaryWriter.Write(_entityOffsets.HighestLabel);
            Dictionary<Type, short> classNames = new Dictionary<Type, short>(_entityTypes.Count);
            binaryWriter.Write(_entityTypes.Count);
            short i = 0;
            foreach (KeyValuePair<Type, List<long>> eType in _entityTypes)
            {
                classNames.Add(eType.Key, i);
                binaryWriter.Write(eType.Key.Name.ToUpper());
                binaryWriter.Write(i);
                i++;
            }

            foreach (XbimIndexEntry item in _entityOffsets)
            {
                binaryWriter.Write(item.EntityLabel);
                binaryWriter.Write(item.Offset);
                binaryWriter.Write(classNames[item.Type]);
            }

            binaryWriter.Write(0L); //no changes following


            binaryWriter.Seek(0, SeekOrigin.Begin);
            binaryWriter.Write(start);
        }
    }

    public enum P21ParseAction
    {
        BeginList, //0
        EndList, //1
        BeginComplex, //2
        EndComplex, //3
        SetIntegerValue, //4
        SetHexValue, //5
        SetFloatValue, //6
        SetStringValue, //7
        SetEnumValue, //8
        SetBooleanValue, //9
        SetNonDefinedValue, //0x0A
        SetOverrideValue, //x0B
        BeginNestedType, //0x0C
        EndNestedType, //0x0D
        EndEntity, //0x0E
        NewEntity, //0x0F
        SetObjectValueUInt16,
        SetObjectValueUInt32,
        SetObjectValueInt64
    }

    public class P21toIndexParser : P21Parser, IDisposable
    {
        public event ReportProgressDelegate ProgressStatus;
        private int _percentageParsed;
        private long _streamSize = -1;
        private readonly XbimP21Indexer _index = new XbimP21Indexer();
        private BinaryWriter _binaryWriter;
        
        private long _currentLabel;
        private string _currentType;
        private int _primaryKeyIdx = -1;

        private Part21Entity _currentInstance;
        private readonly Stack<Part21Entity> _processStack = new Stack<Part21Entity>();
        private PropertyValue _propertyValue;
        private int _listNestLevel = -1;
        private readonly IfcFileHeader _header = new IfcFileHeader();

        public IfcFileHeader Header
        {
            get { return _header; }
        } 

   
        private Microsoft.Isam.Esent.Interop.Session session;
        private Microsoft.Isam.Esent.Interop.Table table;
        private JET_COLUMNID columnidEntityLabel;
        private JET_COLUMNID columnidSecondaryKey;
        private JET_COLUMNID columnidIfcType;
        private JET_COLUMNID columnidEntityData;
        Int64ColumnValue _colEntityLabel;
        Int64ColumnValue _colSecondaryKey;
        Int16ColumnValue _colTypeId;
        BytesColumnValue _colData;
        ColumnValue[] _colValues;
        const int _transactionBatchSize = 100;
        private int _entityCount = 0;
        private long _primaryKeyValue = -1;
        public P21toIndexParser(Stream inputP21, Session session, Table table)
            : base(inputP21)
        {
            this.session = session;
            this.table = table;
            IDictionary<string, JET_COLUMNID> columnids = Api.GetColumnDictionary(session, table);
            columnidEntityLabel = columnids[XbimModel.colNameEntityLabel];
            columnidSecondaryKey = columnids[XbimModel.colNameSecondaryKey];
            columnidIfcType = columnids[XbimModel.colNameIfcType];
            columnidEntityData = columnids[XbimModel.colNameEntityData];
            _colEntityLabel = new Int64ColumnValue { Columnid = columnidEntityLabel};
            _colSecondaryKey = new Int64ColumnValue { Columnid = columnidSecondaryKey };
            _colTypeId = new Int16ColumnValue { Columnid = columnidIfcType };
            _colData = new BytesColumnValue { Columnid = columnidEntityData };
            _colValues = new ColumnValue[] { _colEntityLabel, _colSecondaryKey, _colTypeId, _colData };

            _entityCount = 0;
            if (inputP21.CanSeek)
                _streamSize = inputP21.Length;
        }

        internal override void SetErrorMessage()
        {
            Debug.WriteLine("TODO");
        }

        internal override void CharacterError()
        {
            Debug.WriteLine("TODO");
        }

        internal override void BeginParse()
        {
            _binaryWriter = new BinaryWriter(new MemoryStream(0x7FFF));
        }

        internal override void EndParse()
        {
            _index.Write(_binaryWriter);
            Dispose();
        }

        internal override void BeginHeader()
        {
            // Debug.WriteLine("TODO");
        }

        internal override void EndHeader()
        {
           // _header.Write(_binaryWriter);
        }

        internal override void BeginScope()
        {
            // Debug.WriteLine("TODO");
        }

        internal override void EndScope()
        {
            // Debug.WriteLine("TODO");
        }

        internal override void EndSec()
        {
            // Debug.WriteLine("TODO");
        }

        internal override void BeginList()
        {
            Part21Entity p21 = _processStack.Peek();
            if (p21.CurrentParamIndex == -1)
                p21.CurrentParamIndex++; //first time in take the forst argument
            _listNestLevel++;
            if (!InHeader)
                _binaryWriter.Write((byte)P21ParseAction.BeginList);

        }

        internal override void EndList()
        {
            _listNestLevel--;
            Part21Entity p21 = _processStack.Peek();
            p21.CurrentParamIndex++;
            if (!InHeader)
                _binaryWriter.Write((byte)P21ParseAction.EndList);
        }

        internal override void BeginComplex()
        {
            _binaryWriter.Write((byte)P21ParseAction.BeginComplex);
        }

        internal override void EndComplex()
        {
            _binaryWriter.Write((byte)P21ParseAction.EndComplex);
        }

        internal override void NewEntity(string entityLabel)
        {
            _currentInstance = new Part21Entity(entityLabel);
            _processStack.Push(_currentInstance);
            _entityCount++;
            _primaryKeyValue = -1;
            _currentLabel = Convert.ToInt64(entityLabel.TrimStart('#'));
            MemoryStream data = _binaryWriter.BaseStream as MemoryStream;
            data.SetLength(0);

            _binaryWriter.Write((byte)P21ParseAction.NewEntity);
            if (_streamSize != -1 && ProgressStatus != null)
            {
                Scanner sc = (Scanner)this.Scanner;
                double pos = sc.Buffer.Pos;
                int newPercentage = Convert.ToInt32(pos / _streamSize * 100.0);
                if (newPercentage > _percentageParsed)
                {
                    _percentageParsed = newPercentage;
                    ProgressStatus(_percentageParsed, "Parsing");
                }
            }
        }

        internal override void SetType(string entityTypeName)
        {
            if (InHeader)
            {
                IPersistIfc currentHeaderEntity;
                switch (entityTypeName)
                {
                    case "FILE_DESCRIPTION":
                        currentHeaderEntity = _header.FileDescription;
                        break;
                    case "FILE_NAME":
                        currentHeaderEntity = _header.FileName;
                        break;
                    case "FILE_SCHEMA":
                        currentHeaderEntity = _header.FileSchema;
                        break;
                    default:
                        throw new ArgumentException(string.Format("Invalid Header entity type {0}", entityTypeName));
                }
                _currentInstance = new Part21Entity(currentHeaderEntity);
                _processStack.Push(_currentInstance);
            }
            else
            {

                _currentType = entityTypeName;
                IfcType ifcType = IfcInstances.IfcTypeLookup[_currentType];
                _primaryKeyIdx = ifcType.PrimaryKeyIndex;
            }
        }

        internal override void EndEntity()
        {
            Part21Entity p21 = _processStack.Pop();
            Debug.Assert(_processStack.Count == 0);
            _currentInstance = null;
            if (_currentType != null)
            {
                _binaryWriter.Write((byte)P21ParseAction.EndEntity);
                using (var update = new Update(session, table, JET_prep.Insert))
                {
                    MemoryStream data = _binaryWriter.BaseStream as MemoryStream;

                    _colEntityLabel.Value = _currentLabel;
                    IfcType ifcType = IfcInstances.IfcTypeLookup[_currentType];
                    _colTypeId.Value = ifcType.TypeId;
                    if (_primaryKeyValue > -1) 
                        _colSecondaryKey.Value = _primaryKeyValue;
                    _colData.Value = data.ToArray();
                    Api.SetColumns(session, table, _colValues);
                    update.Save();
                    if (_entityCount % _transactionBatchSize == (_transactionBatchSize - 1))
                    {
                        Api.JetCommitTransaction(session, CommitTransactionGrbit.LazyFlush);
                        Api.JetBeginTransaction(session);
                    }
                }
            }
           
        }

        internal override void EndHeaderEntity()
        {
            _processStack.Pop();
            _currentInstance = null;
        }

        internal override void SetIntegerValue(string value)
        {
            if (InHeader)
            {
                _propertyValue.Init(value, IfcParserType.Integer);
                if (_currentInstance.Entity != null)
                    _currentInstance.ParameterSetter(_currentInstance.CurrentParamIndex, _propertyValue);
               
            }
            else
            {
                _binaryWriter.Write((byte)P21ParseAction.SetIntegerValue);
                _binaryWriter.Write(Convert.ToInt64(value));
            }
            if (_listNestLevel == 0) _currentInstance.CurrentParamIndex++;
        }

        internal override void SetHexValue(string value)
        {
            if (InHeader)
            {
                _propertyValue.Init(value, IfcParserType.HexaDecimal);
                if (_currentInstance.Entity != null)
                    _currentInstance.ParameterSetter(_currentInstance.CurrentParamIndex, _propertyValue);
                
            }
            else
            {
                _binaryWriter.Write((byte)P21ParseAction.SetHexValue);
                _binaryWriter.Write(Convert.ToInt64(value, 16));
                
            }
            if (_listNestLevel == 0) _currentInstance.CurrentParamIndex++;
        }

        internal override void SetFloatValue(string value)
        {
            if (InHeader)
            {
                _propertyValue.Init(value, IfcParserType.Real);
                if (_currentInstance.Entity != null)
                    _currentInstance.ParameterSetter(_currentInstance.CurrentParamIndex, _propertyValue);
               
            }
            else
            {
                _binaryWriter.Write((byte)P21ParseAction.SetFloatValue);
                _binaryWriter.Write(Convert.ToDouble(value));
            }
            if (_listNestLevel == 0) _currentInstance.CurrentParamIndex++;
        }

        internal override void SetStringValue(string value)
        {
            if (InHeader)
            {
                _propertyValue.Init(value, IfcParserType.String);
                if (_currentInstance.Entity != null)
                    _currentInstance.ParameterSetter(_currentInstance.CurrentParamIndex, _propertyValue);
                
            }
            else
            {
                _binaryWriter.Write((byte)P21ParseAction.SetStringValue);
                string res = value.Trim('\'');
                res = PropertyValue.SpecialCharRegEx.Replace(res, PropertyValue.SpecialCharEvaluator);
                res = res.Replace("\'\'", "\'");
                _binaryWriter.Write(res);
                
            }
            if (_listNestLevel == 0)
                _currentInstance.CurrentParamIndex++;
        }

        internal override void SetEnumValue(string value)
        {
            if (InHeader)
            {
                _propertyValue.Init(value, IfcParserType.Enum);
                if (_currentInstance.Entity != null)
                    _currentInstance.ParameterSetter(_currentInstance.CurrentParamIndex, _propertyValue);
                
            }
            else
            {
                _binaryWriter.Write((byte)P21ParseAction.SetEnumValue);
                _binaryWriter.Write(value.Trim('.'));
            }
            if (_listNestLevel == 0) _currentInstance.CurrentParamIndex++;
        }

        internal override void SetBooleanValue(string value)
        {
            if (InHeader)
            {
                _propertyValue.Init(value, IfcParserType.Boolean);
                if (_currentInstance.Entity != null)
                    _currentInstance.ParameterSetter(_currentInstance.CurrentParamIndex, _propertyValue);
            }
            else
            {
                _binaryWriter.Write((byte)P21ParseAction.SetBooleanValue);
                _binaryWriter.Write(value == ".T.");
            }
            if (_listNestLevel == 0) _currentInstance.CurrentParamIndex++;
        }

        internal override void SetNonDefinedValue()
        {
            if (_listNestLevel == 0) _currentInstance.CurrentParamIndex++;
            _binaryWriter.Write((byte)P21ParseAction.SetNonDefinedValue);
        }

        internal override void SetOverrideValue()
        {
            if (_listNestLevel == 0) _currentInstance.CurrentParamIndex++;
            _binaryWriter.Write((byte)P21ParseAction.SetOverrideValue);
        }

        internal override void SetObjectValue(string value)
        {
            long val = Convert.ToInt64(value.TrimStart('#'));

            if (_currentInstance.CurrentParamIndex == _primaryKeyIdx)
                _primaryKeyValue = val;

            if (_listNestLevel == 0) _currentInstance.CurrentParamIndex++;
           
            if (val <= UInt16.MaxValue)
            {
                _binaryWriter.Write((byte)P21ParseAction.SetObjectValueUInt16);
                _binaryWriter.Write(Convert.ToUInt16(val));
            }
            else if (val <= UInt32.MaxValue)
            {
                _binaryWriter.Write((byte)P21ParseAction.SetObjectValueUInt32);
                _binaryWriter.Write(Convert.ToUInt32(val));
            }
            else if (val <= Int64.MaxValue)
            {
                _binaryWriter.Write((byte)P21ParseAction.SetObjectValueInt64);
                _binaryWriter.Write(val);
            }
            else
                throw new Exception("Entity Label exceeds maximim value for a long number");


        }

        internal override void EndNestedType(string value)
        {
            _binaryWriter.Write((byte)P21ParseAction.EndNestedType);
            if (_listNestLevel == 0) _currentInstance.CurrentParamIndex++;
        }

        internal override void BeginNestedType(string value)
        {
            _binaryWriter.Write((byte)P21ParseAction.BeginNestedType);
            _binaryWriter.Write(value);
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (_binaryWriter != null) _binaryWriter.Close();
            _binaryWriter = null;
        }

        #endregion
    }
}