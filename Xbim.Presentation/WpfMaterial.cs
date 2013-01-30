using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.PresentationAppearanceResource;
using Xbim.Ifc2x3.PresentationResource;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Presentation
{
    public class WpfMaterial : IXbimRenderMaterial
    {
        Material Material;
        bool isTransparent = false;
        bool renderBothFaces = true;
        bool switchFrontAndRearFaces = false;
        public static implicit operator Material(WpfMaterial wpfMaterial)
        {
            return wpfMaterial.Material;
        }
        public void CreateMaterial(IfcSurfaceStyle surfaceStyle)
        {
            //set render one or both faces
            renderBothFaces = (surfaceStyle.Side == IfcSurfaceSide.BOTH);
            //switch if required
            switchFrontAndRearFaces = (surfaceStyle.Side == IfcSurfaceSide.NEGATIVE);
            //need to change this to return a material group that considers combinations of Styles
            IfcSurfaceStyleRendering rendering = surfaceStyle.Styles.OfType<IfcSurfaceStyleRendering>().FirstOrDefault();
            if (rendering != null) 
                CreateMaterial(rendering);
            else //try the shading
            {
                IfcSurfaceStyleShading shading = surfaceStyle.Styles.OfType<IfcSurfaceStyleShading>().FirstOrDefault();
                if (shading != null) CreateMaterial(rendering);
            }
            ///no luck
        }

        public void CreateMaterial(IfcSurfaceStyleRendering rendering)
        { 
            MaterialGroup grp = new MaterialGroup();
            if (rendering.DiffuseColour is IfcNormalisedRatioMeasure)
            {
                Brush brush = new SolidColorBrush(rendering.SurfaceColour.ToColor((IfcNormalisedRatioMeasure)rendering.DiffuseColour));
                brush.Opacity = rendering.Transparency.HasValue ? 1.0 - rendering.Transparency.Value : 1.0;
                grp.Children.Add(new DiffuseMaterial(brush));
                isTransparent |= brush.Opacity < 1.0;
            }
            else if (rendering.DiffuseColour is IfcColourRgb)
            {
                Brush brush = new SolidColorBrush(((IfcColourRgb)rendering.DiffuseColour).ToColor());
                brush.Opacity = rendering.Transparency.HasValue ? 1.0 - rendering.Transparency.Value : 1.0;
                grp.Children.Add(new DiffuseMaterial(brush));
                isTransparent |= brush.Opacity < 1.0;
            }
            else if (rendering.DiffuseColour == null)
            {
                Brush brush = new SolidColorBrush(rendering.SurfaceColour.ToColor());
                brush.Opacity = rendering.Transparency.HasValue ? 1.0 - rendering.Transparency.Value : 1.0;
                grp.Children.Add(new DiffuseMaterial(brush));
                isTransparent |= brush.Opacity < 1.0;
            }

            if (rendering.SpecularColour is IfcNormalisedRatioMeasure)
            {
                Brush brush = new SolidColorBrush(rendering.SurfaceColour.ToColor());
                brush.Opacity = rendering.Transparency.HasValue ? 1.0 - rendering.Transparency.Value : 1.0;
                grp.Children.Add(new SpecularMaterial(brush, (IfcNormalisedRatioMeasure)(rendering.SpecularColour)));
                isTransparent |= brush.Opacity < 1.0;
            }
            if (rendering.SpecularColour is IfcColourRgb)
            {
                Brush brush = new SolidColorBrush(((IfcColourRgb)rendering.SpecularColour).ToColor());
                brush.Opacity = rendering.Transparency.HasValue ? 1.0 - rendering.Transparency.Value : 1.0;
                grp.Children.Add(new SpecularMaterial(brush, 100.0));
                isTransparent |= brush.Opacity < 1.0;
            }

            if (grp.Children.Count == 1)
            {
                Material = grp.Children[0];
            }
            else
            {
                Material = grp;
            }
        }

        public void CreateMaterial(IfcSurfaceStyleShading shading)
        {
            if (shading is IfcSurfaceStyleRendering)
                CreateMaterial((IfcSurfaceStyleRendering)shading);
            else
            {
                CreateMaterial(shading.SurfaceColour);
            }
        }

        public void CreateMaterial(IfcColourRgb colourRGB)
        {
            byte red = Convert.ToByte(colourRGB.Red * 255);
            byte green = Convert.ToByte(colourRGB.Green * 255);
            byte blue = Convert.ToByte(colourRGB.Blue * 255);
            CreateMaterial(red, green, blue);
        }


        public void CreateMaterial(byte red, byte green, byte blue, byte alpha = 255)
        {
            Color col = Color.FromArgb(alpha, red, green, blue);
            Brush brush = new SolidColorBrush(col);
            Material = new DiffuseMaterial(brush);
            isTransparent = alpha < 255;
        }

        /// <summary>
        /// creates a material from SC RGB data
        /// </summary>
        /// <param name="red"></param>
        /// <param name="green"></param>
        /// <param name="blue"></param>
        /// <param name="alpha"></param>
        public void CreateMaterial(float red, float green, float blue, float alpha = 1, float emit = 0)
        {
            Color col = Color.FromScRgb(alpha, red, green, blue);
            Brush brush = new SolidColorBrush(col);
            if (emit > 0)
                Material = new EmissiveMaterial(brush);        
            else
                Material = new DiffuseMaterial(brush);
            isTransparent = alpha < 1;
        }


        public void CreateMaterial(XbimColour colour)
        {
            CreateMaterial(colour.Red, colour.Green, colour.Blue, colour.Alpha, colour.Emit);
        }

        public bool IsTransparent
        {
            get { return isTransparent; }
            set { isTransparent = value; }
        }

        public bool RenderBothFaces
        {
            get { return renderBothFaces; }
            set { renderBothFaces = value; }
        }

        public bool SwitchFrontAndRearFaces
        {
            get { return switchFrontAndRearFaces; }
            set { switchFrontAndRearFaces = value; }
        }


    }
}
