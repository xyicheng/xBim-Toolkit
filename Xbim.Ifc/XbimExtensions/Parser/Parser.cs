#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    Parser.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using QUT.Gppg;
using System.Globalization;

#endregion

namespace Xbim.XbimExtensions.Parser
{
    public sealed partial class Scanner : ScanBase
    {
        private IndentedTextWriter _errorLog;

        internal IndentedTextWriter ErrorLog
        {
            set { _errorLog = value; }
        }

        public override void yyerror(string format, params object[] args)
        {
            string errmsg = string.Format(format, args);
            if (_errorLog != null)
                _errorLog.WriteLine(string.Format("Illegal character found at line {0}, column {1}\n{2}", this.yyline,
                                                  this.yycol, errmsg));
            else
                throw new Exception(string.Format("Illegal character found at line {0}, column {1}\n{2}", this.yyline,
                                                  this.yycol, errmsg));
        }
    }


    // Declare a delegate type for processing a P21 value:
    public delegate void ReportProgressDelegate(int percentProgress, object userState);

    public delegate IPersistIfc CreateEntityEventHandler(string className, long? label, bool headerEntity, out int[] i);

    public delegate long EntityStoreHandler(IPersistIfcEntity ent);

    //public delegate void EntitySelectChangedHandler(StepP21Entity entity);

    public delegate void ParameterSetter(int propIndex, IPropertyValue value);

    public enum IfcParserType
    {
        Boolean,
        Enum,
        Entity,
        HexaDecimal,
        Integer,
        Real,
        String,
        Undefined
    }

    public struct PropertyValue : IPropertyValue
    {
        private string _strVal;
        private IfcParserType _ifcParserType;


        private object _entityVal;

        public static readonly Regex SpecialCharRegEx;
        public static readonly MatchEvaluator SpecialCharEvaluator;

        static PropertyValue()
        {
            SpecialCharEvaluator = ConvertFromHex;
            SpecialCharRegEx = new Regex(@"\\X\\([0-9A-F][0-9A-F])");
        }

        private static string ConvertFromHex(Match m)
        {
            // Convert the number expressed in base-16 to an integer.
            int value = Convert.ToInt32(m.Groups[1].Value, 16);
            // Get the character corresponding to the integral value.
            return Char.ConvertFromUtf32(value);
        }

        internal void Init(string value, IfcParserType type)
        {
            _strVal = value;
            _ifcParserType = type;
        }

        internal void Init(object value)
        {
            _entityVal = value;
            _ifcParserType = IfcParserType.Entity;
        }

        public IfcParserType Type
        {
            get { return _ifcParserType; }
        }

        public bool BooleanVal
        {
            get
            {
                if (_ifcParserType == IfcParserType.Boolean) return _strVal == ".T.";
                else
                    throw new Exception(string.Format("Wrong parameter type, found {0}, expected {1}",
                                                      _ifcParserType.ToString(), "Boolean"));
            }
        }

        public string EnumVal
        {
            get
            {
                if (_ifcParserType == IfcParserType.Enum) return _strVal;
                else
                    throw new Exception(string.Format("Wrong parameter type, found {0}, expected {1}",
                                                      _ifcParserType.ToString(), "Enum"));
            }
        }

        public object EntityVal
        {
            get
            {
                if (_ifcParserType == IfcParserType.Entity) return _entityVal;
                else
                    throw new Exception(string.Format("Wrong parameter type, found {0}, expected {1}",
                                                      _ifcParserType.ToString(), "Entity"));
            }
        }

        public long HexadecimalVal
        {
            get
            {
                if (_ifcParserType == IfcParserType.HexaDecimal) return Convert.ToInt64(_strVal, 16);
                else
                    throw new Exception(string.Format("Wrong parameter type, found {0}, expected {1}",
                                                      _ifcParserType.ToString(), "HexaDecimal"));
            }
        }

        public long IntegerVal
        {
            get
            {
                if (_ifcParserType == IfcParserType.Integer) return Convert.ToInt64(_strVal, CultureInfo.InvariantCulture);
                else
                    throw new Exception(string.Format("Wrong parameter type, found {0}, expected {1}",
                                                      _ifcParserType.ToString(), "Integer"));
            }
        }

        /// <summary>
        ///   Returns a double if the type parsed is any kind of number
        /// </summary>
        public double NumberVal
        {
            get
            {
                if (_ifcParserType == IfcParserType.Integer
                    || _ifcParserType == IfcParserType.Real) return Convert.ToDouble(_strVal, CultureInfo.InvariantCulture);
                else if (_ifcParserType == IfcParserType.HexaDecimal)
                    return Convert.ToDouble(Convert.ToInt64(_strVal, 16));
                else
                    throw new Exception(string.Format("Wrong parameter type, found {0}, expected {1}",
                                                      _ifcParserType.ToString(), "Number"));
            }
        }

