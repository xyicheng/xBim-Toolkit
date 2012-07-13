#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcSurfaceStyleLighting.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.ComponentModel;
using Xbim.Ifc.PresentationResource;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc.PresentationAppearanceResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcSurfaceStyleLighting : INotifyPropertyChanged, ISupportChangeNotification, IPersistIfcEntity,
                                           IfcSurfaceStyleElementSelect, INotifyPropertyChanging
    {

        #region IPersistIfcEntity Members

        private long _entityLabel;
        private IModel _model;

        public IModel ModelOf
        {
            get { return _model; }
        }

        void IPersistIfcEntity.Bind(IModel model, long entityLabel)
        {
            _model = model;
            _entityLabel = entityLabel;
        }

        bool IPersistIfcEntity.Activated
        {
            get { return _entityLabel > 0; }
        }

        public long EntityLabel
        {
            get { return _entityLabel; }
        }

        void IPersistIfcEntity.Activate(bool write)
        {
            if (_model != null && _entityLabel <= 0) _entityLabel = _model.Activate(this, false);
            if (write) _model.Activate(this, write);
        }

        #endregion

        #region Fields

        private IfcColourRgb _diffuseTransmissionColour;
        private IfcColourRgb _diffuseReflectionColour;
        private IfcColourRgb _transmissionColour;
        private IfcColourRgb _reflectanceColour;

        #endregion

        #region Part 21 Step file Parse routines

        /// <summary>
        ///   The degree of diffusion of the transmitted light. In the case of completely transparent materials there is no diffusion. 
        ///   The greater the diffusing power, the smaller the direct component of the transmitted light, up to the point where only diffuse light is produced.
        ///   A value of 1 means totally diffuse for that colour part of the light.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcColourRgb DiffuseTransmissionColour
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _diffuseTransmissionColour;
            }
            set
            {
                ModelHelper.SetModelValue(this, ref _diffuseTransmissionColour, value,
                                           v => DiffuseTransmissionColour = v, "DiffuseTransmissionColour");
            }
        }

        /// <summary>
        ///   The degree of diffusion of the reflected light. In the case of specular surfaces there is no diffusion. 
        ///   The greater the diffusing power of the reflecting surface, the smaller the specular component of the reflected light,
        ///   up to the point where only diffuse light is produced. 
        ///   A value of 1 means totally diffuse for that colour part of the light.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Mandatory)]
        public IfcColourRgb DiffuseReflectionColour
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _diffuseReflectionColour;
            }
            set
            {
                ModelHelper.SetModelValue(this, ref _diffuseReflectionColour, value, v => DiffuseReflectionColour = v,
                                           "DiffuseReflectionColour");
            }
        }

        /// <summary>
        ///   Describes how the light falling on a body is totally or partially transmitted.
        /// </summary>
        [IfcAttribute(3, IfcAttributeState.Mandatory)]
        public IfcColourRgb TransmissionColour
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _transmissionColour;
            }
            set
            {
                ModelHelper.SetModelValue(this, ref _transmissionColour, value, v => TransmissionColour = v,
                                           "TransmissionColour");
            }
        }

        /// <summary>
        ///   A coefficient that determines the extent that the light falling onto a surface is fully or partially reflected.
        /// </summary>
        [IfcAttribute(4, IfcAttributeState.Mandatory)]
        public IfcColourRgb ReflectanceColour
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _reflectanceColour;
            }
            set
            {
                ModelHelper.SetModelValue(this, ref _reflectanceColour, value, v => ReflectanceColour = v,
                                           "ReflectanceColour");
            }
        }


        public virtual void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _diffuseTransmissionColour = (IfcColourRgb) value.EntityVal;
                    break;
                case 1:
                    _diffuseReflectionColour = (IfcColourRgb) value.EntityVal;
                    break;
                case 2:
                    _transmissionColour = (IfcColourRgb) value.EntityVal;
                    break;
                case 3:
                    _reflectanceColour = (IfcColourRgb) value.EntityVal;
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }

        #endregion

        #region INotifyPropertyChanged Members

        [field: NonSerialized] //don't serialize events
            private event PropertyChangedEventHandler PropertyChanged;

        event PropertyChangedEventHandler INotifyPropertyChanged.PropertyChanged
        {
            add { PropertyChanged += value; }
            remove { PropertyChanged -= value; }
        }

        void ISupportChangeNotification.NotifyPropertyChanging(string propertyName)
        {
            PropertyChangingEventHandler handler = PropertyChanging;
            if (handler != null)
            {
                handler(this, new PropertyChangingEventArgs(propertyName));
            }
        }

        [field: NonSerialized] //don't serialize events
            private event PropertyChangingEventHandler PropertyChanging;

        event PropertyChangingEventHandler INotifyPropertyChanging.PropertyChanging
        {
            add { PropertyChanging += value; }
            remove { PropertyChanging -= value; }
        }

        #endregion

        #region ISupportChangeNotification Members

        void ISupportChangeNotification.NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion

        #region ISupportIfcParser Members

        public string WhereRule()
        {
            return "";
        }

        #endregion
    }
}