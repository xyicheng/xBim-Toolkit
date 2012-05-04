﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcMaterialLayerSetUsage.cs
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
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.MaterialResource
{
    /// <summary>
    ///   Determines the usage of IfcMaterialLayerSet in terms of its location and orientation relative to the associated element geometry.
    /// </summary>
    /// <remarks>
    ///   Definition from IAI: Determines the usage of IfcMaterialLayerSet in terms of its location and orientation relative to the associated element geometry. The location of matsel layer set shall be compatible with the element geometry (i.e. matsel layers shall fit inside the element geometry).
    ///   EXAMPLE: For a cavity brick wall with extruded geometric representation, the IfcMaterialLayerSet.TotalThickness shall be equal to the wall thickness. 
    ///   Material layer set is primarily intended to be associated with planar elements with constant thickness. With further agreements on the interpretation of layer set direction, the usage can be extended also to other cases, e.g. to curved elements.
    ///   Generally, an element may be layered in any of its primary directions, denoted by its x, y or z axis. However, with geometry use definitions for specific elements, it may become evident which direction is the relevant one for layering.
    ///   EXAMPLE: For a standard wall with extruded geometric representation (vertical extrusion), the layer set direction shall be perpendicular to extrusion direction, and coincide with the direction denoting the wall thickness (in positive or negative sense). For a curved wall, "direction denoting the wall thickness" can be derived from the element LCS, but it will remain perpendicular to the wall path. 
    ///   EXAMPLE: For a slab with vertically extruded geometric representation, the layer set direction shall coincide with the extrusion direction (in positive or negative sense). 
    ///   Fig 1: shows the use of IfcMaterialLayerSetUsage aligned to the axis of a wall.
    /// </remarks>
    [IfcPersistedEntity, Serializable]
    public class IfcMaterialLayerSetUsage : INotifyPropertyChanged, ISupportChangeNotification, IPersistIfcEntity,
                                            IfcMaterialSelect, INotifyPropertyChanging
    {
#if SupportActivation

        #region IPersistIfcEntity Members

        private long _entityLabel;
        private IModel _model;

        IModel IPersistIfcEntity.ModelOf
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

#endif

        #region Fields

        private IfcMaterialLayerSet _forLayerSet;
        private IfcLayerSetDirectionEnum _layerSetDirection;
        private IfcDirectionSenseEnum _directionSense;
        private IfcLengthMeasure _offsetFromReferenceLine;

        #endregion

        #region Constructors

        #endregion

        #region Part 21 Step file Parse routines

        /// <summary>
        ///   Layer set to which the usage is applied.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcMaterialLayerSet ForLayerSet
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _forLayerSet;
            }
            set { ModelManager.SetModelValue(this, ref _forLayerSet, value, v => ForLayerSet = v, "ForLayerSet"); }
        }

        /// <summary>
        ///   Orientation of the layer set relative to element geometry. The meaning of the value of this attribute shall be specified in the geometry use section for each element. For extruded geometric representation, direction can be given along the extrusion path (e.g. for slabs) or perpendicular to it (e.g. for walls).
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Mandatory)]
        public IfcLayerSetDirectionEnum LayerSetDirection
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _layerSetDirection;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _layerSetDirection, value, v => LayerSetDirection = v,
                                           "LayerSetDirection");
            }
        }


        /// <summary>
        ///   Denotion whether the layer set is oriented in positive or negative sense along the axis given by LayerSetDirection.
        /// </summary>
        [IfcAttribute(3, IfcAttributeState.Mandatory)]
        public IfcDirectionSenseEnum DirectionSense
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _directionSense;
            }
            set { ModelManager.SetModelValue(this, ref _directionSense, value, v => DirectionSense = v, "DirectionSense"); }
        }


        /// <summary>
        ///   Offset of the matsel layer set (MlsBase) from reference line. The offset can be positive or negative, unless restricted for a particular building element type in its use definition or by implementer agreement. The reference line for each IfcElement is defined in use definition for the element.
        /// </summary>
        [IfcAttribute(4, IfcAttributeState.Mandatory)]
        public IfcLengthMeasure OffsetFromReferenceLine
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _offsetFromReferenceLine;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _offsetFromReferenceLine, value, v => OffsetFromReferenceLine = v,
                                           "OffsetFromReferenceLine");
            }
        }


        public virtual void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _forLayerSet = (IfcMaterialLayerSet) value.EntityVal;
                    break;
                case 1:
                    _layerSetDirection =
                        (IfcLayerSetDirectionEnum) Enum.Parse(typeof (IfcLayerSetDirectionEnum), value.EnumVal, true);
                    break;
                case 2:
                    _directionSense =
                        (IfcDirectionSenseEnum) Enum.Parse(typeof (IfcDirectionSenseEnum), value.EnumVal, true);
                    break;
                case 3:
                    _offsetFromReferenceLine = value.RealVal;
                    break;
                default:
                    throw new Exception(string.Format("Attribute index {0} is out of range for {1}", propIndex + 1,
                                                      this.GetType().Name.ToUpper()));
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
            get { return _forLayerSet != null ? _forLayerSet.Name : ""; }
        }

        #endregion
    }
}