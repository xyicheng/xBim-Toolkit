using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.GeometryResource;

namespace Xbim.ModelGeometry.Converter
{
    public static class IfcCartesianPointGeometricExtensions
    {
        /// <summary>
        /// returns a Hash for the geometric behaviour of this object
        /// </summary>
        /// <param name="solid"></param>
        /// <returns></returns>
        public static int GetGeometryHashCode(this IfcCartesianPoint pt)
        {
            switch (pt.Dim)
            {
                case 1:
                    return pt.X.GetHashCode();
                case 2:
                    return pt.X.GetHashCode() ^ pt.Y.GetHashCode();
                case 3:
                    return pt.X.GetHashCode() ^ pt.Y.GetHashCode() ^ pt.Z.GetHashCode();
                default:
                    return pt.GetHashCode();
            }
        }
        /// <summary>
        /// Compares two objects for geometric equality
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b">object to compare with</param>
        /// <returns></returns>
        public static bool GeometricEquals(this IfcCartesianPoint a, IfcCartesianPoint b)
        {
            if (a.Equals(b)) return true;
            double precision = a.ModelOf.ModelFactors.Precision;
            return a.IsEqual(b, precision);
        }
    }
}
