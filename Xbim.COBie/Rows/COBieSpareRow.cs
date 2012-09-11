using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Xbim.COBie.Rows
{
    [Serializable()]
    public class COBieSpareRow : COBieRow
    {
        public COBieSpareRow(ICOBieSheet<COBieSpareRow> parentSheet)
            : base(parentSheet) { }

        [COBieAttributes(0, COBieKeyType.PrimaryKey, "", COBieAttributeState.Required, "Name", 255, COBieAllowedType.AlphaNumeric)]
        public string Name { get; set; }

        [COBieAttributes(1, COBieKeyType.None, "", COBieAttributeState.Required, "CreatedBy", 255, COBieAllowedType.Email)]
        public string CreatedBy { get; set; }

        [COBieAttributes(2, COBieKeyType.None, "", COBieAttributeState.Required, "CreatedOn", 19, COBieAllowedType.ISODate)]
        public string CreatedOn { get; set; }

        [COBieAttributes(3, COBieKeyType.None, "", COBieAttributeState.Required, "Category", 255, COBieAllowedType.Text)]
        public string Category { get; set; }

        [COBieAttributes(4, COBieKeyType.None, "", COBieAttributeState.Required, "TypeName", 255, COBieAllowedType.Text)]
        public string TypeName { get; set; }

        [COBieAttributes(5, COBieKeyType.None, "", COBieAttributeState.Required, "Suppliers", 255, COBieAllowedType.Email)]
        public string Suppliers { get; set; }

        [COBieAttributes(6, COBieKeyType.None, "", COBieAttributeState.System, "ExtSystem", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtSystem { get; set; }

        [COBieAttributes(7, COBieKeyType.None, "", COBieAttributeState.System, "ExtObject", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtObject { get; set; }

        [COBieAttributes(8, COBieKeyType.None, "", COBieAttributeState.System, "ExtIdentifier", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtIdentifier { get; set; }

        [COBieAttributes(9, COBieKeyType.None, "", COBieAttributeState.As_Specified, "Description", 255, COBieAllowedType.AlphaNumeric)]
        public string Description { get; set; }

        [COBieAttributes(10, COBieKeyType.None, "", COBieAttributeState.As_Specified, "SetNumber", 255, COBieAllowedType.AlphaNumeric)]
        public string SetNumber { get; set; }

        [COBieAttributes(11, COBieKeyType.None, "", COBieAttributeState.As_Specified, "PartNumber", 255, COBieAllowedType.AlphaNumeric)]
        public string PartNumber { get; set; }
    }
}
