using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Xbim.COBie.Rows
{
    [Serializable()]
    public class COBiePickListsRow : COBieRow
    {
        public COBiePickListsRow(ICOBieSheet<COBiePickListsRow> parentSheet)
            : base(parentSheet) { }

        [COBieAttributes(0, COBieKeyType.None, "", COBieAttributeState.Required, "ApprovalBy", 255, COBieAllowedType.AlphaNumeric)]
        public string ApprovalBy { get; set; }

        [COBieAttributes(1, COBieKeyType.None, "", COBieAttributeState.Required, "AreaUnit", 255, COBieAllowedType.AlphaNumeric)]
        public string AreaUnit { get; set; }

        [COBieAttributes(2, COBieKeyType.None, "", COBieAttributeState.Required, "AssetType", 255, COBieAllowedType.AlphaNumeric)]
        public string AssetType { get; set; }

        [COBieAttributes(3, COBieKeyType.PrimaryKey, "", COBieAttributeState.Required, "Category-Facility", 255, COBieAllowedType.AlphaNumeric)]
        public string CategoryFacility { get; set; }

        [COBieAttributes(4, COBieKeyType.PrimaryKey, "", COBieAttributeState.Required, "Category-Space", 255, COBieAllowedType.AlphaNumeric)]
        public string CategorySpace { get; set; }

        [COBieAttributes(5, COBieKeyType.PrimaryKey, "", COBieAttributeState.Required, "Category-Element", 255, COBieAllowedType.AlphaNumeric)]
        public string CategoryElement { get; set; }

        [COBieAttributes(6, COBieKeyType.PrimaryKey, "", COBieAttributeState.Required, "Category-Product", 255, COBieAllowedType.AlphaNumeric)]
        public string CategoryProduct { get; set; }

        [COBieAttributes(7, COBieKeyType.PrimaryKey, "", COBieAttributeState.Required, "Category-Role", 255, COBieAllowedType.AlphaNumeric)]
        public string CategoryRole { get; set; }

        [COBieAttributes(8, COBieKeyType.None, "", COBieAttributeState.Required, "CoordinateSheet", 255, COBieAllowedType.AlphaNumeric)]
        public string CoordinateSheet { get; set; }

        [COBieAttributes(9, COBieKeyType.None, "", COBieAttributeState.Required, "ConnectionType", 255, COBieAllowedType.AlphaNumeric)]
        public string ConnectionType { get; set; }

        [COBieAttributes(10, COBieKeyType.None, "", COBieAttributeState.Required, "CoordinateType", 255, COBieAllowedType.AlphaNumeric)]
        public string CoordinateType { get; set; }

        [COBieAttributes(11, COBieKeyType.None, "", COBieAttributeState.Required, "DocumentType", 255, COBieAllowedType.AlphaNumeric)]
        public string DocumentType { get; set; }

        [COBieAttributes(12, COBieKeyType.None, "", COBieAttributeState.Required, "DurationUnit", 255, COBieAllowedType.AlphaNumeric)]
        public string DurationUnit { get; set; }

        [COBieAttributes(13, COBieKeyType.None, "", COBieAttributeState.Required, "FloorType", 255, COBieAllowedType.AlphaNumeric)]
        public string FloorType { get; set; }

        [COBieAttributes(14, COBieKeyType.None, "", COBieAttributeState.Required, "IssueCategory", 255, COBieAllowedType.AlphaNumeric)]
        public string IssueCategory { get; set; }

        [COBieAttributes(15, COBieKeyType.None, "", COBieAttributeState.Required, "IssueChance", 255, COBieAllowedType.AlphaNumeric)]
        public string IssueChance { get; set; }

        [COBieAttributes(16, COBieKeyType.None, "", COBieAttributeState.Required, "IssueImpact", 255, COBieAllowedType.AlphaNumeric)]
        public string IssueImpact { get; set; }

        [COBieAttributes(17, COBieKeyType.None, "", COBieAttributeState.Required, "IssueRisk", 255, COBieAllowedType.AlphaNumeric)]
        public string IssueRisk { get; set; }

        [COBieAttributes(18, COBieKeyType.None, "", COBieAttributeState.Required, "JobStatusType", 255, COBieAllowedType.AlphaNumeric)]
        public string JobStatusType { get; set; }

        [COBieAttributes(19, COBieKeyType.None, "", COBieAttributeState.Required, "JobType", 255, COBieAllowedType.AlphaNumeric)]
        public string JobType { get; set; }

        [COBieAttributes(20, COBieKeyType.None, "", COBieAttributeState.Required, "ObjAttribute", 255, COBieAllowedType.AlphaNumeric)]
        public string ObjAttribute { get; set; }

        [COBieAttributes(21, COBieKeyType.None, "", COBieAttributeState.Required, "ObjAttributeType", 255, COBieAllowedType.AlphaNumeric)]
        public string ObjAttributeType { get; set; }

        [COBieAttributes(22, COBieKeyType.None, "", COBieAttributeState.Required, "ObjComponent", 255, COBieAllowedType.AlphaNumeric)]
        public string ObjComponent { get; set; }

        [COBieAttributes(23, COBieKeyType.None, "", COBieAttributeState.Required, "ObjConnection", 255, COBieAllowedType.AlphaNumeric)]
        public string ObjConnection { get; set; }

        [COBieAttributes(24, COBieKeyType.None, "", COBieAttributeState.Required, "ObjContact", 255, COBieAllowedType.AlphaNumeric)]
        public string ObjContact { get; set; }

        [COBieAttributes(25, COBieKeyType.None, "", COBieAttributeState.Required, "ObjCoordinate", 255, COBieAllowedType.AlphaNumeric)]
        public string ObjCoordinate { get; set; }

        [COBieAttributes(26, COBieKeyType.None, "", COBieAttributeState.Required, "ObjDocument", 255, COBieAllowedType.AlphaNumeric)]
        public string ObjDocument { get; set; }

        [COBieAttributes(27, COBieKeyType.None, "", COBieAttributeState.Required, "ObjFacility", 255, COBieAllowedType.AlphaNumeric)]
        public string ObjFacility { get; set; }

        [COBieAttributes(28, COBieKeyType.None, "", COBieAttributeState.Required, "ObjFloor", 255, COBieAllowedType.AlphaNumeric)]
        public string ObjFloor { get; set; }

        [COBieAttributes(29, COBieKeyType.None, "", COBieAttributeState.Required, "ObjIssue", 255, COBieAllowedType.AlphaNumeric)]
        public string ObjIssue { get; set; }

        [COBieAttributes(30, COBieKeyType.None, "", COBieAttributeState.Required, "ObjJob", 255, COBieAllowedType.AlphaNumeric)]
        public string ObjJob { get; set; }

        [COBieAttributes(31, COBieKeyType.None, "", COBieAttributeState.Required, "ObjProject", 255, COBieAllowedType.AlphaNumeric)]
        public string ObjProject { get; set; }

        [COBieAttributes(32, COBieKeyType.None, "", COBieAttributeState.Required, "ObjResource", 255, COBieAllowedType.AlphaNumeric)]
        public string ObjResource { get; set; }

        [COBieAttributes(33, COBieKeyType.None, "", COBieAttributeState.Required, "ObjSite", 255, COBieAllowedType.AlphaNumeric)]
        public string ObjSite { get; set; }

        [COBieAttributes(34, COBieKeyType.None, "", COBieAttributeState.Required, "ObjSpace", 255, COBieAllowedType.AlphaNumeric)]
        public string ObjSpace { get; set; }

        [COBieAttributes(35, COBieKeyType.None, "", COBieAttributeState.Required, "ObjSpare", 255, COBieAllowedType.AlphaNumeric)]
        public string ObjSpare { get; set; }

        [COBieAttributes(36, COBieKeyType.None, "", COBieAttributeState.Required, "ObjSystem", 255, COBieAllowedType.AlphaNumeric)]
        public string ObjSystem { get; set; }

        [COBieAttributes(37, COBieKeyType.None, "", COBieAttributeState.Required, "ObjType", 255, COBieAllowedType.AlphaNumeric)]
        public string ObjType { get; set; }

        [COBieAttributes(38, COBieKeyType.None, "", COBieAttributeState.Required, "ObjWarranty", 255, COBieAllowedType.AlphaNumeric)]
        public string ObjWarranty { get; set; }

        [COBieAttributes(39, COBieKeyType.None, "", COBieAttributeState.Required, "ObjZone", 255, COBieAllowedType.AlphaNumeric)]
        public string ObjZone { get; set; }

        [COBieAttributes(40, COBieKeyType.None, "", COBieAttributeState.Required, "ResourceType", 255, COBieAllowedType.AlphaNumeric)]
        public string ResourceType { get; set; }

        [COBieAttributes(41, COBieKeyType.None, "", COBieAttributeState.Required, "SheetType", 255, COBieAllowedType.AlphaNumeric)]
        public string SheetType { get; set; }

        [COBieAttributes(42, COBieKeyType.None, "", COBieAttributeState.Required, "SpareType", 255, COBieAllowedType.AlphaNumeric)]
        public string SpareType { get; set; }

        [COBieAttributes(43, COBieKeyType.None, "", COBieAttributeState.Required, "StageType", 255, COBieAllowedType.AlphaNumeric)]
        public string StageType { get; set; }

        [COBieAttributes(44, COBieKeyType.None, "", COBieAttributeState.Required, "ZoneType", 255, COBieAllowedType.AlphaNumeric)]
        public string ZoneType { get; set; }

        [COBieAttributes(45, COBieKeyType.None, "", COBieAttributeState.Required, "LinearUnit", 255, COBieAllowedType.AlphaNumeric)]
        public string LinearUnit { get; set; }

        [COBieAttributes(46, COBieKeyType.None, "", COBieAttributeState.Required, "VolumeUnit", 255, COBieAllowedType.AlphaNumeric)]
        public string VolumeUnit { get; set; }

        [COBieAttributes(47, COBieKeyType.None, "", COBieAttributeState.Required, "CostUnit", 255, COBieAllowedType.AlphaNumeric)]
        public string CostUnit { get; set; }

        [COBieAttributes(48, COBieKeyType.None, "", COBieAttributeState.Required, "AssemblyType", 255, COBieAllowedType.AlphaNumeric)]
        public string AssemblyType { get; set; }

        [COBieAttributes(49, COBieKeyType.None, "", COBieAttributeState.Required, "ImpactType", 255, COBieAllowedType.AlphaNumeric)]
        public string ImpactType { get; set; }

        [COBieAttributes(50, COBieKeyType.None, "", COBieAttributeState.Required, "ImpactStage", 255, COBieAllowedType.AlphaNumeric)]
        public string ImpactStage { get; set; }

        [COBieAttributes(51, COBieKeyType.None, "", COBieAttributeState.Required, "ImpactUnit", 255, COBieAllowedType.AlphaNumeric)]
        public string ImpactUnit { get; set; }

        [COBieAttributes(52, COBieKeyType.None, "", COBieAttributeState.Required, "ObjAssembly", 255, COBieAllowedType.AlphaNumeric)]
        public string ObjAssembly { get; set; }

        [COBieAttributes(53, COBieKeyType.None, "", COBieAttributeState.Required, "ObjImpact", 255, COBieAllowedType.AlphaNumeric)]
        public string ObjImpact { get; set; }

    }
}
