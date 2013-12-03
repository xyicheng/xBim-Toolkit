using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.GeometricConstraintResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.Kernel;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.Extensions;

namespace Xbim.ModelGeometry.Converter
{
    public static class IfcProductGeometryExtensions
    {
        public static IXbimGeometryModelGroup Geometry3D(this IfcProduct product, XbimGeometryType xbimGeometryType)
        {
            IXbimGeometryEngine engine = product.ModelOf.GeometryEngine();
            if (engine != null) return engine.GetGeometry3D(product, xbimGeometryType);
            else return XbimEmptyGeometryGroup.Empty;
        }

        public static IXbimGeometryModelGroup Geometry3D(this IfcProduct product)
        {
            IXbimGeometryEngine engine = product.ModelOf.GeometryEngine();
            if (engine != null) return engine.GetGeometry3D(product);
            else return XbimEmptyGeometryGroup.Empty;
        }

        /// <summary>
        /// Resolves the objects placement into a global wcs transformation.
        /// </summary>
        /// <param name="product"></param>
        /// <returns></returns>
        public static XbimMatrix3D Transform(this IfcProduct product)
        {
            if (product.ObjectPlacement != null)
                return product.ObjectPlacement.ToMatrix3D();
            else
                return XbimMatrix3D.Identity;
            
        }
    }
}
