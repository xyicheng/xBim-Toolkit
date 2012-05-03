#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc.Extensions
// Filename:    SurfaceStyleExtensions.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Linq;
using System.Windows.Media.Media3D;
using Xbim.Ifc.PresentationAppearanceResource;
using Xbim.XbimExtensions;

#endregion

namespace Xbim.Presentation
{
    public static class SurfaceStyleExtensions
    {
        /// <summary>
        ///   Returns a material corresponding to this surface style, materials are cached in the ModelDataProvider
        /// </summary>
        /// <param name = "sStyle"></param>
        /// <param name = "mdp"></param>
        /// <returns></returns>
        public static Material ToMaterial(this IfcSurfaceStyle sStyle, ModelDataProvider mdp)
        {
            //need to change this to return a material group that considers all types of Styles
            IfcSurfaceStyleShading shading = sStyle.Styles.OfType<IfcSurfaceStyleShading>().FirstOrDefault();
            if (shading != null) return (shading).ToMaterial(mdp);
            else return null;
        }

        public static IfcSurfaceStyleShading GetSurfaceStyleShading(this IfcSurfaceStyle sStyle)
        {
            return sStyle.Styles.OfType<IfcSurfaceStyleShading>().FirstOrDefault();
        }

        public static IfcSurfaceStyleRendering GetSurfaceStyleRendering(this IfcSurfaceStyle sStyle)
        {
            return sStyle.Styles.OfType<IfcSurfaceStyleRendering>().FirstOrDefault();
        }

        public static IfcSurfaceStyleLighting GetSurfaceStyleLighting(this IfcSurfaceStyle sStyle)
        {
            return sStyle.Styles.OfType<IfcSurfaceStyleLighting>().FirstOrDefault();
        }

        public static IfcSurfaceStyleRefraction GetSurfaceStyleRefraction(this IfcSurfaceStyle sStyle)
        {
            return sStyle.Styles.OfType<IfcSurfaceStyleRefraction>().FirstOrDefault();
        }

        public static IfcSurfaceStyleWithTextures GetSurfaceStyleWithTextures(this IfcSurfaceStyle sStyle)
        {
            return sStyle.Styles.OfType<IfcSurfaceStyleWithTextures>().FirstOrDefault();
        }

        public static IfcExternallyDefinedSurfaceStyle GetExternallyDefinedSurfaceStyle(this IfcSurfaceStyle sStyle)
        {
            return sStyle.Styles.OfType<IfcExternallyDefinedSurfaceStyle>().FirstOrDefault();
        }
    }
}