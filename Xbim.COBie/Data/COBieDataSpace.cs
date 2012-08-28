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
        public COBieSheet<COBieSpaceRow> Fill()
        {
            //create new sheet 
            COBieSheet<COBieSpaceRow> spaces = new COBieSheet<COBieSpaceRow>(Constants.WORKSHEET_SPACE);

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcSpace> ifcSpaces = Model.InstancesOfType<IfcSpace>();//.OrderBy(ifcSpace => ifcSpace.Name, new CompareIfcLabel());
                        
            foreach (IfcSpace sp in ifcSpaces)
            {
                COBieSpaceRow space = new COBieSpaceRow(spaces);

                space.Name = sp.Name;

                space.CreatedBy = GetTelecomEmailAddress(sp.OwnerHistory);
                space.CreatedOn = GetCreatedOnDateAsFmtString(sp.OwnerHistory);

                space.Category = space.GetCategory(sp);

                space.FloorName = sp.SpatialStructuralElementParent.Name.ToString();
                space.Description = GetSpaceDescription(sp);
                space.ExtSystem = GetIfcApplication().ApplicationFullName;
                space.ExtObject = sp.GetType().Name;
                space.ExtIdentifier = sp.GlobalId;
                space.RoomTag = GetSpaceDescription(sp);
                //Do Usable Height
                IfcLengthMeasure usableHt = sp.GetHeight();
                if (usableHt != null) space.UsableHeight = ((double)usableHt).ToString("F3");
                else space.UsableHeight = DEFAULT_VAL;

                //Do Gross Areas 
                IfcAreaMeasure grossAreaValue = sp.GetGrossFloorArea();
                //if we fail on try GSA keys
                IfcQuantityArea spArea = null;
                if (grossAreaValue == null) spArea = sp.GetQuantity<IfcQuantityArea>("GSA Space Areas", "GSA BIM Area");

                if (grossAreaValue != null) space.GrossArea = ((double)grossAreaValue).ToString("F3");
                else if ((spArea is IfcQuantityArea) && (spArea.AreaValue != null)) space.GrossArea = ((double)spArea.AreaValue).ToString("F3");
                else space.GrossArea = DEFAULT_VAL;

                //Do Net Areas 
                IfcAreaMeasure netAreaValue = sp.GetNetFloorArea();  //this extension has the GSA built in so no need to get again
                if (netAreaValue != null) space.NetArea = ((double)netAreaValue).ToString("F3");
                else space.NetArea = DEFAULT_VAL;

                spaces.Rows.Add(space);
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
            return DEFAULT_VAL;
        }
        #endregion
    }
}
