using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.UtilityResource;
using Xbim.Ifc.SelectTypes;
using System.Reflection;

namespace Xbim.COBie.Rows
{
    [Serializable()]
    public class COBieFacilityRow : COBieRow
    {
        IfcProject _ifcProject;
        IfcSite _ifcSite;
        IfcBuilding _ifcBuilding;
        IModel _model;

        static COBieFacilityRow()
        {
            _columns = new Dictionary<int, COBieColumn>();
            //Properties = typeof(COBieFacility).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Properties = typeof(COBieFacilityRow).GetProperties(BindingFlags.Public | BindingFlags.Instance);

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

        // TODO: Review this
        public void InitFacility(IModel model)
        {
            _model = model;
            _ifcProject = model.IfcProject;
            _ifcSite = model.InstancesOfType<IfcSite>().FirstOrDefault();
            _ifcBuilding = model.InstancesOfType<IfcBuilding>().FirstOrDefault();
            //if (_ifcProject != null)
            //    _ifcSite = _ifcProject.GetSites().FirstOrDefault();
            //else
            //    _ifcSite = model.InstancesOfType<IfcSite>().FirstOrDefault();
            //if (_ifcSite != null)
            //    _ifcBuilding = _ifcSite.GetBuildings().FirstOrDefault();
            //else
            //    _ifcBuilding = model.InstancesOfType<IfcBuilding>().FirstOrDefault();

           
        }

        




        [COBieAttributes(0, COBieKeyType.PrimaryKey, COBieAttributeState.Required, "Name", 255, COBieAllowedType.AlphaNumeric)]
        public string Name { get; set; }

        [COBieAttributes(1, COBieKeyType.None, COBieAttributeState.Required, "CreatedBy", 255, COBieAllowedType.Email)]
        public string CreatedBy { get; set; }

        [COBieAttributes(2, COBieKeyType.None, COBieAttributeState.Required, "CreatedOn", 19, COBieAllowedType.ISODate)]
        public string CreatedOn { get; set; }

        [COBieAttributes(3, COBieKeyType.None, COBieAttributeState.Required, "Category", 255, COBieAllowedType.Text)]
        public string Category { get; set; }

        [COBieAttributes(4, COBieKeyType.None, COBieAttributeState.Required, "ProjectName", 255, COBieAllowedType.AlphaNumeric)]
        public string ProjectName { get; set; }

        [COBieAttributes(5, COBieKeyType.None, COBieAttributeState.Required, "SiteName", 255, COBieAllowedType.AlphaNumeric)]
        public string SiteName { get; set; }

        [COBieAttributes(6, COBieKeyType.None, COBieAttributeState.Required, "LinearUnits", 255, COBieAllowedType.Text)]
        public string LinearUnits { get; set; }

        [COBieAttributes(7, COBieKeyType.None, COBieAttributeState.Required, "AreaUnits", 255, COBieAllowedType.Text)]
        public string AreaUnits { get; set; }

        [COBieAttributes(8, COBieKeyType.None, COBieAttributeState.Required, "VolumeUnits", 255, COBieAllowedType.Text)]
        public string VolumeUnits { get; set; }

        [COBieAttributes(9, COBieKeyType.None, COBieAttributeState.Required, "CurrencyUnit", 255, COBieAllowedType.Text)]
        public string CurrencyUnit { get; set; }

        [COBieAttributes(10, COBieKeyType.None, COBieAttributeState.Required, "AreaMeasurement", 255, COBieAllowedType.AlphaNumeric)]
        public string AreaMeasurement { get; set; }

        [COBieAttributes(11, COBieKeyType.None, COBieAttributeState.System, "ExternalSystem", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtSystem { get; set; }

        [COBieAttributes(12, COBieKeyType.None, COBieAttributeState.System, "ExternalProjectObject", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtProjectObject { get; set; }

        [COBieAttributes(13, COBieKeyType.None, COBieAttributeState.System, "ExternalProjectIdentifier", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtProjectIdentifier { get; set; }

        [COBieAttributes(14, COBieKeyType.None, COBieAttributeState.System, "ExternalSiteObject", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtSiteObject { get; set; }

        [COBieAttributes(15, COBieKeyType.None, COBieAttributeState.System, "ExternalSiteIdentifier", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtSiteIdentifier { get; set; }

        [COBieAttributes(16, COBieKeyType.None, COBieAttributeState.System, "ExternalFacilityObject", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtFacilityObject { get; set; }

        [COBieAttributes(17, COBieKeyType.None, COBieAttributeState.System, "ExternalFacilityIdentifier", 255, COBieAllowedType.AlphaNumeric)]
        public string ExtFacilityIdentifier { get; set; }

        [COBieAttributes(18, COBieKeyType.None, COBieAttributeState.As_Specified, "Description", 255, COBieAllowedType.AlphaNumeric)]
        public string Description { get; set; }

        [COBieAttributes(19, COBieKeyType.None, COBieAttributeState.As_Specified, "ProjectDescription", 255, COBieAllowedType.AlphaNumeric)]
        public string ProjectDescription { get; set; }

        [COBieAttributes(20, COBieKeyType.None, COBieAttributeState.As_Specified, "SiteDescription", 255, COBieAllowedType.AlphaNumeric)]
        public string SiteDescription { get; set; }

        [COBieAttributes(21, COBieKeyType.None, COBieAttributeState.As_Specified, "Phase", 255, COBieAllowedType.AlphaNumeric)]
        public string Phase { get; set; }

        
    }

    
}
