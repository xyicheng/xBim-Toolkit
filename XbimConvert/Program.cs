using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.ModelGeometry;

using System.IO;
using Xbim.Common.Logging;
using Xbim.XbimExtensions;
using Xbim.Ifc.Kernel;

namespace XbimConvert
{
    
    class Program
    {
        
        static void Main(string[] args)
        {
            ILogger Logger = LoggerFactory.GetLogger();
            Logger.Info("XbimConvert Started");
            if (args.Length < 1)
            {
                Console.WriteLine("Invalid number of Parameters, filename required");
                Console.WriteLine("Syntax: ConvertToXbim source");
                return;
            }
            string ifcFileName = args[0];
            if (!File.Exists(ifcFileName))
            {
                Console.WriteLine(string.Format("Invalid ifc filename {0}", ifcFileName));
                Console.Error.WriteLine(string.Format("Invalid ifc filename {0}", ifcFileName));
                return;
            }
            try
            {

                string xbimFileName = Path.ChangeExtension(ifcFileName, ".xbim");
                string xbimGeometryFileName = Path.ChangeExtension(ifcFileName, ".xbimGC");
                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                XbimFileModelServer model = new XbimFileModelServer();
                //create a callback for progress
               
                //model.ImportIfc(ifcFileName,
                //    delegate(int percentProgress, object userState)
                //    {
                //        Console.Write(string.Format("{0:D2}% Converted",percentProgress));
                //        Console.SetCursorPosition(0, Console.CursorTop);
                //    }
                //    );
                model.Open(xbimFileName);
                //now convert the geometry
                XbimScene scene = new XbimScene(model);
                TransformGraph graph = new TransformGraph(model, scene);
                //add everything with a representation
                List<IfcProduct> prods = new List<IfcProduct>();
                prods.Add((IfcProduct) model.GetInstance(4984));
                graph.AddProducts(prods);

                using (FileStream sceneStream = new FileStream(xbimGeometryFileName, FileMode.Create, FileAccess.ReadWrite))
                {
                    BinaryWriter bw = new BinaryWriter(sceneStream);
                    graph.Write(bw);
                    bw.Flush();
                }
                model.Close();
                
                watch.Stop();
                Console.SetCursorPosition(0, Console.CursorTop + 1);
                Console.WriteLine("Success. Processed in  " + watch.ElapsedMilliseconds + " ms");
                Console.ReadKey();
            }
            catch (Exception e)
            {
                Console.SetCursorPosition(0, Console.CursorTop+1);
                Console.WriteLine(string.Format("Error converting {0}, {1}", ifcFileName, e.Message));
                Console.ReadLine();
                return;
            }
        }


    }

     
}
