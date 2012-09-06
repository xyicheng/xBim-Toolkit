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

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the Floor tab.
    /// </summary>
    public class COBieDataFloor : COBieData
    {
        /// <summary>
        /// Data Floor constructor
        /// </summary>
        /// <param name="model">IModel to read data from</param>
        public COBieDataFloor(IModel model)
        {
            Model = model;
        }

        #region Methods

        /// <summary>
        /// Fill sheet rows for Floor sheet
        /// </summary>
        /// <returns>COBieSheet<COBieFloorRow></returns>
        public COBieSheet<COBieFloorRow> Fill(ref COBieSheet<COBieAttributeRow> attributes)
        {
            //create new sheet 
            COBieSheet<COBieFloorRow> floors = new COBieSheet<COBieFloorRow>(Constants.WORKSHEET_FLOOR);

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcBuildingStorey> buildingStories = Model.InstancesOfType<IfcBuildingStorey>();

            
            IfcClassification ifcClassification = Model.InstancesOfType<IfcClassification>().FirstOrDefault();
            //list of attributes to exclude form attribute sheet
            List<string> excludePropertyValueNames = new List<string> { "Name", "Line Weight", "Color", 
                                                          "Colour",   "Symbol at End 1 Default", 
                                                          "Symbol at End 2 Default", "Automatic Room Computation Height", "Elevation" };
            List<string> excludePropertyValueNamesWildcard = new List<string> { "Roomtag", "RoomTag", "Tag", "GSA BIM Area", "Length", "Width" };    
            foreach (IfcBuildingStorey bs in buildingStories)
            {
                COBieFloorRow floor = new COBieFloorRow(floors);

                //IfcOwnerHistory ifcOwnerHistory = bs.OwnerHistory;

                floor.Name = bs.Name.ToString();

                floor.CreatedBy = GetTelecomEmailAddress(bs.OwnerHistory);
                floor.CreatedOn = GetCreatedOnDateAsFmtString(bs.OwnerHistory);

                IfcRelAssociatesClassification ifcRAC = bs.HasAssociations.OfType<IfcRelAssociatesClassification>().FirstOrDefault();
                if (ifcRAC != null)
                {
                    IfcClassificationReference ifcCR = (IfcClassificationReference)ifcRAC.RelatingClassification;
                    floor.Category = ifcCR.Name;
                }
                else
                    floor.Category = "";

                floor.ExtSystem = GetIfcApplication().ApplicationFullName;
                floor.ExtObject = bs.GetType().Name;
                floor.ExtIdentifier = bs.GlobalId;
                floor.Description = GetFloorDescription(bs);
                floor.Elevation = bs.Elevation.ToString();
                IEnumerable<IfcQuantityLength> qLen = bs.IsDefinedByProperties.Select(p => p.RelatedObjects.OfType<IfcQuantityLength>()).FirstOrDefault();
                floor.Height = (qLen.FirstOrDefault() == null) ? COBieData.DEFAULT_NUMERIC : qLen.FirstOrDefault().LengthValue.ToString();

                floors.Rows.Add(floor);

                //----------fill in the attribute information for floor-----------
                //pass data from this sheet info as Dictionary
                Dictionary<string, string> passedValues = new Dictionary<string, string>(){{"Sheet", "Floor"}, 
                                                                                          {"Name", floor.Name},
                                                                                          {"CreatedBy", floor.CreatedBy},
                                                                                          {"CreatedOn", floor.CreatedOn},
                                                                                          {"ExtSystem", floor.ExtSystem}
                                                                                          };//required property date <PropertySetName, PropertyName>
                
                //add *ALL* the attributes to the passed attributes sheet except property names that match the passed List<string>
                //Exclude property list
                SetAttributeSheet(bs, passedValues, excludePropertyValueNames, excludePropertyValueNamesWildcard, null, ref attributes);
            }

            return floors;
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
    }
}
