﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcAddress.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.ActorResource
{
    [IfcPersistedEntity, Serializable]
    public class AddressCollection : XbimList<IfcAddress>
    {
        internal AddressCollection(IPersistIfcEntity owner)
            : base(owner)
        {
        }

        //internal void OnModelAdd(ModelInstance model)
        //{
        //    foreach (Address item in this)
        //    {
        //        item.OnModelAdd(model);
        //    }
        //}

        public IEnumerable<IfcPostalAddress> PostalAddresses
        {
            get
            {
                return from IfcAddress addr in this
                       where addr is IfcPostalAddress
                       select (IfcPostalAddress) addr;
            }
        }

        public IEnumerable<IfcTelecomAddress> TelecomAddresses
        {
            get
            {
                return from IfcAddress addr in this
                       where addr is IfcTelecomAddress
                       select (IfcTelecomAddress) addr;
            }
        }

        public override string ToString()
        {
            StringBuilder str = new StringBuilder();
            bool first = true;
            foreach (IfcAddress item in this)
            {
                if (!first) str.AppendLine();
                first = false;
                str.Append(item.ToString());
            }
            return str.ToString();
        }

        public string Summary
        {
            get { return this.ToString(); }
        }

        public string TelecomAddressesString
        {
            get
            {
                StringBuilder str = new StringBuilder();
                bool first = true;
                foreach (IfcTelecomAddress item in this.TelecomAddresses)
                {
                    if (!first) str.AppendLine();
                    first = false;
                    str.Append(item.ToString());
                }
                return str.ToString();
            }
        }

        public string PostalAddressesString
        {
            get
            {
                StringBuilder str = new StringBuilder();
                bool first = true;
                foreach (IfcPostalAddress item in this.PostalAddresses)
                {
                    if (!first) str.AppendLine();
                    first = false;
                    str.Append(item.ToString());
                }

                return str.ToString();
            }
        }


        internal bool IsEquivalent(AddressCollection ac)
        {
            if (ac == null) return false;
            return (ac.ToString() == ToString());
        }
    }

    /// <summary>
    ///   WR1   :   EXISTS (InternalLocation) OR EXISTS (AddressLines) OR EXISTS (PostalBox) OR EXISTS (PostalCode) OR EXISTS (Town) OR EXISTS (Region) OR EXISTS (Country);
    /// </summary>
    [IfcPersistedEntity, Serializable]
    public abstract class IfcAddress : IPersistIfcEntity, IFormattable, ISupportChangeNotification,
                                       INotifyPropertyChanged, IfcObjectReferenceSelect, INotifyPropertyChanging
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

        private IfcAddressType? _purpose;
        private IfcText? _description;
        private IfcLabel? _userDefinedPurpose;

        #endregion

        #region Constructors & Initialisers

        #endregion

        #region Ifc Properties

        /// <summary>
        ///   Optional. Identifies the logical location of the address
        /// </summary>
        [IfcAttribute(1, IfcAttributeState.Optional)]
        public IfcAddressType? Purpose
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _purpose;
            }
            set
            {
                if (_purpose != value)
                {
                    if (value == IfcAddressType.UserDefined)
                        throw new ArgumentException(
                            "An Address Type may not be explicitly set as UserDefined. Set the value of the UserDefinedPurpose Property instead");

                    ModelManager.SetModelValue(this, ref _purpose, value, v => Purpose = v, "Purpose");
                }
            }
        }

        /// <summary>
        ///   Optional. Text that relates the nature of the address.
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
        ///   Optional. Allows for specification of user specific purpose of the address beyond the
        /// </summary>
        [IfcAttribute(3, IfcAttributeState.Optional)]
        public IfcLabel? UserDefinedPurpose
        {
            get
            {
#if SupportActivation
                ((IPersistIfcEntity) this).Activate(false);
#endif
                return _userDefinedPurpose;
            }
            set
            {
                ModelManager.SetModelValue(this, ref _userDefinedPurpose, value, v => UserDefinedPurpose = v,
                                           "UserDefinedPurpose");
            }
        }

        #endregion

        #region Part 21 Step file Parse routines

        public virtual void IfcParse(int propIndex, IPropertyValue value)
        {
            switch (propIndex)
            {
                case 0:
                    _purpose = (IfcAddressType) Enum.Parse(typeof (IfcAddressType), value.EnumVal, true);
                    break;
                case 1:
                    _description = value.StringVal;
                    break;
                case 2:
                    _userDefinedPurpose = value.StringVal;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("propIndex",
                                                          string.Format("P21 index value out of range in {0}",
                                                                        this.GetType().Name));
            }
        }

        #endregion

        #region Ifc Inverse Relationships

        ///// <summary>
        ///// The inverse relationship to Person to whom address is associated.
        ///// </summary>
        //public IEnumerable<Person> OfPerson
        //{
        //    get
        //    {
        //        return from Person p in AddressBook.Persons
        //               where p.Addresses.Contains(this)
        //               select p;

        //    }
        //}

        ///// <summary>
        ///// The inverse relationship to Organization to whom address is associated.
        ///// </summary>
        //public IEnumerable<Organization> OfOrganization
        //{
        //    get
        //    {
        //        return from Organization org in AddressBook.Organizations
        //               where org.Addresses.Contains(this)
        //               select org;
        //    }

        //}

        #endregion

        #region Methods

        protected void Clone(IfcAddress from, bool deep)
        {
            _purpose = from.Purpose;
            _description = from.Description;
            _userDefinedPurpose = from.UserDefinedPurpose;
        }

        #endregion

        #region IFormattable Members

        public override string ToString()
        {
            if (Purpose.HasValue)
            {
                return string.Format(CultureInfo.CurrentCulture, "{0}",
                                     Purpose.Value == IfcAddressType.UserDefined
                                         ? (string) UserDefinedPurpose
                                         : Purpose.Value.ToString());
            }
            else
                return "";
        }

        public string ToString(string format, IFormatProvider formatProvider)
        {
            if (string.IsNullOrEmpty(format)) return ToString();
            char prop = format[0];
            string delim = format.Substring(1);

            switch (prop)
            {
                case 'D':
                    return string.IsNullOrEmpty(Description.GetValueOrDefault()) ? "" : Description + delim;
                case 'P':
                    return Purpose.HasValue ? ToString() + delim : "";
                case 'U':
                    return string.IsNullOrEmpty(UserDefinedPurpose.GetValueOrDefault())
                               ? ""
                               : UserDefinedPurpose + delim;
                default:
                    throw new FormatException(String.Format(CultureInfo.CurrentCulture, "Invalid format string: '{0}'.",
                                                            format));
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

        public virtual string WhereRule()
        {
            if (Purpose.HasValue && Purpose.Value == IfcAddressType.UserDefined && !_userDefinedPurpose.HasValue)
                return
                    "WR1 Address : Either attribute value Purpose is not given, or when attribute Purpose has enumeration value USERDEFINED then attribute UserDefinedPurpose shall also have a value.\n";
            else
                return "";
        }

        #endregion
    }
}