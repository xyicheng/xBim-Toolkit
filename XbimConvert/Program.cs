using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.ModelGeometry;

using System.IO;
using Xbim.Common.Logging;

namespace XbimConvert
{
    class Program
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger();
        private static Params arguments;

        static int Main(string[] args)
        {
            // We need to use the logger early to initialise before we use EventTrace
            Logger.Debug("XbimConvert starting...");
            using (EventTrace eventTrace = LoggerFactory.CreateEventTrace())
            {
                arguments = Params.ParseParams(args);

                if (!arguments.IsValid)
                {
                    return -1;
                }
                
                try
                {
                    Logger.InfoFormat("Starting conversion of {0}", args[0]);

                    string xbimFileName = BuildFileName(arguments.IfcFileName, ".xbim");
                    string xbimGeometryFileName = BuildFileName(arguments.IfcFileName, ".xbimGC");
                    System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

                    watch.Start();
                    XbimFileModelServer model = new XbimFileModelServer();
                    //create a callback for progress

                    model.ImportIfc(arguments.IfcFileName,
                        xbimFileName,
                        delegate(int percentProgress, object userState)
                        {
                            if (!arguments.IsQuiet)
                            {
                                Console.Write(string.Format("{0:D2}% Converted", percentProgress));
                                ResetCursor(Console.CursorTop);
                            }
                        }
                        );

                    //now convert the geometry
                    XbimScene scene = new XbimScene(model);
                    TransformGraph graph = new TransformGraph(model, scene);
                    //add everything with a representation
                    graph.AddProducts(model.IfcProducts.Items);

                    using (FileStream sceneStream = new FileStream(xbimGeometryFileName, FileMode.Create, FileAccess.ReadWrite))
                    {
                        BinaryWriter bw = new BinaryWriter(sceneStream);
                        graph.Write(bw);
                        bw.Flush();
                    }
                    model.Close();

                    watch.Stop();

                    ResetCursor(Console.CursorTop + 1);
                    Console.WriteLine("Success. Processed in " + watch.ElapsedMilliseconds + " ms");
                    GetInput();
                }
                catch (Exception e)
                {
                    ResetCursor(Console.CursorTop + 1);
                    Console.WriteLine(string.Format("Error converting {0}, {1}", arguments.IfcFileName, e.Message));
                    Logger.Error(String.Format("Error converting {0}", arguments.IfcFileName), e);
                    GetInput();
                    
                    return -1;
                }
                int errors = (from e in eventTrace.Events
                             where (e.EventLevel > EventLevel.INFO)
                             select e).Count();

                Logger.Debug("XbimConvert finished...");
                return errors;
            }
            
        }

        private static string BuildFileName(string file, string extension)
        {
            if (arguments.KeepFileExtension)
            {
                return String.Concat(file, extension);
            }
            else
            {
                return Path.ChangeExtension(file, extension);
            }
        }


        private static void GetInput()
        {
            if(!arguments.IsQuiet)
                Console.ReadLine();
        }

        private static void ResetCursor(int top)
        {
            try
            {
                // Can't reset outside of buffer, and should ignore when in quiet mode
                if (top >= Console.BufferHeight || arguments.IsQuiet == true)
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
