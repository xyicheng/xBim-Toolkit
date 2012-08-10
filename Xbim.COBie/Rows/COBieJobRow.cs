using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Xbim.COBie.Rows
{
    [Serializable()]
    public class COBieJobRow : COBieRow
    {
        public COBieJobRow(ICOBieSheet<COBieJobRow> parentSheet)
            : base(parentSheet) { } 

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
