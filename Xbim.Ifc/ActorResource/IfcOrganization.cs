#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcOrganization.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Text;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc.ActorResource
{
    [IfcPersistedEntity, Serializable]
    public class OrganizationCollection : XbimList<IfcOrganization>
    {
        internal OrganizationCollection(IPersistIfcEntity owner)
            : base(owner)
        {
        }

        /// <summary>
        ///   Finds the organization with either the ID of id or if the ID is null the Name == id. Only returns the first match or null if none found
        /// </summary>
        /// <param name = "id"></param>
        /// <returns></returns>
        public IfcOrganization this[IfcIdentifier id]
        {
            get
            {
                foreach (IfcOrganization o in this)
                {
                    if (string.IsNullOrEmpty(o.Id.GetValueOrDefault()) && o.Name == id)
                        return o;
                    else if (o.Id == id) return o;
                }
                return null;
            }
        }
    }


    [IfcPersistedEntity, Serializable]
    public class IfcOrganization : IfcActorSelect, IPersistIfcEntity, IFormattable, ISupportChangeNotification,
                                   INotifyPropertyChanged, IfcObjectReferenceSelect, INotifyPropertyChanging
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


        #region Fields and Events

        //persistent fields
        private IfcIdentifier? _id;
        private IfcLabel _name;
        private IfcText? _description;
        private ActorRoleCollection _roles;
        private AddressCollection _addresses;

        #endregion

        #region Properties

        #endregion

        #region Ifc Properties

        /// <summary>
        ///   Optional. Identification of the organization.
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Optional), Browsable(true)]
        public IfcIdentifier? Id
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _id;
            }
            set { this.SetModelValue(this, ref _id, value, v => Id = v, "Id"); }
        }

        /// <summary>
        ///   The word, or group of words, by which the organization is referred to.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Mandatory), Browsable(true)]
        public IfcLabel Name
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _name;
            }
            set { this.SetModelValue(this, ref _name, value, v => Name = v, "Name"); }
        }

        /// <summary>
        ///   Optional.   Text that relates the nature of the organization.
        /// </summary>
        [IfcAttribute(3, IfcAttributeState.Optional), Browsable(true)]
        public IfcText? Description
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _description;
            }
            set { this.SetModelValue(this, ref _description, value, v => Description = v, "Description"); }
        }


        /// <summary>
        ///   Optional.   Roles played by the organization.
        /// </summary>
        [IfcAttribute(4, IfcAttributeState.Optional, IfcAttributeType.List, 1), Browsable(true)]
        public ActorRoleCollection Roles
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _roles;
            }
            set { this.SetModelValue(this, ref _roles, value, v => Roles = v, "Roles"); }
        }

        /// <summary>
        ///   Optional.   Postal and telecom addresses of an organization
        /// </summary>
        [IfcAttribute(5, IfcAttributeState.Optional, IfcAttributeType.List, 1), Browsable(true)]
        public AddressCollection Addresses
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _addresses;
            }
            set { this.SetModelValue(this, ref _addresses, value, v => Addresses = v, "Addresses"); }
        }

        #endregion

        #region Part 21 Step file Parse routines

        public virtual void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _id = value.StringVal;
                    break;
                case 1:
                    _name = value.StringVal;
                    break;
                case 2:
                    _description = value.StringVal;
                    break;
                case 3:
                    if (_roles == null) _roles = new ActorRoleCollection(this);
                    _roles.Add_Reversible((IfcActorRole) value.EntityVal);
                    break;
                case 4:
                    if (_addresses == null) _addresses = new AddressCollection(this);
                    _addresses.Add_Reversible((IfcAddress) value.EntityVal);
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }

        #endregion

        #region Ifc Inverse Relationships

        /// <summary>
        ///   The inverse relationship for relationship RelatedOrganizations of IfcOrganizationRelationship.
        /// </summary>
        [IfcAttribute(-1, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class)]
        public IEnumerable<IfcOrganizationRelationship> IsRelatedBy
        {
            get
            {
                return
                    ModelOf.InstancesWhere<IfcOrganizationRelationship>(
                        r => r.RelatedOrganizations.Contains(this));
            }
        }

        /// <summary>
        ///   The inverse relationship for relationship RelatingOrganization of IfcOrganizationRelationship.
        /// </summary>
        [IfcAttribute(-1, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class)]
        public IEnumerable<IfcOrganizationRelationship> Relates
        {
            get
            {
                return
                    ModelOf.InstancesWhere<IfcOrganizationRelationship>(
                        r => r.RelatingOrganization == this);
            }
        }

        /// <summary>
        ///   Inverse relationship to IfcPersonAndOrganization relationships in which IfcOrganization is engaged.
        /// </summary>
        [IfcAttribute(-1, IfcAttributeState.Mandatory, IfcAttributeType.Set, IfcAttributeType.Class)]
        public IEnumerable<IfcPersonAndOrganization> Engages
        {
            get
            {
                return
                    ModelOf.InstancesWhere<IfcPersonAndOrganization>(r => r.TheOrganization == this);
            }
        }

        #endregion

        #region Ifc Schema Validation Methods

        //none defined

        #endregion

        #region Properties

        //[Browsable(true)]
        //public string RolesString
        //{
        //    get 
        //    {
        //        return _roles == null ? null : _roles.ToString();
        //    }
        //    set
        //    {
        //        using (Transaction txn = BeginTransaction(string.Format("Roles = {0}", value), true))
        //        {
        //            if (string.IsNullOrEmpty(value))
        //            {
        //                Transaction.AddPropertyChange(v => _roles = v, _roles, null);
        //                _roles = null;
        //            }
        //            else
        //            {
        //                string[] roles = value.Split(new string[] { "; ", ";" }, StringSplitOptions.RemoveEmptyEntries);
        //                if (string.Join("; ", roles) == RolesString) //no real change so exit transaction
        //                    return;
        //                if (_roles == null)
        //                {
        //                    ActorRoleCollection c = new ActorRoleCollection();
        //                    Transaction.AddPropertyChange(v => _roles = v, null, c);
        //                    _roles = c;
        //                }
        //                else
        //                    _roles.Clear_Reversible();

        //                if (roles != null)
        //                {
        //                    foreach (string item in roles)
        //                    {
        //                        ActorRole aRole = new ActorRole( item);
        //                        aRole.OnModelAdd(Model);
        //                        _roles.Add_Reversible(aRole);
        //                    }
        //                }
        //            }
        //            Transaction.AddTransactionReversedHandler(() => { NotifyPropertyChanged("Roles"); NotifyPropertyChanged("RolesString"); });
        //            if (txn != null) txn.Commit();
        //            NotifyPropertyChanged("Roles");
        //            NotifyPropertyChanged("RolesString");
        //        }
        //    }
        //}

        /// <summary>
        ///   A string representation of the Addresses Collection
        /// </summary>
        [Browsable(true)]
        public string AddressesString
        {
            get { return _addresses == null ? null : _addresses.ToString(); }
        }

        #endregion

        #region Add methods

        /////// <summary>
        /////// Creates a new Engagement with the capacity defined in roles
        /////// </summary>
        /////// <param name="engagedPerson"></param>
        /////// <param name="actorRoles"></param>
        /////// <returns></returns>
        ////public PersonAndOrganization AddEngagement(Person engagedPerson, params Role[] roles)
        ////{
        ////    if (roles == null) return null;
        ////    using (Transaction txn = BeginTransaction("AddEngagement", true))
        ////    {
        ////        PersonAndOrganization po = CreateEngagment(engagedPerson);
        ////        po.SetRoles(roles);
        ////        if (txn != null) txn.Commit();
        ////        return po;
        ////    }      
        ////}

        /////// <summary>
        /////// Creates a new Engagement with the capacity defined in actorRoles
        /////// </summary>
        /////// <param name="engagedPerson"></param>
        /////// <param name="actorRoles"></param>
        /////// <returns></returns>
        ////public PersonAndOrganization AddEngagement(Person engagedPerson, params ActorRole[] actorRoles)
        ////{
        ////    if (actorRoles == null) return null;
        ////    using (Transaction txn = BeginTransaction("AddEngagement", true))
        ////    {
        ////        PersonAndOrganization po = CreateEngagment(engagedPerson);
        ////        po.SetRoles(actorRoles);
        ////        if (txn != null) txn.Commit();
        ////        return po;
        ////    }      
        ////}

        ////private PersonAndOrganization CreateEngagment(Person engagedPerson)
        ////{

        ////    foreach (PersonAndOrganization item in Engages)
        ////    {
        ////        if (engagedPerson == item.ThePerson) //already exists
        ////            return item;
        ////    }
        ////    //otherwise we create a new Relationship

        ////    PersonAndOrganization po = AddressBook.AddPersonAndOrganization(engagedPerson,this);
        ////    return po; 
        ////}

        /////// <summary>
        /////// Creates a new Engages relationship between a Person and the Organization
        /////// </summary>
        ////public PersonAndOrganization AddEngagement(Person engagedPerson)
        ////{
        ////    PersonAndOrganization po = CreateEngagment(engagedPerson);   
        ////    return po;
        ////}


        /////// <summary>
        /////// Removes the EngagedIn relationship between the Person and this Organization
        /////// </summary>
        /////// <param name="engagedPerson"></param>
        ////public void RemoveEngagement(Person engagedPerson)
        ////{
        ////    if (engagedPerson != null)
        ////    {

        ////        foreach (PersonAndOrganization item in Engages)
        ////        {
        ////            if (engagedPerson == item.ThePerson) // exists
        ////            {
        ////                    AddressBook.DeletePersonAndOrganization(item); //handles all notifications
        ////                    return;
        ////            }
        ////        }
        ////    }
        ////}
        /////// <summary>
        /////// Creates a new Actor role and adds to the roles collection
        /////// </summary>
        /////// <param name="newRole"></param>
        /////// <returns></returns>
        ////public ActorRole AddRole(Role newRole)
        ////{
        ////    ActorRole aRole = new ActorRole(newRole);
        ////    aRole.OnModelAdd(Model);
        ////    return AddRole(aRole);
        ////}

        ////public ActorRole AddRole(ActorRole newRole)
        ////{
        ////    using (Transaction txn = BeginTransaction("AddRole", true))
        ////    {
        ////        if (_roles == null)
        ////            SetRoles(newRole); //this take care of notifications
        ////        else
        ////        {
        ////            _roles.Add_Reversible(newRole);
        ////            Transaction.AddTransactionReversedHandler(() => { NotifyPropertyChanged("Roles"); NotifyPropertyChanged("RolesString"); });
        ////            NotifyPropertyChanged("Roles");
        ////            NotifyPropertyChanged("RolesString");
        ////        }
        ////        if (txn != null) txn.Commit();
        ////        return newRole;
        ////    }
        ////}

        /////// <summary>
        /////// Deletes the specified Role from the roles collection
        /////// </summary>
        /////// <param name="role"></param>
        ////public void DeleteRole(ActorRole role)
        ////{

        ////    if (_roles != null)
        ////    {
        ////        using (Transaction txn = BeginTransaction("DeleteRole", true))
        ////        {
        ////            _roles.Remove_Reversible(role);
        ////            Transaction.AddTransactionReversedHandler(() => { NotifyPropertyChanged("Roles"); NotifyPropertyChanged("RolesString"); });
        ////            if (txn != null) txn.Commit();
        ////            NotifyPropertyChanged("Roles");
        ////            NotifyPropertyChanged("RolesString");
        ////        }
        ////    }
        ////}

        /////// <summary>
        /////// creates a new Postal Address and adds it to the collection of addresses
        /////// </summary>
        ////public PostalAddress AddPostalAddress()
        ////{
        ////    PostalAddress address = new PostalAddress(Model);

        ////    return AddPostalAddress(address);

        ////}
        /////// <summary>
        /////// creates a new Address and adds it to the collection of addresses
        /////// </summary>
        ////public TelecomAddress AddTelecomAddress()
        ////{
        ////    TelecomAddress address = new TelecomAddress(Model);

        ////    return AddTelecomAddress(address);
        ////}

        /////// <summary>
        /////// Adds the Telecom Address to the colection of addresses
        /////// </summary>
        ////public TelecomAddress AddTelecomAddress(TelecomAddress address)
        ////{

        ////    using (Transaction txn = BeginTransaction("AddTelecomAddress", true))
        ////    {
        ////        if (_addresses == null)
        ////        {
        ////            _addresses = new AddressCollection();
        ////            Transaction.AddPropertyChange(v => _addresses = v, null, _addresses);
        ////        }
        ////        address.OnModelAdd(Model);
        ////        _addresses.Add_Reversible(address);
        ////        Transaction.AddTransactionReversedHandler(() => { NotifyPropertyChanged("TelecomAddresses"); });
        ////        if (txn != null) txn.Commit();
        ////        NotifyPropertyChanged("TelecomAddresses");
        ////        return address;
        ////    }
        ////}
        /////// <summary>
        /////// Adds the Postal Address to the colection of addresses
        /////// </summary>
        ////public PostalAddress AddPostalAddress(PostalAddress address)
        ////{

        ////    using (Transaction txn = BeginTransaction("AddPostalAddress", true))
        ////    {
        ////        if (_addresses == null)
        ////        {
        ////            _addresses = new AddressCollection();
        ////            Transaction.AddPropertyChange(v => _addresses = v, null, _addresses);
        ////        }
        ////        address.OnModelAdd(Model);
        ////        _addresses.Add_Reversible(address);
        ////        Transaction.AddTransactionReversedHandler(() => { NotifyPropertyChanged("PostalAddresses"); });
        ////        if (txn != null) txn.Commit();
        ////        NotifyPropertyChanged("PostalAddresses");
        ////        return address;
        ////    }
        ////}

        /////// <summary>
        /////// Deletes the Postal Address from the Adddresses collection
        /////// </summary>
        ////public void DeletePostalAddress(PostalAddress address)
        ////{
        ////    if (_addresses != null)
        ////        using (Transaction txn = BeginTransaction("DeletePostalAddress", true))
        ////        {
        ////            _addresses.Remove_Reversible(address);
        ////            Transaction.AddTransactionReversedHandler(() => { NotifyPropertyChanged("PostalAddresses"); });
        ////            if (txn != null) txn.Commit();
        ////            NotifyPropertyChanged("PostalAddresses");
        ////        }
        ////}


        /////// <summary>
        /////// Deletes the Telecom Address from the Adddresses collection
        /////// </summary>
        ////public void DeleteTelecomAddress(TelecomAddress address)
        ////{
        ////    if (_addresses != null)
        ////        using (Transaction txn = BeginTransaction("DeleteTelecomAddress", true))
        ////        {
        ////            _addresses.Remove_Reversible(address);
        ////            Transaction.AddTransactionReversedHandler(() => { NotifyPropertyChanged("TelecomAddresses"); });
        ////            if (txn != null) txn.Commit();
        ////            NotifyPropertyChanged("TelecomAddresses");
        ////        }
        ////}

        /////// <summary>
        /////// Creates a new relationship between this and the related Organization
        /////// </summary>
        ////public OrganizationRelationship AddOrganizationRelationship(string relationshipName, Organization relatedOrganization)
        ////{
        ////    if (Model != null) throw new InvalidOperationException("It is illegal to call AddOrganizationRelationship when an object has been added to the model. Use AddressBook.AddOrganizationRelationship");
        ////    OrganizationRelationship newRel = new OrganizationRelationship();
        ////    OrganizationRelationship._temporaries.Add(newRel);
        ////    newRel.RelatedOrganizations.Add(relatedOrganization);
        ////    newRel.RelatingOrganization = this;
        ////    newRel.Name = relationshipName;
        ////    return newRel;

        ////}

        #endregion

        #region Methods

        #endregion

        #region IFormattable Members

        public override string ToString()
        {
            if (string.IsNullOrEmpty(_name))
                return "Not defined";
            else
                return _name;
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            // If no format is passed, display like this: (x, y).
            if (string.IsNullOrEmpty(format)) return ToString();
            char prop = format[0];
            string delim = format.Substring(1);
            StringBuilder str = new StringBuilder();
            switch (prop)
            {
                case 'I':
                    return string.IsNullOrEmpty(Id.GetValueOrDefault()) ? "" : Id + delim;
                case 'N':
                    return string.IsNullOrEmpty(Name) ? "" : Name + delim;
                case 'D':
                    return string.IsNullOrEmpty(Description.GetValueOrDefault()) ? "" : Description + delim;
                case 'R':
                    if (Roles == null) return "";
                    foreach (IfcActorRole item in Roles)
                    {
                        if (str.Length != 0)
                            str.Append(delim);
                        str.Append(item);
                    }
                    return str.ToString();
                default:
                    throw new FormatException(String.Format(CultureInfo.CurrentCulture, "Invalid format string: '{0}'.",
                                                            format));
            }
        }

        #endregion

        #region INotifyPropertyChanged Members

        //TODO: Resolve

        //////internal void NotifyRelatesPropertyChanged(Organization relatedOrganization)
        //////{
        //////        relatedOrganization.NotifyPropertyChanged("IsRelatedBy");
        //////        relatedOrganization.NotifyPropertyChanged("Relates");
        //////        relatedOrganization.NotifyPropertyChanged("IsRelatedByString");
        //////        relatedOrganization.NotifyPropertyChanged("RelatesString");
        //////        NotifyPropertyChanged("IsRelatedBy");
        //////        NotifyPropertyChanged("Relates");
        //////        NotifyPropertyChanged("IsRelatedByString");
        //////        NotifyPropertyChanged("RelatesString");  
        //////}

        #endregion

        internal bool IsEquivalent(IfcOrganization o)
        {
            if (o == null) return false;
            return (o._addresses.IsEquivalent(_addresses)
                    && o._description == _description
                    && o._id == _id
                    && o._name == _name
                    && o._roles.IsEquivalent(_roles));
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
            return "";
        }

        #endregion
    }
}