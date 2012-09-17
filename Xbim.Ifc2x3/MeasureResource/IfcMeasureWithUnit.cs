#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcMeasureWithUnit.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.ComponentModel;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc2x3.MeasureResource
{
    [IfcPersistedEntityAttribute, Serializable]
    public class IfcMeasureWithUnit : IPersistIfcEntity, IfcAppliedValueSelect, IfcMetricValueSelect,
                                      ISupportChangeNotification, INotifyPropertyChanged, INotifyPropertyChanging
    {

        #region IPersistIfcEntity Members

        private int _entityLabel;
        private IModel _model;

        public IModel ModelOf
        {
            get { return _model; }
        }

        void IPersistIfcEntity.Bind(IModel model, int entityLabel)
        {
            _model = model;
            _entityLabel = entityLabel;
        }

        bool IPersistIfcEntity.Activated
        {
            get { return _entityLabel > 0; }
        }

        public int EntityLabel
        {
            get { return _entityLabel; }
        }

        void IPersistIfcEntity.Activate(bool write)
        {
            if (_model != null && _entityLabel <= 0) _entityLabel = _model.Activate(this, false);
            if (write) _model.Activate(this, write);
        }

        #endregion

        #region Part 21 Step file Parse routines

        private IfcValue _valueComponent;
        private IfcUnit _unitComponent;

        /// <summary>
        ///   The value of the physical quantity when expressed in the specified units.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcValue ValueComponent
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _valueComponent;
            }
            set { this.SetModelValue(this, ref _valueComponent, value, v => ValueComponent = v, "ValueComponent"); }
        }

        /// <summary>
        ///   The unit in which the physical quantity is expressed.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Mandatory)]
        public IfcUnit UnitComponent
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _unitComponent;
            }
            set { this.SetModelValue(this, ref _unitComponent, value, v => UnitComponent = v, "UnitComponent"); }
        }

        public virtual void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _valueComponent = (IfcValue) value.EntityVal;
                    break;
                case 1:
                    _unitComponent = (IfcUnit) value.EntityVal;
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }

        #endregion

        public IfcMeasureWithUnit(IfcValue val, IfcUnit unit)
        {
            _valueComponent = val;
            _unitComponent = unit;
        }

        public IfcMeasureWithUnit()
        {
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

        #region ISupportChangeNotification Members

        void ISupportChangeNotification.NotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
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

        #region ISupportIfcParser Members

        public string WhereRule()
        {
            return "";
        }

        #endregion
    }
}