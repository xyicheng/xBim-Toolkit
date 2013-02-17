using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.PresentationAppearanceResource;
using Xbim.Ifc2x3.PresentationResource;
using Xbim.IO;
using Xbim.XbimExtensions;

namespace Xbim.ModelGeometry.Scene
{

    /// <summary>
    /// Provides support for a layer of meshes of the same material, TVISIBLE is the type of the mesh geometry required by the graphics adaptor
    /// </summary>
    public class XbimMeshLayer<TVISIBLE, TMATERIAL>
        where TVISIBLE : IXbimMeshGeometry3D, new()
        where TMATERIAL : IXbimRenderMaterial, new()
    {
        string name;
        XbimMeshLayerCollection<TVISIBLE, TMATERIAL> subLayerMap = new XbimMeshLayerCollection<TVISIBLE, TMATERIAL>();
        XbimColourMap layerColourMap;
        XbimRect3D boundingBoxVisible = XbimRect3D.Empty;
        XbimRect3D boundingBoxHidden = XbimRect3D.Empty;

        /// <summary>
        /// Bounding box of all visible elements, aligned to the XYZ axis, containing all points in this mesh
        /// </summary>
        /// <param name="forceRecalculation">if true the bounding box is recalculated, if false the previous cached version is returned</param>
        /// <returns></returns>
        public XbimRect3D BoundingBoxVisible(bool forceRecalculation = false)
        {
            if (forceRecalculation || boundingBoxVisible.IsEmpty)
            {
                bool first = true;
                foreach (var pos in Hidden.Positions)
                {
                    if (first)
                    {
                        boundingBoxVisible = new XbimRect3D(pos);
                        first = false;
                    }
                    else
                        boundingBoxVisible.Union(pos);

                }
            }
            return boundingBoxVisible; 
        }

        /// <summary>
        /// Bounding box of all hidden elements, aligned to the XYZ axis, containing all points in this mesh
        /// </summary>
        /// <param name="forceRecalculation">if true the bounding box is recalculated, if false the previous cached version is returned</param>
        /// <returns></returns>
        public XbimRect3D BoundingBoxHidden(bool forceRecalculation = false)
        {
            if (forceRecalculation || boundingBoxHidden.IsEmpty)
            {
                bool first = true;
                foreach (var pos in Hidden.Positions)
                {
                    if (first)
                    {
                        boundingBoxHidden = new XbimRect3D(pos);
                        first = false;
                    }
                    else
                        boundingBoxHidden.Union(pos);

                }
            }
            return boundingBoxHidden; 
        }

     


        /// <summary>
        /// The colour map for this scene
        /// </summary>
        public XbimColourMap LayerColourMap
        {
            get { return layerColourMap; }
        }

        public XbimTexture Style { get; set; }
        
        /// <summary>
        /// A mesh that are currently rendered typically on the graphics adaptor
        /// </summary>
        
        public IXbimMeshGeometry3D Visible = new TVISIBLE();

        /// <summary>
        /// The native graphic engine render material
        /// </summary>
        
        public IXbimRenderMaterial Material = new TMATERIAL();
        /// <summary>
        /// A mesh that is loaded but not visible on the graphics display
        /// </summary>
        public IXbimMeshGeometry3D Hidden = new XbimMeshGeometry3D();
      
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        /// <summary>
        /// Creates a mesh using the default colour (typically white)
        /// </summary>
        public XbimMeshLayer()
            :this(XbimColour.Default)
        {
            
        }

        

        /// <summary>
        /// Create a new layer that will display meshes in the specified colour
        /// If the mesh geometry item has a style specified in the IFC definition sub layers will be created for each style
        /// </summary>
        /// <param name="colour"></param>
        public XbimMeshLayer(XbimColour colour)
        {
            Style = new XbimTexture().CreateTexture(colour);
            
        }

       

        public XbimMeshLayer(XbimColour colour, XbimColourMap subCategoryColourMap)
            :this(colour)
        {
            layerColourMap = subCategoryColourMap;
        }

        public XbimMeshLayer(IfcSurfaceStyle style)
        {
            Style = new XbimTexture().CreateTexture(style);
           
        }

        public XbimMeshLayer(XbimTexture xbimTexture)
        {
            Style = xbimTexture;
        }

        public static implicit operator TVISIBLE(XbimMeshLayer<TVISIBLE, TMATERIAL> layer)
        {
            return (TVISIBLE)layer.Visible;
        }
        

        /// <summary>
        /// Returns true if the layer has any geometric content
        /// </summary>
        public bool HasContent
        {
            get
            {
                return Hidden.Meshes.Any() || Visible.Meshes.Any();
            }

        }

        /// <summary>
        /// Moves all items in the hidden mesh to the visible mesh
        /// </summary>
        public void ShowAll()
        {
            Hidden.MoveTo(Visible);
            foreach (var subLayer in subLayerMap)
            {
                subLayer.ShowAll();
            }
        }

        /// <summary>
        /// Moves all items in the visible mesh to the hidden mesh
        /// </summary>
        public void HideAll()
        {
            Visible.MoveTo(Hidden);
            foreach (var subLayer in subLayerMap)
            {
                subLayer.HideAll();
            }
        }

        /// <summary>
        /// Adds the geometry fragment to the hidden mesh, if the model is not null 
        /// the fragment is placed on a sub layer of the correct style
        /// the sub layer is automaticaly created if it does not exist.
        /// </summary>
        /// <param name="geomData"></param>
        /// <param name="model"></param>
        public void AddToHidden(XbimGeometryData geomData, XbimModel model = null)
        {
            if (model != null && geomData.StyleLabel > 0) //check if we need to put this item on a sub layer
            {
                XbimMeshLayer<TVISIBLE, TMATERIAL> subLayer;
                string layerName = geomData.StyleLabel.ToString();
                if (!subLayerMap.Contains(layerName))
                {
                    IfcSurfaceStyle style = model.Instances[geomData.StyleLabel] as IfcSurfaceStyle;
                    //create a sub layer
                    subLayer = new XbimMeshLayer<TVISIBLE, TMATERIAL>(style);
                    subLayer.Name = layerName;
                    subLayerMap.Add(subLayer);
                }
                else
                    subLayer = subLayerMap[layerName];
                
                subLayer.AddToHidden(geomData);
            }
            else
            {
                if(!Hidden.Add(geomData)) //just add it to the main layer, if the main layer is too big split it.
                {
                    //try and find a sublayer that is a split of this, i.e. has the same texture
                    foreach (var sublayer in subLayerMap.Reverse())
                    {
                        if (sublayer.Style == this.Style) //FOUND THE LAST ONE WITH THE SAME STYLE
                        {
                            sublayer.AddToHidden(geomData);//try and add the data to this mesh
                            return; //succeeded so return
                        }
                    }
                    //didn't find a layer to add it to so create a new one
                    XbimMeshLayer<TVISIBLE, TMATERIAL> subLayer = new XbimMeshLayer<TVISIBLE, TMATERIAL>(this.Style);
                    subLayer.Name = this.Name + "-" + subLayerMap.Count;
                    subLayerMap.Add(subLayer);
                    subLayer.Hidden.Add(geomData); //this should always pass as it is a new mesh and ifc geom rarely exceeds max mesh size, graphics cards will truncate anyway
                }
            }
        }

       
        public XbimMeshLayerCollection<TVISIBLE, TMATERIAL> SubLayers 
        {
            get
            {
                return subLayerMap;
            }
        }

        public void Show()
        {
            if (!Material.IsCreated) Material.CreateMaterial(Style);
            Hidden.MoveTo(Visible);
        }

       /// <summary>
        ///  Returns a collection of fragments for this layer, does not traverse sub layers or hidden layers unless arguments are true
       /// </summary>
       /// <param name="entityLabel">the ifc entity label</param>
       /// <param name="includeHidden">Include fragments in hidden layers</param>
       /// <param name="includSublayers">Recurse into sub layers</param>
       /// <returns></returns>
        internal IEnumerable<XbimMeshFragment> GetMeshFragments(int entityLabel, bool includeHidden = false, bool includSublayers = false)
        {
            foreach (var mf in Visible.Meshes.Where(m => m.EntityLabel == entityLabel))
                yield return mf;
            if (includeHidden)
                foreach (var mf in Hidden.Meshes.Where(m => m.EntityLabel == entityLabel))
                    yield return mf;
            if (includSublayers)
                foreach (var layer in SubLayers)
                    foreach (var mf in layer.GetMeshFragments(entityLabel, includeHidden, includSublayers))
                        yield return mf;
        }

        public IXbimMeshGeometry3D GetVisibleMeshGeometry3D(int entityLabel)
        {
            IEnumerable<XbimMeshFragment> fragments = GetMeshFragments(entityLabel); //get all the fragments for this entity in the visible layer
            int maxSize = fragments.Sum(f => f.PositionCount);
            XbimMeshGeometry3D geometry = new XbimMeshGeometry3D(maxSize);
            foreach (var fragment in fragments)
            {
                IXbimMeshGeometry3D geom = Visible.GetMeshGeometry3D(fragment);
                geometry.Add(geom, fragment.EntityLabel, fragment.EntityType);
            } 
            return geometry;
        }
        /// <summary>
        /// Returns all the layers including sub layers of this layer
        /// </summary>
        public IEnumerable<XbimMeshLayer<TVISIBLE, TMATERIAL>> Layers
        {
            get
            {
                foreach (var layer in SubLayers)
                {
                    yield return layer;
                    foreach (var subLayer in layer.Layers)
                    {
                        yield return subLayer;
                    }
                }

            }
        }
        /// <summary>
        /// Resizes the layers so that noe has more than USHORT number of indices
        /// </summary>
        public void Balance()
        {
            if (Hidden.TriangleIndices.Count >= ushort.MaxValue) //split the layer
            {
                System.Diagnostics.Debug.WriteLine("Too big");
            }
            foreach (var layer in SubLayers)
            {
                layer.Balance();
            }
        }
    }
}
