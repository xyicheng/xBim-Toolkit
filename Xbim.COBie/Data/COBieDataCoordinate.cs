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
using Xbim.Ifc.GeometricModelResource;
using Xbim.Ifc.ProfileResource;
using WVector = System.Windows.Vector;

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
                    IfcSpace ifcSpace = ifcSpatialStructureElement as IfcSpace; //cast to IfcSpace
                    //GetBoundingBox(ifcSpace);

                    //ifcSpace.ElevationWithFlooring;
                    coordinate.SheetName = "Space";

                    //temp point fill for now
                    IfcProductDefinitionShape ifcProductDefinitionShape = ifcSpace.Representation as IfcProductDefinitionShape;
                    if (ifcProductDefinitionShape != null)
                    {
                        IfcExtrudedAreaSolid ifcExtrudedAreaSolid = ifcProductDefinitionShape.Representations.OfType<IfcShapeRepresentation>().SelectMany(eas => eas.Items).OfType<IfcExtrudedAreaSolid>().FirstOrDefault();
                    
                        ifcCartesianPoint = ifcExtrudedAreaSolid.Position.Location;
                    }
                }


                coordinate.CoordinateXAxis = (ifcCartesianPoint != null) ? string.Format("{0:F4}", (double)ifcCartesianPoint[0]) : "0.0";
                coordinate.CoordinateYAxis = (ifcCartesianPoint != null) ? string.Format("{0:F4}", (double)ifcCartesianPoint[1]) : "0.0";
                coordinate.CoordinateZAxis = (ifcCartesianPoint != null) ? string.Format("{0:F4}", (double)ifcCartesianPoint[2]) : "0.0";
                coordinate.ExtSystem = GetExternalSystem(ifcSpatialStructureElement);
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

        /// <summary>
        /// TODO: Bounding box for space
        /// </summary>
        /// <param name="ifcSpace"></param>
        private void GetBoundingBox(IfcSpace ifcSpace)
        {
            IfcProductDefinitionShape ifcProductDefinitionShape = ifcSpace.Representation as IfcProductDefinitionShape;

            if (ifcProductDefinitionShape != null)
            {
                IfcLocalPlacement spaceObjPlacement = ifcSpace.ObjectPlacement as IfcLocalPlacement;
                IfcAxis2Placement3D spaceRelPlacment = spaceObjPlacement.RelativePlacement as IfcAxis2Placement3D;
                IfcCartesianPoint spacePt = spaceRelPlacment.Location;

                IEnumerable<IfcShapeRepresentation> ifcShapeRepresentations = ifcProductDefinitionShape.Representations.OfType<IfcShapeRepresentation>();
                IEnumerable<IfcExtrudedAreaSolid> ifcExtrudedAreaSolids = ifcShapeRepresentations.SelectMany(eas => eas.Items).OfType<IfcExtrudedAreaSolid>();
                List<IfcCartesianPoint> ifcCartesianPoints = new List<IfcCartesianPoint>();
                foreach (IfcExtrudedAreaSolid ifcExtrudedAreaSolid in ifcExtrudedAreaSolids)
                {
                    IfcAxis2Placement3D extrudePlacment2D = ifcExtrudedAreaSolid.Position;
                    WVector extrudeXvector = extrudePlacment2D.P[0].WVector();
                    WVector extrudeYvector = extrudePlacment2D.P[1].WVector();

                    IfcRectangleProfileDef ifcRectangleProfileDef = ifcExtrudedAreaSolid.SweptArea as IfcRectangleProfileDef;
                    if (ifcRectangleProfileDef != null)
                    {
                        //get extrusion depth, assume this is the height of the space box
                        double Ht = (ifcExtrudedAreaSolid != null) ? (double)ifcExtrudedAreaSolid.Depth : 0.0;

                        IfcAxis2Placement2D rectanglePlacment2D = ifcRectangleProfileDef.Position;
                        IfcCartesianPoint ctrPt = rectanglePlacment2D.Location; //centre point in object coordinate system??
                        double xDim = ifcRectangleProfileDef.XDim; //X dimension
                        double yDim = ifcRectangleProfileDef.YDim; //Y dimension

                        //get vectors
                        WVector xVector = rectanglePlacment2D.P[0].WVector();
                        WVector yVector = rectanglePlacment2D.P[1].WVector();
                        xVector.Normalize();
                        yVector.Normalize();
                        xVector = xVector * (xDim / 2.0);
                        yVector = yVector * (yDim / 2.0);
                        WVector xVectorNegate = xVector * (xDim / 2.0);
                        WVector yVectorNegate = yVector * (yDim / 2.0);
                        xVectorNegate.Negate();
                        yVectorNegate.Negate();

                        //do it long hand
                        IfcCartesianPoint BtmRightPt = new IfcCartesianPoint(ctrPt.X + xVector.X, ctrPt.Y + yVectorNegate.Y);       //Bottom right Point in object coordinate system.
                        IfcCartesianPoint TopRightPt = new IfcCartesianPoint(ctrPt.X + xVector.X, ctrPt.Y + yVector.Y);             //Top right Point in object coordinate system.
                        IfcCartesianPoint TopLeftPt = new IfcCartesianPoint(ctrPt.X + xVectorNegate.X, ctrPt.Y + yVector.Y);        //Top left Point in object coordinate system.
                        IfcCartesianPoint BtmLeftPt = new IfcCartesianPoint(ctrPt.X + xVectorNegate.X, ctrPt.Y + yVectorNegate.Y);  //Bottom left Point in object coordinate system.

                        //TODO: use transformation matrix to get back to the WCS



                    }
                }
            }
            
        }

        //private string GetExtObject(IModel model)
        //{
        //    IfcBuildingStorey ifcBuildingStorey = model.InstancesOfType<IfcBuildingStorey>().FirstOrDefault();
        //    IfcSpace ifcSpace = model.InstancesOfType<IfcSpace>().FirstOrDefault();
        //    IfcProduct ifcProduct = model.InstancesOfType<IfcProduct>().FirstOrDefault();

        //    if (string.IsNullOrEmpty(ifcBuildingStorey.GlobalId)) return ifcBuildingStorey.GlobalId.ToString();
        //    else if (string.IsNullOrEmpty(ifcSpace.GlobalId)) return ifcSpace.GlobalId.ToString();
        //    else if (string.IsNullOrEmpty(ifcProduct.GlobalId)) return ifcProduct.GlobalId.ToString();

        //    return Constants.DEFAULT_STRING;
        //}
        #endregion
    }
}
