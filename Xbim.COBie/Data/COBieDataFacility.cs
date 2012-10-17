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
using Xbim.Ifc.QuantityResource;


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

            //get Element Quantity holding area values as used for AreaMeasurement below
            IfcElementQuantity ifcElementQuantityAreas = Model.InstancesOfType<IfcElementQuantity>().Where(eq => eq.Quantities.OfType<IfcQuantityArea>().Count() > 0).FirstOrDefault();
           
            IEnumerable<IfcObject> ifcObjects = new List<IfcObject> { ifcProject, ifcSite, ifcBuilding }.AsEnumerable(); ;
            COBieDataPropertySetValues allPropertyValues = new COBieDataPropertySetValues(ifcObjects); //properties helper class
            COBieDataAttributeBuilder attributeBuilder = new COBieDataAttributeBuilder(Context, allPropertyValues);
            attributeBuilder.InitialiseAttributes(ref _attributes);
            
            //list of attributes to exclude form attribute sheet
            //set up filters on COBieDataPropertySetValues for the SetAttributes only
            attributeBuilder.ExcludeAttributePropertyNames.AddRange(Context.Exclude.Facility.AttributesEqualTo);
            attributeBuilder.ExcludeAttributePropertyNamesWildcard.AddRange(Context.Exclude.Facility.AttributesContain);
            attributeBuilder.RowParameters["Sheet"] = "Facility";
            
            COBieFacilityRow facility = new COBieFacilityRow(facilities);

            string name = "";
            if (!string.IsNullOrEmpty(ifcBuilding.Name))
                name = ifcBuilding.Name;
            else if (!string.IsNullOrEmpty(ifcSite.Name))
                name = ifcSite.Name;
            else if (!string.IsNullOrEmpty(ifcProject.Name))
                name = ifcProject.Name;

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
            string AreaMeasurement = (ifcElementQuantityAreas == null) ? DEFAULT_STRING : ifcElementQuantityAreas.MethodOfMeasurement.ToString();
            facility.AreaMeasurement = (AreaMeasurement.ToLower().Contains("bim area")) ? AreaMeasurement : AreaMeasurement + " BIM Area";
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
                attributeBuilder.RowParameters["Name"] = facility.Name;
                attributeBuilder.RowParameters["CreatedBy"] = facility.CreatedBy;
                attributeBuilder.RowParameters["CreatedOn"] = facility.CreatedOn;
                attributeBuilder.RowParameters["ExtSystem"] = facility.ExternalSystem;
                attributeBuilder.PopulateAttributesRows(ifcObject); //fill attribute sheet rows//pass data from this sheet info as Dictionary
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
                        {
                            linearUnit = GetUnitName(ifcUnit);
                            if ((!linearUnit.Contains("feet")) && (linearUnit.Last() != 's'))
                                linearUnit = linearUnit + "s";
                        }


                        if ((ifcNamedUnit.UnitType == IfcUnitEnum.AREAUNIT) && string.IsNullOrEmpty(areaUnit)) //we want area units until we have value
                        {
                            areaUnit = GetUnitName(ifcUnit);
                            if ((!areaUnit.Contains("feet")) && (areaUnit.Last() != 's'))
                                areaUnit = areaUnit + "s";
                            
                        }


                        if ((ifcNamedUnit.UnitType == IfcUnitEnum.VOLUMEUNIT) && string.IsNullOrEmpty(volumeUnit)) //we want volume units until we have value
                        {
                            volumeUnit = GetUnitName(ifcUnit);
                            if ((!volumeUnit.Contains("feet")) && (volumeUnit.Last() != 's'))
                                volumeUnit = volumeUnit + "s";
                        }
                    }
                    //get the money unit
                    if ((ifcUnit is IfcMonetaryUnit) && string.IsNullOrEmpty(moneyUnit))
                    {
                        moneyUnit = GetUnitName(ifcUnit);
                        if (moneyUnit.Last() != 's')
                            moneyUnit = moneyUnit + "s";
                    }
                    
                }
            }

            //ensure we have a value on each unit type, if not then default
            linearUnit = string.IsNullOrEmpty(linearUnit) ? DEFAULT_STRING : linearUnit;
            areaUnit = string.IsNullOrEmpty(areaUnit) ? DEFAULT_STRING : areaUnit;
            volumeUnit = string.IsNullOrEmpty(volumeUnit) ? DEFAULT_STRING : volumeUnit;
            moneyUnit = string.IsNullOrEmpty(moneyUnit) ? DEFAULT_STRING : moneyUnit;
            
            //save values for retrieval by other sheets
            GlobalUnits wBookUnits = Context.WorkBookUnits;
            wBookUnits.LengthUnit = linearUnit;
            wBookUnits.AreaUnit = areaUnit;
            wBookUnits.VolumeUnit = volumeUnit;
            wBookUnits.MoneyUnit = moneyUnit;
            Context.WorkBookUnits = wBookUnits;
            
        }

        

        

        
        
        #endregion

        COBieSheet<COBieAttributeRow> _attributes;

        public void InitialiseAttributes(ref COBieSheet<COBieAttributeRow> attributeSheet)
        {
            _attributes = attributeSheet;
        }
    }
}
