#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.IO
// Filename:    XbimModelServer.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#if SupportActivation

#region Directives

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Xml;
using Xbim.Ifc.ActorResource;
using Xbim.Ifc.DateTimeResource;
using Xbim.Ifc.GeometryResource;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.MaterialResource;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.SelectTypes;
using Xbim.Ifc.SharedBldgElements;
using Xbim.Ifc.UtilityResource;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.DataProviders;
using Xbim.XbimExtensions.Transactions;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using Xbim.XbimExtensions.Interfaces;
using Xbim.IO.Parser;

#endregion

namespace Xbim.IO
{
    public abstract class XbimModelServer : IModel, IDisposable
    {
        private class ParserState
        {
            private class IndexPropertyValue : IPropertyValue
            {
                private bool _bool;
                private string _string;
                private long _long;
                private double _double;
                private IPersistIfc _object;
                private IfcParserType _parserType;

                public IfcParserType Type
                {
                    get { return _parserType; }
                }

                #region IPropertyValue Members

                public bool BooleanVal
                {
                    get
                    {
                        if (_parserType == IfcParserType.Boolean) return _bool;
                        throw new Exception(string.Format("Wrong parameter type, found {0}, expected {1}",
                                                          _parserType.ToString(), "Boolean"));
                    }
                }

                public string EnumVal
                {
                    get
                    {
                        if (_parserType == IfcParserType.Enum) return _string;
                        throw new Exception(string.Format("Wrong parameter type, found {0}, expected {1}",
                                                          _parserType.ToString(), "Enum"));
                    }
                }

                public object EntityVal
                {
                    get
                    {
                        if (_parserType == IfcParserType.Entity) return _object;
                        throw new Exception(string.Format("Wrong parameter type, found {0}, expected {1}",
                                                          _parserType.ToString(), "Entity"));
                    }
                }

                public long HexadecimalVal
                {
                    get
                    {
                        if (_parserType == IfcParserType.HexaDecimal) return _long;
                        throw new Exception(string.Format("Wrong parameter type, found {0}, expected {1}",
                                                          _parserType.ToString(), "HexaDecimal"));
                    }
                }

                public long IntegerVal
                {
                    get
                    {
                        if (_parserType == IfcParserType.Integer) return _long;
                        throw new Exception(string.Format("Wrong parameter type, found {0}, expected {1}",
                                                          _parserType.ToString(), "Integer"));
                    }
                }

                public double NumberVal
                {
                    get
                    {
                        if (_parserType == IfcParserType.Integer) return Convert.ToDouble(_long);
                        if (_parserType == IfcParserType.Real || _parserType == IfcParserType.HexaDecimal)
                            return _double;
                        throw new Exception(string.Format("Wrong parameter type, found {0}, expected {1}",
                                                          _parserType.ToString(), "Number"));
                    }
                }

                public double RealVal
                {
                    get
                    {
                        if (_parserType == IfcParserType.Real) return _double;
                        throw new Exception(string.Format("Wrong parameter type, found {0}, expected {1}",
                                                          _parserType.ToString(), "Real"));
                    }
                }

                public string StringVal
                {
                    get
                    {
                        if (_parserType == IfcParserType.String) return _string;
                        throw new Exception(string.Format("Wrong parameter type, found {0}, expected {1}",
                                                          _parserType.ToString(), "String"));
                    }
                }

                #endregion

                internal void Init(IPersistIfc iPersistIfc)
                {
                    _object = iPersistIfc;
                    _parserType = IfcParserType.Entity;
                }

                internal void Init(long value, IfcParserType ifcParserType)
                {
                    _long = value;
                    _parserType = ifcParserType;
                }

                internal void Init(string value, IfcParserType ifcParserType)
                {
                    _string = value;
                    _parserType = ifcParserType;
                }

                internal void Init(double value, IfcParserType ifcParserType)
                {
                    _double = value;
                    _parserType = ifcParserType;
                }

                internal void Init(bool value, IfcParserType ifcParserType)
                {
                    _bool = value;
                    _parserType = ifcParserType;
                }
            }

            public ParserState(IPersistIfcEntity entity)
            {
                _currentInstance = new Part21Entity(entity);
                _processStack.Push(_currentInstance);
            }

            private readonly Stack<Part21Entity> _processStack = new Stack<Part21Entity>();
            private int _listNestLevel = -1;
            private Part21Entity _currentInstance;
            private readonly IndexPropertyValue _propertyValue = new IndexPropertyValue();

            public void BeginList()
            {
                Part21Entity p21 = _processStack.Peek();
                if (p21.CurrentParamIndex == -1)
                    p21.CurrentParamIndex++; //first time in take the first argument

                _listNestLevel++;
            }

            public void EndList()
            {
                _listNestLevel--;
                Part21Entity p21 = _processStack.Peek();
                p21.CurrentParamIndex++;
                //Console.WriteLine("EndList");
            }

            public void EndEntity()
            {
                _processStack.Pop();
                Debug.Assert(_processStack.Count == 0);
            }

            internal void BeginNestedType(string typeName)
            {
                IfcType ifcType = IfcInstances.IfcTypeLookup[typeName];
                _currentInstance = new Part21Entity((IPersistIfc)Activator.CreateInstance(ifcType.Type));
                _processStack.Push(_currentInstance);
            }

            internal void EndNestedType()
            {
                _propertyValue.Init(_processStack.Pop().Entity);
                _currentInstance = _processStack.Peek();
                if (_currentInstance.Entity != null)
                    _currentInstance.ParameterSetter(_currentInstance.CurrentParamIndex, _propertyValue);
                if (_listNestLevel == 0) _currentInstance.CurrentParamIndex++;
            }

            private void SetEntityParameter()
            {
                if (_currentInstance.Entity != null)
                {
                    //CurrentInstance.SetPropertyValue(PropertyValue);
                    _currentInstance.ParameterSetter(_currentInstance.CurrentParamIndex, _propertyValue);
                }
                if (_listNestLevel == 0)
                    _currentInstance.CurrentParamIndex++;
            }

            internal void SetIntegerValue(long value)
            {
                _propertyValue.Init(value, IfcParserType.Integer);
                SetEntityParameter();
            }

            internal void SetHexValue(double value)
            {
                _propertyValue.Init(value, IfcParserType.HexaDecimal);
                SetEntityParameter();
            }

            internal void SetFloatValue(double value)
            {
                _propertyValue.Init(value, IfcParserType.Real);
                SetEntityParameter();
            }

            internal void SetStringValue(string value)
            {
         
                _propertyValue.Init(value, IfcParserType.String);
                SetEntityParameter();
            }

            internal void SetEnumValue(string value)
            {
                _propertyValue.Init(value, IfcParserType.Enum);
                SetEntityParameter();
            }

