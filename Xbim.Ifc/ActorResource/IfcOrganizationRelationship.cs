#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcOrganizationRelationship.cs
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

namespace Xbim.Ifc.ActorResource
{
    [IfcPersistedEntity, Serializable]
    public class OrganizationRelationshipCollection : XbimList<IfcOrganizationRelationship>
    {
        internal OrganizationRelationshipCollection(IPersistIfcEntity owner)
            : base(owner)
        {
        }
    }

    [IfcPersistedEntity, Serializable]
    public class IfcOrganizationRelationship : ISupportChangeNotification, INotifyPropertyChanged, IPersistIfcEntity,
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


        public IfcOrganizationRelationship()
        {
            _relatingOrganizations = new OrganizationCollection(this);
        }

        #region Fields and Events

        private IfcLabel? _name;
        private IfcText? _description;
        private IfcOrganization _relatingOrganization;

        private OrganizationCollection _relatingOrganizations;

        #endregion

        #region Ifc Properties

        /// <summary>
        ///   The word or group of words by which the relationship is referred to.
        /// </summary>
        /// <value>it should be restricted to max. 255 characters</value>
        [IfcAttribute(1, IfcAttributeState.Optional)]
        public IfcLabel? Name
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
        ///   Optional. Text that relates the nature of the relationship.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Optional)]
        public IfcText? Description
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _description;
            }
            set { ModelManager.SetModelValue(this, ref _description, value, v => Description = v, "Description"); }
        }

        /// <summary>
        ///   Organization which is the relating part of the relationship between organizations.
        /// </summary>
        [IfcAttribute(3, IfcAttributeState.Mandatory)]
        public IfcOrganization RelatingOrganization
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _relatingOrganization;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _relatingOrganization, value, v => RelatingOrganization = v,
                                           "RelatingOrganization");
            }
        }

        /// <summary>
        ///   The other, possibly dependent, organizations which are the related parts of the relationship between organizations.
        /// </summary>
        [IfcAttribute(4, IfcAttributeState.Mandatory, IfcAttributeType.Set, 1)]
        public OrganizationCollection RelatedOrganizations
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _relatingOrganizations;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _relatingOrganizations, value, v => RelatedOrganizations = v,
                                           "RelatedOrganizations");
            }
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

        #region IPersistIfc Members

        public void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _name = value.StringVal;
                    break;
                case 1:
                    _description = value.StringVal;
                    break;
                case 2:
                    _relatingOrganization = value.EntityVal as IfcOrganization;
                    break;
                case 3:
                    _relatingOrganizations.Add_Reversible(value.EntityVal as IfcOrganization);
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