using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace Xbim.Presentation
{
    public static class ModelVisual3DExtensions
    {
        public static void AddGeometry(this ModelVisual3D visual, GeometryModel3D geometry)
        {
            if (visual.Content == null)
                visual.Content = geometry;
            else
            {  
                if (visual.Content is Model3DGroup)
                    ((Model3DGroup)visual.Content).Children.Add(geometry);
                //it is not a group but now needs to be
                else
                {
                    Model3DGroup m3dGroup = new Model3DGroup();
                    m3dGroup.Children.Add(visual.Content);
                    m3dGroup.Children.Add(geometry);
                    visual.Content = m3dGroup;
                }
            }
        }
    }
}