            internal void SetBooleanValue(bool value)
            {
                _propertyValue.Init(value, IfcParserType.Boolean);
                SetEntityParameter();
            }

            internal void SetNonDefinedValue()
            {
                if (_listNestLevel == 0) _currentInstance.CurrentParamIndex++;
            }

            internal void SetOverrideValue()
            {
                if (_listNestLevel == 0) _currentInstance.CurrentParamIndex++;
            }

            internal void SetObjectValue(IPersistIfc value)
            {
                _propertyValue.Init(value);
                //CurrentInstance.SetPropertyValue(PropertyValue);
                _currentInstance.Entity.IfcParse(_currentInstance.CurrentParamIndex, _propertyValue);
                if (_listNestLevel == 0) _currentInstance.CurrentParamIndex++;
            }
        }

        private IfcPersonAndOrganization _defaultOwningUser;
        private IfcApplication _defaultOwningApplication;
        private IfcOwnerHistory _ownerHistoryAddObject;
        private IfcOwnerHistory _ownerHistoryModifyObject;
        protected HashSet<IPersistIfcEntity> ToWrite = new HashSet<IPersistIfcEntity>();
        protected UndoRedoSession undoRedoSession;

        private IfcCoordinatedUniversalTimeOffset _coordinatedUniversalTimeOffset;
        private IfcProject _project;


        private void InitialiseDefaultOwnership()
        {
            IfcPerson person = New<IfcPerson>();

            IfcOrganization organization = New<IfcOrganization>();
            IfcPersonAndOrganization owninguser = New<IfcPersonAndOrganization>(po =>
            {
                po.TheOrganization = organization;
                po.ThePerson = person;
            });
            Transaction.AddPropertyChange<IfcPersonAndOrganization>(m => _defaultOwningUser = m, _defaultOwningUser, owninguser);
            IfcApplication app = New<IfcApplication>(a => a.ApplicationDeveloper = New<IfcOrganization>());
            Transaction.AddPropertyChange<IfcApplication>(m => _defaultOwningApplication = m, _defaultOwningApplication, app);
            IfcOwnerHistory oh = New<IfcOwnerHistory>();
            oh.OwningUser = _defaultOwningUser;
            oh.OwningApplication = _defaultOwningApplication;
            oh.ChangeAction = IfcChangeActionEnum.ADDED;
            Transaction.AddPropertyChange<IfcOwnerHistory>(m => _ownerHistoryAddObject = m, _ownerHistoryAddObject, oh);
            _defaultOwningUser = owninguser;
            _defaultOwningApplication = app;
            _ownerHistoryAddObject = oh;
            IfcOwnerHistory ohc = New<IfcOwnerHistory>();
            ohc.OwningUser = _defaultOwningUser;
            ohc.OwningApplication = _defaultOwningApplication;
            ohc.ChangeAction = IfcChangeActionEnum.MODIFIED;
            Transaction.AddPropertyChange<IfcOwnerHistory>(m => _ownerHistoryModifyObject = m, _ownerHistoryModifyObject, ohc);
            _defaultOwningUser = owninguser;
            _defaultOwningApplication = app;
            _ownerHistoryModifyObject = ohc;
        }


        public abstract IPersistIfcEntity GetInstance(long entityLabel);

        /// <summary>
        ///   Removes any references in the entity properties that are in the entity Label list, returns true if the stream has been modified
        /// </summary>
        protected bool DerefenceEntities(byte[] propertyStream, HashSet<ulong> entityLabels, out byte[] derefencedStream)
        {
            bool modified = false;
            derefencedStream = new byte[propertyStream.Length];
            BinaryReader br = new BinaryReader(new MemoryStream(propertyStream));
            P21ParseAction action = (P21ParseAction)br.ReadByte();

            BinaryWriter bw = new BinaryWriter(new MemoryStream(derefencedStream));

            while (action != P21ParseAction.EndEntity)
            {
                switch (action)
                {
                    case P21ParseAction.BeginList:
                        bw.Write((byte)P21ParseAction.BeginList);
                        break;
                    case P21ParseAction.EndList:
                        bw.Write((byte)P21ParseAction.EndList);
                        break;
                    case P21ParseAction.BeginComplex:
                        break;
                    case P21ParseAction.EndComplex:
                        break;
                    case P21ParseAction.SetIntegerValue:
                        bw.Write((byte)P21ParseAction.SetIntegerValue);
                        bw.Write(br.ReadInt64());
                        break;
                    case P21ParseAction.SetHexValue:
                        bw.Write((byte)P21ParseAction.SetHexValue);
                        bw.Write(br.ReadInt64());
                        break;
                    case P21ParseAction.SetFloatValue:
                        bw.Write((byte)P21ParseAction.SetFloatValue);
                        bw.Write(br.ReadDouble());
                        break;
                    case P21ParseAction.SetStringValue:
                        bw.Write((byte)P21ParseAction.SetStringValue);
                        bw.Write(br.ReadString());
                        break;
                    case P21ParseAction.SetEnumValue:
                        bw.Write((byte)P21ParseAction.SetEnumValue);
                        bw.Write(br.ReadString());
                        break;
                    case P21ParseAction.SetBooleanValue:
                        bw.Write((byte)P21ParseAction.SetBooleanValue);
                        bw.Write(br.ReadBoolean());
                        break;
                    case P21ParseAction.SetNonDefinedValue:
                        bw.Write((byte)P21ParseAction.SetNonDefinedValue);
                        break;
                    case P21ParseAction.SetOverrideValue:
                        bw.Write((byte)P21ParseAction.SetOverrideValue);
                        break;
                    case P21ParseAction.SetObjectValueUInt16:
                        ushort label16 = br.ReadUInt16();
                        if (entityLabels.Contains(label16))
                        {
                            bw.Write((byte)P21ParseAction.SetNonDefinedValue);
                            modified = true;
                        }
                        else
                        {
                            bw.Write((byte)P21ParseAction.SetObjectValueUInt16);
                            bw.Write(label16);
                        }
                        break;
                    case P21ParseAction.SetObjectValueUInt32:
                        uint label32 = br.ReadUInt32();
                        if (entityLabels.Contains(label32))
                        {
                            bw.Write((byte)P21ParseAction.SetNonDefinedValue);
                            modified = true;
                        }
                        else
                        {
                            bw.Write((byte)P21ParseAction.SetObjectValueUInt32);
                            bw.Write(label32);
                        }
                        break;
                    case P21ParseAction.SetObjectValueInt64:
                        ulong label64 = br.ReadUInt64();
                        if (entityLabels.Contains(label64))
                        {
                            bw.Write((byte)P21ParseAction.SetNonDefinedValue);
                            modified = true;
                        }
                        else
                        {
                            bw.Write((byte)P21ParseAction.SetObjectValueInt64);
                            bw.Write(label64);
                        }
                        break;
                    case P21ParseAction.BeginNestedType:
                        bw.Write((byte)P21ParseAction.BeginNestedType);
                        bw.Write(br.ReadString());
                        break;
                    case P21ParseAction.EndNestedType:
                        bw.Write((byte)P21ParseAction.EndNestedType);
                        break;
                    case P21ParseAction.EndEntity:
                        bw.Write((byte)P21ParseAction.EndEntity);
                        break;
                    case P21ParseAction.NewEntity:
                        bw.Write((byte)P21ParseAction.NewEntity);
                        break;
                }
                action = (P21ParseAction)br.ReadByte();
            }
            bw.Write((byte)P21ParseAction.EndEntity);
            bw.Flush();
            return modified;
        }

