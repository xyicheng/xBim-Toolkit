#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcSectionReinforcementProperties.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.ComponentModel;
using Xbim.Ifc.MeasureResource;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc.ProfilePropertyResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcSectionReinforcementProperties : INotifyPropertyChanged, ISupportChangeNotification,
                                                     IPersistIfcEntity, INotifyPropertyChanging
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

        public IfcSectionReinforcementProperties()
        {
            _crossSectionReinforcementDefinitions = new XbimSet<IfcReinforcementBarProperties>(this);
        }

        #region Fields

        private IfcLengthMeasure _longitudinalStartPosition;
        private IfcLengthMeasure _longitudinalEndPosition;
        private IfcLengthMeasure? _transversePosition;
        private IfcReinforcingBarRoleEnum _reinforcementRole;
        private IfcSectionProperties _crossSectionArea;
        private XbimSet<IfcReinforcementBarProperties> _crossSectionReinforcementDefinitions;

        #endregion

        #region Properties

        /// <summary>
        ///   The start position in longitudinal direction for the section reinforcement properties.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcLengthMeasure LongitudinalStartPosition
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _longitudinalStartPosition;
            }
            set
            {
                ModelHelper.SetModelValue(this, ref _longitudinalStartPosition, value,
                                           v => LongitudinalStartPosition = v, "LongitudinalStartPosition");
            }
        }

        /// <summary>
        ///   The end position in longitudinal direction for the section reinforcement properties.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Mandatory)]
        public IfcLengthMeasure LongitudinalEndPosition
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _longitudinalEndPosition;
            }
            set
            {
                ModelHelper.SetModelValue(this, ref _longitudinalEndPosition, value, v => LongitudinalEndPosition = v,
                                           "LongitudinalEndPosition");
            }
        }

        /// <summary>
        ///   The position for the section reinforcement properties in transverse direction.
        /// </summary>
        [IfcAttribute(3, IfcAttributeState.Optional)]
        public IfcLengthMeasure? TransversePosition
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _transversePosition;
            }
            set
            {
                ModelHelper.SetModelValue(this, ref _transversePosition, value, v => TransversePosition = v,
                                           "TransversePosition");
            }
        }

        /// <summary>
        ///   The role, purpose or usage of the reinforcement, i.e. the kind of loads and stresses it is intended to carry, defined for the section reinforcement properties.
        /// </summary>
        [IfcAttribute(4, IfcAttributeState.Mandatory)]
        public IfcReinforcingBarRoleEnum ReinforcementRole
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _reinforcementRole;
            }
            set
            {
                ModelHelper.SetModelValue(this, ref _reinforcementRole, value, v => ReinforcementRole = v,
                                           "ReinforcementRole");
            }
        }

        /// <summary>
        ///   Definition of the cross section profile and longitudinal section type.
        /// </summary>
        [IfcAttribute(5, IfcAttributeState.Mandatory)]
        public IfcSectionProperties CrossSectionArea
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _crossSectionArea;
            }
            set
            {
                ModelHelper.SetModelValue(this, ref _crossSectionArea, value, v => CrossSectionArea = v,
                                           "CrossSectionArea");
            }
        }

        [IfcAttribute(6, IfcAttributeState.Mandatory)]
        public XbimSet<IfcReinforcementBarProperties> CrossSectionReinforcementDefinitions
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _crossSectionReinforcementDefinitions;
            }
            set
            {
                ModelHelper.SetModelValue(this, ref _crossSectionReinforcementDefinitions, value,
                                           v => CrossSectionReinforcementDefinitions = v,
                                           "CrossSectionReinforcementDefinitions");
            }
        }

        #endregion

        public virtual void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _longitudinalStartPosition = value.RealVal;
                    break;
                case 1:
                    _longitudinalEndPosition = value.RealVal;
                    break;
                case 2:
                    _transversePosition = value.RealVal;
                    break;
                case 3:
                    _reinforcementRole =
                        (IfcReinforcingBarRoleEnum)
                        Enum.Parse(typeof (IfcReinforcingBarRoleEnum), value.StringVal, true);
                    break;
                case 4:
                    _crossSectionArea = (IfcSectionProperties) value.EntityVal;
                    break;
                case 5:
                    _crossSectionReinforcementDefinitions.Add((IfcReinforcementBarProperties) value.EntityVal);
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }

        public virtual string WhereRule()
        {
            return "";
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
    }
}