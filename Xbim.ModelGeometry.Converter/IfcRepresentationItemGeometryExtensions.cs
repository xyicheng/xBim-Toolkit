using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.ModelGeometry.Scene;

namespace Xbim.ModelGeometry.Converter
{
    public static class IfcRepresentationItemGeometryExtensions
    {
        public static IXbimGeometryModelGroup Geometry3D(this IfcRepresentationItem repItem)
        {
            IXbimGeometryEngine engine = repItem.ModelOf.GeometryEngine();
            if (engine != null) return engine.GetGeometry3D(repItem);
            else return XbimEmptyGeometryGroup.Empty;
        }
    }
}
