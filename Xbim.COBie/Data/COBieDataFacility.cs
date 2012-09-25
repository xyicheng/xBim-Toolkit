using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.Rows;
using Xbim.Ifc.ExternalReferenceResource;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;
using System.Globalization;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the Facility tab.
    /// </summary>
    public class COBieDataFacility : COBieData<COBieFacilityRow>, IAttributeProvider
    {
        /// <summary>
        /// Data Facility constructor
        /// </summary>
        /// <param name="model">The context of the model being generated</param>
        public COBieDataFacility(COBieContext context) : base(context)
        { }

      
        #region Methods

        /// <summary>
        /// Fill sheet rows for Facility sheet
        /// </summary>
        /// <returns>COBieSheet<COBieFacilityRow></returns>
        public override COBieSheet<COBieFacilityRow> Fill()
        {
            ProgressIndicator.ReportMessage("Starting Facilities...");

            //Create new sheet
            COBieSheet<COBieFacilityRow> facilities = new COBieSheet<COBieFacilityRow>(Constants.WORKSHEET_FACILITY);

            IfcProject ifcProject = Model.IfcProject;
            IfcSite ifcSite = Model.InstancesOfType<IfcSite>().FirstOrDefault();
            IfcBuilding ifcBuilding = Model.InstancesOfType<IfcBuilding>().FirstOrDefault();
            
            IfcElementQuantity ifcElementQuantity = Model.InstancesOfType<IfcElementQuantity>().FirstOrDefault();


            IEnumerable<IfcObject> ifcObjects = new List<IfcObject> { ifcProject, ifcSite, ifcBuilding }.AsEnumerable(); ;
            COBieDataPropertySetValues allPropertyValues = new COBieDataPropertySetValues(ifcObjects); //properties helper class

            //list of attributes to exclude form attribute sheet
            List<string> excludePropertyValueNames = new List<string> { "Phase" };
            List<string> excludePropertyValueNamesWildcard = new List<string> { "Roomtag", "RoomTag", "Tag", "GSA BIM Area", "Length", "Width", "Height" };
            allPropertyValues.ExcludePropertyValueNames.AddRange(excludePropertyValueNames);
            allPropertyValues.ExcludePropertyValueNamesWildcard.AddRange(excludePropertyValueNamesWildcard);
            allPropertyValues.RowParameters["Sheet"] = "Facility";
            
            COBieFacilityRow facility = new COBieFacilityRow(facilities);

            string name = "";
            if (!string.IsNullOrEmpty(ifcProject.Name))
                name = ifcProject.Name;
            else if (!string.IsNullOrEmpty(ifcSite.Name))
                name = ifcSite.Name;
            else if (!string.IsNullOrEmpty(ifcBuilding.Name))
                name = ifcBuilding.Name;

            facility.Name = (string.IsNullOrEmpty(name)) ? "The Facility Name Here" : name;

            facility.CreatedBy = GetTelecomEmailAddress(ifcBuilding.OwnerHistory);
            facility.CreatedOn = GetCreatedOnDateAsFmtString(ifcBuilding.OwnerHistory);

            facility.Category = GetCategory(ifcBuilding);
            
            facility.ProjectName = GetFacilityProjectName(ifcProject);
            facility.SiteName = GetFacilitySiteName(ifcSite);

            string linearUnit = "";
            string areaUnit = "";
            string volumeUnit = "";
            string moneyUnit = "";
            GetGlobalUnits(out linearUnit, out areaUnit, out volumeUnit, out moneyUnit);
            facility.LinearUnits = linearUnit;
            facility.AreaUnits = areaUnit;
            facility.VolumeUnits = volumeUnit;
            facility.CurrencyUnit = moneyUnit;

            facility.AreaMeasurement = (ifcElementQuantity == null) ? "" : ifcElementQuantity.MethodOfMeasurement.ToString();
            facility.ExternalSystem = GetExternalSystem(ifcBuilding);

            facility.ExternalProjectObject = "IfcProject";
            facility.ExternalProjectIdentifier = ifcProject.GlobalId;

            facility.ExternalSiteObject = "IfcSite";
            facility.ExternalSiteIdentifier = ifcSite.GlobalId;

            facility.ExternalFacilityObject = "IfcBuilding";
            facility.ExternalFacilityIdentifier = ifcBuilding.GlobalId;

            facility.Description = GetFacilityDescription(ifcBuilding);
            facility.ProjectDescription = GetFacilityProjectDescription(ifcProject);
            facility.SiteDescription = GetFacilitySiteDescription(ifcSite);
            facility.Phase = (string.IsNullOrEmpty(Model.IfcProject.Phase.ToString())) ? DEFAULT_STRING : Model.IfcProject.Phase.ToString();

            facilities.Rows.Add(facility);

            
            //fill in the attribute information
            foreach (IfcObject ifcObject in ifcObjects) 
            {
                allPropertyValues.RowParameters["Name"] = facility.Name;
                allPropertyValues.RowParameters["CreatedBy"] = facility.CreatedBy;
                allPropertyValues.RowParameters["CreatedOn"] = facility.CreatedOn;
                allPropertyValues.RowParameters["ExtSystem"] = facility.ExternalSystem;
                allPropertyValues.PopulateAttributesRows(ifcObject, ref _attributes); //fill attribute sheet rows//pass data from this sheet info as Dictionary
            }

            ProgressIndicator.Finalise();
            return facilities;
        }

       

        private string GetFacilityDescription(IfcBuilding ifcBuilding)
        {
            if (ifcBuilding != null)
            {
                if (!string.IsNullOrEmpty(ifcBuilding.LongName)) return ifcBuilding.LongName;
                else if (!string.IsNullOrEmpty(ifcBuilding.Description)) return ifcBuilding.Description;
                else if (!string.IsNullOrEmpty(ifcBuilding.Name)) return ifcBuilding.Name;
            }
            return Constants.DEFAULT_STRING;
        }

        private string GetFacilityProjectDescription(IfcProject ifcProject)
        {
            if (ifcProject != null)
            {
                if (!string.IsNullOrEmpty(ifcProject.LongName)) return ifcProject.LongName;
                else if (!string.IsNullOrEmpty(ifcProject.Description)) return ifcProject.Description;
                else if (!string.IsNullOrEmpty(ifcProject.Name)) return ifcProject.Name;
            }
            return "Project Description";
        }

        private string GetFacilitySiteDescription(IfcSite ifcSite)
        {
            if (ifcSite != null)
            {
                if (!string.IsNullOrEmpty(ifcSite.LongName)) return ifcSite.LongName;
                else if (!string.IsNullOrEmpty(ifcSite.Description)) return ifcSite.Description;
                else if (!string.IsNullOrEmpty(ifcSite.Name)) return ifcSite.Name;
            }
            return "Site Description";
        }

        private string GetFacilitySiteName(IfcSite ifcSite)
        {
            if (ifcSite != null)
            {
                if (!string.IsNullOrEmpty(ifcSite.Name)) return ifcSite.Name;
                else if (!string.IsNullOrEmpty(ifcSite.LongName)) return ifcSite.LongName;
                else if (!string.IsNullOrEmpty(ifcSite.GlobalId)) return ifcSite.GlobalId;
            }
            return "Site Name";
        }

        private string GetFacilityProjectName(IfcProject ifcProject)
        {
            if (ifcProject != null)
            {
                if (!string.IsNullOrEmpty(ifcProject.Name)) return ifcProject.Name;
                else if (!string.IsNullOrEmpty(ifcProject.LongName)) return ifcProject.LongName;
                else if (!string.IsNullOrEmpty(ifcProject.GlobalId)) return ifcProject.GlobalId;
            }
            return "Site Name";
        }

        private void GetGlobalUnits(out string linearUnit, out string areaUnit, out string volumeUnit, out string moneyUnit)
        {
            linearUnit = "";
            areaUnit = "";
            volumeUnit = "";
            moneyUnit = "";
            foreach (IfcUnitAssignment ifcUnitAssignment in Model.InstancesOfType<IfcUnitAssignment>()) //loop all IfcUnitAssignment
            {
                foreach (IfcUnit ifcUnit in ifcUnitAssignment.Units) //loop the UnitSet
                {
                    IfcNamedUnit ifcNamedUnit = ifcUnit as IfcNamedUnit;
                    if (ifcNamedUnit != null)
                    {
                        if ((ifcNamedUnit.UnitType == IfcUnitEnum.LENGTHUNIT) && string.IsNullOrEmpty(linearUnit)) //we want length units until we have value
                            linearUnit = GetLinearLengthUnit(ifcUnit);

                        if ((ifcNamedUnit.UnitType == IfcUnitEnum.AREAUNIT) && string.IsNullOrEmpty(areaUnit)) //we want area units until we have value
                            areaUnit = GetUnderScoredUnit(ifcUnit);

                        if ((ifcNamedUnit.UnitType == IfcUnitEnum.VOLUMEUNIT) && string.IsNullOrEmpty(volumeUnit)) //we want volume units until we have value
                            volumeUnit = GetUnderScoredUnit(ifcUnit); 
                    }
                    //get the money unit
                    IfcMonetaryUnit ifcMonetaryUnit = ifcUnit as IfcMonetaryUnit;
                    if ((ifcMonetaryUnit != null) && string.IsNullOrEmpty(moneyUnit))
                        moneyUnit = MonetaryUnit(ifcMonetaryUnit);
                    else
                        moneyUnit = DEFAULT_STRING;
                }
            }
            
        }

        /// <summary>
        /// Extract the Linear Length unit
        /// </summary>
        /// <param name="ifcUnit">ifcUnit object to get unit name from</param>
        /// <returns>string holding length unit name</returns>
        private string GetLinearLengthUnit( IfcUnit ifcUnit)
        {
            string value = "";
            if (ifcUnit is IfcSIUnit)
            {
                IfcSIUnit ifcSIUnit = ifcUnit as IfcSIUnit;

                string prefixUnit = (ifcSIUnit.Prefix != null) ? ifcSIUnit.Prefix.ToString() : "";  //see IfcSIPrefix
                value = ifcSIUnit.Name.ToString();                                             //see IfcSIUnitName
                if (!string.IsNullOrEmpty(value)) value = prefixUnit + value; //combine to give length name

                if (!string.IsNullOrEmpty(value)) return value.ToLower();
            }
            if (ifcUnit is IfcConversionBasedUnit)
            {
                IfcConversionBasedUnit IfcConversionBasedUnit = ifcUnit as IfcConversionBasedUnit;
                value = (IfcConversionBasedUnit.Name != null) ? IfcConversionBasedUnit.Name.ToString() : "";
                if (!string.IsNullOrEmpty(value)) return value.ToLower();
            }

            return DEFAULT_STRING;
        }

        /// <summary>
        /// Extract the Area unit
        /// </summary>
        /// <param name="ifcUnit">ifcUnit object to get unit name from</param>
        /// <returns>string holding area unit name</returns>
        private string GetUnderScoredUnit(IfcUnit ifcUnit)
        {
            string value = "";
            string sqText = "";
            string prefixUnit = "";
                    
            if (ifcUnit is IfcSIUnit)
            {
                IfcSIUnit ifcSIUnit = ifcUnit as IfcSIUnit;

                prefixUnit = (ifcSIUnit.Prefix != null) ? ifcSIUnit.Prefix.ToString() : "";  //see IfcSIPrefix
                value = ifcSIUnit.Name.ToString();                                             //see IfcSIUnitName
                
                if (!string.IsNullOrEmpty(value))
                {
                    string[] split = value.Split('_');
                    if (split.Length > 1) sqText = split.First(); //see if _ delimited value such as SQUARE_METRE
                    value = sqText + prefixUnit + split.Last(); //combine to give full unit name
                }
                if (!string.IsNullOrEmpty(value)) return value.ToLower();
            }
            if (ifcUnit is IfcConversionBasedUnit)
            {
                IfcConversionBasedUnit IfcConversionBasedUnit = ifcUnit as IfcConversionBasedUnit;
                value = (IfcConversionBasedUnit.Name != null) ? IfcConversionBasedUnit.Name.ToString() : "";

                if (!string.IsNullOrEmpty(value))
                {
                    string[] split = value.Split('_');
                    if (split.Length > 1) sqText = split.First(); //see if _ delimited value such as SQUARE_METRE
                    value = sqText + split.Last(); //combine to give full unit name
                }
                if (!string.IsNullOrEmpty(value)) return value.ToLower();
            }

            return DEFAULT_STRING;
        }

        /// <summary>
        /// Get Monetary Unit
        /// </summary>
        /// <param name="ifcMonetaryUnit">IfcMonetaryUnit object</param>
        /// <returns>string holding the Monetary Unit</returns>
        private string MonetaryUnit(IfcMonetaryUnit ifcMonetaryUnit)
        {
            //IfcMonetaryUnit ifcMonetaryUnit = Model.InstancesOfType<IfcMonetaryUnit>().FirstOrDefault();

            string value = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
               .Where(c => new RegionInfo(c.LCID).ISOCurrencySymbol == ifcMonetaryUnit.Currency.ToString())
               .Select(c => new RegionInfo(c.LCID).CurrencyEnglishName)
               .FirstOrDefault();
            //string value = ifcMonetaryUnit.Currency.ToString();
            
            return  string.IsNullOrEmpty(value) ? DEFAULT_STRING : value;
        }

        
        
        #endregion

        COBieSheet<COBieAttributeRow> _attributes;

        public void InitialiseAttributes(ref COBieSheet<COBieAttributeRow> attributeSheet)
        {
            _attributes = attributeSheet;
        }
    }
}
