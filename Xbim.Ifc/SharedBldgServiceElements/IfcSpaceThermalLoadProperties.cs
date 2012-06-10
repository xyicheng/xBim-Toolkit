#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcSpaceThermalLoadProperties.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.TimeSeriesResource;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.SharedBldgServiceElements
{
    [IfcPersistedEntity, Serializable]
    public class IfcSpaceThermalLoadProperties : IfcPropertySetDefinition
    {
        #region fields

        private IfcPositiveRatioMeasure _applicableValueRatio;
        private IfcThermalLoadSourceEnum _thermalLoadSource;
        private IfcPropertySourceEnum _propertySource;
        private IfcText _sourceDescription;
        private IfcPowerMeasure _maximumValue;
        private IfcPowerMeasure? _minimumValue;

        private IfcTimeSeries _thermalLoadTimeSeriesValues;
        private IfcLabel _userDefinedThermalLoadSource;
        private IfcLabel _userDefinedPropertySource;
        private IfcThermalLoadTypeEnum _thermalLoadType;

        #endregion

        [IfcAttribute(5, IfcAttributeState.Optional)]
        public IfcPositiveRatioMeasure ApplicableValueRatio
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _applicableValueRatio;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _applicableValueRatio, value, v => ApplicableValueRatio = v,
                                           "ApplicableValueRatio");
            }
        }

        [IfcAttribute(6, IfcAttributeState.Mandatory)]
        public IfcThermalLoadSourceEnum ThermalLoadSource
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _thermalLoadSource;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _thermalLoadSource, value, v => ThermalLoadSource = v,
                                           "ThermalLoadSource");
            }
        }

        [IfcAttribute(7, IfcAttributeState.Mandatory)]
        public IfcPropertySourceEnum PropertySource
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _propertySource;
            }
            set { ModelManager.SetModelValue(this, ref _propertySource, value, v => PropertySource = v, "PropertySource"); }
        }

        [IfcAttribute(8, IfcAttributeState.Optional)]
        public IfcText SourceDescription
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _sourceDescription;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _sourceDescription, value, v => SourceDescription = v,
                                           "SourceDescription");
            }
        }

        [IfcAttribute(9, IfcAttributeState.Mandatory)]
        public IfcPowerMeasure MaximumValue
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _maximumValue;
            }
            set { ModelManager.SetModelValue(this, ref _maximumValue, value, v => MaximumValue = v, "MaximumValue"); }
        }

        [IfcAttribute(10, IfcAttributeState.Optional)]
        public IfcPowerMeasure? MinimumValue
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _minimumValue;
            }
            set { ModelManager.SetModelValue(this, ref _minimumValue, value, v => MinimumValue = v, "MinimumValue"); }
        }

        [IfcAttribute(11, IfcAttributeState.Optional)]
        public IfcTimeSeries ThermalLoadTimeSeriesValues
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _thermalLoadTimeSeriesValues;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _thermalLoadTimeSeriesValues, value,
                                           v => ThermalLoadTimeSeriesValues = v, "ThermalLoadTimeSeriesValues");
            }
        }

        [IfcAttribute(12, IfcAttributeState.Optional)]
        public IfcLabel UserDefinedThermalLoadSource
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _userDefinedThermalLoadSource;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _userDefinedThermalLoadSource, value,
                                           v => UserDefinedThermalLoadSource = v, "UserDefinedThermalLoadSource");
            }
        }

        [IfcAttribute(13, IfcAttributeState.Optional)]
        public IfcLabel UserDefinedPropertySource
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _userDefinedPropertySource;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _userDefinedPropertySource, value,
                                           v => UserDefinedPropertySource = v, "UserDefinedPropertySource");
            }
        }

        [IfcAttribute(14, IfcAttributeState.Optional)]
        public IfcThermalLoadTypeEnum ThermalLoadType
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _thermalLoadType;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _thermalLoadType, value, v => ThermalLoadType = v,
                                           "ThermalLoadType");
            }
        }

        #region ISupportIfcParser Members

        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                case 1:
                case 2:
                case 3:
                    base.IfcParse(propIndex, value);
                    break;
                case 4:
                    _applicableValueRatio = value.RealVal;
                    break;
                case 5:
                    _thermalLoadSource =
                        (IfcThermalLoadSourceEnum) Enum.Parse(typeof (IfcThermalLoadSourceEnum), value.EnumVal, true);
                    break;
                case 6:
                    _propertySource =
                        (IfcPropertySourceEnum) Enum.Parse(typeof (IfcPropertySourceEnum), value.EnumVal, true);
                    break;
                case 7:
                    _sourceDescription = value.StringVal;
                    break;
                case 8:
                    _maximumValue = value.RealVal;
                    break;
                case 9:
                    _minimumValue = value.RealVal;
                    break;
                case 10:
                    _thermalLoadTimeSeriesValues = (IfcTimeSeries) value.EntityVal;
                    break;
                case 11:
                    _userDefinedThermalLoadSource = value.StringVal;
                    break;
                case 12:
                    _userDefinedPropertySource = value.StringVal;
                    break;
                case 13:
                    _thermalLoadType =
                        (IfcThermalLoadTypeEnum) Enum.Parse(typeof (IfcThermalLoadTypeEnum), value.EnumVal, true);
                    break;

                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }

        #endregion

        public override string WhereRule()
        {
            return "";
        }
    }
}