using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Presentation.LayerStyling
{
    public class LayerStylerTypeAndIFCStyle : ILayerStyler
    {
        private Xbim.IO.GroupingAndStyling.TypeAndStyle LayerGrouper { get; set; }

        public LayerStylerTypeAndIFCStyle()
        {
            UseIfcSubStyles = true;
            LayerGrouper = new Xbim.IO.GroupingAndStyling.TypeAndStyle();
        }

        public Dictionary<string, XbimGeometryHandleCollection> GroupLayers(XbimGeometryHandleCollection InputHandles)
        {
            return LayerGrouper.GroupLayers(InputHandles);
        }

        public ModelGeometry.Scene.XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> GetLayer(
            string layerKey, 
            XbimModel model,
            XbimScene<WpfMeshGeometry3D, WpfMaterial> scene
            )
        {
            XbimColour colour = scene.LayerColourMap[layerKey];
            return new XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>(model, colour) { Name = layerKey };
        }

        public bool UseIfcSubStyles { get; set; }
    }
}
