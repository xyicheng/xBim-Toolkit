using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.TopologyResource;

namespace Xbim.Ifc2x3.Extensions
{
    public static class ConnectedFaceSetExtensions
    {
        public static void Bounds(this IfcConnectedFaceSet fSet,
            out double Xmin, out double Ymin, out double Zmin, out double Xmax, out double Ymax, out double Zmax)
        {
            Xmin = 0; Ymin = 0; Zmin = 0; Xmax = 0; Ymax = 0; Zmax = 0;
            foreach (var face in fSet.CfsFaces)
            {
                IfcFaceBound outer = face.Bounds.OfType<IfcFaceOuterBound>().FirstOrDefault();
                if (outer == null) outer = face.Bounds.FirstOrDefault();
                if (outer == null) break;
                IfcPolyLoop loop = outer.Bound as IfcPolyLoop;
                if (loop != null)
                {
                    foreach (var pt in loop.Polygon)
                    {
                        Xmin = Math.Min(Xmin,pt.X);
                        Ymin = Math.Min(Ymin,pt.Y);
                        Zmin = Math.Min(Zmin,pt.Z);
                        Xmax = Math.Max(Xmax, pt.X);
                        Ymax = Math.Max(Ymax, pt.Y);
                        Zmax = Math.Max(Zmax, pt.Z);
                    }
                }
                
            }
        }
    }
}
