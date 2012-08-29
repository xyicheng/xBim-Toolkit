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
using Xbim.XbimExtensions;
using System.Diagnostics;

namespace XBim.COBie.Client
{
    public partial class COBieGenerator : Form
    {
        public COBieGenerator()
        {
            InitializeComponent();
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            Generate();
        }

        private void Generate()
        {
            try
            {

                string inputFile = txtPath.Text;
                string outputFile = Path.ChangeExtension(inputFile, ".xls");

                if (!File.Exists(txtPath.Text))
                {
                    Log(String.Format("That file doesn't exist in {0}.", Directory.GetCurrentDirectory()));
                    return;
                }

                Log(String.Format("Loading model {0}...", Path.GetFileName(inputFile)));
                using(IModel model = new XbimFileModelServer())
                {

                    model.Open(inputFile);

                    // Build context
                    COBieContext context = new COBieContext();
                    context.Models.Add(model);

                    // Create COBieReader
                    Log("Generating COBie data...");
                    COBieReader reader = new COBieReader(context);

                    // Export
                    Log("Formatting as XLS...");
                    
                    ICOBieFormatter formatter = new XLSFormatter(outputFile);
                    reader.Export(formatter);
                }
                
                Log(String.Format("Export Complete: {0}", outputFile));

                Process.Start(outputFile);
            }
            catch (Exception ex)
            {
                Log(String.Format("ERROR: {0}", ex.Message));
            }
        }

        private void Log(string text)
        {
            txtOutput.AppendText(text + Environment.NewLine);
            Application.DoEvents();
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
    }
}
