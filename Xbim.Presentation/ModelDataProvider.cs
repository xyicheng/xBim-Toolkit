#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    ModelDataProvider.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.RepresentationResource;
using Xbim.XbimExtensions;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions.Interfaces;
using Xbim.IO;

#endregion

namespace Xbim.Presentation
{
    

    public class MaterialDictionary : Dictionary<object, Material>
    {
    }


    public class ModelDataProvider : INotifyPropertyChanged 
    {
      
        public ModelDataProvider()
        {
            _defaultMaterials = new MaterialDictionary();
            _defaultMaterials.Add("IfcProduct", new DiffuseMaterial(new SolidColorBrush(Colors.Wheat)));
            _defaultMaterials.Add("IfcBuildingElementProxy", new DiffuseMaterial(new SolidColorBrush(Colors.Snow)));
            _defaultMaterials.Add("IfcWall", new DiffuseMaterial(new SolidColorBrush(Colors.White)));
            _defaultMaterials.Add("IfcRoof", new DiffuseMaterial(new SolidColorBrush(Colors.LightSteelBlue)));
            _defaultMaterials.Add("IfcSlab", new DiffuseMaterial(new SolidColorBrush(Colors.LightSteelBlue) { }));
            _defaultMaterials.Add("IfcWindow",
                                  new DiffuseMaterial(new SolidColorBrush(Colors.LightBlue) {Opacity = 0.4}));
            _defaultMaterials.Add("IfcDoor", new DiffuseMaterial(new SolidColorBrush(Colors.CadetBlue) {}));
            _defaultMaterials.Add("IfcStair",
                                  new DiffuseMaterial(new SolidColorBrush(Colors.Wheat)));
            _defaultMaterials.Add("IfcBeam", new DiffuseMaterial(new SolidColorBrush(Colors.LightSlateGray) { }));
            _defaultMaterials.Add("IfcColumn", new DiffuseMaterial(new SolidColorBrush(Colors.LightSlateGray) { }));
            _defaultMaterials.Add("IfcFurnishingElement",
                                  new DiffuseMaterial(new SolidColorBrush(Colors.WhiteSmoke) {Opacity = 0.7}));
            _defaultMaterials.Add("IfcDistributionFlowElement",
                                  new DiffuseMaterial(new SolidColorBrush(Colors.AntiqueWhite) {Opacity = 1.0}));
            _defaultMaterials.Add("IfcSpace", new DiffuseMaterial(new SolidColorBrush(Colors.Red) {Opacity = 0.7}));
            _defaultMaterials.Add("IfcPlate", new DiffuseMaterial(new SolidColorBrush(Colors.LightBlue) {Opacity = 0.4}));
            _defaultMaterials.Add("IfcRailing", new DiffuseMaterial(new SolidColorBrush(Colors.Goldenrod) {  }));
        }

        #region Fields

        private readonly MaterialDictionary _defaultMaterials;
        private readonly MaterialDictionary _materials = new MaterialDictionary();
        private double _transparency = 0.5;

       

        private IXbimScene _scene;

        public IXbimScene Scene
        {
            get { return _scene; }
            set { _scene = value; NotifyPropertyChanged("Model"); NotifyPropertyChanged("Scene"); }
        }

        #endregion


        public Material GetDefaultMaterial(string typeName)
        {
            Material mat;
            IfcType elemType = IfcInstances.IfcTypeLookup[typeName.ToUpper()];
            while (elemType != null)
            {
                if (_defaultMaterials.TryGetValue(elemType.Type.Name, out mat))
                    return mat;
                elemType = elemType.IfcSuperType;
            }
            return null;
        }

        public Material GetDefaultMaterial(IPersistIfcEntity obj)
        {
            if (obj != null)
                return GetDefaultMaterial(obj.GetType().Name);
            else
                return null;
        }


        /// <summary>
        ///   Dictionary of shared materials, key is normally an Ifc object that the material represents
        /// </summary>
        public MaterialDictionary Materials
        {
            get { return _materials; }
        }

        public MaterialDictionary DefaultMaterials
        {
            get { return _defaultMaterials; }
        }


        public IModel Model
        {
            get {
                if (_scene != null && _scene.Graph != null)
                    return _scene.Graph.Model;
                return null;
            }
        }

        public double Transparency
        {
            get { return _transparency; }
            set
            {
                foreach (KeyValuePair<object, Material> item in _materials)
                {
                    DiffuseMaterial dMat = item.Value as DiffuseMaterial;
                    if (dMat != null)
                    {
                        SolidColorBrush br = dMat.Brush as SolidColorBrush;
                        if (br != null)
                            br.Opacity = value;
                    }
                }
                foreach (KeyValuePair<object, Material> kvp in _defaultMaterials)
                {
                    DiffuseMaterial dMat = kvp.Value as DiffuseMaterial;
                    if (dMat != null)
                    {
                        SolidColorBrush br = dMat.Brush as SolidColorBrush;
                        if (br != null)
                        {
                            if (((string) kvp.Key) != "IfcSpace")
                                br.Opacity = Math.Max(value, 0);
                            else
                                br.Opacity = Math.Max(value*-1, 0);
                        }
                    }
                }
                _transparency = value;
            }
        }

        #region INotifyPropertyChanged Members

        [field: NonSerialized] //don't serialize events
            private event PropertyChangedEventHandler PropertyChanged;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { PropertyChanged += value; }
            remove { PropertyChanged -= value; }
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}