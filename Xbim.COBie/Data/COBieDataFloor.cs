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
                floor.Height = (qLen.FirstOrDefault() == null) ? "0" : qLen.FirstOrDefault().LengthValue.ToString();

                floors.Rows.Add(floor);

                //----------fill in the attribute information for floor-----------
                //pass data from this sheet info as Dictionary
                Dictionary<string, string> passedValues = new Dictionary<string, string>(){{"Sheet", "Floor"}, 
                                                                                          {"Name", floor.Name},
                                                                                          {"CreatedBy", floor.CreatedBy},
                                                                                          {"CreatedOn", floor.CreatedOn},
                                                                                          {"ExtSystem", floor.ExtSystem}
                                                                                          };//required property date <PropertySetName, PropertyName>
                /*...go for all properties with exclude list
                List<KeyValuePair<string, string>> ReqdProps = new List<KeyValuePair<string, string>>(); //get over the unique key with dictionary
                ReqdProps.Add(new KeyValuePair<string, string>("PSet_Revit_Type_Graphics", "Line Weight"));
                ReqdProps.Add(new KeyValuePair<string, string>("PSet_Revit_Type_Graphics", "Symbol at End 1 Default"));
                ReqdProps.Add(new KeyValuePair<string, string>("PSet_Revit_Type_Graphics", "Symbol at End 2 Default"));
                ReqdProps.Add(new KeyValuePair<string, string>("PSet_Revit_Type_Graphics", "Color"));
                ReqdProps.Add(new KeyValuePair<string, string>("PSet_Revit_Type_Constraints", "Elevation Base"));
                
                //add the attributes to the passed attributes sheet
                SetAttributeSheet(bs, passedValues, ReqdProps, ref attributes);
                */
                //add *ALL* the attributes to the passed attributes sheet except property names that match the passed List<string>
                //Exclude property list
                List<string> AttNames = new List<string> { "Elevation"
                                                            };
                SetAttributeSheet(bs, passedValues, AttNames, new List<string>(), ref attributes);
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
