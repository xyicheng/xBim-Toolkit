#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcDerivedUnitElement.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.ComponentModel;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.MeasureResource
{
    public class DerivedUnitElementSet : XbimSet<IfcDerivedUnitElement>
    {
        internal DerivedUnitElementSet(IPersistIfcEntity owner)
            : base(owner)
        {
        }
    }

    /// <summary>
    ///   A derived unit element is one of the unit quantities which makes up a derived unit. 
    ///   EXAMPLE: Newtons per square millimetre is a derived unit. It has two elements, Newton whose exponent has a value of 1 and millimetre whose exponent is -2. 
    ///   NOTE Corresponding STEP name: derived_unit_element, please refer to ISO/IS 10303-41:1994 for the final definition of the formal standard.
    /// </summary>
    [IfcPersistedEntity, Serializable]
    public class IfcDerivedUnitElement : INotifyPropertyChanged, ISupportChangeNotification, IPersistIfcEntity,
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

        private IfcNamedUnit _unit;
        private int _exponent;

        #region Part 21 Step file Parse routines

        /// <summary>
        ///   The fixed quantity which is used as the mathematical factor.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcNamedUnit Unit
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _unit;
            }
            set { ModelManager.SetModelValue(this, ref _unit, value, v => Unit = v, "Unit"); }
        }

        /// <summary>
        ///   The power that is applied to the unit attribute.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Mandatory)]
        public int Exponent
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _exponent;
            }
            set { ModelManager.SetModelValue(this, ref _exponent, value, v => Exponent = v, "Exponent"); }
        }


        public void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _unit = (IfcNamedUnit) value.EntityVal;
                    break;
                case 1:
                    _exponent = (int) value.IntegerVal;
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
    }
}