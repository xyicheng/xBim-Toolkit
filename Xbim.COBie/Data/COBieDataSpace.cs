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
    public class COBieDataSpace : COBieData
    {
        /// <summary>
        /// Data Space constructor
        /// </summary>
        /// <param name="model">IModel to read data from</param>
        public COBieDataSpace(IModel model)
        {
            Model = model;
        }

        #region Methods

        /// <summary>
        /// Fill sheet rows for Space sheet
        /// </summary>
        /// <returns>COBieSheet<COBieSpaceRow></returns>
        public COBieSheet<COBieSpaceRow> Fill(ref COBieSheet<COBieAttributeRow> attributes)
        {
            //create new sheet 
            COBieSheet<COBieSpaceRow> spaces = new COBieSheet<COBieSpaceRow>(Constants.WORKSHEET_SPACE);

            // get all IfcBuildingStory objects from IFC file
            List<IfcSpace> ifcSpaces = Model.InstancesOfType<IfcSpace>().OrderBy(ifcSpace => ifcSpace.Name, new CompareIfcLabel()).ToList();
            
            COBieDataPropertySetValues allPropertyValues = new COBieDataPropertySetValues(ifcSpaces.OfType<IfcObject>().ToList()); //properties helper class
            

            //list of attributes and property sets to exclude form attribute sheet
            List<string> excludePropertyValueNames = new List<string> { "Area", "Number", "UsableHeight" };
            List<string> excludePropertyValueNamesWildcard = new List<string> { "ZoneName", "Category", "Length", "Width" }; //exclude part names
            List<string> excludePropertSetNames = new List<string>() { "BaseQuantities" };
            //set up filters on COBieDataPropertySetValues
            allPropertyValues.ExcludePropertyValueNames.AddRange(excludePropertyValueNames);
            allPropertyValues.ExcludePropertyValueNamesWildcard.AddRange(excludePropertyValueNamesWildcard);
            allPropertyValues.ExcludePropertySetNames.AddRange(excludePropertSetNames);
            allPropertyValues.RowParameters["Sheet"] = "Space";
            
            foreach (IfcSpace ifcSpace in ifcSpaces)
            {
                COBieSpaceRow space = new COBieSpaceRow(spaces);

                space.Name = ifcSpace.Name;

                space.CreatedBy = GetTelecomEmailAddress(ifcSpace.OwnerHistory);
                space.CreatedOn = GetCreatedOnDateAsFmtString(ifcSpace.OwnerHistory);

                space.Category = GetCategory(ifcSpace);

                space.FloorName = ifcSpace.SpatialStructuralElementParent.Name.ToString();
                space.Description = GetSpaceDescription(ifcSpace);
                space.ExtSystem = GetIfcApplication().ApplicationFullName;
                space.ExtObject = ifcSpace.GetType().Name;
                space.ExtIdentifier = ifcSpace.GlobalId;
                space.RoomTag = GetSpaceDescription(ifcSpace);
                //Do Usable Height
                IfcLengthMeasure usableHt = ifcSpace.GetHeight();
                if (usableHt != null) space.UsableHeight = ((double)usableHt).ToString("F3");
                else space.UsableHeight = DEFAULT_NUMERIC;

                //Do Gross Areas 
                IfcAreaMeasure grossAreaValue = ifcSpace.GetGrossFloorArea();
                //if we fail on try GSA keys
                IfcQuantityArea spArea = null;
                if (grossAreaValue == null) spArea = ifcSpace.GetQuantity<IfcQuantityArea>("GSA Space Areas", "GSA BIM Area");

                if (grossAreaValue != null) space.GrossArea = ((double)grossAreaValue).ToString("F3");
                else if ((spArea is IfcQuantityArea) && (spArea.AreaValue != null)) space.GrossArea = ((double)spArea.AreaValue).ToString("F3");
                else space.GrossArea = DEFAULT_NUMERIC;

                //Do Net Areas 
                IfcAreaMeasure netAreaValue = ifcSpace.GetNetFloorArea();  //this extension has the GSA built in so no need to get again
                if (netAreaValue != null) space.NetArea = ((double)netAreaValue).ToString("F3");
                else space.NetArea = DEFAULT_NUMERIC;

                spaces.Rows.Add(space);

                //----------fill in the attribute information for spaces-----------

                //fill in the attribute information
                allPropertyValues.RowParameters["Name"] = space.Name;
                allPropertyValues.RowParameters["CreatedBy"] = space.CreatedBy;
                allPropertyValues.RowParameters["CreatedOn"] = space.CreatedOn;
                allPropertyValues.RowParameters["ExtSystem"] = space.ExtSystem;
                allPropertyValues.SetAttributesRows(ifcSpace, ref attributes); //fill attribute sheet rows//pass data from this sheet info as Dictionary
                //Dictionary<string, string> passedValues = new Dictionary<string, string>(){{"Sheet", "Space"}, 
                //                                                                          {"Name", space.Name},
                //                                                                          {"CreatedBy", space.CreatedBy},
                //                                                                          {"CreatedOn", space.CreatedOn},
                //                                                                          {"ExtSystem", space.ExtSystem}
                //                                                                          };//required property date <PropertySetName, PropertyName>
                
                //add *ALL* the attributes to the passed attributes sheet except property names that match the passed List<string>
                //SetAttributeSheet(sp, passedValues, excludeAttributes, ExcludeAttributesWildCard, ExcludePropertSetNames, ref attributes);
                            
                
            }

            return spaces;
        }

        //private string GetSpaceCategory(IfcSpace sp)
        //{
        //    return sp.LongName;
        //}

        private string GetSpaceDescription(IfcSpace sp)
        {
            if (sp != null)
            {
                if (!string.IsNullOrEmpty(sp.LongName)) return sp.LongName;
                else if (!string.IsNullOrEmpty(sp.Description)) return sp.Description;
                else if (!string.IsNullOrEmpty(sp.Name)) return sp.Name;
            }
            return DEFAULT_STRING;
        }
        #endregion
    }
}
