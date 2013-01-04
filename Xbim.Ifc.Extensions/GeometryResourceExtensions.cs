using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.XbimExtensions.SelectTypes;

namespace Xbim.Ifc2x3.Extensions
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
