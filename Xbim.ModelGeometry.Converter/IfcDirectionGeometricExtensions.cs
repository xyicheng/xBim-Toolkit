using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.GeometryResource;

namespace Xbim.ModelGeometry.Converter
{
    public static class IfcDirectionGeometricExtensions
    {
        /// <summary>
        /// returns a Hash for the geometric behaviour of this object
        /// </summary>
        /// <param name="solid"></param>
        /// <returns></returns>
        public static int GetGeometryHashCode(this IfcDirection dir)
        {
            switch (dir.Dim)
            {
                case 1:
                    return dir.X.GetHashCode();
                case 2:
                    return dir.X.GetHashCode() ^ dir.Y.GetHashCode();
                case 3:
                    return dir.X.GetHashCode() ^ dir.Y.GetHashCode() ^ dir.Z.GetHashCode();
                default:
                    return dir.GetHashCode();
            }
        }

        /// <summary>
        /// Compares two objects for geometric equality
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b">object to compare with</param>
        /// <returns></returns>
        public static bool GeometricEquals(this IfcDirection a, IfcDirection b)
        {
            if (a.Equals(b)) return true;
            XbimVector3D va = a.XbimVector3D();
            XbimVector3D vb = b.XbimVector3D();
            return va.IsEqual(vb,b.ModelOf.ModelFactors.Precision);
        }
    }
}
