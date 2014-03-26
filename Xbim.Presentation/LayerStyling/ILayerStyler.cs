using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.IO;
using Xbim.IO.GroupingAndStyling;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Presentation.LayerStyling
{
    /// <summary>
    /// Interface defining the functions needed to group and style elements to be visualised in the WPF 3D component.
    /// Note that it inherits from IGeomHandlesGrouping
    /// </summary>
    public interface ILayerStyler : IGeomHandlesGrouping
    {
        /// <summary>
        /// returns a layer for the specified key 
        /// </summary>
        /// <param name="LayerKey">It's a string that is generated in the GroupLayers function of the IGeomHandlesGrouping interface.</param>
        ModelGeometry.Scene.XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> GetLayer(
            string LayerKey, 
            XbimModel model,
            XbimScene<WpfMeshGeometry3D, WpfMaterial> scene
            );

        /// <summary>
        /// Determines whether the engine creates sublayers depending on IFC styles in the model.
        /// </summary>
        bool UseIfcSubStyles { get; }

        /// <summary>
        /// Returns a bool determining the visibility of a layer.
        /// </summary>
        /// <param name="key">Similar to layerkey in GetLayer</param>
        /// <returns></returns>
        bool IsVisibleLayer(string key);
    }
}
