﻿#region XbimHeader

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
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using Xbim.Common.Exceptions;
using System.Diagnostics;
using Xbim.Ifc2x3.ActorResource;
using Xbim.Common.Geometry;
using Xbim.COBie.Serialisers;
using Xbim.COBie;
using Xbim.COBie.Contracts;
using Xbim.ModelGeometry.Converter;
using XbimXplorer.Dialogs;
using System.Windows.Media.Imaging;
using Xbim.Presentation.FederatedModel;
#endregion

namespace XbimXplorer
{
    /// <summary>
    ///   Interaction logic for Window1.xaml
    /// </summary>
    public partial class XplorerMainWindow : Window
    {
        private BackgroundWorker _worker;
        public static RoutedCommand CreateFederationCmd = new RoutedCommand();
        public static RoutedCommand EditFederationCmd = new RoutedCommand();
        public static RoutedCommand OpenFederationCmd = new RoutedCommand();
        public static RoutedCommand InsertCmd = new RoutedCommand();
        public static RoutedCommand ExportCOBieCmd = new RoutedCommand();
        public static RoutedCommand COBieClassFilter = new RoutedCommand();
        private string _currentModelFileName;
        private string _temporaryXbimFileName;
        private string _defaultFileName;
        const string _UKTemplate = "COBie-UK-2012-template.xls";
        const string _USTemplate = "COBie-US-2_4-template.xls";

        private FilterValues UserFilters { get; set; }
        public string COBieTemplate { get; set; }

        public XplorerMainWindow()
        {
            InitializeComponent();
            this.Closed += new EventHandler(XplorerMainWindow_Closed);
            this.Loaded += XplorerMainWindow_Loaded;
            this.Closing += new CancelEventHandler(XplorerMainWindow_Closing);
            this.DrawingControl.UserModeledDimensionChangedEvent += DrawingControl_MeasureChangedEvent;

            UserFilters = new FilterValues();//COBie Class filters, set to initial defaults
            COBieTemplate = _UKTemplate;
        }

        private void DrawingControl_MeasureChangedEvent(DrawingControl3D m, Xbim.Presentation.ModelGeomInfo.PolylineGeomInfo e)
        {
            if (e != null)
            {
                this.EntityLabel.Text = e.ToString();
            }
        }

        void OpenQuery(object sender, RoutedEventArgs e)
        {
            XbimXplorer.Querying.wdwQuery qw = new Querying.wdwQuery();
            qw.ParentWindow = this;
            qw.Show();
        }

        void XplorerMainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (_worker != null && _worker.IsBusy)
                e.Cancel = true; //do nothing if a thread is alive
            else
                e.Cancel = false;

        }

