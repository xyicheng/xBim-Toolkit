using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie;
using Xbim.IO;
using Xbim.XbimExtensions;
using Xbim.COBie.Serialisers;
using System.IO;
using System.Diagnostics;
using Xbim.ModelGeometry.Scene;
using Xbim.Ifc.Kernel;
using Xbim.ModelGeometry;

namespace COBie.Xbim.Tester
{
    class Program
    {
        private static string _templateFile = "COBie-UK-2012-template.xls";

        //private static string _sourceFile = "Clinic-Handover.xbim";
        //private static string _binaryFile = "COBieToXbim.xCOBie";
        
        private const string _duplexSourceFile = "Duplex_A_Co-ord.xbim"; //"Clinic_A.xbim";//"2012-03-23-Duplex-Handover.xbim";
        private const string _duplexBinaryFile = "DuplexCOBieToXbim.xCOBie";
        //private const string GeoFilexBim = "XBim-Duplex_A_Co-ord.xbimGC";

        static void Main(string[] args)
        {
            //string sourceFile = _sourceFile;
            //string binaryFile = _binaryFile;

            string sourceFile = _duplexSourceFile;
            string binaryFile = _duplexBinaryFile;

            COBieWorkbook workBook;
            if (true) //we want a geometry file (!File.Exists(binaryFile))
            {
                COBieContext context = new COBieContext(null);
                IModel model = new XbimFileModelServer();
                model.Open(sourceFile, delegate(int percentProgress, object userState)
                {
                    Console.Write("\rReading File {1} {0}", percentProgress, sourceFile);
                });
                context.Model = model;
                context.TemplateFileName = _templateFile;

                //Create Scene, required for Coordinates sheet
                string cacheFile = Path.ChangeExtension(sourceFile, ".xbimGC");
                /*if (!File.Exists(cacheFile))*/ GenerateGeometry(model, cacheFile, context); //we want to generate each run
                context.Scene = new XbimSceneStream(model, cacheFile);
                
                //Clear filters to see what co-ordinates we generate
                context.Exclude.Clear();

                COBieBuilder builder = new COBieBuilder(context);
                workBook = builder.Workbook;
                COBieBinarySerialiser serialiser = new COBieBinarySerialiser(binaryFile);
                serialiser.Serialise(workBook);
            }
            //else
            //{
            //    COBieBinaryDeserialiser deserialiser = new COBieBinaryDeserialiser(binaryFile);
            //    workBook = deserialiser.Deserialise();
            //}
            string output = Path.GetFileNameWithoutExtension(sourceFile) + "COBieToIFC.ifc";

            Stopwatch sWatch = new Stopwatch();
            sWatch.Start();

            COBieXBimSerialiser xBimSerialiser = new COBieXBimSerialiser(output);
            xBimSerialiser.Serialise(workBook);


            sWatch.Stop();
            Console.WriteLine("Time = {0}", sWatch.Elapsed.Seconds);
            
            //xBimSerialiser.Save();
            Console.WriteLine("Press any key...");
            Console.ReadKey();
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
