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

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the Facility tab.
    /// </summary>
    public class COBieDataFacility : COBieData
    {
        /// <summary>
        /// Data Facility constructor
        /// </summary>
        /// <param name="model">IModel to read data from</param>
        public COBieDataFacility(IModel model)
        {
            Model = model;
        }

      
        #region Methods

        /// <summary>
        /// Fill sheet rows for Facility sheet
        /// </summary>
        /// <returns>COBieSheet<COBieFacilityRow></returns>
        public COBieSheet<COBieFacilityRow> Fill(ref COBieSheet<COBieAttributeRow> attributes)
        {
            //Create new sheet
            COBieSheet<COBieFacilityRow> facilities = new COBieSheet<COBieFacilityRow>(Constants.WORKSHEET_FACILITY);
            //list of attributes to exclude form attribute sheet
            List<string> excludePropertyValueNames = new List<string> { "Phase", "Height" };
            List<string> excludePropertyValueNamesWildcard = new List<string> {  "Roomtag", "RoomTag", "Tag", "GSA BIM Area", "Length", "Width", "Height" };

            IfcProject ifcProject = Model.IfcProject;
            IfcSite ifcSite = Model.InstancesOfType<IfcSite>().FirstOrDefault();
            IfcBuilding ifcBuilding = Model.InstancesOfType<IfcBuilding>().FirstOrDefault();
            IfcMonetaryUnit ifcMonetaryUnit = Model.InstancesOfType<IfcMonetaryUnit>().FirstOrDefault();
            IfcElementQuantity ifcElementQuantity = Model.InstancesOfType<IfcElementQuantity>().FirstOrDefault();

            //IEnumerable<IfcTelecomAddress> ifcTelecomAddresses = model.InstancesOfType<IfcTelecomAddress>();
            //if (ifcTelecomAddresses == null) ifcTelecomAddresses = Enumerable.Empty<IfcTelecomAddress>();

            
            COBieFacilityRow facility = new COBieFacilityRow(facilities);

            facility.Name = ifcBuilding.Name.ToString();

            facility.CreatedBy = GetTelecomEmailAddress(ifcBuilding.OwnerHistory);
            facility.CreatedOn = GetCreatedOnDateAsFmtString(ifcBuilding.OwnerHistory);

            //facility.Category = "";
            //foreach (COBiePickListsRow plRow in pickLists.Rows)
            //    if (plRow != null)
            //        facility.Category += plRow.CategoryFacility + ",";
            //facility.Category = facility.Category.TrimEnd(',');
            facility.Category = GetCategory(ifcBuilding);
            
            facility.ProjectName = GetFacilityProjectName(ifcProject);
            facility.SiteName = GetFacilitySiteName(ifcSite);
            facility.LinearUnits = GetLinearUnits();
            facility.AreaUnits = GetAreaUnits();
            facility.VolumeUnits = GetVolumeUnits();
            facility.CurrencyUnit = (ifcMonetaryUnit == null) ? DEFAULT_STRING : ifcMonetaryUnit.Currency.ToString();
            facility.AreaMeasurement = (ifcElementQuantity == null) ? "" : ifcElementQuantity.MethodOfMeasurement.ToString();
            facility.ExternalSystem = GetIfcApplication().ApplicationFullName;

            facility.ExternalProjectObject = "IfcProject";
            facility.ExternalProjectIdentifier = ifcProject.GlobalId;

            facility.ExternalSiteObject = "IfcSite";
            facility.ExternalSiteIdentifier = ifcSite.GlobalId;

            facility.ExternalFacilityObject = "IfcBuilding";
            facility.ExternalFacilityIdentifier = ifcBuilding.GlobalId;

            facility.Description = GetFacilityDescription(ifcBuilding);
            facility.ProjectDescription = GetFacilityProjectDescription(ifcProject);
            facility.SiteDescription = GetFacilitySiteDescription(ifcSite);
            facility.Phase = Model.IfcProject.Phase;

            facilities.Rows.Add(facility);
            //----------fill in the attribute information for spaces-----------
            //pass data from this sheet info as Dictionary
            Dictionary<string, string> passedValues = new Dictionary<string, string>(){{"Sheet", "Facility"}, 
                                                                                          {"Name", facility.Name},
                                                                                          {"CreatedBy", facility.CreatedBy},
                                                                                          {"CreatedOn", facility.CreatedOn},
                                                                                          {"ExtSystem", facility.ExternalSystem}
                                                                                          };//required property date <PropertySetName, PropertyName>

            //add *ALL* the attributes to the passed attributes sheet except property names that match the passed List<string>


            SetAttributeSheet(ifcProject, passedValues, excludePropertyValueNames, excludePropertyValueNamesWildcard, null, ref attributes);
            SetAttributeSheet(ifcSite, passedValues, excludePropertyValueNames, excludePropertyValueNamesWildcard, null, ref attributes);
            SetAttributeSheet(ifcBuilding, passedValues, excludePropertyValueNames, excludePropertyValueNamesWildcard, null, ref attributes); 
            
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
            return DEFAULT_STRING;
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

        private string GetLinearUnits()
        {
            IEnumerable<IfcUnitAssignment> unitAssignments = Model.InstancesOfType<IfcUnitAssignment>();
            foreach (IfcUnitAssignment ua in unitAssignments)
            {
                UnitSet us = ua.Units;
                foreach (IfcUnit u in us)
                {
                    if (u is IfcSIUnit)
                    {
                        if (((IfcSIUnit)u).UnitType == IfcUnitEnum.LENGTHUNIT)
                        {
                            if (((IfcSIUnit)u).Name.ToString().ToLower() == "milli") return "millimetres";
                            if (((IfcSIUnit)u).Name.ToString().ToLower() == "metre") return "metres";
                            if (((IfcSIUnit)u).Name.ToString().ToLower() == "inch") return "inches";
                        }
                    }
                }
            }
            return "feet";
        }

        private string GetAreaUnits()
        {
            IEnumerable<IfcUnitAssignment> unitAssignments = Model.InstancesOfType<IfcUnitAssignment>();
            foreach (IfcUnitAssignment ua in unitAssignments)
            {
                UnitSet us = ua.Units;
                foreach (IfcUnit u in us)
                {
                    if (u is IfcSIUnit)
                    {
                        if (((IfcSIUnit)u).UnitType == IfcUnitEnum.AREAUNIT)
                            if (((IfcSIUnit)u).Name.ToString().ToLower() == "square_metre") return "squaremetres";
                    }
                    else if (u is IfcConversionBasedUnit)
                    {
                        if (((IfcConversionBasedUnit)u).UnitType == IfcUnitEnum.AREAUNIT)
                            if (((IfcConversionBasedUnit)u).Name.ToString().ToLower() == "square_metre") return "squaremetres";
                    }
                }
            }

            return "squarefeet";
        }

        private string GetVolumeUnits()
        {
            IEnumerable<IfcUnitAssignment> unitAssignments = Model.InstancesOfType<IfcUnitAssignment>();
            foreach (IfcUnitAssignment ua in unitAssignments)
            {
                UnitSet us = ua.Units;
                foreach (IfcUnit u in us)
                {
                    if (u is IfcSIUnit)
                    {
                        if (((IfcSIUnit)u).UnitType == IfcUnitEnum.VOLUMEUNIT)
                            if (((IfcSIUnit)u).Name.ToString().ToLower() == "cubic_metre") return "cubicmetres";
                    }
                    else if (u is IfcConversionBasedUnit)
                    {
                        if (((IfcConversionBasedUnit)u).UnitType == IfcUnitEnum.VOLUMEUNIT)
                            if (((IfcConversionBasedUnit)u).Name.ToString().ToLower() == "cubic_metre") return "cubicmetres";
                    }
                }
            }
            return "cubicfeet";
        }
        #endregion
    }
}
