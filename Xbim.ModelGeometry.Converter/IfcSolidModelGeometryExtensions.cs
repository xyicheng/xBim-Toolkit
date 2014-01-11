using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common.Exceptions;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions;

namespace Xbim.ModelGeometry.Converter
{
    public static class IfcSolidModelGeometryExtensions
    {
      

        public static IXbimGeometryModelGroup Geometry3D(this IfcSolidModel solid, XbimGeometryType xbimGeometryType)
        {
            IXbimGeometryEngine engine = solid.ModelOf.GeometryEngine();
            if (engine != null) return engine.GetGeometry3D(solid, xbimGeometryType);
            else return XbimEmptyGeometryGroup.Empty;
        }

        #region Geometry Hashing

        /// <summary>
        /// Returns a Hash Code for the geometric properties of this object
        /// </summary>
        /// <param name="solid"></param>
        /// <returns></returns>
        public static int GetGeometryHashCode(this IfcSolidModel solid)
        {
            if (solid is IfcExtrudedAreaSolid)
                return ((IfcExtrudedAreaSolid)solid).GetGeometryHashCode();
            else if( solid is IfcFacetedBrep)
                return ((IfcFacetedBrep)solid).GetGeometryHashCode();
            else
            {
                throw new XbimGeometryException("Unsupported solid geometry tpype " + solid.GetType().Name);
            }
        }

        /// <summary>
        /// Compares two objects for geomtric equality
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b">object to compare with</param>
        /// <returns></returns>
        public static bool GeometricEquals(this IfcSolidModel a, IfcRepresentationItem b)
        {
            if (a is IfcExtrudedAreaSolid)
                return ((IfcExtrudedAreaSolid)a).GeometricEquals(b);
            else if (a is IfcFacetedBrep)
                return ((IfcFacetedBrep)a).GeometricEquals(b);
            else
            {
                throw new XbimGeometryException("Unsupported solid geometry tpype " + a.GetType().Name);
            }
        }
        #endregion

    }



}
