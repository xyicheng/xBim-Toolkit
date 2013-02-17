using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Xbim.IO;
using Xbim.COBie;
using Xbim.ModelGeometry.Scene;
using Xbim.ModelGeometry;

using Xbim.XbimExtensions;
using System.Diagnostics;
using Xbim.Ifc2x3.Kernel;
using Xbim.COBie.Contracts;
using Xbim.COBie.Serialisers;
using Xbim.COBie.Rows;
using Xbim.XbimExtensions.Interfaces;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;
using System.Windows.Media.Media3D;
using Xbim.Common.Geometry;

namespace Xbim.COBie.Client
{
    public partial class COBieGenerator : Form
    {

        public COBieGenerator()
        {
            InitializeComponent();
            MergeItemsList = new List<string>();

        }

        public string ModelFile
        {
            get
            {
                return txtPath.Text;
            }
        }

        public string TemplateFile
        {
            get
            {
                if (File.Exists(txtTemplate.Text))
                {
                    return txtTemplate.Text;
                }
                else
                {
                    return Path.Combine("Templates", txtTemplate.Text);
                }
            }
        }

        public List<string> MergeItemsList { get; set; }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            Generate();
        }

        private void Generate()
        {
            CreateWorker();
            _worker.DoWork += COBieWorker;

            Params cobieParams = BuildParams();
            _worker.RunWorkerAsync(cobieParams);
        }

        private Params BuildParams()
        {
            if (MergeChkBox.Checked)
            {
                return new MergeParams() { MergeItemsList = MergeItemsList, ModelFile = MergeItemsList.FirstOrDefault(), TemplateFile = TemplateFile };
            }
            else
            {
                return new Params() { ModelFile = ModelFile, TemplateFile = TemplateFile };
            }
        }

        private void COBieWorker(object s, DoWorkEventArgs args)
        {
            try
            {
                Params parameters = args.Argument as Params;
                
                if ((parameters.ModelFile == null) || (!File.Exists(parameters.ModelFile)))
                {
                    LogBackground(String.Format("That file doesn't exist in {0}.", Directory.GetCurrentDirectory()));
                    return;
                }
                if (parameters is MergeParams)
                {
                    MergeCOBieFiles(parameters as MergeParams);
                }
                else if (Path.GetExtension(parameters.ModelFile).ToLower() == ".xls")
                {
                    GenerateIFCFile(parameters);
                }
                else
                {
                    GenerateCOBieFile(parameters);
                }
            }
            catch (Exception ex)
            {
                args.Result = ex;
                return;
            } 
        }

        /// <summary>
        /// Create XLS file from ifc/xbim files
        /// </summary>
        /// <param name="parameters">Params</param>
        private void GenerateCOBieFile(Params parameters)
        {
            string outputFile = Path.ChangeExtension(parameters.ModelFile, ".xls");
            Stopwatch timer = new Stopwatch();
            timer.Start();
            COBieBuilder builder = GenerateCOBieWorkBook(parameters);
            timer.Stop();
            LogBackground(String.Format("Time to generate COBie data = {0} seconds", timer.Elapsed.TotalSeconds.ToString("F3")));
            
            // Export
            LogBackground(String.Format("Formatting as XLS using {0} template...", Path.GetFileName(parameters.TemplateFile)));
            ICOBieSerialiser serialiser = new COBieXLSSerialiser(outputFile, parameters.TemplateFile);
            builder.Export(serialiser);

            LogBackground(String.Format("Export Complete: {0}", outputFile));

            Process.Start(outputFile);

            LogBackground("Finished COBie Generation");
        }

