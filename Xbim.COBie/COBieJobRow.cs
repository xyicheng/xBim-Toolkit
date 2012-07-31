using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.COBieExtensions;
using System.Reflection;

namespace Xbim.COBie
{
    [Serializable()]
    public class COBieJobRow : COBieRow
    {
        static COBieJobRow()
        {
            _columns = new Dictionary<int, COBieColumn>();
            //Properties = typeof(COBieJob).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Properties = typeof(COBieJobRow).GetProperties(BindingFlags.Public | BindingFlags.Instance);

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

        public COBieJobRow()
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

        [COBieAttributes(3, COBieKeyType.None, COBieAttributeState.Required, "Category", 255, COBieAllowedType.AlphaNumeric)]
        public string Category { get; set; }

        [COBieAttributes(4, COBieKeyType.None, COBieAttributeState.Required, "Status", 255, COBieAllowedType.AlphaNumeric)]
        public string Status { get; set; }

        [COBieAttributes(5, COBieKeyType.CompoundKey, COBieAttributeState.Required, "TypeName", 255, COBieAllowedType.AlphaNumeric)]
        public string TypeName { get; set; }

        [COBieAttributes(6, COBieKeyType.None, COBieAttributeState.Required, "Description", 255, COBieAllowedType.AlphaNumeric)]
        public string Description { get; set; }

        [COBieAttributes(7, COBieKeyType.None, COBieAttributeState.Required, "Duration", sizeof(double), COBieAllowedType.Numeric)]
        public string Duration { get; set; }

        [COBieAttributes(8, COBieKeyType.None, COBieAttributeState.Required, "DurationUnit", 255, COBieAllowedType.Text)]
        public string DurationUnit { get; set; }

        [COBieAttributes(9, COBieKeyType.None, COBieAttributeState.Required, "Start", sizeof(double), COBieAllowedType.Numeric)]
        public string Start { get; set; }

        [COBieAttributes(10, COBieKeyType.None, COBieAttributeState.Required, "TaskStartUnit", 255, COBieAllowedType.Text)]
        public string TaskStartUnit { get; set; }

        [COBieAttributes(11, COBieKeyType.None, COBieAttributeState.Required, "Frequency", sizeof(double), COBieAllowedType.Numeric)]
        public string Frequency { get; set; }

        [COBieAttributes(12, COBieKeyType.None, COBieAttributeState.Required, "FrequencyUnit", 255, COBieAllowedType.Text)]
        public string FrequencyUnit { get; set; }

        [COBieAttributes(13, COBieKeyType.None, COBieAttributeState.Required, "ExtSystem", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtSystem { get; set; }

        [COBieAttributes(14, COBieKeyType.None, COBieAttributeState.Required, "ExtObject", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtObject { get; set; }

        [COBieAttributes(15, COBieKeyType.None, COBieAttributeState.Required, "ExtIdentifier", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtIdentifier { get; set; }

        [COBieAttributes(16, COBieKeyType.CompoundKey, COBieAttributeState.Required, "TaskNumber", 255, COBieAllowedType.AlphaNumeric)]
        public string TaskNumber { get; set; }

        [COBieAttributes(17, COBieKeyType.None, COBieAttributeState.Required, "Priors", 255, COBieAllowedType.Text)]
        public string Priors { get; set; }

        [COBieAttributes(18, COBieKeyType.None, COBieAttributeState.Required, "ResourceNames", 255, COBieAllowedType.Text)]
        public string ResourceNames { get; set; }
    }
}
