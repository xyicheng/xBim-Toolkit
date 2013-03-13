using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Xbim.Common.Logging;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.IO;
using Xbim.ModelGeometry.OCC;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.ModelGeometry.Converter
{
    public class XbimMesher
    {

        private class MapData
        {
            public IXbimGeometryModel Geometry;
            public Matrix3D Matrix;
            public IfcProduct Product;

            public void Clear()
            {
                this.Geometry = null;
                this.Product = null;
            }

            public MapData(IXbimGeometryModel geomModel, Matrix3D m3d, IfcProduct product)
            {
                this.Geometry = geomModel;
                this.Matrix = m3d;
                this.Product = product;
            }
        }

        private class MapRefData
        {
            public int RepresentationLabel;
            public int EntityLabel;
            public short EntityTypeId;
            public Matrix3D Matrix;
            public int SurfaceStyleLabel;

            public MapRefData(MapData toAdd)
            {
                RepresentationLabel = toAdd.Geometry.RepresentationLabel;
                EntityLabel = toAdd.Product.EntityLabel;
                EntityTypeId = IfcMetaData.IfcTypeId(toAdd.Product);
                SurfaceStyleLabel = toAdd.Geometry.SurfaceStyleLabel;
                Matrix = Matrix3D.Multiply(((XbimMap)toAdd.Geometry).Transform, toAdd.Matrix);
            }


        }

        /// <summary>
        /// Use this function on a Xbim Model that has just been creted from IFC content
        /// This will create the default 3D mesh geometry for all IfcProducts and add it to the model
        /// </summary>
        /// <param name="model"></param>
        public static void GenerateGeometry(XbimModel model, ILogger Logger = null, ReportProgressDelegate progDelegate = null )
        {

            //now convert the geometry

            IEnumerable<IfcProduct> toDraw = model.Instances.OfType<IfcProduct>().Where(t => !(t is IfcFeatureElement));
            if (!toDraw.Any()) return; //nothing to do
            TransformGraph graph = new TransformGraph(model);
            //create a new dictionary to hold maps
            ConcurrentDictionary<int, Object> maps = new ConcurrentDictionary<int, Object>();
            //add everything that may have a representation
            graph.AddProducts(toDraw); //load the products as we will be accessing their geometry

            ConcurrentDictionary<int, MapData> mappedModels = new ConcurrentDictionary<int, MapData>();
            ConcurrentQueue<MapRefData> mapRefs = new ConcurrentQueue<MapRefData>();
            ConcurrentDictionary<int, int[]> written = new ConcurrentDictionary<int, int[]>();

            int tally = 0;
            int percentageParsed = 0;
            int total = graph.ProductNodes.Values.Count;

      
            try
            {
                XbimLOD lod = XbimLOD.LOD_Unspecified;
                //use parallel as this improves the OCC geometry generation greatly
                ParallelOptions opts = new ParallelOptions();
                opts.MaxDegreeOfParallelism = 16;

                double deflection = 4;// model.GetModelFactors.DeflectionTolerance;
                Parallel.ForEach<TransformNode>(graph.ProductNodes.Values, opts, node => //go over every node that represents a product
                //   foreach (var node in graph.ProductNodes.Values)
                {
                    IfcProduct product = node.Product(model);
                    try
                    {

                        IXbimGeometryModel geomModel = XbimGeometryModel.CreateFrom(product, maps, false, lod, false);
                        if (geomModel != null)  //it has geometry
                        {
                            Matrix3D m3d = node.WorldMatrix();
                            if (geomModel is XbimMap) //do not process maps now
                            {

                                MapData toAdd = new MapData(geomModel, m3d, product);
                                if (!mappedModels.TryAdd(geomModel.RepresentationLabel, toAdd)) //get unique rep
                                    mapRefs.Enqueue(new MapRefData(toAdd)); //add ref
                            }
                            else
                            {
                                int[] geomIds;
                                XbimGeometryCursor geomTable = model.GetGeometryTable();

                                XbimLazyDBTransaction transaction = geomTable.BeginLazyTransaction();
                                if (written.TryGetValue(geomModel.RepresentationLabel, out geomIds))
                                {
                                    byte[] matrix = Matrix3DExtensions.ToArray(m3d, true);
                                    short? typeId = IfcMetaData.IfcTypeId(product);
                                    foreach (var geomId in geomIds)
                                    {
                                        geomTable.AddMapGeometry(geomId, product.EntityLabel, typeId.Value, matrix, geomModel.SurfaceStyleLabel);
                                    }
                                }
                                else
                                {
                                    List<XbimTriangulatedModel> tm = geomModel.Mesh(true, deflection);
                                    XbimBoundingBox bb = geomModel.GetBoundingBox(true);

                                    byte[] matrix = Matrix3DExtensions.ToArray(m3d, true);
                                    short? typeId = IfcMetaData.IfcTypeId(product);

                                    geomIds = new int[tm.Count + 1];
                                    geomIds[0] = geomTable.AddGeometry(product.EntityLabel, XbimGeometryType.BoundingBox, typeId.Value, matrix, bb.ToArray(), 0, geomModel.SurfaceStyleLabel);

                                    short subPart = 0;
                                    foreach (XbimTriangulatedModel b in tm)
                                    {
                                        geomIds[subPart + 1] = geomTable.AddGeometry(product.EntityLabel, XbimGeometryType.TriangulatedMesh, typeId.Value, matrix, b.Triangles, subPart, b.SurfaceStyleLabel);
                                        subPart++;
                                    }

                                    //            Debug.Assert(written.TryAdd(geomModel.RepresentationLabel, geomIds));
                                    Interlocked.Increment(ref tally);
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
                                transaction.Commit();
                                model.FreeTable(geomTable);

                            }
                        }
                        else
                        {
                            Interlocked.Increment(ref tally);
                        }
                    }
                    catch (Exception e1)
                    {
                        String message = String.Format("Error Triangulating product geometry of entity {0} - {1}",
                            product.EntityLabel,
                            product.ToString());
                        if(Logger!=null) Logger.Warn(message, e1);
                    }
                }

               );
                graph = null;

                // Debug.WriteLine(tally);
                //now sort out maps again in parallel
                Parallel.ForEach<KeyValuePair<int, MapData>>(mappedModels, opts, map =>
                // foreach (var map in mappedModels)
                {
                    IXbimGeometryModel geomModel = map.Value.Geometry;
                    Matrix3D m3d = map.Value.Matrix;
                    IfcProduct product = map.Value.Product;

                    //have we already written it?
                    int[] writtenGeomids;
                    if (written.TryGetValue(geomModel.RepresentationLabel, out writtenGeomids))
                    {
                        //make maps    
                        mapRefs.Enqueue(new MapRefData(map.Value)); //add ref
                    }
                    else
                    {
                        m3d = Matrix3D.Multiply(((XbimMap)geomModel).Transform, m3d);
                        WriteGeometry(model, written, geomModel, m3d, product, deflection);

                    }

                    Interlocked.Increment(ref tally);
                    if (progDelegate != null)
                    {
                        int newPercentage = Convert.ToInt32((double)tally / total * 100.0);
                        if (newPercentage > percentageParsed)
                        {
                            percentageParsed = newPercentage;
                            progDelegate(percentageParsed, "Converted");
                        }
                    }
                    map.Value.Clear(); //release any native memory we are finished with this
                }
                );

                //clear up maps
                mappedModels.Clear();
                XbimGeometryCursor geomMapTable = model.GetGeometryTable();
                XbimLazyDBTransaction mapTrans = geomMapTable.BeginLazyTransaction();
                foreach (var map in mapRefs) //don't do this in parallel to avoid database thrashing as it is very fast
                {

                    int[] geomIds;
                    if (!written.TryGetValue(map.RepresentationLabel, out geomIds))
                    {
                        if(Logger!=null) Logger.WarnFormat("A geometry mapped reference (#{0}) has been found that has no base geometry", map.RepresentationLabel);
                    }
                    else
                    {

                        byte[] matrix = Matrix3DExtensions.ToArray(map.Matrix, true);
                        foreach (var geomId in geomIds)
                        {
                            geomMapTable.AddMapGeometry(geomId, map.EntityLabel, map.EntityTypeId, matrix, map.SurfaceStyleLabel);
                        }
                        mapTrans.Commit();
                        mapTrans.Begin();

                    }
                    Interlocked.Increment(ref tally);
                    if (progDelegate != null)
                    {
                        int newPercentage = Convert.ToInt32((double)tally / total * 100.0);
                        if (newPercentage > percentageParsed)
                        {
                            percentageParsed = newPercentage;
                            progDelegate(percentageParsed, "Converted");
                        }
                    }
                    if (tally % 100 == 100)
                    {
                        mapTrans.Commit();
                        mapTrans.Begin();
                    }

                }
                mapTrans.Commit();
                model.FreeTable(geomMapTable);
            }
            catch (Exception e2)
            {
                if(Logger!=null) Logger.Warn("General Error Triangulating geometry", e2);
            }
            finally
            {

            }
        }

        private static void WriteGeometry(XbimModel model, ConcurrentDictionary<int, int[]> written, IXbimGeometryModel geomModel, Matrix3D m3d, IfcProduct product, double deflection)
        {
            List<XbimTriangulatedModel> tm = geomModel.Mesh(true, deflection);
            XbimBoundingBox bb = geomModel.GetBoundingBox(true);
            byte[] matrix = Matrix3DExtensions.ToArray(m3d, true);
            short? typeId = IfcMetaData.IfcTypeId(product);
            XbimGeometryCursor geomTable = model.GetGeometryTable();

            XbimLazyDBTransaction transaction = geomTable.BeginLazyTransaction();
            int[] geomIds = new int[tm.Count + 1];
            geomIds[0] = geomTable.AddGeometry(product.EntityLabel, XbimGeometryType.BoundingBox, typeId.Value, matrix, bb.ToArray(), 0, geomModel.SurfaceStyleLabel);
            short subPart = 0;
            foreach (XbimTriangulatedModel b in tm)
            {
                geomIds[subPart + 1] = geomTable.AddGeometry(product.EntityLabel, XbimGeometryType.TriangulatedMesh, typeId.Value, matrix, b.Triangles, subPart, b.SurfaceStyleLabel);
                subPart++;
            }
            transaction.Commit();
            written.TryAdd(geomModel.RepresentationLabel, geomIds);
            model.FreeTable(geomTable);

        }
    }
}
