using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using Xbim.XbimExtensions;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ActorResource;
using System.Reflection;


namespace Xbim.COBie.Rows
{
    [Serializable()]
    public class COBieContactRow : COBieRow
    {
        public COBieContactRow(ICOBieSheet<COBieContactRow> parentSheet)
            : base(parentSheet) { }    

        [COBieAttributes(0, COBieKeyType.PrimaryKey, COBieAttributeState.Required, "Email", 255, COBieAllowedType.AlphaNumeric)]
        public string Email { get; set; }

        [COBieAttributes(1, COBieKeyType.None, COBieAttributeState.Required, "CreatedBy", 255, COBieAllowedType.Email)]
        public string CreatedBy { get; set; }

        [COBieAttributes(2, COBieKeyType.None, COBieAttributeState.Required, "CreatedOn", 19, COBieAllowedType.ISODate)]
        public string CreatedOn { get; set; }

        [COBieAttributes(3, COBieKeyType.None, COBieAttributeState.Required, "Category", 255, COBieAllowedType.AlphaNumeric)]
        public string Category { get; set; }

        [COBieAttributes(4, COBieKeyType.None, COBieAttributeState.Required, "Company", 255, COBieAllowedType.AlphaNumeric)]
        public string Company { get; set; }

        [COBieAttributes(5, COBieKeyType.None, COBieAttributeState.Required, "Phone", 255, COBieAllowedType.AlphaNumeric)]
        public string Phone { get; set; }

        [COBieAttributes(6, COBieKeyType.None, COBieAttributeState.System, "ExternalSystem", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtSystem { get; set; }

        [COBieAttributes(7, COBieKeyType.None, COBieAttributeState.System, "ExternalObject", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtObject { get; set; }

        [COBieAttributes(8, COBieKeyType.None, COBieAttributeState.System, "ExternalIdentifier", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtIdentifier { get; set; }

        [COBieAttributes(9, COBieKeyType.None, COBieAttributeState.As_Specified, "Department", 255, COBieAllowedType.AlphaNumeric)]
        public string Department { get; set; }

        [COBieAttributes(10, COBieKeyType.None, COBieAttributeState.As_Specified, "OrganizationCode", 255, COBieAllowedType.AlphaNumeric)]
        public string OrganizationCode { get; set; }

        [COBieAttributes(11, COBieKeyType.None, COBieAttributeState.As_Specified, "GivenName", 255, COBieAllowedType.AlphaNumeric)]
        public string GivenName { get; set; }

        [COBieAttributes(12, COBieKeyType.None, COBieAttributeState.As_Specified, "FamilyName", 255, COBieAllowedType.AlphaNumeric)]
        public string FamilyName { get; set; }

        [COBieAttributes(13, COBieKeyType.None, COBieAttributeState.As_Specified, "Street", 255, COBieAllowedType.AlphaNumeric)]
        public string Street { get; set; }

        [COBieAttributes(14, COBieKeyType.None, COBieAttributeState.As_Specified, "PostalBox", 255, COBieAllowedType.AlphaNumeric)]
        public string PostalBox { get; set; }

        [COBieAttributes(15, COBieKeyType.None, COBieAttributeState.As_Specified, "Town", 255, COBieAllowedType.AlphaNumeric)]
        public string Town { get; set; }

        [COBieAttributes(16, COBieKeyType.None, COBieAttributeState.As_Specified, "StateRegion", 255, COBieAllowedType.AlphaNumeric)]
        public string StateRegion { get; set; }

        [COBieAttributes(17, COBieKeyType.None, COBieAttributeState.As_Specified, "PostalCode", 255, COBieAllowedType.AlphaNumeric)]
        public string PostalCode { get; set; }

        [COBieAttributes(18, COBieKeyType.None, COBieAttributeState.As_Specified, "Country", 255, COBieAllowedType.AlphaNumeric)]
        public string Country { get; set; }

        
    }
}
