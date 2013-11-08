using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.GeometricModelResource;
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
    }
}