        /// <summary>
        /// Create the COBieBuilder, holds COBieWorkBook
        /// </summary>
        /// <param name="parameters">Params</param>
        /// <returns>COBieBuilder</returns>
        private COBieBuilder GenerateCOBieWorkBook(Params parameters)
        {
            string xbimFile = Path.ChangeExtension(parameters.ModelFile, "xBIM");
            COBieBuilder builder = null;
            LogBackground(String.Format("Loading model {0}...", Path.GetFileName(parameters.ModelFile)));
            using (XbimModel model = new XbimModel())
            {

                if (Path.GetExtension(parameters.ModelFile).ToLower() == ".xbim")
                {
                    xbimFile = parameters.ModelFile;
                }
                else
                {
                    model.CreateFrom(parameters.ModelFile, xbimFile, _worker.ReportProgress);
                }
                model.Open(xbimFile, XbimDBAccess.ReadWrite);


                // Build context
                COBieContext context = new COBieContext(_worker.ReportProgress);
                context.TemplateFileName = parameters.TemplateFile;
                context.Model = model;

                //Create Scene, required for Coordinates sheet
                GenerateGeometry(context);

                //set filter option
                var chckBtn = gbFilter.Controls.OfType<RadioButton>().FirstOrDefault(rb => rb.Checked);
                switch (chckBtn.Name)
                {
                    case "rbDefault":
                        break;
                    case "rbPickList":
                        context.ExcludeFromPickList = true;
                        break;
                    case "rbNoFilters":
                        context.Exclude.Clear();
                        break;
                    default:
                        break;
                }

                // Create COBieReader
                LogBackground("Generating COBie data...");
                builder = new COBieBuilder(context);
            }
            return builder;
        }

        /// <summary>
        /// Create IFC file from XLS file
        /// </summary>
        /// <param name="parameters">Params</param>
        /// <returns>Created file name</returns>
        private string GenerateIFCFile(Params parameters)
        {
            string outputFile;

            LogBackground(String.Format("Reading{0}....", parameters.ModelFile));
            COBieXLSDeserialiser deSerialiser = new COBieXLSDeserialiser(parameters.ModelFile);
            COBieWorkbook newbook = deSerialiser.Deserialise();

            LogBackground("Creating xBim objects...");
            Stopwatch timer = new Stopwatch();
            timer.Start();

            outputFile = Path.GetFileNameWithoutExtension(parameters.ModelFile) + "-COBieToIFC.ifc";
            outputFile = Path.GetDirectoryName(parameters.ModelFile) + "\\" + outputFile;

            using (COBieXBimSerialiser xBimSerialiser = new COBieXBimSerialiser(outputFile, _worker.ReportProgress))
            {
                xBimSerialiser.Serialise(newbook);
                timer.Stop();
                LogBackground(String.Format("Time to generate XBim COBie data = {0} seconds", timer.Elapsed.TotalSeconds.ToString("F3")));

            }
            LogBackground(String.Format("Finished {0} Generation", outputFile));
            return outputFile;
        }

        /// <summary>
        /// Merge COBie data files
        /// </summary>
        /// <param name="parameters">MergeParams</param>
        /// <returns>Created file name</returns>
        private string MergeCOBieFiles(MergeParams parameters)
        {
            List<string> mergeList = new List<string>();
            mergeList.AddRange(parameters.MergeItemsList);
            string outputFile = string.Empty;
            if (mergeList.Count > 0)
            {
                Stopwatch timer = new Stopwatch();
                timer.Start();

                string mainFile = mergeList.First();
                mergeList.RemoveAt(0);

                if (!File.Exists(mainFile))
                {
                    LogBackground(String.Format("File {0} doesn't exist. cannot merge as main file not found", mainFile));
                    return string.Empty;
                }

                outputFile = Path.GetFileNameWithoutExtension(mainFile) + "-COBieMergeToIFC.ifc";
                outputFile = Path.GetDirectoryName(mainFile) + "\\" + outputFile;
                parameters.ModelFile = mainFile;
                using (COBieXBimSerialiser xBimSerialiser = new COBieXBimSerialiser(outputFile, _worker.ReportProgress))
                {
                    
                    LogBackground(String.Format("Reading main file {0}....", Path.GetFileName(mainFile)));
                    COBieWorkbook mainWorkBook = GetWorkBook(parameters);

                    LogBackground(String.Format("Writing main file {0} to {1}....", Path.GetFileName(mainFile), Path.GetFileName(outputFile)));
                    xBimSerialiser.Create(mainWorkBook);
                    xBimSerialiser.MergeGeometryOnly = GeoOnlyChkBox.Checked;
                    foreach (string mergeFile in mergeList)
                    {
                        if (File.Exists(mergeFile))
                        {
                            string mergExt = Path.GetExtension(mergeFile).ToLower();
                            LogBackground(String.Format("Reading file to merge {0}....", Path.GetFileName(mergeFile)));
                            parameters.ModelFile = mergeFile;
                            COBieWorkbook mergeWorkBook = GetWorkBook(parameters);
                            LogBackground(String.Format("Writing merge file {0} into {1}....", Path.GetFileName(mergeFile), Path.GetFileName(outputFile)));
                            xBimSerialiser.Merge(mergeWorkBook);

                        }
                        else
                        {
                            LogBackground(String.Format("File {0} doesn't exist. skipping merge on this file", mergeFile));
                        }
                    }

                    timer.Stop();
                    LogBackground(String.Format("Time to generate XBim COBie data = {0} seconds", timer.Elapsed.TotalSeconds.ToString("F3")));


                    LogBackground(String.Format("Creating file {0}....", Path.GetFileName(outputFile)));

                    xBimSerialiser.Save();
                }
                LogBackground(String.Format("Finished Generating {0}", outputFile));
            }
            return outputFile;
        }

