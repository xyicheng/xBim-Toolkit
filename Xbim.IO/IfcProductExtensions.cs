using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Kernel;
using Xbim.XbimExtensions;

namespace Xbim.IO
{
    public static class IfcProductExtensions
    {
        public static XbimGeometryData GeometryData(this  IfcProduct product, XbimGeometryType geomType)
        {
            XbimModel model =  product.ModelOf as XbimModel;
            if (model != null) 
                return model.GetGeometryData(product, geomType);
            else
                return null;
        }
    }
}