        void XplorerMainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 1) //load one if specified
            {
                StatusBar.Visibility = Visibility.Visible;
                string toOpen = args[1];
                CreateWorker();
                string ext = Path.GetExtension(toOpen);
                switch (ext)
                {
                    case ".ifc": //it is an Ifc File
                    case ".ifcxml": //it is an IfcXml File
                    case ".ifczip": //it is a zip file containing xbim or ifc File
                    case ".zip": //it is a zip file containing xbim or ifc File
                        CloseAndDeleteTemporaryFiles();
                        _worker.DoWork += OpenIfcFile;
                        _worker.RunWorkerAsync(toOpen);
                        break;
                    case ".xbimf":
                    case ".xbim": //it is an xbim File, just open it in the main thread
                        CloseAndDeleteTemporaryFiles();
                        _worker.DoWork += OpenXbimFile;
                        _worker.RunWorkerAsync(toOpen);
                        break;
                    default:
                        break;
                }
            }
            else //just create an empty model
            {
                XbimModel model = XbimModel.CreateTemporaryModel();
                model.Initialise();
                ModelProvider.ObjectInstance = model;
                ModelProvider.Refresh();
            }
        }

        void XplorerMainWindow_Closed(object sender, EventArgs e)
        {
            CloseAndDeleteTemporaryFiles();
        }

        void DrawingControl_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            
        }

        public IPersistIfcEntity SelectedItem
        {
            get { return (IPersistIfcEntity)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedItem.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(IPersistIfcEntity), typeof(XplorerMainWindow), 
                                        new UIPropertyMetadata(null, new PropertyChangedCallback(OnSelectedItemChanged)));


        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            XplorerMainWindow mw = d as XplorerMainWindow;
            if (mw != null && e.NewValue is IPersistIfcEntity)
            {
                IPersistIfcEntity label = (IPersistIfcEntity)e.NewValue;
                mw.EntityLabel.Text = label !=null ? "#" + label.EntityLabel.ToString() : "";
            }
            else
                mw.EntityLabel.Text = "";
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
                model.CreateFrom(ifcFilename, _temporaryXbimFileName, worker.ReportProgress,true,false);
              //  model.Open(_temporaryXbimFileName, XbimDBAccess.ReadWrite);
                XbimMesher.GenerateGeometry(model, null, worker.ReportProgress);
               // model.Close();
                if (worker.CancellationPending == true) //if a cancellation has been requested then don't open the resulting file
                {
                    try
                    {
                        model.Close();
                        if (File.Exists(_temporaryXbimFileName))
                            File.Delete(_temporaryXbimFileName); //tidy up;
                        _temporaryXbimFileName = null;
                        _defaultFileName = null;
                    }
                    catch (Exception)
                    {


                    }
                    return;
                }
              //  model.Open(_temporaryXbimFileName, XbimDBAccess.ReadWrite, worker.ReportProgress);

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

        private void InsertIfcFile(object s, DoWorkEventArgs args)
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
                XbimMesher.GenerateGeometry(model, null, worker.ReportProgress);
                model.Close();
                if (worker.CancellationPending == true) //if a cancellation has been requested then don't open the resulting file
                {
                    try
                    {
                        if (File.Exists(_temporaryXbimFileName))
                            File.Delete(_temporaryXbimFileName); //tidy up;
                        _temporaryXbimFileName = null;
                        _defaultFileName = null;
                    }
                    catch (Exception)
                    {


                    }
                    return;
                }
               // model.Open(_temporaryXbimFileName, XbimDBAccess.Read, worker.ReportProgress);
                this.Dispatcher.BeginInvoke(new Action(() => { Model.AddModelReference(_temporaryXbimFileName, "Organisation X", IfcRole.BuildingOperator); }), System.Windows.Threading.DispatcherPriority.Background);
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

                if (model.IsFederation)
                {
                    model.Close();
                    model.Open(fileName, XbimDBAccess.ReadWrite, worker.ReportProgress); // federations need to be opened in read/write for the editor to work
                    // sets a convenient integer to all children for model identification
                    // this is used by the federated model selection mechanisms.
                    int i = 0;
                    foreach (var item in model.AllModels)
                    {
                        item.Tag = i++;
                    }
                }

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

        private void dlg_InsertXbimFile(object sender, CancelEventArgs e)
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
                        _worker.DoWork += InsertIfcFile;
                        _worker.RunWorkerAsync(dlg.FileName);
                        break;
                    case ".xbimf":
                    case ".xbim": //it is an xbim File, just open it in the main thread
                        Model.AddModelReference(dlg.FileName,"Organisation X", IfcRole.BuildingOperator);
                        break;
                    default:
                        break;
                }
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
                    case ".xbimf":
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
            _worker.WorkerSupportsCancellation = true;
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
                                                      // PropertiesControl.Model = (XbimModel)args.Result; // // done thtough binding in xaml
                                                      ModelProvider.Refresh();

                                                      ProgressBar.Value = 0;
                                                      StatusMsg.Text = "Ready";
                                                  }
                                                  else //we have a problem
                                                  {
                                                      string errMsg = args.Result as String;
                                                      if (!string.IsNullOrEmpty(errMsg))
                                                          MessageBox.Show(this, errMsg, "Error Opening File",
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
                                                      ProgressBar.Value = 0;
                                                      StatusMsg.Text = "Error/Ready";
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

        
        // this variable is used to determine when the user is trying again to double click on the selected item
        // from this we detect that he's probably not happy with the view, therefore we add a cutting plane to make the 
        // element visible.
        //
        private bool _camChanged = false;
        private void SpatialControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            _camChanged = false;
            DrawingControl.Viewport.Camera.Changed += Camera_Changed;
            DrawingControl.ZoomSelected();
            DrawingControl.Viewport.Camera.Changed -= Camera_Changed;
            if (!_camChanged)
                DrawingControl.ClipBaseSelected(0.15);
        }

        void Camera_Changed(object sender, EventArgs e)
        {
            _camChanged = true;
        }

        private void dlg_FileSaveAs(object sender, CancelEventArgs e)
        {
            SaveFileDialog dlg = sender as SaveFileDialog;
            if (dlg != null)
            {
                FileInfo fInfo = new FileInfo(dlg.FileName);
                try
                {
                    if (fInfo.Exists)
                    {
                        // the user has been asked to confirm deletion previously
                        fInfo.Delete();
                    }
                    if (Model != null)
                    {
                        Model.SaveAs(dlg.FileName);
                        string extension = Path.GetExtension(dlg.FileName).ToLowerInvariant();
                        if (extension == "xbim" && !string.IsNullOrWhiteSpace(_temporaryXbimFileName))  //we have a temp file open, it is now redundant as we have upgraded to another xbim file
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
            string filter = "xBIM File (*.xBIM)|*.xBIM|IfcXml File (*.IfcXml)|*.ifcxml|IfcZip File (*.IfcZip)|*.ifczip";
            if (Model.IsFederation)
            {
                dlg.DefaultExt = "xBIMF";
                filter = "xBIM Federation file (*.xBIMF)|*.xbimf|Ifc File (*.ifc)|*.ifc|" + filter;
            }
            else
            {
                dlg.DefaultExt = "ifc";
                filter = "Ifc File (*.ifc)|*.ifc|" +  filter + "|xBIM Federation file (*.xBIMF)|*.xbimf" ;
            }

            dlg.Filter = filter;// Filter files by extension 
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

        private void CommandBinding_New(object sender, ExecutedRoutedEventArgs e)
        {
            CloseAndDeleteTemporaryFiles();
            XbimModel model=   XbimModel.CreateTemporaryModel();
            model.Initialise();
            ModelProvider.ObjectInstance = model;
            ModelProvider.Refresh();

        }
        
       
        private void CommandBinding_Open(object sender, ExecutedRoutedEventArgs e)
        {
           
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Xbim Files|*.xbim;*.xbimf;*.ifc;*.ifcxml;*.ifczip"; // Filter files by extension 
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
                if (_worker != null && _worker.IsBusy)
                    _worker.CancelAsync(); //tell it to stop
                XbimModel model = ModelProvider.ObjectInstance as XbimModel;
                _currentModelFileName = null;
                if (model != null)
                {
                    model.Dispose();
                    ModelProvider.ObjectInstance = null;
                    // PropertiesControl.Model = null; // done thtough binding in xaml
                    ModelProvider.Refresh();
                }
            }
            finally
            {
                if (!(_worker != null && _worker.IsBusy && _worker.CancellationPending)) //it is still busy but has been cancelled 
                {
             
                    if (!string.IsNullOrWhiteSpace(_temporaryXbimFileName) && File.Exists(_temporaryXbimFileName))
                        File.Delete(_temporaryXbimFileName);
                    _temporaryXbimFileName = null;
                    _defaultFileName = null;
                } //else do nothing it will be cleared up in the worker thread
            }
        }

        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            if (_worker != null && _worker.IsBusy)
                e.CanExecute = false;
            else
            {
                if (e.Command == ApplicationCommands.Close || e.Command == ApplicationCommands.SaveAs)
                {
                    XbimModel model = ModelProvider.ObjectInstance as XbimModel;
                    e.CanExecute = (model != null);
                }
                else
                    e.CanExecute = true; //for everything else
            }

        }


        private void MenuItem_ZoomExtents(object sender, RoutedEventArgs e)
        {
            DrawingControl.ViewHome();
        }

        private void ExportCOBieCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            string outputFile = Path.ChangeExtension(Model.DatabaseName, ".xls");

            // Build context
            COBieContext context = new COBieContext();
            context.TemplateFileName = COBieTemplate;
            context.Model = Model;
            //set filter option
            context.Exclude = UserFilters;

            //set the UI language to get correct resource file for template
            //if (Path.GetFileName(parameters.TemplateFile).Contains("-UK-"))
            //{
            try
            {
                System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("en-GB");
                System.Threading.Thread.CurrentThread.CurrentUICulture = ci;
            }
            catch (Exception)
            {
                //to nothing Default culture will still be used

            }

            COBieBuilder builder = new COBieBuilder(context);
            COBieXLSSerialiser serialiser = new COBieXLSSerialiser(outputFile, context.TemplateFileName);
            serialiser.Excludes = UserFilters;
            builder.Export(serialiser);
            Process.Start(outputFile);
        }

        // CanExecuteRoutedEventHandler for the custom color command.
        private void ExportCOBieCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            XbimModel model = ModelProvider.ObjectInstance as XbimModel;
            bool canEdit = (model!=null && model.CanEdit && model.Instances.OfType<IfcBuilding>().FirstOrDefault()!=null);       
            e.CanExecute = canEdit && !(_worker != null && _worker.IsBusy);
        }

        private void COBieClassFilterCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            COBieClassFilter classFilterDlg = new COBieClassFilter(UserFilters);
            bool? done = classFilterDlg.ShowDialog();
            if (done.HasValue && done.Value == true)
            {
                UserFilters = classFilterDlg.UserFilters; //not needed, but makes intent clear
            }
        }

        private void COBieClassFilterCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void InsertCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Xbim Files|*.xbim;*.ifc;*.ifcxml;*.ifczip"; // Filter files by extension 
            dlg.FileOk += new CancelEventHandler(dlg_InsertXbimFile);
            dlg.ShowDialog(this);
        }

        // CanExecuteRoutedEventHandler for the custom color command.
        private void InsertCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            XbimModel model = ModelProvider.ObjectInstance as XbimModel;
            bool canEdit = (model!=null && model.CanEdit);       
            e.CanExecute = canEdit && !(_worker != null && _worker.IsBusy);
        }

        private void EditFederationCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            FederatedModelDialog fdlg = new FederatedModelDialog();          
            fdlg.DataContext = Model;
            bool? done = fdlg.ShowDialog();
            if (done.HasValue && done.Value == true)
            {

            }
        }
        private void EditFederationCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            
            e.CanExecute = Model!=null &&  Model.IsFederation;
        }

        private void OpenFederationCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Xbim Federation Files|*.xbimf|Xbim Model Files|*.ifc"; // Filter files by extension 
            dlg.CheckFileExists = true;
            dlg.Multiselect = true;
            bool? done = dlg.ShowDialog(this);
            if (done.HasValue && done.Value == true)
            {
                if (dlg.FileNames.Any()) // collection is not empty
                {
                    //use the first filename it's extension to decide which action should happen
                    var firstExtension = Path.GetExtension(dlg.FileNames[0]).ToLower();

                    XbimModel fedModel = null;
                    if (firstExtension == ".xbimf")
                    {
                        if (dlg.FileNames.Length > 1)
                        {
                            var res = MessageBox.Show("Multiple files selected, open " + dlg.FileNames[0] + "?", "Cannot open multiple Xbim files", 
                                MessageBoxButton.OKCancel, MessageBoxImage.Warning);
                            if (res == MessageBoxResult.Cancel)
                                return;
                        }
                        fedModel = new XbimModel();
                        fedModel.Open(dlg.FileNames[0], XbimDBAccess.ReadWrite);
                    }
                    else if (firstExtension == ".ifc")
                    {
                        //create temp file as a placeholder for the temperory xbim file
                        var filePath = Path.GetTempFileName();
                        filePath = Path.ChangeExtension(filePath, "xbimf");
                        fedModel = XbimModel.CreateModel(filePath);
                        fedModel.Initialise("Default Author", "Default Organization");
                        using (var txn = fedModel.BeginTransaction())
                        {
                            fedModel.IfcProject.Name = "Default Project Name";
                            txn.Commit();
                        }


                        bool informUser = true;
                        for (int i = 0; i < dlg.FileNames.Length; i++)
                        {
                            var fileName = dlg.FileNames[i];
                            var builder = new XbimReferencedModelViewModel();
                            builder.Name = fileName;
                            builder.OrganisationName = "OrganisationName " + i;
                            builder.OrganisationRole = "Undefined";

                            bool buildRes = false;
                            Exception exception = null;
                            try
                            {
                                buildRes = builder.TryBuild(fedModel);
                            }
                            catch (Exception ex)
                            {
                                //usually an EsentDatabaseSharingViolationException, user needs to close db first
                                exception = ex;
                            }

                            if (!buildRes && informUser)
                            {
                                string msg = exception == null ? "" : "\r\nMessage: " + exception.Message;
                                var res = MessageBox.Show(fileName + " couldn't be opened." + msg + "\r\nShow this message again?", 
                                    "Failed to open a file", MessageBoxButton.YesNoCancel, MessageBoxImage.Error);
                                if (res == MessageBoxResult.No)
                                    informUser = false;
                                else if (res == MessageBoxResult.Cancel)
                                {
                                    fedModel = null;
                                    break;
                                }
                            }
                        }
                    }
                    if (fedModel != null)
                    {
                        CloseAndDeleteTemporaryFiles();
                        ModelProvider.ObjectInstance = fedModel;
                        ModelProvider.Refresh();

                    }
                }
            }
        }

    

        private void OpenFederationCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        //private void CreateFederationCmdExecuted(object sender, ExecutedRoutedEventArgs e)
        //{
        //    CreateFederationWindow fedwin = new CreateFederationWindow();
        //    bool? done = fedwin.ShowDialog();
        //    if (done.HasValue && done.Value == true)
        //    {
        //        if (File.Exists(fedwin.ModelFullPath))
        //        {
        //            if (MessageBox.Show(fedwin.ModelFullPath + " Exists.\nDo you want to overwrite it?", "Overwrite file", MessageBoxButton.YesNo) == MessageBoxResult.No)
        //                return;
        //        }
        //        try
        //        {
        //            XbimModel fedModel = XbimModel.CreateModel(fedwin.ModelFullPath);

        //            fedModel.Initialise(fedwin.Author, fedwin.Organisation);
        //            using (var txn = fedModel.BeginTransaction())
        //            {
        //                fedModel.IfcProject.Name = fedwin.Project;
        //                txn.Commit();
        //            }
        //            //FederatedModelDlg fdlg = new FederatedModelDlg();
        //            //fdlg.DataContext = Model;
        //            //fdlg.ShowDialog();
        //            CloseAndDeleteTemporaryFiles();
        //            ModelProvider.ObjectInstance = fedModel;
        //            ModelProvider.Refresh();
        //            //fedModel.SaveAs(Path.ChangeExtension(fedwin.ModelFullPath, ".ifc"), XbimStorageType.IFC);

        //        }
        //        catch (Exception ex)
        //        {
        //            MessageBox.Show(ex.Message, "Model Creation Failed", MessageBoxButton.OK);
        //        }
        //    }
        //}
        //private void CreateFederationCmdCanExecute(object sender, CanExecuteRoutedEventArgs e)
        //{
        //    e.CanExecute = true;
        //}

        private void SeparateMenuItem_Click(object sender, RoutedEventArgs e)
        {
            ModelSeparation separate = new ModelSeparation();

            //set data binding
            Binding b = new Binding("DataContext");
            b.Source = this.MainFrame;
            b.Mode = BindingMode.TwoWay;
            separate.SetBinding(ModelSeparation.DataContextProperty, b);

            separate.Show();
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            Gat.Controls.About about = new Gat.Controls.About();
            //
            about.Title = "xBIM Xplorer";
            about.Hyperlink = new Uri("http://xbim.codeplex.com",UriKind.Absolute);
            about.HyperlinkText = "http://xbim.codeplex.com";
            about.Publisher="xBIM Team - Steve Lockley";
            about.Description = "This application is designed to demonstrate potential usages of the xBIM toolkit";
            about.ApplicationLogo = new BitmapImage(new Uri(@"pack://application:,,/xBIM.ico", UriKind.RelativeOrAbsolute));
            about.Copyright = "Prof. Steve Lockley";
            about.PublisherLogo = about.ApplicationLogo;
            about.AdditionalNotes = "The xBIM toolkit is an Open Source software initiative to help software developers and researchers to support the next generation of BIM tools; unlike other open source application xBIM license is compatible with commercial environments (http://xbim.codeplex.com/license)";
            about.Show();
        }

        private void UKTemplate_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (MenuItem)sender;

            if (mi.IsChecked)
            {
                COBieTemplate = _UKTemplate;
                if (US.IsChecked)
                {
                    US.IsChecked = false;
                }
            }
            else
            {
                US.IsChecked = true;
                COBieTemplate = _USTemplate;
            }
        }

        private void USTemplate_Click(object sender, RoutedEventArgs e)
        {
            MenuItem mi = (MenuItem)sender;
            
            if (mi.IsChecked)
            {
                COBieTemplate = _USTemplate;
                if (UK.IsChecked)
                {
                    UK.IsChecked = false;
                }
            }
            else
            {
                UK.IsChecked = true;
                COBieTemplate = _UKTemplate;
            }
        }

        private void OpenScriptingWindow(object sender, RoutedEventArgs e)
        {
            var win = new Scripting.ScriptingWindow();
            win.Owner = this;

            win.ScriptingConcrol.DataContext = ModelProvider;
            var binding = new Binding();
            win.ScriptingConcrol.SetBinding(ScriptingControl.ModelProperty, binding);

            win.ScriptingConcrol.OnModelChangedByScript += delegate(object o, Xbim.Script.ModelChangedEventArgs arg)
            {
                ModelProvider.ObjectInstance = null;
                XbimMesher.GenerateGeometry(arg.NewModel);
                ModelProvider.ObjectInstance = arg.NewModel;
                ModelProvider.Refresh();
            };

            win.ScriptingConcrol.OnScriptParsed += delegate(object o, Xbim.Script.ScriptParsedEventArgs arg)
            {
                GroupControl.Regenerate();
                //SpatialControl.Regenerate();
            };
            

            ScriptResults.Visibility = Visibility.Visible;
            win.Closing += new CancelEventHandler(delegate(object s, CancelEventArgs arg) {
                ScriptResults.Visibility = Visibility.Collapsed; 
            });
            
            win.Show();
        }
    }
}
