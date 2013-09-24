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
using Xbim.ModelGeometry.Converter;

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

        public XbimModel Model { get; set; }

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
                    if (ValidateChkBox.Checked)
                        ValidateXLSfile(parameters);
                    else
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
                Model = model; //used to check we close file on Close event
                //Create Scene, required for Coordinates sheet
                if (!SkipGeoChkBox.Checked)
                {
                    GenerateGeometry(context);
                }

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
        /// Validate XLS file for COBie errors, also will swap templates if required
        /// </summary>
        /// <param name="parameters">Params</param>
        /// <returns>Created file name</returns>
        private void ValidateXLSfile(Params parameters)
        {
            
            //read xls file
            LogBackground(String.Format("Reading {0}....", parameters.ModelFile));
            COBieXLSDeserialiser deSerialiser = new COBieXLSDeserialiser(parameters.ModelFile);
            COBieWorkbook Workbook = deSerialiser.Deserialise();

            //extract pick list from the template sheet and swap into workbook (US / UK)
            LogBackground("Swapping PickList from template...");
            COBieSheet<COBiePickListsRow> CobiePickLists = null;
            if ((!string.IsNullOrEmpty(parameters.TemplateFile)) &&
                File.Exists(parameters.TemplateFile)
                )
            {
                //extract the pick list sheet from template
                COBieXLSDeserialiser deSerialiserPickList = new COBieXLSDeserialiser(parameters.TemplateFile, Constants.WORKSHEET_PICKLISTS);
                COBieWorkbook wbookPickList = deSerialiserPickList.Deserialise();
                if (wbookPickList.Count > 0) CobiePickLists = (COBieSheet<COBiePickListsRow>)wbookPickList.FirstOrDefault();
                //check the workbook last sheet is a pick list
                if (Workbook.LastOrDefault() is COBieSheet<COBiePickListsRow>)
                {
                    //remove original pick list and replace with templates
                    Workbook.RemoveAt(Workbook.Count - 1);
                    Workbook.Add(CobiePickLists);
                }
                else
                {
                    LogBackground("Failed to Swap PickList from template...");
                }

            }

            COBieContext context = new COBieContext(_worker.ReportProgress);
            COBieProgress progress = new COBieProgress(context);

            //Validate
            progress.Initialise("Validating Workbooks", Workbook.Count, 0);
            progress.ReportMessage("Building Indices...");
            foreach (ICOBieSheet<COBieRow> item in Workbook)
            {
                item.BuildIndices();
            }
            progress.ReportMessage("Building Indices...Finished");
                
            // Validate the workbook
            progress.ReportMessage("Starting Validation...");

            Workbook.Validate(ErrorRowIndexBase.Two, (lastProcessedSheetIndex) =>
            {
                // When each sheet has been processed, increment the progress bar
                progress.IncrementAndUpdate();
            } );
            progress.ReportMessage("Finished Validation");
            progress.Finalise();
                
            // Export
            LogBackground(String.Format("Formatting as XLS using {0} template...", Path.GetFileName(parameters.TemplateFile)));
            ICOBieSerialiser serialiser = new COBieXLSSerialiser(parameters.ModelFile, parameters.TemplateFile);
            serialiser.Serialise(Workbook);

            LogBackground(String.Format("Export Complete: {0}", parameters.ModelFile));
            
            Process.Start(parameters.ModelFile);

            LogBackground("Finished COBie Validation");
            
            
        }

        /// <summary>
        /// Create IFC file from XLS file
        /// </summary>
        /// <param name="parameters">Params</param>
        /// <returns>Created file name</returns>
        private string GenerateIFCFile(Params parameters)
        {
            string outputFile;

            LogBackground(String.Format("Reading {0}....", parameters.ModelFile));
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
            int total = (int)model.Instances.CountOf<IfcProduct>();
            ReportProgressDelegate progDelegate = delegate(int percentProgress, object userState)
            {
                context.UpdateStatus("Creating Geometry File", total, (total * percentProgress / 100));
            };
            XbimMesher.GenerateGeometry(model, null, progDelegate);
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
                if (Path.GetExtension(dlg.FileName).ToLower() == ".xls")
                    ValidateChkBox.Enabled = true;
                else
                    ValidateChkBox.Enabled = false;
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

        private void COBieGenerator_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (Model != null)
            {
                Model.Close();
            }
            
            
        }

        private void txtPath_TextChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtPath.Text))
            {
                if (Path.GetExtension(txtPath.Text).ToLower() == ".xbim")
                {
                    SkipGeoChkBox.Checked = true; //if xbim file assume we have geometry
                    SkipGeoChkBox.Enabled = false;
                }
                else
                    SkipGeoChkBox.Enabled = true;

            }
            
        }
    }

}
