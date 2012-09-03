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
using Xbim.Common.Logging;
using Xbim.Common.Exceptions;
#endregion

namespace Xbim.XbimExtensions.Parser
{
    public class XbimIndexEntry
    {
        public XbimIndexEntry(long entityLabel, long offset, Type type)
        {
            _entityLabel = entityLabel;
            _offset = offset;
            _type = type;
            _entityRef = null;
        }

        /// <summary>
        ///   Constructs an entry for a completely new instance of a type, offset == 0, Type == typeof entity
        /// </summary>
        public XbimIndexEntry(long entityLabel, IPersistIfcEntity entity)
        {
            _entityLabel = entityLabel;
            _offset = 0;
            _type = entity.GetType();
            _entityRef = null;
            Entity = entity;
        }

        public XbimIndexEntry(XbimIndexEntry xbimIndexEntry)
        {
            _entityLabel = xbimIndexEntry._entityLabel;
            _offset = xbimIndexEntry._offset;
            _type = xbimIndexEntry._type;
            _entityRef = null;
            Entity = xbimIndexEntry.Entity;
        }

        private long _entityLabel;

        public long EntityLabel
        {
            get { return _entityLabel; }
            set { _entityLabel = value; }
        }
        private long _offset;

        public long Offset
        {
            get { return _offset; }
            set { _offset = value; }
        }
        private Type _type;

        public Type Type
        {
            get { return _type; }
            set { _type = value; }
        }

        private IPersistIfcEntity _entityRef;

        /// <summary>
        ///   Drops an object from memory cache
        /// </summary>
        public void Drop()
        {
            _entityRef = null;
        }

        public IPersistIfcEntity Entity
        {
            get
            {
                return _entityRef;

                //if (_entityRef != null)
                //    return (IPersistIfcEntity)_entityRef.Target;
                //else if (_offset == 0 && _type != null)
                ////we have a newly created entity but it has been released by garbage collector
                //{
                //    IPersistIfcEntity newEntity = (IPersistIfcEntity)Activator.CreateInstance(_type);
                //    _entityRef = new WeakReference(newEntity);
                //    return newEntity;
                //}
                //else
                //    return null;
            }
            set
            {
                _entityRef=value;
                //if (_entityRef != null)
                //    _entityRef.Target = value;
                //else
                //    _entityRef = new WeakReference(value);
            }
        }
    }

    public class XbimIndex : KeyedCollection<long, XbimIndexEntry>
    {
        private long _highestLabel;
        private readonly ILogger Logger = LoggerFactory.GetLogger();

        public long NextLabel
        {
            get { return _highestLabel + 1; }
        }

        public long HighestLabel
        {
            get { return _highestLabel; }
        }

        /// <summary>
        ///   Releases all objects that are cached in the index
        /// </summary>
        public void DropAll()
        {
            foreach (XbimIndexEntry item in this)
            {
                item.Drop();
            }
        }

        public XbimIndexEntry AddNew<T>(out IPersistIfcEntity newEntity)
        {
            newEntity = (IPersistIfcEntity)Activator.CreateInstance<T>();
            XbimIndexEntry entry = new XbimIndexEntry(NextLabel, newEntity);
            IList<XbimIndexEntry> entryList = this as IList<XbimIndexEntry>;
            entryList.Add_Reversible(entry);
            return entry;
        }

        protected override long GetKeyForItem(XbimIndexEntry item)
        {
            return item.EntityLabel;
        }

        protected override void InsertItem(int index, XbimIndexEntry item)
        {
            base.InsertItem(index, item);
            Transaction txn = Transaction.Current;
            if (txn != null)
                Transaction.AddPropertyChange<long>(h => _highestLabel = h, _highestLabel, Math.Max(_highestLabel, item.EntityLabel));
            _highestLabel = Math.Max(_highestLabel, item.EntityLabel);
        }

        protected override void SetItem(int index, XbimIndexEntry item)
        {
            base.SetItem(index, item);
            Transaction txn = Transaction.Current;
            if (txn != null)
                Transaction.AddPropertyChange<long>(h => _highestLabel = h, _highestLabel, Math.Max(_highestLabel, item.EntityLabel));
            _highestLabel = Math.Max(_highestLabel, item.EntityLabel);

        }

        protected override void ClearItems()
        {
            base.ClearItems();
            Transaction txn = Transaction.Current;
            if (txn != null)
                Transaction.AddPropertyChange<long>(h => _highestLabel = h, _highestLabel, 0);
            _highestLabel = 0;
        }


        public XbimIndex(long highestLabel)
        {
            _highestLabel = highestLabel;
        }

