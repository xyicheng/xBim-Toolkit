#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcMaterialLayer.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.ComponentModel;
using System.Linq;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc2x3.MaterialResource
{
    [IfcPersistedEntityAttribute, Serializable]
    public class MaterLayerList : XbimList<IfcMaterialLayer>
    {
        internal MaterLayerList(IPersistIfcEntity owner)
            : base(owner)
        {
        }
    }

    /// <summary>
    ///   A single and identifiable part of an element which is constructed of a number of layers (one or more).
    /// </summary>
    /// <remarks>
    ///   Definition from IAI: A single and identifiable part of an element which is constructed of a number of layers (one or more). 
    ///   Each IfcMaterialLayer is located relative to the referencing IfcMaterialLayerSet. 
    ///   EXAMPLE: A cavity wall with brick masonry used in each leaf would be modeled using three IfcMaterialLayers: Brick-Air-Brick.
    /// </remarks>
    [IfcPersistedEntityAttribute, Serializable]
    public class IfcMaterialLayer : INotifyPropertyChanged, ISupportChangeNotification, IPersistIfcEntity,
                                    IfcMaterialSelect, IfcObjectReferenceSelect, INotifyPropertyChanging
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

        private IfcMaterial _material;
        private IfcPositiveLengthMeasure _layerThickness;
        private IfcBoolean? _isVentilated;

        #endregion

        #region Constructors

        #endregion

        #region Part 21 Step file Parse routines

        /// <summary>
        ///   Optional reference to the matsel from which the layer is constructed. Note, that if this value is not given, it does not denote a layer with no matsel (an air gap), it only means that the matsel is not specified at that point.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Optional)]
        public IfcMaterial Material
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _material;
            }
            set { this.SetModelValue(this, ref _material, value, v => Material = v, "Material"); }
        }


        /// <summary>
        ///   The thickness of the layer (dimension measured along the local x-axis of Mls LCS, in positive direction).
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Mandatory)]
        public IfcPositiveLengthMeasure LayerThickness
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _layerThickness;
            }
            set { this.SetModelValue(this, ref _layerThickness, value, v => LayerThickness = v, "LayerThickness"); }
        }


        /// <summary>
        ///   Indication of whether the matsel layer represents an air layer (or cavity).
        /// </summary>
        /// <remarks>
        ///   set to TRUE if the matsel layer is an air gap and provides air exchange from the layer to the outside air. 
        ///   set to NULL if the matsel layer is an air gap and does not provide air exchange (or when this information about air exchange of the air gap is not available). 
        ///   set to FALSE if the matsel layer is a solid matsel layer (the default).
        /// </remarks>
        [IfcAttribute(3, IfcAttributeState.Optional)]
        public IfcBoolean? IsVentilated
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _isVentilated;
            }
            set { this.SetModelValue(this, ref _isVentilated, value, v => IsVentilated = v, "IsVentilated"); }
        }


        public virtual void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _material = (IfcMaterial) value.EntityVal;
                    break;
                case 1:
                    _layerThickness = value.RealVal;
                    break;
                case 2:
                    _isVentilated = value.BooleanVal;
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }

        #endregion

        #region Inverse Relationships

        /// <summary>
        ///   Inverse. Reference to the matsel layer set, in which the matsel layer is included.
        /// </summary>
        [IfcAttribute(-1, IfcAttributeState.Mandatory)]
        public IfcMaterialLayerSet ToMaterialLayerSet
        {
            get
            {
                return
                    ModelOf.InstancesWhere<IfcMaterialLayerSet>(
                        ml => ml.MaterialLayers.Contains(this)).FirstOrDefault();
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

        #region MaterialSelect Members

        public string Name
        {
            get
            {
                return Material != null ? string.Format("{0} {1}", LayerThickness, Material.Name) : "";
            }
        }

        #endregion
    }
}