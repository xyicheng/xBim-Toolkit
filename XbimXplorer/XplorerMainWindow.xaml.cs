#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     XbimXplorer
// Filename:    XplorerMainWindow.xaml.cs
// Published:   01, 2012
// Last Edited: 9:05 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Xml;
using Microsoft.Win32;
using Xbim.IO;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.SharedBldgElements;
using Xbim.Presentation;
using Xbim.XbimExtensions;
using Xbim.ModelGeometry;
using Xbim.ModelGeometry.Scene;

#endregion

namespace XbimXplorer
{
    /// <summary>
    ///   Interaction logic for Window1.xaml
    /// </summary>
    public partial class XplorerMainWindow : Window
    {
        private BackgroundWorker _worker;
        private PropertiesWindow _propertyWindow;
        private IfcProduct _currentProduct;
        private string _currentModelFileName;
        private Dictionary<string, XbimMaterialProvider> _materials;

        public XplorerMainWindow()
        {
            InitializeComponent();

            DrawingControl.SelectionChanged += new SelectionChangedEventHandler(DrawingControl_SelectionChanged);
            SpatialControl.SelectedItemChanged +=new RoutedPropertyChangedEventHandler<SpatialStructureTreeItem>(SpatialControl_SelectedItemChanged);
            DrawingControl.OnSetMaterial += new SetMaterialEventHandler(DrawingControl_OnSetMaterial);
            //DrawingControl.OnSetFilter += new SetFilterEventHandler(DrawingControl_OnSetFilter);
           
        }

        void DrawingControl_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            
        }

        /// <summary>
        ///   Set the filter to draw the products you are interested in, called multiply to allow phased drawing of products
        /// </summary>
        /// <param name = "pass">The count of number of calls</param>
        /// <returns></returns>
        private Func<IfcProduct, bool> DrawingControl_OnSetFilter(int pass)
        {
            switch (pass)
            {
                case 1:
                    return (p =>  p is IfcSlab);
                case 2:
                    return (p => p is IfcWall);
                case 3:
                    return (p => p is IfcRoof);
                case 4:
                    return (p => p is IfcWindow);
                case 5:
                    return (p => p is IfcDoor);
                case 6:
                    return (p => !(p is IfcDoor) && !(p is IfcWindow) && !(p is IfcRoof) && !(p is IfcWall) && !(p is IfcSlab) && !(p is IfcSpace) && !(p is IfcFeatureElement));
                default:
                    return null;
            }
        }

        /// <summary>
        ///   Each product will call back once for its material and bind to a material provider
        ///   To change the material dynamically, change it in the material provider
        /// </summary>
        /// <param name = "product"></param>
        /// <returns></returns>
        private XbimMaterialProvider DrawingControl_OnSetMaterial(IfcProduct product)
        {
            //set up your material list
            if (_materials == null)
            {
                _materials = new Dictionary<string, XbimMaterialProvider>();
                _materials.Add("201",
                               new XbimMaterialProvider(
                                   new DiffuseMaterial(new SolidColorBrush(Colors.LightBlue) {Opacity = 0.7})));
                _materials.Add("202",
                               new XbimMaterialProvider(
                                   new DiffuseMaterial(new SolidColorBrush(Colors.LightGreen) {Opacity = 0.7})));
                _materials.Add("211",
                               new XbimMaterialProvider(
                                   new DiffuseMaterial(new SolidColorBrush(Colors.LightYellow) {Opacity = 0.7})));
            }
            XbimMaterialProvider mat;
            //do what you need here to set materials, this is just a default behaviour
            IfcSpace space = product as IfcSpace;
            if (space != null)
            {
                switch (product.Name.ToString())
                {
                    case "201":
                        mat = _materials["201"];
                        break;
                    case "202":
                        mat = _materials["202"];
                        break;
                    case "211":
                        mat = _materials["211"];
                        break;
                    default:
                        ModelDataProvider modelProvider = ModelProvider;
                        mat = new XbimMaterialProvider(modelProvider.GetDefaultMaterial(product));
                        break;
                }
            }
            else
            {
                ModelDataProvider modelProvider = ModelProvider;
                mat = new XbimMaterialProvider(modelProvider.GetDefaultMaterial(product));
            }


            //create a list of materials and then reuse them, do not create a new material for each call
            return mat;
        }

