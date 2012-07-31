using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.COBieExtensions;
using System.Reflection;

namespace Xbim.COBie
{
    [Serializable()]
    public class COBieIssueRow : COBieRow
    {
        static COBieIssueRow()
        {
            _columns = new Dictionary<int, COBieColumn>();
            //Properties = typeof(COBieIssue).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Properties = typeof(COBieIssueRow).GetProperties(BindingFlags.Public | BindingFlags.Instance);

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

        public COBieIssueRow()
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




        [COBieAttributes(0, COBieKeyType.CompoundKey, COBieAttributeState.Required, "Name", 255, COBieAllowedType.AlphaNumeric)]
        public string Name { get; set; }

        [COBieAttributes(1, COBieKeyType.None, COBieAttributeState.Required, "CreatedBy", 255, COBieAllowedType.Email)]
        public string CreatedBy { get; set; }

        [COBieAttributes(2, COBieKeyType.None, COBieAttributeState.Required, "CreatedOn", 19, COBieAllowedType.ISODate)]
        public string CreatedOn { get; set; }

        [COBieAttributes(3, COBieKeyType.None, COBieAttributeState.Required, "Type", 255, COBieAllowedType.Text)]
        public string Type { get; set; }

        [COBieAttributes(4, COBieKeyType.None, COBieAttributeState.Required, "Risk", 255, COBieAllowedType.Text)]
        public string Risk { get; set; }

        [COBieAttributes(5, COBieKeyType.None, COBieAttributeState.Required, "Chance", 255, COBieAllowedType.Text)]
        public string Chance { get; set; }

        [COBieAttributes(6, COBieKeyType.None, COBieAttributeState.Required, "Impact", 255, COBieAllowedType.Text)]
        public string Impact { get; set; }

        [COBieAttributes(7, COBieKeyType.CompoundKey, COBieAttributeState.Required, "SheetName1", 255, COBieAllowedType.Text)]
        public string SheetName1 { get; set; }

        [COBieAttributes(8, COBieKeyType.CompoundKey, COBieAttributeState.Required, "RowName1", 255, COBieAllowedType.AlphaNumeric)]
        public string RowName1 { get; set; }

        [COBieAttributes(9, COBieKeyType.CompoundKey, COBieAttributeState.Required, "SheetName2", 255, COBieAllowedType.Text)]
        public string SheetName2 { get; set; }

        [COBieAttributes(10, COBieKeyType.CompoundKey, COBieAttributeState.Required, "RowName2", 255, COBieAllowedType.AlphaNumeric)]
        public string RowName2 { get; set; }

        [COBieAttributes(11, COBieKeyType.None, COBieAttributeState.Required, "Description", 255, COBieAllowedType.AlphaNumeric)]
        public string Description { get; set; }

        [COBieAttributes(12, COBieKeyType.None, COBieAttributeState.Required, "Owner", 255, COBieAllowedType.Email)]
        public string Owner { get; set; }

        [COBieAttributes(13, COBieKeyType.None, COBieAttributeState.Required, "Mitigation", 255, COBieAllowedType.AlphaNumeric)]
        public string Mitigation { get; set; }

        [COBieAttributes(14, COBieKeyType.None, COBieAttributeState.System, "ExtSystem", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtSystem { get; set; }

        [COBieAttributes(15, COBieKeyType.None, COBieAttributeState.System, "ExtObject", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtObject { get; set; }

        [COBieAttributes(16, COBieKeyType.None, COBieAttributeState.System, "ExtIdentifier", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtIdentifier { get; set; }
    }
}
