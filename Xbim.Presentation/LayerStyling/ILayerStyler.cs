using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.IO;
using Xbim.IO.GroupingAndStyling;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Presentation.LayerStyling
{
    public interface ILayerStyler : IGeomHandlesGrouping
    {
        ModelGeometry.Scene.XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> GetLayer(
            string LayerKey, 
            XbimModel model,
            XbimScene<WpfMeshGeometry3D, WpfMaterial> scene
            );

        bool UseIfcSubStyles { get; }
    }
}