        /// <summary>
        /// Generate a COBieWorkbook from ifc/xbim/xls files
        /// </summary>
        /// <param name="parameters">MergeParams</param>
        /// <returns>COBieWorkbook</returns>
        private COBieWorkbook GetWorkBook(MergeParams parameters)
        {
            string mainFile = parameters.ModelFile;
            string mainExt = Path.GetExtension(mainFile).ToLower();
                    
            COBieWorkbook workBook = null;
            if (mainExt == ".xls")
            {
                COBieXLSDeserialiser deSerialiser = new COBieXLSDeserialiser(mainFile);
                workBook = deSerialiser.Deserialise();
            }
            else if ((mainExt == ".ifc") || (mainExt == ".xbim"))
            {
                COBieBuilder builder = GenerateCOBieWorkBook(parameters);
                workBook = builder.Workbook;
            }
            return workBook;
        }


        /// <summary>
        /// Create the xbimGC file
        /// </summary>
        /// <param name="context">Context object</param>
        //private void GenerateGeometry(COBieContext context)
        //{
            //need to resolve generate geometry
            //now convert the geometry
            //IEnumerable<IfcProduct> toDraw = .IfcProducts.Cast<IfcProduct>(); //get all products for this model to place in return graph
            //int total = toDraw.Count();
            //XbimScene.ConvertGeometry(toDraw, delegate(int percentProgress, object userState)
            //{
            //    context.UpdateStatus("Creating Geometry File", total, (total * percentProgress / 100));
            //}, false);
        //}

