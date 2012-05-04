﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcPropertyEnumeration.cs
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

namespace Xbim.Ifc.PropertyResource
{
    /// <summary>
    ///   A collection of simple or measure values that define a prescribed set of alternatives from which 'enumeration values' are selected.
    /// </summary>
    /// <remarks>
    ///   Definition from IAI: A collection of simple or measure values that define a prescribed set of alternatives from which 'enumeration values' are selected. This enables inclusion of enumeration values in property sets. IfcPropertyEnumeration provides a name for the enumeration as well as a list of unique (numeric or descriptive) values (that may have a measure type assigned). The entity defines the list of potential enumerators to be exchanged together (or separately) with properties of type IfcPropertyEnumeratedValue that selects their actual property values from this enumeration. 
    ///   The unit is handled by the Unit attribute:
    ///   If the Unit attribute is not given, than the unit is already implied by the type of IfcMeasureValue or IfcDerivedMeasureValue. The associated unit can be found at the IfcUnitAssignment globally defined at the project level (IfcProject.UnitsInContext). 
    ///   If the Unit attribute is given, the unit assigned by the unit attribute overrides the globally assigned unit. 
    ///   Name EnumerationValues Type (through IfcValue) Unit 
    ///   PEnum_DamperBladeAction Parallel IfcString - 
    ///   Opposed IfcString   
    ///   Other IfcString   
    ///   Unset IfcString   
    ///   HISTORY: New Entity in IFC Release 2.0, capabilities enhanced in IFC Release 2x. Entity has been renamed from IfcEnumeration in IFC Release 2x.
    ///   Formal Propositions:
    ///   WR01   :   All values within the list of EnumerationValues shall be of the same measure type.
    /// </remarks>
    [IfcPersistedEntity, Serializable]
    public class IfcPropertyEnumeration : INotifyPropertyChanged, ISupportChangeNotification, IPersistIfcEntity,
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

        public IfcPropertyEnumeration()
        {
            _enumerationValues = new XbimList<IfcValue>(this);
        }

        #region Fields

        private IfcLabel _name;
        private XbimList<IfcValue> _enumerationValues;
        private IfcUnit _unit;

        #endregion

        /// <summary>
        ///   Name of this enumeration.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcLabel Name
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _name;
            }
            set { ModelManager.SetModelValue(this, ref _name, value, v => Name = v, "Name"); }
        }

        /// <summary>
        ///   List of values that form the enumeration.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Mandatory, IfcAttributeType.List, IfcAttributeType.Class, 1)]
        public XbimList<IfcValue> EnumerationValues
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _enumerationValues;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _enumerationValues, value, v => EnumerationValues = v,
                                           "EnumerationValues");
            }
        }

        /// <summary>
        ///   Optional. Unit for the enumerator values, if not given, the default value for the measure type (given by the TYPE of nominal value) is used as defined by the global unit assignment at IfcProject.
        /// </summary>
        [IfcAttribute(3, IfcAttributeState.Optional)]
        public IfcUnit Unit
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

        #region ISupportIfcParser Members

        public void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _name = value.StringVal;
                    break;
                case 1:
                    _enumerationValues.Add_Reversible((IfcValue) value.EntityVal);
                    break;
                case 2:
                    _unit = (IfcUnit) value.EntityVal;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(string.Format("P21 index value out of range in {0}",
                                                                        this.GetType().Name));
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
            bool first = true;
            Type type = null;
            foreach (IfcValue ev in _enumerationValues)
            {
                if (first)
                {
                    type = ev.GetType();
                    first = false;
                }
                else if (type != ev.GetType())
                    return
                        "WR1 PropertyEnumeration : All values within the list of EnumerationValues shall be of the same measure type.";
            }
            return "";
        }

        #endregion
    }
}