using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.PresentationAppearanceResource;
using Xbim.Ifc2x3.PresentationResource;

namespace Xbim.ModelGeometry.Scene
{
    /// <summary>
    /// Class to hold the surface style or texture of an object, corresponds to IfcSurfaceStyle and OpenGL Texture
    /// Does not handle bitmap textured surfaces etc at present
    /// </summary>
    public class XbimTexture 
    {
        public XbimColourMap ColourMap = new XbimColourMap();
        
        bool renderBothFaces = true;
        bool switchFrontAndRearFaces = false;

        public XbimColour DiffuseTransmissionColour;
        public XbimColour TransmissionColour;
        public XbimColour DiffuseReflectionColour;
        public XbimColour ReflectanceColour;

        public XbimTexture CreateTexture(IfcSurfaceStyle surfaceStyle)
        {
            //set render one or both faces
            renderBothFaces = (surfaceStyle.Side == IfcSurfaceSide.BOTH);
            //switch if required
            switchFrontAndRearFaces = (surfaceStyle.Side == IfcSurfaceSide.NEGATIVE);
            ColourMap.Clear();
            foreach (var style in surfaceStyle.Styles)
            {
                if (style is IfcSurfaceStyleRendering) AddColour((IfcSurfaceStyleRendering)style);
                else if (style is IfcSurfaceStyleShading) AddColour((IfcSurfaceStyleShading)style);
                else if (style is IfcSurfaceStyleLighting) AddLighting((IfcSurfaceStyleLighting)style);
            }
            return this;
        }

        private void AddColour(IfcSurfaceStyleShading shading)
        {
            ColourMap.Add(new XbimColour(shading.SurfaceColour));
        }

        private void AddColour(IfcSurfaceStyleRendering rendering)
        {
            if (rendering.DiffuseColour is IfcNormalisedRatioMeasure)
            {
                ColourMap.Add(new XbimColour(
                    rendering.SurfaceColour,
                    rendering.Transparency.HasValue ? 1.0 - rendering.Transparency.Value : 1.0,
                    (IfcNormalisedRatioMeasure)rendering.DiffuseColour
                    ));

            }
            else if (rendering.DiffuseColour is IfcColourRgb)
            {
                ColourMap.Add(new XbimColour(
                    (IfcColourRgb)rendering.DiffuseColour,
                    rendering.Transparency.HasValue ? 1.0 - rendering.Transparency.Value : 1.0
                    ));

            }
            else if (rendering.DiffuseColour == null)
            {
                ColourMap.Add(new XbimColour(
                    rendering.SurfaceColour,
                    rendering.Transparency.HasValue ? 1.0 - rendering.Transparency.Value : 1.0
                    ));
            }

            if (rendering.SpecularColour is IfcNormalisedRatioMeasure)
            {
                ColourMap.Add(new XbimColour(
                    rendering.SurfaceColour,
                    rendering.Transparency.HasValue ? 1.0 - rendering.Transparency.Value : 1.0,
                    1.0,
                    (IfcNormalisedRatioMeasure)(rendering.SpecularColour)
                    ));
            }
            if (rendering.SpecularColour is IfcColourRgb)
            {
                ColourMap.Add(new XbimColour(
                    (IfcColourRgb)rendering.SpecularColour,
                    rendering.Transparency.HasValue ? 1.0 - rendering.Transparency.Value : 1.0
                    ));

            }
        }

        private void AddLighting(IfcSurfaceStyleLighting lighting)
        {
            DiffuseReflectionColour = new XbimColour(lighting.DiffuseReflectionColour);
            DiffuseTransmissionColour = new XbimColour(lighting.DiffuseTransmissionColour);
            TransmissionColour = new XbimColour(lighting.TransmissionColour);
            ReflectanceColour = new XbimColour(lighting.ReflectanceColour);
        }


        public XbimTexture CreateTexture(IfcColourRgb colour)
        {
            ColourMap.Clear();
            ColourMap.Add(new XbimColour(colour));
            return this;
        }

        public XbimTexture CreateTexture(IfcSurfaceStyleRendering rendering)
        {
            ColourMap.Clear();
            AddColour(rendering);
            return this;
            
        }

        public XbimTexture CreateTexture(IfcSurfaceStyleShading shading)
        {
            ColourMap.Clear();
            if (shading is IfcSurfaceStyleRendering)
                AddColour((IfcSurfaceStyleRendering)shading);
            else
                AddColour(shading);
            return this;
        }

        public XbimTexture CreateTexture(byte red = 255, byte green = 255, byte blue = 255, byte alpha = 255)
        {
            ColourMap.Clear();
            ColourMap.Add(new XbimColour("C1", red, green, blue, alpha));
            return this;
        }

        public XbimTexture CreateTexture(float red = 255, float green = 255, float blue = 255, float alpha = 255)
        {
            ColourMap.Clear();
            ColourMap.Add(new XbimColour("C1", red, green, blue, alpha));
            return this;
        }

        public bool IsTransparent
        {
            get { return ColourMap.IsTransparent; }
        }

        public bool RenderBothFaces
        {
            get { return renderBothFaces; }
        }

        public bool SwitchFrontAndRearFaces
        {
            get { return switchFrontAndRearFaces; }
        }

        public XbimTexture CreateTexture(XbimColour colour)
        {
            ColourMap.Clear();
            AddColour(colour);
            return this;
        }

        private void AddColour(XbimColour colour)
        {
            if (string.IsNullOrEmpty(colour.Name))
                colour.Name = "C" + (ColourMap.Count + 1);
            ColourMap.Add(colour);
        }
    }
}
