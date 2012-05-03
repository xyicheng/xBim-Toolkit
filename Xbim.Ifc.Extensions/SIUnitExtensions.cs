#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc.Extensions
// Filename:    SIUnitExtensions.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using Xbim.Ifc.MeasureResource;

#endregion

namespace Xbim.Ifc.Extensions
{
    public static class SIUnitExtensions
    {
        /// <summary>
        ///   returns the power of the SIUnit prefix, i.e. MILLI = 0.001, if undefined returns 1.0
        /// </summary>
        /// <param name = "si"></param>
        /// <returns></returns>
        public static double Power(this IfcSIUnit si)
        {
            if (si.Prefix.HasValue)
                switch (si.Prefix.Value)
                {
                    case IfcSIPrefix.EXA:
                        return 1.0e+18;
                    case IfcSIPrefix.PETA:
                        return 1.0e+15;
                    case IfcSIPrefix.TERA:
                        return 1.0e+12;
                    case IfcSIPrefix.GIGA:
                        return 1.0e+9;
                    case IfcSIPrefix.MEGA:
                        return 1.0e+6;
                    case IfcSIPrefix.KILO:
                        return 1.0e+3;
                    case IfcSIPrefix.HECTO:
                        return 1.0e+2;
                    case IfcSIPrefix.DECA:
                        return 10;
                    case IfcSIPrefix.DECI:
                        return 1.0e-1;
                    case IfcSIPrefix.CENTI:
                        return 1.0e-2;
                    case IfcSIPrefix.MILLI:
                        return 1.0e-3;
                    case IfcSIPrefix.MICRO:
                        return 1.0e-6;
                    case IfcSIPrefix.NANO:
                        return 1.0e-9;
                    case IfcSIPrefix.PICO:
                        return 1.0e-12;
                    case IfcSIPrefix.FEMTO:
                        return 1.0e-15;
                    case IfcSIPrefix.ATTO:
                        return 1.0e-18;
                    default:
                        return 1.0;
                }
            else
                return 1.0;
        }
    }
}