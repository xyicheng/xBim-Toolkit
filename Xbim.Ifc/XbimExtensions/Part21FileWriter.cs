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
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions.Parser;
using Xbim.Ifc.GeometryResource;
using Xbim.Ifc.MeasureResource;

#endregion

namespace Xbim.XbimExtensions
{
    public class Part21FileWriter : IDisposable
    {


        [NonSerialized]
        private TextWriter _output;
        [NonSerialized]
        private HashSet<long> _written;
        
        public Part21FileWriter()
        {
        }
        public Part21FileWriter (TextWriter Output)
        {
            _written = new HashSet<long>();
            _output = Output;
        }
               

        private void WriteEntity(Type type, List<string> tokens, string id)
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
            _output.WriteLine(str);
        }


        public void Write(IModel model, TextWriter output)
        {
            try
            {

                _written = new HashSet<long>();
                _output = output;
                WriteHeader(model);

                foreach (IPersistIfcEntity item in model.Instances /*.Types.OrderBy(t=>t.Name)*/)
                {
                    //foreach (var item in model.Instances[type])
                    //{
                    Write(item);
                    //}
                }

                WriteFooter();
                Close();
            }
            catch (Exception e)
            {
                Close();
                throw new Exception("Failed to write Ifc file", e);
            }
            finally
            {
                _written = null;
                _output = null;
            }
        }

        public string Write(IPersistIfcEntity entity)
        {
            string id = string.Format("#{0}", entity.EntityLabel);
            if (_written.Contains(entity.EntityLabel))
                return id;
            else
            {
                _written.Add(entity.EntityLabel);
            }
            IfcType ifcType = IfcInstances.IfcEntities[entity.GetType()];

            List<string> tokens = new List<string>();
            foreach (IfcMetaProperty ifcProperty in ifcType.IfcProperties.Values)
            //only write out persistent attributes, ignore inverses
            {
                if (ifcProperty.IfcAttribute.State == IfcAttributeState.DerivedOverride)
                    tokens.Add("*");
                else
                {
                    Type propType = ifcProperty.PropertyInfo.PropertyType;
                    object propVal = ifcProperty.PropertyInfo.GetValue(entity, null);

                    tokens.Add(ConvertPropertyToPart21String(propType, propVal, entity));
                }
            }


            WriteEntity(ifcType.Type, tokens, id);

            return id;
        }

        private string ConvertPropertyToPart21String(Type propType, object propVal, object entity)
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
                                         ConvertPropertyToPart21String(realType, propVal, entity));
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
                        listStr.Append(ConvertPropertyToPart21String(itemType, item, entity));
                        first = false;
                    }
                    else
                    {
                        listStr.Append(",");
                        listStr.Append(ConvertPropertyToPart21String(itemType, item, entity));
                    }
                }
                listStr.Append(")");
                return listStr.ToString();
            }

            else if (typeof(IPersistIfcEntity).IsAssignableFrom(propType))
                //all writable entities must support this interface and ExpressType have been handled so only entities left
                return Write((IPersistIfcEntity)propVal);
            else if (propType.IsValueType) //it might be an in-built value type double, string etc
                return ConvertValueTypeToPart21String(propVal.GetType(), propVal);
            else if (typeof(ExpressSelectType).IsAssignableFrom(propType))
            // a select type get the type of the actual value
            {
                if (propVal.GetType().IsValueType) //we have a value type, so write out explicitly
                    return string.Format("{0}({1})", propVal.GetType().Name.ToUpper(),
                                         ConvertPropertyToPart21String(propVal.GetType(), propVal,
                                                                       entity));
                else //could be anything so re-evaluate actual type
                    return ConvertPropertyToPart21String(propVal.GetType(), propVal, entity);
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

        public void Close()
        {
            if (_output != null)
            {
                _output.Flush();
                _output.Close();
            }
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

        public void WriteHeader(IModel model)
        {
            StringBuilder header = new StringBuilder();
            header.AppendLine("ISO-10303-21;");
            header.AppendLine("HEADER;");
            //FILE_DESCRIPTION
            header.Append("FILE_DESCRIPTION ((");
            int i = 0;

            if (model.Header.FileDescription.Description.Count == 0)
            {
                header.Append(@"''");
            }
            else
            {
                foreach (string item in model.Header.FileDescription.Description)
                {
                    header.AppendFormat(@"{0}'{1}'", i == 0 ? "" : ",", item);
                    i++;
                }
            }
            header.AppendFormat(@"), '{0}');", model.Header.FileDescription.ImplementationLevel);
            header.AppendLine();
            //FileName
            header.Append("FILE_NAME (");
            header.AppendFormat(@"'{0}'", model.Header.FileName.Name);
            header.AppendFormat(@", '{0}'", model.Header.FileName.TimeStamp);
            header.Append(", (");
            i = 0;
            if (model.Header.FileName.AuthorName.Count == 0)
                header.Append(@"''");
            else
            {
                foreach (string item in model.Header.FileName.AuthorName)
                {
                    header.AppendFormat(@"{0}'{1}'", i == 0 ? "" : ",", item);
                    i++;
                }
            }
            header.Append("), (");
            i = 0;
            if (model.Header.FileName.Organization.Count == 0)
                header.Append(@"''");
            else
            {
                foreach (string item in model.Header.FileName.Organization)
                {
                    header.AppendFormat(@"{0}'{1}'", i == 0 ? "" : ",", item);
                    i++;
                }
            }
            header.AppendFormat(@"), '{0}', '{1}', '{2}');", model.Header.FileName.PreprocessorVersion, model.Header.FileName.OriginatingSystem,
                                model.Header.FileName.AuthorizationName);
            header.AppendLine();
            //FileSchema
            header.AppendFormat("FILE_SCHEMA (('{0}'));", model.Header.FileSchema.Schemas.FirstOrDefault());
            header.AppendLine();
            header.AppendLine("ENDSEC;");
            header.AppendLine("DATA;");
            _output.Write(header.ToString());
        }

        public void WriteFooter()
        {
            _output.WriteLine("ENDSEC;");
            _output.WriteLine("END-ISO-10303-21;");
        }

        //public bool Save(XbimMemoryModel model, string fileName, XbimFileType fileType)
        //{
        //    switch (fileType)
        //    {
        //        case XbimFileType.Xbim:
        //            try
        //            {
        //                BinaryFormatter formatter = new BinaryFormatter();
        //                formatter.AssemblyFormat = FormatterAssemblyStyle.Simple;
        //                model.Header.FileDescription.EntityCount = model.Instances.Count();
        //                Stream stream = new FileStream(fileName, FileMode.Create, FileAccess.Write, FileShare.None);
        //                formatter.Serialize(stream, this);
        //                formatter.Serialize(stream, model);
        //                stream.Close();
        //                return true;
        //            }
        //            catch (Exception)
        //            {
        //                return false;
        //            }
        //        case XbimFileType.Ifc:
        //            try
        //            {
        //                _written = new HashSet<long>();
        //                _output = new StreamWriter(fileName);
        //                WriteHeader(model);
        //                foreach (IPersistIfcEntity item in model.Instances)
        //                {
        //                    Write(item);
        //                }
        //                WriteFooter();
        //                Close();
        //                return true;
        //            }
        //            catch (Exception)
        //            {
        //                return false;
        //            }

        //        default:
        //            return false;
        //    }
        //}

        #region IDisposable Members

        public void Dispose()
        {
            if (_output != null) _output.Close();
        }

        #endregion
    }
}