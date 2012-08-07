using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Xbim.COBie.Rows
{
    [Serializable()]
    public class COBieImpactRow : COBieRow
    {
        public COBieImpactRow()
        {
            Columns = new Dictionary<int, COBieColumn>();
            //Properties = typeof(COBieImpact).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Properties = typeof(COBieImpactRow).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // add column info 
            foreach (PropertyInfo propInfo in Properties)
            {
                object[] attrs = propInfo.GetCustomAttributes(typeof(COBieAttributes), true);
                if (attrs != null && attrs.Length > 0)
                {
                    Columns.Add(((COBieAttributes)attrs[0]).Order, new COBieColumn(((COBieAttributes)attrs[0]).ColumnName, ((COBieAttributes)attrs[0]).MaxLength, ((COBieAttributes)attrs[0]).AllowedType, ((COBieAttributes)attrs[0]).KeyType));
                }
            }
        }


        [COBieAttributes(0, COBieKeyType.CompoundKey, COBieAttributeState.Required, "Name", 255, COBieAllowedType.AlphaNumeric)]
        public string Name { get; set; }

        [COBieAttributes(1, COBieKeyType.None, COBieAttributeState.Required, "CreatedBy", 255, COBieAllowedType.Email)]
        public string CreatedBy { get; set; }

        [COBieAttributes(2, COBieKeyType.None, COBieAttributeState.Required, "CreatedOn", 19, COBieAllowedType.ISODate)]
        public string CreatedOn { get; set; }

        [COBieAttributes(3, COBieKeyType.CompoundKey, COBieAttributeState.Required, "ImpactType", 255, COBieAllowedType.Text)]
        public string ImpactType { get; set; }

        [COBieAttributes(4, COBieKeyType.CompoundKey, COBieAttributeState.Required, "ImpactStage", 255, COBieAllowedType.Text)]
        public string ImpactStage { get; set; }

        [COBieAttributes(5, COBieKeyType.CompoundKey, COBieAttributeState.Required, "SheetName", 255, COBieAllowedType.Text)]
        public string SheetName { get; set; }

        [COBieAttributes(6, COBieKeyType.CompoundKey, COBieAttributeState.Required, "RowName", 255, COBieAllowedType.AlphaNumeric)]
        public string RowName { get; set; }

        [COBieAttributes(7, COBieKeyType.None, COBieAttributeState.Required, "Value", 255, COBieAllowedType.Numeric)]
        public string Value { get; set; }

        [COBieAttributes(8, COBieKeyType.None, COBieAttributeState.Required, "ImpactUnit", 255, COBieAllowedType.Text)]
        public string ImpactUnit { get; set; }

        [COBieAttributes(9, COBieKeyType.None, COBieAttributeState.As_Specified, "LeadInTime", sizeof(double), COBieAllowedType.Numeric)]
        public string LeadInTime { get; set; }

        [COBieAttributes(10, COBieKeyType.None, COBieAttributeState.As_Specified, "Duration", sizeof(double), COBieAllowedType.Numeric)]
        public string Duration { get; set; }

        [COBieAttributes(11, COBieKeyType.None, COBieAttributeState.As_Specified, "LeadOutTime", sizeof(double), COBieAllowedType.Numeric)]
        public string LeadOutTime { get; set; }

        [COBieAttributes(12, COBieKeyType.None, COBieAttributeState.System, "ExtSystem", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtSystem { get; set; }

        [COBieAttributes(13, COBieKeyType.None, COBieAttributeState.System, "ExtObject", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtObject { get; set; }

        [COBieAttributes(14, COBieKeyType.None, COBieAttributeState.System, "ExtIdentifier", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtIdentifier { get; set; }

        [COBieAttributes(15, COBieKeyType.None, COBieAttributeState.As_Specified, "Description", 255, COBieAllowedType.AlphaNumeric)]
        public string Description { get; set; }
    }
}
