using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common.Exceptions;
using Xbim.Ifc2x3.TopologyResource;

namespace Xbim.ModelGeometry.Converter
{
    public static class IfcLoopGeometricExtensions
    {
        /// <summary>
        /// returns a Hash for the geometric behaviour of this object
        /// </summary>
        /// <param name="solid"></param>
        /// <returns></returns>
        public static int GetGeometryHashCode(this IfcLoop loop)
        {
            IfcPolyLoop polyLoop = loop as IfcPolyLoop;
            if (polyLoop != null)
                return polyLoop.GetGeometryHashCode();
            else
                return loop.GetType().Name.GetHashCode();

            //    throw new XbimGeometryException("Only loops of type IfcPolyLoop are currently supported");
        }

        /// <summary>
        /// Compares two objects for geometric equality
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b">object to compare with</param>
        /// <returns></returns>
        public static bool GeometricEquals(this IfcLoop a, IfcLoop b)
        {
            if (a.Equals(b)) return true;
            IfcPolyLoop aLoop = a as IfcPolyLoop;
            IfcPolyLoop bLoop = b as IfcPolyLoop;
            if (aLoop != null && bLoop != null)
                return aLoop.GeometricEquals(bLoop);
            else
                throw new XbimGeometryException("Only loops of type IfcPolyLoop are currently supported");
        }
    }
}
