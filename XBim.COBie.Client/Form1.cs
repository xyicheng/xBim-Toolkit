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

namespace XBim.COBie.Client
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnGenerate_Click(object sender, EventArgs e)
        {
            try
            {
                if (!File.Exists(txtPath.Text))
                {
                    Log(String.Format("That file doesn't exist in {0}.", Directory.GetCurrentDirectory()));
                    return;
                }

                // Get model
                Log("Loading model...");
                IModel model = new XbimFileModelServer();
                model.Open(txtPath.Text);

                // Build context
                Log("Building context...");
                COBieContext context = new COBieContext();
                context.Models.Add(model);

                // Create COBieReader
                Log("Reading COBie data (this may take a while)...");
                COBieReader reader = new COBieReader(context);

                // Export
                Log("Exporting...");
                ICOBieFormatter formatter = new XLSFormatter();
                reader.Export(formatter);

                Log("EXPORT COMPLETE!");
            }
            catch (Exception ex)
            {
                Log(ex.Message);
            }
        }

        private void Log(string text)
        {
            txtOutput.AppendText(text + Environment.NewLine);
            Application.DoEvents();
        }
    }
}
