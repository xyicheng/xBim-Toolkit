using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Kernel;
using System.Diagnostics;

using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.MaterialResource;
using System.ComponentModel;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.MaterialPropertyResource;
using System.Globalization;
using Xbim.XbimExtensions.Interfaces;
using Xbim.XbimExtensions.SelectTypes;

namespace Xbim.DOM.PropertiesQuantities
{
    public class XbimSimplePropertySet : List<XbimPropertySingleValue>
    {
        private IfcPropertySet _pSet;
        private IfcExtendedMaterialProperties _matPset;
        public string Name { get { return _pSet != null ? _pSet.Name : _matPset.Name; } set { if (_pSet != null)_pSet.Name = value; else _matPset.Name = value; } }
        public string Description { get { return _pSet != null ? _pSet.Description : _matPset.Description; } set { if (_pSet != null)_pSet.Description = value; else _matPset.Description = value; } }

        internal XbimSimplePropertySet(IfcPropertySet pSet)
        {
            _pSet = pSet;
            foreach (IfcProperty prop in pSet.HasProperties)
            {
                IfcPropertySingleValue property = prop as IfcPropertySingleValue;
                if (property != null)
                {
                    this.Add(new XbimPropertySingleValue(property, Name));
                }
            }
        }
        internal XbimSimplePropertySet(IfcExtendedMaterialProperties pSet)
        {
            _matPset = pSet;
            foreach (IfcProperty prop in pSet.ExtendedProperties)
            {
                IfcPropertySingleValue property = prop as IfcPropertySingleValue;
                if (property != null)
                {
                    this.Add(new XbimPropertySingleValue(property, Name));
                }
            }
        }

        /// <summary>
        /// Creates new simple single value property for object of type XbimMaterial, XbimBuildingElement or XbimBuildingElementType
        /// </summary>
        /// <param name="name">Property name</param>
        /// <param name="value">Value of the property (could be [long|double|string|bool])</param>
        /// <param name="type">Enumeration of type</param>
        /// <returns></returns>
        public XbimPropertySingleValue NewProperty(string propertyName, object value, XbimValueTypeEnum type)
        {
            IModel model = _matPset == null?(_pSet as IPersistIfcEntity).ModelOf :(_matPset as IPersistIfcEntity).ModelOf;
            IfcPropertySingleValue prop = model.New<IfcPropertySingleValue>(p => p.Name = propertyName);
            
            if (_pSet.HasProperties != null) _pSet.HasProperties.Add_Reversible(prop);
            else _matPset.ExtendedProperties.Add_Reversible(prop);

            XbimPropertySingleValue result = new XbimPropertySingleValue(prop, Name);
            result.Type = type;
            result.Value = value;
            Add(result);
            return result;
        }

        //internal void initialize(object obj, Dictionary<IfcIdentifier, IfcValue> data)
        //{
        //    foreach (KeyValuePair<IfcIdentifier, IfcValue> pair in data)
        //    {
        //        string pName = pair.Key;
        //        IfcValue val = pair.Value;
        //        XbimValueTypeEnum type = XbimValueTypeEnum.REAL;

        //        XbimPropertySingleValue property = null;

