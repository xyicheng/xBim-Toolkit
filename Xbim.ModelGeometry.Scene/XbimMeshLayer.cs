﻿using System;
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
        XbimRect3D boundingBox;

        /// <summary>
        /// Bounding box, aligned to the XYZ axis, containing all points in this mesh
        /// </summary>
        public XbimRect3D BoundingBox
        {
            get { return boundingBox; }
            set { boundingBox = value; }
        }

        public void CalculateBoundingBox()
        {

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
                subLayer.Hidden.Append(geomData);
            }
            else
            {
                Hidden.Append(geomData); //just add it to the main layer
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
    }
}
