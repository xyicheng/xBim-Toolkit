using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.COBie;
using Xbim.XbimExtensions;
using Xbim.IO;
using Xbim.COBie.Serialisers;
using System.IO;
using System.Diagnostics;
using Xbim.COBie.Contracts;
using Xbim.Ifc2x3.UtilityResource;
using Xbim.ModelGeometry.Scene;
using Xbim.Ifc2x3.Kernel;
using Xbim.ModelGeometry;
using Xbim.COBie.Serialisers.XbimSerialiser;
using System.Collections.Concurrent;
using Xbim.XbimExtensions.Interfaces;
using Xbim.Ifc2x3.ProductExtension;
using System.Threading.Tasks;
using System.Threading;
using Xbim.Common.Exceptions;
using System.Windows.Media.Media3D;

namespace Xbim.Tests.COBie
{
    //[DeploymentItem(SourceFile, Root)]
    //[DeploymentItem(PickListFile, Root)]
    [DeploymentItem(ExcelTemplateFile, Root)]
    [DeploymentItem(BinaryFile, Root)]
    [DeploymentItem(DuplexFile, Root)]
    [DeploymentItem(DuplexBinaryFile, Root)]
    [DeploymentItem(DLLFiles)]
    [TestClass]
    public class COBieXbimSerialisationTests
    {
        private const string Root = "TestSourceFiles";
        private const string SourceBinaryFile = "COBieToXbim.xCOBie";
        private const string ExcelTemplateLeaf = "COBie-US-2_4-template.xls";

        private const string DuplexModelLeaf = "Duplex_A_Co-ord.xbim"; //"Clinic_A.xbim";//"2012-03-23-Duplex-Handover.xbim";
        private const string DuplexFile = Root + @"\" + DuplexModelLeaf;
        private const string DuplexBinaryLeaf = "DuplexCOBieToXbim.xCOBie";
        private const string DuplexBinaryFile = Root + @"\" + DuplexBinaryLeaf;
        
        private const string ExcelTemplateFile = Root + @"\" + ExcelTemplateLeaf;
        private const string BinaryFile = Root + @"\" + SourceBinaryFile;

        private const string DLLFiles = @"C:\Xbim\XbimFramework\Dev\COBie\Xbim.ModelGeometry\OpenCascade\Win32\Bin";

        [TestMethod]
        public void Contacts_XBimSerialise()
        {
            COBieWorkbook workBook;
            COBieContext context;
            COBieBuilder builder;
            COBieWorkbook book;

            COBieBinaryDeserialiser deserialiser = new COBieBinaryDeserialiser(BinaryFile);
            workBook = deserialiser.Deserialise();

            using (COBieXBimSerialiser xBimSerialiser = new COBieXBimSerialiser(Path.ChangeExtension(BinaryFile, ".xBIM")))
            {
                xBimSerialiser.Serialise(workBook);

                context = new COBieContext(null);
                context.TemplateFileName = ExcelTemplateFile;
                context.Model = xBimSerialiser.Model;

                builder = new COBieBuilder(context);
                book = builder.Workbook;
            }
            
            
            //create excel file
            string excelFile = Path.ChangeExtension(SourceBinaryFile, ".xls");
            ICOBieSerialiser formatter = new COBieXLSSerialiser(excelFile, ExcelTemplateFile);
            builder.Export(formatter);
            Process.Start(excelFile);
            
        }

