#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    Part21FileWriter.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.XbimExtensions.Interfaces;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.XbimExtensions;
using Xbim.Common.Exceptions;

#endregion

namespace Xbim.IO
{
    public class Part21FileWriter 
    {

        private HashSet<long> _written;
        
        private void WriteEntity(TextWriter output, Type type, List<string> tokens, string id)
        {
            StringBuilder str = new StringBuilder();
            str.AppendFormat("{0}={1}(", id, type.Name.ToUpper());
            for (int i = 0; i < tokens.Count; i++)
            {
                if (i == 0)
                    str.Append(tokens[i]);
                else
                    str.AppendFormat(",{0}", tokens[i]);
            }
            str.Append(");");
            output.WriteLine(str);
        }


        public void Write(XbimModel model, TextWriter output)
        {
            try
            {

                _written = new HashSet<long>();
                output.Write(HeaderAsString(model.Header));
                foreach (IfcInstanceHandle item in model.InstanceHandles /*.Types.OrderBy(t=>t.Name)*/)
                {
                    IPersistIfcEntity entity = model.GetInstanceVolatile(item.EntityLabel);
                    entity.WriteEntity(output);
                }

                output.WriteLine("ENDSEC;");
                output.WriteLine("END-ISO-10303-21;");
               
            }
            catch (Exception e)
            {  
                throw new XbimException("Failed to write Ifc file", e);
            }
            finally
            {
                _written = null;
            }
        }

        private string Write(XbimModel model, TextWriter output, IfcInstanceHandle handle)
        {
            string id = string.Format("#{0}", handle.EntityLabel);
            if (_written.Contains(handle.EntityLabel))
                return id;
            else
            {
                _written.Add(handle.EntityLabel);
            }
            IfcType ifcType = IfcInstances.IfcEntities[handle.EntityType];

            List<string> tokens = new List<string>();
            IPersistIfcEntity entity = model.GetInstanceVolatile(handle.EntityLabel); //load either the cache or a volatile version of the entity
            foreach (IfcMetaProperty ifcProperty in ifcType.IfcProperties.Values)
            //only write out persistent attributes, ignore inverses
            {
                if (ifcProperty.IfcAttribute.State == IfcAttributeState.DerivedOverride)
                    tokens.Add("*");
                else
                {
                    Type propType = ifcProperty.PropertyInfo.PropertyType;
                    object propVal = ifcProperty.PropertyInfo.GetValue(entity, null);

                    tokens.Add(ConvertPropertyToPart21String(model, output, propType, propVal, entity));
                }
            }
            WriteEntity(output, ifcType.Type, tokens, id);
            return id;
        }

        private string ConvertPropertyToPart21String(XbimModel model, TextWriter output, Type propType, object propVal, object entity)
        {
            Type itemType;
            if (propVal == null) //null or a value type that maybe null
                return "$";
            else if (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>) &&
                     (typeof(ExpressType).IsAssignableFrom(propVal.GetType()))) //deal with undefined types (nullables)
                return ((ExpressType)propVal).ToPart21;
            else if (typeof(ExpressType).IsAssignableFrom(propType))
            {
                Type realType = propVal.GetType();

                if (realType != propType)
                    //we have a type but it is a select type use the actual value but write out explicitly
                    return string.Format("{0}({1})", realType.Name.ToUpper(),
                                         ConvertPropertyToPart21String(model, output, realType, propVal, entity));
                else
                    return ((ExpressType)propVal).ToPart21;
            }
            else if (typeof(ExpressEnumerable).IsAssignableFrom(propType) &&
                     (itemType = GetItemTypeFromGenericType(propType)) != null)
            //only process lists that are real lists, see cartesianpoint
            {
                StringBuilder listStr = new StringBuilder("(");
                bool first = true;

                foreach (object item in ((ExpressEnumerable)propVal))
                {
                    if (first)
                    {
                        listStr.Append(ConvertPropertyToPart21String(model, output, itemType, item, entity));
                        first = false;
                    }
                    else
                    {
                        listStr.Append(",");
                        listStr.Append(ConvertPropertyToPart21String(model, output, itemType, item, entity));
                    }
                }
                listStr.Append(")");
                return listStr.ToString();
            }

