using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.MeasureResource;
using System.Collections.Concurrent;
using Xbim.Ifc2x3.QuantityResource;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.Ifc2x3.ExternalReferenceResource;
using System.Text.RegularExpressions;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.Ifc2x3.Extensions;


namespace Xbim.WebXplorer.xbim
{
    /// <summary>
    /// Static Class for retrieving Property values
    /// </summary>
    public static class ModelPropertyValues
    {
        /// <summary>
        /// convert the ² and ³ to html
        /// </summary>
        /// <param name="abbriv">Unit Abbreviation</param>
        /// <returns>string</returns>
        public static string AbbreviationToHTML(string abbriv)
        {
            if (abbriv.Contains('\u00B2'))
            {
                abbriv = abbriv.Replace('\u00B2'.ToString(), "<sup>2</sup>");
            }
            if (abbriv.Contains('\u00B3'))
            {
                abbriv = abbriv.Replace('\u00B3'.ToString(), "<sup>3</sup>");
            }
            return abbriv;
        }

        /// <summary>
        /// Get the html version of the Derived unit
        /// </summary>
        /// <param name="ifcDerivedUnit">IfcDerivedUnit to get name for</param>
        /// <returns>String holding name</returns>
        public static string GetDerivedUnitName(IfcDerivedUnit ifcDerivedUnit)
        {

            string unit = string.Empty;
            if (ifcDerivedUnit.UserDefinedType.HasValue)
                unit = ifcDerivedUnit.UserDefinedType;
            else
            {
                List<string> values = new List<string>();
                foreach (IfcDerivedUnitElement item in ifcDerivedUnit.Elements)//loop the units associated to this type
                {
                    string value = string.Empty;
                    value = item.Unit.GetName();
                    //add power
                    if (item.Exponent > 0)
                    {
                        value += "<sup>" + item.Exponent.ToString() + "</sup>";
                    }
                }
                if (values.Count > 1)
                    unit = string.Join("/", values);
                else
                    unit = string.Empty;
            }
            return unit;
        }

        /// <summary>
        /// Get the unit abbreviation html friendly
        /// </summary>
        /// <param name="ifcNamedUnit">IfcNamedUnit unit to abbreviate</param>
        /// <returns>string as abbreviation</returns>
        public static string GetUnitAbbreviation(IfcNamedUnit ifcNamedUnit)
        {
            string abbriv = string.Empty;

            if (ifcNamedUnit is IfcSIUnit)
            {
                //get abbreviation for unit
                abbriv = (ifcNamedUnit as IfcSIUnit).GetSymbol(); //try Unicode
            }

            if (ifcNamedUnit is IfcConversionBasedUnit)
            {
                abbriv = (ifcNamedUnit as IfcConversionBasedUnit).GetSymbol();
            }

            if (ifcNamedUnit is IfcContextDependentUnit)
            {
                abbriv = (ifcNamedUnit as IfcContextDependentUnit).GetSymbol();
            }

            abbriv = AbbreviationToHTML(abbriv);

            return abbriv.ToLower();
        }



        /// <summary>
        /// Get the unit abbreviation html friendly
        /// </summary>
        /// <param name="ifcUnit">IfcUnit unit to abbreviate</param>
        /// <param name="ifcGlobalUnits">global unit dictionary</param>
        /// <returns>string as abbreviation</returns>
        public static string GetUnitAbbreviation(IfcUnit ifcUnit, ConcurrentDictionary<string, string> ifcGlobalUnits)
        {
            string unit = string.Empty;
            if (ifcUnit == null)
                unit = string.Empty;
            else if (ifcUnit is IfcNamedUnit)
            {
                unit = GetUnitAbbreviation(ifcUnit as IfcNamedUnit);
                //get global unit
                if (string.IsNullOrEmpty(unit))
                {
                    if (!ifcGlobalUnits.TryGetValue((ifcUnit as IfcNamedUnit).UnitType.ToString(), out unit))
                        unit = string.Empty;
                }

            }
            else if (ifcUnit is IfcMonetaryUnit)
            {
                unit = (ifcUnit as IfcMonetaryUnit).GetSymbol(); //get abbreviation
                if (string.IsNullOrEmpty(unit))
                {
                    if (!ifcGlobalUnits.TryGetValue("MonetaryUnit", out unit))
                        unit = string.Empty;
                }

            }
            else if (ifcUnit is IfcDerivedUnit)
                unit = GetDerivedUnitName((ifcUnit as IfcDerivedUnit));

            unit = AbbreviationToHTML(unit);
            return unit.ToLower(); ;
        }


