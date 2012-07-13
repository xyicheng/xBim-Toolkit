using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Kernel;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.SelectTypes;

namespace Xbim.DOM.PropertiesQuantities
{
    public class XbimSingleProperties : IXbimSingleProperties
    {
        private IfcObject _object;

        internal XbimSingleProperties(IfcObject ifcObject)
        {
            _object = ifcObject;
        }


        public void SetProperty_double(string propertySetName, string propertyName, double? value)
        {
            if (value == null) { _object.DeletePropertySingleValueValue(propertySetName, propertyName); return; }
            IfcReal val = value ?? 0;
            _object.SetPropertySingleValue(propertySetName, propertyName, val);
        }
        public void SetProperty_long(string propertySetName, string propertyName, long? value)
        {
            if (value == null) { _object.DeletePropertySingleValueValue(propertySetName, propertyName); return; }
            IfcInteger val = value ?? 0;
            _object.SetPropertySingleValue(propertySetName, propertyName, val);
        }
        public void SetProperty_bool(string propertySetName, string propertyName, bool? value)
        {
            if (value == null) { _object.DeletePropertySingleValueValue(propertySetName, propertyName); return; }
            IfcBoolean val = value ?? false;
            _object.SetPropertySingleValue(propertySetName, propertyName, val);
        }
        public void SetProperty_string(string propertySetName, string propertyName, string value)
        {
            if (value == null) { _object.DeletePropertySingleValueValue(propertySetName, propertyName); return; }
            IfcLabel val = value;
            _object.SetPropertySingleValue(propertySetName, propertyName, val);
        }

        public double? GetProperty_double(string propertySetName, string propertyName)
        {
            IfcValue value = _object.GetPropertySingleNominalValue(propertySetName, propertyName);
            IfcSimpleValue simpleValue = value as IfcSimpleValue;
            if (simpleValue != null)
            {
                IfcReal val = (IfcReal)simpleValue;
                return val;
            }
            IfcMeasureValue measure = value as IfcMeasureValue;
            if (measure != null)
            {
                return (double?)measure.Value;
            }
            return null;
            
        }
        public long? GetProperty_long(string propertySetName, string propertyName)
        {
            IfcSimpleValue value = _object.GetPropertySingleNominalValue(propertySetName, propertyName) as IfcSimpleValue;
            if (value == null) return null;
            IfcInteger val = (IfcInteger)value;
            return (long)val.Value;
        }
        public string GetProperty_string(string propertySetName, string propertyName)
        {
            IfcSimpleValue value = _object.GetPropertySingleNominalValue(propertySetName, propertyName) as IfcSimpleValue;
            if (value == null) return null;
            IfcLabel val = (IfcLabel)value;
            return val;
        }
        public bool? GetProperty_bool(string propertySetName, string propertyName)
        {
            IfcSimpleValue value = _object.GetPropertySingleNominalValue(propertySetName, propertyName) as IfcSimpleValue;
            if (value == null) return null;
            IfcBoolean val = (IfcBoolean)value;
            return val;
        }

        public XbimSimplePropertySets PropertySets { get { return new XbimSimplePropertySets(_object); } }


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



        public void SetProperty(IBimPropertySingleValue property)
        {
            string name = property.Name;
            string description = property.Description;
            string pSetName = property.PsetName;
            object value = property.Value;

            XbimPropertySingleValue prop = property as XbimPropertySingleValue;
            if (prop != null)
            {
                _object.SetPropertySingleValue(pSetName, name, prop._property.NominalValue).Description = description;
            }
            else
            {
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

    
}
