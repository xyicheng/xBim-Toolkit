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
                Console.WriteLine("Press any key to continue..."); 
                Console.ReadLine();
                return;
            }
            string fileName = args[0];
            if (!File.Exists(fileName))
            {
                Console.WriteLine(string.Format("Invalid filename {0}", fileName));
                Console.WriteLine("Press any key to continue..."); 
                Console.ReadLine();
                return;
            }
            try
            {
                string fileType = Path.GetExtension(fileName);
                string xbimFileName = Path.ChangeExtension(fileName, ".xbim");
                
                XbimFileModelServer model = new XbimFileModelServer();

                if (string.Compare(fileType, ".ifc", true) == 0)
                {
                    //create a callback for progress

                    model.ImportIfc(fileName,
                        delegate(int percentProgress, object userState)
                        {
                            Console.Write(string.Format("{0:D2}% Converted", percentProgress));
                            Console.SetCursorPosition(0, Console.CursorTop);
                        }
                        );
                }
                else if (string.Compare(fileType, ".xbim", true) == 0)
                { 
                    model.Open(fileName);
                }
                else if (string.Compare(fileType, ".ifcxml", true) == 0)
                {
                    model.ImportXml(fileName, xbimFileName);
                }
                else
                {
                    Console.WriteLine("Invalid file type, ifc, xbim or ifcxml required");
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadLine();
                    return;
                }
                string xbimGeometryFileName = Path.ChangeExtension(fileName, ".xbimGC");
                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                watch.Start();
                Console.WriteLine("Converting Geometry...");
                //now convert the geometry
                
                
                long processMe = 0;
                IfcType t=null;
                bool hasType = false;
                IEnumerable<IfcProduct> toDraw;
                if (args.Length > 1) 
                {

                    hasType = IfcInstances.IfcTypeLookup.TryGetValue(args[1].ToUpper(), out t);
                    if (!hasType) //try getting an instance
                        processMe = Convert.ToInt64(args[1]);
                }
                if (processMe > 0)
                {
                    List<IfcProduct> l =  new List<IfcProduct>();
                    l.Add((IfcProduct)model.GetInstance(processMe));
                    toDraw = l;
                }
                else if(hasType)
                {
                    Type theType = t.Type;
                    toDraw=model.InstancesWhere<IfcProduct>(i=>i.GetType()== theType);
                }
                else
                {
                    //add everything with a representation
                    toDraw=model.IfcProducts.Items;
                }
                XbimScene scene = new XbimScene(model, toDraw);
                using (FileStream sceneStream = new FileStream(xbimGeometryFileName, FileMode.Create, FileAccess.ReadWrite))
                {
                    BinaryWriter bw = new BinaryWriter(sceneStream);
                    scene.Graph.Write(bw);
                    bw.Flush();
                }
                model.Close();
                
                watch.Stop();
                Console.SetCursorPosition(0, Console.CursorTop + 1);
                Console.WriteLine("Success. Processed in  " + watch.ElapsedMilliseconds + " ms");
                Console.WriteLine("Press any key to continue..."); 
                Console.ReadKey();
            }
            catch (Exception e)
            {
                Console.SetCursorPosition(0, Console.CursorTop+1);
                Console.WriteLine(string.Format("Error converting {0}, {1}", fileName, e.Message));
                Console.WriteLine("Press any key to continue..."); 
                Console.ReadLine();
                return;
            }
        }

        private static void ResetCursor(int top)
        {
            try
            {
                // Can't reset outside of buffer
                if (top >= Console.BufferHeight)
                    return;
                Console.SetCursorPosition(0, top);
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }


    }

     
}