        //Needed Geometry to test, but Steve's comment on "need to resolve generate geometry" may see GenerateGeometry change
        private  void GenerateGeometry(COBieContext context)
        {
            //now convert the geometry
            XbimModel model = context.Model;
            IEnumerable<IfcProduct> toDraw = model.IfcProducts.Cast<IfcProduct>(); ;
            if (!toDraw.Any()) return; //nothing to do
            TransformGraph graph = new TransformGraph(model);
            //create a new dictionary to hold maps
            ConcurrentDictionary<int, Object> maps = new ConcurrentDictionary<int, Object>();
            //add everything that may have a representation
            graph.AddProducts(toDraw); //load the products as we will be accessing their geometry

            ConcurrentDictionary<int, Tuple<IXbimGeometryModel, XbimMatrix3D, IfcProduct>> mappedModels = new ConcurrentDictionary<int, Tuple<IXbimGeometryModel, XbimMatrix3D, IfcProduct>>();
            ConcurrentQueue<Tuple<IXbimGeometryModel, XbimMatrix3D, IfcProduct>> mapRefs = new ConcurrentQueue<Tuple<IXbimGeometryModel, XbimMatrix3D, IfcProduct>>();
            ConcurrentDictionary<int, int[]> written = new ConcurrentDictionary<int, int[]>();

            int tally = 0;
            int percentageParsed = 0;
            int total = graph.ProductNodes.Values.Count;

            ReportProgressDelegate progDelegate = delegate(int percentProgress, object userState)
            {
                context.UpdateStatus("Creating Geometry File", total, (total * percentProgress / 100));
            };
            try
            {
                XbimLOD lod = XbimLOD.LOD_Unspecified;
                //use parallel as this improves the OCC geometry generation greatly
                ParallelOptions opts = new ParallelOptions();
                opts.MaxDegreeOfParallelism = 16;

                double deflection = 4;// model.GetModelFactors.DeflectionTolerance;
                Parallel.ForEach<TransformNode>(graph.ProductNodes.Values, opts, node => //go over every node that represents a product
                {
                    IfcProduct product = node.Product(model);
                    try
                    {

                        IXbimGeometryModel geomModel = XbimGeometryModel.CreateFrom(product, maps, false, lod, false );
                        if (geomModel != null)  //it has geometry
                        {
                            XbimMatrix3D m3d = node.WorldMatrix();
                            if (geomModel is XbimMap) //do not process maps now
                            {

                                Tuple<IXbimGeometryModel, XbimMatrix3D, IfcProduct> toAdd = new Tuple<IXbimGeometryModel, XbimMatrix3D, IfcProduct>(geomModel, m3d, product);
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
                                    byte[] matrix = m3d.ToArray();
                                    short? typeId = IfcMetaData.IfcTypeId(product);
                                    foreach (var geomId in geomIds)
                                    {
                                        geomTable.AddMapGeometry(geomId, product.EntityLabel, typeId.Value, matrix, geomModel.SurfaceStyleLabel);
                                    }
                                }
                                else
                                {
                                    List<XbimTriangulatedModel> tm = geomModel.Mesh(true, deflection);
                                    XbimBoundingBox bb = geomModel.GetBoundingBox(true);

                                    byte[] matrix = m3d.ToArray();
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
                        string message = String.Format("Error Triangulating product geometry of entity {0} - {1}, {2}",
                            product.EntityLabel,
                            product.ToString(), e1);
                        LogBackground(message);
                    }
                }
               );
                // Debug.WriteLine(tally);
                //now sort out maps again in parallel
                Parallel.ForEach<KeyValuePair<int, Tuple<IXbimGeometryModel, XbimMatrix3D, IfcProduct>>>(mappedModels, opts, map =>
                {
                    IXbimGeometryModel geomModel = map.Value.Item1;
                    XbimMatrix3D m3d = map.Value.Item2;
                    IfcProduct product = map.Value.Item3;

                    //have we already written it?
                    int[] writtenGeomids;
                    if (written.TryGetValue(geomModel.RepresentationLabel, out writtenGeomids))
                    {
                        //make maps    
                        mapRefs.Enqueue(map.Value); //add ref
                    }
                    else
                    {
                        m3d = XbimMatrix3D.Multiply(((XbimMap)geomModel).Transform, m3d);
                        WriteGeometry(model, written, geomModel, m3d, product, deflection);
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
                    XbimMatrix3D m3d = map.Item2;
                    m3d = XbimMatrix3D.Multiply(((XbimMap)geomModel).Transform, m3d);
                    IfcProduct product = map.Item3;
                    int[] geomIds;
                    if (!written.TryGetValue(geomModel.RepresentationLabel, out geomIds))
                    {
                        //we have a map specified but it is not pointing to a mapped item so write one anyway
                        WriteGeometry(model, written, geomModel, m3d, product, deflection);
                    }
                    else
                    {

                        byte[] matrix = m3d.ToArray();
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
                string message = String.Format("General Error Triangulating geometry {0}",e2);
                LogBackground(message);
                
            }
            finally
            {

            }
        }

        private static void WriteGeometry(XbimModel model, ConcurrentDictionary<int, int[]> written, IXbimGeometryModel geomModel, XbimMatrix3D m3d, IfcProduct product, double deflection)
        {
            List<XbimTriangulatedModel> tm = geomModel.Mesh(true, deflection);
            XbimBoundingBox bb = geomModel.GetBoundingBox(true);
            byte[] matrix = m3d.ToArray();
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



        private void AppendLog(string text)
        {
            txtOutput.AppendText(text + Environment.NewLine);
            txtOutput.ScrollToCaret();
        }

        private void LogBackground(string text)
        {
            _worker.ReportProgress(0, text);
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "IFC Files|*.ifc;*.ifcxml;*.ifczip|Xbim Files|*.xbim|XLS Files|*.xls";
            dlg.Title = "Choose a source model file";
            
            dlg.CheckFileExists = true;
            // Show open file dialog box 
            dlg.FileOk += new CancelEventHandler(dlg_FileOk);
            dlg.ShowDialog();
        }

        private void dlg_FileOk(object sender, CancelEventArgs ce)
        {
            OpenFileDialog dlg = sender as OpenFileDialog;
            if (dlg != null)
            {
                txtPath.Text = dlg.FileName;
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            txtOutput.Clear();
        }

        private void btnBrowseTemplate_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Title = "Choose a COBie template file";
            dlg.Filter = "XLS Files|*.xls";
            
            dlg.CheckFileExists = true;
            // Show open file dialog box 
            dlg.FileOk += new CancelEventHandler(dlg_TemplateFileOk);
            dlg.ShowDialog();
        }

        private void dlg_TemplateFileOk(object sender, CancelEventArgs ce)
        {
            OpenFileDialog dlg = sender as OpenFileDialog;
            if (dlg != null)
            {
                txtTemplate.Text = dlg.FileName;
            }
        }

        BackgroundWorker _worker;

        private void CreateWorker()
        {
            _worker = new BackgroundWorker();
            _worker.WorkerReportsProgress = true;
            _worker.WorkerSupportsCancellation = false;
            _worker.ProgressChanged += (object s, ProgressChangedEventArgs args) => 
            {
                StatusMsg.Text = (string)args.UserState;
                if (args.ProgressPercentage == 0)
                {
                    AppendLog(args.UserState.ToString());
                }
                else
                {
                    ProgressBar.Value = args.ProgressPercentage;
                }
            };

            _worker.RunWorkerCompleted += (object s, RunWorkerCompletedEventArgs args) =>
            {
                string errMsg = args.Result as String;
                if (!string.IsNullOrEmpty(errMsg))
                    AppendLog(errMsg);

                if (args.Result is Exception)
                {
                    
                    StringBuilder sb = new StringBuilder();
                    Exception ex = args.Result as Exception;
                    String indent = "";
                    while (ex != null)
                    {
                        sb.AppendFormat("{0}{1}\n", indent, ex.Message);
                        ex = ex.InnerException;
                        indent += "\t";
                    }
                    AppendLog(sb.ToString());
                }

            };
        }

        private class Params
        {
            public string ModelFile { get; set; }
            public string TemplateFile { get; set; }
        }

        private class MergeParams : Params
        {
            public List<string> MergeItemsList { get; set; }
            //public string FileToMerge { get; set; }
        }

        private void MergeChkBox_CheckedChanged(object sender, EventArgs e)
        {
            if (MergeChkBox.Checked)
            {
                GeoOnlyChkBox.Enabled = true;
                mergeBtn.Enabled = true;
                groupBox1.Enabled = false;
            }
            else
            {
                GeoOnlyChkBox.Enabled = false;
                mergeBtn.Enabled = false;
                groupBox1.Enabled = true;
            }
        }

        private void mergeBtn_Click(object sender, EventArgs e)
        {
            MergeSelect mergeSelectDlg = new MergeSelect(MergeItemsList);
            if (mergeSelectDlg.ShowDialog() == DialogResult.OK)
            {
                MergeItemsList = mergeSelectDlg.mergeItemsOut;
                AppendLog("Files to merge");
                foreach (string item in MergeItemsList)
                {
                    AppendLog(Path.GetFileName(item));
                }
            }
        }
    }

}
