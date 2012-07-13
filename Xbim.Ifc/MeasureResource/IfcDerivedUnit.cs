﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcDerivedUnit.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.ComponentModel;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc.MeasureResource
{
    [IfcPersistedEntity, Serializable]
    public class UnitSet : XbimSet<IfcUnit>
    {
        internal UnitSet(IPersistIfcEntity owner)
            : base(owner)
        {
        }
    }

    [IfcPersistedEntity, Serializable]
    public class IfcDerivedUnit : INotifyPropertyChanged, ISupportChangeNotification, IPersistIfcEntity, IfcUnit,
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

        public IfcDerivedUnit()
        {
            _elements = new DerivedUnitElementSet(this);
        }

        #region Fields

        private readonly DerivedUnitElementSet _elements;
        private IfcDerivedUnitEnum _unitType;
        private IfcLabel? _userDefinedType;

        #endregion

        #region Overrides

        public override int GetHashCode()
        {
            return _unitType.GetHashCode();
        }

        #endregion

        #region Part 21 Step file Parse routines

        /// <summary>
        ///   The group of units and their exponents that define the derived unit.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class, 1)]
        public DerivedUnitElementSet Elements
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _elements;
            }
        }

        /// <summary>
        ///   Name of the derived unit chosen from an enumeration of derived unit types for use in IFC models.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Mandatory)]
        public IfcDerivedUnitEnum UnitType
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _unitType;
            }
            set
            {
                this.SetModelValue(this, ref _unitType, value, v => UnitType = v, "UnitType");
                if (value != IfcDerivedUnitEnum.USERDEFINED)
                    UserDefinedType = null;
            }
        }


        [IfcAttribute(3, IfcAttributeState.Optional)]
        public IfcLabel? UserDefinedType
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _userDefinedType;
            }
            set
            {
                this.SetModelValue(this, ref _userDefinedType, value, v => UserDefinedType = v,
                                           "UserDefinedType");
                UnitType = IfcDerivedUnitEnum.USERDEFINED;
            }
        }


        public virtual void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _elements.Add((IfcDerivedUnitElement) value.EntityVal);
                    break;
                case 1:
                    _unitType = (IfcDerivedUnitEnum) Enum.Parse(typeof (IfcDerivedUnitEnum), value.EnumVal);
                    break;
                case 2:
                    _userDefinedType = value.StringVal;
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }

        #endregion

        /// <summary>
        ///   Dimensional exponents derived using the function IfcDerivedDimensionalExponents using (SELF) as the input value.
        /// </summary>
        public IfcDimensionalExponents Dimensions
        {
            get { return IfcDimensionalExponents.DeriveDimensionalExponents(this); }
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

        public string WhereRule()
        {
            string err = "";
            if (_elements.Count > 1 || (_elements.Count == 1 && _elements.First.Exponent != 1))
            {
                if (
                    !(_unitType != IfcDerivedUnitEnum.USERDEFINED ||
                      (_unitType == IfcDerivedUnitEnum.USERDEFINED && _userDefinedType.HasValue)))

                    err +=
                        "WR2 DerivedUnit:   When attribute UnitType has enumeration value USERDEFINED then attribute UserDefinedType shall also have a value.";
            }
            else
                err += "WR1 DerivedUnit:   Units as such shall not be re-defined as derived units.";
            return err;
        }

        #endregion
    }
}