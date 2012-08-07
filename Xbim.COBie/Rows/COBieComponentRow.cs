using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Xbim.COBie.Rows
{
    [Serializable()]
    public class COBieComponentRow : COBieRow
    {
        public COBieComponentRow()
        {
            Columns = new Dictionary<int, COBieColumn>();
            Properties = typeof(COBieComponentRow).GetProperties(BindingFlags.Public | BindingFlags.Instance);

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


        [COBieAttributes(3, COBieKeyType.None, COBieAttributeState.Required, "TypeName", 255, COBieAllowedType.AlphaNumeric)]
        public string TypeName { get; set; }

        [COBieAttributes(4, COBieKeyType.None, COBieAttributeState.Required, "Space", 255, COBieAllowedType.AlphaNumeric)]
        public string Space { get; set; }

        [COBieAttributes(5, COBieKeyType.None, COBieAttributeState.Required, "Description", 255, COBieAllowedType.AlphaNumeric)]
        public string Description { get; set; }

        [COBieAttributes(6, COBieKeyType.None, COBieAttributeState.System, "ExtSystem", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtSystem { get; set; }

        [COBieAttributes(7, COBieKeyType.None, COBieAttributeState.System, "ExtObject", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtObject { get; set; }

        [COBieAttributes(8, COBieKeyType.None, COBieAttributeState.System, "ExtIdentifier", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtIdentifier { get; set; }

        [COBieAttributes(9, COBieKeyType.None, COBieAttributeState.Required, "SerialNumber", 255, COBieAllowedType.AlphaNumeric)]
        public string SerialNumber { get; set; }

        [COBieAttributes(10, COBieKeyType.None, COBieAttributeState.Required, "InstallationDate", 19, COBieAllowedType.ISODate)]
        public string InstallationDate { get; set; }

        [COBieAttributes(11, COBieKeyType.None, COBieAttributeState.Required, "WarrantyStartDate", 19, COBieAllowedType.ISODate)]
        public string WarrantyStartDate { get; set; }

        [COBieAttributes(12, COBieKeyType.None, COBieAttributeState.As_Specified, "TagNumber", 255, COBieAllowedType.AlphaNumeric)]
        public string TagNumber { get; set; }

        [COBieAttributes(13, COBieKeyType.None, COBieAttributeState.As_Specified, "BarCode", 255, COBieAllowedType.AlphaNumeric)]
        public string BarCode { get; set; }

        [COBieAttributes(14, COBieKeyType.None, COBieAttributeState.As_Specified, "AssetIdentifier", 255, COBieAllowedType.AlphaNumeric)]
        public string AssetIdentifier { get; set; }
    }
}