        private void SliderColour_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            byte nv = Convert.ToByte(e.NewValue);
            if (_materials != null) //change the value of the first material
            {
                XbimMaterialProvider firstMaterial = _materials.Values.FirstOrDefault();
                if (firstMaterial != null)
                {
                    Color cl = Colors.LimeGreen;

                    cl.R = nv;
                    cl.G = Convert.ToByte(255 - nv);
                    //cl.B = nv;
                    //cl.A = 100;


                    firstMaterial.FaceMaterial = new DiffuseMaterial(new SolidColorBrush(cl) {Opacity = 0.7});
                    firstMaterial.BackgroundMaterial = firstMaterial.FaceMaterial; //set them both the same
                }
            }
            e.Handled = true;
        }

        private void SpatialControl_SelectedItemChanged(object sender,
                                                        RoutedPropertyChangedEventArgs<SpatialStructureTreeItem> e)
        {
            SpatialStructureTreeItem item = e.NewValue as SpatialStructureTreeItem;
            if (item != null)
            {
            }
        }

        private void DrawingControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach (var item in e.AddedItems)
            {
                IfcProduct product = item as IfcProduct;
                if (product != null)
                {
                    //SpatialControl.
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }

        private void _propertyWindow_Closed(object sender, EventArgs e)
        {
            _propertyWindow = null;
        }


        private void FileOpen_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Filter = "Xbim Files|*.xbim;*.ifc;*.ifcxml"; // Filter files by extension 
            dlg.FileOk += new CancelEventHandler(dlg_OpenXbimFile);
            dlg.ShowDialog(this);
        }

        private ModelDataProvider ModelProvider
        {
            get
            {
                ObjectDataProvider objProvider = FindResource("ModelProvider") as ObjectDataProvider;
                if (objProvider != null) return objProvider.ObjectInstance as ModelDataProvider;
                else return null;
            }
            set
            {
                ObjectDataProvider objProvider = FindResource("ModelProvider") as ObjectDataProvider;
                objProvider.ObjectInstance = value;
            }
        }

        private void OpenIfcFile(object s, DoWorkEventArgs args)
        {
            BackgroundWorker worker = s as BackgroundWorker;
            string ifcFilename = args.Argument as string;

            IModel model = new XbimMemoryModel();
            IfcInputStream input = null;
            try
            {
                //attach it to the Ifc Stream Parser
                input = new IfcInputStream(new FileStream(ifcFilename, FileMode.Open, FileAccess.Read));
                if (input.Load(model) != 0)
                    throw new Exception("Ifc file parsing errors\n" + input.ErrorLog.ToString());
                XbimScene geomEngine = new XbimScene(model);
                ModelProvider.Scene = geomEngine;
            }
            catch (Exception ex)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Error reading " + ifcFilename);
                string indent = "\t";
                while (ex != null)
                {
                    sb.AppendLine(indent + ex.Message);
                    ex = ex.InnerException;
                    indent += "\t";
                }

                args.Result = new Exception(sb.ToString());

            }
            finally
            {
                if (input != null) input.Close();
            }
           

        }

        private void OpenIfcXmlFile(object s, DoWorkEventArgs args)
        {
            BackgroundWorker worker = s as BackgroundWorker;
            ModelDataProvider modelProvider = ModelProvider;
            string fileName = args.Argument as string;
            Stream xmlInStream = null;
            XbimMemoryModel m = null;
            try
            {
                XmlReaderSettings settings = new XmlReaderSettings() {IgnoreComments = true, IgnoreWhitespace = true};
                xmlInStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
                m = new XbimMemoryModel();
                using (XmlReader xmlReader = XmlReader.Create(xmlInStream, settings))
                {
                    IfcXmlReader reader = new IfcXmlReader();
                    reader.Read(m, xmlReader);
                }
                XbimScene geomEngine = new XbimScene(m);
                modelProvider.Scene = geomEngine;
            }
            catch (Exception ex)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Error reading " + fileName);
                string indent = "\t";
                while (ex != null)
                {
                    sb.AppendLine(indent + ex.Message);
                    ex = ex.InnerException;
                    indent += "\t";
                }
                args.Result = new Exception(sb.ToString());
            }
            finally
            {
                if (xmlInStream != null) xmlInStream.Close();
            }
        }

        /// <summary>
        ///   This is called when we explcitly want to open an xBIM file
        /// </summary>
        /// <param name = "s"></param>
        /// <param name = "args"></param>
        private void OpenXbimFile(object s, DoWorkEventArgs args)
        {
            BackgroundWorker worker = s as BackgroundWorker;

            string fileName = args.Argument as string;

            XbimFileModelServer m = new XbimFileModelServer();
            ModelDataProvider modelProvider = ModelProvider;


            try
            {
                if (fileName.ToLower() == _currentModelFileName) //same file do nothing
                    return;
                else
                    _currentModelFileName = fileName.ToLower();
                string cacheFile = Path.ChangeExtension(_currentModelFileName, "xbimGC");
               
                m.Open(fileName); //load entities into the model
                ModelProvider.Scene = new XbimSceneStream(m, cacheFile);
               
            }
            catch (Exception el)
            {
                args.Result = el;
            }
        }

        private void dlg_OpenXbimFile(object sender, CancelEventArgs e)
        {
            OpenFileDialog dlg = sender as OpenFileDialog;
            if (dlg != null)
            {
                FileInfo fInfo = new FileInfo(dlg.FileName);
                string ext = fInfo.Extension.ToLower();
                StatusBar.Visibility = Visibility.Visible;
                CreateWorker();
                switch (ext)
                {
                    case ".ifc": //it is an Ifc File
                        _worker.DoWork += OpenIfcFile;
                        _worker.RunWorkerAsync(dlg.FileName);
                        break;
                    case ".ifcxml": //it is an IfcXml File
                        _worker.DoWork += OpenIfcXmlFile;
                        _worker.RunWorkerAsync(dlg.FileName);
                        break;
                    case ".xbim": //it is an xbim File
                        _worker.DoWork += OpenXbimFile;
                        _worker.RunWorkerAsync(dlg.FileName);
                        break;

                    default:
                        break;
                }
            }
        }

        private void CreateWorker()
        {
            _worker = new BackgroundWorker();
            _worker.WorkerReportsProgress = true;
            _worker.WorkerSupportsCancellation = false;
            _worker.ProgressChanged += delegate(object s, ProgressChangedEventArgs args)
                                           {
                                               ProgressBar.Value = args.ProgressPercentage;
                                               StatusMsg.Text = (string) args.UserState;
                                           };

            _worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
                                              {
                                                  string errMsg = args.Result as String;
                                                  if (!string.IsNullOrEmpty(errMsg))
                                                      MessageBox.Show(this, errMsg, "Error Opening Ifc File",
                                                                      MessageBoxButton.OK, MessageBoxImage.Error,
                                                                      MessageBoxResult.None, MessageBoxOptions.None);
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
                                                      MessageBox.Show(this, sb.ToString(), "Error Opening Ifc File",
                                                                      MessageBoxButton.OK, MessageBoxImage.Error,
                                                                      MessageBoxResult.None, MessageBoxOptions.None);
                                                  }
                                                  // StatusBar.Visibility = Visibility.Hidden;
                                              };
        }


        private void ProgressChanged(object sender, ProgressChangedEventArgs args)
        {
            ProgressBar.Value = args.ProgressPercentage;
            string msg = args.UserState as string;
            if (msg != null) StatusMsg.Text = msg;
        }

        private void SpatialControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DisplayPropertyWindow();
        }

        private void DisplayPropertyWindow()
        {
            if (_propertyWindow == null)
            {
                _propertyWindow = new PropertiesWindow();
                _propertyWindow.Owner = this;

                Binding b = new Binding("SelectedItem");
                b.Source = SpatialControl;
                _propertyWindow.SetBinding(PropertiesWindow.InstanceProperty, b);
                _propertyWindow.Closed += new EventHandler(_propertyWindow_Closed);
                _propertyWindow.Show();
            }
            _propertyWindow.Focus();
        }

        private void ShowProperties(object sender, RoutedEventArgs e)
        {
            DisplayPropertyWindow();
            _propertyWindow.Instance = _currentProduct;
        }

        private void DrawingControl_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _currentProduct = DrawingControl.GetProductAt(e);
            if (_currentProduct != null)
            {
                ContextMenu = new ContextMenu();
                MenuItem mi = new MenuItem() {Header = string.Format("Hide this {0}", _currentProduct.GetType().Name)};
                mi.Click += new RoutedEventHandler(HideProduct);
                ContextMenu.Items.Add(mi);
                mi = new MenuItem() {Header = string.Format("Hide all {0}s", _currentProduct.GetType().Name)};
                mi.Click += new RoutedEventHandler(HideAllTypesOf);
                ContextMenu.Items.Add(mi);
                mi = new MenuItem() {Header = string.Format("Show all {0}s", _currentProduct.GetType().Name)};
                mi.Click += new RoutedEventHandler(ShowAllTypesOf);
                ContextMenu.Items.Add(mi);
                ContextMenu.Items.Add(new Separator());
                mi = new MenuItem() {Header = "Show all"};
                mi.Click += new RoutedEventHandler(ShowAll);
                ContextMenu.Items.Add(mi);
                ContextMenu.Items.Add(new Separator());
                mi = new MenuItem() {Header = "Properties"};
                mi.Click += new RoutedEventHandler(ShowProperties);
                ContextMenu.Items.Add(mi);
            }
            else
                ContextMenu = null;
        }

        private void ShowAll(object sender, RoutedEventArgs e)
        {
            DrawingControl.ShowAll();
        }

        private void ShowAllTypesOf(object sender, RoutedEventArgs e)
        {
            if (_currentProduct != null)
            {
                DrawingControl.Show(_currentProduct.GetType());
            }
        }

        private void HideProduct(object sender, RoutedEventArgs e)
        {
            if (_currentProduct != null)
            {
                DrawingControl.Hide(_currentProduct);
            }
        }

        private void HideAllTypesOf(object sender, RoutedEventArgs e)
        {
            if (_currentProduct != null)
            {
                DrawingControl.Hide(_currentProduct.GetType());
            }
        }


        private void FileImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();

            dlg.Filter = "Ifc Files|*.Ifc;*.Ifcx;*.IfcXml"; // Filter files by extension 
            dlg.Title = "Import/Merge Ifc model file";
            dlg.CheckFileExists = true;
            // Show open file dialog box 
            dlg.FileOk += new CancelEventHandler(dlg_ImportOk);
            dlg.ShowDialog();
        }

        private void dlg_ImportOk(object sender, CancelEventArgs ce)
        {
            OpenFileDialog dlg = sender as OpenFileDialog;
            if (dlg != null)
            {
                StatusBar.Visibility = Visibility.Visible;
                _worker = new BackgroundWorker();
                _worker.WorkerReportsProgress = true;
                _worker.WorkerSupportsCancellation = false;

                _worker.DoWork += delegate(object s, DoWorkEventArgs args)
                                      {
                                          BackgroundWorker worker = s as BackgroundWorker;

                                          try
                                          {

                                              string xbimFileName = Path.ChangeExtension(dlg.FileName, ".xbim");
                                              string xbimGeometryFileName = Path.ChangeExtension(dlg.FileName, ".xbimGC");
                                              XbimScene scene = new XbimScene(dlg.FileName, xbimFileName, xbimGeometryFileName, false);
                                              ModelProvider.Scene = scene.AsSceneStream();
                                          }
                                          catch (Exception ex)
                                          {
                                              args.Result = ex;
                                              return;
                                          }
                                      };

                _worker.ProgressChanged += delegate(object s, ProgressChangedEventArgs args)
                                               {
                                                   ProgressBar.Value = args.ProgressPercentage;
                                                   StatusMsg.Text = (string)args.UserState;
                                               };

                _worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
                                                  {
                                                      Exception e = args.Result as Exception;
                                                      if (e != null) //it failed
                                                      {
                                                          Exception ex = e;
                                                          StringBuilder msg = new StringBuilder();
                                                          while (ex != null)
                                                          {
                                                              msg.AppendLine(ex.Message);
                                                              ex = ex.InnerException;
                                                          }
                                                          MessageBox.Show(this, msg.ToString(), "Importing Ifc File",
                                                                          MessageBoxButton.OK, MessageBoxImage.Error,
                                                                          MessageBoxResult.None, MessageBoxOptions.None);
                                                      }

                                                  };

                _worker.RunWorkerAsync();
            }
        }

        private void FileExport_Click(object sender, RoutedEventArgs e)
        {
        }

        private void FileNew_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();

            dlg.Filter = "Xbim Files|*.xbim"; // Filter files by extension 
            dlg.Title = "Create New Xbim database";
            dlg.AddExtension = true;
            // Show open file dialog box 
            dlg.FileOk += new CancelEventHandler(dlg_FileOk);
            dlg.ShowDialog(this);
        }

        private void dlg_FileOk(object sender, CancelEventArgs e)
        {
            //SaveFileDialog dlg = sender as SaveFileDialog;
            //if (dlg != null)
            //{
            //    FileInfo fInfo = new FileInfo(dlg.FileName);
            //    try
            //    {
            //        if (fInfo.Exists) fInfo.Delete();
            //        ModelDataProvider modelProvider = ModelProvider;
            //        if (modelProvider != null)
            //        {
            //            ModelManager.ReleaseModel(modelProvider.Model);
            //            modelProvider.Model = new ModelPersisted(dlg.FileName);

            //        }
            //    }
            //    catch (Exception except)
            //    {

            //        MessageBox.Show(except.Message, "Error creating database", MessageBoxButton.OK, MessageBoxImage.Error);
            //    }

            //}
        }

        private void SaveAsIfcXmlClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Ifc Xml Files|*.ifcxml"; // Filter files by extension 
            dlg.Title = "Save As IfcXml File";
            dlg.AddExtension = true;
            // Show open file dialog box 
            dlg.FileOk += new CancelEventHandler(dlg_FileSaveAsIfcXml);
            dlg.ShowDialog(this);
        }

        private void SaveAsIfcClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Ifc Files|*.ifc"; // Filter files by extension 
            dlg.Title = "Save As Ifc File";
            dlg.AddExtension = true;
            // Show open file dialog box 
            dlg.FileOk += new CancelEventHandler(dlg_FileSaveAsIfc);
            dlg.ShowDialog(this);
        }

        private void dlg_FileSaveAsIfc(object sender, CancelEventArgs e)
        {
            SaveFileDialog dlg = sender as SaveFileDialog;
            if (dlg != null)
            {
                FileInfo fInfo = new FileInfo(dlg.FileName);
                try
                {
                    if (fInfo.Exists) fInfo.Delete();
                    ModelDataProvider modelProvider = ModelProvider;
                    XbimFileModelServer fs = modelProvider.Model as XbimFileModelServer;
                    if (fs != null) fs.ExportIfc(dlg.FileName);
                    else throw new Exception("Invalid Model Server");
                }
                catch (Exception except)
                {
                    MessageBox.Show(except.Message, "Error Saving as Ifc File", MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                }
            }
        }

        private void dlg_FileSaveAsIfcXml(object sender, CancelEventArgs e)
        {
            SaveFileDialog dlg = sender as SaveFileDialog;
            if (dlg != null)
            {
                FileInfo fInfo = new FileInfo(dlg.FileName);
                try
                {
                    if (fInfo.Exists) fInfo.Delete();
                    ModelDataProvider modelProvider = ModelProvider;
                    XbimFileModelServer fs = modelProvider.Model as XbimFileModelServer;
                    if (fs != null) fs.ExportIfcXml(dlg.FileName);
                    else throw new Exception("Invalid Model Server");
                }
                catch (Exception except)
                {
                    MessageBox.Show(except.Message, "Error Saving as Ifc File", MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                }
            }
        }

        private void SaveAsIfcZipClick(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Ifc Zip Files|*.ifczip"; // Filter files by extension 
            dlg.Title = "Save As IfcZip File";
            dlg.AddExtension = true;
            // Show open file dialog box 
            dlg.FileOk += new CancelEventHandler(dlg_SaveAsIfcZip);
            dlg.ShowDialog(this);
        }

        private void dlg_SaveAsIfcZip(object sender, CancelEventArgs e)
        {
            SaveFileDialog dlg = sender as SaveFileDialog;
            if (dlg != null)
            {
                FileInfo fInfo = new FileInfo(dlg.FileName);
                try
                {
                    if (fInfo.Exists) fInfo.Delete();
                    ModelDataProvider modelProvider = ModelProvider;
                    XbimFileModelServer fs = modelProvider.Model as XbimFileModelServer;
                    if (fs != null) fs.ExportIfc(dlg.FileName, true);
                    else throw new Exception("Invalid Model Server");
                }
                catch (Exception except)
                {
                    StringBuilder sb = new StringBuilder();
                    Exception ex = except;
                    String indent = "";
                    while (ex != null)
                    {
                        sb.AppendFormat("{0}{1}\n", indent, ex.Message);
                        ex = ex.InnerException;
                        indent += "\t";
                    }
                    MessageBox.Show(sb.ToString(), "Error Saving as Xbim File", MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                }
            }
        }
    }
}