#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcCurrencyRelationship.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.ComponentModel;
using Xbim.Ifc.DateTimeResource;
using Xbim.Ifc.ExternalReferenceResource;
using Xbim.Ifc.MeasureResource;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc.CostResource
{
    /// <summary>
    ///   An IfcCurrencyRelationship defines the rate of exchange that applies between two designated currencies at a particular time and as published by a particular source.
    /// </summary>
    /// <remarks>
    ///   Definition from IAI:An IfcCurrencyRelationship defines the rate of exchange that applies between two designated currencies at a particular time and as published by a particular source. 
    ///   HISTORY: New Entity in IFC 2x2. 
    ///   Use Definitions
    ///   An IfcCurrencyRelationship is used where there may be a need to reference an IfcCostValue in one currency to an IfcCostValue in another currency. It takes account of fact that currency exchange rates may vary by requiring the recording the date and time of the currency exchange rate used and the source that publishes the rate. There may be many sources and there are different strategies for currency conversion (spot rate, forward buying of currency at a fixed rate). 
    ///   The source for the currency exchange is defined as an instance of IfcLibraryInformation that includes a name and a location (typically a URL, since most rates are now published in reliable sources via the web, although it may be a string value defining a lication of any type).
    /// </remarks>
    [IfcPersistedEntity, Serializable]
    public class IfcCurrencyRelationship : INotifyPropertyChanged, ISupportChangeNotification, IPersistIfcEntity,
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

        #region Fields

        private IfcMonetaryUnit _relatingMonetaryUnit;
        private IfcMonetaryUnit _relatedMonetaryUnit;
        private IfcPositiveRatioMeasure _exchangeRate;
        private IfcDateAndTime _rateDateTime;
        private IfcLibraryInformation _rateSource;

        #endregion

        /// <summary>
        ///   The monetary unit from which an exchange is derived. For instance, in the case of a conversion from GBP to USD, the relating monetary unit is GBP.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcMonetaryUnit RelatingMonetaryUnit
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _relatingMonetaryUnit;
            }
            set
            {
                ModelHelper.SetModelValue(this, ref _relatingMonetaryUnit, value, v => RelatingMonetaryUnit = v,
                                           "RelatingMonetaryUnit");
            }
        }

        /// <summary>
        ///   The monetary unit to which an exchange results. For instance, in the case of a conversion from GBP to USD, the related monetary unit is USD.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Mandatory)]
        public IfcMonetaryUnit RelatedMonetaryUnit
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _relatedMonetaryUnit;
            }
            set
            {
                ModelHelper.SetModelValue(this, ref _relatedMonetaryUnit, value, v => RelatedMonetaryUnit = v,
                                           "RelatedMonetaryUnit");
            }
        }

        /// <summary>
        ///   The currently agreed ratio of the amount of a related monetary unit that is equivalent to a unit amount of the relating monetary unit in a currency relationship. For instance, in the case of a conversion from GBP to USD, the value of the exchange rate may be 1.486 (USD) : 1 (GBP).
        /// </summary>
        [IfcAttribute(3, IfcAttributeState.Mandatory)]
        public IfcPositiveRatioMeasure ExchangeRate
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _exchangeRate;
            }
            set { ModelHelper.SetModelValue(this, ref _exchangeRate, value, v => ExchangeRate = v, "ExchangeRate"); }
        }

        /// <summary>
        ///   The date and time at which an exchange rate applies.
        /// </summary>
        [IfcAttribute(4, IfcAttributeState.Mandatory)]
        public IfcDateAndTime RateDateTime
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _rateDateTime;
            }
            set { ModelHelper.SetModelValue(this, ref _rateDateTime, value, v => RateDateTime = v, "RateDateTime"); }
        }

        /// <summary>
        ///   The source from which an exchange rate is obtained.
        /// </summary>
        [IfcAttribute(5, IfcAttributeState.Optional)]
        public IfcLibraryInformation RateSource
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _rateSource;
            }
            set { ModelHelper.SetModelValue(this, ref _rateSource, value, v => RateSource = v, "RateSource"); }
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
                    _relatingMonetaryUnit = (IfcMonetaryUnit) value.EntityVal;
                    break;
                case 1:
                    _relatedMonetaryUnit = (IfcMonetaryUnit) value.EntityVal;
                    break;
                case 2:
                    _exchangeRate = value.RealVal;
                    break;
                case 3:
                    _rateDateTime = (IfcDateAndTime) value.EntityVal;
                    break;
                case 4:
                    _rateSource = (IfcLibraryInformation) value.EntityVal;
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