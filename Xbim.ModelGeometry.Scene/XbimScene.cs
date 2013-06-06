﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using Xbim.IO;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.ModelGeometry.Scene
{
    [Serializable]
    public class XbimScene<TVISIBLE, TMATERIAL>
        where TVISIBLE : IXbimMeshGeometry3D, new()
        where TMATERIAL : IXbimRenderMaterial, new()
    {
        
        XbimMeshLayerCollection<TVISIBLE, TMATERIAL> layers = new XbimMeshLayerCollection<TVISIBLE, TMATERIAL>();

        public XbimMeshLayerCollection<TVISIBLE, TMATERIAL> SubLayers
        {
            get { return layers; }
            set { layers = value; }
        }
       

        XbimColourMap layerColourMap;
        private XbimModel model;

        public XbimModel Model
        {
            get { return model; }
            set { model = value; }
        }

        /// <summary>
        /// The colour map for this scene
        /// </summary>
        public XbimColourMap LayerColourMap
        {
            get { return layerColourMap; }
        }
        /// <summary>
        /// Constructs a scene using the default IfcProductType colour map
        /// </summary>
        public XbimScene(XbimModel model)
            :this(model,new XbimColourMap())
        {
          
        }

        /// <summary>
        /// Constructs a scene, using the specfified colourmap
        /// </summary>
        /// <param name="colourMap"></param>
        public XbimScene(XbimModel model, XbimColourMap colourMap)
        {
            this.layerColourMap = colourMap;
            this.model = model;
           
        }

       

        /// <summary>
        /// Returns all the layers including sub layers of this scene
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
        /// Returns all layers and sublayers that have got some graphic content that is visible
        /// </summary>
        
        public IEnumerable<XbimMeshLayer<TVISIBLE, TMATERIAL>> VisibleLayers
        {
            get
            {
                foreach (var layer in layers)
                {
                    if (layer.Visible.Meshes.Any()) yield return layer;
                    foreach (var subLayer in layer.SubLayers)
                    {
                        if (subLayer.Visible.Meshes.Any()) yield return subLayer;
                    }
                }

            }
        }
        /// <summary>
        /// Add the layer to the scene
        /// </summary>
        /// <param name="layer"></param>
        public void Add(XbimMeshLayer<TVISIBLE, TMATERIAL> layer)
        {
            if (string.IsNullOrEmpty(layer.Name)) //ensure a layer has a unique name if the user has not defined one
                layer.Name = "Layer " + layers.Count();
            layers.Add(layer);
            
        }


        /// <summary>
        /// Makes all meshes in all layers in the scene Hidden
        /// </summary>
        public void HideAll()
        {
            foreach (var layer in layers)
                layer.HideAll();
        }

        public void ShowAll()
        {
            foreach (var layer in layers)
                layer.ShowAll();
        }

        /// <summary>
        /// Retrieves all the mesh fragments for the specified entity in this scene
        /// </summary>
        /// <param name="entityLabel"></param>
        /// <returns></returns>
        public XbimMeshFragmentCollection GetMeshFragments(int entityLabel)
        {
            XbimMeshFragmentCollection fragments = new XbimMeshFragmentCollection();
            foreach (var layer in Layers)
                fragments.AddRange(layer.GetMeshFragments(entityLabel));
            return fragments;
        }



        public IXbimMeshGeometry3D GetMeshGeometry3D(IPersistIfcEntity entity)
        {
            XbimMeshGeometry3D geometry = new XbimMeshGeometry3D();
            IModel m = entity.ModelOf;
            foreach (var layer in Layers)
            {
                if(layer.Model == m)
                    geometry.Add(layer.GetVisibleMeshGeometry3D(Math.Abs(entity.EntityLabel)));
            }
            return geometry;
        }

        public void Balance()
        {
            foreach (var layer in SubLayers)
            {
                layer.Balance();
            }
        }
    }
}