        public string PrintProperties(IPersistIfcEntity entity, BinaryReader br, bool inSingleLine = false)
        {
            StringBuilder sb = new StringBuilder();
            StringBuilder sbSingleLine = new StringBuilder();
            P21ParseAction action = (P21ParseAction)br.ReadByte();
            string s = "";

            ParserState parserState = new ParserState(entity);
            while (action != P21ParseAction.EndEntity)
            {
                switch (action)
                {
                    case P21ParseAction.BeginList:
                        sb.AppendLine("(");
                        sbSingleLine.Append("(");
                        break;
                    case P21ParseAction.EndList:
                        sb.AppendLine(")");
                        sbSingleLine.Append(")");
                        break;
                    case P21ParseAction.BeginComplex:
                        sb.AppendLine("BC ");
                        sbSingleLine.Append("BC ");
                        break;
                    case P21ParseAction.EndComplex:
                        sb.AppendLine("EC ");
                        sbSingleLine.Append("EC ");
                        break;
                    case P21ParseAction.SetIntegerValue:
                        sb.Append("i64=");
                        s = br.ReadInt64().ToString();
                        sb.AppendLine(s);
                        sbSingleLine.Append("i64=");
                        sbSingleLine.Append(s);
                        break;
                    case P21ParseAction.SetHexValue:
                        sb.Append("H=");
                        s = br.ReadInt64().ToString();
                        sb.AppendLine(s);
                        sbSingleLine.Append("H=");
                        sbSingleLine.Append(s);
                        break;
                    case P21ParseAction.SetFloatValue:
                        sb.Append("F=");
                        s = br.ReadDouble().ToString();
                        sb.AppendLine(s);
                        sbSingleLine.Append("F=");
                        sbSingleLine.Append(s);
                        break;
                    case P21ParseAction.SetStringValue:
                        sb.Append("S=");
                        s = br.ReadString();
                        sb.AppendLine(s);
                        sbSingleLine.Append("S=");
                        sbSingleLine.Append(s);
                        break;
                    case P21ParseAction.SetEnumValue:
                        sb.Append("E=");
                        s = br.ReadString();
                        sb.AppendLine(s);
                        sbSingleLine.Append("E=");
                        sbSingleLine.Append(s);
                        break;
                    case P21ParseAction.SetBooleanValue:
                        sb.Append("i64=");
                        s = br.ReadBoolean().ToString();
                        sb.AppendLine(s);
                        sbSingleLine.Append("i64=");
                        sbSingleLine.Append(s);
                        break;
                    case P21ParseAction.SetNonDefinedValue:
                        sb.Append("$");
                        sbSingleLine.Append("$");
                        break;
                    case P21ParseAction.SetOverrideValue:
                        sb.Append("*");
                        sbSingleLine.Append("*");
                        break;
                    case P21ParseAction.SetObjectValueUInt16:
                        sb.Append("i16=");
                        s = br.ReadUInt16().ToString();
                        sb.AppendLine(s);
                        sbSingleLine.Append("i16=");
                        sbSingleLine.Append(s);
                        break;
                    case P21ParseAction.SetObjectValueUInt32:
                        sb.Append("i32=");
                        s = br.ReadUInt32().ToString();
                        sb.AppendLine(s);
                        sbSingleLine.Append("i32=");
                        sbSingleLine.Append(s);
                        break;
                    case P21ParseAction.SetObjectValueInt64:
                        sb.Append("i64=");
                        s = br.ReadInt64().ToString();
                        sb.AppendLine(s);
                        sbSingleLine.Append("i64=");
                        sbSingleLine.Append(s);
                        break;
                    case P21ParseAction.BeginNestedType:
                        sb.Append("BNT ");
                        s = br.ReadString();
                        sb.AppendLine(s);
                        sbSingleLine.Append("BNT ");
                        sbSingleLine.Append(s);
                        break;
                    case P21ParseAction.EndNestedType:
                        sb.Append("ENT ");
                        sbSingleLine.Append("ENT ");
                        break;
                    case P21ParseAction.EndEntity:
                        sb.Append("EE ");
                        sbSingleLine.Append("EE ");
                        break;
                    case P21ParseAction.NewEntity:
                        sb.Append("NE ");
                        sbSingleLine.Append("NE ");
                        break;
                    default:
                        throw new Exception("Invalid Property Record #" + entity.EntityLabel + " EntityType: " + entity.GetType().Name);
                }
                action = (P21ParseAction)br.ReadByte();
            }

            if (inSingleLine)
                return sbSingleLine.ToString();
            else
                return sb.ToString();
        }

        protected void PopulateProperties(IPersistIfcEntity entity, BinaryReader br)
        {
            P21ParseAction action = (P21ParseAction)br.ReadByte();

            ParserState parserState = new ParserState(entity);
            while (action != P21ParseAction.EndEntity)
            {
                switch (action)
                {
                    case P21ParseAction.BeginList:
                        parserState.BeginList();
                        break;
                    case P21ParseAction.EndList:
                        parserState.EndList();
                        break;
                    case P21ParseAction.BeginComplex:
                        break;
                    case P21ParseAction.EndComplex:
                        break;
                    case P21ParseAction.SetIntegerValue:
                        parserState.SetIntegerValue(br.ReadInt64());
                        break;
                    case P21ParseAction.SetHexValue:
                        parserState.SetHexValue(br.ReadInt64());
                        break;
                    case P21ParseAction.SetFloatValue:
                        parserState.SetFloatValue(br.ReadDouble());
                        break;
                    case P21ParseAction.SetStringValue:
                        parserState.SetStringValue(br.ReadString());
                        break;
                    case P21ParseAction.SetEnumValue:
                        parserState.SetEnumValue(br.ReadString());
                        break;
                    case P21ParseAction.SetBooleanValue:
                        parserState.SetBooleanValue(br.ReadBoolean());
                        break;
                    case P21ParseAction.SetNonDefinedValue:
                        parserState.SetNonDefinedValue();
                        break;
                    case P21ParseAction.SetOverrideValue:
                        parserState.SetOverrideValue();
                        break;
                    case P21ParseAction.SetObjectValueUInt16:
                        parserState.SetObjectValue(GetOrCreateEntity(br.ReadUInt16(), null));
                        break;
                    case P21ParseAction.SetObjectValueUInt32:
                        parserState.SetObjectValue(GetOrCreateEntity(br.ReadUInt32(), null));
                        break;
                    case P21ParseAction.SetObjectValueInt64:
                        parserState.SetObjectValue(GetOrCreateEntity(br.ReadInt64(), null));
                        break;
                    case P21ParseAction.BeginNestedType:
                        parserState.BeginNestedType(br.ReadString());
                        break;
                    case P21ParseAction.EndNestedType:
                        parserState.EndNestedType();
                        break;
                    case P21ParseAction.EndEntity:
                        parserState.EndEntity();
                        break;
                    case P21ParseAction.NewEntity:
                        parserState = new ParserState(entity);
                        break;
                    default:
                        throw new Exception("Invalid Property Record #" + entity.EntityLabel + " EntityType: " + entity.GetType().Name);
                }
                action = (P21ParseAction)br.ReadByte();
            }
        }

