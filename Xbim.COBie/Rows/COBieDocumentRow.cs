using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Xbim.COBie.Rows
{
    [Serializable()]
    public class COBieDocumentRow : COBieRow
    {
        public COBieDocumentRow(ICOBieSheet<COBieDocumentRow> parentSheet)
            : base(parentSheet) { }

        [COBieAttributes(0, COBieKeyType.CompoundKey, "", COBieAttributeState.Required, "Name", 255, COBieAllowedType.AlphaNumeric)]
        public string Name { get; set; }

        [COBieAttributes(1, COBieKeyType.None, "", COBieAttributeState.Required, "CreatedBy", 255, COBieAllowedType.Email)]
        public string CreatedBy { get; set; }

        [COBieAttributes(2, COBieKeyType.None, "", COBieAttributeState.Required, "CreatedOn", 19, COBieAllowedType.ISODate)]
        public string CreatedOn { get; set; }

        [COBieAttributes(3, COBieKeyType.None, "", COBieAttributeState.Required, "Category", 255, COBieAllowedType.Text)]
        public string Category { get; set; }

        [COBieAttributes(4, COBieKeyType.None, "", COBieAttributeState.Required, "ApprovalBy", 255, COBieAllowedType.Text)]
        public string ApprovalBy { get; set; }

        [COBieAttributes(5, COBieKeyType.CompoundKey, "", COBieAttributeState.Required, "Stage", 255, COBieAllowedType.Text)]
        public string Stage { get; set; }

        [COBieAttributes(6, COBieKeyType.CompoundKey, "", COBieAttributeState.Required, "SheetName", 255, COBieAllowedType.Text)]
        public string SheetName { get; set; }

        [COBieAttributes(7, COBieKeyType.CompoundKey, "", COBieAttributeState.Required, "RowName", 255, COBieAllowedType.AlphaNumeric)]
        public string RowName { get; set; }

        [COBieAttributes(8, COBieKeyType.None, "", COBieAttributeState.Required, "Directory", 255, COBieAllowedType.AlphaNumeric)]
        public string Directory { get; set; }

        [COBieAttributes(9, COBieKeyType.None, "", COBieAttributeState.Required, "File", 255, COBieAllowedType.AlphaNumeric)]
        public string File { get; set; }

        [COBieAttributes(10, COBieKeyType.None, "", COBieAttributeState.System, "ExtSystem", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtSystem { get; set; }

        [COBieAttributes(11, COBieKeyType.None, "", COBieAttributeState.System, "ExtObject", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtObject { get; set; }

        [COBieAttributes(12, COBieKeyType.None, "", COBieAttributeState.System, "ExtIdentifier", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtIdentifier { get; set; }

        [COBieAttributes(13, COBieKeyType.None, "", COBieAttributeState.As_Specified, "Description", 255, COBieAllowedType.AlphaNumeric)]
        public string Description { get; set; }

        [COBieAttributes(14, COBieKeyType.None, "", COBieAttributeState.As_Specified, "Reference", 255, COBieAllowedType.AlphaNumeric)]
        public string Reference { get; set; }
    }
}
