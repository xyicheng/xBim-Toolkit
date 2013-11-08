using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Xbim.Common.Logging;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;
namespace Xbim.ModelGeometry.Converter
{
     public static class XbimModelGeometryExtensions
    {
         public static IXbimGeometryModel GetGeometry3D(this XbimModel model, string plyData, XbimGeometryType geomType)
         {
             IXbimGeometryEngine engine = ((IModel)model).GeometryEngine();
             return engine.GetGeometry3D(plyData, geomType);
         }
    }
}
