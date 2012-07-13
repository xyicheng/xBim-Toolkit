#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcSurfaceStyleRefraction.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.ComponentModel;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc.PresentationAppearanceResource
{
    [IfcPersistedEntityAttribute, Serializable]
    public class IfcSurfaceStyleRefraction : INotifyPropertyChanged, ISupportChangeNotification, IPersistIfcEntity,
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

        private IfcReal? _refractionIndex;
        private IfcReal? _dispersionFactor;

        #endregion

        #region Part 21 Step file Parse routines

        /// <summary>
        ///   The index of refraction for all wave lengths of light. 
        ///   The refraction index is the ratio between the speed of light in a vacuum and the speed of light in the medium. 
        ///   E.g. glass has a refraction index of 1.5, whereas water has an index of 1.33
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Optional)]
        public IfcReal? RefractionIndex
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _refractionIndex;
            }
            set
            {
                this.SetModelValue(this, ref _refractionIndex, value, v => RefractionIndex = v,
                                           "RefractionIndex");
            }
        }

        /// <summary>
        ///   The Abbe constant given as a fixed ratio between the refractive indices of the material at different wavelengths. 
        ///   A low Abbe number means a high dispersive power. 
        ///   In general this translates to a greater angular spread of the emergent spectrum.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Optional)]
        public IfcReal? DispersionFactor
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _dispersionFactor;
            }
            set
            {
                this.SetModelValue(this, ref _dispersionFactor, value, v => DispersionFactor = v,
                                           "DispersionFactor");
            }
        }


        public virtual void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _refractionIndex = value.RealVal;
                    break;
                case 1:
                    _dispersionFactor = value.RealVal;
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