        /// <summary>
        /// get the IfcPhysicalSimpleQuantity property value 
        /// </summary>
        /// <param name="p">IfcPhysicalSimpleQuantity</param>
        /// <param name="ifcGlobalUnits">global unit dictionary</param>
        /// <returns>string representing the value with units</returns>
        public static string FormatQuantityValue(IfcPhysicalSimpleQuantity p, ConcurrentDictionary<string, string> ifcGlobalUnits)
        {
            string numberFormat = "{0,0:N2}";
            string unit = string.Empty;

            //get associated unit abbreviation to the IfcPhysicalSimpleQuantity
            if (p.Unit != null)
                unit = GetUnitAbbreviation(p.Unit);

            if (p is IfcQuantityLength)
            {
                string value = string.Format(numberFormat, (p as IfcQuantityLength).LengthValue.Value);
                //get global if unit is null
                if (string.IsNullOrEmpty(unit))
                {
                    if (!ifcGlobalUnits.TryGetValue(IfcUnitEnum.LENGTHUNIT.ToString(), out unit))
                        unit = string.Empty;
                }

                return value + unit;
            }
            if (p is IfcQuantityArea)
            {
                string value = string.Format(numberFormat, (p as IfcQuantityArea).AreaValue.Value);
                if (string.IsNullOrEmpty(unit))
                {
                    if (!ifcGlobalUnits.TryGetValue(IfcUnitEnum.AREAUNIT.ToString(), out unit))
                        unit = string.Empty;
                }

                return value + unit;
            }
            if (p is IfcQuantityVolume)
            {
                string value = string.Format(numberFormat, (p as IfcQuantityVolume).VolumeValue.Value);
                if (string.IsNullOrEmpty(unit))
                {
                    if (!ifcGlobalUnits.TryGetValue(IfcUnitEnum.VOLUMEUNIT.ToString(), out unit))
                        unit = string.Empty;
                }

                return value + unit;
            }
            if (p is IfcQuantityCount)
            {
                string value = string.Format(numberFormat, (p as IfcQuantityCount).CountValue.Value);

                return value + unit;
            }
            if (p is IfcQuantityWeight)
            {
                string value = string.Format(numberFormat, (p as IfcQuantityWeight).WeightValue.Value);
                if (string.IsNullOrEmpty(unit))
                {
                    if (!ifcGlobalUnits.TryGetValue(IfcUnitEnum.MASSUNIT.ToString(), out unit))
                        unit = string.Empty;
                }

                return value + unit;
            }
            if (p is IfcQuantityTime)
            {
                string value = string.Format(numberFormat, (p as IfcQuantityTime).TimeValue.Value);
                if (string.IsNullOrEmpty(unit))
                {
                    if (!ifcGlobalUnits.TryGetValue(IfcUnitEnum.TIMEUNIT.ToString(), out unit))
                        unit = string.Empty;
                }

                return value + unit;
            }
            return string.Empty;
        }

        /// <summary>
        /// get the property value
        /// </summary>
        /// <param name="p"></param>
        /// <param name="ifcGlobalUnits">global unit dictionary</param>
        /// <returns></returns>
        public static string FormatPropertyValue(IfcSimpleProperty p, ConcurrentDictionary<string, string> ifcGlobalUnits)
        {
            if ((p is IfcPropertySingleValue) &&
                (((IfcPropertySingleValue)p).NominalValue != null))
            {
                return FormatPropertyValue((p as IfcPropertySingleValue), ifcGlobalUnits);
            }
            if (p is IfcPropertyEnumeratedValue)
            {
                return FormatPropertyValue((p as IfcPropertyEnumeratedValue), ifcGlobalUnits);
            }
            if (p is IfcPropertyBoundedValue)
            {
                return FormatePropertyValue((p as IfcPropertyBoundedValue), ifcGlobalUnits);
            }
            if (p is IfcPropertyTableValue)
            {
                return FormatePropertyValue((p as IfcPropertyTableValue), ifcGlobalUnits);
            }
            if (p is IfcPropertyReferenceValue)
            {
                return FormatPropertyValue((p as IfcPropertyReferenceValue));
            }
            if (p is IfcPropertyListValue)
            {
                return FormatPropertyValue((p as IfcPropertyListValue), ifcGlobalUnits);
            }
            return string.Empty;
        }

