using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.PropertyResource;
using Xbim.Ifc.SelectTypes;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.Extensions;

namespace Xbim.DOM.PropertiesQuantities
{
    public class XbimMaterialQuantities
    {
        private IfcObject _object;
        private IfcTypeObject _typeObject;
        private XbimDocument _document;

        internal static string _pSetName = "Xbim_MaterialQuantities";
        internal static string _pVolumeName = "MaterialVolume";
        internal static string _pAreaName = "MaterialArea";

        internal XbimMaterialQuantities(IfcObject ifcObject, XbimDocument document)
        {
            if (ifcObject == null || document == null) throw new ArgumentNullException();
            _object = ifcObject;
            _typeObject = null;
            _document = document;
        }

        internal XbimMaterialQuantities(IfcTypeObject ifcTypeObject, XbimDocument document)
        {
            if (ifcTypeObject == null || document == null) throw new ArgumentNullException();
            _typeObject = ifcTypeObject;
            _object = null;
            _document = document;
        }

        public Dictionary<XbimMaterial, double> MaterialsVolume //IfcThermalTransmittanceMeasure
        {
            get
            {
                IfcPropertyTableValue table = GetTable(_pSetName, _pVolumeName);
                Dictionary<XbimMaterial, double> result = new Dictionary<XbimMaterial, double>();
                if (table == null) return result;
                Dictionary<IfcValue, IfcValue> tableDict = table.GetAsDictionary();

                foreach (KeyValuePair<IfcValue, IfcValue> row in tableDict)
                {
                    string materialName = (IfcLabel)row.Key;
                    XbimMaterial material = _document.Materials[materialName];
                    double volume = (IfcVolumeMeasure)row.Value;
                    result.Add(material, volume);
                }
                return result;
            }
        }

        public Dictionary<XbimMaterial, double> MaterialsArea //IfcThermalTransmittanceMeasure
        {
            get
            {
                IfcPropertyTableValue table = GetTable(_pSetName, _pAreaName);
                Dictionary<XbimMaterial, double> result = new Dictionary<XbimMaterial, double>();
                if (table == null) return result;
                Dictionary<IfcValue, IfcValue> tableDict = table.GetAsDictionary();

                foreach (KeyValuePair<IfcValue, IfcValue> row in tableDict)
                {
                    string materialName = (IfcLabel)row.Key;
                    XbimMaterial material = _document.Materials[materialName];
                    double volume = (IfcVolumeMeasure)row.Value;
                    result.Add(material, volume);
                }
                return result;
            }
        }

        public IList<XbimMaterial> Materials
        {
            get
            {
                IfcPropertyTableValue table = GetTable(_pSetName, _pAreaName);
                List<XbimMaterial> result = new List<XbimMaterial>();
                if (table == null) return result;
                Dictionary<IfcValue, IfcValue> tableDict = table.GetAsDictionary();

                foreach (KeyValuePair<IfcValue, IfcValue> row in tableDict)
                {
                    string materialName = (IfcLabel)row.Key;
                    XbimMaterial material = _document.Materials[materialName];
                    result.Add(material);
                }
                return result;
            }
        }

        private IfcPropertyTableValue GetTable(string pSetName, string propertyName)
        {
            return _object != null ? _object.GetPropertyTableValue(pSetName, propertyName) : _typeObject.GetPropertyTableValue(pSetName, propertyName);
        }

        public void SetMaterialVolume(XbimMaterial material, double volume)
        {
            IfcVolumeMeasure volumeMeasure = volume;
            IfcLabel materialName = material.Name;

            if (_object != null)
                _object.SetPropertyTableItemValue(_pSetName, _pVolumeName, materialName, volumeMeasure);
            else
                _typeObject.SetPropertyTableItemValue(_pSetName, _pVolumeName, materialName, volumeMeasure);

        }

        public void SetMaterialVolume(string material, double volume)
        {
            //check if the material exists in the document
            if (!_document.Materials.Contains(material)) throw new Exception(string.Format("Material \"{0}\" does not exist in the document.", material));
            XbimMaterial xbimMaterial = _document.Materials[material];

            SetMaterialVolume(xbimMaterial, volume);
        }

        public void SetMaterialArea(XbimMaterial material, double area)
        {
            IfcAreaMeasure volumeMeasure = area;
            IfcLabel materialName = material.Name;

            if (_object != null)
                _object.SetPropertyTableItemValue(_pSetName, _pAreaName, materialName, volumeMeasure);
            else
                _typeObject.SetPropertyTableItemValue(_pSetName, _pAreaName, materialName, volumeMeasure);
        }

        public void SetMaterialArea(string material, double area)
        {
            //check if the material exists in the document
            if (!_document.Materials.Contains(material)) throw new Exception(string.Format("Material \"{0}\" does not exist in the document.", material));
            XbimMaterial xbimMaterial = _document.Materials[material];

            SetMaterialArea(xbimMaterial, area);
        }

        public double? GetMaterialVolume(string material)
        {
            //check if the material exists in the document
            if (!_document.Materials.Contains(material)) throw new Exception(string.Format("Material \"{0}\" does not exist in the document.", material));
            XbimMaterial xbimMaterial = _document.Materials[material];

            return GetMaterialVolume(xbimMaterial);
        }

        public double? GetMaterialVolume(XbimMaterial material)
        {
            IfcLabel materialName = material.Name;
            IfcValue value = _object != null ? _object.GetPropertyTableItemValue(_pSetName, _pVolumeName, materialName) : _typeObject.GetPropertyTableItemValue(_pSetName, _pVolumeName, materialName);
            if (value == null) return null;

            IfcVolumeMeasure volumeMeasure = (IfcVolumeMeasure)value;
            return (double)volumeMeasure;
        }

        public double? GetMaterialArea(string material)
        {
            //check if the material exists in the document
            if (!_document.Materials.Contains(material)) throw new Exception(string.Format("Material \"{0}\" does not exist in the document.", material));
            XbimMaterial xbimMaterial = _document.Materials[material];

            return GetMaterialArea(xbimMaterial);
        }

        public double? GetMaterialArea(XbimMaterial material)
        {
            IfcLabel materialName = material.Name;
            IfcValue value = _object != null ? _object.GetPropertyTableItemValue(_pSetName, _pAreaName, materialName) : _typeObject.GetPropertyTableItemValue(_pSetName, _pAreaName, materialName);
            if (value == null) return null;

            IfcAreaMeasure volumeMeasure = (IfcAreaMeasure)value;
            return (double)volumeMeasure;
        }

        public List<string> GetVolumeMaterialNames()
        {
            List<string> result = new List<string>();
            IfcPropertyTableValue table = _object.GetPropertyTableValue(_pSetName, _pVolumeName);
            if (table == null) return result;
            if (table.DefiningValues == null) return result;
            foreach (IfcValue val in table.DefiningValues)
            {
                IfcSimpleValue simVal = val as IfcSimpleValue;
                if (simVal is IfcLabel) result.Add((string)(IfcLabel)simVal);
            }
            return result;
        }
    }
}
