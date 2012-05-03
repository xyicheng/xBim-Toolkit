#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Presentation
// Filename:    XbimMaterialProvider.cs
// Published:   01, 2012
// Last Edited: 9:05 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.ComponentModel;
using System.Windows.Media.Media3D;

#endregion

namespace Xbim.Presentation
{
    public class XbimMaterialProvider : INotifyPropertyChanged
    {
        private Material _faceMaterial;
        private Material _backgroundMaterial;

        /// <summary>
        ///   Sets face and background Material to material
        /// </summary>
        /// <param name = "material"></param>
        public XbimMaterialProvider(Material material)
        {
            _faceMaterial = material;
            _backgroundMaterial = material;
        }

        public XbimMaterialProvider(Material faceMaterial, Material backgroundMaterial)
        {
            _faceMaterial = faceMaterial;
            _backgroundMaterial = backgroundMaterial;
        }

        public Material FaceMaterial
        {
            get { return _faceMaterial; }
            set
            {
                _faceMaterial = value;
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs("FaceMaterial"));
                }
            }
        }


        public Material BackgroundMaterial
        {
            get { return _backgroundMaterial; }
            set
            {
                _backgroundMaterial = value;
                PropertyChangedEventHandler handler = PropertyChanged;
                if (handler != null)
                {
                    handler(this, new PropertyChangedEventArgs("BackgroundMaterial"));
                }
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

        #endregion
    }
}