        /// <summary>
        /// Get the IfcPropertySingleValue value with unit
        /// </summary>
        /// <param name="ifcPropertySingleValue">IfcPropertySingleValue</param>
        /// <param name="ifcGlobalUnits">global unit dictionary</param>
        /// <returns>string holding value</returns>
        public static string FormatPropertyValue(IfcPropertySingleValue ifcPropertySingleValue, ConcurrentDictionary<string, string> ifcGlobalUnits)
        {
            bool moneyUnit = false;
            string unit = GetUnitAbbreviation(ifcPropertySingleValue.Unit, ifcGlobalUnits);
            if (ifcPropertySingleValue.Unit is IfcMonetaryUnit)
                moneyUnit = true;
            IfcValue ifcValue = ifcPropertySingleValue.NominalValue;
            return FormatIfcValue(ifcValue, unit, moneyUnit);
        }

        /// <summary>
        /// get the IfcPropertyTableValue values as a string
        /// </summary>
        /// <param name="ifcPropertyTableValue"></param>
        /// <param name="ifcGlobalUnits">global unit dictionary</param>
        /// <returns></returns>
        public static string FormatePropertyValue(IfcPropertyTableValue ifcPropertyTableValue, ConcurrentDictionary<string, string> ifcGlobalUnits)
        {
            StringBuilder cellValue = new StringBuilder();
            //sort units
            bool definingMoneyUnit = false;
            bool definedMoneyUnit = false;
            string definingUnit = GetUnitAbbreviation(ifcPropertyTableValue.DefiningUnit, ifcGlobalUnits);
            if (ifcPropertyTableValue.DefiningUnit is IfcMonetaryUnit)
                definingMoneyUnit = true;
            string definedUnit = GetUnitAbbreviation(ifcPropertyTableValue.DefinedUnit, ifcGlobalUnits);
            if (ifcPropertyTableValue.DefinedUnit is IfcMonetaryUnit)
                definedMoneyUnit = true;

            //check we have a correctly formatted table
            if ((ifcPropertyTableValue.DefiningValues != null) &&
                            (ifcPropertyTableValue.DefinedValues != null) &&
                            (ifcPropertyTableValue.DefiningValues.Count() == ifcPropertyTableValue.DefinedValues.Count())
                            )
            {
                int i = 0;
                foreach (var item in ifcPropertyTableValue.DefiningValues)
                {
                    cellValue.Append("(");
                    //get defining value
                    string defining = FormatIfcValue(item, definingUnit, definingMoneyUnit);
                    cellValue.Append(defining);
                    cellValue.Append(":");
                    //get defined value
                    string defined = FormatIfcValue(ifcPropertyTableValue.DefinedValues[i], definedUnit, definedMoneyUnit);
                    cellValue.Append(defined);
                    cellValue.Append(")");
                    i++;
                }
            }
            else
            {
                cellValue.Append("Badly formatted IfcPropertyTableValue table");
            }
            return cellValue.ToString(); ;
        }

        /// <summary>
        /// Get the IfcPropertyBoundedValue values as a string
        /// </summary>
        /// <param name="ifcPropertyBoundedValue">IfcPropertyBoundedValue object</param>
        /// <param name="ifcGlobalUnits">global unit dictionary</param>
        /// <returns>string</returns>
        public static string FormatePropertyValue(IfcPropertyBoundedValue ifcPropertyBoundedValue, ConcurrentDictionary<string, string> ifcGlobalUnits)
        {
            string lowervalue = string.Empty;
            string uppervalue = string.Empty;
            bool moneyUnit = false;

            string unit = GetUnitAbbreviation(ifcPropertyBoundedValue.Unit, ifcGlobalUnits);
            if (ifcPropertyBoundedValue.Unit is IfcMonetaryUnit)
                moneyUnit = true;

            if (ifcPropertyBoundedValue.LowerBoundValue != null)
                lowervalue = FormatIfcValue(ifcPropertyBoundedValue.LowerBoundValue, unit, moneyUnit);
            else
                lowervalue = "unknown";
            if (ifcPropertyBoundedValue.UpperBoundValue != null)
                uppervalue = FormatIfcValue(ifcPropertyBoundedValue.UpperBoundValue, unit, moneyUnit);
            else
                uppervalue = "unknown";
            return lowervalue + "(lower) : " + uppervalue + "(upper)";
        }

