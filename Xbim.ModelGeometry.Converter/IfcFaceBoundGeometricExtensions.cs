using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.TopologyResource;

namespace Xbim.ModelGeometry.Converter
{
    public static class IfcFaceBoundGeometricExtensions
    {
        /// <summary>
        /// returns a Hash for the geometric behaviour of this object
        /// </summary>
        /// <param name="solid"></param>
        /// <returns></returns>
        public static int GetGeometryHashCode(this IfcFaceBound faceBound)
        {
            return faceBound.Bound.GetGeometryHashCode();
        }

        /// <summary>
        /// Compares two objects for geometric equality
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b">object to compare with</param>
        /// <returns></returns>
        public static bool GeometricEquals(this IfcFaceBound a, IfcFaceBound b)
        {
            if (a.Equals(b)) return true;
            return a.Bound.GeometricEquals(b.Bound);
        }
    }
}
