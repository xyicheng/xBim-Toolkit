using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.Rows;
using Xbim.Ifc.ExternalReferenceResource;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.QuantityResource;
using Xbim.XbimExtensions;
using Xbim.Ifc.Extensions;
using Xbim.Ifc.PropertyResource;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the Floor tab.
    /// </summary>
    public class COBieDataFloor : COBieData<COBieFloorRow>, IAttributeProvider
    {
        /// <summary>
        /// Data Floor constructor
        /// </summary>
        /// <param name="model">The context of the model being generated</param>
        public COBieDataFloor(COBieContext context) : base(context)
        { }

        #region Methods

        /// <summary>
        /// Fill sheet rows for Floor sheet
        /// </summary>
        /// <returns>COBieSheet<COBieFloorRow></returns>
        public override COBieSheet<COBieFloorRow> Fill()
        {
            ProgressIndicator.ReportMessage("Starting Floors...");

            //create new sheet 
            COBieSheet<COBieFloorRow> floors = new COBieSheet<COBieFloorRow>(Constants.WORKSHEET_FLOOR);

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcBuildingStorey> buildingStories = Model.InstancesOfType<IfcBuildingStorey>();

            COBieDataPropertySetValues allPropertyValues = new COBieDataPropertySetValues(buildingStories.OfType<IfcObject>().ToList()); //properties helper class
            
            
            //IfcClassification ifcClassification = Model.InstancesOfType<IfcClassification>().FirstOrDefault();
            //list of attributes to exclude form attribute sheet
            List<string> excludePropertyValueNames = new List<string> { "Name", "Line Weight", "Color", 
                                                          "Colour",   "Symbol at End 1 Default", 
                                                          "Symbol at End 2 Default", "Automatic Room Computation Height", "Elevation" };
            List<string> excludePropertyValueNamesWildcard = new List<string> { "Roomtag", "RoomTag", "Tag", "GSA BIM Area", "Length", "Width" };
            allPropertyValues.ExcludePropertyValueNames.AddRange(excludePropertyValueNames);
            allPropertyValues.ExcludePropertyValueNamesWildcard.AddRange(excludePropertyValueNamesWildcard);
            allPropertyValues.RowParameters["Sheet"] = "Floor";
            
           

            ProgressIndicator.Initialise("Creating Components", buildingStories.Count());

            foreach (IfcBuildingStorey ifcBuildingStorey in buildingStories)
            {
                ProgressIndicator.IncrementAndUpdate();

                COBieFloorRow floor = new COBieFloorRow(floors);

                floor.Name = ifcBuildingStorey.Name.ToString();

                floor.CreatedBy = GetTelecomEmailAddress(ifcBuildingStorey.OwnerHistory);
                floor.CreatedOn = GetCreatedOnDateAsFmtString(ifcBuildingStorey.OwnerHistory);

                floor.Category = GetCategory(ifcBuildingStorey);

                floor.ExtSystem = ifcApplication.ApplicationFullName;
                floor.ExtObject = ifcBuildingStorey.GetType().Name;
                floor.ExtIdentifier = ifcBuildingStorey.GlobalId;
                floor.Description = GetFloorDescription(ifcBuildingStorey);
                floor.Elevation = ifcBuildingStorey.Elevation.ToString();

                floor.Height = GetFloorHeight(ifcBuildingStorey, allPropertyValues);

                floors.Rows.Add(floor);

                //fill in the attribute information
                allPropertyValues.RowParameters["Name"] = floor.Name;
                allPropertyValues.RowParameters["CreatedBy"] = floor.CreatedBy;
                allPropertyValues.RowParameters["CreatedOn"] = floor.CreatedOn;
                allPropertyValues.RowParameters["ExtSystem"] = floor.ExtSystem;
                allPropertyValues.SetAttributesRows(ifcBuildingStorey, ref _attributes); //fill attribute sheet rows//pass data from this sheet info as Dictionary
                
            }
            ProgressIndicator.Finalise();

            return floors;
        }
        /// <summary>
        /// Get the floor height
        /// </summary>
        /// <param name="ifcBuildingStorey">IfcBuildingStory object</param>
        /// <param name="allPropertyValues">COBieDataPropertySetValues object holds all the properties for all the IfcBuildingStory </param>
        /// <returns></returns>
        private string GetFloorHeight (IfcBuildingStorey ifcBuildingStorey, COBieDataPropertySetValues allPropertyValues)
        {
            //try for a IfcQuantityLength related property to this building story
            IEnumerable<IfcQuantityLength> qLen = ifcBuildingStorey.IsDefinedByProperties.Select(p => p.RelatedObjects.OfType<IfcQuantityLength>()).FirstOrDefault();
            if (qLen.FirstOrDefault() != null) 
                return qLen.FirstOrDefault().LengthValue.ToString();
            
            //Fall back properties
            //get the property single values for this building story
            allPropertyValues.SetFilteredPropertySingleValues(ifcBuildingStorey);

            //try and find it in the attached properties of the building story
            string value = allPropertyValues.GetFilteredPropertySingleValueValue("StoreyHeight", true);
            if (value == DEFAULT_STRING)
                value = allPropertyValues.GetFilteredPropertySingleValueValue("Storey Height", true);
            if (value == DEFAULT_STRING)
                value = allPropertyValues.GetFilteredPropertySingleValueValue("FloorHeight", true);
            if (value == DEFAULT_STRING)
                value = allPropertyValues.GetFilteredPropertySingleValueValue("Floor Height", true);
            
            if (value == DEFAULT_STRING)
                return DEFAULT_NUMERIC;
            else
                return value;

        }

        private string GetFloorDescription(IfcBuildingStorey bs)
        {
            if (bs != null)
            {
                if (!string.IsNullOrEmpty(bs.LongName)) return bs.LongName;
                else if (!string.IsNullOrEmpty(bs.Description)) return bs.Description;
                else if (!string.IsNullOrEmpty(bs.Name)) return bs.Name;
            }
            return DEFAULT_STRING;
        }

        #endregion

        COBieSheet<COBieAttributeRow> _attributes;

        public void InitialiseAttributes(ref COBieSheet<COBieAttributeRow> attributeSheet)
        {
            _attributes = attributeSheet;
        }
    }
}
