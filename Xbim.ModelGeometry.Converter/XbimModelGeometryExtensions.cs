using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Xbim.Common.Logging;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.RepresentationResource;
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

         public static IXbimGeometryModelGroup Geometry3D(this IfcRepresentationItem repItem)
         {
             IXbimGeometryEngine engine = repItem.ModelOf.GeometryEngine();
             if (engine != null) return engine.GetGeometry3D(repItem);
             else return XbimEmptyGeometryGroup.Empty;
         }

         public static IXbimGeometryModelGroup Geometry3D(this IfcProduct product, XbimGeometryType xbimGeometryType)
         {
             IXbimGeometryEngine engine = product.ModelOf.GeometryEngine();
             if (engine != null) return engine.GetGeometry3D(product, xbimGeometryType);
             else return XbimEmptyGeometryGroup.Empty;
         }

         public static IXbimGeometryModelGroup Geometry3D(this IfcProduct product)
         {
             IXbimGeometryEngine engine = product.ModelOf.GeometryEngine();
             if (engine != null) return engine.GetGeometry3D(product);
             else return XbimEmptyGeometryGroup.Empty;
         }

         public static IXbimGeometryModelGroup Geometry3D(this IfcRepresentation representation)
         {
             IXbimGeometryEngine engine = representation.ModelOf.GeometryEngine();
             if (engine != null) return engine.GetGeometry3D(representation);
             else return XbimEmptyGeometryGroup.Empty;
         }

         public static IXbimGeometryModelGroup Geometry3D(this IfcSolidModel solid, XbimGeometryType xbimGeometryType)
         {
             IXbimGeometryEngine engine = solid.ModelOf.GeometryEngine();
             if (engine != null) return engine.GetGeometry3D(solid, xbimGeometryType);
             else return XbimEmptyGeometryGroup.Empty;
         }


    }
}
