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
using XBim.COBie.Client.Formatters;
using Xbim.ModelGeometry.Scene;
using Xbim.ModelGeometry;

using Xbim.XbimExtensions;
using System.Diagnostics;
using Xbim.Ifc.Kernel;

namespace XBim.COBie.Client
{
    public partial class COBieGenerator : Form
    {
        public COBieGenerator()
        {
            InitializeComponent();
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

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            Generate();
        }

        private void Generate()
        {
            CreateWorker();
            _worker.DoWork += COBieWorker;
            _worker.RunWorkerAsync(new Params() { ModelFile = ModelFile, TemplateFile = TemplateFile } );
        }

        private void COBieWorker(object s, DoWorkEventArgs args)
        {

            try
            {
                Params parameters = args.Argument as Params;
                
                
                string outputFile = Path.ChangeExtension(parameters.ModelFile, ".xls");

                if (!File.Exists(parameters.ModelFile))
                {
                    LogBackground(String.Format("That file doesn't exist in {0}.", Directory.GetCurrentDirectory()));
                    return;
                }

                //set the UI language to get correct resource file for template
                if (Path.GetFileName(parameters.TemplateFile).Contains("-UK-"))
                    ChangeUILanguage("en-GB"); //have to set as default is from install language which is en-US
                
                LogBackground(String.Format("Loading model {0}...", Path.GetFileName(parameters.ModelFile)));
                using(IModel model = new XbimFileModelServer())
                {

                    model.Open(parameters.ModelFile, _worker.ReportProgress);

                    // Build context
                    COBieContext context = new COBieContext(_worker.ReportProgress);
                    context.TemplateFileName = parameters.TemplateFile;
                    context.COBieGlobalValues.Add("FILENAME", parameters.ModelFile); //pass over template file name

                    context.Model = model;
                    
                    string cacheFile = Path.ChangeExtension(parameters.TemplateFile, ".xbimGC");
                    if (!File.Exists(cacheFile)) GenerateGeometry(model, cacheFile, context);
                    context.Scene = new XbimSceneStream(model, cacheFile);

                    // Create COBieReader
                    LogBackground("Generating COBie data...");
                    
                    Stopwatch timer = new Stopwatch();
                    timer.Start();
                    COBieBuilder builder = new COBieBuilder(context);
                    timer.Stop();
                    LogBackground(String.Format("Time to generate COBie data = {0}", timer.Elapsed.ToString()));
                    
                    // Export
                    LogBackground(String.Format("Formatting as XLS using {0} template...", Path.GetFileName(parameters.TemplateFile)));
                    
                    ICOBieFormatter formatter = new XLSFormatter(outputFile, parameters.TemplateFile );
                    builder.Export(formatter);
                }
                
                LogBackground(String.Format("Export Complete: {0}", outputFile));

                Process.Start(outputFile);

                LogBackground("Finished COBie Generation");
            }
            catch (Exception ex)
            {
                args.Result = ex;
                return;
            }
        }

        /// <summary>
        /// Create the xbimGC file
        /// </summary>
        /// <param name="model">IModel object</param>
        /// <param name="cacheFile">file path to write file too</param>
        /// <param name="context">Context object</param>
        private void GenerateGeometry(IModel model, string cacheFile, COBieContext context)
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

        /// <summary>
        /// set resource file culture via CurrentUICulture
        /// </summary>
        /// <param name="languageKey"></param>
        public void ChangeUILanguage(string languageKey)
        {
            try
            {
                System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo(languageKey);
                System.Threading.Thread.CurrentThread.CurrentUICulture = ci;
            }
            catch (Exception)
            {
                //to nothing Default culture will still be used
                Log("Default User Interface Culture used");
            }
        }

        private void Log(string text)
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

            dlg.Filter = "IFC Files|*.ifc;*.ifcxml;*.ifczip|Xbim Files|*.xbim"; 
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

            dlg.Filter = "XLS Files|*.xls";
            dlg.Title = "Choose a COBie template file";
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
                    Log(args.UserState.ToString());
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
                    Log(errMsg);

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
                    Log(sb.ToString());
                }

            };
        }

        private class Params
        {
            public string ModelFile { get; set; }
            public string TemplateFile { get; set; }
        }

        private void txtTemplate_TextChanged(object sender, EventArgs e)
        {
            
            //set the UI language to get correct resource file for template
            if (txtTemplate.Text.Contains("-UK-"))
                ChangeUILanguage("en-GB"); //have to set as default is from install language which is en-US
            else if (txtTemplate.Text.Contains("-US-"))
                ChangeUILanguage("en-US"); //have to set as default is from install language which is en-US
        }
    }

}
