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
using System.Collections.Specialized;
using System.Threading.Tasks;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.ExternalReferenceResource;
using System.Text;

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
            Viewport = Canvas;
            Canvas.MouseDown += Canvas_MouseDown;
            this.Loaded += DrawingControl3D_Loaded;
            federationColours = new XbimColourMap(StandardColourMaps.Federation);
            Viewport.CameraChanged += Viewport_CameraChanged;
        }

        void Viewport_CameraChanged(object sender, RoutedEventArgs e)
        {
            // Debug.WriteLine("Cam changed " + DateTime.Now);
            
            HelixViewport3D snd = sender as HelixViewport3D;
            if (viewBounds.Length() > 0 && snd != null)
            {
                var middlePoint = viewBounds.Centroid();
                double CentralDistance = Math.Sqrt(
                    Math.Pow(snd.Camera.Position.X, 2) + Math.Pow(middlePoint.X, 2) +
                    Math.Pow(snd.Camera.Position.Y, 2) + Math.Pow(middlePoint.Y, 2) +
                    Math.Pow(snd.Camera.Position.Z, 2) + Math.Pow(middlePoint.Z, 2)
                    );

                double FarPlane = CentralDistance + viewBounds.Length();
                double NearPlane = CentralDistance - viewBounds.Length();

                // if (NearPlane <= FarPlane / 7000)
                //     NearPlane = FarPlane/7000;
                if (NearPlane < 0.125)
                {
                    NearPlane = 0.125;
                }
                if (Viewport.Camera.NearPlaneDistance != NearPlane)
                {
                    Viewport.Camera.NearPlaneDistance = NearPlane;
                    // Debug.WriteLine("Near: " + NearPlane);
                }
                if (Viewport.Camera.FarPlaneDistance != FarPlane)
                {
                    Viewport.Camera.FarPlaneDistance = FarPlane;
                    // Debug.WriteLine("Far: " + FarPlane);
                }
            }
        }

        void DrawingControl3D_Loaded(object sender, RoutedEventArgs e)
        {
            ShowSpaces = false; 
        }

        #region Fields
        public List<XbimScene<WpfMeshGeometry3D, WpfMaterial>> scenes = new List<XbimScene<WpfMeshGeometry3D, WpfMaterial>>();
        private XbimColourMap federationColours;

        protected RayMeshGeometry3DHitTestResult _hitResult;
       
        private XbimRect3D modelBounds;
        private XbimRect3D viewBounds;
        private int? _currentProduct;
        private List<Material> _materials = new List<Material>();
        private Dictionary<Material, double> _opacities = new Dictionary<Material, double>();
        /// <summary>
        /// Gets or sets the model.
        /// </summary>
        /// <value>The model.</value>
        public Model3D Model3d { get; set; }

        public void SetCutPlane(double PosX, double PosY, double PosZ, double NrmX, double NrmY, double NrmZ)
        {   
            object p = this.FindName("cuttingGroup");
            XbimCuttingPlaneGroup cpg = p as XbimCuttingPlaneGroup;
            if (cpg != null)
            {
                cpg.CuttingPlanes.Clear();
                cpg.CuttingPlanes.Add(
                    new Plane3D(
                        new Point3D(PosX, PosY, PosZ),
                        new Vector3D(NrmX, NrmY, NrmZ)
                        ));
                cpg.IsEnabled = true;
            }
        }

        public void ClearCutPlane()
        {
            object p = this.FindName("cuttingGroup");
            XbimCuttingPlaneGroup cpg = p as XbimCuttingPlaneGroup;
            if (cpg != null)
            {
                cpg.IsEnabled = false;
            }
        }

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
                    var frag = layer.Visible.Meshes.Find(hit.VertexIndex1);
                    if (!frag.IsEmpty)
                    {
                        // the highlighting of the selected component is triggered by the change of SelectedEntity (see OnSelectedEntityChanged)
                        int id = frag.EntityLabel;
                        _hitResult = hit;
                        _currentProduct = (int)id;
                        SelectedEntity = layer.Model.Instances[_currentProduct.Value];
                        return;
                    }
                }
            }
            
            Highlighted.Mesh = null;
            _currentProduct = null;
            SelectedEntity = null;
        }

        #endregion

        #region Dependency Properties

        public double ModelOpacity
        {
            get { return (double)GetValue(ModelOpacityProperty); }
            set { SetValue(ModelOpacityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ModelOpacity.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ModelOpacityProperty =
            DependencyProperty.Register("ModelOpacity", typeof(double), typeof(DrawingControl3D), new UIPropertyMetadata(1.0, OnModelOpacityChanged));


        private static void OnModelOpacityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
             DrawingControl3D d3d = d as DrawingControl3D;
             if (d3d != null && e.NewValue !=null)
             {
                 d3d.SetOpacity((double)e.NewValue);
             }
        }

        private void SetOpacity( double opacityPercent)
        {
            double opacity = Math.Min(1, opacityPercent);
            opacity = Math.Max(0, opacity); //bound opacity factor
            
            foreach (var material in _materials)
            {
                SetOpacityPercent(material, opacity);
            }
        }

        private void SetOpacityPercent(Material material,  double opacity)
        {
            var g = material as MaterialGroup;
            if (g != null)
            {
                foreach (var item in g.Children)
                {
                    SetOpacityPercent(item, opacity);
                }
                return;
            }

            var dm = material as DiffuseMaterial;
            if (dm != null)
            {
                double oldValue;
                if (!_opacities.TryGetValue(dm, out oldValue))
                {
                    oldValue = dm.Brush.Opacity;
                    _opacities.Add(dm, oldValue);
                }
                dm.Brush.Opacity = oldValue * opacity;
            }
            var sm = material as SpecularMaterial;
            if (sm != null)
            {
                double oldValue;
                if (!_opacities.TryGetValue(sm, out oldValue))
                {
                    oldValue = sm.Brush.Opacity;
                    _opacities.Add(sm, oldValue);
                }
                sm.Brush.Opacity = oldValue * opacity;
            }
        }

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
                d3d.SetValue(LayerSetProperty, d3d.LayerSetRefresh());
            }
        }

        public bool ForceRenderBothSides
        {
            get { return (bool)GetValue(ForceRenderBothSidesProperty); }
            set { SetValue(ForceRenderBothSidesProperty, value); }
        }

        // Using a DependencyProperty as the backing store for ForceRenderBothSides.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty ForceRenderBothSidesProperty =
            DependencyProperty.Register("ForceRenderBothSides", typeof(bool), typeof(DrawingControl3D), new PropertyMetadata(true));

        

        public IPersistIfcEntity SelectedEntity
        {
            get { return (IPersistIfcEntity)GetValue(SelectedEntityProperty); }
            set { SetValue(SelectedEntityProperty, value); }
        }

        // Using a DependencyProperty as the backing store for SelectedEntity.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty SelectedEntityProperty =
            DependencyProperty.Register("SelectedEntity", typeof(IPersistIfcEntity), typeof(DrawingControl3D), new PropertyMetadata(OnSelectedEntityChanged));

        
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
     
        private static void OnSelectedEntityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is DrawingControl3D)
            {
                DrawingControl3D d3d = d as DrawingControl3D;
                IPersistIfcEntity oldVal = e.OldValue as IPersistIfcEntity;
                if(oldVal!=null)
                {
                    d3d.Deselect(oldVal);
                }
                IPersistIfcEntity newVal = e.NewValue as IPersistIfcEntity;
                if (newVal!=null)
                {
                    d3d.Select(newVal);
                }
            }
        }

        private void Deselect(IPersistIfcEntity oldVal)
        { 
            Highlighted.Mesh = null;
        }

        /// <summary>
        /// Executed when a new entity is selected
        /// </summary>
        /// <param name="newVal"></param>
        private void Select(IPersistIfcEntity newVal)
        {
            // todo: bonghi: investigate why this does not cause flickering in uncut models.
            if (cuttingGroup.IsEnabled)
            {
                XbimMeshGeometry3D m = new XbimMeshGeometry3D();
                var geomDataSet = Model.GetGeometryData(newVal.EntityLabel, XbimGeometryType.TriangulatedMesh);
                foreach (var geomData in geomDataSet)
                {
                    geomData.TransformBy(wcsTransform);
                    m.Add(geomData);    
                }
                List<Point3D> ps = new List<Point3D>(m.PositionCount);
                foreach (var item in m.Positions)
                {
                    ps.Add(new Point3D(item.X, item.Y, item.Z));
                }
                // Highlighted is defined in the XAML of drawingcontrol3d
                Highlighted.Mesh = new Mesh3D(ps, m.TriangleIndices);
            }
            else
            {
                foreach (var scene in scenes)
                {
                    IXbimMeshGeometry3D mesh = scene.GetMeshGeometry3D(newVal);
                    WpfMeshGeometry3D wpfGeom = new WpfMeshGeometry3D(mesh);
                    if (mesh.Meshes.Count() > 0)
                    {
                        // Highlighted is defined in the XAML of drawingcontrol3d
                        Highlighted.Mesh = new Mesh3D(wpfGeom.Mesh.Positions, wpfGeom.Mesh.TriangleIndices);
                        return;
                    }
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
        private XbimVector3D _modelTranslation;
        public XbimMatrix3D wcsTransform;
      

        private void ClearGraphics()
        {
            PercentageLoaded = 0;
            _hitResult = null;
            _currentProduct = null;
            Opaques.Children.Clear();
            Transparents.Children.Clear();
            modelBounds = XbimRect3D.Empty;
            viewBounds = new XbimRect3D(0, 0, 0, 10000, 10000, 5000);    
            scenes = new List<XbimScene<WpfMeshGeometry3D, WpfMaterial>>();
            Viewport.ResetCamera();
           // PropertiesBillBoard.IsRendering = false;
            Highlighted.Mesh = null;
        }

        private XbimRect3D GetModelBounds(XbimModel model)
        {
            XbimRect3D box = new XbimRect3D();
            if (model == null) return box;
            bool first = true;
            foreach (XbimGeometryData shape in model.GetGeometryData(XbimGeometryType.BoundingBox))
            {
                XbimMatrix3D matrix3d = shape.Transform;
                XbimRect3D bb = XbimRect3D.FromArray(shape.ShapeData);
                bb = XbimRect3D.TransformBy(bb, matrix3d);  
                if (first) { box = bb; first = false; }
                else box.Union(bb);
            }
            return box;
        }

        /// <summary>
        /// Clears the current graphics and initiates the cascade of events that result in viewing the scene.
        /// </summary>
        /// <param name="EntityLabels">If null loads the whole model, otherwise only elements listed in the enumerable</param>
        public void LoadGeometry(XbimModel model, IEnumerable<int> EntityLabels = null)
        {
            // AddLayerToDrawingControl is the function that actually populates the geometry in the viewer.
            // AddLayerToDrawingControl is triggered by BuildRefModelScene and BuildScene below here when layers get ready.

            //reset all the visuals
            ClearGraphics();
            _materials.Clear();
            _opacities.Clear();
            this.ClearCutPlane();
            if (model == null) 
                return; //nothing to show

            XbimRegion largest = GetLargestRegion(model);
            XbimPoint3D c = new XbimPoint3D(0,0,0);
            XbimRect3D bb = XbimRect3D.Empty;
            if(largest!=null)
                bb = new XbimRect3D(largest.Centre, largest.Centre);
            
            foreach (var refModel in model.RefencedModels)
            {
                XbimRegion r = GetLargestRegion(refModel.Model);
                if (r != null)
                {
                    if(bb.IsEmpty)
                        bb = new XbimRect3D(r.Centre, r.Centre);
                    else
                        bb.Union(r.Centre);
                }
            }
            XbimPoint3D p = bb.Centroid();
            _modelTranslation = new XbimVector3D(-p.X,-p.Y,-p.Z);
            model.RefencedModels.CollectionChanged += RefencedModels_CollectionChanged;
            //build the geometric scene and render as we go
            XbimScene<WpfMeshGeometry3D, WpfMaterial> scene = BuildScene(model, EntityLabels);
            if(scene.Layers.Count() > 0)
                scenes.Add(scene);
            foreach (var refModel in model.RefencedModels)
            {
                scenes.Add(BuildRefModelScene(refModel.Model, refModel.DocumentInformation));
            }
            ShowSpaces = false;
            RecalculateView(model);
        }

        private XbimRegion GetLargestRegion(XbimModel model)
        {
            IfcProject project = model.IfcProject;
            int projectId = 0;
            if (project != null) projectId = Math.Abs(project.EntityLabel);
            XbimGeometryData regionData = model.GetGeometryData(projectId, XbimGeometryType.Region).FirstOrDefault(); //get the region data should only be one
            
            if (regionData != null)
            {
                XbimRegionCollection regions = XbimRegionCollection.FromArray(regionData.ShapeData);
                return regions.MostPopulated();
            }
            else
                return null;
        }

        private void RecalculateView(XbimModel model)
        {
            if (!modelBounds.IsEmpty) //we have  geometry so create view box
                viewBounds = modelBounds;
          
            // Assumes a NearPlaneDistance of 1/8 of meter.
            //all models are now in metres
            Viewport_CameraChanged(null, null);

            //get bounding box for the whole scene and adapt gridlines to the model units
            //
            double widthModelUnits = viewBounds.SizeY;
            double lengthModelUnits = viewBounds.SizeX;
            long gridWidth = Convert.ToInt64(widthModelUnits /  10);
            long gridLen = Convert.ToInt64(lengthModelUnits / 10);
            if (gridWidth > 10 || gridLen > 10)
                this.GridLines.MinorDistance = 10;
            else
                this.GridLines.MinorDistance = 1;
            this.GridLines.Width = (gridWidth + 1) * 10;
            this.GridLines.Length = (gridLen + 1) * 10;

            this.GridLines.MajorDistance =  10;
            this.GridLines.Thickness = 0.01;
            XbimPoint3D p3d = viewBounds.Centroid();
            TranslateTransform3D t3d = new TranslateTransform3D(p3d.X, p3d.Y, viewBounds.Z);
            this.GridLines.Transform = t3d;
           
            //make sure whole scene is visible
            ViewHome();   
        }

        void RefencedModels_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add && e.NewItems.Count > 0)
            {
                XbimReferencedModel refModel = e.NewItems[0] as XbimReferencedModel;
                if (scenes.Count == 0) //need to calculate extents
                {
                    XbimRegion largest = GetLargestRegion(refModel.Model);
                    XbimPoint3D c = new XbimPoint3D(0, 0, 0);
                    XbimRect3D bb = XbimRect3D.Empty;
                    if (largest != null)
                        bb = new XbimRect3D(largest.Centre, largest.Centre);
                    XbimPoint3D p = bb.Centroid();
                    _modelTranslation = new XbimVector3D(-p.X, -p.Y, -p.Z);
                }
                XbimScene<WpfMeshGeometry3D, WpfMaterial> scene = BuildRefModelScene(refModel.Model, refModel.DocumentInformation);
                scenes.Add(scene);
                RecalculateView(refModel.Model);
            }
        }

        public void ReportData(StringBuilder sb, IModel model, int entityLabel)
        {
            foreach (var scene in scenes)
            {
                IXbimMeshGeometry3D mesh = scene.GetMeshGeometry3D(model.Instances[entityLabel]);
                mesh.ReportGeometryTo(sb);
            }
        }

        private XbimScene<WpfMeshGeometry3D, WpfMaterial> BuildRefModelScene(XbimModel model, IfcDocumentInformation docInfo)
        {
            XbimScene<WpfMeshGeometry3D, WpfMaterial> scene = new XbimScene<WpfMeshGeometry3D, WpfMaterial>(model);
            XbimGeometryHandleCollection handles = new XbimGeometryHandleCollection(model.GetGeometryHandles()
                                                       .Exclude(IfcEntityNameEnum.IFCFEATUREELEMENT)); // ifcSpaces added to the geometry
            double total = handles.Count;
            double processed = 0;

            XbimColour colour = federationColours[docInfo.DocumentOwner.RoleName()];
            double metre = model.GetModelFactors.OneMetre;
            wcsTransform = XbimMatrix3D.CreateTranslation(_modelTranslation) * XbimMatrix3D.CreateScale(1 / (float)metre);
                
            XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> layer = new XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>(model, colour) { Name = "All" };
            //add all content initially into the hidden field
            foreach (var geomData in model.GetGeometryData(handles))
            {
                geomData.TransformBy(wcsTransform);
                layer.AddToHidden(geomData);
                processed++;
                int progress = Convert.ToInt32(100.0 * processed / total);
            }

            this.Dispatcher.BeginInvoke(new Action(() => { AddLayerToDrawingControl(layer); }), System.Windows.Threading.DispatcherPriority.Background);
            lock (scene)
            {
                scene.Add(layer);

                if (modelBounds.IsEmpty) modelBounds = layer.BoundingBoxHidden();
                else modelBounds.Union(layer.BoundingBoxHidden());
            }

            this.Dispatcher.BeginInvoke(new Action(() => { Hide<IfcSpace>(); }), System.Windows.Threading.DispatcherPriority.Background);
            return scene;
        }

        private XbimScene<WpfMeshGeometry3D, WpfMaterial> BuildScene(XbimModel model, IEnumerable<int> LoadLabels)
        {
            // spaces are not excluded from the model to make the ShowSpaces property meaningful
            XbimScene<WpfMeshGeometry3D, WpfMaterial> scene = new XbimScene<WpfMeshGeometry3D, WpfMaterial>(model);
            XbimGeometryHandleCollection handles; 
                    // = new XbimGeometryHandleCollection(model.GetGeometryHandles().Exclude(IfcEntityNameEnum.IFCFEATUREELEMENT));
                    // .Exclude(IfcEntityNameEnum.IFCFEATUREELEMENT | IfcEntityNameEnum.IFCSPACE));
            if (LoadLabels == null)
                handles = new XbimGeometryHandleCollection(model.GetGeometryHandles().Exclude(IfcEntityNameEnum.IFCFEATUREELEMENT));
            else 
                handles = new XbimGeometryHandleCollection(model.GetGeometryHandles().Where(t => LoadLabels.Contains(t.ProductLabel)));

            double total = handles.Count;
            double processed = 0;

            IfcProject project = model.IfcProject;
            int projectId = 0;
            if (project != null) projectId = Math.Abs(project.EntityLabel);
            double metre = model.GetModelFactors.OneMetre;
            wcsTransform = XbimMatrix3D.CreateTranslation(_modelTranslation) * XbimMatrix3D.CreateScale((float)(1 / metre));

            Parallel.ForEach<KeyValuePair<string, XbimGeometryHandleCollection>>(handles.FilterByBuildingElementTypes(), layerContent =>
            //  foreach (var layerContent in handles.FilterByBuildingElementTypes())
            {
                string elementTypeName = layerContent.Key;
                XbimGeometryHandleCollection layerHandles = layerContent.Value;
                IEnumerable<XbimGeometryData> geomColl = model.GetGeometryData(layerHandles);
                XbimColour colour = scene.LayerColourMap[elementTypeName];
                XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> layer = new XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial>(model, colour) { Name = elementTypeName };
                //add all content initially into the hidden field
                foreach (var geomData in geomColl)
                {
                    geomData.TransformBy(wcsTransform);
                    layer.AddToHidden(geomData, model);
                    processed++;
                    int progress = Convert.ToInt32(100.0 * processed / total);
                }

                this.Dispatcher.BeginInvoke(new Action(() => { AddLayerToDrawingControl(layer); }), System.Windows.Threading.DispatcherPriority.Background);
                lock (scene)
                {
                    scene.Add(layer);

                    if (modelBounds.IsEmpty) modelBounds = layer.BoundingBoxHidden();
                    else modelBounds.Union(layer.BoundingBoxHidden());
                }
            }
            );
            this.Dispatcher.BeginInvoke(new Action(() => { Hide<IfcSpace>(); }), System.Windows.Threading.DispatcherPriority.Background);

            return scene;
        }


        

        /// <summary>
        /// function that actually populates the geometry from the layer into the viewer meshes.
        /// </summary>
        private void AddLayerToDrawingControl(XbimMeshLayer<WpfMeshGeometry3D, WpfMaterial> layer) // Formerely called DrawLayer
        {
            //move it to the visual element
            // byte[] bytes = ((XbimMeshGeometry3D)layer.Hidden).ToByteArray();
            layer.Show();

            GeometryModel3D m3d = (WpfMeshGeometry3D)layer.Visible;
            m3d.SetValue(TagProperty, layer);
            //sort out materials and bind
            if (layer.Style.RenderBothFaces)
                m3d.BackMaterial = m3d.Material = (WpfMaterial)layer.Material;
            else if (layer.Style.SwitchFrontAndRearFaces)
                m3d.BackMaterial = (WpfMaterial)layer.Material;
            else
                m3d.Material = (WpfMaterial)layer.Material;
            if (ForceRenderBothSides) m3d.BackMaterial = m3d.Material;
            _materials.Add(m3d.Material);
            // SetOpacityPercent(m3d.Material, ModelOpacity);
            ModelVisual3D mv = new ModelVisual3D();
            mv.Content = m3d;
            if (layer.Style.IsTransparent)
                Transparents.Children.Add(mv);
            else
                Opaques.Children.Add(mv);
            foreach (var subLayer in layer.SubLayers)
                AddLayerToDrawingControl(subLayer);
        }

        /// <summary>
        /// Returns the list of nested visual elements.
        /// </summary>
        /// <param name="OfItem">Valid names are for instance: Opaques, Transparents, BuildingModel, cuttingGroup...</param>
        /// <returns>IEnumerable names</returns>
        public IEnumerable<string> ListItems(string OfItem)
        {
            foreach (var scene in scenes)
                foreach (var layer in scene.SubLayers) //go over top level layers only
                    yield return layer.Name;
        }


        public void SetVisibility(string LayerName, bool visibility)
        {
            foreach (var scene in scenes)
            {
                foreach (var layer in scene.SubLayers) //go over top level layers only
                {
                    if (layer.Name == LayerName)
                    {
                        if (visibility == true)
                            layer.ShowAll();
                        else
                            layer.HideAll();
                    }
                }
            }
        }

        /// <summary>
        ///   Hides all instances of the specified type
        /// </summary>
        public void Hide<T>()
        {
            IfcType ifcType = IfcMetaData.IfcType(typeof(T));
            string toHide = ifcType.Name + ";";
            foreach (var subType in ifcType.NonAbstractSubTypes)
                toHide += subType.Name + ";";
            foreach (var scene in scenes)
                foreach (var layer in scene.SubLayers) //go over top level layers only
                    if (toHide.Contains(layer.Name + ";"))
                        layer.HideAll();
        }


        // Using a DependencyProperty as the backing store for ShowWalls.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LayerSetProperty =
            DependencyProperty.Register("LayerSet", typeof(List<LayerViewModel>), typeof(DrawingControl3D));

        public List<LayerViewModel> LayerSet
        {
            get { return (List<LayerViewModel>)GetValue(LayerSetProperty); }
            // set { SetValue(ShowSpacesProperty, value); }
        }

        private List<LayerViewModel> LayerSetRefresh()
        {
            var ret = new List<LayerViewModel>();
            ret.Add(new LayerViewModel("All", this));
            foreach (var scene in scenes)
                foreach (var layer in scene.SubLayers) //go over top level layers only
                    ret.Add(new LayerViewModel(layer.Name, this));
            return ret;
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
            IfcType ifcType = IfcMetaData.IfcType(typeof(T));
            string toShow = ifcType.Name + ";";
            foreach (var subType in ifcType.NonAbstractSubTypes)
                toShow += subType.Name + ";";
            foreach (var scene in scenes)
                foreach (var layer in scene.SubLayers) //go over top level layers only
                    if (toShow.Contains(layer.Name + ";"))
                        layer.ShowAll();
        }

        public void ShowAll()
        {
            //scene.ShowAll();
        }

        public void HideAll()
        {
            //scene.HideAll();
        }

        public void ViewHome()
        {
            XbimPoint3D c = viewBounds.Centroid();
            Point3D p = new Point3D(c.X, c.Y, c.Z);
            Viewport.CameraController.ResetCamera();
            Rect3D r3d = new Rect3D(viewBounds.X, viewBounds.Y, viewBounds.Z, viewBounds.SizeX, viewBounds.SizeY, viewBounds.SizeZ);
            Viewport.ZoomExtents(r3d);
        }

        public void ZoomSelected()
        {
            if (SelectedEntity != null && Highlighted != null && Highlighted.Mesh != null)
            {
                Rect3D r3d = Highlighted.Mesh.GetBounds();
                // Debug.WriteLine("SelectedBBox: " + r3d.ToString());
                ZoomTo(r3d);
            }
        }

        /// <summary>
        /// Zooms to a selected portion of the space.
        /// </summary>
        /// <param name="r3d">The box to be zoomed to</param>
        /// <param name="DoubleRectSize">Effectively doubles the size of the bounding box so to fit more space around it.</param>
        private void ZoomTo(Rect3D r3d, bool DoubleRectSize = true)
        {
            
            if (!r3d.IsEmpty)
            {
                Rect3D bounds = new Rect3D(viewBounds.X, viewBounds.Y, viewBounds.Z, viewBounds.SizeX, viewBounds.SizeY, viewBounds.SizeZ);
                if (DoubleRectSize)
                {
                    r3d.Offset(-r3d.SizeX / 2, -r3d.SizeY / 2, -r3d.SizeZ / 2);
                    r3d.SizeX *= 2;
                    r3d.SizeY *= 2;
                    r3d.SizeZ *= 2;
                }
                if (!r3d.IsEmpty)
                {
                    if (r3d.Contains(bounds)) //if bigger than bounds zoom bounds
                        Viewport.ZoomExtents(bounds, 200);
                    else
                        Viewport.ZoomExtents(r3d, 200);
                }
            }
        }

        public void ZoomTo(XbimRect3D r3d)
        {
            ZoomTo(new Rect3D(
                        new Point3D(r3d.X, r3d.Y, r3d.Z),
                        new Size3D(r3d.SizeX, r3d.SizeY, r3d.SizeZ)
                        ),
                   false);
        }
    }
}