        //        if (val is IfcSimpleValue)
        //        {
        //            if (val is IfcInteger) { type = XbimValueTypeEnum.INTEGER; long value = (IfcInteger)(val as IfcSimpleValue); property = new XbimPropertySingleValue(obj, _pSetName, pName, value, type); }
        //            if (val is IfcReal) { type = XbimValueTypeEnum.REAL; double value = (IfcReal)(val as IfcSimpleValue); property = new XbimPropertySingleValue(obj, _pSetName, pName, value, type); }
        //            if (val is IfcBoolean) { type = XbimValueTypeEnum.BOOLEAN; bool value = (IfcBoolean)(val as IfcSimpleValue); property = new XbimPropertySingleValue(obj, _pSetName, pName, value, type); }
        //            if (val is IfcIdentifier) { type = XbimValueTypeEnum.STRING; string value = (IfcIdentifier)(val as IfcSimpleValue); property = new XbimPropertySingleValue(obj, _pSetName, pName, value, type); }
        //            if (val is IfcText) { type = XbimValueTypeEnum.STRING; string value = (IfcText)(val as IfcSimpleValue); property = new XbimPropertySingleValue(obj, _pSetName, pName, value, type); }
        //            if (val is IfcLabel) { type = XbimValueTypeEnum.STRING; string value = (IfcLabel)(val as IfcSimpleValue); property = new XbimPropertySingleValue(obj, _pSetName, pName, value, type); }
        //            if (val is IfcLogical) { type = XbimValueTypeEnum.BOOLEAN; bool value = ((bool?)((IfcLogical)(val as IfcSimpleValue)).Value) ?? false; property = new XbimPropertySingleValue(obj, _pSetName, pName, value, type); }
        //        }
        //        if (val is IfcMeasureValue)
        //        {
        //            type = XbimValueTypeEnum.REAL;
        //            double value = (double)(val as IfcMeasureValue).Value;
        //            property = new XbimPropertySingleValue(obj, _pSetName, pName, value, type);
        //        }
        //        if (val is IfcDerivedMeasureValue)
        //        {
        //            Debug.WriteLine("XbimPropertySet: Not Supported type");
        //        }
        //        if (property != null) Add(property);
        //    }
        //}
    }

    public class XbimSimplePropertySets : List<XbimSimplePropertySet>
    {
        IfcTypeObject _ifcTypeObject;
        IfcObject _ifcObject;
        IfcMaterial _material;

        internal XbimSimplePropertySets(IfcTypeObject ifcTypeObject)
        {
            _ifcTypeObject = ifcTypeObject;
            foreach (IfcPropertySet pSet in ifcTypeObject.GetAllPropertySets())
            {
                Add(new XbimSimplePropertySet(pSet));
            }
        }
        internal XbimSimplePropertySets(IfcObject ifcObject)
        {
            _ifcObject = ifcObject;
            foreach (IfcPropertySet pSet in ifcObject.GetAllPropertySets())
            {
                Add(new XbimSimplePropertySet(pSet));
            }
        }
        internal XbimSimplePropertySets(IfcMaterial material)
        {
            _material = material;
            foreach (IfcExtendedMaterialProperties prop in material.GetAllPropertySets())
            {
                Add(new XbimSimplePropertySet(prop));
            }
        }

        public XbimSimplePropertySet GetPropertySet(string name)
        {
            return this.Where(set => set.Name == name).FirstOrDefault();
        }

        public XbimSimplePropertySet NewPropertySet(string name)
        {
            IModel model = null;
            XbimSimplePropertySet result = null;

            if (_ifcObject != null)
            {
                model = (_ifcObject as IPersistIfcEntity).ModelOf;
                IfcPropertySet pset = model.New<IfcPropertySet>(p => p.Name = name);
                IfcRelDefinesByProperties rel = model.New<IfcRelDefinesByProperties>();

                rel.RelatingPropertyDefinition = pset;
                rel.RelatedObjects.Add_Reversible(_ifcObject);

                result = new XbimSimplePropertySet(pset);
                Add(result);
            } 
            else if (_ifcTypeObject != null)
            {
                model = (_ifcTypeObject as IPersistIfcEntity).ModelOf;
                IfcPropertySet pset = model.New<IfcPropertySet>(p => p.Name = name);

                if (_ifcTypeObject.HasPropertySets == null) _ifcTypeObject.CreateHasPropertySets();
                _ifcTypeObject.HasPropertySets.Add_Reversible(pset);

                result = new XbimSimplePropertySet(pset);
                Add(result);
            }
            else if (_material != null)
            {
                model = (_material as IPersistIfcEntity).ModelOf;
                IfcExtendedMaterialProperties pset = model.New<IfcExtendedMaterialProperties>(p => p.Name = name);
                pset.Material = _material;

                result = new XbimSimplePropertySet(pset);
                Add(result);
            }
                return result;
        }

