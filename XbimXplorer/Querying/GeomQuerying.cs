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
        }

        internal static string GeomLayers(XbimModel Model, int item, List<Xbim.ModelGeometry.Scene.XbimScene<Xbim.Presentation.WpfMeshGeometry3D, Xbim.Presentation.WpfMaterial>> scenes)
        {
            StringBuilder sb = new StringBuilder();
            // XbimMeshGeometry3D geometry = new XbimMeshGeometry3D();
            // IModel m = entity.ModelOf;
            foreach (var scene in scenes)
            {
                foreach (var layer in scene.SubLayers)
                {
                    // an entity model could be spread across many layers (e.g. in case of different materials)
                    if (layer.Model == Model)
                    {
                        foreach (var mi in layer.GetMeshInfo(item))
                        {
                            sb.AppendLine(mi.ToString());
                        }
                    }
                }    
            }
            return sb.ToString();            
        }
    }
}
