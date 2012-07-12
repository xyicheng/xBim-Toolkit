using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.SelectTypes;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.MaterialPropertyResource;
using Xbim.XbimExtensions;
using Xbim.Ifc.MaterialResource;
using Xbim.Ifc.Extensions;
using Xbim.Ifc.PropertyResource;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.DOM.PropertiesQuantities
{
    public class XbimSingleMaterialProperties :IXbimSingleProperties
    {
        IModel _model;
        IfcMaterial _material;

        internal XbimSingleMaterialProperties(IfcMaterial material, IModel model)
        {
            _model = model;
            _material = material;
        }

        public void SetProperty_double(string propertySetName, string propertyName, double? value)
        {
            if (value == null) { _material.DeleteExtendedSingleValue(_model, propertySetName, propertyName); return; }
            IfcReal val = value ?? 0;
            _material.SetExtendedSingleValue(_model, propertySetName, propertyName, val);
        }
        public void SetProperty_long(string propertySetName, string propertyName, long? value)
        {
            if (value == null) { _material.DeleteExtendedSingleValue(_model, propertySetName, propertyName); return; }
            IfcInteger val = value ?? 0;
            _material.SetExtendedSingleValue(_model, propertySetName, propertyName, val);
        }
        public void SetProperty_bool(string propertySetName, string propertyName, bool? value)
        {
            if (value == null) { _material.DeleteExtendedSingleValue(_model, propertySetName, propertyName); return; }
            IfcBoolean val = value ?? false;
            _material.SetExtendedSingleValue(_model, propertySetName, propertyName, val);
        }
        public void SetProperty_string(string propertySetName, string propertyName, string value)
        {
            if (value == null) { _material.DeleteExtendedSingleValue(_model, propertySetName, propertyName); return; }
            IfcLabel val = value;
            _material.SetExtendedSingleValue(_model, propertySetName, propertyName, val);
        }

        public double? GetProperty_double(string propertySetName, string propertyName)
        {
            IfcSimpleValue value = _material.GetExtendedSingleValueValue(_model, propertySetName, propertyName) as IfcSimpleValue;
            if (value == null) return null;
            IfcReal val = (IfcReal)value;
            return val;
        }
        public long? GetProperty_long(string propertySetName, string propertyName)
        {
            IfcSimpleValue value = _material.GetExtendedSingleValueValue(_model, propertySetName, propertyName) as IfcSimpleValue;
            if (value == null) return null;
            IfcInteger val = (IfcInteger)value;
            return (long)val.Value;
        }
        public string GetProperty_string(string propertySetName, string propertyName)
        {
            IfcSimpleValue value = _material.GetExtendedSingleValueValue(_model, propertySetName, propertyName) as IfcSimpleValue;
            if (value == null) return null;
            IfcLabel val = (IfcLabel)value;
            return val;
        }
        public bool? GetProperty_bool(string propertySetName, string propertyName)
        {
            IfcSimpleValue value = _material.GetExtendedSingleValueValue(_model, propertySetName, propertyName) as IfcSimpleValue;
            if (value == null) return null;
            IfcBoolean val = (IfcBoolean)value;
            return val;
        }

        //public object GetProperty(string propertySetName, string propertyName)
        //{
        //    IfcSimpleValue simpleValue = _material.GetExtendedSingleValueValue(_model, propertySetName, propertyName) as IfcSimpleValue;
        //    if (simpleValue == null) return null;
        //    if (simpleValue is IfcInteger) return (int)((IfcInteger)simpleValue);
        //    if (simpleValue is IfcReal) return (double)((IfcReal)simpleValue);
        //    if (simpleValue is IfcBoolean) return (bool)((IfcBoolean)simpleValue);
        //    if (simpleValue is IfcIdentifier) return (string)((IfcIdentifier)simpleValue);
        //    if (simpleValue is IfcText) return (string)((IfcText)simpleValue);
        //    if (simpleValue is IfcLabel) return (string)((IfcLabel)simpleValue);
        //    if (simpleValue is IfcLogical) return (bool)((IfcLogical)simpleValue);
        //    return simpleValue;
        //}

        //public List<string> GetAllPropertySets()
        //{
        //    List<string> result = new List<string>();
        //    foreach (IfcExtendedMaterialProperties props in _material.GetAllExtendedPropertySets(_model))
        //    {
        //        result.Add(props.Name);
        //    }
        //    return result;
        //}

        //public List<string> GetAllPropertiesInSet(string propertySet)
        //{
        //    List<string> result = new List<string>();
        //    IfcExtendedMaterialProperties properties = _material.GetExtendedProperties(_model, propertySet);
        //    foreach (IfcProperty property in properties.ExtendedProperties)
        //    {
        //        result.Add(property.Name);
        //    }
        //    return result;
        //}


        public XbimSimplePropertySets PropertySets { get { return new XbimSimplePropertySets(_material); } }

        public IEnumerable<XbimPropertySingleValue> FlatProperties
        {
            get
            {
                foreach (XbimSimplePropertySet pSet in PropertySets)
                {
                    foreach (XbimPropertySingleValue prop in pSet)
                    {
                        yield return prop;
                    }
                }
            }
        }

        IEnumerable<IBimPropertySingleValue> IBimSingleProperties.FlatProperties
        {
            get { return FlatProperties.Cast<IBimPropertySingleValue>(); }
        }


        public void SetProperty(IBimPropertySingleValue property)
        {
            string name = property.Name;
            string description = property.Description;
            string pSetName = property.PsetName;
            object value = property.Value;

            switch (property.Type)
            {
                case XbimValueTypeEnum.INTEGER:
                    SetProperty_long(pSetName, name, value as long?);
                    break;
                case XbimValueTypeEnum.REAL:
                    SetProperty_double(pSetName, name, value as double?);
                    break;
                case XbimValueTypeEnum.BOOLEAN:
                    SetProperty_bool(pSetName, name, value as bool?);
                    break;
                case XbimValueTypeEnum.STRING:
                    SetProperty_string(pSetName, name, value as string);
                    break;
                default:
                    break;
            }
        }
    }
}