        [TestMethod]
        public void Contacts_XBimSerialise_Duplex()
        {
            COBieWorkbook workBook;
            COBieContext context;
            COBieBuilder builder;
            COBieWorkbook book;
            
            //string cacheFile = Path.ChangeExtension(DuplexFile, ".xbimGC");

            context = new COBieContext(null);
            context.TemplateFileName = ExcelTemplateFile;

            using (XbimModel model = new XbimModel())
            {
                model.Open(DuplexFile, XbimDBAccess.ReadWrite, delegate(int percentProgress, object userState)
                {
                    Console.Write("\rReading File {1} {0}", percentProgress, DuplexFile);
                });
                context.Model = model;
                
                //Create Scene, required for Coordinates sheet
                GenerateGeometry(context); //we want to generate each run
                //context.Scene = new XbimSceneStream(model, cacheFile);

                builder = new COBieBuilder(context);
                workBook = builder.Workbook;
                COBieBinarySerialiser serialiser = new COBieBinarySerialiser(DuplexBinaryFile);
                serialiser.Serialise(workBook);

            }
            
            

            using (COBieXBimSerialiser xBimSerialiser = new COBieXBimSerialiser(Path.ChangeExtension(DuplexBinaryFile, ".xBIM")))
            {
                xBimSerialiser.Serialise(workBook);


                context = new COBieContext(null);
                context.TemplateFileName = ExcelTemplateFile;
                context.Model = xBimSerialiser.Model;
                
                GenerateGeometry(context); //we want to generate each run
                
                builder = new COBieBuilder(context);

                book = builder.Workbook;

            }

            //create excel file
            string excelFile = Path.ChangeExtension(SourceBinaryFile, ".xls");
            ICOBieSerialiser formatter = new COBieXLSSerialiser(excelFile, ExcelTemplateFile);
            builder.Export(formatter);
            Process.Start(excelFile);

            
        }
        [TestMethod]
        public void Delimited_Strings()
        {
            string test1 = "This is split : here and , here and : again";
            string test2 = "This is not split";
            string test3 = "This is also not split";
            string test4 = "This is split , here and , here and , again";
            List<string> strList = new List<string>() { test1, test2, test3, test4 };

            string delimited = COBieXBim.JoinStrings(':', strList);
            List<string> delimitesStrings = COBieXBim.SplitString(delimited, ':');

            Debug.WriteLine(string.Format("\"{0}\"", delimited));
            Debug.WriteLine(string.Format("\"{0}\" \"{1}\"", test1, delimitesStrings[0]));
            Debug.WriteLine(string.Format("\"{0}\" \"{1}\"", test2, delimitesStrings[1]));
            Debug.WriteLine(string.Format("\"{0}\" \"{1}\"", test3, delimitesStrings[2]));
            Debug.WriteLine(string.Format("\"{0}\" \"{1}\"", test4, delimitesStrings[3]));

            Assert.AreEqual(test1, delimitesStrings[0]);
            Assert.AreEqual(test2, delimitesStrings[1]);
            Assert.AreEqual(test3, delimitesStrings[2]);
            Assert.AreEqual(test4, delimitesStrings[3]);
            
        }
        /// <summary>
        /// Create the xbimGC file
        /// </summary>
        /// <param name="model">IModel object</param>
        /// <param name="context">Context object</param>
        private static void GenerateGeometry(COBieContext context)
        {
            //now convert the geometry
            IEnumerable<IfcProduct> toDraw = context.Model.IfcProducts.Cast<IfcProduct>(); //get all products for this model to place in return graph
            int total = toDraw.Count();
            CreateGeometry(context.Model, toDraw, delegate(int percentProgress, object userState)
            {
                context.UpdateStatus("Creating Geometry", total, (total * percentProgress / 100));
            });

        }