        public double RealVal
        {
            get
            {
                if (_ifcParserType == IfcParserType.Real || _ifcParserType == IfcParserType.Integer)
                {
                    if (_ifcParserType != IfcParserType.Real)
                        Debug.WriteLine(
                            "A Real parameter has been illegally written into the Ifc File as an Integer, it has been converted to a Real without loss of data");
                    return Convert.ToDouble(_strVal, CultureInfo.InvariantCulture);
                }
                else
                    throw new Exception(string.Format("Wrong parameter type, found {0}, expected {1}",
                                                      _ifcParserType.ToString(), "Real"));
            }
        }

        public string StringVal
        {
            get
            {
                string trimmed = _strVal.Substring(1, _strVal.Length - 2); //remove the quotes

                string res = SpecialCharRegEx.Replace(trimmed, SpecialCharEvaluator);
                res = res.Replace("\'\'", "\'");
                if (_ifcParserType == IfcParserType.String)
                    return res;
                else
                    throw new Exception(string.Format("Wrong parameter type, found {0}, expected {1}",
                                                      _ifcParserType.ToString(), "String"));
            }
        }
    }


    public class Part21Entity
    {
        public Part21Entity(string label)
        {
            EntityLabel = Convert.ToInt64(label.TrimStart('#'));
        }

        public Part21Entity(string label, IPersistIfc ent)
            : this(Convert.ToInt64(label.TrimStart('#')), ent)
        {
        }

        public Part21Entity(IPersistIfc ent)
            : this(-1, ent)
        {
        }

        public Part21Entity(long label, IPersistIfc ent)
        {
            EntityLabel = label;
            Entity = ent;
        }

        private IPersistIfc _entity;

        public IPersistIfc Entity
        {
            get { return _entity; }
            set { _entity = value; }
        }

        public long EntityLabel;


        public int CurrentParamIndex = -1;
        public int[] RequiredParameters;

        public void SetPropertyValue(IPropertyValue pValue)
        {
            //Type itemType;
            //PropertyInfo pInfo = IfcType.IfcProperties[CurrentParamIndex + 1].PropertyInfo;
            //Type realType = pInfo.PropertyType;
            //if (typeof(ExpressEnumerable).IsAssignableFrom(realType) && (itemType = GetItemTypeFromGenericType(realType)) != null)
            //{
            //    if (itemType.IsPrimitive) //double, int etc
            //    {
            //        Debug.WriteLine("ListItem Primitive " + itemType.Name);
            //    }
            //    else if (itemType.IsValueType) //IfcLengthMeasure etc
            //    {
            //        Debug.WriteLine("ListItem ValueType " + itemType.Name);
            //    }
            //    else if (itemType.IsEnum) //Enumeration
            //    {
            //        Debug.WriteLine("ListItem Enum " + itemType.Name);
            //    }
            //    else if (itemType.IsClass) //entity
            //    {
            //        Debug.WriteLine("ListItem Class " + itemType.Name);
            //    }
            //    else
            //        throw new Exception("Undefined item type encountered");
            //    return;
            //    try
            //    {
            //        switch (pValue.Type)
            //        {
            //            case IfcParserType.Boolean:
            //                _addMethod.Invoke(_entity, new object[] { Activator.CreateInstance(itemType, pValue.BooleanVal) });
            //                break;
            //            case IfcParserType.Enum:
            //                _addMethod.Invoke(_entity, new object[] { Activator.CreateInstance(itemType, pValue.EnumVal) });
            //                break;
            //            case IfcParserType.Entity:
            //                _addMethod.Invoke(_entity, new object[] { Convert.ChangeType(pValue.EntityVal, realType) });
            //                break;
            //            case IfcParserType.HexaDecimal:
            //                _addMethod.Invoke(_entity, new object[] { Activator.CreateInstance(itemType, pValue.IntegerVal) });
            //                break;
            //            case IfcParserType.Integer:
            //                _addMethod.Invoke(_entity, new object[] { Activator.CreateInstance(itemType, pValue.IntegerVal) });
            //                break;
            //            case IfcParserType.Real:
            //                _addMethod.Invoke(_entity, new object[] { Activator.CreateInstance(itemType, pValue.RealVal) });
            //                break;
            //            case IfcParserType.String:
            //                pInfo.SetValue(_entity, Activator.CreateInstance(realType, pValue.StringVal), null);
            //                break;
            //            case IfcParserType.Undefined:
            //                break;
            //            default:
            //                break;
            //        }
            //    }
            //    catch (Exception e)
            //    {

            //        throw;
            //    }
            //}
            //else
            //{
            //    if (realType.IsPrimitive) //double, int etc
            //    {
            //        Debug.WriteLine("Primitive " + realType.Name);
            //    }
            //    else if (realType.IsValueType) //IfcLengthMeasure etc
            //    {
            //        Debug.WriteLine("ValueType " + realType.Name);
            //    }
            //    else if (realType.IsEnum) //Enumeration
            //    {
            //        Debug.WriteLine("Enum " + realType.Name);
            //    }
            //    else if (realType.IsClass) //entity
            //    {
            //        Debug.WriteLine("Class " + realType.Name);
            //    }
            //    else
            //        throw new Exception("Undefined item type encountered");
            //    return;
            //    try
            //    {


            //    switch (pValue.Type)
            //    {
            //        case IfcParserType.Boolean:
            //            pInfo.SetValue(_entity, Activator.CreateInstance(realType, pValue.BooleanVal), null);
            //            break;
            //        case IfcParserType.Enum:
            //            pInfo.SetValue(_entity, Activator.CreateInstance(realType, pValue.EnumVal), null);
            //            break;
            //        case IfcParserType.Entity:
            //            pInfo.SetValue(_entity, Convert.ChangeType(pValue.EntityVal, realType), null);
            //            break;
            //        case IfcParserType.HexaDecimal:
            //            pInfo.SetValue(_entity, Activator.CreateInstance(realType, pValue.IntegerVal), null);
            //            break;
            //        case IfcParserType.Integer:
            //            pInfo.SetValue(_entity, Activator.CreateInstance(realType, pValue.IntegerVal), null);
            //            break;
            //        case IfcParserType.Real:
            //            pInfo.SetValue(_entity, Activator.CreateInstance(realType, pValue.RealVal), null);
            //            break;
            //        case IfcParserType.String:
            //            pInfo.SetValue(_entity, Activator.CreateInstance(realType,pValue.StringVal) , null);
            //            break;
            //        case IfcParserType.Undefined:
            //            break;
            //        default:
            //            break;
            //    }
            //    }
            //    catch (Exception e)
            //    {

            //        throw;
            //    }
            //}
        }


