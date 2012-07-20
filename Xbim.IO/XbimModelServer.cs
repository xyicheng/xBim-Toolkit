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
using Xbim.Ifc2x3.ActorResource;
using Xbim.Ifc2x3.DateTimeResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.SelectTypes;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.UtilityResource;
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
    public abstract class XbimModelServer : XbimModel
    {
       

        

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

            XbimParserState parserState = new XbimParserState(entity);
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

            XbimParserState parserState = new XbimParserState(entity);
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
                        parserState.SetObjectValue(GetInstance(br.ReadUInt16()));
                        break;
                    case P21ParseAction.SetObjectValueUInt32:
                        parserState.SetObjectValue(GetInstance(br.ReadUInt32()));
                        break;
                    case P21ParseAction.SetObjectValueInt64:
                        parserState.SetObjectValue(GetInstance(br.ReadInt64()));
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
                        parserState = new XbimParserState(entity);
                        break;
                    default:
                        throw new Exception("Invalid Property Record #" + entity.EntityLabel + " EntityType: " + entity.GetType().Name);
                }
                action = (P21ParseAction)br.ReadByte();
            }
        }



        #region IDisposable Members

       

        #endregion

        #region IModel Members

      

        protected IPersistIfcEntity CreateEntity(long label, Type type)
        {
            IPersistIfcEntity entity = (IPersistIfcEntity)Activator.CreateInstance(type);
            entity.Bind(this, (label * -1));
            return entity;
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

        private void TransactionReversed()
        {

        }

        protected abstract void TransactionFinalised();

        #endregion

        #region Access Properties

        #endregion



        public override IEnumerable<Tuple<string, long>> ModelStatistics()
        {
            IfcType ifcType = IfcInstances.IfcEntities[typeof(IfcBuildingElement)];
            return (from elemType in ifcType.NonAbstractSubTypes
                    let cnt = InstancesOfTypeCount(elemType)
                    where cnt > 0
                    select Tuple.Create(elemType.Name, cnt)).ToList();
        }

        /// <summary>
        /// Saves incremental changes to the model
        /// </summary>
        public abstract void WriteChanges(BinaryWriter dataStream);
        public abstract void MergeChanges(Stream dataStream);

    }
}

#endif