        private static void CreateGeometry(XbimModel model, IEnumerable<IfcProduct> toDraw, ReportProgressDelegate progDelegate)
        {

           if (!toDraw.Any()) return; //nothing to do
            TransformGraph graph = new TransformGraph(model);
            //create a new dictionary to hold maps
            ConcurrentDictionary<int, Object> maps = new ConcurrentDictionary<int, Object>();
            //add everything that may have a representation
            graph.AddProducts(toDraw); //load the products as we will be accessing their geometry

            ConcurrentDictionary<int, Tuple<IXbimGeometryModel, Matrix3D, IfcProduct>> mappedModels = new ConcurrentDictionary<int, Tuple<IXbimGeometryModel, Matrix3D, IfcProduct>>();
            ConcurrentQueue<Tuple<IXbimGeometryModel, Matrix3D, IfcProduct>> mapRefs = new ConcurrentQueue<Tuple<IXbimGeometryModel, Matrix3D, IfcProduct>>();
            ConcurrentDictionary<int, int[]> written = new ConcurrentDictionary<int, int[]>();

            int tally = 0;
            int percentageParsed = 0;
            int total = graph.ProductNodes.Values.Count;

            try
            {
                XbimLOD lod = XbimLOD.LOD_Unspecified;
                //use parallel as this improves the OCC geometry generation greatly
                Parallel.ForEach<TransformNode>(graph.ProductNodes.Values, node => //go over every node that represents a product
                // foreach (var node in graph.ProductNodes.Values)
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
                                Tuple<IXbimGeometryModel, Matrix3D, IfcProduct> toAdd = new Tuple<IXbimGeometryModel, Matrix3D, IfcProduct>(geomModel, m3d, product);
                                if (!mappedModels.TryAdd(geomModel.RepresentationLabel, toAdd)) //get unique rep
                                    mapRefs.Enqueue(toAdd); //add ref
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
                                    List<XbimTriangulatedModel> tm = geomModel.Mesh(true);
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
                        throw new XbimException(message, e1);
                    }
                }
                 );
                // Debug.WriteLine(tally);
                //now sort out maps again in parallel
                Parallel.ForEach<KeyValuePair<int, Tuple<IXbimGeometryModel, Matrix3D, IfcProduct>>>(mappedModels, map =>
                //  foreach (var map in mappedModels)
                {
                    IXbimGeometryModel geomModel = map.Value.Item1;
                    Matrix3D m3d = map.Value.Item2;
                    IfcProduct product = map.Value.Item3;

                    //have we already written it?
                    int[] writtenGeomids;
                    if (written.TryGetValue(geomModel.RepresentationLabel, out writtenGeomids))
                    {
                        //make maps
                        Tuple<IXbimGeometryModel, Matrix3D, IfcProduct> toAdd = new Tuple<IXbimGeometryModel, Matrix3D, IfcProduct>(geomModel, m3d, product);
                        mapRefs.Enqueue(toAdd); //add ref
                    }
                    else
                    {
                        WriteGeometry(model, written, geomModel, m3d, product);
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
                }
                );
                XbimGeometryCursor geomMapTable = model.GetGeometryTable();
                XbimLazyDBTransaction mapTrans = geomMapTable.BeginLazyTransaction();
                foreach (var map in mapRefs) //don't do this in parallel to avoid database thrashing as it is very fast
                {
                    IXbimGeometryModel geomModel = map.Item1;
                    Matrix3D m3d = map.Item2;
                    IfcProduct product = map.Item3;
                    int[] geomIds;
                    if (!written.TryGetValue(geomModel.RepresentationLabel, out geomIds))
                    {
                        //we have a map specified but it is not pointing to a mapped item so write one anyway
                        WriteGeometry(model, written, geomModel, m3d, product);
                    }
                    else
                    {

                        byte[] matrix = Matrix3DExtensions.ToArray(m3d, true);
                        short? typeId = IfcMetaData.IfcTypeId(product);
                        foreach (var geomId in geomIds)
                        {
                            geomMapTable.AddMapGeometry(geomId, product.EntityLabel, typeId.Value, matrix, geomModel.SurfaceStyleLabel);
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
                throw new XbimException("General Error Triangulating geometry", e2);
            }
            finally
            {

            }
        }

        private static void WriteGeometry(XbimModel model, ConcurrentDictionary<int, int[]> written, IXbimGeometryModel geomModel, Matrix3D m3d, IfcProduct product)
        {
            List<XbimTriangulatedModel> tm = geomModel.Mesh(true);
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
            Debug.Assert(written.TryAdd(geomModel.RepresentationLabel, geomIds));
            model.FreeTable(geomTable);

        }

    }
}
