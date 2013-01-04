using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.Ifc2x3.MaterialPropertyResource;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.DOM.PropertiesQuantities;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.DOM
{
    public class XbimMaterial : Xbim.DOM.IBimMaterial
    {
        IfcMaterial _ifcMaterial;
        XbimDocument _document;

        public IcimMaterialProperties IcimProperties { get { return new IcimMaterialProperties(_ifcMaterial, _document.Model); } }
        public IXbimSingleProperties SingleProperties { get { return new XbimSingleMaterialProperties(_ifcMaterial, _document.Model); } }

        public XbimDocument Document { get { return _document; } }

        internal IfcMaterial Material { get { return _ifcMaterial; } }

        internal XbimMaterial(XbimDocument document, string materialName)
        {
            _ifcMaterial = document.Model.Instances.New<IfcMaterial>();
            _ifcMaterial.Name = materialName;
            _document = document;
            document.Materials.Add(this);
        }

        internal XbimMaterial(XbimDocument document, IfcMaterial material)
        {
            _ifcMaterial = material;
            _document = document;
        }

        public string Name
        {
            get { return _ifcMaterial.Name; }
            set { _ifcMaterial.Name = value; }
        }

        public static implicit operator IfcMaterial (XbimMaterial xmat)
        {
            return xmat._ifcMaterial;
        }

        public double? GetAreaForElement(XbimBuildingElement buildingElement)
        {
            XbimMaterialQuantities quantities = buildingElement.MaterialQuantities;
            return quantities.GetMaterialArea(this);
        }

        public double? GetAreaForElement(XbimBuildingElementType buildingElementType)
        {
            XbimMaterialQuantities quantities = buildingElementType.MaterialQuantities;
            return quantities.GetMaterialArea(this);
        }

        public double? GetVolumeForElement(XbimBuildingElement buildingElement)
        {
            XbimMaterialQuantities quantities = buildingElement.MaterialQuantities;
            return quantities.GetMaterialVolume(this);
        }

        public double? GetVolumeForElement(XbimBuildingElementType buildingElementType)
        {
            XbimMaterialQuantities quantities = buildingElementType.MaterialQuantities;
            return quantities.GetMaterialVolume(this);
        }

        public double? iCimVolume
        {
            get
            {
                IfcLabel matName = _ifcMaterial.Name;
                IEnumerable<IfcPropertySet> pSets = _document.Model.Instances.Where<IfcPropertySet>(p => p.Name == XbimMaterialQuantities._pSetName);
                double volume = 0;
                if (pSets.Count() == 0) return null;

                foreach (IfcPropertySet pSet in pSets)
                {
                    IEnumerable<IfcPropertyTableValue> tables = pSet.HasProperties.Where<IfcPropertyTableValue>(p => p.Name == XbimMaterialQuantities._pVolumeName && p.DefiningValues.Contains(matName));
                    foreach (IfcPropertyTableValue table in tables)
                    {
                        int index = table.DefiningValues.IndexOf(matName);
                        IfcVolumeMeasure vol = (IfcVolumeMeasure)(table.DefinedValues[index]);
                        double volD = (double)(vol.Value);
                        volume += volD;
                    }
                }
                volume = Math.Round(volume, 2);
                return volume;
            }
        }

        public double? iCimCO2
        {
            get 
            {
                if (iCimVolume == null || IcimProperties.CO2 == null) return null;
                double volume = iCimVolume ?? 0;
                double CO2 = IcimProperties.CO2 ?? 0;
                return Math.Round(volume * CO2, 2); 
            }
        }

        public double? iCimCO2e
        {
            get
            {
                if (iCimVolume == null || IcimProperties.CO2e == null) return null;
                double volume = iCimVolume ?? 0;
                double CO2e = IcimProperties.CO2e ?? 0;
                return Math.Round(volume * CO2e, 2);
            }
        }
        public double? iCimHeatingCapacity
        {
            get
            {
                if (iCimVolume == null || IcimProperties.HeatingCapacity == null) return null;
                double volume = iCimVolume ?? 0;
                double heatCap = IcimProperties.HeatingCapacity ?? 0;
                return Math.Round(volume * heatCap, 2);
            }
        }


        IBimSingleProperties IBimMaterial.SingleProperties
        {
            get { return SingleProperties as IBimSingleProperties; }
        }
    }

    public class IcimMaterialProperties
    {
        private IfcMaterial _material;
        private IModel _model;

        internal IcimMaterialProperties(IfcMaterial material, IModel model)
        {
            _material = material;
            _model = model;
        }

        public string Comments
        {
            get 
            {
                IfcSimpleValue value = _material.GetExtendedSingleValueValue(_model, "IcimMaterialProperties", "Comments") as IfcSimpleValue;
                if (value == null) return null;
                IfcLabel val = (IfcLabel)value;
                return val;
            }
            set
            {
                if (value == null)
                    _material.DeleteExtendedSingleValue(_model, "IcimMaterialProperties", "Comments");
                else
                {
                    IfcLabel val = value;
                    _material.SetExtendedSingleValue(_model, "IcimMaterialProperties", "Comments", val);
                }
            }
        }

        public double? Energy
        {
            get
            {
                IfcSimpleValue value = _material.GetExtendedSingleValueValue(_model, "IcimMaterialProperties", "Embodied Energy") as IfcSimpleValue;
                if (value == null) return null;
                IfcReal val = (IfcReal)value;
                return val;
            }
            set
            {
                if (value == null)
                    _material.DeleteExtendedSingleValue(_model, "IcimMaterialProperties", "Embodied Energy");
                else
                {
                    IfcReal val = value ?? 0;
                    _material.SetExtendedSingleValue(_model, "IcimMaterialProperties", "Embodied Energy", val);
                }
            }
        }

        public double? CO2
        {
            get
            {
                IfcSimpleValue value = _material.GetExtendedSingleValueValue(_model, "IcimMaterialProperties", "Embodied CO2") as IfcSimpleValue;
                if (value == null) return null;
                IfcReal val = (IfcReal)value;
                return val;
            }
            set
            {
                if (value == null) 
                    _material.DeleteExtendedSingleValue(_model, "IcimMaterialProperties", "Embodied CO2");
                else
                {
                    IfcReal val = value ?? 0;
                    _material.SetExtendedSingleValue(_model, "IcimMaterialProperties", "Embodied CO2", val);
                }
            }
        }

        public double? CO2e
        {
            get
            {
                IfcSimpleValue value = _material.GetExtendedSingleValueValue(_model, "IcimMaterialProperties", "Embodied CO2e") as IfcSimpleValue;
                if (value == null) return null;
                IfcReal val = (IfcReal)value;
                return val;
            }
            set
            {
                if (value == null) 
                    _material.DeleteExtendedSingleValue(_model, "IcimMaterialProperties", "Embodied CO2e");
                else
                {
                    IfcReal val = value ?? 0;
                    _material.SetExtendedSingleValue(_model, "IcimMaterialProperties", "Embodied CO2e", val);
                }
            }
        }

        public double? Density
        {
            get
            {
                IfcSimpleValue value = _material.GetExtendedSingleValueValue(_model, "IcimMaterialProperties", "Density") as IfcSimpleValue;
                if (value == null) return null;
                IfcReal val = (IfcReal)value;
                return val;
            }
            set
            {
                if (value == null)
                    _material.DeleteExtendedSingleValue(_model, "IcimMaterialProperties", "Density");
                else
                {
                    IfcReal val = value ?? 0;
                    _material.SetExtendedSingleValue(_model, "IcimMaterialProperties", "Density", val);
                }
            }
        }

        public double? Conductivity
        {
            get
            {
                IfcSimpleValue value = _material.GetExtendedSingleValueValue(_model, "IcimMaterialProperties", "Conductivity") as IfcSimpleValue;
                if (value == null) return null;
                IfcReal val = (IfcReal)value;
                return val;
            }
            set
            {
                if (value == null) 
                    _material.DeleteExtendedSingleValue(_model, "IcimMaterialProperties", "Conductivity");
                else
                {
                    IfcReal val = value ?? 0;
                    _material.SetExtendedSingleValue(_model, "IcimMaterialProperties", "Conductivity", val);
                }
            }
        }

        public double? HeatingCapacity
        {
            get
            {
                IfcSimpleValue value = _material.GetExtendedSingleValueValue(_model, "IcimMaterialProperties", "HeatingCapacity") as IfcSimpleValue;
                if (value == null) return null;
                IfcReal val = (IfcReal)value;
                return val;
            }
            set
            {
                if (value == null)
                    _material.DeleteExtendedSingleValue(_model, "IcimMaterialProperties", "HeatingCapacity");
                else
                {
                    IfcReal val = value ?? 0;
                    _material.SetExtendedSingleValue(_model, "IcimMaterialProperties", "HeatingCapacity", val);
                }
            }
        }
    }

   
}