        public ParameterSetter ParameterSetter
        {
            get
            {
                if (RequiredParameters == null || RequiredParameters.Contains(CurrentParamIndex))
                    return (Entity).IfcParse;
                else
                    return ParameterEater;
            }
        }

        private void ParameterEater(int i, IPropertyValue v)
        {
        }

        private Type GetItemTypeFromGenericType(Type genericType)
        {
            if (genericType.IsGenericType || genericType.IsInterface)
            {
                Type[] genericTypes = genericType.GetGenericArguments();
                if (genericTypes.GetUpperBound(0) >= 0)
                {
                    return genericTypes[genericTypes.GetUpperBound(0)];
                }
                else
                {
                    return null;
                }
            }
            else
            {
                if (genericType.BaseType != null)
                {
                    return GetItemTypeFromGenericType(genericType.BaseType);
                }
                else
                {
                    return null;
                }
            }
        }
    }

    abstract partial class P21Parser : ShiftReduceParser<ValueType, LexLocation>
    {
        public P21Parser(Stream strm)
            : base(new Scanner(strm))
        {
        }


        internal virtual void SetErrorMessage()
        {
        }

        internal abstract void CharacterError();
        internal abstract void BeginParse();
        internal abstract void EndParse();
        internal abstract void BeginHeader();
        internal abstract void EndHeader();
        internal abstract void BeginScope();
        internal abstract void EndScope();
        internal abstract void EndSec();
        internal abstract void BeginList();
        internal abstract void EndList();
        internal abstract void BeginComplex();
        internal abstract void EndComplex();
        internal abstract void SetType(string entityTypeName);
        internal abstract void NewEntity(string entityLabel);
        internal abstract void EndEntity();
        internal abstract void EndHeaderEntity();
        internal abstract void SetIntegerValue(string value);
        internal abstract void SetHexValue(string value);
        internal abstract void SetFloatValue(string value);
        internal abstract void SetStringValue(string value);
        internal abstract void SetEnumValue(string value);
        internal abstract void SetBooleanValue(string value);
        internal abstract void SetNonDefinedValue();
        internal abstract void SetOverrideValue();
        internal abstract void SetObjectValue(string value);
        internal abstract void EndNestedType(string value);
        internal abstract void BeginNestedType(string value);
    }
}