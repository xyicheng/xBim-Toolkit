#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcSIUnit.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Xml.Serialization;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.MeasureResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcSIUnit : IfcNamedUnit
    {
        #region Fields

        private IfcSIPrefix? _prefix;
        private IfcSIUnitName _name;

        #endregion

        #region Part 21 Step file Parse routines

        /// <summary>
        ///   The dimensional exponents of SI units are derived and override the NamedUnit value
        /// </summary>
        [XmlArrayItem(typeof (Int16))]
        [IfcAttribute(1, IfcAttributeState.DerivedOverride)]
        public override IfcDimensionalExponents Dimensions
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return IfcDimensionalExponents.DimensionsForSiUnit(Name);
            }
        }

        /// <summary>
        ///   The SI Prefix for defining decimal multiples and submultiples of the unit.
        /// </summary>
        [IfcAttribute(3, IfcAttributeState.Optional)]
        public IfcSIPrefix? Prefix
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _prefix;
            }
            set { ModelManager.SetModelValue(this, ref _prefix, value, v => Prefix = v, "Prefix"); }
        }


        /// <summary>
        ///   The word, or group of words, by which the SI unit is referred to.
        /// </summary>
        [IfcAttribute(4, IfcAttributeState.Mandatory)]
        public IfcSIUnitName Name
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _name;
            }
            set { ModelManager.SetModelValue(this, ref _name, value, v => Name = v, "Name"); }
        }

        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    break; //do nothing NamedUnit.Dimensional Exponents is overrideen and derived in this class
                case 1:
                    base.IfcParse(propIndex, value);
                    break;
                case 2:
                    _prefix = (IfcSIPrefix) Enum.Parse(typeof (IfcSIPrefix), value.EnumVal, true);
                    break;
                case 3:
                    _name = (IfcSIUnitName) Enum.Parse(typeof (IfcSIUnitName), value.EnumVal, true);
                    break;
                default:
                    throw new Exception(string.Format("Attribute index {0} is out of range for {1}", propIndex + 1,
                                                      this.GetType().Name.ToUpper()));
            }
        }

        #endregion

        public override string ToString()
        {
            return string.Format("{0}{1}", _prefix.HasValue ? _prefix.Value.ToString() : "", _name.ToString());
        }
    }
}