        //todo: implement creating and adding and deleting of the property sets and properties
    }

    public class XbimPropertySingleValue : INotifyPropertyChanged, Xbim.DOM.IBimPropertySingleValue
    {
        private XbimValueTypeEnum _type;
        private string _pSetName;
        internal IfcPropertySingleValue _property;
        private IModel _model;

        public IfcPropertySingleValue IfcPropertySingleValue { get { return _property; } }
        public string Name { get { return _property.Name; } set { _property.Name = value; } }
        public string Description { get { return _property.Description; } set { _property.Description = value; } } //todo: corect this
        public string PsetName { get { return _pSetName; } }
        public XbimValueTypeEnum Type { get { return _type; } set { _type = value; } }
        public object Value
        {
            get 
            {
                if (_property.NominalValue == null) return null;
                return _property.NominalValue.Value; 
            }
            set
            {
                if (value == null)
                {
                    _property.NominalValue = null;
                    return;
                }
                if (!(value is double || value is long || value is bool || value is string || value is float || value is int))
                {
#if DEBUG
                    throw new Exception("Not suppported data type.");
#else
                    return;
#endif
                }
                //if input is from forms, it is going to be a string. So lets try to parse it :-)
                if (value is string)
                {
                    if (_property.NominalValue is IfcInteger) value = int.Parse(value as string);
                    if (_property.NominalValue is IfcReal) value = double.Parse(value as string, CultureInfo.InvariantCulture);
                    if (_property.NominalValue is IfcBoolean) value = bool.Parse(value as string);
                    if (_property.NominalValue is IfcLogical) value = bool.Parse(value as string); 
                }

                //cast object acording to the data type of the target
                if (_property.NominalValue is IfcSimpleValue)
                {
                    if (_property.NominalValue is IfcInteger) { _property.NominalValue = (IfcInteger)(int)value; }
                    if (_property.NominalValue is IfcReal) { _property.NominalValue = (IfcReal)(double)value; }
                    if (_property.NominalValue is IfcBoolean) { _property.NominalValue = (IfcBoolean)(bool)value; }
                    if (_property.NominalValue is IfcIdentifier) { _property.NominalValue = (IfcIdentifier)(string)value; }
                    if (_property.NominalValue is IfcText) { _property.NominalValue = (IfcText)(string)value; }
                    if (_property.NominalValue is IfcLabel) { _property.NominalValue = (IfcLabel)(string)value; }
                    if (_property.NominalValue is IfcLogical) { _property.NominalValue = (IfcLogical)(bool)value; }
                    NotifyPropertyChanged("Value");
                }
                else if (_property.NominalValue == null)
                {
                    switch (Type)
                    {
                        case XbimValueTypeEnum.INTEGER: _property.NominalValue = (IfcInteger)(int)value;
                            break;
                        case XbimValueTypeEnum.REAL: _property.NominalValue = (IfcReal)(double)value;
                            break;
                        case XbimValueTypeEnum.BOOLEAN: _property.NominalValue = (IfcBoolean)(bool)value;
                            break;
                        case XbimValueTypeEnum.STRING: _property.NominalValue = (IfcLabel)(string)value;
                            break;
                        default:
                            break;
                    }
                }
                else
                {
#if DEBUG
                    throw new Exception("Not suppported data type.");
#else
                    return;
#endif
                }
                
            }
        }

//        private bool SetValue(object newValue, IfcPropertySingleValue oldValue)
//        {
//            if (newValue == null) return false;
//            bool changed = false;

//            string value = newValue as string;  //handling input as string to be able to handle directly data from forms
//            if (value == null) 
//                value = newValue.ToString();
//            if (value == null)
//            {
//#if Debug
//                throw new Exception("Setting of calue is prepared just for string input. It is parsed after that;");
//#endif
//                return false;
//            }

//            //there is real danger of wrong input format of the data

//            try
//            {
//                if (oldValue.NominalValue is IfcSimpleValue)
//                {
//                    if (oldValue.NominalValue is IfcInteger) { oldValue.NominalValue = (IfcInteger)(long.Parse(value)); changed = true; }
//                    if (oldValue.NominalValue is IfcReal) { oldValue.NominalValue = (IfcReal)(double.Parse(value)); changed = true; }
//                    if (oldValue.NominalValue is IfcBoolean) { oldValue.NominalValue = (IfcBoolean)(bool.Parse(value)); changed = true; }
//                    if (oldValue.NominalValue is IfcIdentifier) { oldValue.NominalValue = (IfcIdentifier)value; changed = true; }
//                    if (oldValue.NominalValue is IfcText) { oldValue.NominalValue = (IfcText)value; changed = true; }
//                    if (oldValue.NominalValue is IfcLabel) { oldValue.NominalValue = (IfcLabel)value; changed = true; }
//                    if (oldValue.NominalValue is IfcLogical) { oldValue.NominalValue = (IfcLogical)(bool.Parse(value)); changed = true; }
//                }
//                if (oldValue.NominalValue is IfcMeasureValue)
//                {
//#if Debug
//                throw new Exception("Not supported for writing property values");
//#endif
//                }
//                if (oldValue.NominalValue is IfcDerivedMeasureValue)
//                {
//#if Debug
//                throw new Exception("Not supported for writing property values");
//#endif
//                }


//                if (changed)
//                {
//                    NotifyPropertyChanged("Value");
//                    return true;
//                }
//            }
//            catch (Exception e)
//            {
//#if Debug
//                throw new Exception("Wrong input format of the data");
//#endif
//            }

//            return false;
//        }