        public XbimIndex()
        {
            _highestLabel = 0;
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write((long)Count);
            writer.Write(HighestLabel);

            HashSet<Type> types = new HashSet<Type>();
            foreach (var item in this)
            {
                types.Add(item.Type);
            }
            Dictionary<Type, short> classNames = new Dictionary<Type, short>();
            writer.Write(types.Count);
            short i = 0;
            foreach (var eType in types)
            {
                classNames.Add(eType, i);
                writer.Write(eType.Name.ToUpper());
                writer.Write(i);
                i++;
            }
            // writer.Write(this.Count);
            foreach (var item in this)
            {
                writer.Write(item.EntityLabel);
                writer.Write(item.Offset);
                writer.Write(classNames[item.Type]);
            }
        }

        public static XbimIndex Read(Stream dataStream)
        {

            BinaryReader reader = new BinaryReader(dataStream);
            long count = reader.ReadInt64();
            XbimIndex index = new XbimIndex(reader.ReadInt64());


            HashSet<Type> types = new HashSet<Type>();

            int typeCount = reader.ReadInt32();
            Dictionary<short, string> classNames = new Dictionary<short, string>(typeCount);
            for (int i = 0; i < typeCount; i++)
            {
                string typeName = reader.ReadString();
                short typeId = reader.ReadInt16();
                classNames.Add(typeId, typeName);
            }
            //  int instanceCount = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                long label = reader.ReadInt64();
                long offset = reader.ReadInt64();
                short id = reader.ReadInt16();
                index.Add(new XbimIndexEntry(label, offset, IfcInstances.IfcTypeLookup[classNames[id]].Type));
            }
            return index;
        }

        public XbimIndexEntry AddNew(Type type, out IPersistIfcEntity newEntity, long label)
        {
            newEntity = (IPersistIfcEntity)Activator.CreateInstance(type);
            XbimIndexEntry entry = new XbimIndexEntry(label, newEntity);
            IList<XbimIndexEntry> entryList = this as IList<XbimIndexEntry>;
            entryList.Add_Reversible(entry);
            _highestLabel = Math.Max(label, _highestLabel);
            return entry;
        }

