using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Xbim.COBie.Rows
{
    [Serializable()]
    public class COBieIssueRow : COBieRow
    {
        public COBieIssueRow(ICOBieSheet<COBieIssueRow> parentSheet)
            : base(parentSheet) { }

        [COBieAttributes(0, COBieKeyType.CompoundKey, "", COBieAttributeState.Required, "Name", 255, COBieAllowedType.AlphaNumeric)]
        public string Name { get; set; }

        [COBieAttributes(1, COBieKeyType.ForeignKey, "Contact.Email", COBieAttributeState.Required, "CreatedBy", 255, COBieAllowedType.Email)]
        public string CreatedBy { get; set; }

        [COBieAttributes(2, COBieKeyType.None, "", COBieAttributeState.Required, "CreatedOn", 19, COBieAllowedType.ISODateTime)]
        public string CreatedOn { get; set; }

        [COBieAttributes(3, COBieKeyType.ForeignKey, "PickLists.IssueCategory", COBieAttributeState.Required, "Type", 255, COBieAllowedType.Text)]
        public string Type { get; set; }

        [COBieAttributes(4, COBieKeyType.ForeignKey, "PickLists.IssueRisk", COBieAttributeState.Required, "Risk", 255, COBieAllowedType.Text)]
        public string Risk { get; set; }

        [COBieAttributes(5, COBieKeyType.ForeignKey, "PickLists.IssueChance", COBieAttributeState.Required, "Chance", 255, COBieAllowedType.Text)]
        public string Chance { get; set; }

        [COBieAttributes(6, COBieKeyType.ForeignKey, "PickLists.IssueImpact", COBieAttributeState.Required, "Impact", 255, COBieAllowedType.Text)]
        public string Impact { get; set; }

        [COBieAttributes(7, COBieKeyType.CompoundKey_ForeignKey, "PickLists.SheetType", COBieAttributeState.Required, "SheetName1", 255, COBieAllowedType.Text)]
        public string SheetName1 { get; set; }

        [COBieAttributes(8, COBieKeyType.CompoundKey, "", COBieAttributeState.Required, "RowName1", 255, COBieAllowedType.AlphaNumeric)]
        public string RowName1 { get; set; }

        [COBieAttributes(9, COBieKeyType.CompoundKey_ForeignKey, "PickLists.SheetType", COBieAttributeState.Required, "SheetName2", 255, COBieAllowedType.Text)]
        public string SheetName2 { get; set; }

        [COBieAttributes(10, COBieKeyType.CompoundKey, "", COBieAttributeState.Required, "RowName2", 255, COBieAllowedType.AlphaNumeric)]
        public string RowName2 { get; set; }

        [COBieAttributes(11, COBieKeyType.None, "", COBieAttributeState.Required, "Description", 255, COBieAllowedType.AlphaNumeric)]
        public string Description { get; set; }

        [COBieAttributes(12, COBieKeyType.ForeignKey, "Contact.Email", COBieAttributeState.Required, "Owner", 255, COBieAllowedType.Email)]
        public string Owner { get; set; }

        [COBieAttributes(13, COBieKeyType.None, "", COBieAttributeState.Required, "Mitigation", 255, COBieAllowedType.AlphaNumeric)]
        public string Mitigation { get; set; }

        [COBieAttributes(14, COBieKeyType.None, "", COBieAttributeState.System, "ExtSystem", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtSystem { get; set; }

        [COBieAttributes(15, COBieKeyType.None, "", COBieAttributeState.System, "ExtObject", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtObject { get; set; }

        [COBieAttributes(16, COBieKeyType.None, "", COBieAttributeState.System, "ExtIdentifier", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtIdentifier { get; set; }
    }
}
