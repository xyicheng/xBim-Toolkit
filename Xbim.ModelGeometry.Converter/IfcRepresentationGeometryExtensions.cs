using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.RepresentationResource;

using Xbim.ModelGeometry.Scene;

namespace Xbim.ModelGeometry.Converter
{
    public static class IfcRepresentationGeometryExtensions
    {
        public static IXbimGeometryModelGroup Geometry3D(this IfcRepresentation representation)
        {
            IXbimGeometryEngine engine = representation.ModelOf.GeometryEngine();
            if (engine != null) return engine.GetGeometry3D(representation);
            else return XbimEmptyGeometryGroup.Empty;
        }
    }
}
