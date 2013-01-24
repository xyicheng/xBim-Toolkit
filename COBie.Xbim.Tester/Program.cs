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
using Xbim.Ifc2x3.Kernel;
using Xbim.ModelGeometry;
using Xbim.Ifc2x3.ProductExtension;

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
            //SRL: Needs to be upgraded to work with Xbim files



            //////////string sourceFile = _sourceFile;
            //////////string binaryFile = _binaryFile;

            ////////string sourceFile = _duplexSourceFile;
            ////////string binaryFile = _duplexBinaryFile;

            ////////COBieWorkbook workBook;
            ////////if (true) //we want a geometry file (!File.Exists(binaryFile))
            ////////{
            ////////    COBieContext context = new COBieContext(null);
            ////////    XbimModel model = new XbimModel();
            ////////    model.Open(sourceFile, XbimDBAccess.Read,delegate(int percentProgress, object userState)
            ////////    {
            ////////        Console.Write("\rReading File {1} {0}", percentProgress, sourceFile);
            ////////    });
            ////////    context.Model = model;
            ////////    context.TemplateFileName = _templateFile;

            ////////    //Create Scene, required for Coordinates sheet
            ////////    string cacheFile = Path.ChangeExtension(sourceFile, ".xbimGC");
            ////////    /*if (!File.Exists(cacheFile))*/ GenerateGeometry(model, cacheFile, context); //we want to generate each run
            ////////    context.Scene = new XbimSceneStream(model, cacheFile);
                
            ////////    //Clear filters to see what co-ordinates we generate
            ////////    context.Exclude.Clear();

            ////////    COBieBuilder builder = new COBieBuilder(context);
            ////////    workBook = builder.Workbook;
            ////////    COBieBinarySerialiser serialiser = new COBieBinarySerialiser(binaryFile);
            ////////    serialiser.Serialise(workBook);
            ////////}
            //////////else
            //////////{
            //////////    COBieBinaryDeserialiser deserialiser = new COBieBinaryDeserialiser(binaryFile);
            //////////    workBook = deserialiser.Deserialise();
            //////////}
            ////////Stopwatch sWatch = new Stopwatch();
            ////////sWatch.Start();
            
            ////////COBieXBimSerialiser xBimSerialiser = new COBieXBimSerialiser();
            ////////xBimSerialiser.Serialise(workBook);


            ////////sWatch.Stop();
            ////////Console.WriteLine("Time = {0}", sWatch.Elapsed.Seconds);
            ////////string output = Path.GetFileNameWithoutExtension(sourceFile) + "COBieToIFC.ifc";

            ////////xBimSerialiser.Save(output);
            ////////Console.WriteLine("Press any key...");
            ////////Console.ReadKey();
        }

        /// <summary>
        /// Create the xbimGC file
        /// </summary>
        /// <param name="model">IModel object</param>
        /// <param name="cacheFile">file path to write file too</param>
        /// <param name="context">Context object</param>
        private static void GenerateGeometry(XbimModel model, string cacheFile, COBieContext context)
        {
            //need to resolve gemerate geometry
            //int total = (int)model.GeometriesCount;
            ////create the geometry file

            //IEnumerable<IfcProduct> toDraw = model.Instances.OfType<IfcProduct>().Where(t => !(t is IfcFeatureElement)); //exclude openings and additions
            //XbimScene.ConvertGeometry(toDraw, delegate(int percentProgress, object userState)
            //{
            //    context.UpdateStatus("Creating Geometry File", total, (total * percentProgress / 100));
            //}, false);
          
        }

        
    }
}
