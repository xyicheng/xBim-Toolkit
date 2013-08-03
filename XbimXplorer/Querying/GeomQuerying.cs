using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common.Geometry;
using Xbim.IO;
using Xbim.XbimExtensions;

namespace XbimXplorer.Querying
{
    public static class GeomQuerying
    {
        public static string GeomInfoBoundBox(XbimModel model, int iEntLabel)
        {
            XbimGeometryData geomdata = model.GetGeometryData(iEntLabel, XbimGeometryType.BoundingBox).FirstOrDefault();
            if (geomdata == null)
                return "<not found>";
            
            XbimRect3D r3d = XbimRect3D.FromArray(geomdata.ShapeData);
            return string.Format("Bounding box (position, size): {0} \r\n", r3d.ToString());
            // XbimRect3D transformed = r3d.Transform(composed);
            
            
        }
    }
}
