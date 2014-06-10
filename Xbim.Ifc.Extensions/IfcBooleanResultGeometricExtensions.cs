using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.GeometryResource;

namespace Xbim.Ifc2x3.Extensions
{
    public static class IfcBooleanResultGeometricExtensions
    {
        /// <summary>
        /// returns a Hash for the geometric behaviour of this object
        /// </summary>
        /// <param name="solid"></param>
        /// <returns></returns>
        public static int GetGeometryHashCode(this  IfcBooleanResult bResult)
        {
            return Math.Abs(bResult.FirstOperand.EntityLabel) ^ Math.Abs(bResult.SecondOperand.EntityLabel); //good enough for most
        }

        /// <summary>
        /// Compares two objects for geomtric equality
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b">object to compare with</param>
        /// <returns></returns>
        public static bool GeometricEquals(this  IfcBooleanResult a, IfcRepresentationItem b)
        {
            IfcBooleanResult p = b as IfcBooleanResult;
            if (p == null) return false; //different types are not the same
            if(a.Equals(p)) return true;
            return Math.Abs(a.FirstOperand.EntityLabel) == Math.Abs(p.FirstOperand.EntityLabel) &&
                 Math.Abs(a.SecondOperand.EntityLabel) == Math.Abs(p.SecondOperand.EntityLabel) &&
                 a.Operator == p.Operator;

        }
    }
}
