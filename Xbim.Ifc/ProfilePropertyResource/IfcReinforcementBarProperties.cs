#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcReinforcementBarProperties.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.ComponentModel;
using Xbim.Ifc.MeasureResource;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.ProfilePropertyResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcReinforcementBarProperties : INotifyPropertyChanged, ISupportChangeNotification, IPersistIfcEntity,
                                                 INotifyPropertyChanging
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

        private IfcAreaMeasure _totalCrossSectionArea;
        private IfcLabel _steelGrade;
        private IfcReinforcingBarSurfaceEnum? _barSurface;
        private IfcLengthMeasure? _effectiveDepth;
        private IfcPositiveLengthMeasure? _nominalBarDiameter;
        private IfcCountMeasure? _barCount;

        #endregion

        #region Properties

        /// <summary>
        ///   The total effective cross-section area of the reinforcement of a specific steel grade.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcAreaMeasure TotalCrossSectionArea
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _totalCrossSectionArea;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _totalCrossSectionArea, value, v => TotalCrossSectionArea = v,
                                           "TotalCrossSectionArea");
            }
        }

        /// <summary>
        ///   The nominal steel grade defined according to local standards.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Mandatory)]
        public IfcLabel SteelGrade
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _steelGrade;
            }
            set { ModelManager.SetModelValue(this, ref _steelGrade, value, v => SteelGrade = v, "SteelGrade"); }
        }

        ///<summary>
        ///  Indicator for whether the bar surface is plain or textured.
        ///</summary>
        [IfcAttribute(3, IfcAttributeState.Optional)]
        public IfcReinforcingBarSurfaceEnum? BarSurface
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _barSurface;
            }
            set { ModelManager.SetModelValue(this, ref _barSurface, value, v => BarSurface = v, "BarSurface"); }
        }

        /// <summary>
        ///   The effective depth, i.e. the distance of the specific reinforcement cross section area or reinforcement configuration in a row, counted from a common specific reference point. Usually 
        ///   the reference point is the upper surface (for beams and slabs) or a similar projection in a plane (for columns).
        /// </summary>
        [IfcAttribute(4, IfcAttributeState.Optional)]
        public IfcLengthMeasure? EffectiveDepth
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _effectiveDepth;
            }
            set { ModelManager.SetModelValue(this, ref _effectiveDepth, value, v => EffectiveDepth = v, "EffectiveDepth"); }
        }

        /// <summary>
        ///   The nominal diameter defining the cross-section size of the reinforcing bar. 
        ///   The bar diameter should be identical for all bars included in the specific reinforcement configuration.
        /// </summary>
        [IfcAttribute(5, IfcAttributeState.Optional)]
        public IfcPositiveLengthMeasure? NominalBarDiameter
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _nominalBarDiameter;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _nominalBarDiameter, value, v => NominalBarDiameter = v,
                                           "NominalBarDiameter");
            }
        }

        [IfcAttribute(7, IfcAttributeState.Optional)]
        public IfcCountMeasure? BarCount
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _barCount;
            }
            set { ModelManager.SetModelValue(this, ref _barCount, value, v => BarCount = v, "BarCount"); }
        }

        #endregion

        public virtual void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _totalCrossSectionArea = value.RealVal;
                    break;
                case 1:
                    _steelGrade = value.StringVal;
                    break;
                case 2:
                    _barSurface =
                        (IfcReinforcingBarSurfaceEnum)
                        Enum.Parse(typeof (IfcReinforcingBarSurfaceEnum), value.StringVal, true);
                    break;
                case 3:
                    _effectiveDepth = value.RealVal;
                    break;
                case 4:
                    _nominalBarDiameter = value.RealVal;
                    break;
                case 5:
                    _barCount = value.NumberVal;
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
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

        #region IPersistIfc Members

        public virtual string WhereRule()
        {
            return "";
        }

        #endregion
    }
}