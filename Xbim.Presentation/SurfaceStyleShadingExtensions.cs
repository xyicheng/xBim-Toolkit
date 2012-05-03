#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc.Extensions
// Filename:    SurfaceStyleShadingExtensions.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.Ifc.PresentationAppearanceResource;
using Xbim.XbimExtensions;

#endregion

namespace Xbim.Presentation
{
    public static class SurfaceStyleShadingExtensions
    {
        public static Material ToMaterial(this IfcSurfaceStyleShading shading, ModelDataProvider mdp)
        {
            if (shading is IfcSurfaceStyleRendering)
                return ((IfcSurfaceStyleRendering) shading).ToMaterial(mdp);
            else
            {
                Material mat;
                if (mdp.Materials.TryGetValue(shading, out mat)) return mat; //already made one
                else
                {
                    byte red = Convert.ToByte(shading.SurfaceColour.Red*255);
                    byte green = Convert.ToByte(shading.SurfaceColour.Green*255);
                    byte blue = Convert.ToByte(shading.SurfaceColour.Blue*255);
                    Color col = Color.FromRgb(red, green, blue);
                    Brush brush = new SolidColorBrush(col);
                    //brush.Opacity = mdp.Transparency;
                    mat = new DiffuseMaterial(brush);
                    mdp.Materials.Add(shading, mat);
                    return mat;
                }
            }
        }
    }
}