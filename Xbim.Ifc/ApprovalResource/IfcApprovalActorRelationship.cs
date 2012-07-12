#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcApprovalActorRelationship.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.ComponentModel;
using Xbim.Ifc.ActorResource;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc.ApprovalResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcApprovalActorRelationship : IPersistIfcEntity, ISupportChangeNotification, INotifyPropertyChanged,
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

        #region Fields

        private IfcActorSelect _actor;
        private IfcApproval _approval;
        private IfcActorRole _role;

        #endregion

        #region Properties

        /// <summary>
        ///   The reference to the actor who is acting in the given role on the approval specified in this relationship.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Mandatory)]
        public IfcActorSelect Actor
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _actor;
            }
            set { ModelManager.SetModelValue(this, ref _actor, value, v => Actor = v, "Actor"); }
        }

        /// <summary>
        ///   The approval on which the actor is acting in the role specified in this relationship.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Mandatory)]
        public IfcApproval Approval
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _approval;
            }
            set { ModelManager.SetModelValue(this, ref _approval, value, v => Approval = v, "Approval"); }
        }

        /// <summary>
        ///   The role of the actor w.r.t the approval. May be omitted, if the actor's general role implies also the role w.r.t the approval and does not need more detailed definition.
        /// </summary>
        [IfcAttribute(3, IfcAttributeState.Optional)]
        public IfcActorRole Role
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _role;
            }
            set { ModelManager.SetModelValue(this, ref _role, value, v => Role = v, "Role"); }
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

        public virtual void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _actor = (IfcActorSelect) value.EntityVal;
                    break;
                case 1:
                    _approval = (IfcApproval) value.EntityVal;
                    break;
                case 2:
                    _role = (IfcActorRole) value.EntityVal;
                    break;

                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }

        public string WhereRule()
        {
            return "";
        }

        #endregion
    }
}