        protected abstract IPersistIfcEntity GetOrCreateEntity(long label, Type type);

        #region IDisposable Members

        public abstract void Dispose();

        #endregion

        #region IModel Members

        public abstract IEnumerable<TIfcType> InstancesOfType<TIfcType>() where TIfcType : IPersistIfcEntity;


        protected IPersistIfcEntity CreateEntity(long label, Type type)
        {
            IPersistIfcEntity entity = (IPersistIfcEntity)Activator.CreateInstance(type);

            entity.Bind(this, (label * -1));
            //_keepAlive.Add(entity);
            return entity;
        }


        public IEnumerable<T> InstancesWhere<T>(Expression<Func<T, bool>> expr) where T : IPersistIfcEntity
        {
            IEnumerable<T> sourceEnum = InstancesOfType<T>();
            Func<T, bool> predicate = expr.Compile();
            return sourceEnum.Where(predicate);
        }

        public abstract TIfcType New<TIfcType>() where TIfcType : IPersistIfcEntity, new();

        public abstract IPersistIfcEntity AddNew(IfcType ifcType, long label);


        public TIfcType New<TIfcType>(InitProperties<TIfcType> initPropertiesFunc)
            where TIfcType : IPersistIfcEntity, new()
        {
            TIfcType instance = New<TIfcType>();
            initPropertiesFunc(instance);
            return instance;
        }

        public bool Delete(IPersistIfcEntity instance)
        {
            throw new NotImplementedException();
        }

        public abstract bool ContainsInstance(IPersistIfcEntity instance);


        public abstract IEnumerable<IPersistIfcEntity> Instances { get; }


        public abstract long InstancesCount { get; }

        /// <summary>
        ///   Returns the number of instances of a specific type, NB does not include subtypes
        /// </summary>
        /// <param name = "t"></param>
        /// <returns></returns>
        public abstract long InstancesOfTypeCount(Type t);

        public int ParsePart21(Stream inputStream, FilterViewDefinition filter, TextWriter errorLog,
                               ReportProgressDelegate progressHandler)
        {
            throw new NotImplementedException();
        }


        public IfcOwnerHistory OwnerHistoryAddObject
        {
            get
            {
                return _ownerHistoryAddObject;
            }
        }
        public IfcOwnerHistory OwnerHistoryModifyObject
        {
            get
            {
                return _ownerHistoryModifyObject;
            }
        }
        public IfcCoordinatedUniversalTimeOffset CoordinatedUniversalTimeOffset
        {
            get
            {
                if (_coordinatedUniversalTimeOffset == null)
                {
                    using (BeginTransaction("CoordinatedUniversalTimeOffset"))
                    {
                        _coordinatedUniversalTimeOffset = New<IfcCoordinatedUniversalTimeOffset>();
                        DateTimeOffset localTime = DateTimeOffset.Now;
                        _coordinatedUniversalTimeOffset.HourOffset = new IfcHourInDay(localTime.Offset.Hours);
                        _coordinatedUniversalTimeOffset.MinuteOffset = new IfcMinuteInHour(localTime.Offset.Minutes);
                        if (localTime.Offset.Hours < 0 || (localTime.Offset.Hours == 0 && localTime.Offset.Minutes < 0))
                            _coordinatedUniversalTimeOffset.Sense = IfcAheadOrBehind.BEHIND;
                        else
                            _coordinatedUniversalTimeOffset.Sense = IfcAheadOrBehind.AHEAD;
                    }
                }
                return _coordinatedUniversalTimeOffset;
            }
        }

        public IfcProject IfcProject
        {
            get
            {
                if (_project == null) _project = InstancesOfType<IfcProject>().FirstOrDefault();
                return _project;
            }
        }

        public IfcProducts IfcProducts
        {
            get { return new IfcProducts(this); }
        }


        protected int WriteEntity(BinaryWriter entityWriter, IPersistIfcEntity entity)
        {
            long len = entityWriter.BaseStream.Position;

            IfcType ifcType = IfcInstances.IfcEntities[entity.GetType()];

            entityWriter.Write(Convert.ToByte(P21ParseAction.NewEntity));
            entityWriter.Write(Convert.ToByte(P21ParseAction.BeginList));
            foreach (IfcMetaProperty ifcProperty in ifcType.IfcProperties.Values)
            //only write out persistent attributes, ignore inverses
            {
                if (ifcProperty.IfcAttribute.State == IfcAttributeState.DerivedOverride)
                    entityWriter.Write(Convert.ToByte(P21ParseAction.SetOverrideValue));
                else
                {
                    Type propType = ifcProperty.PropertyInfo.PropertyType;
                    object propVal = ifcProperty.PropertyInfo.GetValue(entity, null);
                    WriteProperty(propType, propVal, entityWriter);
                }
            }
            entityWriter.Write(Convert.ToByte(P21ParseAction.EndList));
            entityWriter.Write(Convert.ToByte(P21ParseAction.EndEntity));

            return (int)(entityWriter.BaseStream.Position - len);
        }

