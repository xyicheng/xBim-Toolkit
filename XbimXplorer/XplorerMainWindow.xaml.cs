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
using Xbim.Ifc2x3.ActorResource;
using Xbim.Common.Geometry;
using Xbim.COBie.Serialisers;
using Xbim.COBie;
using Xbim.COBie.Contracts;
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
            this.Loaded += XplorerMainWindow_Loaded;
            
        }

        void XplorerMainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            XbimModel model = XbimModel.CreateTemporaryModel();
            model.Initialise();
            ModelProvider.ObjectInstance = model;
            ModelProvider.Refresh();
        }

        void XplorerMainWindow_Closed(object sender, EventArgs e)
        {
            CloseAndDeleteTemporaryFiles();
        }

        void DrawingControl_ProgressChanged(object sender, ProgressChangedEventArgs e)
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
                GenerateGeometry(model, worker.ReportProgress);
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

      private void GenerateGeometry(XbimModel model, ReportProgressDelegate progDelegate)
      {

          IEnumerable<IfcProduct> toDraw = model.Instances.OfType<IfcProduct>(true).Where(t => !(t is IfcFeatureElement)); //exclude openings and additions
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
          ParallelOptions opts = new ParallelOptions();
          opts.MaxDegreeOfParallelism = 16;
          double deflection = 4;// model.GetModelFactors.OneMilliMetre * 10;
          try
          {
              XbimLOD lod = XbimLOD.LOD_Unspecified;
              //use parallel as this improves the OCC geometry generation greatly
              Parallel.ForEach<TransformNode>(graph.ProductNodes.Values,opts, node => //go over every node that represents a product
              // foreach (var node in graph.ProductNodes.Values)
              {
                  IfcProduct product = node.Product(model);
                  try
                  {
                      IXbimGeometryModel geomModel = XbimGeometryModel.CreateFrom(product, maps, false, lod,false);
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
                                  byte[] matrix = m3d.ToArray(true);
                                  short? typeId = IfcMetaData.IfcTypeId(product);
                                  foreach (var geomId in geomIds)
                                  {
                                      geomTable.AddMapGeometry(geomId, product.EntityLabel, typeId.Value, matrix, geomModel.SurfaceStyleLabel);
                                  }
                              }
                              else
                              {
                                  List<XbimTriangulatedModel> tm = geomModel.Mesh(true,deflection);
                                  Xbim.ModelGeometry.XbimBoundingBox bb = geomModel.GetBoundingBox(true);

                                  byte[] matrix = m3d.ToArray(true);
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
                      String message = String.Format("Error Triangulating product geometry of entity {0} - {1}",
                          product.EntityLabel,
                          product.ToString());
                      throw new XbimException(message, e1);
                  }
              }
               );
              // Debug.WriteLine(tally);
             
              //now sort out maps again in parallel
              Parallel.ForEach<KeyValuePair<int, Tuple<IXbimGeometryModel, XbimMatrix3D, IfcProduct>>>(mappedModels, opts, map =>
              //  foreach (var map in mappedModels)
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
                      WriteGeometry(model, written, geomModel, m3d, product);
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
                      WriteGeometry(model, written, geomModel, m3d, product);
                  }
                  else
                  {

                      byte[] matrix = m3d.ToArray(true);
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
              throw new XbimException("General Error Triangulating geometry", e2);
          }
          finally
          {

          }
      }

      private static void WriteGeometry(XbimModel model, ConcurrentDictionary<int, int[]> written, IXbimGeometryModel geomModel, XbimMatrix3D m3d, IfcProduct product)
      {
          List<XbimTriangulatedModel> tm = geomModel.Mesh(true);
          Xbim.ModelGeometry.XbimBoundingBox bb = geomModel.GetBoundingBox(true);
          byte[] matrix = m3d.ToArray(true);
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

        private void dlg_InsertXbimFile(object sender, CancelEventArgs e)
        {
             OpenFileDialog dlg = sender as OpenFileDialog;
            if (dlg != null)
            {
                FileInfo fInfo = new FileInfo(dlg.FileName);
                string ext = fInfo.Extension.ToLower();
                StatusBar.Visibility = Visibility.Visible;
               
                if (dlg.FileName.ToLower() == _currentModelFileName) //same file do nothing
                   return;
                switch (ext)
                {
                    case ".ifc": //it is an Ifc File
                    case ".ifcxml": //it is an IfcXml File
                    case ".ifczip": //it is a xip file containing xbim or ifc File
                    case ".zip": //it is a xip file containing xbim or ifc File
                       
                        //_worker.DoWork += OpenIfcFile;
                        //_worker.RunWorkerAsync(dlg.FileName);
                        break;
                    case ".xbim": //it is an xbim File, just open it in the main thread
                        Model.AddModelReference(dlg.FileName,"Organisation X",IfcRole.BuildingOperator);
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

        private void CommandBinding_New(object sender, ExecutedRoutedEventArgs e)
        {
            CloseAndDeleteTemporaryFiles();
            XbimModel model=   XbimModel.CreateTemporaryModel();
            model.Initialise();
            ModelProvider.ObjectInstance = Model;
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

        private void InsertModel(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.Filter = "Xbim Files|*.xbim;*.ifc;*.ifcxml;*.ifczip"; // Filter files by extension 
            dlg.FileOk += new CancelEventHandler(dlg_InsertXbimFile);
            dlg.ShowDialog(this);
        }

        private void ExportCoBie(object sender, RoutedEventArgs e)
        {

            string outputFile = Path.ChangeExtension(Model.DatabaseName, ".xls");

            // Build context
            COBieContext context = new COBieContext();
            context.TemplateFileName = "COBie-UK-2012-template.xls";
            context.Model = Model;
            //set filter option

            //switch (chckBtn.Name)
            //{
            //    case "rbDefault":
            //        break;
            //    case "rbPickList":
            //        context.ExcludeFromPickList = true;
            //        break;
            //    case "rbNoFilters":
            //        context.Exclude.Clear();
            //        break;
            //    default:
            //        break;
            //}
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
            context.TemplateCulture = "en-GB";
            COBieBuilder builder = new COBieBuilder(context);
            ICOBieSerialiser serialiser = new COBieXLSSerialiser(outputFile, context.TemplateFileName);
            builder.Export(serialiser);
            Process.Start(outputFile);

        }

    }
}
