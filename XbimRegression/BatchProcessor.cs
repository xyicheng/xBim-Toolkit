using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xbim.Common.Logging;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using System.Reflection;
using Xbim.XbimExtensions;


namespace XbimRegression
{
    public class BatchProcessor
    {
        private static readonly ILogger Logger = LoggerFactory.GetLogger();
        private const string XbimConvert = @"XbimConvert.exe";

        Params _params;

        public BatchProcessor(Params arguments)
        {
            _params = arguments;
            
        }

        public Params Params
        {
            get
            {
                return _params;
            }
        }

        public void Run()
        {
            DirectoryInfo di = new DirectoryInfo(Params.TestFileRoot);

            String resultsFile = Path.Combine(Params.TestFileRoot,  String.Format("XbimRegression_{0:yyyyMMdd-hhmmss}.csv", DateTime.Now));

            using (StreamWriter writer = new StreamWriter(resultsFile))
            {
                writer.WriteLine(ProcessResult.CsvHeader);

                foreach (FileInfo file in di.GetFiles("*.IFC", SearchOption.AllDirectories))
                {
                    Console.WriteLine("Processing {0}", file);
                    ProcessResult result = ProcessFile(file.FullName);
                    writer.WriteLine(result.ToCsv());
                    writer.Flush();

                    Console.WriteLine("Processed {0} : {1} errors in {2}ms. {3} IFC Elements & {4} Geometry Nodes.",
                        file, result.ExitCode, result.Duration, result.Entities, result.GeometryEntries);
                }
                Console.WriteLine("Finished. Press Enter to continue...");
                writer.Close();
            }

            Console.ReadLine();
        }

        private ProcessResult ProcessFile(string ifcFile)
        {
            Process proc = null;
            try
            {
                String executionFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                String processorPath = Path.Combine(executionFolder, XbimConvert);
                ProcessStartInfo startInfo = new ProcessStartInfo(processorPath, String.Format("\"{0}\" -q -ke", ifcFile));
                
                startInfo.WorkingDirectory = Path.GetDirectoryName(ifcFile);
                startInfo.UseShellExecute = true;

                Stopwatch watch = new Stopwatch();

                watch.Start();
                proc = Process.Start(startInfo);
                bool r = proc.WaitForExit(Params.Timeout);
                watch.Stop();

                ProcessResult result = CaptureResults(ifcFile, proc.ExitCode, watch);

                return result;
            }
            catch (Exception ex)
            {
                Logger.Error(String.Format("Problem converting file: {0}", ifcFile), ex);
                if (!proc.HasExited)
                    proc.Kill();
            }
            return new ProcessResult();
        }

        private static ProcessResult CaptureResults(string ifcFile, int exitCode, Stopwatch timer)
        {

            ProcessResult result = new ProcessResult()
            {
                Duration = timer.ElapsedMilliseconds,
                FileName = ifcFile,
                ExitCode = exitCode

            };

            try
            {
                GetXbimData(ifcFile, result);
            }
            catch (Exception e)
            {
                Logger.Warn(String.Format("Failed to get results for {0}", ifcFile), e);
            }

            return result;
        }


        private static void GetXbimData(string ifcFile, ProcessResult result)
        {
            // We're appending the xbim extension to avoid clashes between ifc, ifcxml and ifczips with the same leafname
            String xbimFile = String.Concat(ifcFile, ".xbim");
            String xbimGCFile = String.Concat(ifcFile, ".xbimgc");
            if (!File.Exists(xbimFile) || !File.Exists(xbimGCFile))
                return;
            XbimFileModelServer model=null;
            IXbimScene scene=null;
            try
            {
                result.IfcLength = ReadFileLength(ifcFile);
                result.XbimLength = ReadFileLength(xbimFile);
                result.XbimGCLength = ReadFileLength(xbimGCFile);

                model = new XbimFileModelServer(xbimFile, FileAccess.Read);
                result.Entities = model.InstancesCount;
                
                IfcFileHeader header = model.Header;
                result.IfcSchema = header.FileSchema.Schemas.FirstOrDefault();
                result.IfcDescription = String.Format("{0}, {1}", header.FileDescription.Description.FirstOrDefault(), header.FileDescription.ImplementationLevel);
                // TODO: Ifc Name
                scene = new XbimSceneStream(model, xbimGCFile);
                // TODO: verify if there is a better metric
                result.GeometryEntries = scene.Graph.ProductNodes.Count();
            }
            finally
            {
                if (scene != null)
                {
                    scene.Close();
                    scene = null;
                }
                if (model != null)
                {
                    model.Close();
                    model.Dispose();
                    model = null;
                }

            }
        }



        private static long ReadFileLength(string file)
        {
            long length = 0;
            FileInfo fi = new FileInfo(file);
            if (fi.Exists)
            {
                length = fi.Length;
            }
            return length;
        }

    }
}
