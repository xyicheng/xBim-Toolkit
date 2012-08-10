using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.Ifc.MeasureResource;
using Xbim.XbimExtensions.Parser;
using System.ComponentModel;

namespace Xbim.Ifc.MaterialPropertyResource
{
    /// <summary>
    /// Measure of the decrease in stress over long time interval resulting from plastic flow. 
    /// It describes the time dependent relative relaxation value for a given initial stress level at constant strain
    /// </summary>
    [IfcPersistedEntity, Serializable]
    public class IfcRelaxation : IPersistIfcEntity, IPersistIfc, 
                                 INotifyPropertyChanged, ISupportChangeNotification, INotifyPropertyChanging
    {
        #region Fields

        private IfcNormalisedRatioMeasure _relaxationValue; 
        private IfcNormalisedRatioMeasure _initialStress;

        #endregion

        #region Properties

        /// <summary>
        ///  Time dependent loss of stress, relative to initial stress and therefore dimensionless.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcNormalisedRatioMeasure RelaxationValue
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity)this).Activate(false);
#endif
                return _relaxationValue;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _relaxationValue, value, v => RelaxationValue = v, "RelaxationValue");
            }
        }

        /// <summary>
        ///  Stress at the beginning. Given as relative to the yield stress of the material and is therefore dimensionless.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Mandatory)]
        public IfcNormalisedRatioMeasure InitialStress
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity)this).Activate(false);
#endif
                return _initialStress;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _initialStress, value, v => InitialStress = v, "InitialStress");
            }
        }

        #endregion

        public void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _relaxationValue = value.RealVal;
                    break;
                case 1:
                    _initialStress = value.RealVal;
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }

        public string WhereRule()
        {
            return ""; 
        }

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
        
    }
}
