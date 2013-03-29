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
        
        public static implicit operator Material(WpfMaterial wpfMaterial)
        {
            return wpfMaterial.Material;
        }
       
        public void CreateMaterial(XbimTexture texture)
        {
            if (texture.ColourMap.Count > 1)
            {
                Material = new MaterialGroup();
                foreach (var colour in texture.ColourMap)
                {
                    ((MaterialGroup)Material).Children.Add(CreateMaterial(colour));
                }
            }
            else if(texture.ColourMap.Count == 1)
            {
                XbimColour colour = texture.ColourMap[0];
                Material = CreateMaterial(colour);
            }
        }

        private Material CreateMaterial(XbimColour colour)
        {
            Color col = Color.FromScRgb(colour.Alpha, colour.Red, colour.Green, colour.Blue);
            Brush brush = new SolidColorBrush(col);
            if (colour.SpecularFactor > 0)
                return new SpecularMaterial(brush, colour.SpecularFactor * 100);
            else if (colour.ReflectionFactor > 0)
                return new EmissiveMaterial(brush);
            else
                return new DiffuseMaterial(brush);
            
        }


        public bool IsCreated
        {
            get { return Material != null; }
        }
    }
}
