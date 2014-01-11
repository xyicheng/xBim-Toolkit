using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.GeometryResource;

namespace Xbim.ModelGeometry.Converter
{
    public static class IfcCurveGeometricExtensions
    {
        /// <summary>
        /// returns a Hash for the geometric behaviour of this object
        /// </summary>
        /// <param name="solid"></param>
        /// <returns></returns>
        public static int GetGeometryHashCode(this IfcCurve curve)
        {
            return curve.GetHashCode(); //mostly in ifc files equality is enough, might need to define hash functions for specific types later
        }

        /// <summary>
        /// Compares two objects for geometric equality
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b">object to compare with</param>
        /// <returns></returns>
        public static bool GeometricEquals(this IfcCurve a, IfcCurve b)
        {
            return a.Equals(b);
        }

    }
}
