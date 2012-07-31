using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.COBieExtensions;
using System.Reflection;

namespace Xbim.COBie
{
    [Serializable()]
    public class COBieComponentRow : COBieRow
    {
        static COBieComponentRow()
        {
            _columns = new Dictionary<int, COBieColumn>();
            Properties = typeof(COBieComponentRow).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // add column info 
            foreach (PropertyInfo propInfo in Properties)
            {
                object[] attrs = propInfo.GetCustomAttributes(typeof(COBieAttributes), true);
                if (attrs != null && attrs.Length > 0)
                {
                    _columns.Add(((COBieAttributes)attrs[0]).Order, new COBieColumn(((COBieAttributes)attrs[0]).ColumnName, ((COBieAttributes)attrs[0]).MaxLength, ((COBieAttributes)attrs[0]).AllowedType, ((COBieAttributes)attrs[0]).KeyType));
                }

            }
        }    

        public COBieComponentRow()
        {
        }


        public COBieCell this[int i]
        {
            get
            {
                foreach (PropertyInfo propInfo in Properties)
                {
                    object[] attrs = propInfo.GetCustomAttributes(typeof(COBieAttributes), true);
                    if (attrs != null && attrs.Length > 0)
                    {
                        if (((COBieAttributes)attrs[0]).Order == i) // return (COBieCell)propInfo.GetValue(this, null);
                        {
                            //COBieCell cell = (COBieCell)propInfo.GetValue(this, null);
                            PropertyInfo pinfo = this.GetType().GetProperty(propInfo.Name);
                            COBieCell cell = new COBieCell(pinfo.GetValue(this, null).ToString());
                            cell.COBieState = ((COBieAttributes)attrs[0]).State;
                            cell.CobieCol = _columns[((COBieAttributes)attrs[0]).Order];

                            return cell;
                        }
                    }

                }

                return null;
            }
        }

        public COBieCell this[string name]
        {
            get
            {
                foreach (PropertyInfo propInfo in Properties)
                {
                    object[] attrs = propInfo.GetCustomAttributes(typeof(COBieAttributes), true);
                    if (attrs != null && attrs.Length > 0)
                    {
                        if (((COBieAttributes)attrs[0]).ColumnName == name) // return (COBieCell)propInfo.GetValue(this, null);
                        {
                            //COBieCell cell = (COBieCell)propInfo.GetValue(this, null);
                            PropertyInfo pinfo = this.GetType().GetProperty(propInfo.Name);
                            COBieCell cell = new COBieCell(pinfo.GetValue(this, null).ToString());
                            cell.COBieState = ((COBieAttributes)attrs[0]).State;
                            cell.CobieCol = _columns[((COBieAttributes)attrs[0]).Order];

                            return cell;
                        }
                    }

                }

                return null;
            }
        }

        private string _name;
        private string _createdBy;
        private string _createdOn;

        private string _typeName;
        private string _space;
        private string _description;
        private string _extSystem;
        private string _extObject;
        private string _extIdentifier;
        private string _serialNumber;
        private string _installationDate;
        private string _warrantyStartDate;
        private string _tagNumber;
        private string _barCode;
        private string _assetIdentifier;


        private COBieReader _cobieReader = new COBieReader();

        [COBieAttributes(0, COBieKeyType.PrimaryKey, COBieAttributeState.Required, "Name", 255, COBieAllowedType.AlphaNumeric)]
        public string Name
        {
            get
            { return _name; }
            set
            {
                _name = value;
            }
        }

        [COBieAttributes(1, COBieKeyType.None, COBieAttributeState.Required, "CreatedBy", 255, COBieAllowedType.Email)]
        public string CreatedBy
        {
            get { return _createdBy; }
            set
            {
                _createdBy = value;
            }
        }

        [COBieAttributes(2, COBieKeyType.None, COBieAttributeState.Required, "CreatedOn", 19, COBieAllowedType.ISODate)]
        public string CreatedOn
        {
            get { return _createdOn; }
            set
            {
                _createdOn = value;
            }
        }


        [COBieAttributes(3, COBieKeyType.None, COBieAttributeState.Required, "TypeName", 255, COBieAllowedType.AlphaNumeric)]
        public string TypeName
        {
            get
            { return _typeName; }
            set
            {
                _typeName = value;
            }
        }

        [COBieAttributes(4, COBieKeyType.None, COBieAttributeState.Required, "Space", 255, COBieAllowedType.AlphaNumeric)]
        public string Space
        {
            get
            { return _space; }
            set
            {
                _space = value;
            }
        }

        [COBieAttributes(5, COBieKeyType.None, COBieAttributeState.Required, "Description", 255, COBieAllowedType.AlphaNumeric)]
        public string Description
        {
            get
            { return _description; }
            set
            {
                _description = value;
            }
        }

        [COBieAttributes(6, COBieKeyType.None, COBieAttributeState.System, "ExtSystem", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtSystem
        {
            get
            { return _extSystem; }
            set
            {
                _extSystem = value;
            }
        }

        [COBieAttributes(7, COBieKeyType.None, COBieAttributeState.System, "ExtObject", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtObject
        {
            get
            { return _extObject; }
            set
            {
                _extObject = value;
            }
        }

        [COBieAttributes(8, COBieKeyType.None, COBieAttributeState.System, "ExtIdentifier", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtIdentifier
        {
            get
            { return _extIdentifier; }
            set
            {
                _extIdentifier = value;
            }
        }

        [COBieAttributes(9, COBieKeyType.None, COBieAttributeState.Required, "SerialNumber", 255, COBieAllowedType.AlphaNumeric)]
        public string SerialNumber
        {
            get
            { return _serialNumber; }
            set
            {
                _serialNumber = value;
            }
        }

        [COBieAttributes(10, COBieKeyType.None, COBieAttributeState.Required, "InstallationDate", 19, COBieAllowedType.ISODate)]
        public string InstallationDate
        {
            get
            { return _installationDate; }
            set
            {
                _installationDate = value;
            }
        }

        [COBieAttributes(11, COBieKeyType.None, COBieAttributeState.Required, "WarrantyStartDate", 19, COBieAllowedType.ISODate)]
        public string WarrantyStartDate
        {
            get
            { return _warrantyStartDate; }
            set
            {
                _warrantyStartDate = value;
            }
        }

        [COBieAttributes(12, COBieKeyType.None, COBieAttributeState.As_Specified, "TagNumber", 255, COBieAllowedType.AlphaNumeric)]
        public string TagNumber
        {
            get
            { return _tagNumber; }
            set
            {
                _tagNumber = value;
            }
        }

        [COBieAttributes(13, COBieKeyType.None, COBieAttributeState.As_Specified, "BarCode", 255, COBieAllowedType.AlphaNumeric)]
        public string BarCode
        {
            get
            { return _barCode; }
            set
            {
                _barCode = value;
            }
        }

        [COBieAttributes(14, COBieKeyType.None, COBieAttributeState.As_Specified, "AssetIdentifier", 255, COBieAllowedType.AlphaNumeric)]
        public string AssetIdentifier
        {
            get
            { return _assetIdentifier; }
            set
            {
                _assetIdentifier = value;
            }
        }
    }
}
