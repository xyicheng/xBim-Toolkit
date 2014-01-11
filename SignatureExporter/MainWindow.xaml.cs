using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Xbim.Analysis;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.IO;
using Xbim.ModelGeometry.Converter;
using Xbim.ModelGeometry.Scene;
using PropertyTools.Wpf;
using PropertyTools.Wpf.Shell32;
namespace SignatureExporter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void StartExport_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(DirectoryBox.Text)) return;
            bool header = true;
            if (File.Exists("Model Analysis.csv")) header = false;
            StreamWriter summaryFile = new StreamWriter("Model Analysis.csv",true);
            
            if(header) summaryFile.WriteLine(IfcElementSignatureSummary.CSVheader());

            DirectoryInfo di = new DirectoryInfo(DirectoryBox.Text);
            FileInfo[] toProcess = di.GetFiles("*.IFC", SearchOption.AllDirectories);
            this.Dispatcher.BeginInvoke(new Action(() => MsgTxt.Clear()));

            foreach (var fileInfo in toProcess)
            {
                string outFileName;



                using (XbimModel model = new XbimModel())
                {
                    outFileName = System.IO.Path.ChangeExtension(fileInfo.FullName, "csv");
                    using (StreamWriter outFile = new StreamWriter(outFileName))
                    {
                        try
                        {
                            IfcElementSignatureSummary summary = new IfcElementSignatureSummary();
                            this.Dispatcher.BeginInvoke((new Action(() => MsgTxt.AppendText("Processing " + fileInfo.FullName + "..."))));
                            model.CreateFrom(fileInfo.FullName, null, null, true, true);
                            model.GeometryEngine().CacheStart(true);
                            summary.OriginatingSystem = model.Header.FileName.OriginatingSystem;
                            summary.FileName = fileInfo.FullName;
                            summary.PreprocessorVersion = model.Header.FileName.PreprocessorVersion;
                            summary.IfcVersion = string.Join(";", model.Header.FileSchema.Schemas);
                            outFile.WriteLine(IfcElementSignature.CSVheader());
                            foreach (var elem in model.Instances.OfType<IfcElement>())
                            {
                                IfcElementSignature sig = new IfcElementSignature(elem);
                                summary.Add(sig);
                                outFile.WriteLine(sig.ToCSV());
                            }
                            summaryFile.WriteLine(summary.ToString());
                            this.Dispatcher.BeginInvoke((new Action(() => MsgTxt.AppendText(" complete\r\n"))));
                        }
                        catch (Exception ex)
                        {
                            this.Dispatcher.BeginInvoke((new Action(() => MsgTxt.AppendText(" FAILED\r\n" + ex.Message + "\r\n"))));

                        }
                        finally
                        {
                            outFile.Close();
                            model.Close();
                        }
                    }
                }
                
                
            }
            summaryFile.Close();

        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {

            BrowseForFolderDialog dlg = new BrowseForFolderDialog();
            bool? ok = dlg.ShowDialog(this);
            if (ok.HasValue && ok.Value == true)
            {
                DirectoryBox.Text = dlg.SelectedFolder;
            }
        }
    }
}
