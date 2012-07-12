#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc.Extensions
// Filename:    UnitAssignmentExtensions.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Linq;
using Xbim.Ifc.MeasureResource;
using Xbim.XbimExtensions;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc.Extensions
{
    public static class UnitAssignmentExtensions
    {
        /// <summary>
        ///   Returns the factor to scale units by to convert them to SI millimetres, if they are SI units, returns 1 otherwise
        /// </summary>
        /// <param name = "ua"></param>
        /// <returns></returns>
        public static double LengthUnitPower(this IfcUnitAssignment ua)
        {
            IfcSIUnit si = ua.Units.OfType<IfcSIUnit>().FirstOrDefault(u => u.UnitType == IfcUnitEnum.LENGTHUNIT);
            if (si != null && si.Prefix.HasValue)
                return si.Power();
            else
            {
                IfcConversionBasedUnit cu =
                    ua.Units.OfType<IfcConversionBasedUnit>().FirstOrDefault(u => u.UnitType == IfcUnitEnum.LENGTHUNIT);
                if (cu != null)
                {
                    IfcMeasureWithUnit mu = cu.ConversionFactor;
                    IfcSIUnit uc = mu.UnitComponent as IfcSIUnit;
                    //some BIM tools such as StruCAD write the conversion value out as a Length Measure
                    if (uc != null)
                    {
                        ExpressType et = ((ExpressType)mu.ValueComponent);
                        double cFactor = 1.0;
                        if(et.UnderlyingSystemType==typeof(double))
                            cFactor = (double) et.Value;
                        else if(et.UnderlyingSystemType==typeof(int))
                            cFactor = (double) ((int)et.Value);
                        else if (et.UnderlyingSystemType == typeof(long))
                            cFactor = (double)((long)et.Value);

                        return uc.Power() * cFactor ;
                    }
                }
            }
            return 1.0;
        }

        public static double GetPower(this IfcUnitAssignment ua, IfcUnitEnum unitType)
        {
           
            IfcSIUnit si = ua.Units.OfType<IfcSIUnit>().FirstOrDefault(u => u.UnitType == unitType);
            if (si != null && si.Prefix.HasValue)
                return si.Power();
            else
            {
                IfcConversionBasedUnit cu =
                    ua.Units.OfType<IfcConversionBasedUnit>().FirstOrDefault(u => u.UnitType == unitType);
                if (cu != null)
                {
                    IfcMeasureWithUnit mu = cu.ConversionFactor;
                    IfcSIUnit uc = mu.UnitComponent as IfcSIUnit;
                    //some BIM tools such as StruCAD write the conversion value out as a Length Measure
                    if (uc != null)
                    {
                        ExpressType et = ((ExpressType)mu.ValueComponent);
                        double cFactor = 1.0;
                        if (et.UnderlyingSystemType == typeof(double))
                            cFactor = (double)et.Value;
                        else if (et.UnderlyingSystemType == typeof(int))
                            cFactor = (double)((int)et.Value);
                        else if (et.UnderlyingSystemType == typeof(long))
                            cFactor = (double)((long)et.Value);

                        return uc.Power() * cFactor;
                    }
                }
            }
            return 1.0;
        }

        /// <summary>
        ///   Sets the Length Unit to be SIUnit and SIPrefix, returns false if the units are not SI
        /// </summary>
        /// <param name = "ua"></param>
        /// <param name = "siUnitName"></param>
        /// <param name = "siPrefix"></param>
        /// <returns></returns>
        public static bool SetSILengthUnits(this IfcUnitAssignment ua, IfcSIUnitName siUnitName, IfcSIPrefix? siPrefix)
        {
            IfcSIUnit si = ua.Units.OfType<IfcSIUnit>().FirstOrDefault(u => u.UnitType == IfcUnitEnum.LENGTHUNIT);
            if (si != null)
            {
                si.Prefix = siPrefix;
                si.Name = siUnitName;
                return true;
            }
            else
                return false;
        }

        public static void SetOrChangeSIUnit(this IfcUnitAssignment ua, IfcUnitEnum unitType, IfcSIUnitName siUnitName,
                                             IfcSIPrefix siUnitPrefix)
        {
            IModel model = ModelManager.ModelOf(ua);
            IfcSIUnit si = ua.Units.OfType<IfcSIUnit>().FirstOrDefault(u => u.UnitType == unitType);
            if (si != null)
            {
                si.Prefix = siUnitPrefix;
                si.Name = siUnitName;
            }
            else
            {
                ua.Units.Add_Reversible(model.New<IfcSIUnit>(s =>
                                                                 {
                                                                     s.UnitType = unitType;
                                                                     s.Name = siUnitName;
                                                                     s.Prefix = siUnitPrefix;
                                                                 }));
            }
        }

        public static IfcNamedUnit GetAreaUnit(this IfcUnitAssignment ua)
        {
            IfcNamedUnit nu = ua.Units.OfType<IfcSIUnit>().FirstOrDefault(u => u.UnitType == IfcUnitEnum.AREAUNIT);
            if (nu == null)
                nu = ua.Units.OfType<IfcConversionBasedUnit>().FirstOrDefault(u => u.UnitType == IfcUnitEnum.AREAUNIT);
            return nu;
        }

        public static IfcNamedUnit GetVolumeUnit(this IfcUnitAssignment ua)
        {
            IfcNamedUnit nu = ua.Units.OfType<IfcSIUnit>().FirstOrDefault(u => u.UnitType == IfcUnitEnum.VOLUMEUNIT);
            if (nu == null)
                nu = ua.Units.OfType<IfcConversionBasedUnit>().FirstOrDefault(u => u.UnitType == IfcUnitEnum.VOLUMEUNIT);
            return nu;
        }

        public static string GetLengthUnitName(this IfcUnitAssignment ua)
        {
            IfcSIUnit si = ua.Units.OfType<IfcSIUnit>().FirstOrDefault(u => u.UnitType == IfcUnitEnum.LENGTHUNIT);
            if (si != null)
            {
                if (si.Prefix.HasValue)
                    return string.Format("{0}{1}", si.Prefix.Value.ToString(), si.Name.ToString());
                else
                    return si.Name.ToString();
            }
            else
            {
                IfcConversionBasedUnit cu =
                    ua.Units.OfType<IfcConversionBasedUnit>().FirstOrDefault(u => u.UnitType == IfcUnitEnum.LENGTHUNIT);
                if (cu != null)
                {
                    return cu.Name;
                }
                else
                {
                    IfcConversionBasedUnit cbu =
                        ua.Units.OfType<IfcConversionBasedUnit>().FirstOrDefault(
                            u => u.UnitType == IfcUnitEnum.LENGTHUNIT);
                    if (cbu != null)
                    {
                        return cbu.Name;
                    }
                }
            }
            return "";
        }

        public static void SetOrChangeConversionUnit(this IfcUnitAssignment ua, IfcUnitEnum unitType,
                                                     ConversionBasedUnit unit)
        {
            IModel model = ModelManager.ModelOf(ua);
            IfcSIUnit si = ua.Units.OfType<IfcSIUnit>().FirstOrDefault(u => u.UnitType == unitType);
            if (si != null)
            {
                ua.Units.Remove_Reversible(si);
                model.Delete(si);
            }
            ua.Units.Add_Reversible(GetNewConversionUnit(model, unitType, unit));
        }

        private static IfcConversionBasedUnit GetNewConversionUnit(IModel model, IfcUnitEnum unitType,
                                                                   ConversionBasedUnit unitEnum)
        {
            IfcConversionBasedUnit unit = model.New<IfcConversionBasedUnit>();
            unit.UnitType = unitType;

            switch (unitEnum)
            {
                case ConversionBasedUnit.INCH:
                    SetConversionUnitsParameters(model, unit, "inch", 25.4, IfcUnitEnum.LENGTHUNIT, IfcSIUnitName.METRE,
                                                 IfcSIPrefix.MILLI, GetLengthDimension(model));
                    break;
                case ConversionBasedUnit.FOOT:
                    SetConversionUnitsParameters(model, unit, "foot", 304.8, IfcUnitEnum.LENGTHUNIT, IfcSIUnitName.METRE,
                                                 IfcSIPrefix.MILLI, GetLengthDimension(model));
                    break;
                case ConversionBasedUnit.YARD:
                    SetConversionUnitsParameters(model, unit, "yard", 914, IfcUnitEnum.LENGTHUNIT, IfcSIUnitName.METRE,
                                                 IfcSIPrefix.MILLI, GetLengthDimension(model));
                    break;
                case ConversionBasedUnit.MILE:
                    SetConversionUnitsParameters(model, unit, "mile", 1609, IfcUnitEnum.LENGTHUNIT, IfcSIUnitName.METRE,
                                                 null, GetLengthDimension(model));
                    break;
                case ConversionBasedUnit.ACRE:
                    SetConversionUnitsParameters(model, unit, "acre", 4046.86, IfcUnitEnum.AREAUNIT,
                                                 IfcSIUnitName.SQUARE_METRE, null, GetAreaDimension(model));
                    break;
                case ConversionBasedUnit.LITRE:
                    SetConversionUnitsParameters(model, unit, "litre", 0.001, IfcUnitEnum.VOLUMEUNIT,
                                                 IfcSIUnitName.CUBIC_METRE, null, GetVolumeDimension(model));
                    break;
                case ConversionBasedUnit.PINT_UK:
                    SetConversionUnitsParameters(model, unit, "pint UK", 0.000568, IfcUnitEnum.VOLUMEUNIT,
                                                 IfcSIUnitName.CUBIC_METRE, null, GetVolumeDimension(model));
                    break;
                case ConversionBasedUnit.PINT_US:
                    SetConversionUnitsParameters(model, unit, "pint US", 0.000473, IfcUnitEnum.VOLUMEUNIT,
                                                 IfcSIUnitName.CUBIC_METRE, null, GetVolumeDimension(model));
                    break;
                case ConversionBasedUnit.GALLON_UK:
                    SetConversionUnitsParameters(model, unit, "gallon UK", 0.004546, IfcUnitEnum.VOLUMEUNIT,
                                                 IfcSIUnitName.CUBIC_METRE, null, GetVolumeDimension(model));
                    break;
                case ConversionBasedUnit.GALLON_US:
                    SetConversionUnitsParameters(model, unit, "gallon US", 0.003785, IfcUnitEnum.VOLUMEUNIT,
                                                 IfcSIUnitName.CUBIC_METRE, null, GetVolumeDimension(model));
                    break;
                case ConversionBasedUnit.OUNCE:
                    SetConversionUnitsParameters(model, unit, "ounce", 28.35, IfcUnitEnum.MASSUNIT, IfcSIUnitName.GRAM,
                                                 null, GetMassDimension(model));
                    break;
                case ConversionBasedUnit.POUND:
                    SetConversionUnitsParameters(model, unit, "pound", 0.454, IfcUnitEnum.MASSUNIT, IfcSIUnitName.GRAM,
                                                 IfcSIPrefix.KILO, GetMassDimension(model));
                    break;
            }

            return unit;
        }

        private static void SetConversionUnitsParameters(IModel model, IfcConversionBasedUnit unit, IfcLabel name,
                                                         IfcReal ratio, IfcUnitEnum unitType, IfcSIUnitName siUnitName,
                                                         IfcSIPrefix? siUnitPrefix, IfcDimensionalExponents dimensions)
        {
            unit.Name = name;
            unit.ConversionFactor = model.New<IfcMeasureWithUnit>();
            unit.ConversionFactor.ValueComponent = ratio;
            unit.ConversionFactor.UnitComponent = model.New<IfcSIUnit>(s =>
                                                                           {
                                                                               s.UnitType = unitType;
                                                                               s.Name = siUnitName;
                                                                               s.Prefix = siUnitPrefix;
                                                                           });
            unit.Dimensions = dimensions;
        }

        private static IfcDimensionalExponents GetLengthDimension(IModel model)
        {
            IfcDimensionalExponents dimension = model.New<IfcDimensionalExponents>();
            dimension.AmountOfSubstanceExponent = 0;
            dimension.ElectricCurrentExponent = 0;
            dimension.LengthExponent = 1;
            dimension.LuminousIntensityExponent = 0;
            dimension.MassExponent = 0;
            dimension.ThermodynamicTemperatureExponent = 0;
            dimension.TimeExponent = 0;

            return dimension;
        }

        private static IfcDimensionalExponents GetVolumeDimension(IModel model)
        {
            IfcDimensionalExponents dimension = model.New<IfcDimensionalExponents>();
            dimension.AmountOfSubstanceExponent = 0;
            dimension.ElectricCurrentExponent = 0;
            dimension.LengthExponent = 3;
            dimension.LuminousIntensityExponent = 0;
            dimension.MassExponent = 0;
            dimension.ThermodynamicTemperatureExponent = 0;
            dimension.TimeExponent = 0;

            return dimension;
        }

      
        private static IfcDimensionalExponents GetAreaDimension(IModel model)
        {
            IfcDimensionalExponents dimension = model.New<IfcDimensionalExponents>();
            dimension.AmountOfSubstanceExponent = 0;
            dimension.ElectricCurrentExponent = 0;
            dimension.LengthExponent = 2;
            dimension.LuminousIntensityExponent = 0;
            dimension.MassExponent = 0;
            dimension.ThermodynamicTemperatureExponent = 0;
            dimension.TimeExponent = 0;

            return dimension;
        }

        private static IfcDimensionalExponents GetMassDimension(IModel model)
        {
            IfcDimensionalExponents dimension = model.New<IfcDimensionalExponents>();
            dimension.AmountOfSubstanceExponent = 0;
            dimension.ElectricCurrentExponent = 0;
            dimension.LengthExponent = 0;
            dimension.LuminousIntensityExponent = 0;
            dimension.MassExponent = 1;
            dimension.ThermodynamicTemperatureExponent = 0;
            dimension.TimeExponent = 0;

            return dimension;
        }
    }

    public enum ConversionBasedUnit
    {
        INCH,
        FOOT,
        YARD,
        MILE,
        ACRE,
        LITRE,
        PINT_UK,
        PINT_US,
        GALLON_UK,
        GALLON_US,
        OUNCE,
        POUND
    }
}
