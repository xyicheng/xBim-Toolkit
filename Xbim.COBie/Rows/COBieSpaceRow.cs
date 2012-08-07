using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using System.Reflection;

namespace Xbim.COBie.Rows
{
    [Serializable()]
    public class COBieSpaceRow : COBieRow
    {
        public COBieSpaceRow()
        {
            Columns = new Dictionary<int, COBieColumn>();
            Properties = typeof(COBieSpaceRow).GetProperties(BindingFlags.Public | BindingFlags.Instance);

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

        [COBieAttributes(0, COBieKeyType.PrimaryKey, COBieAttributeState.Required, "Name", 255, COBieAllowedType.AlphaNumeric)]
        public string Name { get; set; }

        [COBieAttributes(1, COBieKeyType.None, COBieAttributeState.Required, "CreatedBy", 255, COBieAllowedType.Email)]
        public string CreatedBy { get; set; }

        [COBieAttributes(2, COBieKeyType.None, COBieAttributeState.Required, "CreatedOn", 19, COBieAllowedType.ISODate)]
        public string CreatedOn { get; set; }

        [COBieAttributes(3, COBieKeyType.None, COBieAttributeState.Required, "Category", 255, COBieAllowedType.AlphaNumeric)]
        public string Category { get; set; }

        [COBieAttributes(4, COBieKeyType.None, COBieAttributeState.Required, "FloorName", 255, COBieAllowedType.AlphaNumeric)]
        public string FloorName { get; set; }

        [COBieAttributes(5, COBieKeyType.None, COBieAttributeState.Required, "Description", 255, COBieAllowedType.AlphaNumeric)]
        public string Description { get; set; }

        [COBieAttributes(6, COBieKeyType.None, COBieAttributeState.System, "ExtSystem", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtSystem { get; set; }

        [COBieAttributes(7, COBieKeyType.None, COBieAttributeState.System, "ExtObject", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtObject { get; set; }

        [COBieAttributes(8, COBieKeyType.None, COBieAttributeState.System, "ExtIdentifier", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtIdentifier { get; set; }

        [COBieAttributes(9, COBieKeyType.None, COBieAttributeState.As_Specified, "RoomTag", 255, COBieAllowedType.AlphaNumeric)]
        public string RoomTag { get; set; }

        [COBieAttributes(10, COBieKeyType.None, COBieAttributeState.As_Specified, "UsableHeight", sizeof(double), COBieAllowedType.Numeric)]
        public string UsableHeight { get; set; }

        [COBieAttributes(11, COBieKeyType.None, COBieAttributeState.As_Specified, "GrossArea", sizeof(double), COBieAllowedType.Numeric)]
        public string GrossArea { get; set; }

        [COBieAttributes(12, COBieKeyType.None, COBieAttributeState.As_Specified, "NetArea", sizeof(double), COBieAllowedType.Numeric)]
        public string NetArea { get; set; }
    }
}