        private void WriteProperty(Type propType, object propVal, BinaryWriter entityWriter)
        {
            Type itemType;
            if (propVal == null) //null or a value type that maybe null
                entityWriter.Write(Convert.ToByte(P21ParseAction.SetNonDefinedValue));
            else if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>))
            //deal with undefined types (nullables)
            {
                if (typeof(ExpressComplexType).IsAssignableFrom(propVal.GetType()))
                {
                    entityWriter.Write(Convert.ToByte(P21ParseAction.BeginList));
                    foreach (var compVal in ((ExpressComplexType)propVal).Properties)
                        WriteProperty(compVal.GetType(), compVal, entityWriter);
                    entityWriter.Write(Convert.ToByte(P21ParseAction.EndList));
                }
                else if ((typeof(ExpressType).IsAssignableFrom(propVal.GetType())))
                {
                    ExpressType expressVal = (ExpressType)propVal;
                    WriteValueType(expressVal.UnderlyingSystemType, expressVal.Value, entityWriter);
                }
                else
                {
                    WriteValueType(propVal.GetType(), propVal, entityWriter);
                }
            }
            else if (typeof(ExpressComplexType).IsAssignableFrom(propType))
            {
                entityWriter.Write(Convert.ToByte(P21ParseAction.BeginList));
                foreach (var compVal in ((ExpressComplexType)propVal).Properties)
                    WriteProperty(compVal.GetType(), compVal, entityWriter);
                entityWriter.Write(Convert.ToByte(P21ParseAction.EndList));
            }
            else if (typeof(ExpressType).IsAssignableFrom(propType))
            //value types with a single property (IfcLabel, IfcInteger)
            {
                Type realType = propVal.GetType();
                if (realType != propType)
                //we have a type but it is a select type use the actual value but write out explicitly
                {
                    entityWriter.Write(Convert.ToByte(P21ParseAction.BeginNestedType));
                    entityWriter.Write(realType.Name.ToUpper());
                    entityWriter.Write(Convert.ToByte(P21ParseAction.BeginList));
                    WriteProperty(realType, propVal, entityWriter);
                    entityWriter.Write(Convert.ToByte(P21ParseAction.EndList));
                    entityWriter.Write(Convert.ToByte(P21ParseAction.EndNestedType));
                }
                else //need to write out underlying property value
                {
                    ExpressType expressVal = (ExpressType)propVal;
                    WriteValueType(expressVal.UnderlyingSystemType, expressVal.Value, entityWriter);
                }
            }
            else if (typeof(ExpressEnumerable).IsAssignableFrom(propType) &&
                     (itemType = GetItemTypeFromGenericType(propType)) != null)
            //only process lists that are real lists, see cartesianpoint
            {
                entityWriter.Write(Convert.ToByte(P21ParseAction.BeginList));
                foreach (var item in ((ExpressEnumerable)propVal))
                    WriteProperty(itemType, item, entityWriter);
                entityWriter.Write(Convert.ToByte(P21ParseAction.EndList));
            }
            else if (typeof(IPersistIfcEntity).IsAssignableFrom(propType))
            //all writable entities must support this interface and ExpressType have been handled so only entities left
            {
                long val = Math.Abs(((IPersistIfcEntity)propVal).EntityLabel);
                if (val <= UInt16.MaxValue)
                {
                    entityWriter.Write((byte)P21ParseAction.SetObjectValueUInt16);
                    entityWriter.Write(Convert.ToUInt16(val));
                }
                else if (val <= UInt32.MaxValue)
                {
                    entityWriter.Write((byte)P21ParseAction.SetObjectValueUInt32);
                    entityWriter.Write(Convert.ToUInt32(val));
                }
                else if (val <= Int64.MaxValue)
                {
                    entityWriter.Write((byte)P21ParseAction.SetObjectValueInt64);
                    entityWriter.Write(val);
                }
                else
                    throw new Exception("Entity Label exceeds maximim value for a long number");
            }
            else if (propType.IsValueType) //it might be an in-built value type double, string etc
            {
                WriteValueType(propVal.GetType(), propVal, entityWriter);
            }
            else if (typeof(ExpressSelectType).IsAssignableFrom(propType))
            // a select type get the type of the actual value
            {
                if (propVal.GetType().IsValueType) //we have a value type, so write out explicitly
                {
                    entityWriter.Write(Convert.ToByte(P21ParseAction.BeginNestedType));
                    entityWriter.Write(propVal.GetType().Name.ToUpper());
                    entityWriter.Write(Convert.ToByte(P21ParseAction.BeginList));
                    WriteProperty(propVal.GetType(), propVal, entityWriter);
                    entityWriter.Write(Convert.ToByte(P21ParseAction.EndList));
                    entityWriter.Write(Convert.ToByte(P21ParseAction.EndNestedType));
                }
                else //could be anything so re-evaluate actual type
                {
                    WriteProperty(propVal.GetType(), propVal, entityWriter);
                }
            }
            else
                throw new Exception(string.Format("Entity  has illegal property {0} of type {1}",
                                                  propType.Name, propType.Name));
        }

        private void WriteValueType(Type pInfoType, object pVal, BinaryWriter entityWriter)
        {
            if (pInfoType == typeof(Double))
            {
                entityWriter.Write(Convert.ToByte(P21ParseAction.SetFloatValue));
                entityWriter.Write((double)pVal);
            }
            else if (pInfoType == typeof(String)) //convert  string
            {
                if (pVal == null)
                    entityWriter.Write(Convert.ToByte(P21ParseAction.SetNonDefinedValue));
                else
                {
                    entityWriter.Write(Convert.ToByte(P21ParseAction.SetStringValue));
                    entityWriter.Write((string)pVal);
                }
            }
            else if (pInfoType == typeof(Int16))
            {
                entityWriter.Write(Convert.ToByte(P21ParseAction.SetIntegerValue));
                entityWriter.Write((long)(Int16)pVal);
            }
            else if (pInfoType == typeof(Int32))
            {
                entityWriter.Write(Convert.ToByte(P21ParseAction.SetIntegerValue));
                entityWriter.Write((long)(Int32)pVal);
            }
            else if (pInfoType == typeof(Int64))
            {
                entityWriter.Write(Convert.ToByte(P21ParseAction.SetIntegerValue));
                entityWriter.Write((long)pVal);
            }
            else if (pInfoType.IsEnum) //convert enum
            {
                entityWriter.Write(Convert.ToByte(P21ParseAction.SetEnumValue));
                entityWriter.Write(pVal.ToString().ToUpper());
            }
            else if (pInfoType == typeof(Boolean))
            {
                entityWriter.Write(Convert.ToByte(P21ParseAction.SetBooleanValue));
                entityWriter.Write((bool)pVal);
            }

            else if (pInfoType == typeof(DateTime)) //convert  TimeStamp
            {
                IfcTimeStamp ts = IfcTimeStamp.ToTimeStamp((DateTime)pVal);
                entityWriter.Write(Convert.ToByte(P21ParseAction.SetIntegerValue));
                entityWriter.Write((long)ts);
            }
            else if (pInfoType == typeof(Guid)) //convert  Guid string
            {
                if (pVal == null)
                    entityWriter.Write(Convert.ToByte(P21ParseAction.SetNonDefinedValue));
                else
                {
                    entityWriter.Write(Convert.ToByte(P21ParseAction.SetStringValue));
                    entityWriter.Write((string)pVal);
                }
            }
            else if (pInfoType == typeof(bool?)) //convert  logical
            {
                bool? b = (bool?)pVal;
                if (!b.HasValue)
                    entityWriter.Write(Convert.ToByte(P21ParseAction.SetNonDefinedValue));
                else
                {
                    entityWriter.Write(Convert.ToByte(P21ParseAction.SetBooleanValue));
                    entityWriter.Write(b.Value);
                }
            }
            else
                throw new ArgumentException(string.Format("Invalid Value Type {0}", pInfoType.Name), "pInfoType");
        }

        public static Type GetItemTypeFromGenericType(Type genericType)
        {
            if (genericType == typeof(ICoordinateList))
                return typeof(IfcLengthMeasure); //special case for coordinates
            if (genericType.IsGenericType || genericType.IsInterface)
            {
                Type[] genericTypes = genericType.GetGenericArguments();
                if (genericTypes.GetUpperBound(0) >= 0)
                    return genericTypes[genericTypes.GetUpperBound(0)];
                return null;
            }
            if (genericType.BaseType != null)
                return GetItemTypeFromGenericType(genericType.BaseType);
            return null;
        }

        #region Part21 Writing

        // Extract first ifc file from zipped file and save in the same directory
        public string ExportZippedIfc(string inputIfcFile)
        {
            try
            {
                using (ZipInputStream zis = new ZipInputStream(File.OpenRead(inputIfcFile)))
                {
                    ZipEntry zs = zis.GetNextEntry();
                    while (zs != null)
                    {
                        String filePath = Path.GetDirectoryName(zs.Name);
                        String fileName = Path.GetFileName(zs.Name);
                        if (fileName.ToLower().EndsWith(".ifc"))
                        {
                            using (FileStream fs = File.Create(fileName))
                            {
                                int i = 2048;
                                byte[] b = new byte[i];
                                while (true)
                                {
                                    i = zis.Read(b, 0, b.Length);
                                    if (i > 0)
                                        fs.Write(b, 0, i);
                                    else
                                        break;
                                }
                            }
                            return fileName;
                        }
                    }

                }
            }
            catch (Exception e)
            {
                throw new Exception("Error creating Ifc File from ZIP = " + inputIfcFile, e);
            }
            return "";
        }

        public void ExportIfc(string fileName)
        {
            ExportIfc(fileName, false);
        }

        public void ExportIfc(string fileName, bool compress, bool isGZip = true)
        {
            TextWriter ifcFile = null;
            FileStream fs = null;
            try
            {
                if (compress)
                {
                    if (isGZip)
                    {
                        fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
                        GZipStream zip = new GZipStream(fs, CompressionMode.Compress);
                        ifcFile = new StreamWriter(zip);
                    }
                    else // if isGZip == false then use sharpziplib
                    {
                        string ext = "";
                        if (fileName.ToLower().EndsWith(".zip") == false || fileName.ToLower().EndsWith(".ifczip") == false) ext = ".ifczip";
                        fs = new FileStream(fileName + ext, FileMode.Create, FileAccess.Write);
                        ZipOutputStream zipStream = new ZipOutputStream(fs);
                        zipStream.SetLevel(3); //0-9, 9 being the highest level of compression
                        ZipEntry newEntry = new ZipEntry(fileName);
                        newEntry.DateTime = DateTime.Now;
                        zipStream.PutNextEntry(newEntry);

                        ifcFile = new StreamWriter(zipStream);
                    }
                }
                else
                {
                    ifcFile = new StreamWriter(fileName);
                }
                ExportIfc(ifcFile);
                ifcFile.Flush();
            }
            catch (Exception e)
            {
                throw new Exception("Error creating Ifc File = " + fileName, e);
            }
            finally
            {
                if (ifcFile != null) ifcFile.Close();
                if (fs != null) fs.Close();
            }
        }

        public void ExportIfc(TextWriter entityWriter)
        {
            WriteHeader(entityWriter);
            foreach (var item in Instances)
            {
                WriteEntity(entityWriter, item);
            }
            WriteFooter(entityWriter);
        }

        public void ExportIfcXml(string ifcxmlFileName)
        {
            FileStream xmlOutStream = null;
            try
            {
                xmlOutStream = new FileStream(ifcxmlFileName, FileMode.Create, FileAccess.ReadWrite);
                XmlWriterSettings settings = new XmlWriterSettings { Indent = true };
                using (XmlWriter xmlWriter = XmlWriter.Create(xmlOutStream, settings))
                {
                    IfcXmlWriter writer = new IfcXmlWriter();

                    // when onverting ifc to xml, 
                    // 1. you can specify perticular lines in fic file as below below 
                    // 2. OR pass null to convert full ifc format to xml
                    //List<IPersistIfcEntity> instances = new List<IPersistIfcEntity>();
                    //instances.Add(this.GetInstance(79480));
                    //instances.Add(this.GetInstance(2717770));
                    //writer.Write(this, xmlWriter, instances);

                    writer.Write(this, xmlWriter, null);
                }
            }
            catch (Exception e)
            {
                throw new Exception("Failed to write IfcXml file " + ifcxmlFileName, e);
            }
            finally
            {
                if (xmlOutStream != null) xmlOutStream.Close();
            }
        }


        private void WriteHeader(TextWriter tw)
        {
            //FileDescription fileDescriptor = new FileDescription("2;1");
            FileDescription fileDescriptor = Header.FileDescription;
            FileName fileName = Header.FileName;
            //FileName fileName = new FileName(DateTime.Now)
            //                        {
            //                            //PreprocessorVersion =
            //                            //    string.Format("Xbim.Ifc File Processor version {0}",
            //                            //                  Assembly.GetAssembly(typeof (P21Parser)).GetName().Version),
            //                            //OriginatingSystem =
            //                            //    string.Format("Xbim version {0}",
            //                            //                  Assembly.GetExecutingAssembly().GetName().Version),

            //                            PreprocessorVersion = Header.FileName.PreprocessorVersion,
            //                            OriginatingSystem = Header.FileName.OriginatingSystem,
            //                            Name = Header.FileName.Name,
            //                        };
            FileSchema fileSchema = new FileSchema("IFC2X3");
            StringBuilder header = new StringBuilder();
            header.AppendLine("ISO-10303-21;");
            header.AppendLine("HEADER;");
            //FILE_DESCRIPTION
            header.Append("FILE_DESCRIPTION((");
            int i = 0;
            foreach (string item in fileDescriptor.Description)
            {
                header.AppendFormat(@"{0}'{1}'", i == 0 ? "" : ",", item);
                i++;
            }
            header.AppendFormat(@"),'{0}');", fileDescriptor.ImplementationLevel);
            header.AppendLine();
            //FileName
            header.Append("FILE_NAME(");
            header.AppendFormat(@"'{0}'", fileName.Name);
            header.AppendFormat(@",'{0}'", fileName.TimeStamp);
            header.Append(",(");
            i = 0;
            foreach (string item in fileName.AuthorName)
            {
                header.AppendFormat(@"{0}'{1}'", i == 0 ? "" : ",", item);
                i++;
            }
            header.Append("),(");
            i = 0;
            foreach (string item in fileName.Organization)
            {
                header.AppendFormat(@"{0}'{1}'", i == 0 ? "" : ",", item);
                i++;
            }
            header.AppendFormat(@"),'{0}','{1}','{2}');", fileName.PreprocessorVersion, fileName.OriginatingSystem,
                                fileName.AuthorizationName);
            header.AppendLine();
            //FileSchema
            header.AppendFormat("FILE_SCHEMA(('{0}'));", fileSchema.Schemas.FirstOrDefault());
            header.AppendLine();
            header.AppendLine("ENDSEC;");
            header.AppendLine("DATA;");
            tw.Write(header.ToString());
        }

        private void WriteFooter(TextWriter tw)
        {
            tw.WriteLine("ENDSEC;");
            tw.WriteLine("END-ISO-10303-21;");
        }


        private void WriteEntity(TextWriter entityWriter, IPersistIfcEntity entity)
        {
            //try
            //{
            entityWriter.Write(string.Format("#{0}={1}(", Math.Abs(entity.EntityLabel), entity.GetType().Name.ToUpper()));
            IfcType ifcType = IfcInstances.IfcEntities[entity.GetType()];
            bool first = true;
            entity.Activate(false);
            foreach (IfcMetaProperty ifcProperty in ifcType.IfcProperties.Values)
            //only write out persistent attributes, ignore inverses
            {
                if (ifcProperty.IfcAttribute.State == IfcAttributeState.DerivedOverride)
                {
                    if (!first)
                        entityWriter.Write(',');
                    entityWriter.Write('*');
                    first = false;
                }
                else
                {
                    Type propType = ifcProperty.PropertyInfo.PropertyType;
                    object propVal = ifcProperty.PropertyInfo.GetValue(entity, null);
                    if (!first)
                        entityWriter.Write(',');
                    WriteProperty(propType, propVal, entityWriter);
                    first = false;
                }
            }
            entityWriter.WriteLine(");");
            //}
            //catch (Exception e)
            //{

            //    throw;
            //}
        }

        private void WriteProperty(Type propType, object propVal, TextWriter entityWriter)
        {
            Type itemType;
            if (propVal == null) //null or a value type that maybe null
                entityWriter.Write('$');

            else if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>))
            //deal with undefined types (nullables)
            {
                if (typeof(ExpressComplexType).IsAssignableFrom(propVal.GetType()))
                {
                    entityWriter.Write('(');
                    bool first = true;
                    foreach (var compVal in ((ExpressComplexType)propVal).Properties)
                    {
                        if (!first)
                            entityWriter.Write(',');
                        WriteProperty(compVal.GetType(), compVal, entityWriter);
                        first = false;
                    }
                    entityWriter.Write(')');
                }
                else if ((typeof(ExpressType).IsAssignableFrom(propVal.GetType())))
                {
                    ExpressType expressVal = (ExpressType)propVal;
                    WriteValueType(expressVal.UnderlyingSystemType, expressVal.Value, entityWriter);
                }
                else // if (propVal.GetType().IsEnum)
                {
                    WriteValueType(propVal.GetType(), propVal, entityWriter);
                }

            }
            else if (typeof(ExpressComplexType).IsAssignableFrom(propType))
            {
                entityWriter.Write('(');
                bool first = true;
                foreach (var compVal in ((ExpressComplexType)propVal).Properties)
                {
                    if (!first)
                        entityWriter.Write(',');
                    WriteProperty(compVal.GetType(), compVal, entityWriter);
                    first = false;
                }
                entityWriter.Write(')');
            }
            else if (typeof(ExpressType).IsAssignableFrom(propType))
            //value types with a single property (IfcLabel, IfcInteger)
            {
                Type realType = propVal.GetType();
                if (realType != propType)
                //we have a type but it is a select type use the actual value but write out explricitly
                {
                    entityWriter.Write(realType.Name.ToUpper());
                    entityWriter.Write('(');
                    WriteProperty(realType, propVal, entityWriter);
                    entityWriter.Write(')');
                }
                else //need to write out underlying property value
                {
                    ExpressType expressVal = (ExpressType)propVal;
                    WriteValueType(expressVal.UnderlyingSystemType, expressVal.Value, entityWriter);
                }
            }
            else if (typeof(ExpressEnumerable).IsAssignableFrom(propType) &&
                     (itemType = GetItemTypeFromGenericType(propType)) != null)
            //only process lists that are real lists, see cartesianpoint
            {
                entityWriter.Write('(');
                bool first = true;
                foreach (var item in ((ExpressEnumerable)propVal))
                {
                    if (!first)
                        entityWriter.Write(',');
                    WriteProperty(itemType, item, entityWriter);
                    first = false;
                }
                entityWriter.Write(')');
            }
            else if (typeof(IPersistIfcEntity).IsAssignableFrom(propType))
            //all writable entities must support this interface and ExpressType have been handled so only entities left
            {
                entityWriter.Write('#');
                entityWriter.Write(Math.Abs(((IPersistIfcEntity)propVal).EntityLabel));
            }
            else if (propType.IsValueType) //it might be an in-built value type double, string etc
            {
                WriteValueType(propVal.GetType(), propVal, entityWriter);
            }
            else if (typeof(ExpressSelectType).IsAssignableFrom(propType))
            // a select type get the type of the actual value
            {
                if (propVal.GetType().IsValueType) //we have a value type, so write out explicitly
                {
                    entityWriter.Write(propVal.GetType().Name.ToUpper());
                    entityWriter.Write('(');
                    WriteProperty(propVal.GetType(), propVal, entityWriter);
                    entityWriter.Write(')');
                }
                else //could be anything so re-evaluate actual type
                {
                    WriteProperty(propVal.GetType(), propVal, entityWriter);
                }
            }
            else
                throw new Exception(string.Format("Entity  has illegal property {0} of type {1}",
                                                  propType.Name, propType.Name));
        }

        private void WriteValueType(Type pInfoType, object pVal, TextWriter entityWriter)
        {
            if (pInfoType == typeof(Double))
                entityWriter.Write(string.Format(new Part21Formatter(), "{0:R}", pVal));
            else if (pInfoType == typeof(String)) //convert  string
            {
                if (pVal == null)
                    entityWriter.Write('$');
                else
                {
                    entityWriter.Write('\'');
                    entityWriter.Write(IfcText.Escape((string)pVal));
                    entityWriter.Write('\'');
                }
            }
            else if (pInfoType == typeof(Int16) || pInfoType == typeof(Int32) || pInfoType == typeof(Int64))
                entityWriter.Write(pVal.ToString());
            else if (pInfoType.IsEnum) //convert enum
                entityWriter.Write(string.Format(".{0}.", pVal.ToString().ToUpper()));
            else if (pInfoType == typeof(Boolean))
            {
                bool b = (bool)pVal;
                entityWriter.Write(string.Format(".{0}.", b ? "T" : "F"));
            }
            else if (pInfoType == typeof(DateTime)) //convert  TimeStamp
                entityWriter.Write(string.Format(new Part21Formatter(), "{0:T}", pVal));
            else if (pInfoType == typeof(Guid)) //convert  Guid string
            {
                if (pVal == null)
                    entityWriter.Write('$');
                else
                    entityWriter.Write(string.Format(new Part21Formatter(), "{0:G}", pVal));
            }
            else if (pInfoType == typeof(bool?)) //convert  logical
            {
                bool? b = (bool?)pVal;
                entityWriter.Write(!b.HasValue ? "$" : string.Format(".{0}.", b.Value ? "T" : "F"));
            }
            else
                throw new ArgumentException(string.Format("Invalid Value Type {0}", pInfoType.Name), "pInfoType");
        }

        #endregion

        public abstract long Activate(IPersistIfcEntity entity, bool write);


        public IfcApplication DefaultOwningApplication
        {
            get { return _defaultOwningApplication; }
        }

        public IfcPersonAndOrganization DefaultOwningUser
        {
            get { return _defaultOwningUser; }
        }

        protected Transaction BeginEdit(string operationName)
        {
            if (undoRedoSession == null)
            {
                undoRedoSession = new UndoRedoSession();
                Transaction txn = undoRedoSession.Begin(operationName);
                InitialiseDefaultOwnership();
                return txn;
            }
            else return null;
        }

        public Transaction BeginTransaction(string operationName)
        {
            Transaction txn = BeginEdit(operationName);
            //Debug.Assert(ToWrite.Count == 0);
            if (txn == null) txn = undoRedoSession.Begin(operationName);
            //txn.Finalised += TransactionFinalised;
            //txn.Reversed += TransactionReversed;
            return txn;
        }

        private void TransactionReversed()
        {

        }

        protected abstract void TransactionFinalised();

        #endregion

        #region Access Properties

        #endregion

        public IList<IfcSpace> Spaces()
        {
            IList<IfcSpace> spaces = InstancesOfType<IfcSpace>().ToList();
            foreach (var item in spaces)
            {
                ((IPersistIfcEntity)item).Activate(false);
            }
            return spaces;
        }

        public IList<IfcBuildingElementType> BuildingElementTypes()
        {
            IList<IfcBuildingElementType> types = InstancesOfType<IfcBuildingElementType>().ToList();
            foreach (var item in types)
            {
                ((IPersistIfcEntity)item).Activate(false);
            }
            return types;
        }

        public IList<IfcBuildingElement> BuildingElement()
        {
            IList<IfcBuildingElement> types = InstancesOfType<IfcBuildingElement>().ToList();
            foreach (var item in types)
            {
                ((IPersistIfcEntity)item).Activate(false);
            }
            return types;
        }

        public IList<IfcWall> WallElements()
        {
            IList<IfcWall> types = InstancesOfType<IfcWall>().ToList();
            foreach (var item in types)
            {
                ((IPersistIfcEntity)item).Activate(false);
            }
            return types;
        }

        public IList<IfcMaterial> Materials()
        {
            IList<IfcMaterial> materials = InstancesOfType<IfcMaterial>().ToList();
            foreach (var item in materials)
            {
                ((IPersistIfcEntity)item).Activate(false);
            }
            return materials;
        }


        public abstract IfcFileHeader Header { get; }
        public abstract bool ReOpen();
        public abstract void Close();

        public IEnumerable<Tuple<string, long>> ModelStatistics()
        {
            IfcType ifcType = IfcInstances.IfcEntities[typeof(IfcBuildingElement)];
            return (from elemType in ifcType.NonAbstractSubTypes
                    let cnt = InstancesOfTypeCount(elemType)
                    where cnt > 0
                    select Tuple.Create(elemType.Name, cnt)).ToList();
        }

        public abstract int Validate(TextWriter errStream, ReportProgressDelegate progressDelegate,
                                     ValidationFlags validateFlags);

        public int Validate(TextWriter errStream)
        {
            return Validate(errStream, null, ValidationFlags.All);
        }

        public int Validate(TextWriter errStream, ReportProgressDelegate progressDelegate)
        {
            return Validate(errStream, progressDelegate, ValidationFlags.All);
        }

        public string Validate(ValidationFlags validateFlags)
        {
            StringBuilder sb = new StringBuilder();
            TextWriter tw = new StringWriter(sb);
            Validate(tw, null, validateFlags);
            return sb.ToString();
        }


        public IEnumerable<IfcWall> Walls
        {
            get { return InstancesOfType<IfcWall>(); }
        }

        public IEnumerable<IfcSlab> Slabs
        {
            get { return InstancesOfType<IfcSlab>(); }
        }

        public IEnumerable<IfcDoor> Doors
        {
            get { return InstancesOfType<IfcDoor>(); }
        }

        public IEnumerable<IfcRoof> Roofs
        {
            get { return InstancesOfType<IfcRoof>(); }
        }
        /// <summary>
        /// Saves incremental changes to the model
        /// </summary>
        public abstract void WriteChanges(BinaryWriter dataStream);
        public abstract void MergeChanges(Stream dataStream);


        public UndoRedoSession UndoRedo
        {
            get { return undoRedoSession; }
        }



        /// <summary>
        ///   Convert xBim file to IFC, IFCXml Or Zip format
        /// </summary>
        /// <param name = "fileType">file type to convert to</param>
        /// <param name = "outputFileName">output filename for the new file after Export</param>
        /// <returns>outputFileName</returns>
        public void Export(XbimStorageType fileType, string outputFileName)
        {
            if (fileType.HasFlag(XbimStorageType.IFCXML))
            {
                // modelServer would have been created with xbim file
                ExportIfcXml(outputFileName);
            }
            else if (fileType.HasFlag(XbimStorageType.IFC))
            {
                // modelServer would have been created with xbim file and readwrite fileaccess
                ExportIfc(outputFileName);
            }
            else if (fileType.HasFlag(XbimStorageType.IFCZIP))
            {
                // modelServer would have been created with xbim file and readwrite fileaccess
                ExportIfc(outputFileName, true, false);
            }
            else
                throw new Exception("Invalid file type. Expected filetypes to Export: IFC, IFCXml, IFCZip");
            
        }

        abstract public bool Save();
       
        public bool SaveAs(string outputFileName)
        {
            // always save file as xbim, with user given filename
            Export(XbimStorageType.XBIM, outputFileName);

            return true;
        }

        public abstract string Open(string inputFileName);
        public abstract string Open(string inputFileName, ReportProgressDelegate progDelegate);
        public abstract void Import(string inputFileName);


        public abstract bool ContainsInstance(long entityLabel);
    }
}

#endif