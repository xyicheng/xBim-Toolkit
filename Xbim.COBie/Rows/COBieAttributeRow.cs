using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Xbim.COBie.Rows
{
     [Serializable()]
    public class COBieAttributeRow : COBieRow
    {
         public COBieAttributeRow(ICOBieSheet<COBieAttributeRow> parentSheet)
            : base(parentSheet) { }

        [COBieAttributes(0, COBieKeyType.CompoundKey, "", COBieAttributeState.Required, "Name", 255, COBieAllowedType.AlphaNumeric)]
        public string Name  { get; set; }

        [COBieAttributes(1, COBieKeyType.ForeignKey, "Contact.Email", COBieAttributeState.Required, "CreatedBy", 255, COBieAllowedType.Email)]
        public string CreatedBy { get; set; }

        [COBieAttributes(2, COBieKeyType.None, "", COBieAttributeState.Required, "CreatedOn", 19, COBieAllowedType.ISODateTime)]
        public string CreatedOn { get; set; }

        [COBieAttributes(3, COBieKeyType.ForeignKey, "PickLists.StageType", COBieAttributeState.Required, "Category", 255, COBieAllowedType.AlphaNumeric)]
        public string Category { get; set; }

        [COBieAttributes(4, COBieKeyType.CompoundKey_ForeignKey, "PickLists.SheetType", COBieAttributeState.Required, "SheetName", 255, COBieAllowedType.AlphaNumeric)]
        public string SheetName { get; set; }

        [COBieAttributes(5, COBieKeyType.CompoundKey, "", COBieAttributeState.Required, "RowName", 255, COBieAllowedType.AlphaNumeric)]
        public string RowName { get; set; }

        [COBieAttributes(6, COBieKeyType.None, "", COBieAttributeState.Required, "Value", 255, COBieAllowedType.AlphaNumeric)]
        public string Value { get; set; }

        [COBieAttributes(7, COBieKeyType.None, "", COBieAttributeState.Required, "Unit", 255, COBieAllowedType.AlphaNumeric)]
        public string Unit { get; set; }

        [COBieAttributes(8, COBieKeyType.None, "", COBieAttributeState.System, "ExtSystem", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtSystem { get; set; }

        [COBieAttributes(9, COBieKeyType.None, "", COBieAttributeState.System, "ExtObject", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtObject { get; set; }

        [COBieAttributes(10, COBieKeyType.None, "", COBieAttributeState.System, "ExtIdentifier", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtIdentifier { get; set; }

        [COBieAttributes(11, COBieKeyType.None, "", COBieAttributeState.System, "Description", 255, COBieAllowedType.AlphaNumeric)]
        public string Description { get; set; }

        [COBieAttributes(12, COBieKeyType.None, "", COBieAttributeState.System, "AllowedValues", 255, COBieAllowedType.AlphaNumeric)]
        public string AllowedValues { get; set; }

    }
}
