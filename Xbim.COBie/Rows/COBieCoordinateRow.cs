using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Xbim.COBie.Rows
{
    [Serializable()]
    public class COBieCoordinateRow : COBieRow
    {
        static COBieCoordinateRow()
        {
            _columns = new Dictionary<int, COBieColumn>();
            Properties = typeof(COBieCoordinateRow).GetProperties(BindingFlags.Public | BindingFlags.Instance);

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

        [COBieAttributes(0, COBieKeyType.CompoundKey, COBieAttributeState.Required, "Name", 255, COBieAllowedType.AlphaNumeric)]
        public string Name { get; set; }

        [COBieAttributes(1, COBieKeyType.None, COBieAttributeState.Required, "CreatedBy", 255, COBieAllowedType.Email)]
        public string CreatedBy { get; set; }

        [COBieAttributes(2, COBieKeyType.None, COBieAttributeState.Required, "CreatedOn", 19, COBieAllowedType.ISODate)]
        public string CreatedOn { get; set; }

        [COBieAttributes(3, COBieKeyType.CompoundKey, COBieAttributeState.Required, "Category", 255, COBieAllowedType.AlphaNumeric)]
        public string Category { get; set; }

        [COBieAttributes(4, COBieKeyType.CompoundKey, COBieAttributeState.Required, "SheetName", 255, COBieAllowedType.AlphaNumeric)]
        public string SheetName { get; set; }


        [COBieAttributes(5, COBieKeyType.CompoundKey, COBieAttributeState.Required, "RowName", 255, COBieAllowedType.AlphaNumeric)]
        public string RowName { get; set; }

        [COBieAttributes(6, COBieKeyType.None, COBieAttributeState.Required, "CoordinateXAxis", sizeof(double), COBieAllowedType.AlphaNumeric)]
        public string CoordinateXAxis { get; set; }

        [COBieAttributes(7, COBieKeyType.None, COBieAttributeState.Required, "CoordinateYAxis", sizeof(double), COBieAllowedType.AlphaNumeric)]
        public string CoordinateYAxis { get; set; }

        [COBieAttributes(8, COBieKeyType.None, COBieAttributeState.Required, "CoordinateZAxis", sizeof(double), COBieAllowedType.AlphaNumeric)]
        public string CoordinateZAxis { get; set; }

        [COBieAttributes(9, COBieKeyType.None, COBieAttributeState.System, "ExtSystem", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtSystem { get; set; }

        [COBieAttributes(10, COBieKeyType.None, COBieAttributeState.System, "ExtObject", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtObject { get; set; }

        [COBieAttributes(11, COBieKeyType.None, COBieAttributeState.System, "ExtIdentifier", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtIdentifier { get; set; }

        [COBieAttributes(12, COBieKeyType.None, COBieAttributeState.As_Specified, "ClockwiseRotation", 255, COBieAllowedType.AlphaNumeric)]
        public string ClockwiseRotation { get; set; }

        [COBieAttributes(13, COBieKeyType.None, COBieAttributeState.As_Specified, "ElevationalRotation", 255, COBieAllowedType.AlphaNumeric)]
        public string ElevationalRotation { get; set; }

        [COBieAttributes(14, COBieKeyType.None, COBieAttributeState.As_Specified, "YawRotation", 255, COBieAllowedType.AlphaNumeric)]
        public string YawRotation { get; set; }
    }
}
