using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Kernel;

namespace Xbim.Spatial
{
    public interface ISpatialRelationsAnalyser
    {
        bool? Equals(IfcProduct first, IfcProduct second);
        bool? Disjoint(IfcProduct first, IfcProduct second);
        bool? Intersects(IfcProduct first, IfcProduct second);
        bool? Touches(IfcProduct first, IfcProduct second);
        bool? Crosses(IfcProduct first, IfcProduct second);
        bool? Within(IfcProduct first, IfcProduct second);
        bool? Contains(IfcProduct first, IfcProduct second);
        bool? Overlaps(IfcProduct first, IfcProduct second);
        bool? Relate(IfcProduct first, IfcProduct second);
        
    }
}
