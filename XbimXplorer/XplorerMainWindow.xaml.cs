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
using System.Collections.Concurrent;
using System.Threading.Tasks;
using System.Threading;
using Xbim.Common.Exceptions;
using System.Diagnostics;
using Xbim.ModelGeometry.Converter;
#endregion

namespace XbimXplorer
{
    /// <summary>
    ///   Interaction logic for Window1.xaml
    /// </summary>
    public partial class XplorerMainWindow : Window
    {
        private BackgroundWorker _worker;
       
        private string _currentModelFileName;
        private string _temporaryXbimFileName;
        private string _defaultFileName;
        
        public XplorerMainWindow()
        {
            InitializeComponent();    
            this.Closed += new EventHandler(XplorerMainWindow_Closed);
           
            
        }

        void XplorerMainWindow_Closed(object sender, EventArgs e)
        {
            CloseAndDeleteTemporaryFiles();
        }

        void DrawingControl_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            
        }

      

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }



        public int SelectedItem
        {
            get { return (int)GetValue(SelectedItemProperty); }
            set { SetValue(SelectedItemProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedItem.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(int), typeof(XplorerMainWindow), 
                                        new UIPropertyMetadata(-1, new PropertyChangedCallback(OnSelectedItemChanged)));


        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            XplorerMainWindow mw = d as XplorerMainWindow;
            if (mw != null && e.NewValue is int)
            {
                int label = (int)e.NewValue;
                mw.EntityLabel.Text = label > 0 ? "#" + label.ToString() : "";
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
                model.CreateFrom(ifcFilename, _temporaryXbimFileName, worker.ReportProgress);
                model.Open(_temporaryXbimFileName, XbimDBAccess.ReadWrite);
                XbimMesher.GenerateGeometry(model, null, worker.ReportProgress);
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
            DrawingControl.ZoomSelected();
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
            DrawingControl.ViewHome();
        }

    }
}
