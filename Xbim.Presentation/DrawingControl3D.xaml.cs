#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Presentation
// Filename:    DrawingControl3D.xaml.cs
// Published:   01, 2012
// Last Edited: 9:05 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.ModelGeometry;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.SharedComponentElements;
using Xbim.XbimExtensions.Interfaces;
using Xbim.IO;
using System.Diagnostics;
using System.Windows.Markup;
using Xbim.Common.Exceptions;
using System.Threading;
using Xbim.Ifc2x3;
using HelixToolkit.Wpf;

#endregion

namespace Xbim.Presentation
{


    /// <summary>
    ///   Interaction logic for DrawingControl3D.xaml
    /// </summary>
    public partial class DrawingControl3D : UserControl
    {
        public DrawingControl3D()
        {
            InitializeComponent();
            _selectedVisualMaterial = new DiffuseMaterial(Brushes.LightGreen);
            Viewport = Canvas;
            Canvas.MouseDown += Canvas_MouseDown;
            this.Loaded += DrawingControl3D_Loaded;
           
        }

        void DrawingControl3D_Loaded(object sender, RoutedEventArgs e)
        {
            ShowSpaces = false;
        }


        #region Fields
        private XbimScene<WpfMeshGeometry3D, WpfMaterial> scene = new XbimScene<WpfMeshGeometry3D, WpfMaterial>();


        private BackgroundWorker _worker;
        

        private int? _currentProduct;
        protected RayMeshGeometry3DHitTestResult _hitResult;
        protected Material _selectedVisualMaterial;
        private Rect3D _boundingBox;
       
        private event ProgressChangedEventHandler _progressChanged;

        public event ProgressChangedEventHandler ProgressChanged
        {
            add { _progressChanged += value; }
            remove { _progressChanged -= value; }
        }
        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        /// <value>The model.</value>
        public Model3D Model3d { get; set; }



        #endregion

        #region Events

        public static readonly RoutedEvent SelectionChangedEvent =
            EventManager.RegisterRoutedEvent("SelectionChangedEvent", RoutingStrategy.Bubble,
                                             typeof(SelectionChangedEventHandler), typeof(DrawingControl3D));

        public event SelectionChangedEventHandler SelectionChanged
        {
            add { AddHandler(SelectionChangedEvent, value); }
            remove { RemoveHandler(SelectionChangedEvent, value); }
        }

        public new static readonly RoutedEvent LoadedEvent =
            EventManager.RegisterRoutedEvent("LoadedEvent", RoutingStrategy.Bubble, typeof(RoutedEventHandler),
                                             typeof(DrawingControl3D));

