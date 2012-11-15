using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.Common
{
    public class XbimModelFactors
    {

        readonly public double WireTolerance;
        readonly public double DeflectionTolerance;    
        readonly public double AngleToRadiansConversionFactor;
        readonly public double LengthToMetresConversionFactor;
        /// <summary>
        /// Returns the value for one metre in the units of the model
        /// </summary>
        readonly public double OneMetre;
        /// <summary>
        /// Returns the value for one millimetre in the units of the model
        /// </summary>
        readonly public double OneMilliMetre;
        public XbimModelFactors(double angToRads, double lenToMeter)
        {
            AngleToRadiansConversionFactor = angToRads;
            LengthToMetresConversionFactor = lenToMeter;
            WireTolerance = 0.000001 / lenToMeter; //0.001mm
            DeflectionTolerance = 0.01 / lenToMeter; //10mm deflection
            OneMetre = lenToMeter;
            OneMilliMetre = 1000 * lenToMeter;
        }
    }
}
