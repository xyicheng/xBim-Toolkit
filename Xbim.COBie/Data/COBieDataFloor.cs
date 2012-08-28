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
        public COBieSheet<COBieFloorRow> Fill()
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
            return DEFAULT_VAL;
        }

        #endregion
    }
}
