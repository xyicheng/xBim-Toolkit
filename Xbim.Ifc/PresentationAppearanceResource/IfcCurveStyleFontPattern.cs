#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcCurveStyleFontPattern.cs
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

namespace Xbim.Ifc.PresentationAppearanceResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcCurveStyleFontPattern : INotifyPropertyChanged, ISupportChangeNotification, IPersistIfcEntity,
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

        private IfcLengthMeasure _visibleSegmentLength;
        private IfcPositiveLengthMeasure _invisibleSegmentLength;

        #endregion

        #region Part 21 Step file Parse routines

        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcLengthMeasure VisibleSegmentLength
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _visibleSegmentLength;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _visibleSegmentLength, value, v => VisibleSegmentLength = v,
                                           "VisibleSegmentLength");
            }
        }


        [IfcAttribute(2, IfcAttributeState.Mandatory)]
        public IfcPositiveLengthMeasure InvisibleSegmentLength
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _invisibleSegmentLength;
            }
            set
            {
                if (value <= 0)
                    throw new ArgumentException("CurveStyleFontPattern.InvisibleSegmentLength must be greater than 0");
                else
                    ModelManager.SetModelValue(this, ref _invisibleSegmentLength, value, v => InvisibleSegmentLength = v,
                                               "InvisibleSegmentLength");
            }
        }


        public virtual void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _visibleSegmentLength = value.RealVal;
                    break;
                case 1:
                    _invisibleSegmentLength = value.RealVal;
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
            if (_visibleSegmentLength < 0)
                return "WR1   :   The value of a visible pattern length shall be equal or greater then zero.";
            else
                return "";
        }

        #endregion
    }
}