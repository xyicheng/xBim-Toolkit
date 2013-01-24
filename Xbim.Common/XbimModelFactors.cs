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
        readonly public double VertxPointDiameter;
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
           
            OneMetre = 1/lenToMeter;
            OneMilliMetre = OneMetre / 1000;
            DeflectionTolerance = OneMilliMetre*10; //10mm deflection
            WireTolerance = OneMilliMetre / 1000; //0.001mm
            VertxPointDiameter = OneMilliMetre * 10; //1 cm
        }
    }
}
