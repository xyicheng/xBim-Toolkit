using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.Ifc.ProductExtension;
using System.Reflection;

namespace Xbim.COBie.Rows
{
    [Serializable()]
    public class COBieFloorRow : COBieRow
    {
        public COBieFloorRow(ICOBieSheet<COBieFloorRow> parentSheet)
            : base(parentSheet) { }

        [COBieAttributes(0, COBieKeyType.PrimaryKey, "", COBieAttributeState.Required, "Name", 255, COBieAllowedType.AlphaNumeric)]
        public string Name { get; set; }

        [COBieAttributes(1, COBieKeyType.ForeignKey, "Contact.Email", COBieAttributeState.Required, "CreatedBy", 255, COBieAllowedType.Email)]
        public string CreatedBy { get; set; }

        [COBieAttributes(2, COBieKeyType.None, "", COBieAttributeState.Required, "CreatedOn", 19, COBieAllowedType.ISODate)]
        public string CreatedOn { get; set; }


        [COBieAttributes(3, COBieKeyType.ForeignKey, "PickLists.FloorType", COBieAttributeState.Required, "Category", 255, COBieAllowedType.AlphaNumeric)]
        public string Category { get; set; }

        [COBieAttributes(4, COBieKeyType.None, "", COBieAttributeState.Required, "ExtSystem", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtSystem { get; set; }

        [COBieAttributes(5, COBieKeyType.None, "", COBieAttributeState.Required, "ExtObject", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtObject { get; set; }

        [COBieAttributes(6, COBieKeyType.None, "", COBieAttributeState.Required, "ExtIdentifier", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtIdentifier { get; set; }

        [COBieAttributes(7, COBieKeyType.None, "", COBieAttributeState.As_Specified, "Description", 255, COBieAllowedType.AlphaNumeric)]
        public string Description { get; set; }

        [COBieAttributes(8, COBieKeyType.None, "", COBieAttributeState.As_Specified, "Elevation", sizeof(double), COBieAllowedType.Numeric)]
        public string Elevation { get; set; }

        [COBieAttributes(9, COBieKeyType.None, "", COBieAttributeState.As_Specified, "Height", sizeof(double), COBieAllowedType.Numeric)]
        public string Height { get; set; }
    }
}
