using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Xbim.Common.Logging;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using System.Reflection;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;


namespace XbimRegression
{
    /// <summary>
    /// Class to process a folder of IFC files, capturing key metrics about their conversion
    /// </summary>
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

                    if (result.ExitCode >= 0)
                    {
                        Console.WriteLine("Processed {0} : {1} errors in {2}ms. {3} IFC Elements & {4} Geometry Nodes.",
                            file, result.ExitCode, result.Duration, result.Entities, result.GeometryEntries);
                    }
                    else
                    {
                        Console.WriteLine("Processing failed for {0} after {1}ms.",
                            file, result.ExitCode, result.Duration);
                    }
                }
                writer.Close();
            }
            Console.WriteLine("Finished. Press Enter to continue...");
            Console.ReadLine();
        }

        private ProcessResult ProcessFile(string ifcFile)
        {
            Process proc = null;
            try
            {
                RemoveFiles(ifcFile);
                ProcessStartInfo startInfo = BuildCommand(ifcFile);

                Stopwatch watch = new Stopwatch();

                watch.Start();
                proc = Process.Start(startInfo);
                if (proc.WaitForExit(Params.Timeout) == false)
                {
                    // timed out.
                    KillProcess(proc);
                    Logger.WarnFormat("Timed out processing {0} - hit {1} second limit.", ifcFile, Params.Timeout / 1000);
                }
                watch.Stop();

                return CaptureResults(ifcFile, proc.ExitCode, watch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                Logger.Error(String.Format("Problem converting file: {0}", ifcFile), ex);
                KillProcess(proc);
                
                return CaptureResults(ifcFile, -1, 0);
            }

        }

        private static ProcessStartInfo BuildCommand(string ifcFile)
        {
            String executionFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            String processorPath = Path.Combine(executionFolder, XbimConvert);
            // Run quietly (-q) and keeping existing (-ke) extensions.
            ProcessStartInfo startInfo = new ProcessStartInfo(processorPath, String.Format("\"{0}\" -q -ke", ifcFile));
            startInfo.WorkingDirectory = Path.GetDirectoryName(ifcFile);
            startInfo.WindowStyle = ProcessWindowStyle.Minimized;
            startInfo.UseShellExecute = true;
            return startInfo;
        }

        private static ProcessResult CaptureResults(string ifcFile, int exitCode, long elapsedMilliseconds)
        {

            ProcessResult result = new ProcessResult()
            {
                Duration = elapsedMilliseconds,
                FileName = ifcFile,
                ExitCode = exitCode
            };

            try
            {
                GetXbimData(ifcFile, result);
            }
            catch (Exception e)
            {
                Logger.Warn(String.Format("Failed to capture results for {0}", ifcFile), e);
            }

            return result;
        }

        private static void GetXbimData(string ifcFile, ProcessResult result)
        {
            // We're appending the xbim extension to avoid clashes between ifc, ifcxml and ifczips with the same leafname
            String xbimFile = BuildFileName(ifcFile, ".xbim");
            String xbimGCFile = BuildFileName(ifcFile, ".xbimgc");
            if (!File.Exists(xbimFile))
                return;
            XbimModel model=null;
            IXbimScene scene=null;
            try
            {
                result.IfcLength = ReadFileLength(ifcFile);
                result.XbimLength = ReadFileLength(xbimFile);
                result.XbimGCLength = ReadFileLength(xbimGCFile);

                model = new XbimModel();
                model.Open(xbimFile);
                result.Entities = model.InstancesCount;
                
                IIfcFileHeader header = model.Header;
                result.IfcSchema = header.FileSchema.Schemas.FirstOrDefault();
                result.IfcDescription = String.Format("{0}, {1}", header.FileDescription.Description.FirstOrDefault(), header.FileDescription.ImplementationLevel);
                // TODO: Ifc Name

                if (!File.Exists(xbimGCFile))
                    return;

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

        private static String BuildFileName(string ifcFile, string extension)
        {
            return String.Concat(ifcFile, extension);
        }

        private void RemoveFiles(string ifcFile)
        {
            DeleteFile(BuildFileName(ifcFile, ".xbim"));
            DeleteFile(BuildFileName(ifcFile, ".xbimgc"));
        }

        private void DeleteFile(string file)
        {
            try
            {
                if (File.Exists(file))
                    File.Delete(file);
            }
            catch (Exception)
            {
                // ignore
            }
        }

        private static void KillProcess(Process proc)
        {
            if (proc!=null && !proc.HasExited)
                proc.Kill();
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
