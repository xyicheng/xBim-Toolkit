#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcTimeSeriesValue.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.ComponentModel;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc.TimeSeriesResource
{
    /// <summary>
    ///   A time series value is a list of values that comprise the time series. At least one value must be supplied. 
    ///   Applications are expected to normalize values by applying the following three rules: 
    ///   All time (universal, local, daylight savings, and solar) is normalized against the ISO 8601 standard GMT/UTC (Universal Coordinated Time). 
    ///   Any rollover is handled by the application providing the data. Rollover occurs, for example, 
    ///   when the measurement device resets itself while measuring and the recording data do not include the data measured before the reset. 
    ///   The normalized data refer to the preceding time unit. 
    ///   The time series example shown in the figure below contains four time points: Time "a" indicates the beginning of the time 
    ///   series and the associated datum has no relevance. Data at time points "b," "c" and "d" are associated with values 1, 2 and 3, respectively.
    /// </summary>
    [IfcPersistedEntity, Serializable]
    public class IfcTimeSeriesValue : INotifyPropertyChanged, ISupportChangeNotification, IPersistIfcEntity,
                                      INotifyPropertyChanging
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

        public IfcTimeSeriesValue()
        {
            _listValues = new XbimList<object>(this);
        }

        #region Fields

        private XbimList<object> _listValues;

        #endregion

        /// <summary>
        ///   A list of time-series values. At least one value is required.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory, IfcAttributeType.List, IfcAttributeType.Class, 1)]
        public XbimList<object> ListValues
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _listValues;
            }
            set { this.SetModelValue(this, ref _listValues, value, v => ListValues = v, "ListValues"); }
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

        #region ISupportIfcParser Members

        public void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _listValues.Add_Reversible(value.EntityVal);
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
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