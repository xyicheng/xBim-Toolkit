using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Kernel;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions;

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
    }
}
