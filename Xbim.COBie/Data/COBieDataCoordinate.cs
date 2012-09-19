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
using Xbim.Ifc.GeometricConstraintResource;
using Xbim.Ifc.RepresentationResource;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the Coordinate tab.
    /// </summary>
    public class COBieDataCoordinate : COBieData<COBieCoordinateRow>
    {
        /// <summary>
        /// Data Coordinate constructor
        /// </summary>
        /// <param name="model">The context of the model being generated</param>
        public COBieDataCoordinate(COBieContext context) : base(context)
        { }

        #region Methods

        /// <summary>
        /// Fill sheet rows for Coordinate sheet
        /// </summary>
        /// <returns>COBieSheet<COBieCoordinateRow></returns>
        public override COBieSheet<COBieCoordinateRow> Fill()
        {
            ProgressIndicator.ReportMessage("Starting Coordinates...");

            //Create new sheet
            COBieSheet<COBieCoordinateRow> coordinates = new COBieSheet<COBieCoordinateRow>(Constants.WORKSHEET_COORDINATE);
            
            IEnumerable<IfcBuildingStorey> ifcBuildingStoreys = Model.InstancesOfType<IfcBuildingStorey>();

            IEnumerable<IfcSpace> ifcSpaces = Model.InstancesOfType<IfcSpace>().OrderBy(ifcSpace => ifcSpace.Name, new CompareIfcLabel());

            IEnumerable<IfcSpatialStructureElement> ifcSpatialStructureElements = ifcBuildingStoreys.Union<IfcSpatialStructureElement>(ifcSpaces);

            // get all IfcRelAggregates objects from IFC file
            //IEnumerable<IfcRelAggregates> ifcRelAggregates = Model.InstancesOfType<IfcRelAggregates>();
            //IfcTelecomAddress ifcTelecomAddres = Model.InstancesOfType<IfcTelecomAddress>().FirstOrDefault();
            //IfcCartesianPoint ifcCartesianPoint = Model.InstancesOfType<IfcCartesianPoint>().FirstOrDefault();
            
            //IfcOwnerHistory ifcOwnerHistory = Model.InstancesOfType<IfcOwnerHistory>().FirstOrDefault();
            //IfcProduct ifcProduct = Model.InstancesOfType<IfcProduct>().FirstOrDefault();
            //IfcClassification ifcClassification = Model.InstancesOfType<IfcClassification>().FirstOrDefault();
            //string applicationFullName = ifcApplication.ApplicationFullName;


            ProgressIndicator.Initialise("Creating Coordinates", ifcSpatialStructureElements.Count());

            foreach (IfcSpatialStructureElement ifcSpatialStructureElement in ifcSpatialStructureElements)
            {
                ProgressIndicator.IncrementAndUpdate();

                COBieCoordinateRow coordinate = new COBieCoordinateRow(coordinates);

                coordinate.Name = (string.IsNullOrEmpty(ifcSpatialStructureElement.Name.ToString())) ? DEFAULT_STRING : ifcSpatialStructureElement.Name.ToString();// (ifcBuildingStorey == null || ifcBuildingStorey.Name.ToString() == "") ? "CoordinateName" : ifcBuildingStorey.Name.ToString();
                
                coordinate.CreatedBy = GetTelecomEmailAddress(ifcSpatialStructureElement.OwnerHistory);
                coordinate.CreatedOn = GetCreatedOnDateAsFmtString(ifcSpatialStructureElement.OwnerHistory);
                coordinate.Category = GetCategory(ifcSpatialStructureElement);
                
                coordinate.RowName = coordinate.Name;
                IfcCartesianPoint ifcCartesianPoint = null;
                if (ifcSpatialStructureElement is IfcBuildingStorey)
                {
                    coordinate.SheetName = "Floor";
                    ifcCartesianPoint = (ifcSpatialStructureElement.ObjectPlacement as IfcLocalPlacement).RelativePlacement.Location;
                }
                else //is IfcSpace
                {
                    IfcSpace ifcSpace = ifcSpatialStructureElement as IfcSpace;
                    
                    //ifcSpace.ElevationWithFlooring;
                    coordinate.SheetName = "Space";
                    ifcCartesianPoint = (ifcSpatialStructureElement.ObjectPlacement as IfcLocalPlacement).RelativePlacement.Location;
                }
               

                coordinate.CoordinateXAxis = (ifcCartesianPoint != null) ? ifcCartesianPoint[0].ToString() : "0";
                coordinate.CoordinateYAxis = (ifcCartesianPoint != null) ? ifcCartesianPoint[1].ToString() : "0";
                coordinate.CoordinateZAxis = (ifcCartesianPoint != null) ? ifcCartesianPoint[2].ToString() : "0";
                coordinate.ExtSystem = ifcApplication.ApplicationFullName;
                coordinate.ExtObject = ifcSpatialStructureElement.GetType().Name;
                coordinate.ExtIdentifier = ifcSpatialStructureElement.GlobalId.ToString();
                coordinate.ClockwiseRotation = DEFAULT_NUMERIC;
                coordinate.ElevationalRotation = DEFAULT_NUMERIC;
                coordinate.YawRotation = DEFAULT_NUMERIC;

                coordinates.Rows.Add(coordinate);
            }
            ProgressIndicator.Finalise();
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

            return Constants.DEFAULT_STRING;
        }
        #endregion
    }
}
