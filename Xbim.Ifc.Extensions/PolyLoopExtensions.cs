#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc.Extensions
// Filename:    PolyLoopExtensions.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Collections.Generic;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.TopologyResource;

#endregion

namespace Xbim.Ifc2x3.Extensions
{
    public static class PolyLoopExtensions
    {
        public static IfcAreaMeasure Area(this IfcPolyLoop loop, IfcDirection normal)
        {
            IfcCartesianPoint sum = new IfcCartesianPoint(0, 0, 0);
            IList<IfcCartesianPoint> pts = loop.Polygon;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                sum.Add(pts[i].CrossProduct(pts[i + 1]));
            }
            IfcDirection n = normal.Normalise();

            return n.DotProduct(sum)/2;
        }
    }
}