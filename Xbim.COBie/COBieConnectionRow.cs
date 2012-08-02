using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.COBieExtensions;
using System.Reflection;

namespace Xbim.COBie
{
    [Serializable()]
    public class COBieConnectionRow : COBieRow
    {
        static COBieConnectionRow()
        {
            _columns = new Dictionary<int, COBieColumn>();
            //Properties = typeof(COBieConnection).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Properties = typeof(COBieConnectionRow).GetProperties(BindingFlags.Public | BindingFlags.Instance);

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

        [COBieAttributes(3, COBieKeyType.CompoundKey, COBieAttributeState.Required, "ConnectionType", 255, COBieAllowedType.Text)]
        public string ConnectionType { get; set; }

        [COBieAttributes(4, COBieKeyType.None, COBieAttributeState.None, "SheetName", 255, COBieAllowedType.Text)]
        public string SheetName { get; set; }

        [COBieAttributes(5, COBieKeyType.CompoundKey, COBieAttributeState.Required, "RowName1", 255, COBieAllowedType.Text)]
        public string RowName1 { get; set; }

        [COBieAttributes(6, COBieKeyType.CompoundKey, COBieAttributeState.Required, "RowName2", 255, COBieAllowedType.Text)]
        public string RowName2 { get; set; }

        [COBieAttributes(7, COBieKeyType.None, COBieAttributeState.None, "RealizingElement", 255, COBieAllowedType.Text)]
        public string RealizingElement { get; set; }

        [COBieAttributes(8, COBieKeyType.None, COBieAttributeState.None, "PortName1", 255, COBieAllowedType.Text)]
        public string PortName1 { get; set; }

        [COBieAttributes(9, COBieKeyType.None, COBieAttributeState.None, "PortName2", 255, COBieAllowedType.Text)]
        public string PortName2 { get; set; }

        [COBieAttributes(10, COBieKeyType.None, COBieAttributeState.System, "ExtSystem", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtSystem { get; set; }

        [COBieAttributes(11, COBieKeyType.None, COBieAttributeState.System, "ExtObject", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtObject { get; set; }

        [COBieAttributes(12, COBieKeyType.None, COBieAttributeState.System, "ExtIdentifier", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtIdentifier { get; set; }

        [COBieAttributes(13, COBieKeyType.None, COBieAttributeState.As_Specified, "Description", 255, COBieAllowedType.AlphaNumeric)]
        public string Description { get; set; }
    }
}
