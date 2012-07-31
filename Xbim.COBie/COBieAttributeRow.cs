using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.COBieExtensions;
using System.Reflection;

namespace Xbim.COBie
{
     [Serializable()]
    public class COBieAttributeRow : COBieRow
    {
         static COBieAttributeRow()
        {
            _columns = new Dictionary<int, COBieColumn>();
            Properties = typeof(COBieAttributeRow).GetProperties(BindingFlags.Public | BindingFlags.Instance);

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

        public COBieAttributeRow()
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
        private string _category;
        private string _sheetName;
        private string _rowName;
        private string _value;
        private string _unit;
        private string _extSystem;
        private string _extObject;
        private string _extIdentifier;
        private string _description;
        private string _allowedValues;

        private COBieReader _cobieReader = new COBieReader();

        [COBieAttributes(0, COBieKeyType.CompoundKey, COBieAttributeState.Required, "Name", 255, COBieAllowedType.AlphaNumeric)]
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

        [COBieAttributes(3, COBieKeyType.None, COBieAttributeState.Required, "Category", 255, COBieAllowedType.AlphaNumeric)]
        public string Category
        {
            get
            { return _category; }
            set
            {
                _category = value;
            }
        }

        [COBieAttributes(4, COBieKeyType.CompoundKey, COBieAttributeState.Required, "SheetName", 255, COBieAllowedType.AlphaNumeric)]
        public string SheetName
        {
            get
            { return _sheetName; }
            set
            {
                _sheetName = value;
            }
        }

        [COBieAttributes(5, COBieKeyType.CompoundKey, COBieAttributeState.Required, "RowName", 255, COBieAllowedType.AlphaNumeric)]
        public string RowName
        {
            get
            { return _rowName; }
            set
            {
                _rowName = value;
            }
        }

        [COBieAttributes(6, COBieKeyType.None, COBieAttributeState.Required, "Value", 255, COBieAllowedType.AlphaNumeric)]
        public string Value
        {
            get
            { return _value; }
            set
            {
                _value = value;
            }
        }

        [COBieAttributes(7, COBieKeyType.None, COBieAttributeState.Required, "Unit", 255, COBieAllowedType.AlphaNumeric)]
        public string Unit
        {
            get
            { return _unit; }
            set
            {
                _unit = value;
            }
        }

        [COBieAttributes(8, COBieKeyType.None, COBieAttributeState.System, "ExtSystem", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtSystem
        {
            get
            { return _extSystem; }
            set
            {
                _extSystem = value;
            }
        }

        [COBieAttributes(9, COBieKeyType.None, COBieAttributeState.System, "ExtObject", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtObject
        {
            get
            { return _extObject; }
            set
            {
                _extObject = value;
            }
        }

        [COBieAttributes(10, COBieKeyType.None, COBieAttributeState.System, "ExtIdentifier", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtIdentifier
        {
            get
            { return _extIdentifier; }
            set
            {
                _extIdentifier = value;
            }
        }

        [COBieAttributes(11, COBieKeyType.None, COBieAttributeState.System, "Description", 255, COBieAllowedType.AlphaNumeric)]
        public string Description
        {
            get
            { return _description; }
            set
            {
                _description = value;
            }
        }

        [COBieAttributes(12, COBieKeyType.None, COBieAttributeState.System, "AllowedValues", 255, COBieAllowedType.AlphaNumeric)]
        public string AllowedValues
        {
            get
            { return _allowedValues; }
            set
            {
                _allowedValues = value;
            }
        }

    }
}