            else if (typeof(IPersistIfcEntity).IsAssignableFrom(propType))
                //all writable entities must support this interface and ExpressType have been handled so only entities left
                return Write(model, output, new IfcInstanceHandle(((IPersistIfcEntity)propVal).EntityLabel, propVal.GetType()));
            else if (propType.IsValueType) //it might be an in-built value type double, string etc
                return ConvertValueTypeToPart21String(propVal.GetType(), propVal);
            else if (typeof(ExpressSelectType).IsAssignableFrom(propType))
            // a select type get the type of the actual value
            {
                if (propVal.GetType().IsValueType) //we have a value type, so write out explicitly
                    return string.Format("{0}({1})", propVal.GetType().Name.ToUpper(),
                                         ConvertPropertyToPart21String(model, output, propVal.GetType(), propVal,
                                                                       entity));
                else //could be anything so re-evaluate actual type
                    return ConvertPropertyToPart21String(model, output, propVal.GetType(), propVal, entity);
                //reduce to actual type
            }
            else
                throw new Exception(
                    string.Format("Entity of type {0} has illegal property {1} of type {2}",
                                  entity.GetType(), propType.Name, propType.Name));
        }

        private Type GetItemTypeFromGenericType(Type genericType)
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


        private string ConvertValueTypeToPart21String(Type pInfoType, object pVal)
        {
            if (pInfoType.IsEnum) //convert enum
                return string.Format(".{0}.", pVal.ToString().ToUpper());
            else if (pInfoType.UnderlyingSystemType == typeof(Boolean))
                return string.Format(".{0}.", (bool)pVal ? "T" : "F");
            if (pInfoType.UnderlyingSystemType == typeof(Double))
                return string.Format(new Part21Formatter(), "{0:R}", pVal);
            if (pInfoType.UnderlyingSystemType == typeof(Int16) || pInfoType.UnderlyingSystemType == typeof(Int32) ||
                pInfoType.UnderlyingSystemType == typeof(Int64))
                return pVal.ToString();
            else if (pInfoType.UnderlyingSystemType == typeof(DateTime)) //convert  TimeStamp
                return string.Format(new Part21Formatter(), "{0:T}", pVal);
            else if (pInfoType.UnderlyingSystemType == typeof(Guid)) //convert  Guid string
                return string.Format(new Part21Formatter(), "{0:G}", pVal);
            else if (pInfoType.UnderlyingSystemType == typeof(String)) //convert  string
                return string.Format(new Part21Formatter(), "{0}", pVal);
            else
                throw new ArgumentException(string.Format("Invalid Value Type {0}", pInfoType.Name), "pInfoType");
        }

        private string ConvertEntityTypeToPart21String(Type pInfoType, object pVal)
        {
            return "$";
        }

        private string HeaderAsString(IIfcFileHeader header)
        {
            StringBuilder headerStr = new StringBuilder();
            headerStr.AppendLine("ISO-10303-21;");
            headerStr.AppendLine("HEADER;");
            //FILE_DESCRIPTION
            headerStr.Append("FILE_DESCRIPTION ((");
            int i = 0;

            if (header.FileDescription.Description.Count == 0)
            {
                headerStr.Append(@"''");
            }
            else
            {
                foreach (string item in header.FileDescription.Description)
                {
                    headerStr.AppendFormat(@"{0}'{1}'", i == 0 ? "" : ",", item);
                    i++;
                }
            }
            headerStr.AppendFormat(@"), '{0}');", header.FileDescription.ImplementationLevel);
            headerStr.AppendLine();
            //FileName
            headerStr.Append("FILE_NAME (");
            headerStr.AppendFormat(@"'{0}'", header.FileName.Name);
            headerStr.AppendFormat(@", '{0}'", header.FileName.TimeStamp);
            headerStr.Append(", (");
            i = 0;
            if (header.FileName.AuthorName.Count == 0)
                headerStr.Append(@"''");
            else
            {
                foreach (string item in header.FileName.AuthorName)
                {
                    headerStr.AppendFormat(@"{0}'{1}'", i == 0 ? "" : ",", item);
                    i++;
                }
            }
            headerStr.Append("), (");
            i = 0;
            if (header.FileName.Organization.Count == 0)
                headerStr.Append(@"''");
            else
            {
                foreach (string item in header.FileName.Organization)
                {
                    headerStr.AppendFormat(@"{0}'{1}'", i == 0 ? "" : ",", item);
                    i++;
                }
            }
            headerStr.AppendFormat(@"), '{0}', '{1}', '{2}');", header.FileName.PreprocessorVersion, header.FileName.OriginatingSystem,
                                header.FileName.AuthorizationName);
            headerStr.AppendLine();
            //FileSchema
            headerStr.AppendFormat("FILE_SCHEMA (('{0}'));", header.FileSchema.Schemas.FirstOrDefault());
            headerStr.AppendLine();
            headerStr.AppendLine("ENDSEC;");
            headerStr.AppendLine("DATA;");
            return headerStr.ToString();
        }

       
        
    }
}