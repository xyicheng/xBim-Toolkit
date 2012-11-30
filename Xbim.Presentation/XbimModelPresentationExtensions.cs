using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using Xbim.Ifc2x3.PresentationAppearanceResource;
using Xbim.IO;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.Presentation
{
    public static class XbimModelPresentationExtensions
    {
        public static XbimMaterialProvider GetRenderMaterial(this XbimModel model, XbimGeometryData geomData)
        {
            if (geomData.StyleLabel > 0)
            {
                IfcSurfaceStyle surfaceStyle = model.Instances[geomData.StyleLabel] as IfcSurfaceStyle;
                if (surfaceStyle != null)
                    return new XbimMaterialProvider(surfaceStyle.ToMaterial());
                
            }
            //nothing specificgo for default of type
            return ModelDataProvider.GetDefaultMaterial(IfcMetaData.IfcType(geomData.IfcTypeId));
        }
    }
}