        public new event RoutedEventHandler Loaded
        {
            add { AddHandler(LoadedEvent, value); }
            remove { RemoveHandler(LoadedEvent, value); }
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {

            var pos = e.GetPosition(Canvas);
            var hit = FindHit(pos);
        
            if (hit != null)
            {
                XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> layer = hit.ModelHit.GetValue(TagProperty) as XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>; //get the fragments
                if (layer!=null)
                {
                    //var frag = layer.Rendered.Find(f => f.Contains(hit.VertexIndex1));
                    //if (!frag.IsEmpty)
                    //{

                    //    int id = frag.EntityLabel;
                    //    IList remove;
                    //    if (_currentProduct.HasValue)
                    //    {
                    //        remove = new int[] { _currentProduct.Value };
                    //    }
                    //    else
                    //        remove = new int[] { };
                    //    _hitResult = hit;
                    //    _currentProduct = (int)id;
                    //    SelectedItem = _currentProduct.Value;
                    //    if (!PropertiesBillBoard.IsRendering)
                    //    {
                    //        this.Viewport.Children.Add(PropertiesBillBoard);
                    //        PropertiesBillBoard.IsRendering = true;
                    //    }
                    //    PropertiesBillBoard.Text = Model.Instances[_currentProduct.Value].SummaryString().EnumerateToString(null, "\n");
                    //    PropertiesBillBoard.Position = hit.PointHit;
                    //    hitMesh = hit.MeshHit.GetMeshGeometry3D(frag);
                    //    Highlighted.Mesh = new Mesh3D(hitMesh.Positions, hitMesh.TriangleIndices);
                    //    return;
                   // }
                }
            }

            PropertiesBillBoard.IsRendering = false;          
            this.Viewport.Children.Remove(PropertiesBillBoard);
            Highlighted.Mesh = null; ;

        }

        #endregion

        #region Dependency Properties



        public XbimModel Model
        {
            get { return (XbimModel)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Model.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModelProperty =
            DependencyProperty.Register("Model", typeof(XbimModel), typeof(DrawingControl3D), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits,
                                                                      new PropertyChangedCallback(OnModelChanged)));

        private static void OnModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DrawingControl3D d3d = d as DrawingControl3D;
            if (d3d != null)
            {
                XbimModel model = e.NewValue as XbimModel;
                d3d.LoadGeometry(model);
            }

        }


        public bool ShowWalls
        {
            get { return (bool)GetValue(ShowWallsProperty); }
            set { SetValue(ShowWallsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowWalls.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowWallsProperty =
            DependencyProperty.Register("ShowWalls", typeof(bool), typeof(DrawingControl3D), new UIPropertyMetadata(true, OnShowWallsChanged));

        private static void OnShowWallsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DrawingControl3D d3d = d as DrawingControl3D;
            if (d3d != null)
            {
                if (e.NewValue is bool)
                {
                    bool on = (bool)e.NewValue;
                    if (on)
                        d3d.Show<IfcWall>();
                    else
                        d3d.Hide<IfcWall>();
                }
            }
        }

        public bool ShowDoors
        {
            get { return (bool)GetValue(ShowDoorsProperty); }
            set { SetValue(ShowDoorsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowWalls.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowDoorsProperty =
            DependencyProperty.Register("ShowDoors", typeof(bool), typeof(DrawingControl3D), new UIPropertyMetadata(true, OnShowDoorsChanged));

        private static void OnShowDoorsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DrawingControl3D d3d = d as DrawingControl3D;
            if (d3d != null)
            {
                if (e.NewValue is bool)
                {
                    bool on = (bool)e.NewValue;
                    if (on)
                        d3d.Show<IfcDoor>();
                    else
                        d3d.Hide<IfcDoor>();
                }
            }
        }

        public bool ShowWindows
        {
            get { return (bool)GetValue(ShowWindowsProperty); }
            set { SetValue(ShowWindowsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowWalls.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowWindowsProperty =
            DependencyProperty.Register("ShowWindows", typeof(bool), typeof(DrawingControl3D), new UIPropertyMetadata(true, OnShowWindowsChanged));

        private static void OnShowWindowsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DrawingControl3D d3d = d as DrawingControl3D;
            if (d3d != null)
            {
                if (e.NewValue is bool)
                {
                    if ((bool)e.NewValue)
                        d3d.Show<IfcWindow>();
                    else
                        d3d.Hide<IfcWindow>();
                }
            }
        }

        public bool ShowSlabs
        {
            get { return (bool)GetValue(ShowSlabsProperty); }
            set { SetValue(ShowSlabsProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowWalls.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowSlabsProperty =
            DependencyProperty.Register("ShowSlabs", typeof(bool), typeof(DrawingControl3D), new UIPropertyMetadata(true, OnShowSlabsChanged));

        private static void OnShowSlabsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DrawingControl3D d3d = d as DrawingControl3D;
            if (d3d != null)
            {
                if (e.NewValue is bool)
                {
                    if ((bool)e.NewValue)
                        d3d.Show<IfcSlab>();
                    else
                        d3d.Hide<IfcSlab>();
                }
            }
        }
        public bool ShowFurniture
        {
            get { return (bool)GetValue(ShowFurnitureProperty); }
            set { SetValue(ShowFurnitureProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowWalls.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowFurnitureProperty =
            DependencyProperty.Register("ShowFurniture", typeof(bool), typeof(DrawingControl3D), new UIPropertyMetadata(true, OnShowFurnitureChanged));

        private static void OnShowFurnitureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DrawingControl3D d3d = d as DrawingControl3D;
            if (d3d != null)
            {
                if (e.NewValue is bool)
                {
                    if ((bool)e.NewValue)
                        d3d.Show<IfcFurnishingElement>();
                    else
                        d3d.Hide<IfcFurnishingElement>();
                }
            }
        }

        public bool ShowGridLines
        {
            get { return (bool)GetValue(ShowGridLinesProperty); }
            set { SetValue(ShowGridLinesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowWalls.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowGridLinesProperty =
            DependencyProperty.Register("ShowGridLines", typeof(bool), typeof(DrawingControl3D), new UIPropertyMetadata(true, OnShowGridLinesChanged));

        private static void OnShowGridLinesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DrawingControl3D d3d = d as DrawingControl3D;
            if (d3d != null)
            {
                if (e.NewValue is bool)
                {
                    if ((bool)e.NewValue)
                        d3d.Viewport.Children.Insert(0, d3d.GridLines);
                    else
                        d3d.Viewport.Children.Remove( d3d.GridLines);
                }
            }
        }
        public bool ShowSpaces
        {
            get { return (bool)GetValue(ShowSpacesProperty); }
            set { SetValue(ShowSpacesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ShowWalls.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ShowSpacesProperty =
            DependencyProperty.Register("ShowSpaces", typeof(bool), typeof(DrawingControl3D), new UIPropertyMetadata(true, OnShowSpacesChanged));

        private static void OnShowSpacesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            DrawingControl3D d3d = d as DrawingControl3D;
            if (d3d != null)
            {
                if (e.NewValue is bool)
                {
                    if ((bool)e.NewValue)
                        d3d.Show<IfcSpace>();
                    else
                        d3d.Hide<IfcSpace>();
                }
            }
        }

        public HelixToolkit.Wpf.HelixViewport3D Viewport
        {
            get { return (HelixToolkit.Wpf.HelixViewport3D)GetValue(ViewportProperty); }
            set { SetValue(ViewportProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Viewport.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ViewportProperty =
            DependencyProperty.Register("Viewport", typeof(HelixToolkit.Wpf.HelixViewport3D), typeof(DrawingControl3D), new PropertyMetadata(null));



        public int? SelectedItem
        {
            get { return (int?)GetValue(SelectedItemProperty); }
            set
            {
                SetValue(SelectedItemProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for SelectedItem.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof(int?), typeof(DrawingControl3D),
                                        new UIPropertyMetadata(null, new PropertyChangedCallback(OnSelectedItemChanged)));

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DrawingControl3D)
            {
                DrawingControl3D d3d = d as DrawingControl3D;
                if (e.OldValue is int) //there is an old value to deselect
                {
                    int oldVal = (int)e.OldValue;
                    d3d.Deselect(oldVal);

                }
                if (e.NewValue is int)
                {
                    int newVal = (int)e.NewValue;
                    d3d.Select(newVal);
                }


            }

        }

        private void Deselect(int oldVal)
        {
            //ModelVisual3D mv3d;
            //if (_items.TryGetValue(oldVal, out mv3d) && mv3d.Content != null)
            //{

            //    UnHighlight(mv3d);
            //}
        }

        private void Select(int newVal)
        {
            //ModelVisual3D mv3d;
            //if (_items.TryGetValue(newVal, out mv3d) && mv3d.Content != null)
            //{
            //    Highlight(mv3d);
            //}
        }


        private void UnHighlight(ModelVisual3D m3d)
        {

            if (m3d.Content != null)
                UnHighlight(m3d.Content);
        }


        private void UnHighlight(Model3D m3d)
        {

            if (m3d is Model3DGroup)
            {
                foreach (var item in ((Model3DGroup)m3d).Children)
                {
                    UnHighlight(item);
                }
            }
            else if (m3d is GeometryModel3D)
            {
                GeometryModel3D g3d = (GeometryModel3D)m3d;
                XbimMaterialProvider matProv = g3d.GetValue(TagProperty) as XbimMaterialProvider;
                if (matProv != null)
                {
                    BindingOperations.SetBinding(g3d, GeometryModel3D.BackMaterialProperty, matProv.BackgroundMaterialBinding);
                    BindingOperations.SetBinding(g3d, GeometryModel3D.MaterialProperty, matProv.FaceMaterialBinding);
                    g3d.SetValue(TagProperty, null);
                }
            }
        }

        private RayMeshGeometry3DHitTestResult FindHit(Point position)
        {
            RayMeshGeometry3DHitTestResult result = null;
            HitTestResultCallback callback = hit =>
            {
                var rayHit = hit as RayMeshGeometry3DHitTestResult;
                if (rayHit != null)
                {
                    if (rayHit.MeshHit != null)
                    {
                        result = rayHit;
                        return HitTestResultBehavior.Stop;
                    }
                }

                return HitTestResultBehavior.Continue;
            };
            var hitParams = new PointHitTestParameters(position);
            VisualTreeHelper.HitTest(Viewport.Viewport, null, callback, hitParams);
            return result;
        }

        private XbimMaterialModelVisual FindMaterialVisual(ModelVisual3D mv)
        {

            DependencyObject parent = mv;
            while (parent != null)
            {
                var vp = parent as XbimMaterialModelVisual;
                if (vp != null)
                {
                    return vp as XbimMaterialModelVisual;
                }

                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }
        private void Highlight(ModelVisual3D mv3d)
        {
            if (mv3d.Content != null)
                Highlight(mv3d.Content);
        }

        private void Highlight(Model3D m3d)
        {
            if (m3d is Model3DGroup)
            {
                foreach (var item in ((Model3DGroup)m3d).Children)
                {
                    Highlight(item);
                }
            }
            else if (m3d is GeometryModel3D)
            {
                GeometryModel3D g3d = (GeometryModel3D)m3d;
                Binding b = BindingOperations.GetBinding(g3d, GeometryModel3D.MaterialProperty);
                g3d.SetValue(TagProperty, b.Source);
                g3d.SetValue(GeometryModel3D.MaterialProperty, _selectedVisualMaterial);
                g3d.SetValue(GeometryModel3D.BackMaterialProperty, _selectedVisualMaterial);
            }
        }




        #endregion

      


        public double PercentageLoaded
        {
            get { return (double)GetValue(PercentageLoadedProperty); }
            set { SetValue(PercentageLoadedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PercentageLoaded.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PercentageLoadedProperty =
            DependencyProperty.Register("PercentageLoaded", typeof(double), typeof(DrawingControl3D),
                                        new UIPropertyMetadata(0.0));
        private XbimMeshGeometry3D hitMesh;



        private void ClearGraphics()
        {
            PercentageLoaded = 0;
           
            _hitResult = null;
            _currentProduct = null;
            Opaques.Children.Clear();
            Transparents.Children.Clear();
            scene = new XbimScene<WpfMeshGeometry3D, WpfMaterial>();
            Viewport.ResetCamera();
            PropertiesBillBoard.IsRendering = false;
            this.Viewport.Children.Remove(PropertiesBillBoard);

        }

  

        private Rect3D GetModelBounds(XbimModel model)
        {
            Rect3D box = new Rect3D();
            if (model == null) return box;
            bool first = true;
            foreach (XbimGeometryData shape in model.GetGeometryData(XbimGeometryType.BoundingBox))
            {
                Matrix3D matrix3d = new Matrix3D();
                matrix3d = matrix3d.FromArray(shape.TransformData);
                Rect3D bb = new Rect3D();
                bb = bb.FromArray(shape.ShapeData);
                bb = bb.TransformBy(matrix3d);
                if (first) { box = bb; first = false; }
                else box.Union(bb);
            }
            return box;
        }

        private void LoadGeometry(XbimModel model)
        {

            //reset all the visuals
            ClearGraphics();
            if (model == null) return; //nothing to do
            double metre =  model.GetModelFactors.OneMetre;

            //get bounding box for the whole building
            _boundingBox = GetModelBounds(model);

            double metresWide = _boundingBox.SizeY;
            double metresLong = _boundingBox.SizeX;

            Point3D p3d = _boundingBox.Centroid();
            TranslateTransform3D t3d = new TranslateTransform3D(p3d.X, p3d.Y, _boundingBox.Z);

            long gridWidth = Convert.ToInt64(metresWide / (metre * 10));
            long gridLen = Convert.ToInt64(metresLong / (metre * 10));
            if(gridWidth>10 || gridLen>10) 
                this.GridLines.MinorDistance = metre * 10;
            else
                this.GridLines.MinorDistance = metre;
            this.GridLines.Width = (gridWidth + 1) * 10 * metre;
            this.GridLines.Length = (gridLen + 1) * 10 * metre;
           
            this.GridLines.MajorDistance = metre * 10;
            this.GridLines.Thickness = 0.01 * metre;
            this.GridLines.Transform = t3d;

            ViewHome();
            Viewport.DefaultCamera.NearPlaneDistance = 0.125 * metre;
            Viewport.Camera.NearPlaneDistance = 0.125 * metre;

            XbimGeometryHandleCollection handles = new XbimGeometryHandleCollection(model.GetGeometryHandles()
                                                       .Exclude(IfcEntityNameEnum.IFCFEATUREELEMENT));
            double total = handles.Count;
            double processed = 0;
            foreach (var layerContent in handles.FilterByBuildingElementTypes())
            {
                string elementTypeName = layerContent.Key;
                XbimGeometryHandleCollection layerHandles = layerContent.Value;
                IEnumerable<XbimGeometryData> geomColl = model.GetGeometryData(layerHandles);
                XbimColour colour = scene.LayerColourMap[elementTypeName];
                XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> layer = new XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>(colour);
                //add all content initially into the hidden field
                foreach (var geomData in geomColl)
                {
                    layer.AddToHidden(geomData, model);
                    processed++;
                    int progress = Convert.ToInt32(100.0 * processed / total);
                }
                scene.Add(layer);
            }

            DrawShapes();
            


            //_worker = new BackgroundWorker();
            //_worker.DoWork += new DoWorkEventHandler(GenerateGeometry);

            //_worker.WorkerReportsProgress = true;
            //_worker.WorkerSupportsCancellation = false;
            //_worker.ProgressChanged += new ProgressChangedEventHandler(GenerateGeometry_ProgressChanged);
            //_worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(GenerateGeometry_RunWorkerCompleted);
            //_worker.RunWorkerAsync(model);


        }


        private void GenerateGeometry_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            _worker = null;

            RoutedEventArgs ev = new RoutedEventArgs(LoadedEvent);
            RaiseEvent(ev);
        }

        private void GenerateGeometry_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //XbimSurfaceStyle shapesToDraw = e.UserState as XbimSurfaceStyle;
            //if (shapesToDraw != null)
            //{
            //    DrawShapes(shapesToDraw);
            //}
            //ProgressChangedEventHandler handler = _progressChanged;
            //if (handler != null)
            //{
            //    handler(this, e);
            //}

        }

        private void DrawShapes()
        {

            foreach (var layer in scene.Layers.Where(l => l.HasContent))
            {
                //move it to the visual element
                layer.ShowAll();
                GeometryModel3D m3d = (WpfMeshGeometry3D)layer.Visible;
                m3d.SetValue(TagProperty, layer.Name);         
                //sort out materials and bind
                if (layer.Material.RenderBothFaces)
                    m3d.BackMaterial = m3d.Material = (WpfMaterial)layer.Material;
                else if(layer.Material.SwitchFrontAndRearFaces)
                    m3d.BackMaterial = (WpfMaterial)layer.Material;
                else
                    m3d.Material = (WpfMaterial)layer.Material;
                ModelVisual3D mv = new ModelVisual3D();
                mv.Content = m3d;
                if (layer.Material.IsTransparent)
                    Transparents.Children.Add(mv);
                else
                    Opaques.Children.Add(mv);
            }
        }

        private void GenerateGeometry(object s, DoWorkEventArgs args)
        {
            //BackgroundWorker worker = s as BackgroundWorker;
            //XbimModel model = args.Argument as XbimModel;

            //if (worker != null && model != null)
            //{
            //    worker.ReportProgress(0, "Reading Geometry");

            //    XbimGeometryHandleCollection handles = new XbimGeometryHandleCollection(model.GetGeometryHandles()
            //                                            .Exclude(IfcEntityNameEnum.IFCFEATUREELEMENT));
            //    double total = handles.Count;
            //    double processed = 0;
            //    foreach (var ss in handles.GetSurfaceStyles())
            //    {
            //        ss.GeometryData = model.GetGeometryData(handles.GetGeometryHandles(ss)).ToList();
            //        processed += ss.GeometryData.Count;
            //        int progress = Convert.ToInt32(100.0 * processed / total);
            //        worker.ReportProgress(progress, ss);
            //        Thread.Sleep(100);
            //    }
            //}
            //worker.ReportProgress(-1, "Complete");
            //args.Result = model;
        }




        #region Query methods


        private void ContainerElementMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var element = sender as ModelUIElement3D;
                var model = element.Model as GeometryModel3D;
                model.Material = model.Material == Materials.Green ? Materials.Gray : Materials.Green;
                e.Handled = true;
            }
        }


        #endregion

        /// <summary>
        ///   Hides all instances of the specified type
        /// </summary>
        public void Hide<T>()
        {
            scene.HideAll();
        //   Type typeToHide = typeof(T);
        //    foreach (var v3d in Opaques.Children.OfType<ModelVisual3D>().Concat(Transparents.Children.OfType<ModelVisual3D>()))
        //    {
        //        GeometryModel3D g3d = v3d.Content as GeometryModel3D;
        //        if (g3d != null)
        //        {
        //            XbimMeshLayer layer = g3d.GetValue(TagProperty) as XbimMeshLayer;
        //            if (layer != null && layer.Rendered.Contains<T>())
        //            {
        //                MeshGeometry3D m3d = g3d.Geometry as MeshGeometry3D;
        //                if (m3d != null)
        //                {
        //                    XbimMeshFragmentCollection toCopy = level.Excluding<T>();
        //                    if (toCopy.Count == 0) //just clear whole mesh
        //                    {
        //                        g3d.Geometry = null;
        //                    }
        //                    else if (toCopy.Count != fragments.Count) //WE HAVE SOMETHING TO REMOVE
        //                    {
        //                        List<XbimMeshGeometry3D> toSave;
        //                        XbimMeshGeometry3D copy = m3d.Copy(toCopy, out toSave);
        //                        MeshGeometry3D meshgeom = new MeshGeometry3D();
        //                        meshgeom.Positions = new Point3DCollection(copy.Positions);
        //                        meshgeom.Normals = new Vector3DCollection(copy.Normals);
        //                        meshgeom.TriangleIndices = new Int32Collection(copy.TriangleIndices);
        //                        meshgeom.Freeze();
        //                        g3d.Geometry = meshgeom;
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    //foreach (var vis in  _items.Values)
        //    //{
        //    //    object h = vis.GetValue(TagProperty);
        //    //    if (h is XbimInstanceHandle)
        //    //    {

        //    //        if (typeToHide.IsAssignableFrom(((XbimInstanceHandle)h).EntityType))
        //    //        {
        //    //            ModelVisual3D parent = VisualTreeHelper.GetParent(vis) as ModelVisual3D;
        //    //            if (parent != null)
        //    //            {
        //    //                _hidden.Add(vis, parent);
        //    //                parent.Children.Remove(vis);
        //    //            }
        //    //        }
        //    //    }
        //    //}
        //    //_hiddenTypes.Add(typeToHide);
        }

        public void Hide(int hideProduct)
        {
            //ModelVisual3D item;
            //if (_items.TryGetValue(hideProduct, out item))
            //{
            //    ModelVisual3D parent = VisualTreeHelper.GetParent(item) as ModelVisual3D;
            //    if (parent != null)
            //    {
            //        _hidden.Add(item, parent);
            //        parent.Children.Remove(item);
            //    }
            //    return;
            //}
        }

        private void Show<T>()
        {
            scene.ShowAll();
            //List<Visual3D> alive = new List<Visual3D>();
            //Type typeToShow = typeof(T);
            //foreach (var item in _hidden)
            //{
            //    object h = item.Key.GetValue(TagProperty);

            //    if (h is XbimInstanceHandle && typeToShow.IsAssignableFrom(((XbimInstanceHandle)h).EntityType))
            //    {

            //        item.Value.Children.Add(item.Key);
            //        alive.Add(item.Key);
            //    }
            //}
            //foreach (var bornAgain in alive)
            //{
            //    _hidden.Remove(bornAgain);
            //}
            //_hiddenTypes.Remove(typeToShow);
        }

        public void ShowAll()
        {
            scene.ShowAll();
        }

        public void HideAll()
        {
            scene.HideAll();
        }


        public void ViewHome()
        {
            Point3D c = _boundingBox.Centroid();
            Viewport.Camera = Viewport.DefaultCamera;
            CameraHelper.LookAt(Viewport.Camera, c, new Vector3D(-100, 100, -30), new Vector3D(0, 0, 1), 0);
            Viewport.ZoomExtents(_boundingBox);
            double biggest = Math.Max(Math.Max(_boundingBox.SizeX, _boundingBox.SizeY), _boundingBox.SizeZ);
            // Viewport.Camera.FarPlaneDistance = biggest * 100;

        }


        public void ZoomSelected()
        {
            //ModelVisual3D selVis;
            //if (SelectedItem.HasValue && _items.TryGetValue(SelectedItem.Value, out selVis))
            //{
            //    Rect3D bounds = VisualTreeHelper.GetDescendantBounds(selVis);
            //    if (!bounds.IsEmpty)
            //    {
            //        bounds = bounds.Inflate(bounds.SizeX / 2, bounds.SizeY / 2, bounds.SizeZ / 2);
            //        Viewport.ZoomExtents(bounds);
            //    }
            //}
        }


    }
}