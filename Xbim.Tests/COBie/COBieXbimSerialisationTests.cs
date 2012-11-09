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
using Xbim.Ifc.UtilityResource;
using Xbim.ModelGeometry.Scene;
using Xbim.Ifc.Kernel;
using Xbim.ModelGeometry;

namespace Xbim.Tests.COBie
{
    [DeploymentItem(SourceFile, Root)]
    [DeploymentItem(PickListFile, Root)]
    [DeploymentItem(ExcelTemplateFile, Root)]
    [DeploymentItem(BinaryFile, Root)]
    [DeploymentItem(DuplexFile, Root)]
    [DeploymentItem(DuplexBinaryFile, Root)]
    [DeploymentItem(DLLFiles)]
    [TestClass]
    public class COBieXbimSerialisationTests
    {
        private const string Root = "TestSourceFiles";
        private const string SourceModelLeaf = "Clinic-Handover.xbim"; //"Clinic_A.xbim";//"2012-03-23-Duplex-Handover.xbim";
        private const string PickListLeaf = "PickLists.xml";
        private const string SourceBinaryFile = "COBieToXbim.xCOBie";
        private const string ExcelTemplateLeaf = "COBie-US-2_4-template.xls";

        private const string DuplexModelLeaf = "Duplex_A_Co-ord.xbim"; //"Clinic_A.xbim";//"2012-03-23-Duplex-Handover.xbim";
        private const string DuplexFile = Root + @"\" + DuplexModelLeaf;
        private const string DuplexBinaryLeaf = "DuplexCOBieToXbim.xCOBie";
        private const string DuplexBinaryFile = Root + @"\" + DuplexBinaryLeaf;
        private const string GeoFilexBim = Root + @"\" + "XBim-Duplex_A_Co-ord.xbimGC";

        private const string SourceFile = Root + @"\" + SourceModelLeaf;
        private const string PickListFile = Root + @"\" + PickListLeaf;
        private const string ExcelTemplateFile = Root + @"\" + ExcelTemplateLeaf;
        private const string BinaryFile = Root + @"\" + SourceBinaryFile;

        private const string DLLFiles = @"C:\Xbim\XbimFramework\Dev\COBie\Xbim.ModelGeometry\OpenCascade\Win32\Bin";

        [TestMethod]
        public void Contacts_XBimSerialise()
        {
            COBieWorkbook workBook;
            COBieContext context;
            IModel model;
            COBieBuilder builder;

            COBieBinaryDeserialiser deserialiser = new COBieBinaryDeserialiser(BinaryFile);
            workBook = deserialiser.Deserialise();

            COBieXBimSerialiser xBimSerialiser = new COBieXBimSerialiser();
            xBimSerialiser.Serialise(workBook);

            
            context = new COBieContext(null);
            model = xBimSerialiser.Model;
            
            context.Model = model;
            context.TemplateFileName = ExcelTemplateFile;

            builder = new COBieBuilder(context);

            COBieWorkbook book = builder.Workbook;
            
            
            //create excel file
            string excelFile = Path.ChangeExtension(SourceBinaryFile, ".xls");
            ICOBieSerialiser formatter = new COBieXLSSerialiser(excelFile, ExcelTemplateFile);
            builder.Export(formatter);
            Process.Start(excelFile);

            // Assert
            //Assert.AreEqual(19, newBook.Count);

            // 19 workbooks. # rows in a selection.
        }

        [TestMethod]
        public void Contacts_XBimSerialise_Duplex()
        {
            COBieWorkbook workBook;
            COBieContext context;
            IModel model;
            COBieBuilder builder;
            
            string cacheFile = Path.ChangeExtension(DuplexFile, ".xbimGC");

            if (true)//(!File.Exists(DuplexBinaryFile))
            {
                context = new COBieContext(null);
                model = new XbimFileModelServer();
                model.Open(DuplexFile, delegate(int percentProgress, object userState)
                {
                    Console.Write("\rReading File {1} {0}", percentProgress, DuplexFile);
                });
                context.Model = model;
                context.TemplateFileName = ExcelTemplateFile;

                //Create Scene, required for Coordinates sheet
                GenerateGeometry(model, cacheFile, context); //we want to generate each run
                context.Scene = new XbimSceneStream(model, cacheFile);
                
                builder = new COBieBuilder(context);
                workBook = builder.Workbook;
                COBieBinarySerialiser serialiser = new COBieBinarySerialiser(DuplexBinaryFile);
                serialiser.Serialise(workBook);
                
            }
            //else
            //{
            //    COBieBinaryDeserialiser deserialiser = new COBieBinaryDeserialiser(DuplexBinaryFile);
            //    workBook = deserialiser.Deserialise();
            //}

            COBieXBimSerialiser xBimSerialiser = new COBieXBimSerialiser();
            xBimSerialiser.Serialise(workBook);


            context = new COBieContext(null);
            model = xBimSerialiser.Model;

            GenerateGeometry(model, GeoFilexBim, context); //we want to generate each run
            context.Scene = new XbimSceneStream(model, GeoFilexBim);
            context.Model = model;
            context.TemplateFileName = ExcelTemplateFile;

            builder = new COBieBuilder(context);

            COBieWorkbook book = builder.Workbook;


            //create excel file
            string excelFile = Path.ChangeExtension(SourceBinaryFile, ".xls");
            ICOBieSerialiser formatter = new COBieXLSSerialiser(excelFile, ExcelTemplateFile);
            builder.Export(formatter);
            Process.Start(excelFile);

            // Assert
            //Assert.AreEqual(19, newBook.Count);

            // 19 workbooks. # rows in a selection.
        }
        /// <summary>
        /// Create the xbimGC file
        /// </summary>
        /// <param name="model">IModel object</param>
        /// <param name="cacheFile">file path to write file too</param>
        /// <param name="context">Context object</param>
        private static void GenerateGeometry(IModel model, string cacheFile, COBieContext context)
        {
            //now convert the geometry
            IEnumerable<IfcProduct> toDraw = model.IfcProducts.Items; //get all products for this model to place in return graph

            XbimScene scene = new XbimScene(model, toDraw);
            int total = scene.Graph.ProductNodes.Count();
            //create the geometry file

            using (FileStream sceneStream = new FileStream(cacheFile, FileMode.Create, FileAccess.ReadWrite))
            {
                BinaryWriter bw = new BinaryWriter(sceneStream);
                //show current status to user
                scene.Graph.Write(bw, delegate(int percentProgress, object userState)
                {
                    context.UpdateStatus("Creating Geometry File", total, (total * percentProgress / 100));
                });
                bw.Flush();
            }

        }
    }
}
