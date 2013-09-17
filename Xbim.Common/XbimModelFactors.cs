using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.Common
{
    public class XbimModelFactors
    {
        /// <summary>
        /// The defection on a curve when triangulating the model
        /// </summary>
        public double DeflectionTolerance;    
        /// <summary>
        /// Convert degrees to radians
        /// </summary>
        readonly public double AngleToRadiansConversionFactor;
        /// <summary>
        /// Conversion to metres
        /// </summary>
        readonly public double LengthToMetresConversionFactor;
        /// <summary>
        /// Used to display a vertex this is the diameter that will be used to auto-generate a geometric representation of a topological vertex
        /// </summary>
        readonly public double VertexPointDiameter;
        /// <summary>
        /// The maximum number of faces to sew and check the result is a valid BREP, face sets with more than this number of faces will be processed as read from the model
        /// </summary>
        public int MaxBRepSewFaceCount;
        /// <summary>
        /// The  normal tolerance under which two given points are still assumed to be identical
        /// </summary>
        readonly public double Precision;
        /// <summary>
        /// Returns the value for one metre in the units of the model
        /// </summary>
        /// /// <summary>
        /// The  maximum tolerance under which two given points are still assumed to be identical
        /// </summary>
        readonly public double PrecisionMax;
        readonly public double OneMetre;
        /// <summary>
        /// Returns the value for one millimetre in the units of the model
        /// </summary>
        readonly public double OneMilliMetre;
        public XbimModelFactors(double angToRads, double lenToMeter, double? precision = null)
        {
            AngleToRadiansConversionFactor = angToRads;
            LengthToMetresConversionFactor = lenToMeter;
           
            OneMetre = 1/lenToMeter;
            OneMilliMetre = OneMetre / 1000;
            DeflectionTolerance = OneMilliMetre*10; //10mm chord deflection
            VertexPointDiameter = OneMilliMetre * 10; //1 cm
            if (precision.HasValue)
                Precision = Math.Min(precision.Value,OneMilliMetre / 1000);
            else
                Precision = OneMilliMetre / 1000;
            PrecisionMax = OneMilliMetre / 10;
            MaxBRepSewFaceCount = 1024;
        }
    }
}
