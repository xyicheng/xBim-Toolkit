using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.QuantityResource;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.Kernel;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.DOM.PropertiesQuantities
{
    public abstract class XbimQuantities
    {

        private IfcObject _ifcObject;
        private IfcTypeObject _ifcTypeObject;
        private string _psetName;

        //units for assignment
        private static IfcNamedUnit _lengthUnit;
        private static IfcNamedUnit _areaUnit;
        private static IfcNamedUnit _volumeUnit;
        private static IfcNamedUnit _weight;


        internal XbimQuantities(IfcObject ifcObject, string propertySetName)
        {
            if (ifcObject == null || propertySetName == null || propertySetName == "") throw new ArgumentNullException();
            _psetName = propertySetName;
            _ifcObject = ifcObject;
            _ifcTypeObject = null;
            SetMetersAndMilimetersAsBaseUnit(ifcObject);
        }

        internal XbimQuantities(IfcTypeObject ifcTypeObject, string propertySetName)
        {
            if (ifcTypeObject == null || propertySetName == null || propertySetName == "") throw new ArgumentNullException();
            _psetName = propertySetName;
            _ifcObject = null;
            _ifcTypeObject = ifcTypeObject ;
            SetMetersAndMilimetersAsBaseUnit(ifcTypeObject);
        }

        /// <summary>
        /// These units are used for specification of the new properties. Existing properties are not affected.
        /// </summary>
        /// <param name="lengthUnit">Length unit</param>
        /// <param name="areaUnit">Area unit</param>
        /// <param name="volumeUnit">Volume unit</param>
        public void SetNamedUnitsForProperties(IfcNamedUnit lengthUnit, IfcNamedUnit areaUnit, IfcNamedUnit volumeUnit, IfcNamedUnit weight)
        {
            _lengthUnit = lengthUnit;
            _areaUnit = areaUnit;
            _volumeUnit = volumeUnit;
            _weight = weight;
        }

        /// <summary>
        /// These units are used for specification of the new properties. Existing properties are not affected.
        /// </summary>
        public static void SetMetersAndMilimetersAsBaseUnit(IfcObjectDefinition ifcObject)
        {
            IModel model = ifcObject.ModelOf;

            if (_lengthUnit == null || !(_lengthUnit is IfcSIUnit)) 
            {
                //length unit
                IfcSIUnit lengthUnit = model.New<IfcSIUnit>();
                lengthUnit.Name = IfcSIUnitName.METRE;
                lengthUnit.Prefix = IfcSIPrefix.MILLI;
                lengthUnit.UnitType = IfcUnitEnum.LENGTHUNIT;
                _lengthUnit = lengthUnit;
            }

            if (_areaUnit == null ||!(_areaUnit is IfcSIUnit))
            {
                //area unit
                IfcSIUnit areaUnit = model.New<IfcSIUnit>();
                areaUnit.Name = IfcSIUnitName.SQUARE_METRE;
                areaUnit.UnitType = IfcUnitEnum.AREAUNIT;
                _areaUnit = areaUnit;
            }

            if (_volumeUnit == null || !(_volumeUnit is IfcSIUnit))
            {
                //volume init
                IfcSIUnit volumeUnit = model.New<IfcSIUnit>();
                volumeUnit.Name = IfcSIUnitName.CUBIC_METRE;
                volumeUnit.UnitType = IfcUnitEnum.VOLUMEUNIT;
                _volumeUnit = volumeUnit;
            }
        }

        protected double? GetElementQuantityAsDouble(string quantityName)
        {
            IfcPhysicalSimpleQuantity quantity = _ifcObject != null ? _ifcObject.GetElementPhysicalSimpleQuantity(_psetName, quantityName) : _ifcTypeObject.GetElementPhysicalSimpleQuantity(_psetName, quantityName);
            if (quantity == null) return null;
            if (quantity is IfcQuantityLength) return (quantity as IfcQuantityLength).LengthValue;
            if (quantity is IfcQuantityArea) return (quantity as IfcQuantityArea).AreaValue;
            if (quantity is IfcQuantityVolume) return (quantity as IfcQuantityVolume).VolumeValue;
            if (quantity is IfcQuantityCount) return (quantity as IfcQuantityCount).CountValue;
            if (quantity is IfcQuantityWeight) return (quantity as IfcQuantityWeight).WeightValue;
            if (quantity is IfcQuantityTime) return (quantity as IfcQuantityTime).TimeValue;
            return null;
        }

        private void RemoveQuantity(string quantityName)
        {
            if (_ifcObject != null)
                _ifcObject.RemoveElementPhysicalSimpleQuantity(_psetName, quantityName);
            else
                _ifcTypeObject.RemoveElementPhysicalSimpleQuantity(_psetName, quantityName);

        }

        private void SetQuantity(string quantityName, XbimQuantityTypeEnum quantityType, double value)
        {
            if (_ifcObject != null)
            {
                switch (quantityType)
                {
                    case XbimQuantityTypeEnum.AREA: _ifcObject.SetElementPhysicalSimpleQuantity(_psetName, quantityName, value, quantityType, _areaUnit); break;
                    case XbimQuantityTypeEnum.COUNT: _ifcObject.SetElementPhysicalSimpleQuantity(_psetName, quantityName, value, quantityType, null); break;
                    case XbimQuantityTypeEnum.LENGTH: _ifcObject.SetElementPhysicalSimpleQuantity(_psetName, quantityName, value, quantityType, _lengthUnit); break;
                    case XbimQuantityTypeEnum.TIME: _ifcObject.SetElementPhysicalSimpleQuantity(_psetName, quantityName, value, quantityType, null); break;
                    case XbimQuantityTypeEnum.VOLUME: _ifcObject.SetElementPhysicalSimpleQuantity(_psetName, quantityName, value, quantityType, _volumeUnit); break;
                    case XbimQuantityTypeEnum.WEIGHT: _ifcObject.SetElementPhysicalSimpleQuantity(_psetName, quantityName, value, quantityType, null); break;
                }
            }
            else
            {
                switch (quantityType)
                {
                    case XbimQuantityTypeEnum.AREA: _ifcTypeObject.SetElementPhysicalSimpleQuantity(_psetName, quantityName, value, quantityType, _areaUnit); break;
                    case XbimQuantityTypeEnum.COUNT: _ifcTypeObject.SetElementPhysicalSimpleQuantity(_psetName, quantityName, value, quantityType, null); break;
                    case XbimQuantityTypeEnum.LENGTH: _ifcTypeObject.SetElementPhysicalSimpleQuantity(_psetName, quantityName, value, quantityType, _lengthUnit); break;
                    case XbimQuantityTypeEnum.TIME: _ifcTypeObject.SetElementPhysicalSimpleQuantity(_psetName, quantityName, value, quantityType, null); break;
                    case XbimQuantityTypeEnum.VOLUME: _ifcTypeObject.SetElementPhysicalSimpleQuantity(_psetName, quantityName, value, quantityType, _volumeUnit); break;
                    case XbimQuantityTypeEnum.WEIGHT: _ifcTypeObject.SetElementPhysicalSimpleQuantity(_psetName, quantityName, value, quantityType, null); break;
                }
            }
            
        }

        protected void SetOrRemoveQuantity(string quantityName, XbimQuantityTypeEnum quantityType, double? value)
        {
            if (value == null)
            {
                RemoveQuantity(quantityName);
            }
            else
            {
                double val = value ?? 0;
                SetQuantity(quantityName, quantityType, val);
            }
        }
    }
}