        /// <summary>
        /// Get the value/s of the IfcPropertyEnumeratedValue
        /// </summary>
        /// <param name="ifcPropertyEnumeratedValue">IfcPropertyEnumeratedValue</param>
        /// <param name="ifcGlobalUnits">global unit dictionary</param>
        /// <returns>comma delimited string of values</returns>
        public static string FormatPropertyValue(IfcPropertyEnumeratedValue ifcPropertyEnumeratedValue, ConcurrentDictionary<string, string> ifcGlobalUnits)
        {
            string unit = string.Empty;
            bool moneyUnit = false;

            //get the unit
            IfcUnit ifcUnit = null;
            if ((ifcPropertyEnumeratedValue.EnumerationReference != null) &&
                (ifcPropertyEnumeratedValue.EnumerationReference.Unit != null)
                )
            {
                ifcUnit = ifcPropertyEnumeratedValue.EnumerationReference.Unit;
            }

            //process the unit value
            unit = GetUnitAbbreviation(ifcUnit, ifcGlobalUnits);
            if (ifcUnit is IfcMonetaryUnit)
                moneyUnit = true;

            //create enumerated values as comma delimited list, not the Enum values which may be held in EnumerationReference
            List<string> values = new List<string>();
            foreach (IfcValue item in ifcPropertyEnumeratedValue.EnumerationValues)
            {
                string value = FormatIfcValue(item, unit, moneyUnit);
                values.Add(value);
            }
            return string.Join(", ", values);
        }

        /// <summary>
        /// Get the value associated of the ifcPropertyReferenceValue
        /// </summary>
        /// <param name="ifcPropertyEnumeratedValue">ifcPropertyReferenceValue</param>
        /// <returns>comma delimited string of values</returns>
        public static string FormatPropertyValue(IfcPropertyReferenceValue ifcPropertyReferenceValue)
        {
            IfcObjectReferenceSelect ifcObjectReferenceSelect = ifcPropertyReferenceValue.PropertyReference;
            string value = ifcObjectReferenceSelect.GetValuesAsString();
            if (ifcObjectReferenceSelect is IfcExternalReference)
            {
                return ConvertUrlsToLinks(value);
            }

            return value;
        }


        /// <summary>
        /// Get the value/s of the IfcPropertyListValue
        /// </summary>
        /// <param name="ifcPropertyListValue">IfcPropertyListValue</param>
        /// <param name="ifcGlobalUnits">global unit dictionary</param>
        /// <returns>comma delimited string of values</returns>
        public static string FormatPropertyValue(IfcPropertyListValue ifcPropertyListValue, ConcurrentDictionary<string, string> ifcGlobalUnits)
        {
            string unit = string.Empty;
            bool moneyUnit = false;

            //get the unit
            IfcUnit ifcUnit = ifcPropertyListValue.Unit;
            //process the unit value
            unit = GetUnitAbbreviation(ifcUnit, ifcGlobalUnits);
            if (ifcUnit is IfcMonetaryUnit)
                moneyUnit = true;

            //create enumerated values as comma delimited list, not the Enum values which may be held in EnumerationReference
            List<string> values = new List<string>();
            foreach (IfcValue item in ifcPropertyListValue.ListValues)
            {
                string value = FormatIfcValue(item, unit, moneyUnit);
                values.Add(value);
            }
            return string.Join(", ", values);
        }

        /// <summary>
        /// format the IfcValue if a number, if not just get the string value. append/prepend the unit string to the value depending on unit type
        /// </summary>
        /// <param name="ifcValue">IfcValue</param>
        /// <param name="unit">string holding unit</param>
        /// <param name="moneyUnit">bool true if a money value</param>
        /// <returns></returns>
        public static string FormatIfcValue(IfcValue ifcValue, string unit, bool moneyUnit)
        {
            string numberFormat = "{0,0:N2}";
            string value = string.Empty;

            if (ifcValue.UnderlyingSystemType == typeof(double))
                value = string.Format(numberFormat, double.Parse(ifcValue.ToString()));
            else if (ifcValue.UnderlyingSystemType == typeof(string))
                return ConvertUrlsToLinks(ifcValue.ToString());
            else
                return ifcValue.ToString();

            //add the unit
            if (moneyUnit)
                return unit + value;
            else
                return value + unit;
        }


        public static string ConvertUrlsToLinks(string msg)
        {
            string regex = @"((www\.|(http|https|ftp|news|file)+\:\/\/)[&#95;.a-z0-9-]+\.[a-z0-9\/&#95;:@=.+?,##%&~-]*[^.|\'|\# |!|\(|?|,| |>|<|;|\)])";
            Regex r = new Regex(regex, RegexOptions.IgnoreCase);
            return r.Replace(msg, "<a href=\"$1\" title=\"Click to open in a new window or tab\" target=\"&#95;blank\">$1</a>").Replace("href=\"www", "href=\"http://www");
        }


    }
}