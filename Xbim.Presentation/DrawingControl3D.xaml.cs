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


    public delegate XbimMaterialProvider SetMaterialEventHandler(IfcProduct product);

    public delegate Func<IfcProduct, bool> SetFilterEventHandler(int pass);

    /// <summary>
    ///   Interaction logic for DrawingControl3D.xaml
    /// </summary>
    public partial class DrawingControl3D : UserControl
    {
        public DrawingControl3D()
        {
            InitializeComponent();
            
            _defaultMaterial = new XbimMaterialProvider(new DiffuseMaterial(Brushes.LightGray));
            _selectedVisualMaterial = new DiffuseMaterial(Brushes.LightGreen);
            SolidColorBrush transparentBrush = new SolidColorBrush(Colors.LightBlue);
            transparentBrush.Opacity = 0.5;
            MaterialGroup window = new MaterialGroup();
            window.Children.Add(new DiffuseMaterial(transparentBrush));
            window.Children.Add(new SpecularMaterial(transparentBrush, 40));
            _defaultTransparentMaterial = new XbimMaterialProvider(window);
           
            Viewport = Canvas;
            Canvas.MouseDown+=Canvas_MouseDown;
           
        }


        #region Statics

        private static Dictionary<Type, int> _zOrders;

        static DrawingControl3D()
        {
            _zOrders = new Dictionary<Type, int>();
            _zOrders.Add(typeof (IfcSpace), 50);
            _zOrders.Add(typeof (IfcWall), 200);
            _zOrders.Add(typeof (IfcWallStandardCase), 200);
            _zOrders.Add(typeof (IfcWindow), 180);
            _zOrders.Add(typeof (IfcDoor), 170);
            _zOrders.Add(typeof (IfcSlab), 100);
        }

        /// <summary>
        ///   Returns the Z order for the specified product type
        /// </summary>
        /// <param name = "product"></param>
        /// <returns></returns>
        public static int ZOrder(IfcProduct product, bool external)
        {
            int z;
            if (_zOrders.TryGetValue(product.GetType(), out z))
                return external ? z + 1000 : z;
            else
                return 10;
        }

        #endregion

        #region Fields

        private BackgroundWorker _worker;
        Dictionary<int, MeshGeometry3D> _meshMap = new Dictionary<int, MeshGeometry3D>();
        private Dictionary<int, ModelVisual3D> _items = new Dictionary<int, ModelVisual3D>();
        private Dictionary<ModelVisual3D, ModelVisual3D> _hidden = new Dictionary<ModelVisual3D, ModelVisual3D>();
       
        private List<Type> _hiddenTypes = new List<Type>();
       
        private XbimMaterialProvider _defaultMaterial;
        private XbimMaterialProvider _defaultTransparentMaterial;


        private int? _currentProduct;
        protected HelixToolkit.Wpf.Viewport3DHelper.HitResult _hitResult;
        protected Material _selectedVisualMaterial;
        protected Material _selectedVisualPreviousMaterial;
        private Rect3D _boundingBox;
        private Transform3DGroup modelTransform = new Transform3DGroup();
   
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
                                             typeof (SelectionChangedEventHandler), typeof (DrawingControl3D));

        public event SelectionChangedEventHandler SelectionChanged
        {
            add { AddHandler(SelectionChangedEvent, value); }
            remove { RemoveHandler(SelectionChangedEvent, value); }
        }

        public new static readonly RoutedEvent LoadedEvent =
            EventManager.RegisterRoutedEvent("LoadedEvent", RoutingStrategy.Bubble, typeof (RoutedEventHandler),
                                             typeof (DrawingControl3D));

        public new event RoutedEventHandler Loaded
        {
            add { AddHandler(LoadedEvent, value); }
            remove { RemoveHandler(LoadedEvent, value); }
        }

        private void Canvas_MouseDown(object sender, MouseButtonEventArgs e)
        {

            var pos = e.GetPosition(Canvas);
            foreach (var hit in Viewport3DHelper.FindHits(Canvas.Viewport, pos))
            {
                object id = hit.Visual.GetValue(TagProperty);
                if (id is int)
                {
                    IList remove;
                    if (_currentProduct.HasValue)
                    {
                        GeometryModel3D geom = _hitResult.Model as GeometryModel3D;
                        //if (geom != null)
                        //    geom.Material = _selectedVisualPreviousMaterial;
                        remove = new int[] { _currentProduct.Value };
                    }
                    else
                        remove = new int[] { };
                    _hitResult = hit;
                    GeometryModel3D selGeom = _hitResult.Model as GeometryModel3D;
                    if (selGeom != null)
                    {
                        //_selectedVisualPreviousMaterial = selGeom.Material;
                        //selGeom.Material = _selectedVisualMaterial;
                    } 
                    _currentProduct=(int)id; 
                    SelectedItem =    _currentProduct.Value   ;           
                    return;
                }
            }
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


        public HelixToolkit.Wpf.HelixViewport3D Viewport
        {
            get { return (HelixToolkit.Wpf.HelixViewport3D)GetValue(ViewportProperty); }
            set { SetValue(ViewportProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Viewport.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ViewportProperty =
            DependencyProperty.Register("Viewport", typeof(HelixToolkit.Wpf.HelixViewport3D), typeof(DrawingControl3D),new PropertyMetadata(null));

        

        public int? SelectedItem
        {
            get { return (int?) GetValue(SelectedItemProperty); }
            set
            { 
                SetValue(SelectedItemProperty, value);
            }
        }

        // Using a DependencyProperty as the backing store for SelectedItem.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register("SelectedItem", typeof (int?), typeof (DrawingControl3D),
                                        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits,
                                                                      new PropertyChangedCallback(OnSelectedItemChanged)));

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
                    int newVal = (int) e.NewValue;
                    d3d.Select(newVal);
                }
      

            }
            //PropertiesBillBoard.IsRendering = false;
            //this.Canvas.Children.Remove(PropertiesBillBoard);

            //var pos = e.GetPosition(Canvas);
            //foreach (var hit in Viewport3DHelper.FindHits(Canvas.Viewport, pos))
            //{
            //    object id = hit.Visual.GetValue(TagProperty);
            //    if (id is int)
            //    {
            //        IList remove;
            //        if (_currentProduct.HasValue)
            //        {
            //            GeometryModel3D geom = _selectedVisual.Model as GeometryModel3D;
            //            if (geom != null)
            //                geom.Material = _selectedVisualPreviousMaterial;
            //            remove = new int[] { _currentProduct.Value };
            //        }
            //        else
            //            remove = new int[] { };
            //        _selectedVisual = hit;
            //        GeometryModel3D selGeom = _selectedVisual.Model as GeometryModel3D;
            //        if (selGeom != null)
            //        {
            //            _selectedVisualPreviousMaterial = selGeom.Material;
            //            selGeom.Material = _selectedVisualMaterial;
            //        }
            //        this.Canvas.Children.Add(PropertiesBillBoard);
            //        PropertiesBillBoard.IsRendering = true;
            //        _currentProduct = (int)id;

            //        PropertiesBillBoard.Text = Model.Instances[_currentProduct.Value].SummaryString().EnumerateToString(null, "\n");
            //        PropertiesBillBoard.Position = hit.RayHit.PointHit;


            //        IList add = new int[] { _currentProduct.Value };
            //        SelectionChangedEventArgs selEv = new SelectionChangedEventArgs(SelectionChangedEvent, remove, add);
            //        RaiseEvent(selEv);
            //        return;
            //    }
            }

        private void Deselect(int oldVal)
        {
            ModelVisual3D mv3d;
            if (_items.TryGetValue(oldVal, out mv3d) && mv3d.Content != null)
            {

                UnHighlight(mv3d.Content);
            }
        }

        private void Select(int newVal)
        {
            ModelVisual3D mv3d;
            if (_items.TryGetValue(newVal, out mv3d) && mv3d.Content != null)
            {
                Highlight(mv3d.Content);
            }
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
                g3d.SetValue(GeometryModel3D.BackMaterialProperty, g3d.GetValue(TagProperty));
                g3d.SetValue(GeometryModel3D.MaterialProperty, g3d.GetValue(TagProperty));
                m3d.ClearValue(TagProperty);
            }
        }

        private void Highlight(Model3D m3d)
        {
            if (m3d is Model3DGroup)
            {
                foreach (var item in ((Model3DGroup) m3d).Children)
                {
                    Highlight(item);
                }
            }
            else if (m3d is GeometryModel3D)
            {
                GeometryModel3D g3d = (GeometryModel3D) m3d;
                m3d.SetValue(TagProperty, g3d.Material);
                g3d.SetValue(GeometryModel3D.MaterialProperty, _selectedVisualMaterial);
                g3d.SetValue(GeometryModel3D.BackMaterialProperty, _selectedVisualMaterial); 
            }
        }


        //public double Transparency
        //{
        //    get { return (double) GetValue(TransparencyProperty); }
        //    set { SetValue(TransparencyProperty, value); }
        //}

        //// Using a DependencyProperty as the backing store for Transparency.  This enables animation, styling, binding, etc...
        //public static readonly DependencyProperty TransparencyProperty =
        //    DependencyProperty.Register("Transparency", typeof (double), typeof (DrawingControl3D),
        //                                new UIPropertyMetadata(1.0, new PropertyChangedCallback(OnTransparencyChanged)));


        /// <summary>
        ///   Typical length of a Step when moving in the model, default = 0.7 metre
        /// </summary>
        public double Stride
        {
            get { return (double) GetValue(StrideProperty); }
            set
            {
                SetValue(StrideProperty, value);
                //TrackBall.StepFactor = Stride;
            }
        }

        // Using a DependencyProperty as the backing store for Stride.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty StrideProperty =
            DependencyProperty.Register("Stride", typeof (double), typeof (DrawingControl3D),
                                        new UIPropertyMetadata(0.7));


    

        #endregion

        public IDictionary<int, ModelVisual3D> Items
        {
            get { return _items; }
        }

       
        public double PercentageLoaded
        {
            get { return (double) GetValue(PercentageLoadedProperty); }
            set { SetValue(PercentageLoadedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for PercentageLoaded.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty PercentageLoadedProperty =
            DependencyProperty.Register("PercentageLoaded", typeof (double), typeof (DrawingControl3D),
                                        new UIPropertyMetadata(0.0));
        


        private void ClearGraphics()
        {
            PercentageLoaded = 0;
            _hidden.Clear();
            _hiddenTypes.Clear();
            _items.Clear();
            _meshMap.Clear();
            _hitResult = null;
            _currentProduct = null;
            BuildingModel.Children.Clear();
            Viewport.ResetCamera();
            
        }

        private MeshGeometry3D MakeBoundingBox(Rect3D r3D)
        {
            MeshGeometry3D mesh = new MeshGeometry3D();
            Point3D p0 = r3D.Location;
            Point3D p1 = p0;
            p1.X += r3D.SizeX;
            Point3D p2 = p1;
            p2.Z += r3D.SizeZ;
            Point3D p3 = p2;
            p3.X -= r3D.SizeX;
            Point3D p4 = p3;
            p4.Y += r3D.SizeY;
            Point3D p5 = p4;
            p5.Z -= r3D.SizeZ;
            Point3D p6 = p5;
            p6.X += r3D.SizeX;
            Point3D p7 = p6;
            p7.Z += r3D.SizeZ;

            List<Point3D> points = new List<Point3D>();
            points.Add(p0);
            points.Add(p1);
            points.Add(p2);
            points.Add(p3);
            points.Add(p4);
            points.Add(p5);
            points.Add(p6);
            points.Add(p7);

            AddVertex(3, mesh, points);
            AddVertex(0, mesh, points);
            AddVertex(2, mesh, points);

            AddVertex(0, mesh, points);
            AddVertex(1, mesh, points);
            AddVertex(2, mesh, points);

            AddVertex(4, mesh, points);
            AddVertex(5, mesh, points);
            AddVertex(3, mesh, points);

            AddVertex(5, mesh, points);
            AddVertex(0, mesh, points);
            AddVertex(3, mesh, points);

            AddVertex(7, mesh, points);
            AddVertex(6, mesh, points);
            AddVertex(4, mesh, points);

            AddVertex(6, mesh, points);
            AddVertex(5, mesh, points);
            AddVertex(4, mesh, points);

            AddVertex(2, mesh, points);
            AddVertex(1, mesh, points);
            AddVertex(7, mesh, points);

            AddVertex(1, mesh, points);
            AddVertex(6, mesh, points);
            AddVertex(7, mesh, points);

            AddVertex(4, mesh, points);
            AddVertex(3, mesh, points);
            AddVertex(7, mesh, points);

            AddVertex(3, mesh, points);
            AddVertex(2, mesh, points);
            AddVertex(7, mesh, points);

            AddVertex(6, mesh, points);
            AddVertex(1, mesh, points);
            AddVertex(5, mesh, points);

            AddVertex(1, mesh, points);
            AddVertex(0, mesh, points);
            AddVertex(5, mesh, points);

            return mesh;
        }

        private void AddVertex(int index, MeshGeometry3D mesh, List<Point3D> points)
        {
            mesh.TriangleIndices.Add(mesh.Positions.Count);
            mesh.Positions.Add(points[index]);
        }

        private Rect3D GetModelBounds(XbimModel model)
        {
            Rect3D box = new Rect3D();
            if (model == null) return box;
            foreach (XbimGeometryData shape in model.GetGeometryData(XbimGeometryType.BoundingBox))
            {
                Matrix3D matrix3d = new Matrix3D();
                matrix3d = matrix3d.FromArray(shape.TransformData);
                Rect3D bb = new Rect3D(); 
                bb = bb.FromArray(shape.ShapeData);
                bb = bb.TransformBy(matrix3d);
                box.Union(bb);
            }
            return box;
        }

        private void LoadGeometry(XbimModel model)
        {
            
            //reset all the visuals
            ClearGraphics();
            if (model == null) return; //nothing to do
            double metre = 1/model.GetModelFactors.OneMetre;

            //get bounding box for the whole building
            _boundingBox = GetModelBounds(model);
            
            double biggest = Math.Max(Math.Max(_boundingBox.SizeX, _boundingBox.SizeY),_boundingBox.SizeZ);

            double factor = 100 / biggest;
            double metresWide = _boundingBox.SizeY ;
            double metresLong = _boundingBox.SizeX ;
            modelTransform = new Transform3DGroup();
            ScaleTransform3D s3d = new ScaleTransform3D(factor, factor, factor);
            RotateTransform3D r3d = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0,0,1),90));
            Point3D p3d = _boundingBox.Centroid();
            TranslateTransform3D t3d = new TranslateTransform3D(-p3d.X,-p3d.Y,-_boundingBox.Z);
            modelTransform.Children.Add(t3d);
            modelTransform.Children.Add(s3d);
            modelTransform.Children.Add(r3d);         
            _boundingBox = modelTransform.TransformBounds(_boundingBox);
            long gridWidth =  Convert.ToInt64(metresWide / (metre * 10))+1;
            long gridLen = Convert.ToInt64(metresLong / (metre * 10)) + 1;
            this.GridLines.Width = gridWidth*10*metre;
            this.GridLines.Length = gridLen * 10 * metre;
            this.GridLines.MinorDistance = metre ;
            this.GridLines.MajorDistance = metre * 10;
            this.GridLines.Thickness = 0.1 / factor;
            this.GridLines.Transform = s3d;
            Viewport.ZoomExtents(_boundingBox);
            BuildingModel.Transform = modelTransform;
            _worker = new BackgroundWorker();
            _worker.DoWork += new DoWorkEventHandler(GenerateGeometry);

            _worker.WorkerReportsProgress = true;
            _worker.WorkerSupportsCancellation = false;
            _worker.ProgressChanged += new ProgressChangedEventHandler(GenerateGeometry_ProgressChanged);
            _worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(GenerateGeometry_RunWorkerCompleted);
            _worker.RunWorkerAsync(model);

           
        }


        private void GenerateGeometry_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _worker = null;
            
            RoutedEventArgs ev = new RoutedEventArgs(LoadedEvent);
            RaiseEvent(ev);
        }

        private void GenerateGeometry_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            XbimSurfaceStyle shapesToDraw = e.UserState as XbimSurfaceStyle;
            if (shapesToDraw != null)
            {
                DrawShapes(shapesToDraw);
            }
            ProgressChangedEventHandler handler = _progressChanged;
            if (handler != null)
            {
                handler(this, e);
            }

        }

        private void DrawShapes(XbimSurfaceStyle StyleToDraw)
        {
            //create a variable to hold visuals and avoid too many redraws
            ModelVisual3D visualsToAdd = new ModelVisual3D();
            XbimMaterialProvider mat = Model.GetRenderMaterial(StyleToDraw);
            visualsToAdd.SetValue(TagProperty, mat);
            foreach (var prodGeom in StyleToDraw.ProductGeometries)
            {
                //Try and get the visual for the product, if not found create one
                ModelVisual3D mv;
                bool newVisual = false;
                
                if (!_items.TryGetValue(prodGeom.ProductLabel, out mv))
                {
                    mv = new ModelVisual3D();
                    newVisual = true;
                }
               
                //Set up the Model Geometry to hold the product geometry, this has unique material and tranform
                //and may reuse GeometryModel3D meshes from previous renders
                GeometryModel3D m3d = new GeometryModel3D();
               
                BindingOperations.SetBinding(m3d, GeometryModel3D.BackMaterialProperty,mat.BackgroundMaterialBinding);  
                BindingOperations.SetBinding(m3d, GeometryModel3D.MaterialProperty, mat.FaceMaterialBinding);
               
                bool first = true;
                foreach (var geom in prodGeom.Geometry) //create a new one don't add it to the scene yet as we may have no valid content
                {
                    if (first)
                    {
                        Matrix3D matrix3d = new Matrix3D().FromArray(geom.TransformData);
                        m3d.Transform = new MatrixTransform3D(matrix3d);
                        first = false;
                    }
                    int geomHash = geom.GeometryHash; //check if it is already loaded
                    MeshGeometry3D mesh;
                    if (!_meshMap.TryGetValue(geomHash, out mesh)) //if not loaded, load it and merge with any other meshes in play
                    {
                        if (geom.GeometryType == XbimGeometryType.TriangulatedMesh)
                        {
                            XbimTriangulatedModelStream tMod = new XbimTriangulatedModelStream(geom.ShapeData);
                            mesh = tMod.AsMeshGeometry3D();
                            _meshMap.Add(geomHash, mesh);
                        }
                        else if (geom.GeometryType == XbimGeometryType.BoundingBox)
                        {
                            Rect3D r3d = new Rect3D().FromArray(geom.ShapeData);
                            mesh = MakeBoundingBox(r3d);
                            _meshMap.Add(geomHash, mesh);
                        }
                        else
                            throw new XbimException("Illegal geometry type found");
                        if (m3d.Geometry == null)
                            m3d.Geometry = mesh;
                        else
                            m3d.Geometry = m3d.Geometry.Append(mesh);
                    }
                    else //add a new GeometryModel3d to the visual as we want to reference an existing mesh
                    {
                        GeometryModel3D m3dRef = new GeometryModel3D();
                        BindingOperations.SetBinding(m3dRef, GeometryModel3D.BackMaterialProperty, mat.BackgroundMaterialBinding);
                        BindingOperations.SetBinding(m3dRef, GeometryModel3D.MaterialProperty, mat.FaceMaterialBinding);
                        m3dRef.Geometry = mesh;
                        m3dRef.Transform = m3d.Transform; //reuse the same transform
                        mv.AddGeometry(m3dRef);
                    }
                }

                if (m3d.Geometry != null) //we have added some unique content to this object
                {
                    mv.AddGeometry(m3d);
                }
                    if (newVisual) //we have some new visual representation to add, don't add model visual otherwise
                    {
                        mv.SetValue(TagProperty, prodGeom.ProductLabel);
                        _items.Add(prodGeom.ProductLabel, mv);
                        visualsToAdd.Children.Add(mv);
                    }
            }
            if (visualsToAdd.Children.Count > 0)
            {
                BuildingModel.Children.Add(visualsToAdd);
            }
        }


        private void GenerateGeometry(object s, DoWorkEventArgs args)
        {
            BackgroundWorker worker = s as BackgroundWorker;
            XbimModel model = args.Argument as XbimModel;
           
            if (worker != null && model != null)
            {
                worker.ReportProgress(0, "Reading Geometry");

                XbimGeometryHandleCollection handles = new XbimGeometryHandleCollection(model.GetGeometryHandles()
                                                        .Exclude(IfcEntityNameEnum.IFCSPACE, IfcEntityNameEnum.IFCFEATUREELEMENT));
                double total = handles.Count;
                double processed = 0;
                foreach (var ss in handles.GetSurfaceStyles())
                {
                    ss.GeometryData = model.GetGeometryData(handles.GetGeometryHandles(ss)).ToList();
                    processed += ss.GeometryData.Count;
                    int progress = Convert.ToInt32(100.0 * processed / total);
                    worker.ReportProgress(progress, ss);
                    Thread.Sleep(100);
                } 
            }
            worker.ReportProgress(-1, "Complete");
            args.Result = model;
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
        public void HideAllTypesOf(int product)
        {
            Type typeToHide = Model.Instances[Math.Abs(product)].GetType();
            foreach (var placement in _items)
            {
                int prod = placement.Key;
                Type type = Model.Instances[Math.Abs(prod)].GetType();
                if (type == typeToHide)
                {
                    ModelVisual3D parent = VisualTreeHelper.GetParent(placement.Value) as ModelVisual3D;
                    if (parent != null)
                    {
                        _hidden.Add(placement.Value, parent);
                        parent.Children.Remove(placement.Value);
                    }
                }
            }
            _hiddenTypes.Add(typeToHide);
        }

        public void Hide(int hideProduct)
        {
            ModelVisual3D item;
            if (_items.TryGetValue(hideProduct, out item))
            {
                ModelVisual3D parent = VisualTreeHelper.GetParent(item) as ModelVisual3D;
                if (parent != null)
                {
                    _hidden.Add(item, parent);
                    parent.Children.Remove(item);
                }
                return;
            }
        }

        public void Show(Type type)
        {
            List<ModelVisual3D> alive = new List<ModelVisual3D>();
            foreach (var item in _hidden)
            {
                IfcProduct prod = item.Key.GetValue(TagProperty) as IfcProduct;
                if (prod != null && prod.GetType() == type)
                {
                    item.Value.Children.Add(item.Key);
                    alive.Add(item.Key);
                }
            }
            foreach (var bornAgain in alive)
            {
                _hidden.Remove(bornAgain);
            }
            _hiddenTypes.Remove(type);
        }

        public void ShowAll()
        {
            foreach (var item in _hidden)
            {
                item.Value.Children.Add(item.Key);
            }
            _hiddenTypes.Clear();
            _hidden.Clear();
        }


        public void ZoomExtents(int? selection)
        {
            if (!selection.HasValue)
                Canvas.ZoomExtents(_boundingBox);//);
            else
            {
                ModelVisual3D mv = _items[selection.Value];
            }
        }
        
    }
}
