using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.COBieExtensions;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Xbim.COBie
{
    [Serializable()]
    public class COBieAssemblyRow : COBieRow
    {
        static COBieAssemblyRow()
        {
            _columns = new Dictionary<int, COBieColumn>();
            //_columns.Add(new COBieColumn("Name"));
            //_columns.Add(new COBieColumn("CreatedBy"));
            //_columns.Add(new COBieColumn("CreatedOn"));
            //_columns.Add(new COBieColumn("SheetName"));
            //_columns.Add(new COBieColumn("ParentName"));
            //_columns.Add(new COBieColumn("ChildNames"));
            //_columns.Add(new COBieColumn("AssemblyType"));
            //_columns.Add(new COBieColumn("ExtSystem"));
            //_columns.Add(new COBieColumn("ExtObject"));
            //_columns.Add(new COBieColumn("ExtIdentifier"));
            //_columns.Add(new COBieColumn("Description"));
            Properties = typeof(COBieAssemblyRow).GetProperties(BindingFlags.Public | BindingFlags.Instance);

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
       
        public COBieAssemblyRow()
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
        private string _sheetName;
        private string _parentName;
        private string _childNames;
        private string _assemblyType;
        private string _extSystem;
        private string _extObject;
        private string _extIdentifier;
        private string _description;


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

        [COBieAttributes(3, COBieKeyType.CompoundKey, COBieAttributeState.Required, "SheetName", 255, COBieAllowedType.AlphaNumeric)]
        public string SheetName
        {
            get { return _sheetName; }
            set
            {
                _sheetName = value;
            }
        }

        [COBieAttributes(4, COBieKeyType.CompoundKey, COBieAttributeState.Required, "ParentName", 255, COBieAllowedType.AlphaNumeric)]
        public string ParentName
        {
            get { return _parentName; }
            set
            {
                _parentName = value;
            }
        }

        [COBieAttributes(5, COBieKeyType.None, COBieAttributeState.Required, "ChildNames", 255, COBieAllowedType.AlphaNumeric)]
        public string ChildNames
        {
            get { return _childNames; }
            set
            {
                _childNames = value;
            }
        }

        [COBieAttributes(6, COBieKeyType.None, COBieAttributeState.System, "AssemblyType", 255, COBieAllowedType.AlphaNumeric)]
        public string AssemblyType
        {
            get { return _assemblyType; }
            set
            {
                _assemblyType = value;
            }
        }

        [COBieAttributes(7, COBieKeyType.None, COBieAttributeState.System, "ExtSystem", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtSystem
        {
            get { return _extSystem; }
            set
            {
                _extSystem = value;
            }
        }

        [COBieAttributes(8, COBieKeyType.None, COBieAttributeState.System, "ExtObject", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtObject
        {
            get { return _extObject; }
            set
            {
                _extObject = value;
            }
        }

        [COBieAttributes(9, COBieKeyType.None, COBieAttributeState.As_Specified, "ExtIdentifier", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtIdentifier
        {
            get { return _extIdentifier; }
            set
            {
                _extIdentifier = value;
            }
        }

        [COBieAttributes(10, COBieKeyType.None, COBieAttributeState.As_Specified, "Description", 255, COBieAllowedType.AlphaNumeric)]
        public string Description
        {
            get { return _description; }
            set
            {
                _description = value;
            }
        }




    }
}
