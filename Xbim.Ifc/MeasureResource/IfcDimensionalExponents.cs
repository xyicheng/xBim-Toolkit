#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcDimensionalExponents.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc.MeasureResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcDimensionalExponents : IPersistIfcEntity, ISupportChangeNotification, INotifyPropertyChanged,
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

        private readonly int[] _exponents;

        #region Part 21 Step file Parse routines

        /// <summary>
        ///   The power of the length base quantity.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public int LengthExponent
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return this[0];
            }
            set { ModelManager.SetModelValue(this, ref _exponents[0], value, v => LengthExponent = v, "LengthExponent"); }
        }

        /// <summary>
        ///   The power of the mass base quantity.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Mandatory)]
        public int MassExponent
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return this[1];
            }
            set { ModelManager.SetModelValue(this, ref _exponents[1], value, v => MassExponent = v, "MassExponent"); }
        }

        /// <summary>
        ///   The power of the time base quantity.
        /// </summary>
        [IfcAttribute(3, IfcAttributeState.Mandatory)]
        public int TimeExponent
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return this[2];
            }
            set { ModelManager.SetModelValue(this, ref _exponents[2], value, v => TimeExponent = v, "TimeExponent"); }
        }

        /// <summary>
        ///   The power of the electric current base quantity.
        /// </summary>
        [IfcAttribute(4, IfcAttributeState.Mandatory)]
        public int ElectricCurrentExponent
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return this[3];
            }
            set
            {
                ModelManager.SetModelValue(this, ref _exponents[3], value, v => ElectricCurrentExponent = v,
                                           "ElectricCurrentExponent");
            }
        }

        /// <summary>
        ///   The power of the thermodynamic temperature base quantity.
        /// </summary>
        [IfcAttribute(5, IfcAttributeState.Mandatory)]
        public int ThermodynamicTemperatureExponent
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return this[4];
            }
            set
            {
                ModelManager.SetModelValue(this, ref _exponents[4], value, v => ThermodynamicTemperatureExponent = v,
                                           "ThermodynamicTemperatureExponent");
            }
        }

        /// <summary>
        ///   The power of the amount of substance base quantity.
        /// </summary>
        [IfcAttribute(6, IfcAttributeState.Mandatory)]
        public int AmountOfSubstanceExponent
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return this[5];
            }
            set
            {
                ModelManager.SetModelValue(this, ref _exponents[5], value, v => AmountOfSubstanceExponent = v,
                                           "AmountOfSubstanceExponent");
            }
        }

        /// <summary>
        ///   The power of the luminous intensity base quantity.
        /// </summary>
        [IfcAttribute(7, IfcAttributeState.Mandatory)]
        public int LuminousIntensityExponent
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return this[6];
            }
            set
            {
                ModelManager.SetModelValue(this, ref _exponents[6], value, v => LuminousIntensityExponent = v,
                                           "LuminousIntensityExponent");
            }
        }


        public virtual void IfcParse(int propIndex, IPropertyValue value)
        {
            if (propIndex >= 0 && propIndex < 7)
                _exponents[propIndex] = (int) value.IntegerVal;
            else
                this.HandleUnexpectedAttribute(propIndex, value);
        }

        #endregion

        public IfcDimensionalExponents()
            : this(0, 0, 0, 0, 0, 0, 0)
        {
        }

        public IfcDimensionalExponents(int length, int mass, int time, int elec, int temp, int substs, int lumin)
        {
            _exponents = new int[7];
            _exponents[0] = length;
            _exponents[1] = mass;
            _exponents[2] = time;
            _exponents[3] = elec;
            _exponents[4] = temp;
            _exponents[5] = substs;
            _exponents[6] = lumin;
        }

        public int this[int index]
        {
            get { return _exponents == null ? 0 : _exponents[index]; }
        }

        public override bool Equals(object obj)
        {
            if (obj == null)
                return false;

            if (GetType() != obj.GetType())
                return false;
            for (int i = 0; i < 7; i++)
            {
                if (this[i] != ((IfcDimensionalExponents) obj)[i]) return false;
            }
            return true;
        }

        public static bool operator ==(IfcDimensionalExponents dim1, IfcDimensionalExponents dim2)
        {
            return Equals(dim1, dim2);
        }

        public static bool operator !=(IfcDimensionalExponents dim1, IfcDimensionalExponents dim2)
        {
            return !Equals(dim1, dim2);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static IfcDimensionalExponents DimensionsForSiUnit(IfcSIUnitName? siUnit)
        {
            if (siUnit == null) return null;
            switch (siUnit)
            {
                case IfcSIUnitName.METRE:
                    return new IfcDimensionalExponents(1, 0, 0, 0, 0, 0, 0);
                case IfcSIUnitName.SQUARE_METRE:
                    return new IfcDimensionalExponents(2, 0, 0, 0, 0, 0, 0);
                case IfcSIUnitName.CUBIC_METRE:
                    return new IfcDimensionalExponents(3, 0, 0, 0, 0, 0, 0);
                case IfcSIUnitName.GRAM:
                    return new IfcDimensionalExponents(0, 1, 0, 0, 0, 0, 0);
                case IfcSIUnitName.SECOND:
                    return new IfcDimensionalExponents(0, 0, 1, 0, 0, 0, 0);
                case IfcSIUnitName.AMPERE:
                    return new IfcDimensionalExponents(0, 0, 0, 1, 0, 0, 0);
                case IfcSIUnitName.KELVIN:
                    return new IfcDimensionalExponents(0, 0, 0, 0, 1, 0, 0);
                case IfcSIUnitName.MOLE:
                    return new IfcDimensionalExponents(0, 0, 0, 0, 0, 1, 0);
                case IfcSIUnitName.CANDELA:
                    return new IfcDimensionalExponents(0, 0, 0, 0, 0, 0, 1);
                case IfcSIUnitName.RADIAN:
                    return new IfcDimensionalExponents(0, 0, 0, 0, 0, 0, 0);
                case IfcSIUnitName.STERADIAN:
                    return new IfcDimensionalExponents(0, 0, 0, 0, 0, 0, 0);
                case IfcSIUnitName.HERTZ:
                    return new IfcDimensionalExponents(0, 0, -1, 0, 0, 0, 0);
                case IfcSIUnitName.NEWTON:
                    return new IfcDimensionalExponents(1, 1, -2, 0, 0, 0, 0);
                case IfcSIUnitName.PASCAL:
                    return new IfcDimensionalExponents(-1, 1, -2, 0, 0, 0, 0);
                case IfcSIUnitName.JOULE:
                    return new IfcDimensionalExponents(2, 1, -2, 0, 0, 0, 0);
                case IfcSIUnitName.WATT:
                    return new IfcDimensionalExponents(2, 1, -3, 0, 0, 0, 0);
                case IfcSIUnitName.COULOMB:
                    return new IfcDimensionalExponents(0, 0, 1, 1, 0, 0, 0);
                case IfcSIUnitName.VOLT:
                    return new IfcDimensionalExponents(2, 1, -3, -1, 0, 0, 0);
                case IfcSIUnitName.FARAD:
                    return new IfcDimensionalExponents(-2, -1, 4, 1, 0, 0, 0);
                case IfcSIUnitName.OHM:
                    return new IfcDimensionalExponents(2, 1, -3, -2, 0, 0, 0);
                case IfcSIUnitName.SIEMENS:
                    return new IfcDimensionalExponents(-2, -1, 3, 2, 0, 0, 0);
                case IfcSIUnitName.WEBER:
                    return new IfcDimensionalExponents(2, 1, -2, -1, 0, 0, 0);
                case IfcSIUnitName.TESLA:
                    return new IfcDimensionalExponents(0, 1, -2, -1, 0, 0, 0);
                case IfcSIUnitName.HENRY:
                    return new IfcDimensionalExponents(2, 1, -2, -2, 0, 0, 0);
                case IfcSIUnitName.DEGREE_CELSIUS:
                    return new IfcDimensionalExponents(0, 0, 0, 0, 1, 0, 0);
                case IfcSIUnitName.LUMEN:
                    return new IfcDimensionalExponents(0, 0, 0, 0, 0, 0, 1);
                case IfcSIUnitName.LUX:
                    return new IfcDimensionalExponents(-2, 0, 0, 0, 0, 0, 1);
                case IfcSIUnitName.BECQUEREL:
                    return new IfcDimensionalExponents(0, 0, -1, 0, 0, 0, 0);
                case IfcSIUnitName.GRAY:
                    return new IfcDimensionalExponents(2, 0, -2, 0, 0, 0, 0);
                case IfcSIUnitName.SIEVERT:
                    return new IfcDimensionalExponents(2, 0, -2, 0, 0, 0, 0);
                default:
                    return new IfcDimensionalExponents(0, 0, 0, 0, 0, 0, 0);
            }
        }

        public static bool CorrectDimensions(IfcUnitEnum unit, IfcDimensionalExponents Dim)
        {
            switch (unit)
            {
                case IfcUnitEnum.LENGTHUNIT:
                    return (Dim == new IfcDimensionalExponents(1, 0, 0, 0, 0, 0, 0));
                case IfcUnitEnum.MASSUNIT:
                    return (Dim == new IfcDimensionalExponents(0, 1, 0, 0, 0, 0, 0));
                case IfcUnitEnum.TIMEUNIT:
                    return (Dim == new IfcDimensionalExponents(0, 0, 1, 0, 0, 0, 0));
                case IfcUnitEnum.ELECTRICCURRENTUNIT:
                    return (Dim == new IfcDimensionalExponents(0, 0, 0, 1, 0, 0, 0));
                case IfcUnitEnum.THERMODYNAMICTEMPERATUREUNIT:
                    return (Dim == new IfcDimensionalExponents(0, 0, 0, 0, 1, 0, 0));
                case IfcUnitEnum.AMOUNTOFSUBSTANCEUNIT:
                    return (Dim == new IfcDimensionalExponents(0, 0, 0, 0, 0, 1, 0));
                case IfcUnitEnum.LUMINOUSINTENSITYUNIT:
                    return (Dim == new IfcDimensionalExponents(0, 0, 0, 0, 0, 0, 1));
                case IfcUnitEnum.PLANEANGLEUNIT:
                    return (Dim == new IfcDimensionalExponents(0, 0, 0, 0, 0, 0, 0));
                case IfcUnitEnum.SOLIDANGLEUNIT:
                    return (Dim == new IfcDimensionalExponents(0, 0, 0, 0, 0, 0, 0));
                case IfcUnitEnum.AREAUNIT:
                    return (Dim == new IfcDimensionalExponents(2, 0, 0, 0, 0, 0, 0));
                case IfcUnitEnum.VOLUMEUNIT:
                    return (Dim == new IfcDimensionalExponents(3, 0, 0, 0, 0, 0, 0));
                case IfcUnitEnum.ABSORBEDDOSEUNIT:
                    return (Dim == new IfcDimensionalExponents(2, 0, -2, 0, 0, 0, 0));
                case IfcUnitEnum.RADIOACTIVITYUNIT:
                    return (Dim == new IfcDimensionalExponents(0, 0, -1, 0, 0, 0, 0));
                case IfcUnitEnum.ELECTRICCAPACITANCEUNIT:
                    return (Dim == new IfcDimensionalExponents(-2, 1, 4, 1, 0, 0, 0));
                case IfcUnitEnum.DOSEEQUIVALENTUNIT:
                    return (Dim == new IfcDimensionalExponents(2, 0, -2, 0, 0, 0, 0));
                case IfcUnitEnum.ELECTRICCHARGEUNIT:
                    return (Dim == new IfcDimensionalExponents(0, 0, 1, 1, 0, 0, 0));
                case IfcUnitEnum.ELECTRICCONDUCTANCEUNIT:
                    return (Dim == new IfcDimensionalExponents(-2, -1, 3, 2, 0, 0, 0));
                case IfcUnitEnum.ELECTRICVOLTAGEUNIT:
                    return (Dim == new IfcDimensionalExponents(2, 1, -3, -1, 0, 0, 0));
                case IfcUnitEnum.ELECTRICRESISTANCEUNIT:
                    return (Dim == new IfcDimensionalExponents(2, 1, -3, -2, 0, 0, 0));
                case IfcUnitEnum.ENERGYUNIT:
                    return (Dim == new IfcDimensionalExponents(2, 1, -2, 0, 0, 0, 0));
                case IfcUnitEnum.FORCEUNIT:
                    return (Dim == new IfcDimensionalExponents(1, 1, -2, 0, 0, 0, 0));
                case IfcUnitEnum.FREQUENCYUNIT:
                    return (Dim == new IfcDimensionalExponents(0, 0, -1, 0, 0, 0, 0));
                case IfcUnitEnum.INDUCTANCEUNIT:
                    return (Dim == new IfcDimensionalExponents(2, 1, -2, -2, 0, 0, 0));
                case IfcUnitEnum.ILLUMINANCEUNIT:
                    return (Dim == new IfcDimensionalExponents(-2, 0, 0, 0, 0, 0, 1));
                case IfcUnitEnum.LUMINOUSFLUXUNIT:
                    return (Dim == new IfcDimensionalExponents(0, 0, 0, 0, 0, 0, 1));
                case IfcUnitEnum.MAGNETICFLUXUNIT:
                    return (Dim == new IfcDimensionalExponents(2, 1, -2, -1, 0, 0, 0));
                case IfcUnitEnum.MAGNETICFLUXDENSITYUNIT:
                    return (Dim == new IfcDimensionalExponents(0, 1, -2, -1, 0, 0, 0));
                case IfcUnitEnum.POWERUNIT:
                    return (Dim == new IfcDimensionalExponents(2, 1, -3, 0, 0, 0, 0));
                case IfcUnitEnum.PRESSUREUNIT:
                    return (Dim == new IfcDimensionalExponents(-1, 1, -2, 0, 0, 0, 0));
                default:
                    return false;
            }
        }

        public static IfcDimensionalExponents DimensionsForUnit(IfcUnitEnum unit, IfcDimensionalExponents Dim)
        {
            switch (unit)
            {
                case IfcUnitEnum.LENGTHUNIT:
                    return new IfcDimensionalExponents(1, 0, 0, 0, 0, 0, 0);
                case IfcUnitEnum.MASSUNIT:
                    return new IfcDimensionalExponents(0, 1, 0, 0, 0, 0, 0);
                case IfcUnitEnum.TIMEUNIT:
                    return new IfcDimensionalExponents(0, 0, 1, 0, 0, 0, 0);
                case IfcUnitEnum.ELECTRICCURRENTUNIT:
                    return new IfcDimensionalExponents(0, 0, 0, 1, 0, 0, 0);
                case IfcUnitEnum.THERMODYNAMICTEMPERATUREUNIT:
                    return new IfcDimensionalExponents(0, 0, 0, 0, 1, 0, 0);
                case IfcUnitEnum.AMOUNTOFSUBSTANCEUNIT:
                    return new IfcDimensionalExponents(0, 0, 0, 0, 0, 1, 0);
                case IfcUnitEnum.LUMINOUSINTENSITYUNIT:
                    return new IfcDimensionalExponents(0, 0, 0, 0, 0, 0, 1);
                case IfcUnitEnum.PLANEANGLEUNIT:
                    return new IfcDimensionalExponents(0, 0, 0, 0, 0, 0, 0);
                case IfcUnitEnum.SOLIDANGLEUNIT:
                    return new IfcDimensionalExponents(0, 0, 0, 0, 0, 0, 0);
                case IfcUnitEnum.AREAUNIT:
                    return new IfcDimensionalExponents(2, 0, 0, 0, 0, 0, 0);
                case IfcUnitEnum.VOLUMEUNIT:
                    return new IfcDimensionalExponents(3, 0, 0, 0, 0, 0, 0);
                case IfcUnitEnum.ABSORBEDDOSEUNIT:
                    return new IfcDimensionalExponents(2, 0, -2, 0, 0, 0, 0);
                case IfcUnitEnum.RADIOACTIVITYUNIT:
                    return new IfcDimensionalExponents(0, 0, -1, 0, 0, 0, 0);
                case IfcUnitEnum.ELECTRICCAPACITANCEUNIT:
                    return new IfcDimensionalExponents(-2, 1, 4, 1, 0, 0, 0);
                case IfcUnitEnum.DOSEEQUIVALENTUNIT:
                    return new IfcDimensionalExponents(2, 0, -2, 0, 0, 0, 0);
                case IfcUnitEnum.ELECTRICCHARGEUNIT:
                    return new IfcDimensionalExponents(0, 0, 1, 1, 0, 0, 0);
                case IfcUnitEnum.ELECTRICCONDUCTANCEUNIT:
                    return new IfcDimensionalExponents(-2, -1, 3, 2, 0, 0, 0);
                case IfcUnitEnum.ELECTRICVOLTAGEUNIT:
                    return new IfcDimensionalExponents(2, 1, -3, -1, 0, 0, 0);
                case IfcUnitEnum.ELECTRICRESISTANCEUNIT:
                    return new IfcDimensionalExponents(2, 1, -3, -2, 0, 0, 0);
                case IfcUnitEnum.ENERGYUNIT:
                    return new IfcDimensionalExponents(2, 1, -2, 0, 0, 0, 0);
                case IfcUnitEnum.FORCEUNIT:
                    return new IfcDimensionalExponents(1, 1, -2, 0, 0, 0, 0);
                case IfcUnitEnum.FREQUENCYUNIT:
                    return new IfcDimensionalExponents(0, 0, -1, 0, 0, 0, 0);
                case IfcUnitEnum.INDUCTANCEUNIT:
                    return new IfcDimensionalExponents(2, 1, -2, -2, 0, 0, 0);
                case IfcUnitEnum.ILLUMINANCEUNIT:
                    return new IfcDimensionalExponents(-2, 0, 0, 0, 0, 0, 1);
                case IfcUnitEnum.LUMINOUSFLUXUNIT:
                    return new IfcDimensionalExponents(0, 0, 0, 0, 0, 0, 1);
                case IfcUnitEnum.MAGNETICFLUXUNIT:
                    return new IfcDimensionalExponents(2, 1, -2, -1, 0, 0, 0);
                case IfcUnitEnum.MAGNETICFLUXDENSITYUNIT:
                    return new IfcDimensionalExponents(0, 1, -2, -1, 0, 0, 0);
                case IfcUnitEnum.POWERUNIT:
                    return new IfcDimensionalExponents(2, 1, -3, 0, 0, 0, 0);
                case IfcUnitEnum.PRESSUREUNIT:
                    return new IfcDimensionalExponents(-1, 1, -2, 0, 0, 0, 0);
                default:
                    return new IfcDimensionalExponents(0, 0, 0, 0, 0, 0, 0);
            }
        }

        public static IfcDimensionalExponents DeriveDimensionalExponents(IfcUnit unit)
        {
            throw new NotImplementedException();
        }

        public static IfcDimensionalExponents CorrectUnitAssignment(List<IfcUnit> units)
        {
            throw new NotImplementedException();
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