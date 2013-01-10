using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.ModelGeometry;

using System.IO;
using Xbim.Common.Logging;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.SharedBldgElements;

using Xbim.Common.Exceptions;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.MeasureResource;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using Xbim.XbimExtensions.Interfaces;
using System.Threading;

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
                    //string xbimGeometryFileName = BuildFileName(arguments.IfcFileName, ".xbimGC");
                    System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();

                    


                    using (XbimModel model = ParseModelFile(xbimFileName))
                    {
                        watch.Start();

                        model.Open(xbimFileName, XbimDBAccess.ReadWrite);
                        //using (var txn = model.BeginTransaction())
                        //{
                            GenerateGeometry( model);
                        //}
                        
                        model.Close();

                    }
                    watch.Stop();
                    XbimModel.Terminate();
                    ResetCursor(Console.CursorTop + 1);
                    Console.WriteLine("Success. Processed in " + watch.ElapsedMilliseconds + " ms");
                    GetInput();
                }
                catch (Exception e)
                {
                    if(e is XbimException || e is NotImplementedException)
                    {
                         // Errors we have already handled or know about. Keep details brief in the log
                        Logger.ErrorFormat("One or more errors converting {0}. Exiting...", arguments.IfcFileName);
                        CreateLogFile(arguments.IfcFileName, eventTrace.Events);

                        DisplayError(string.Format("One or more errors converting {0}, {1}", arguments.IfcFileName, e.Message));
                    }
                    else
                    {
                        // Unexpected failures. Log exception details
                        Logger.Fatal(String.Format("Fatal Error converting {0}. Exiting...", arguments.IfcFileName), e);
                        CreateLogFile(arguments.IfcFileName, eventTrace.Events);

                        DisplayError(string.Format("Fatal Error converting {0}, {1}", arguments.IfcFileName, e.Message));
                    }
                    return -1;
                }
                Logger.Info("XbimConvert finished successfully...");

                int errors = (from e in eventTrace.Events
                             where (e.EventLevel > EventLevel.INFO)
                             select e).Count();

                if (errors > 0)
                {
                    CreateLogFile(arguments.IfcFileName, eventTrace.Events);
                }

                
                return errors;
            }
            
        }

        private static void DisplayError(String  message)
        {
            ResetCursor(Console.CursorTop + 1);
            Console.WriteLine(message);
            GetInput();
        }

        private static void GenerateGeometry( XbimModel model)
        {
            //now convert the geometry

            List<IfcProduct> toDraw = GetProducts(model).ToList();
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
            
            ReportProgressDelegate progDelegate = delegate(int percentProgress, object userState)
                {
                    if (!arguments.IsQuiet)
                    {
                        Console.Write(string.Format("{0:D5} Converted", percentProgress));
                        ResetCursor(Console.CursorTop);
                    }
                };
            try
            {
                XbimLOD lod = XbimLOD.LOD_Unspecified;
                Parallel.ForEach<TransformNode>(graph.ProductNodes.Values, node=>{ //go over every node that represents a product
                
                    IfcProduct product = node.Product;
                    try
                    {

                        IXbimGeometryModel geomModel = XbimGeometryModel.CreateFrom(product, maps, false, lod, arguments.OCC);
                        if (geomModel != null)  //it has geometry
                        {
                            Matrix3D m3d = node.WorldMatrix();
                            
                            List<XbimTriangulatedModel> tm=null ;
                            lock (mappedModels)
                            {
                            if (geomModel is XbimMap)
                            {
                                XbimMap map = (XbimMap)geomModel;
                                m3d = Matrix3D.Multiply(map.Transform, m3d);
                                List<XbimTriangulatedModel> lookup;
                                int key = map.MappedItem.RepresentationLabel;

                                if (mappedModels.TryGetValue(key, out lookup))
                                    tm = lookup;

                                if(tm==null)
                                {   
                                    tm = geomModel.Mesh(true);
                                    lock (mappedModels)
                                    {
                                        if (mappedModels.TryGetValue(key, out lookup))
                                            tm = lookup;
                                        else
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

                                if (mappedModels.TryGetValue(key, out lookup))
                                    tm = lookup;

                                if (tm == null)
                                {
                                    tm = geomModel.Mesh(true);
                                    lock (mappedModels)
                                    {
                                        if (mappedModels.TryGetValue(key, out lookup))
                                            tm = lookup;
                                        else
                                            mappedModels.Add(key, tm);
                                    }
                                }
                            }
                            else
                                tm = geomModel.Mesh(true);

                            //if (!(geomModel is XbimMap))
                            {
                                //tm = geomModel.Mesh(true);

                                //lock (tm)
                                //{
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
                                }
                                transaction.Commit();
                                model.FreeTable(geomTable);
                            }
                                //}
                            }
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
                    catch (Exception e1)
                    {
                        String message = String.Format("Error Triangulating product geometry of entity {0} - {1}",
                            product.EntityLabel,
                            product.ToString());
                        Logger.Warn(message, e1);
                    }
                });
                
            }
            catch (Exception e2)
            {
                Logger.Warn("General Error Triangulating geometry", e2);
            }
            finally
            {
                
            }
        }


            




           



                //XbimScene.ConvertGeometry(toDraw, delegate(int percentProgress, object userState)
                //{
                //    if (!arguments.IsQuiet)
                //    {
                //        Console.Write(string.Format("{0:D5} Converted", percentProgress));
                //        ResetCursor(Console.CursorTop);
                //    }
                //}, arguments.OCC);
            


        private static IEnumerable<IfcProduct> GetProducts(XbimModel model)
        {
            IEnumerable<IfcProduct> result = null;

            switch (arguments.FilterType)
            {
                case FilterType.None:
                    result = model.Instances.OfType<IfcProduct>().Where(t=>!(t is IfcFeatureElement)); //exclude openings and additions
                    Logger.Debug("All geometry items will be generated");
                    break;

                case FilterType.ElementID:
                    List<IfcProduct> list = new List<IfcProduct>();
                    list.Add(model.Instances[arguments.ElementIdFilter] as IfcProduct);
                    result = list;
                    Logger.DebugFormat("Only generating product element ID {0}", arguments.ElementIdFilter);
                    break;

                case FilterType.ElementType:
                    Type theType = arguments.ElementTypeFilter.Type;
                    result = model.Instances.Where<IfcProduct>(i=> i.GetType() == theType);
                    Logger.DebugFormat("Only generating product elements of type '{0}'", arguments.ElementTypeFilter);
                    break;

                default:
                    throw new NotImplementedException();
            }
            return result;
        }

        private static XbimModel ParseModelFile(string xbimFileName)
        {
            XbimModel model = new XbimModel();
            //create a callback for progress
            switch (Path.GetExtension(arguments.IfcFileName).ToLowerInvariant())
            {
                case ".ifc":
                case ".ifczip":
                case ".ifcxml":
                    model.CreateFrom(arguments.IfcFileName,
                        xbimFileName,
                        delegate(int percentProgress, object userState)
                        {
                            if (!arguments.IsQuiet)
                            {
                                Console.Write(string.Format("{0:D2}% Parsed", percentProgress));
                                ResetCursor(Console.CursorTop);
                            }
                        }
                        );
                    break;
                case ".xbim":
                    
                    model = new XbimModel();
                    break;
                default:
                    throw new NotImplementedException(String.Format("XbimConvert does not support {0} file formats currently", Path.GetExtension(arguments.IfcFileName)));
            }
            
            return model;
        }

        private static void CreateLogFile(string ifcFile, IList<Event> events)
        {
            try
            {
                string logfile = String.Concat(ifcFile, ".log");
                using (StreamWriter writer = new StreamWriter(logfile, false))
                {
                    foreach (Event logEvent in events)
                    {
                        string message = SanitiseMessage(logEvent.Message);
                        writer.WriteLine("{0:yyyy-MM-dd HH:mm:ss} : {1:-5} {2}.{3} - {4}",
                            logEvent.EventTime,
                            logEvent.EventLevel.ToString(),
                            logEvent.Logger,
                            logEvent.Method,
                            message
                            );
                    }
                    writer.Flush();
                    writer.Close();
                }
            }
            catch (Exception e)
            {
                Logger.Error(String.Format("Failed to create Log File for {0}", ifcFile), e);
            }
        }

        private static string SanitiseMessage(string message)
        {
            if (arguments.SanitiseLogs == false)
                return message;

            string modelPath = Path.GetDirectoryName(arguments.IfcFileName);
            string currentPath = Environment.CurrentDirectory;

            return message
                .Replace(modelPath, String.Empty)
                .Replace(currentPath, String.Empty);
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
            if (!arguments.IsQuiet)
            {
                Console.WriteLine("Press Enter to continue...");
                Console.ReadLine();
            }
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
