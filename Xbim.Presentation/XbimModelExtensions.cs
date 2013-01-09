using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Kernel;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions.Interfaces;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Xbim.ModelGeometry;
using Xbim.Common.Exceptions;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.ProductExtension;
namespace Xbim.Presentation
{
    public static class XbimModelExtensions
    {
        /// <summary>
        /// Creates a geometry for every object that has a geometry in the model, excluding openings, these are cut fro the solids
        /// </summary>
        /// <param name="model"></param>
        /// <param name="progDelegate"></param>
        public static void GenerateGeometry(this XbimModel model, ReportProgressDelegate progDelegate)
        {
            //now convert the geometry

            List<IfcProduct> toDraw = model.IfcProducts.Where(t=>!(t is IfcFeatureElement)).Cast<IfcProduct>().ToList();
            if (!toDraw.Any()) return; //nothing to do
            TransformGraph graph = new TransformGraph(model);
            //create a new dictionary to hold maps
            Dictionary<int, Object> maps = new Dictionary<int, Object>();
            //add everything that may have a representation
            graph.AddProducts(toDraw); //load the products as we will be accessing their geometry
            Dictionary<int, List<XbimTriangulatedModel>> mappedModels = new Dictionary<int, List<XbimTriangulatedModel>>();
            int tally = 0;
            int percentageParsed = 0;
            int total = toDraw.Count();
            try
            {
                XbimLOD lod = XbimLOD.LOD_Unspecified;
                Parallel.ForEach<TransformNode>(graph.ProductNodes.Values, node =>
                { 
                    //go over every node that represents a product
                    IfcProduct product = node.Product;
                    try
                    {
                        IXbimGeometryModel geomModel = XbimGeometryModel.CreateFrom(product, maps, false, lod, false);
                        if (geomModel != null)  //it has geometry
                        {
                            List<XbimTriangulatedModel> tm;
                            Matrix3D m3d = node.WorldMatrix();
                            if (geomModel is XbimMap)
                            {
                                XbimMap map = (XbimMap)geomModel;
                                m3d = Matrix3D.Multiply(map.Transform, m3d);
                                List<XbimTriangulatedModel> lookup;
                                int key = map.MappedItem.RepresentationLabel;

                                lock (mappedModels)
                                {
                                    if (mappedModels.TryGetValue(key, out lookup))
                                    {
                                        tm = lookup;
                                    }
                                    else
                                    {
                                        tm = geomModel.Mesh(true);
                                        mappedModels.Add(key, tm);
                                    }
                                }
                            }
                            else if (geomModel is XbimGeometryModelCollection && ((XbimGeometryModelCollection)geomModel).IsMap)
                            {
                                XbimGeometryModelCollection mapColl = (XbimGeometryModelCollection)geomModel;

                                m3d = Matrix3D.Multiply(mapColl.Transform, m3d);
                                List<XbimTriangulatedModel> lookup;
                                int key = mapColl.RepresentationLabel;

                                lock (mappedModels)
                                {
                                    {
                                        if (mappedModels.TryGetValue(key, out lookup))
                                        {
                                            tm = lookup;
                                        }
                                        else
                                        {
                                            tm = geomModel.Mesh(true);
                                            mappedModels.Add(key, tm);
                                        }
                                    }
                                }
                            }
                            else
                                tm = geomModel.Mesh(true);


                            XbimBoundingBox bb = geomModel.GetBoundingBox(true);

                            byte[] matrix = Matrix3DExtensions.ToArray(m3d, true);

                            short? typeId = IfcMetaData.IfcTypeId(product);
                            XbimGeometryCursor geomTable = model.GetGeometryTable();

                            XbimLazyDBTransaction transaction = geomTable.BeginLazyTransaction();
                            geomTable.AddGeometry(product.EntityLabel, XbimGeometryType.BoundingBox, typeId.Value, matrix, bb.ToArray(), 0, geomModel.SurfaceStyleLabel);
                            short subPart = 0;
                            foreach (XbimTriangulatedModel b in tm)
                            {
                                geomTable.AddGeometry(product.EntityLabel, XbimGeometryType.TriangulatedMesh, typeId.Value, matrix, b.Triangles, subPart, b.SurfaceStyleLabel);
                                subPart++;
                            } transaction.Commit();
                            model.FreeTable(geomTable);
                        }
                        lock (product) //lock anything
                        {
                            tally++;
                            if (progDelegate != null)
                            {
                                int newPercentage = Convert.ToInt32((double)tally / total * 100.0);
                                if (newPercentage > percentageParsed)
                                {
                                    percentageParsed = newPercentage;
                                    progDelegate(percentageParsed, "Converted");
                                }
                            }

                        }
                    }
                    catch (Exception e1)
                    {
                        String message = String.Format("Error Triangulating product geometry of entity {0} - {1}",
                            product.EntityLabel,
                            product.ToString());
                        throw new XbimException(message);
                    }
                });

            }
            catch (Exception e2)
            {
                throw new XbimException("General Error Triangulating geometry\n" + e2);
            }
           
        }
    }
}