        internal XbimPropertySingleValue(IfcPropertySingleValue property, string propertySetName)
        {
            if (string.IsNullOrEmpty(propertySetName) || property == null) throw new ArgumentNullException();
            _property = property;
            _pSetName = propertySetName;

            _type = XbimValueTypeEnum.STRING;
            ExpressType exp = property.NominalValue as ExpressType;
            if (exp != null)
            {
                Type t = exp.UnderlyingSystemType;

                if (IsInteger(t)) _type = XbimValueTypeEnum.INTEGER;
                if (IsReal(t)) _type = XbimValueTypeEnum.REAL;
                if (IsBool(t)) _type = XbimValueTypeEnum.BOOLEAN;
                if (IsString(t)) _type = XbimValueTypeEnum.STRING;
            }

            _model = (_property as IPersistIfcEntity).ModelOf;
        }

        private bool IsInteger(Type type) {
            if (type == typeof(Int16)) return true;
            if (type == typeof(UInt16)) return true;
            if (type == typeof(Int32)) return true;
            if (type == typeof(UInt32)) return true;
            if (type == typeof(Int64)) return true;
            if (type == typeof(UInt64)) return true;
            if (type == typeof(long)) return true;
            if (type == typeof(ulong)) return true;

            return false;
        }

        private bool IsReal(Type type)
        {
            if (type == typeof(float)) return true;
            if (type == typeof(double)) return true;
            if (type == typeof(Double)) return true;

            return false;
        }

        private bool IsBool(Type type)
        {
            if (type == typeof(bool)) return true;
            if (type == typeof(Boolean)) return true;
            if (type == typeof(bool?)) return true;
            if (type == typeof(Boolean?)) return true;

            return false; 
        }

