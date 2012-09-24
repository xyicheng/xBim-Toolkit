using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.Rows;
using Xbim.Ifc.Extensions;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.QuantityResource;
using Xbim.XbimExtensions;
using System.Collections;
using Xbim.Ifc.PropertyResource;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ExternalReferenceResource;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the Space tab.
    /// </summary>
    public class COBieDataSpace : COBieData<COBieSpaceRow>, IAttributeProvider
    {
        /// <summary>
        /// Data Space constructor
        /// </summary>
        /// <param name="model">The context of the model being generated</param>
        public COBieDataSpace(COBieContext context) : base(context)
        { }

        #region Methods

        /// <summary>
        /// Fill sheet rows for Space sheet
        /// </summary>
        /// <returns>COBieSheet<COBieSpaceRow></returns>
        public override COBieSheet<COBieSpaceRow> Fill()
        {
            ProgressIndicator.ReportMessage("Starting Spaces...");

            //create new sheet 
            COBieSheet<COBieSpaceRow> spaces = new COBieSheet<COBieSpaceRow>(Constants.WORKSHEET_SPACE);

            // get all IfcBuildingStory objects from IFC file
            List<IfcSpace> ifcSpaces = Model.InstancesOfType<IfcSpace>().OrderBy(ifcSpace => ifcSpace.Name, new CompareIfcLabel()).ToList();
            
            COBieDataPropertySetValues allPropertyValues = new COBieDataPropertySetValues(ifcSpaces.OfType<IfcObject>()); //properties helper class
            

            //list of attributes and property sets to exclude form attribute sheet
            List<string> excludePropertyValueNames = new List<string> { "Area", "Number", "UsableHeight", "RoomTag", "Room Tag" };
            List<string> excludePropertyValueNamesWildcard = new List<string> { "ZoneName", "Category", "Length", "Width"}; //exclude part names
            List<string> excludePropertSetNames = new List<string>() { "BaseQuantities" };
            //set up filters on COBieDataPropertySetValues
            allPropertyValues.ExcludePropertyValueNames.AddRange(excludePropertyValueNames);
            allPropertyValues.ExcludePropertyValueNamesWildcard.AddRange(excludePropertyValueNamesWildcard);
            allPropertyValues.ExcludePropertySetNames.AddRange(excludePropertSetNames);
            allPropertyValues.RowParameters["Sheet"] = "Space";

            ProgressIndicator.Initialise("Creating Spaces", ifcSpaces.Count());

            foreach (IfcSpace ifcSpace in ifcSpaces)
            {
                ProgressIndicator.IncrementAndUpdate();

                COBieSpaceRow space = new COBieSpaceRow(spaces);

                space.Name = ifcSpace.Name;

                space.CreatedBy = GetTelecomEmailAddress(ifcSpace.OwnerHistory);
                space.CreatedOn = GetCreatedOnDateAsFmtString(ifcSpace.OwnerHistory);

                space.Category = GetCategory(ifcSpace);

                space.FloorName = ifcSpace.SpatialStructuralElementParent.Name.ToString();
                space.Description = GetSpaceDescription(ifcSpace);
                space.ExtSystem = GetExternalSystem(ifcSpace);
                space.ExtObject = ifcSpace.GetType().Name;
                space.ExtIdentifier = ifcSpace.GlobalId;
                space.RoomTag = GetRoomTag(ifcSpace, allPropertyValues);
                
                //Do Unit Values
                space.UsableHeight = GetUsableHeight(ifcSpace, allPropertyValues);
                space.GrossArea = GetGrossFloorArea(ifcSpace, allPropertyValues);
                space.NetArea = GetNetArea(ifcSpace, allPropertyValues);

                spaces.Rows.Add(space);

                //----------fill in the attribute information for spaces-----------

                //fill in the attribute information
                allPropertyValues.RowParameters["Name"] = space.Name;
                allPropertyValues.RowParameters["CreatedBy"] = space.CreatedBy;
                allPropertyValues.RowParameters["CreatedOn"] = space.CreatedOn;
                allPropertyValues.RowParameters["ExtSystem"] = space.ExtSystem;
                allPropertyValues.PopulateAttributesRows(ifcSpace, ref _attributes); //fill attribute sheet rows//pass data from this sheet info as Dictionary
                
            }
            ProgressIndicator.Finalise();
            return spaces;
        }

        /// <summary>
        /// Get Net Area value
        /// </summary>
        /// <param name="ifcSpace">IfcSpace object</param>
        /// <param name="allPropertyValues">COBieDataPropertySetValues object holds all the properties for all the IfcSpace</param>
        /// <returns>property value as string or default value</returns>
        private string GetNetArea(IfcSpace ifcSpace, COBieDataPropertySetValues allPropertyValues)
        {
            IfcAreaMeasure netAreaValue = ifcSpace.GetNetFloorArea();  //this extension has the GSA built in so no need to get again
            if (netAreaValue != null) 
                return ((double)netAreaValue).ToString("F3");

            //Fall back to properties
            //get the property single values for this ifcSpace
            allPropertyValues.SetFilteredPropertySingleValues(ifcSpace);

            //try and find it in the attached properties of the ifcSpace
            string value = allPropertyValues.GetFilteredPropertySingleValueValue("NetFloorArea", true);
            if (value == DEFAULT_STRING)
                value = allPropertyValues.GetFilteredPropertySingleValueValue("GSA", true);

            if (value == DEFAULT_STRING)
                return DEFAULT_NUMERIC;
            else
                return value; 
        }
        /// <summary>
        /// Get space gross floor area
        /// </summary>
        /// <param name="ifcSpace">IfcSpace object</param>
        /// <param name="allPropertyValues">COBieDataPropertySetValues object holds all the properties for all the IfcSpace</param>
        /// <returns>property value as string or default value</returns>
        private string GetGrossFloorArea(IfcSpace ifcSpace, COBieDataPropertySetValues allPropertyValues)
        {
            //Do Gross Areas 
            IfcAreaMeasure grossAreaValue = ifcSpace.GetGrossFloorArea();
            //if we fail on try GSA keys
            IfcQuantityArea spArea = null;
            if (grossAreaValue == null) spArea = ifcSpace.GetQuantity<IfcQuantityArea>("GSA Space Areas", "GSA BIM Area");

            if (grossAreaValue != null) 
                return ((double)grossAreaValue).ToString("F3");
            else if ((spArea is IfcQuantityArea) && (spArea.AreaValue != null))
                return ((double)spArea.AreaValue).ToString("F3");
            else
            {
                //Fall back to properties
                //get the property single values for this ifcSpace
                allPropertyValues.SetFilteredPropertySingleValues(ifcSpace);

                //try and find it in the attached properties of the ifcSpace
                string value = allPropertyValues.GetFilteredPropertySingleValueValue("GrossFloorArea", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("GSA", true);
                
                if (value == DEFAULT_STRING)
                    return DEFAULT_NUMERIC;
                else
                    return value; 
            }
        }
        /// <summary>
        /// Get space usable height
        /// </summary>
        /// <param name="ifcSpace">IfcSpace object</param>
        /// <param name="allPropertyValues">COBieDataPropertySetValues object holds all the properties for all the IfcSpace</param>
        /// <returns>property value as string or default value</returns>
        private string GetUsableHeight(IfcSpace ifcSpace, COBieDataPropertySetValues allPropertyValues)
        {
            IfcLengthMeasure usableHt = ifcSpace.GetHeight();
            if (usableHt != null)
            return ((double)usableHt).ToString("F3");
            
            //Fall back to properties
            //get the property single values for this ifcSpace
            allPropertyValues.SetFilteredPropertySingleValues(ifcSpace);

            //try and find it in the attached properties of the ifcSpace
            string value = allPropertyValues.GetFilteredPropertySingleValueValue("UsableHeight", true);
            if (value == DEFAULT_STRING)
                value = allPropertyValues.GetFilteredPropertySingleValueValue("FinishCeiling", true);
            if (value == DEFAULT_STRING)
                value = allPropertyValues.GetFilteredPropertySingleValueValue("Height", true);
            
            if (value == DEFAULT_STRING)
                return DEFAULT_NUMERIC;
            else
                return value; 
        }
        /// <summary>
        /// Get space description 
        /// </summary>
        /// <param name="ifcSpace">IfcSpace object</param>
        /// <returns>property value as string or default value</returns>
        private string GetSpaceDescription(IfcSpace ifcSpace)
        {
            if (ifcSpace != null)
            {
                if (!string.IsNullOrEmpty(ifcSpace.LongName)) return ifcSpace.LongName;
                else if (!string.IsNullOrEmpty(ifcSpace.Description)) return ifcSpace.Description;
                else if (!string.IsNullOrEmpty(ifcSpace.Name)) return ifcSpace.Name;
            }
            return DEFAULT_STRING;
        }
        /// <summary>
        /// Get space room tag 
        /// </summary>
        /// <param name="ifcSpace">IfcSpace object</param>
        /// <param name="allPropertyValues">COBieDataPropertySetValues object holds all the properties for all the IfcSpace</param>
        /// <returns>property value as string or default value</returns>
        private string GetRoomTag(IfcSpace ifcSpace, COBieDataPropertySetValues allPropertyValues)
        {
            string value = GetSpaceDescription(ifcSpace);
            if (value == DEFAULT_STRING)
            {
                //Fall back to properties
                //get the property single values for this ifcSpace
                allPropertyValues.SetFilteredPropertySingleValues(ifcSpace);

                //try and find it in the attached properties of the ifcSpace
                value = allPropertyValues.GetFilteredPropertySingleValueValue("RoomTag", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("Tag", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("Room_Tag", true);

            }

            return value;
        }
        #endregion

        COBieSheet<COBieAttributeRow> _attributes;

        public void InitialiseAttributes(ref COBieSheet<COBieAttributeRow> attributeSheet)
        {
            _attributes = attributeSheet;
        }
    }
}
