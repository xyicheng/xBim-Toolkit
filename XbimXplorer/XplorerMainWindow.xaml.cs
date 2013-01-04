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
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Presentation;
using Xbim.XbimExtensions;
using Xbim.ModelGeometry;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions.Interfaces;
using Xbim.Ifc2x3.Extensions;
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
        private int? _currentProduct;
        private string _currentModelFileName;
        private string _temporaryXbimFileName;
        private string _defaultFileName;
        
        public XplorerMainWindow()
        {
            InitializeComponent();

            DrawingControl.SelectionChanged += new SelectionChangedEventHandler(DrawingControl_SelectionChanged);
            //SpatialControl.SelectedItemChanged +=new RoutedPropertyChangedEventHandler<SpatialStructureTreeItem>(SpatialControl_SelectedItemChanged);
            //DrawingControl.OnSetMaterial += new SetMaterialEventHandler(DrawingControl_OnSetMaterial);
            //DrawingControl.OnSetFilter += new SetFilterEventHandler(DrawingControl_OnSetFilter);
            this.Closed += new EventHandler(XplorerMainWindow_Closed);
           
            
        }

        void XplorerMainWindow_Closed(object sender, EventArgs e)
        {
            CloseAndDeleteTemporaryFiles();
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
                    return (p => p is IfcSlab);
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
            //////if (_materials == null)
            //////{
            //////    _materials = new Dictionary<string, XbimMaterialProvider>();
            //////    _materials.Add("201",
            //////                   new XbimMaterialProvider(
            //////                       new DiffuseMaterial(new SolidColorBrush(Colors.LightBlue) {Opacity = 0.7})));
            //////    _materials.Add("202",
            //////                   new XbimMaterialProvider(
            //////                       new DiffuseMaterial(new SolidColorBrush(Colors.LightGreen) {Opacity = 0.7})));
            //////    _materials.Add("211",
            //////                   new XbimMaterialProvider(
            //////                       new DiffuseMaterial(new SolidColorBrush(Colors.LightYellow) {Opacity = 0.7})));
            //////}
            //////XbimMaterialProvider mat;
            ////////do what you need here to set materials, this is just a default behaviour
            //////IfcSpace space = product as IfcSpace;
            //////if (space != null)
            //////{
            //////    switch (product.Name.ToString())
            //////    {
            //////        case "201":
            //////            mat = _materials["201"];
            //////            break;
            //////        case "202":
            //////            mat = _materials["202"];
            //////            break;
            //////        case "211":
            //////            mat = _materials["211"];
            //////            break;
            //////        default:
            //////            ModelDataProvider modelProvider = ModelProvider;
            //////            mat = new XbimMaterialProvider(modelProvider.GetDefaultMaterial(product));
            //////            break;
            //////    }
            //////}
            //////else
            //////{
            //////    ModelDataProvider modelProvider = ModelProvider;
            //////    mat = new XbimMaterialProvider(modelProvider.GetDefaultMaterial(product));
            //////}


            ////////create a list of materials and then reuse them, do not create a new material for each call
            //////return mat;
            return null;
        }

        private void SliderColour_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //byte nv = Convert.ToByte(e.NewValue);
            //if (_materials != null) //change the value of the first material
            //{
            //    XbimMaterialProvider firstMaterial = _materials.Values.FirstOrDefault();
            //    if (firstMaterial != null)
            //    {
            //        Color cl = Colors.LimeGreen;

            //        cl.R = nv;
            //        cl.G = Convert.ToByte(255 - nv);
            //        //cl.B = nv;
            //        //cl.A = 100;


            //        firstMaterial.FaceMaterial = new DiffuseMaterial(new SolidColorBrush(cl) {Opacity = 0.7});
            //        firstMaterial.BackgroundMaterial = firstMaterial.FaceMaterial; //set them both the same
            //    }
            //}
            e.Handled = true;
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


        private ObjectDataProvider ModelProvider
        {
            get
            {
                return MainFrame.DataContext as ObjectDataProvider;
               
            }
            
        }

        public XbimModel Model
        {
            get
            {
                ObjectDataProvider op = MainFrame.DataContext as ObjectDataProvider;
                return op == null ? null : op.ObjectInstance as XbimModel;
            }
        }
        private void OpenIfcFile(object s, DoWorkEventArgs args)
        {
            BackgroundWorker worker = s as BackgroundWorker;
            string ifcFilename = args.Argument as string;
            
            XbimModel model = new XbimModel();
            try
            {
                _temporaryXbimFileName = Path.GetTempFileName();
                _defaultFileName = Path.GetFileNameWithoutExtension(ifcFilename);
                model.CreateFrom(ifcFilename, _temporaryXbimFileName, worker.ReportProgress);
                model.Open(_temporaryXbimFileName, XbimDBAccess.ReadWrite);
                XbimScene.ConvertGeometry(model.Instances.OfType<IfcProduct>().Where(t=>!(t is IfcFeatureElement)), worker.ReportProgress, false);
                model.Close();
                model.Open(_temporaryXbimFileName, XbimDBAccess.Read, worker.ReportProgress);
                args.Result = model;
                
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
            XbimModel model = new XbimModel();
            try
            {
                _currentModelFileName = fileName.ToLower();
                model.Open(fileName, XbimDBAccess.Read, worker.ReportProgress); //load entities into the model
                args.Result = model;
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
                if (dlg.FileName.ToLower() == _currentModelFileName) //same file do nothing
                   return;
                switch (ext)
                {
                    case ".ifc": //it is an Ifc File
                    case ".ifcxml": //it is an IfcXml File
                    case ".ifczip": //it is a xip file containing xbim or ifc File
                    case ".zip": //it is a xip file containing xbim or ifc File
                        CloseAndDeleteTemporaryFiles();
                        _worker.DoWork += OpenIfcFile;
                        _worker.RunWorkerAsync(dlg.FileName);
                        break;
                    case ".xbim": //it is an xbim File, just open it in the main thread
                        CloseAndDeleteTemporaryFiles();
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
                                                  if (args.Result is XbimModel) //all ok
                                                  {
                                                      ModelProvider.ObjectInstance = (XbimModel)args.Result; //this Triggers the event to load the model into the views 
                                                      ModelProvider.Refresh();
                                                  }
                                                  else //we have a problem
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
            DrawingControl.ZoomExtents(_currentProduct);
        }

        private void DisplayPropertyWindow()
        {
            if (_propertyWindow == null)
            {
                _propertyWindow = new PropertiesWindow();
               
                _propertyWindow.Owner = this;

                Binding b = new Binding("SelectedItem.Entity");
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

      

        private void ShowAll(object sender, RoutedEventArgs e)
        {
            DrawingControl.ShowAll();
        }

        private void ShowAllTypesOf(object sender, RoutedEventArgs e)
        {
            if (_currentProduct.HasValue && Model !=null)
            {
                IPersistIfcEntity product = Model.Instances[_currentProduct.Value];
                DrawingControl.Show(product.GetType());
            }
        }

        private void HideProduct(object sender, RoutedEventArgs e)
        {
            if (_currentProduct.HasValue)
            {
                DrawingControl.Hide(_currentProduct.Value);
            }
        }

        private void HideAllTypesOf(object sender, RoutedEventArgs e)
        {
            if (_currentProduct.HasValue)
            {

                DrawingControl.HideAllTypesOf(_currentProduct.Value);
            }
        }
       


        private void dlg_FileSaveAs(object sender, CancelEventArgs e)
        {
            SaveFileDialog dlg = sender as SaveFileDialog;
            if (dlg != null)
            {
                FileInfo fInfo = new FileInfo(dlg.FileName);
                try
                {
                    if (fInfo.Exists) fInfo.Delete();

                    if (Model != null)
                    {
                        Model.SaveAs(dlg.FileName);
                       
                        if (string.Compare(Path.GetExtension(dlg.FileName),"XBIM",true)==0 && 
                            !string.IsNullOrWhiteSpace(_temporaryXbimFileName)) //we have a temp file open, it is now redundant as we have upgraded to another xbim file
                        {
                            File.Delete(_temporaryXbimFileName);
                            _temporaryXbimFileName = null;
                        }
                    }
                    else throw new Exception("Invalid Model Server");
                }
                catch (Exception except)
                {
                    MessageBox.Show(except.Message, "Error Saving as", MessageBoxButton.OK,
                                    MessageBoxImage.Error);
                }
            }
        }

       

        private void CommandBinding_SaveAs(object sender, ExecutedRoutedEventArgs e)
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.DefaultExt = "ifc";
            dlg.FileName = _defaultFileName;
            dlg.Filter = "xBIM File (*.xBIM)|*.xBIM|Ifc File (*.ifc)|*.ifc|IfcXml File (*.IfcXml)|*.ifcxml|IfcZip File (*.IfcZip)|*.ifczip"; // Filter files by extension 
            dlg.Title = "Save As";
            dlg.AddExtension = true;
           
            // Show open file dialog box 
            dlg.FileOk += new CancelEventHandler(dlg_FileSaveAs);
            dlg.ShowDialog(this);
        }

        private void CommandBinding_Close(object sender, ExecutedRoutedEventArgs e)
        {
            CloseAndDeleteTemporaryFiles();
        }

        private void CommandBinding_Open(object sender, ExecutedRoutedEventArgs e)
        {
           
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Xbim Files|*.xbim;*.ifc;*.ifcxml;*.ifczip"; // Filter files by extension 
            dlg.FileOk += new CancelEventHandler(dlg_OpenXbimFile);
            dlg.ShowDialog(this);
        }

        /// <summary>
        /// Tidies up any open files and closes any open models
        /// </summary>
        private void CloseAndDeleteTemporaryFiles()
        {
            try
            {
                XbimModel model = ModelProvider.ObjectInstance as XbimModel;
                if (model != null)
                {
                    model.Close();
                    ModelProvider.ObjectInstance = null;
                    ModelProvider.Refresh();
                }
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(_temporaryXbimFileName))
                    File.Delete(_temporaryXbimFileName);
                _temporaryXbimFileName = null;
                _defaultFileName = null;
            }
        }

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (e.Command == ApplicationCommands.Close || e.Command == ApplicationCommands.SaveAs)
            {
                XbimModel model = ModelProvider.ObjectInstance as XbimModel;
                e.CanExecute = (model != null);
            }

        }


        private void MenuItem_ZoomExtents(object sender, RoutedEventArgs e)
        {
            DrawingControl.ZoomExtents(null);
        }

    }
}