        private bool IsString(Type type)
        {
            if (type == typeof(string)) return true;
            if (type == typeof(char)) return true;
            if (type == typeof(Char)) return true;
            if (type == typeof(char[])) return true;

            return false;
        }

//        public void WriteToModel(object forObject)
//        {
//            if (_existInModel) return;

//            object obj = null;
//            if (forObject is XbimMaterial) obj = (forObject as XbimMaterial).Material;
//            if (forObject is XbimBuildingElement) obj = (forObject as XbimBuildingElement).IfcBuildingElement;
//            if (forObject is XbimBuildingElementType) obj = (forObject as XbimBuildingElementType).IfcTypeProduct;

//            if (obj == null) throw new Exception("Object must be specified as XbimMaterial, XbimBuildingElement or XbimBuildingElementtype");
//            _object = obj;
//            _value = _value.ToString();

//            switch (_type)
//            {
//                case XbimValueTypeEnum.INTEGER:
//                    try
//                    {
//                        _value = long.Parse(_value as string);
//                        if (_object is IfcMaterial) (_object as IfcMaterial).SetExtendedSingleValue(_pSetName, _name, (IfcInteger)(long)_value);
//                        if (_object is IfcTypeObject) (_object as IfcTypeObject).SetPropertySingleValue(_pSetName, _name, (IfcInteger)(long)_value);
//                        if (_object is IfcObject) (_object as IfcObject).SetPropertySingleValue(_pSetName, _name, (IfcInteger)(long)_value);
//                        break;
//                    }
//                    catch (Exception e)
//                    {
//                        break;
//                    }
//                case XbimValueTypeEnum.REAL:
//                    try
//                    {
//                        _value = double.Parse(_value as string);
//                        if (_object is IfcMaterial) (_object as IfcMaterial).SetExtendedSingleValue(_pSetName, _name, (IfcReal)(double)_value);
//                        if (_object is IfcTypeObject) (_object as IfcTypeObject).SetPropertySingleValue(_pSetName, _name, (IfcReal)(double)_value);
//                        if (_object is IfcObject) (_object as IfcObject).SetPropertySingleValue(_pSetName, _name, (IfcReal)(double)_value);
//                        break;
//                    }
//                    catch (Exception e)
//                    {
//                        break;
//                    }
//                case XbimValueTypeEnum.BOOLEAN:
//                    try
//                    {
//                        _value = bool.Parse(_value as string);
//                        if (_object is IfcMaterial) (_object as IfcMaterial).SetExtendedSingleValue(_pSetName, _name, (IfcBoolean)(bool)_value);
//                        if (_object is IfcTypeObject) (_object as IfcTypeObject).SetPropertySingleValue(_pSetName, _name, (IfcBoolean)(bool)_value);
//                        if (_object is IfcObject) (_object as IfcObject).SetPropertySingleValue(_pSetName, _name, (IfcBoolean)(bool)_value);
//                        break;
//                    }
//                    catch (Exception e)
//                    {
//                        break;
//                    }
//                case XbimValueTypeEnum.STRING:
//                    if (_object is IfcMaterial) (_object as IfcMaterial).SetExtendedSingleValue(_pSetName, _name, (IfcLabel)(string)_value);
//                    if (_object is IfcTypeObject) (_object as IfcTypeObject).SetPropertySingleValue(_pSetName, _name, (IfcLabel)(string)_value);
//                    if (_object is IfcObject) (_object as IfcObject).SetPropertySingleValue(_pSetName, _name, (IfcLabel)(string)_value);
//                    break;
//                default:
//                    break;
//            }

//            IfcValue control = null;
//            if (_object is IfcMaterial) control = (_object as IfcMaterial).GetExtendedSingleValueValue(_pSetName, _name);
//            if (_object is IfcTypeObject) control = (_object as IfcTypeObject).GetPropertySingleValueValue(_pSetName, _name);
//            if (_object is IfcObject) control = (_object as IfcObject).GetPropertySingleValueValue(_pSetName, _name);

//            if (control != null) _existInModel = true;
//#if Debug
//            else throw new Exception("Property was not written into the model");
//#endif
//        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }
    }

    
}
