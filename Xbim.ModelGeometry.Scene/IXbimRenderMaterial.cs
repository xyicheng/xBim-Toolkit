using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.PresentationAppearanceResource;
using Xbim.Ifc2x3.PresentationResource;

namespace Xbim.ModelGeometry.Scene
{
    /// <summary>
    /// Interface for grpahic card specific render materials or shaders
    /// </summary>
    public interface IXbimRenderMaterial
    {
        /// <summary>
        /// Creates a native material for the specified style
        /// </summary>
        /// <param name="surfaceStyle"></param>
        void CreateMaterial(IfcSurfaceStyle surfaceStyle);
        void CreateMaterial(IfcColourRgb colour);
        void CreateMaterial(IfcSurfaceStyleRendering rendering);
        void CreateMaterial(IfcSurfaceStyleShading shading);
        /// <summary>
        /// Creates a material colour that default to opaque white
        /// </summary>
        /// <param name="red"></param>
        /// <param name="green"></param>
        /// <param name="blue"></param>
        /// <param name="alpha"></param>
        void CreateMaterial(byte red = 255, byte green = 255, byte blue = 255, byte alpha = 255);
        void CreateMaterial(float red = 255, float green = 255, float blue = 255, float alpha = 255, float emit = 0);
        /// <summary>
        /// Returns true if the material is transparent
        /// </summary>
        bool IsTransparent { get; }

        /// <summary>
        /// Returns true if the material is double sided, i.e. the front and back faces are both rendered the same material
        /// </summary>
        bool RenderBothFaces { get; }

        /// <summary>
        /// True if the front and rear faces are reversed
        /// </summary>
        bool SwitchFrontAndRearFaces { get; }

        void CreateMaterial(XbimColour colour);
    }
}
