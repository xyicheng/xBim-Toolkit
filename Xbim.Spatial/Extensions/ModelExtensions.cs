using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Xbim.Ifc2x3.Kernel;
using Xbim.XbimExtensions.Interfaces;
//using Xbim.Shapefile;
using Xbim.ModelGeometry;
using Xbim.Ifc2x3.RepresentationResource;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Spatial.Extensions
{
    public static class ModelExtensions
    {
        /// <summary>
        /// This function will create specified directory if is does not exist 
        /// and export all IfcProducts with geometry representation to the 
        /// ESRI shapefile files - one file for the type
        /// </summary>
        public static void SaveAsShapefile(this IModel model, string directory)
        {
            //if (!Directory.Exists(directory))
            //    Directory.CreateDirectory(directory);
            //IEnumerable<IfcProduct> prods = model.Instances.OfType<IfcProduct>();
            //IEnumerable<IGrouping<Type, IfcProduct>> groups = prods.GroupBy<IfcProduct, Type>( t => t.GetType());

            ////create a map dictionary to hold represenations that are repeated maps
            //Dictionary<IfcRepresentation, IXbimGeometryModel> maps = new Dictionary<IfcRepresentation, IXbimGeometryModel>();

            //foreach (var group in groups)
            //{
            //    //this will be used as a name of the shapefile
            //    string name = group.Key.Name;

            //    //Creation of SHP and DBF file
            //    IShapefileSHPWriter handleSHP = new SHPObjectData(name, ShapeType.MultiPatch);
            //    IShapefileDBFWriter handleDBF = new DBFObjectData(name);


            //    foreach (var item in group)
            //    {
            //        //write attributes
            //        handleDBF.addValue(item.EntityLabel, DBFFieldType.FTInteger, "Label", 1, 0);
            //        handleDBF.addValue(item.GlobalId.ToString(), DBFFieldType.FTString, "Guid", 30, 0);
            //        handleDBF.addValue(item.Name.ToString(), DBFFieldType.FTString, "Name", 30, 0);
            //        handleDBF.addValue(item.EntityLabel, DBFFieldType.FTInteger, "Label", 1, 0);
            //        handleDBF.writeRow();


            //        //get geometry
            //        //Get an interface to the XbimGeometry
            //        IXbimGeometryModel iGeom = XbimGeometryModel.CreateFrom(item, true, XbimLOD.LOD_Unspecified, false);

            //        List<XbimTriangulatedModel> triangModels = iGeom.Mesh();
            //        foreach (var triangModel in triangModels)
            //        {
            //            //write geometry to the SHP
            //            throw new NotImplementedException();
            //        }
                    
            //        handleSHP.writeData();
            //    }

            //}

            throw new NotImplementedException();
        }
    }
}
