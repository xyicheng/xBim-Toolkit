#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcOwnerHistory.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Xbim.Ifc.ActorResource;
using Xbim.Ifc.MeasureResource;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.UtilityResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcOwnerHistory : IPersistIfcEntity, ISupportChangeNotification, INotifyPropertyChanged,
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

        #region Fields and Events

        private IfcPersonAndOrganization _owningUser;
        private IfcApplication _owningApplication;
        private IfcStateEnum? _state;
        private IfcChangeActionEnum _changeAction = IfcChangeActionEnum.NOCHANGE;
        private IfcTimeStamp? _lastModifiedDate;
        private IfcPersonAndOrganization _lastModifyingUser;
        private IfcApplication _lastModifyingApplication;
        private IfcTimeStamp _creationDate;

        #endregion

        #region Part 21 Step file Parse routines

        /// <summary>
        ///   Direct reference to the end user who currently "owns" this object. Note that IFC includes the concept of ownership transfer from one user to another and therefore distinguishes between the Owning User and Creating User.
        /// </summary>
        [DataMember(Order = 0)]
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcPersonAndOrganization OwningUser
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _owningUser;
            }
            set { ModelManager.SetModelValue(this, ref _owningUser, value, v => OwningUser = v, "OwningUser"); }
        }

        /// <summary>
        ///   Direct reference to the application which currently "Owns" this object on behalf of the owning user, who uses this application. Note that IFC includes the concept of ownership transfer from one app to another and therefore distinguishes between the Owning Application and Creating Application.
        /// </summary>
        [DataMember(Order = 1)]
        [IfcAttribute(2, IfcAttributeState.Mandatory)]
        public IfcApplication OwningApplication
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _owningApplication;
            }

            set
            {
                ModelManager.SetModelValue(this, ref _owningApplication, value, v => OwningApplication = v,
                                           "OwningApplication");
            }
        }

        /// <summary>
        ///   Enumeration that defines the current access state of the object.
        /// </summary>
        [DataMember(Order = 2, IsRequired = false, EmitDefaultValue = false)]
        [IfcAttribute(3, IfcAttributeState.Optional)]
        public IfcStateEnum? State
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _state;
            }
            set { ModelManager.SetModelValue(this, ref _state, value, v => State = v, "State"); }
        }

        /// <summary>
        ///   Enumeration that defines the actions associated with changes made to the object.
        /// </summary>
        [DataMember(Order = 3)]
        [IfcAttribute(4, IfcAttributeState.Mandatory)]
        public IfcChangeActionEnum ChangeAction
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _changeAction;
            }
            set { ModelManager.SetModelValue(this, ref _changeAction, value, v => ChangeAction = v, "ChangeAction"); }
        }

        /// <summary>
        ///   Date and Time at which the last modification occurred. This is an optional value and will return null if not defined
        /// </summary>
        [DataMember(Order = 4, IsRequired = false, EmitDefaultValue = false)]
        [IfcAttribute(5, IfcAttributeState.Optional)]
        public IfcTimeStamp? LastModifiedDate
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _lastModifiedDate;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _lastModifiedDate, value, v => LastModifiedDate = v,
                                           "LastModifiedDate");
            }
        }

        /// <summary>
        ///   User who carried out the last modification.
        /// </summary>
        [DataMember(Order = 5, IsRequired = false, EmitDefaultValue = false)]
        [IfcAttribute(6, IfcAttributeState.Optional)]
        public IfcPersonAndOrganization LastModifyingUser
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _lastModifyingUser;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _lastModifyingUser, value, v => LastModifyingUser = v,
                                           "LastModifyingUser");
            }
        }

        /// <summary>
        ///   Application used to carry out the last modification.
        /// </summary>
        [DataMember(Order = 6, IsRequired = false, EmitDefaultValue = false)]
        [IfcAttribute(7, IfcAttributeState.Optional)]
        public IfcApplication LastModifyingApplication
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _lastModifyingApplication;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _lastModifyingApplication, value, v => LastModifyingApplication = v,
                                           "LastModifyingApplication");
            }
        }

        /// <summary>
        ///   Time and date of creation. This is an optional value and will return null if not defined
        /// </summary>
        [DataMember(Order = 7)]
        [IfcAttribute(8, IfcAttributeState.Mandatory)]
        public IfcTimeStamp CreationDate
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _creationDate;
            }
            set { ModelManager.SetModelValue(this, ref _creationDate, value, v => CreationDate = v, "CreationDate"); }
        }

        public virtual void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _owningUser = (IfcPersonAndOrganization) value.EntityVal;
                    break;
                case 1:
                    _owningApplication = (IfcApplication) value.EntityVal;
                    break;
                case 2:
                    _state = (IfcStateEnum?) Enum.Parse(typeof (IfcStateEnum), value.EnumVal, true);
                    break;
                case 3:
                    _changeAction = (IfcChangeActionEnum) Enum.Parse(typeof (IfcChangeActionEnum), value.EnumVal, true);
                    break;
                case 4:
                    _lastModifiedDate = value.IntegerVal;
                    break;
                case 5:
                    _lastModifyingUser = (IfcPersonAndOrganization) value.EntityVal;
                    break;
                case 6:
                    _lastModifyingApplication = (IfcApplication) value.EntityVal;
                    break;
                case 7:
                    _creationDate = value.IntegerVal;
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        ///   Creates an Ifc schema compliant OwnerHistory, Creation Date default to Now, changeAction to ADDED
        /// </summary>
        public IfcOwnerHistory()
        {
            _creationDate = IfcTimeStamp.ToTimeStamp(DateTime.UtcNow);
        }

        #endregion

        #region Properties

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