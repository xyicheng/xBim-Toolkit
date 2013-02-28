using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Kernel;

namespace Xbim.Spatial
{
    public interface ISpatialFunctionsAnalyser
    {
        double Distance(IfcProduct first, IfcProduct second);
        GeometryStruct Buffer(IfcProduct product);
        GeometryStruct ConvexHull(IfcProduct product);
        GeometryStruct Intersection(IfcProduct first, IfcProduct second);
        GeometryStruct Union(IfcProduct first, IfcProduct second);
        GeometryStruct Difference(IfcProduct first, IfcProduct second);
        GeometryStruct SymDifference(IfcProduct first, IfcProduct second);
    }
}
