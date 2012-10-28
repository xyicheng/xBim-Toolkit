#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcPerson.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.Serialization;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.Ifc2x3.ActorResource
{
    [IfcPersistedEntityAttribute, Serializable]
    public class PersonCollection : XbimList<IfcPerson>
    {
        internal PersonCollection(IPersistIfcEntity owner)
            : base(owner)
        {
        }


        /// <summary>
        ///   Finds the person with either the ID of id or if the ID is null the FamilyName == id. Only returns the first match or null if none found
        /// </summary>
        /// <param name = "id"></param>
        /// <returns></returns>
        public IfcPerson this[IfcIdentifier id]
        {
            get
            {
                foreach (IfcPerson p in this)
                {
                    if (string.IsNullOrEmpty(p.Id.GetValueOrDefault()) && p.FamilyName.GetValueOrDefault() == id)
                        return p;
                    else if (p.Id == id) return p;
                }
                return null;
            }
        }
    }

    [IfcPersistedEntityAttribute, Serializable]
    public class IfcPerson : IfcActorSelect, IPersistIfcEntity, IFormattable, ISupportChangeNotification,
                             INotifyPropertyChanged, IfcObjectReferenceSelect, INotifyPropertyChanging
    {

        public override bool Equals(object obj)
        {
            // Check for null
            if (obj == null) return false;

            // Check for type
            if (this.GetType() != obj.GetType()) return false;

            // Cast as IfcRoot
            IfcPerson root = (IfcPerson)obj;
            return this == root;
        }
        public override int GetHashCode()
        {
            return Math.Abs(_entityLabel); //good enough as most entities will be in collections of  only one model, equals distinguishes for model
        }

        public static bool operator ==(IfcPerson left, IfcPerson right)
        {
            // If both are null, or both are same instance, return true.
            if (System.Object.ReferenceEquals(left, right))
                return true;

            // If one is null, but not both, return false.
            if (((object)left == null) || ((object)right == null))
                return false;

            return (Math.Abs(left.EntityLabel) == Math.Abs(right.EntityLabel)) && (left.ModelOf == right.ModelOf);

        }

        public static bool operator !=(IfcPerson left, IfcPerson right)
        {
            return !(left == right);
        }
        #region IPersistIfcEntity Members

        private int _entityLabel;
        private IModel _model;

        public IModel ModelOf
        {
            get { return _model; }
        }

        void IPersistIfcEntity.Bind(IModel model, int entityLabel)
        {
            _model = model;
            _entityLabel = entityLabel;
        }

        bool IPersistIfcEntity.Activated
        {
            get { return _entityLabel > 0; }
        }

        public int EntityLabel
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

        private IfcIdentifier? _id;
        private IfcLabel? _familyName;
        private IfcLabel? _givenName;
        private LabelCollection _middleNames;
        private LabelCollection _prefixTitles;
        private LabelCollection _suffixTitles;
        private ActorRoleCollection _roles;
        private AddressCollection _addresses;

        #endregion

        #region Constructors & Initialisers

        #endregion

        #region Properties

        //TODO: Resolve below

        //////[Browsable(true)]
        //////public string PrefixTitlesString
        //////{
        //////    get { return _prefixTitles == null ? null : string.Join(" ", _prefixTitles.ToArray()); }
        //////    set
        //////    {

        //////        using (Transaction txn = BeginTransaction(string.Format("PrefixTitles = {0}", value),true))
        //////        {

        //////            if (string.IsNullOrEmpty(value))
        //////            {
        //////                Transaction.AddPropertyChange(v => _prefixTitles = v, _prefixTitles, null);
        //////                _prefixTitles = null;
        //////            }
        //////            else
        //////            {
        //////                string[] prefixes = value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        //////                if (string.Join(" ", prefixes) == PrefixTitlesString) //no real change so exit transaction
        //////                    return;   
        //////                if (_prefixTitles == null)
        //////                {
        //////                    LabelCollection c = new LabelCollection();
        //////                    Transaction.AddPropertyChange(v => _prefixTitles = v, null, c);
        //////                    _prefixTitles = c;
        //////                }
        //////                else
        //////                    _prefixTitles.Clear_Reversible();

        //////                if (prefixes != null)
        //////                {
        //////                    _prefixTitles.AddRange_Reversible(prefixes);
        //////                }
        //////            }
        //////            Transaction.AddTransactionReversedHandler(() => { NotifyPropertyChanged("PrefixTitles"); NotifyPropertyChanged("PrefixTitlesString"); });
        //////            if (txn != null) txn.Commit();
        //////            NotifyPropertyChanged("PrefixTitles");
        //////            NotifyPropertyChanged("PrefixTitlesString");
        //////        }
        //////    }
        //////}

        //////[Browsable(true)]
        //////public string SuffixTitlesString
        //////{
        //////    get { return _suffixTitles == null ? null : string.Join("; ", _suffixTitles.ToArray()); }
        //////    set
        //////    {

        //////        using (Transaction txn = BeginTransaction(string.Format("SuffixTitles = {0}", value),true))
        //////        {

        //////            if (string.IsNullOrEmpty(value))
        //////            {
        //////                Transaction.AddPropertyChange(v => _suffixTitles = v, _suffixTitles, null);
        //////                _suffixTitles = null;
        //////            }
        //////            else
        //////            {
        //////                string[] suffixes = value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        //////                if (string.Join(" ", suffixes) == SuffixTitlesString) //no real change so exit transaction
        //////                    return;
        //////                if (_suffixTitles == null)
        //////                {
        //////                    LabelCollection c = new LabelCollection();
        //////                    Transaction.AddPropertyChange(v => _suffixTitles = v, null, c);
        //////                    _suffixTitles = c;

        //////                }
        //////                else
        //////                    _suffixTitles.Clear_Reversible();

        //////                if (suffixes != null)
        //////                {
        //////                    _suffixTitles.AddRange_Reversible(suffixes);
        //////                }
        //////            }
        //////            Transaction.AddTransactionReversedHandler(() => { NotifyPropertyChanged("SuffixTitles"); NotifyPropertyChanged("SuffixTitlesString"); });
        //////            if (txn != null) txn.Commit();
        //////            NotifyPropertyChanged("SuffixTitles");
        //////            NotifyPropertyChanged("SuffixTitlesString");
        //////        }

        //////    }
        //////}

        //////[Browsable(true)]
        //////public string MiddleNamesString
        //////{
        //////    get { return _middleNames == null ? null : string.Join(" ", _middleNames.ToArray()); }
        //////    set
        //////    {
        //////        using (Transaction txn = BeginTransaction(string.Format("Middle Names = {0}", value),true))
        //////        {

        //////            if (string.IsNullOrEmpty(value))
        //////            {
        //////                Transaction.AddPropertyChange(v => _middleNames = v, _middleNames, null);
        //////                _middleNames = null;
        //////            }
        //////            else
        //////            {
        //////                string[] middleNames = value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        //////                if (string.Join(" ", middleNames) == MiddleNamesString) //no real change so exit transaction
        //////                    return;

        //////                if (_middleNames == null)
        //////                {
        //////                    LabelCollection c = new LabelCollection();
        //////                    Transaction.AddPropertyChange(v => _middleNames = v, null, c);
        //////                    _middleNames = c;

        //////                }
        //////                else
        //////                    _middleNames.Clear_Reversible();

        //////                if (middleNames != null)
        //////                {
        //////                    _middleNames.AddRange_Reversible(middleNames);
        //////                }
        //////            }
        //////            Transaction.AddTransactionReversedHandler(() => { NotifyPropertyChanged("MiddleNames"); NotifyPropertyChanged("MiddleNamesString"); });
        //////            if (txn != null) txn.Commit();
        //////            NotifyPropertyChanged("MiddleNames");
        //////            NotifyPropertyChanged("MiddleNamesString");
        //////        } 
        //////    }
        //////}

        //////[Browsable(true)]
        //////public string RolesString
        //////{
        //////    get { return _roles == null ? null : _roles.ToString("D; ", null); }
        //////    set
        //////    {
        //////        using (Transaction txn = BeginTransaction(string.Format("Roles = {0}", value), true))
        //////        {
        //////            if (string.IsNullOrEmpty(value))
        //////            {
        //////                Transaction.AddPropertyChange(v => _roles = v, _roles, null);
        //////                _roles = null;
        //////            }
        //////            else
        //////            {
        //////                string[] roles = value.Split(new string[] { "; ", ";" }, StringSplitOptions.RemoveEmptyEntries);
        //////                if (string.Join("; ", roles) == RolesString) //no real change so exit transaction
        //////                    return;
        //////                if (_roles == null)
        //////                {
        //////                    ActorRoleCollection c = new ActorRoleCollection();
        //////                    Transaction.AddPropertyChange(v => _roles = v, null, c);
        //////                    _roles = c;
        //////                }
        //////                else
        //////                    _roles.Clear_Reversible();

        //////                if (roles != null)
        //////                {
        //////                    foreach (string item in roles)
        //////                    {
        //////                        ActorRole aRole = new ActorRole(item);
        //////                        aRole.OnModelAdd(Model);
        //////                        _roles.Add_Reversible(aRole);
        //////                    }
        //////                }
        //////            }
        //////            Transaction.AddTransactionReversedHandler(() => { NotifyPropertyChanged("Roles"); NotifyPropertyChanged("RolesString"); });
        //////            if (txn != null) txn.Commit();
        //////            NotifyPropertyChanged("Roles");
        //////            NotifyPropertyChanged("RolesString");
        //////        }
        //////    }
        //////}


        //////[XmlIgnore]
        //////[Browsable(true)]
        //////public PostalAddressCollection PostalAddresses
        //////{
        //////    get 
        //////    { 
        //////        if(Addresses==null) return null;

        //////        ObservableCollection<PostalAddress> coll = new ObservableCollection<PostalAddress>();
        //////        foreach (PostalAddress item in _addresses.PostalAddresses)
        //////        {
        //////            coll.Add(item);
        //////        }
        //////        PostalAddressCollection paColl = new PostalAddressCollection( coll);
        //////        return paColl;

        //////    }
        //////}
        //////[XmlIgnore]
        //////[Browsable(true)]
        //////public TelecomAddressCollection TelecomAddresses
        //////{
        //////    get
        //////    {
        //////        if (Addresses == null) return null;
        //////        ObservableCollection<TelecomAddress> coll = new ObservableCollection<TelecomAddress>();
        //////        foreach (TelecomAddress item in _addresses.TelecomAddresses)
        //////        {
        //////            coll.Add(item);
        //////        }
        //////        TelecomAddressCollection tcColl = new TelecomAddressCollection(coll);
        //////        return tcColl;

        //////    }
        //////}


        //////[XmlIgnore]
        //////public string EngagedInString
        //////{
        //////    get
        //////    {

        //////        StringBuilder str = new StringBuilder();
        //////        foreach (PersonAndOrganization item in EngagedIn)
        //////        {
        //////            if (str.Length > 0)
        //////                str.Append("; ");
        //////            str.AppendFormat("{0:N}{1}{2}", item.TheOrganization, item.Roles != null ? " as " : "", item.Roles != null ? item.Roles.Summary : "");
        //////        }
        //////        return str.ToString();
        //////    }
        //////}

        #endregion

        #region Methods

        protected string GivenNameString(string appendIfNotEmpty)
        {
            if (!string.IsNullOrEmpty(GivenName.GetValueOrDefault()))
                return GivenName + appendIfNotEmpty;
            else
                return "";
        }

        protected string FamilyNameString(string appendIfNotEmpty)
        {
            if (!string.IsNullOrEmpty(FamilyName.GetValueOrDefault()))
                return FamilyName + appendIfNotEmpty;
            else
                return "";
        }


        protected string PrefixTitlesDelimited(string delimiter)
        {
            return PrefixTitlesDelimited(delimiter, "");
        }

        protected string PrefixTitlesDelimited(string delimiter, string appendIfNotEmpty)
        {
            string ret = PrefixTitles == null ? "" : PrefixTitles.ToString("D" + delimiter, null);
            //return delimited string
            if (!string.IsNullOrEmpty(ret))
                ret += appendIfNotEmpty;
            return ret;
        }


        protected string SuffixTitlesDelimited(string delimiter)
        {
            return SuffixTitlesDelimited(delimiter, "");
        }

        protected string SuffixTitlesDelimited(string delimiter, string appendIfNotEmpty)
        {
            string ret = SuffixTitles == null ? "" : SuffixTitles.ToString("D" + delimiter, null);
            //return delimited string
            if (!string.IsNullOrEmpty(ret))
                ret += appendIfNotEmpty;
            return ret;
        }


        protected string MiddleNamesDelimited(string delimiter)
        {
            return MiddleNamesDelimited(delimiter, "");
        }

        protected string MiddleNamesDelimited(string delimiter, string appendIfNotEmpty)
        {
            string ret = MiddleNames == null ? "" : _middleNames.ToString("D" + delimiter, null);
            //return delimited string 
            if (!string.IsNullOrEmpty(ret))
                ret += appendIfNotEmpty;
            return ret;
        }

        protected string RolesDelimited(string delimiter)
        {
            return RolesDelimited(delimiter, "");
        }

        protected string RolesDelimited(string delimiter, string appendIfNotEmpty)
        {
            string ret = _roles == null ? "" : _roles.ToString("D" + delimiter, null); //return delimited string
            if (!string.IsNullOrEmpty(ret))
                ret += appendIfNotEmpty;
            return ret;
        }

        #endregion

        #region Ifc Properties

        /// <summary>
        ///   Optional Identification of the person
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Optional)]
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
        ///   Optional.   The name by which the family identity of the person may be recognized.
        /// </summary>
        [IfcAttribute(2, IfcAttributeState.Optional)]
        public IfcLabel? FamilyName
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _familyName;
            }
            set { this.SetModelValue(this, ref _familyName, value, v => FamilyName = v, "FamilyName"); }
        }

        /// <summary>
        ///   Optional. The name by which a person is known within a family and by which he or she may be familiarly recognized.
        /// </summary>
        [IfcAttribute(3, IfcAttributeState.Optional)]
        public IfcLabel? GivenName
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _givenName;
            }
            set { this.SetModelValue(this, ref _givenName, value, v => GivenName = v, "GivenName"); }
        }

        /// <summary>
        ///   Optional. Additional names given to a person that enable their identification apart from others who may have the same or similar family and given names.
        /// </summary>
        [IfcAttribute(4, IfcAttributeState.Optional, IfcAttributeType.List)]
        public LabelCollection MiddleNames
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _middleNames;
            }
            set { this.SetModelValue(this, ref _middleNames, value, v => MiddleNames = v, "MiddleNames"); }
        }

        /// <summary>
        ///   Optional The word, or group of words, which specify the person's social and/or professional standing and appear before his/her names.
        /// </summary>
        [IfcAttribute(5, IfcAttributeState.Optional, IfcAttributeType.List)]
        public LabelCollection PrefixTitles
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _prefixTitles;
            }
            set { this.SetModelValue(this, ref _prefixTitles, value, v => PrefixTitles = v, "PrefixTitles"); }
        }

        /// <summary>
        ///   Optional. The word, or group of words, which specify the person's social and/or professional standing and appear after his/her names.
        /// </summary>
        [IfcAttribute(6, IfcAttributeState.Optional, IfcAttributeType.List)]
        public LabelCollection SuffixTitles
        {
            get
            {
                ((IPersistIfcEntity) this).Activate(false);
                return _suffixTitles;
            }
            set { this.SetModelValue(this, ref _suffixTitles, value, v => SuffixTitles = v, "SuffixTitles"); }
        }

        /// <summary>
        ///   Optional. Roles played by the person
        /// </summary>
        [IfcAttribute(7, IfcAttributeState.Optional, IfcAttributeType.List)]
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
        ///   Optional. Postal and telecommunication addresses of a person.
        /// </summary>
        /// <remarks>
        ///   NOTE - A person may have several addresses.
        /// </remarks>

        [IfcAttribute(8, IfcAttributeState.Optional, IfcAttributeType.List)]
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
                    _familyName = value.StringVal;
                    break;
                case 2:
                    _givenName = value.StringVal;
                    break;
                case 3:
                    if (_middleNames == null) _middleNames = new LabelCollection(this);
                    _middleNames.Add(value.StringVal);
                    break;
                case 4:
                    if (_prefixTitles == null) _prefixTitles = new LabelCollection(this);
                    _prefixTitles.Add(value.StringVal);
                    break;
                case 5:
                    if (_suffixTitles == null) _suffixTitles = new LabelCollection(this);
                    _suffixTitles.Add(value.StringVal);
                    break;
                case 6:
                    if (_roles == null) _roles = new ActorRoleCollection(this);
                    _roles.Add((IfcActorRole)value.EntityVal);
                    break;
                case 7:
                    if (_addresses == null) _addresses = new AddressCollection(this);
                    _addresses.Add((IfcAddress)value.EntityVal);
                    break;
                default:
                    this.HandleUnexpectedAttribute(propIndex, value); break;
            }
        }

        #endregion

        #region Inverse Relationships

        //TODO: Resolve
        //////[Browsable(false)]
        //////public IEnumerable<PersonAndOrganization> EngagedIn
        //////{
        //////    get
        //////    {
        //////        if (AddressBook == null) return null;
        //////        return from PersonAndOrganization instance in AddressBook.PersonAndOrganizations
        //////               where instance.ThePerson == this
        //////               select instance;
        //////    }
        //////}

        #endregion

        #region IFormattable Members

        public override string ToString()
        {
            string str = String.Format(CultureInfo.CurrentCulture, "{0}{1}{2}{3}{4}{5}",
                                       PrefixTitlesDelimited(", ", ", "),
                                       GivenNameString(", "),
                                       MiddleNamesDelimited(", ", ", "),
                                       FamilyNameString(", "),
                                       SuffixTitlesDelimited(", ", ", "),
                                       RolesDelimited(", ", ""));
            return str.TrimEnd(',', ' ');
        }

        /// <summary>
        ///   Special format method for the properties of a Person
        /// </summary>
        /// <remarks>
        ///   Format string in two parts. {FormatChar}{Text}. i.e. "F,"
        ///   Text is any arbitrary text to appear after the formatted text. If the formatted text is an empty string the arbitrary text is not appended. Where there is a list of values these are listed and delimited by the arbitrary text.
        /// </remarks>
        /// <param name = "format">
        ///   Format string in two parts. {FormatChar}{Text}. i.e. "F,"
        ///   Text is any arbitrary text to appear after the formatted text. If the formatted text is an empty string the arbitrary text is not appended. Where there is a list of values these are listed and delimited by the arbitrary text.
        ///   'I' = Identifier
        ///   'F' = FirstName
        ///   'M' = List of Middle names
        ///   'G' = Given or first name
        ///   'P' = List of Prefix Titles (Dr. Mr. etc)
        ///   'S' = List of Suffix titles
        ///   'R' = List of Roles (Engineer, Architect)
        /// </param>
        /// <param name = "formatProvider">
        /// </param>
        /// <returns>String with the formatted result.</returns>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            // If no format is passed, display like this: (x, y).
            if (string.IsNullOrEmpty(format)) return ToString();
            char prop = format[0];
            string delim = format.Substring(1);

            switch (prop)
            {
                case 'I':
                    return string.IsNullOrEmpty(Id.GetValueOrDefault()) ? "" : Id + delim;
                case 'F':
                    return FamilyNameString(delim);
                case 'G':
                    return GivenNameString(delim);
                case 'M':
                    return MiddleNamesDelimited(delim);
                case 'P':
                    return PrefixTitlesDelimited(delim);
                case 'S':
                    return SuffixTitlesDelimited(delim);
                case 'R':
                    return RolesDelimited(delim);
                default:
                    throw new FormatException(String.Format(CultureInfo.CurrentCulture, "Invalid format string: '{0}'.",
                                                            format));
            }
        }

        #endregion

        #region Add Methods

        //TODO: Resolve
        ////public PersonAndOrganization AddEngagement(Organization engagingOrganization, params Role[] roles)
        ////{
        ////    if (roles == null) return null;
        ////    using (Transaction txn = BeginTransaction(string.Format("AddEngagment"), true))
        ////    {
        ////        PersonAndOrganization po = CreateEngagement(engagingOrganization);
        ////        po.SetRoles(roles);
        ////        if (txn != null) txn.Commit();
        ////        return po;
        ////    }

        ////}
        ////public PersonAndOrganization AddEngagement(Organization engagingOrganization, params ActorRole[] roles)
        ////{
        ////    if (roles == null) return null;
        ////    using (Transaction txn = BeginTransaction(string.Format("AddEngagment"), true))
        ////    {
        ////        PersonAndOrganization po = CreateEngagement(engagingOrganization);
        ////        po.SetRoles(roles);
        ////        if (txn != null) txn.Commit();
        ////        return po;
        ////    }

        ////}

        ////private PersonAndOrganization CreateEngagement(Organization engagingOrganization)
        ////{

        ////    foreach (PersonAndOrganization item in EngagedIn)
        ////    {
        ////        if (engagingOrganization == item.TheOrganization) //already exists
        ////            return item;
        ////    }
        ////    //otherwise we create a new Relationship

        ////    PersonAndOrganization po = AddressBook.AddPersonAndOrganization(this, engagingOrganization);
        ////    return po;

        ////}

        /////// <summary>
        /////// Creates a new PersonAndOrganization relationship between this Person and the Engaging Organization
        /////// </summary>
        /////// <param name="engagingOrganization"></param>
        /////// <returns></returns>
        ////public PersonAndOrganization AddEngagement(Organization engagingOrganization)
        ////{
        ////    engagingOrganization.OnModelAdd(Model);    
        ////    PersonAndOrganization po = CreateEngagement(engagingOrganization);
        ////        return po;
        ////}

        /////// <summary>
        /////// Removes the EngagedIn relationship between this Person the engaging Organization
        /////// </summary>
        /////// <param name="engagingOrganization"></param>
        ////public void RemoveEngagement(Organization engagingOrganization)
        ////{

        ////    foreach (PersonAndOrganization item in EngagedIn)
        ////    {
        ////        if (engagingOrganization == item.TheOrganization) // exists
        ////        {
        ////                AddressBook.DeletePersonAndOrganization(item); //handles all notifications
        ////                return;
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
        ////    ActorRole aRole = new ActorRole( newRole);
        ////    aRole.OnModelAdd(Model);
        ////    return AddRole(aRole);
        ////}

        ////public ActorRole AddRole(ActorRole newRole)
        ////{
        ////    using (Transaction txn = BeginTransaction("AddRole", true))
        ////    {
        ////        newRole.OnModelAdd(Model);
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
        ////        return AddTelecomAddress(address);      
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

        #endregion

        #region Set Collections

        public void SetMiddleNames(params string[] middleNames)
        {
            if (_middleNames == null) _middleNames = new LabelCollection(this);
            else
                _middleNames.Clear_Reversible();
            foreach (string item in middleNames)
            {
                _middleNames.Add_Reversible(item);
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
            if (!_givenName.HasValue && !_familyName.HasValue)
                return "WR1 Person: Requires that either the family name or the given name is used.\n";
            else
                return "";
        }

        #endregion
    }
}