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
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }

        #endregion
        /// <summary>
        /// Returns the full name of the unit
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            string prefixUnit = (_prefix.HasValue) ? _prefix.ToString() : "";  //see IfcSIPrefix
            string value = _name.ToString();                                   //see IfcSIUnitName
            //Handle the "_" in _name value, should work for lengths, but might have to look at other values later
            if (!string.IsNullOrEmpty(value))
            {
                if (value.Contains("_"))
                    return value = value.Replace("_", prefixUnit);
                else
                    return value = prefixUnit + value; //combine to give length name
            }
            else
                return string.Format("{0}{1}", _prefix.HasValue ? _prefix.Value.ToString() : "", _name.ToString());
        }

        /// <summary>
        /// Return the Symbol string or full name string 
        /// </summary>
        /// <param name="sym">bool sym - true = symbol name</param>
        /// <returns>string</returns>
        public string ToString(bool sym)
        {
            if (sym)
            {
                IfcSIUnitName ifcSIUnitName = this.Name;
                IfcSIPrefix ifcSIPrefix;
                string value = string.Empty;
                string prefix = string.Empty;
                if (this.Prefix != null)
                {
                    ifcSIPrefix = (IfcSIPrefix)this.Prefix;
                    switch (ifcSIPrefix)
                    {
                        case IfcSIPrefix.CENTI:
                            prefix = "c";
                            break;
                        case IfcSIPrefix.MILLI:
                            prefix = "m";
                            break;
                        default: //TODO: the other values of IfcSIPrefix
                            prefix = ifcSIPrefix.ToString();
                            break;
                    }
                }

                switch (ifcSIUnitName)
                {
                    case IfcSIUnitName.METRE:
                        value = prefix + "m";
                        break;
                    case IfcSIUnitName.SQUARE_METRE:
                        value = prefix + "m" + ((char)0x00B2);
                        break;
                    case IfcSIUnitName.CUBIC_METRE:
                        value = prefix + "m" + ((char)0x00B3);
                        break;
                    default://TODO: the other values of IfcSIUnitName
                        value = this.ToString();
                        break;
                }
                return value;
            }
            else
                return this.ToString();
        }
    }
}