        public XbimIndexEntry AddNew(Type type, out IPersistIfcEntity newEntity)
        {
            newEntity = (IPersistIfcEntity)Activator.CreateInstance(type);
            XbimIndexEntry entry = new XbimIndexEntry(NextLabel, newEntity);
            IList<XbimIndexEntry> entryList = this as IList<XbimIndexEntry>;
            entryList.Add_Reversible(entry);
            return entry;
        }
    }


    public class XbimP21Index
    {
        private readonly XbimIndex _entityOffsets = new XbimIndex();
        private readonly Dictionary<Type, List<long>> _entityTypes = new Dictionary<Type, List<long>>();
        private readonly ILogger Logger = LoggerFactory.GetLogger();

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
            catch (KeyNotFoundException)
            {
                string message = string.Format("Unsupported IFC Type found. #{1} '{0}' is not a recognised type", entityType, entityLabel);

                Logger.Error(message);
                throw new XbimParserException(message);
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
        private readonly XbimP21Index _index = new XbimP21Index();
        private BinaryWriter _binaryWriter;
        private readonly Stream _indexStrm;
        private long _currentLabel;
        private string _currentType;
        private long _startOffset = -1;

        private Part21Entity _currentInstance;
        private readonly Stack<Part21Entity> _processStack = new Stack<Part21Entity>();
        private PropertyValue _propertyValue;
        private int _listNestLevel = -1;
        private readonly IfcFileHeader _header = new IfcFileHeader();

        public P21toIndexParser(Stream inputP21, Stream indexStrm)
            : base(inputP21)
        {
            if (inputP21.CanSeek)
                _streamSize = inputP21.Length;
            _indexStrm = indexStrm;
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
            if (_binaryWriter != null) _binaryWriter.Close();
            _binaryWriter = new BinaryWriter(_indexStrm);
            _binaryWriter.Write(0L); //data
            int reservedSize = 32;
            _binaryWriter.Write(reservedSize);
            _binaryWriter.Write(new byte[reservedSize]);
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
            _header.Write(_binaryWriter);
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
            if (InHeader)
            {
                Part21Entity p21 = _processStack.Peek();
                if (p21.CurrentParamIndex == -1)
                    p21.CurrentParamIndex++; //first time in take the forst argument

                _listNestLevel++;
            }
            else
                _binaryWriter.Write((byte)P21ParseAction.BeginList);
        }

        internal override void EndList()
        {
            if (InHeader)
            {
                _listNestLevel--;
                Part21Entity p21 = _processStack.Peek();
                p21.CurrentParamIndex++;
            }
            else
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
            _currentLabel = Convert.ToInt64(entityLabel.TrimStart('#'));
            _startOffset = _indexStrm.Position;
            _binaryWriter.Write((int)0);
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
                _currentType = entityTypeName;
        }

        internal override void EndEntity()
        {
            _index.Add(_currentLabel, _currentType, _startOffset);
            _currentLabel = -1;
            _currentType = null;
            _binaryWriter.Write((byte)P21ParseAction.EndEntity);
            long endpos = _indexStrm.Position;
            _indexStrm.Seek(_startOffset, SeekOrigin.Begin);
            _binaryWriter.Write((int)(endpos - _startOffset - sizeof(int)));
            _indexStrm.Seek(endpos, SeekOrigin.Begin);
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
                if (_listNestLevel == 0) _currentInstance.CurrentParamIndex++;
            }
            else
            {
                _binaryWriter.Write((byte)P21ParseAction.SetIntegerValue);
                _binaryWriter.Write(Convert.ToInt64(value));
            }
        }

        internal override void SetHexValue(string value)
        {
            if (InHeader)
            {
                _propertyValue.Init(value, IfcParserType.HexaDecimal);
                if (_currentInstance.Entity != null)
                    _currentInstance.ParameterSetter(_currentInstance.CurrentParamIndex, _propertyValue);
                if (_listNestLevel == 0) _currentInstance.CurrentParamIndex++;
            }
            else
            {
                _binaryWriter.Write((byte)P21ParseAction.SetHexValue);
                _binaryWriter.Write(Convert.ToInt64(value, 16));
            }
        }

        internal override void SetFloatValue(string value)
        {
            if (InHeader)
            {
                _propertyValue.Init(value, IfcParserType.Real);
                if (_currentInstance.Entity != null)
                    _currentInstance.ParameterSetter(_currentInstance.CurrentParamIndex, _propertyValue);
                if (_listNestLevel == 0) _currentInstance.CurrentParamIndex++;
            }
            else
            {
                _binaryWriter.Write((byte)P21ParseAction.SetFloatValue);
                _binaryWriter.Write(Convert.ToDouble(value));
            }
        }

        internal override void SetStringValue(string value)
        {
            if (InHeader)
            {
                _propertyValue.Init(value, IfcParserType.String);
                if (_currentInstance.Entity != null)
                    _currentInstance.ParameterSetter(_currentInstance.CurrentParamIndex, _propertyValue);
                if (_listNestLevel == 0)
                    _currentInstance.CurrentParamIndex++;
            }
            else
            {
                _binaryWriter.Write((byte)P21ParseAction.SetStringValue);
                string res = value.Trim('\'');
                res = PropertyValue.SpecialCharRegEx.Replace(res, PropertyValue.SpecialCharEvaluator);
                res = res.Replace("\'\'", "\'");
                _binaryWriter.Write(res);
                
            }
        }

        internal override void SetEnumValue(string value)
        {
            if (InHeader)
            {
                _propertyValue.Init(value, IfcParserType.Enum);
                if (_currentInstance.Entity != null)
                    _currentInstance.ParameterSetter(_currentInstance.CurrentParamIndex, _propertyValue);
                if (_listNestLevel == 0) _currentInstance.CurrentParamIndex++;
            }
            else
            {
                _binaryWriter.Write((byte)P21ParseAction.SetEnumValue);
                _binaryWriter.Write(value.Trim('.'));
            }
        }

        internal override void SetBooleanValue(string value)
        {
            if (InHeader)
            {
                _propertyValue.Init(value, IfcParserType.Boolean);
                if (_currentInstance.Entity != null)
                    _currentInstance.ParameterSetter(_currentInstance.CurrentParamIndex, _propertyValue);
                if (_listNestLevel == 0) _currentInstance.CurrentParamIndex++;
            }
            else
            {
                _binaryWriter.Write((byte)P21ParseAction.SetBooleanValue);
                _binaryWriter.Write(value == ".T.");
            }
        }

        internal override void SetNonDefinedValue()
        {
            if (InHeader && _listNestLevel == 0)
                _currentInstance.CurrentParamIndex++;
            else
                _binaryWriter.Write((byte)P21ParseAction.SetNonDefinedValue);
        }

        internal override void SetOverrideValue()
        {
            if (InHeader && _listNestLevel == 0)
                _currentInstance.CurrentParamIndex++;
            else
                _binaryWriter.Write((byte)P21ParseAction.SetOverrideValue);
        }

        internal override void SetObjectValue(string value)
        {
            if (InHeader && _listNestLevel == 0)
                _currentInstance.CurrentParamIndex++;
            else
            {
                long val = Convert.ToInt64(value.TrimStart('#'));
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
        }

        internal override void EndNestedType(string value)
        {
            _binaryWriter.Write((byte)P21ParseAction.EndNestedType);
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