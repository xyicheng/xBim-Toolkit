using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.PresentationAppearanceResource;

namespace Xbim.IO
{
    public static class IfcRepresentationItemExtensions
    {
        public static IfcSurfaceStyle SurfaceStyle(this IfcRepresentationItem repItem)
        {
            IfcStyledItem styledItem = repItem.ModelOf.Instances.Where<IfcStyledItem>(s => s.Item == repItem).FirstOrDefault();
            if (styledItem != null)
            {
                foreach (var presStyle in styledItem.Styles)
                {
                    IfcSurfaceStyle aSurfaceStyle = presStyle.Styles.OfType<IfcSurfaceStyle>().FirstOrDefault();
                    if (aSurfaceStyle != null) return aSurfaceStyle;
                }

            }
            return null;
        }

      
    }
}
