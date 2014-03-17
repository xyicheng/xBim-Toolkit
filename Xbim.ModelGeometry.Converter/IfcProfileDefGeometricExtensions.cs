using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common.Exceptions;
using Xbim.Ifc2x3.ProfileResource;

namespace Xbim.ModelGeometry.Converter
{
    public static class IfcProfileDefGeometricExtensions
    {
        /// <summary>
        /// returns a Hash for the geometric behaviour of this object
        /// </summary>
        /// <param name="solid"></param>
        /// <returns></returns>
        public static int GetGeometryHashCode(this IfcProfileDef profile)
        {
            if (profile is IfcRectangleProfileDef)
                return ((IfcRectangleProfileDef)profile).GetGeometryHashCode();
            else if (profile is IfcArbitraryClosedProfileDef)
                return ((IfcArbitraryClosedProfileDef)profile).GetGeometryHashCode();
            else if(profile is IfcCircleProfileDef)
                return ((IfcCircleProfileDef)profile).GetGeometryHashCode();
            else if (profile is IfcCircleHollowProfileDef)
                return ((IfcCircleHollowProfileDef)profile).GetGeometryHashCode();
            else if (profile is IfcLShapeProfileDef)
                return ((IfcLShapeProfileDef)profile).GetGeometryHashCode();
            else if (profile is IfcIShapeProfileDef)
                return ((IfcIShapeProfileDef)profile).GetGeometryHashCode();
            else
            {
                return profile.GetHashCode();
                throw new XbimGeometryException("Unsupported solid geometry type " + profile.GetType().Name);
            }
        }

        /// <summary>
        /// Compares two objects for geometric equality
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b">object to compare with</param>
        /// <returns></returns>
        public static bool GeometricEquals(this IfcProfileDef a, IfcProfileDef b)
        {
            if (a is IfcRectangleProfileDef)
                return ((IfcRectangleProfileDef)a).GeometricEquals(b);
            else if (a is IfcArbitraryClosedProfileDef)
                return ((IfcArbitraryClosedProfileDef)a).GeometricEquals(b);
            else if (a is IfcCircleProfileDef)
                return ((IfcCircleProfileDef)a).GeometricEquals(b);
            else if (a is IfcCircleHollowProfileDef)
                return ((IfcCircleHollowProfileDef)a).GeometricEquals(b);
            else if (a is IfcLShapeProfileDef)
                return ((IfcLShapeProfileDef)a).GeometricEquals(b);
            else if (a is IfcIShapeProfileDef)
                return ((IfcIShapeProfileDef)a).GeometricEquals(b);
            else
            {
                return false; //default to false
            }
        }


    }
}
