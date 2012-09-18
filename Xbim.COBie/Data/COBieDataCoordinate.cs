using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.COBie.Rows;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.UtilityResource;
using Xbim.Ifc.ExternalReferenceResource;
using Xbim.Ifc.GeometryResource;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the Coordinate tab.
    /// </summary>
    public class COBieDataCoordinate : COBieData
    {
        /// <summary>
        /// Data Coordinate constructor
        /// </summary>
        /// <param name="model">IModel to read data from</param>
        public COBieDataCoordinate(IModel model)
        {
            Model = model;
        }

        #region Methods

        /// <summary>
        /// Fill sheet rows for Coordinate sheet
        /// </summary>
        /// <returns>COBieSheet<COBieCoordinateRow></returns>
        public COBieSheet<COBieCoordinateRow> Fill()
        {
            //Create new sheet
            COBieSheet<COBieCoordinateRow> coordinates = new COBieSheet<COBieCoordinateRow>(Constants.WORKSHEET_COORDINATE);

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcRelAggregates> ifcRelAggregates = Model.InstancesOfType<IfcRelAggregates>();
            IfcBuildingStorey ifcBuildingStorey = Model.InstancesOfType<IfcBuildingStorey>().FirstOrDefault();
            //IfcTelecomAddress ifcTelecomAddres = Model.InstancesOfType<IfcTelecomAddress>().FirstOrDefault();
            IfcCartesianPoint ifcCartesianPoint = Model.InstancesOfType<IfcCartesianPoint>().FirstOrDefault();
            
            IfcOwnerHistory ifcOwnerHistory = Model.InstancesOfType<IfcOwnerHistory>().FirstOrDefault();
            IfcProduct ifcProduct = Model.InstancesOfType<IfcProduct>().FirstOrDefault();
            IfcClassification ifcClassification = Model.InstancesOfType<IfcClassification>().FirstOrDefault();
            string applicationFullName = ifcApplication.ApplicationFullName;

            foreach (IfcRelAggregates ra in ifcRelAggregates)
            {
                COBieCoordinateRow coordinate = new COBieCoordinateRow(coordinates);
                coordinate.Name = (ifcBuildingStorey == null || ifcBuildingStorey.Name.ToString() == "") ? "CoordinateName" : ifcBuildingStorey.Name.ToString();

                coordinate.CreatedBy = GetTelecomEmailAddress(ra.OwnerHistory);
                coordinate.CreatedOn = GetCreatedOnDateAsFmtString(ra.OwnerHistory);

                //IfcRelAssociatesClassification ifcRAC = ra.HasAssociations.OfType<IfcRelAssociatesClassification>().FirstOrDefault();
                //IfcClassificationReference ifcCR = (IfcClassificationReference)ifcRAC.RelatingClassification;
                coordinate.Category = "";

                coordinate.SheetName = "PickList.SheetType";
                coordinate.RowName = DEFAULT_STRING;
                coordinate.CoordinateXAxis = ifcCartesianPoint[0].ToString();
                coordinate.CoordinateYAxis = ifcCartesianPoint[1].ToString();
                coordinate.CoordinateZAxis = ifcCartesianPoint[2].ToString();
                coordinate.ExtSystem = applicationFullName;
                coordinate.ExtObject = GetExtObject(Model);
                coordinate.ExtIdentifier = "PickList.objType";
                coordinate.ClockwiseRotation = DEFAULT_NUMERIC;
                coordinate.ElevationalRotation = DEFAULT_NUMERIC;
                coordinate.YawRotation = DEFAULT_NUMERIC;

                coordinates.Rows.Add(coordinate);
            }

            return coordinates;
        }

        private string GetExtObject(IModel model)
        {
            IfcBuildingStorey ifcBuildingStorey = model.InstancesOfType<IfcBuildingStorey>().FirstOrDefault();
            IfcSpace ifcSpace = model.InstancesOfType<IfcSpace>().FirstOrDefault();
            IfcProduct ifcProduct = model.InstancesOfType<IfcProduct>().FirstOrDefault();

            if (string.IsNullOrEmpty(ifcBuildingStorey.GlobalId)) return ifcBuildingStorey.GlobalId.ToString();
            else if (string.IsNullOrEmpty(ifcSpace.GlobalId)) return ifcSpace.GlobalId.ToString();
            else if (string.IsNullOrEmpty(ifcProduct.GlobalId)) return ifcProduct.GlobalId.ToString();

            return DEFAULT_STRING;
        }
        #endregion
    }
}
