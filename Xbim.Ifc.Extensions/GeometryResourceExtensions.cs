using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.GeometryResource;
using Xbim.Ifc.SelectTypes;

namespace Xbim.Ifc.Extensions
{
    static public class GeometryResourceExtensions
    {
        static public IfcCircle Create(this IfcCircle c, IfcAxis2Placement position, double radius)
        {
            IfcCircle circle = new IfcCircle()
            {
                Position = position,
                Radius = radius
            };
            return circle;
        }
    }
}
