using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions.Transactions;
using Xbim.COBie.Rows;
using Xbim.Ifc.GeometricConstraintResource;
using Xbim.Ifc.RepresentationResource;
using System.Windows.Media.Media3D;
using Xbim.Ifc.GeometryResource;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.Extensions;
using Xbim.COBie.Data;
using Xbim.Ifc.ProfileResource;
using Xbim.Ifc.GeometricModelResource;
using Xbim.Ifc.UtilityResource;

namespace Xbim.COBie.Serialisers.XbimSerialiser
{
    public class COBieXBimCoordinate : COBieXBim
    {

        public COBieXBimCoordinate(COBieXBimContext xBimContext)
            : base(xBimContext)
        {

        }

        #region Methods
        /// <summary>
        /// Create and setup Bounding Box's
        /// </summary>
        /// <param name="cOBieSheet">COBieSheet of COBieCoordinateRow to read data from</param>
        public void SerialiseCoordinate(COBieSheet<COBieCoordinateRow> cOBieSheet)
        {
            using (Transaction trans = Model.BeginTransaction("Add Coordinate"))
            {

                try
                {
                    ProgressIndicator.ReportMessage("Starting Coordinates...");
                    ProgressIndicator.Initialise("Creating Coordinates", cOBieSheet.RowCount);
                    for (int i = 0; i < cOBieSheet.RowCount; i++)
                    {
                        COBieCoordinateRow row = cOBieSheet[i];
                        COBieCoordinateRow rowNext = null;

                        //do floor placement point
                        if ((ValidateString(row.Category)) &&  
                            (row.Category.ToLower() == "point")
                            )
                        {
                            ProgressIndicator.IncrementAndUpdate();
                        
                            AddFloorPlacement(row);
                            continue; //work done, next loop please
                        }
                        //do bounding box items
                        if ((ValidateString(row.Category)) && 
                            (row.Category.Contains("box-"))
                            )
                        {    
                            i++; //set to get next row

                            if (i < cOBieSheet.RowCount) //get next row if still in range
                                rowNext = cOBieSheet[i];

                            if ((rowNext != null) &&
                                (ValidateString(rowNext.Category)) && 
                                (rowNext.Category.Contains("box-")) &&
                                (ValidateString(row.SheetName)) && 
                                (ValidateString(row.RowName)) &&
                                (ValidateString(rowNext.SheetName)) &&
                                (ValidateString(rowNext.RowName)) &&
                                (row.SheetName == rowNext.SheetName) &&
                                (row.RowName == rowNext.RowName)
                                )
                            {
                                ProgressIndicator.IncrementAndUpdate();
                                
                                AddBoundingBoxAsExtrudedAreaSolid(row, rowNext);
                                
                                ProgressIndicator.IncrementAndUpdate(); //two row processed here
                        
#if DEBUG
                                Console.WriteLine("{0} : {1} == {2} : {3} ", row.SheetName, row.RowName, rowNext.SheetName, rowNext.RowName);
#endif                           
                                continue;
                            }
                            else
                            {
#if DEBUG
                                Console.WriteLine("*************Failed to find pair*************");
#endif
                                i--; //set back in case next is point, as two box points failed
                            }
                        }
                       
                    }
                    ProgressIndicator.Finalise();
                    
                    trans.Commit();

                }
                catch (Exception)
                {
                    trans.Rollback();
                    //TODO: Catch with logger?
                    throw;
                }

            }
        }

        /// <summary>
        /// Add floor placement point
        /// </summary>
        /// <param name="row">COBieCoordinateRow holding the data</param>
        private void AddFloorPlacement(COBieCoordinateRow row)
        {
            IfcBuildingStorey ifcBuildingStorey = null;
            if (ValidateString(row.ExtIdentifier))
            {
                IfcGloballyUniqueId id = new IfcGloballyUniqueId(row.ExtIdentifier);
                ifcBuildingStorey = Model.InstancesWhere<IfcBuildingStorey>(bs => bs.GlobalId == id).FirstOrDefault();
            }

            if ((ifcBuildingStorey == null) && (ValidateString(row.RowName)))
            {
                ifcBuildingStorey = Model.InstancesWhere<IfcBuildingStorey>(bs => bs.Name == row.RowName).FirstOrDefault();
            }

            if (ifcBuildingStorey != null)
            {
                IfcProduct placementRelToIfcProduct = ifcBuildingStorey.SpatialStructuralElementParent as IfcProduct;
                IfcLocalPlacement objectPlacement = CalcObjectPlacement(row, placementRelToIfcProduct);
                if (objectPlacement != null)
                {
                    //using statement will set the Model.OwnerHistoryAddObject to IfcRoot.OwnerHistory as OwnerHistoryAddObject is used upon any property changes, 
                    //then swaps the original OwnerHistoryAddObject back in the dispose, so set any properties within the using statement
                    using (COBieXBimEditScope context = new COBieXBimEditScope(Model, ifcBuildingStorey.OwnerHistory))
                    {
                        ifcBuildingStorey.ObjectPlacement = objectPlacement;
                    }
                }
            }
        }

        /// <summary>
        /// Add space placement
        /// </summary>
        /// <param name="row">COBieCoordinateRow holding the data for one corner</param>
        /// <param name="rowNext">COBieCoordinateRow holding the data for the other corner</param>
        private void AddBoundingBoxAsExtrudedAreaSolid(COBieCoordinateRow row, COBieCoordinateRow rowNext)
        {
            if (row.SheetName.ToLower() == "space")
            {
                IfcSpace ifcSpace = null;
                if (ValidateString(row.ExtIdentifier))
                {
                    IfcGloballyUniqueId id = new IfcGloballyUniqueId(row.ExtIdentifier);
                    ifcSpace = Model.InstancesWhere<IfcSpace>(bs => bs.GlobalId == id).FirstOrDefault();
                }
                if ((ifcSpace == null) && (ValidateString(row.RowName)))
                {
                    ifcSpace = Model.InstancesWhere<IfcSpace>(bs => bs.Name == row.RowName).FirstOrDefault();
                }
                if ((ifcSpace == null) && (ValidateString(row.RowName)))
                {
                    IEnumerable<IfcSpace> ifcSpaces = Model.InstancesWhere<IfcSpace>(bs => bs.Description == row.RowName);
                    //check we have one, if >1 then no match
                    if ((ifcSpaces.Any()) && (ifcSpaces.Count() == 1))
                        ifcSpace = ifcSpaces.FirstOrDefault();
                }

                if (ifcSpace != null)
                {
                    //using statement will set the Model.OwnerHistoryAddObject to IfcRoot.OwnerHistory as OwnerHistoryAddObject is used upon any property changes, 
                    //then swaps the original OwnerHistoryAddObject back in the dispose, so set any properties within the using statement
                    using (COBieXBimEditScope context = new COBieXBimEditScope(Model, ifcSpace.OwnerHistory))
                    {
                        IfcProduct placementRelToIfcProduct = ifcSpace.SpatialStructuralElementParent as IfcProduct;

                        AddExtrudedRectangle(row, rowNext, ifcSpace, placementRelToIfcProduct);
                    }
                }

            }

            if (row.SheetName.ToLower() == "component")
            {
                IfcElement ifcElement = null;
                if (ValidateString(row.ExtIdentifier))
                {
                    IfcGloballyUniqueId id = new IfcGloballyUniqueId(row.ExtIdentifier);
                    ifcElement = Model.InstancesWhere<IfcElement>(bs => bs.GlobalId == id).FirstOrDefault();
                }
                if ((ifcElement == null) && (ValidateString(row.RowName)))
                {
                    ifcElement = Model.InstancesWhere<IfcElement>(bs => bs.Name == row.RowName).FirstOrDefault();
                }

                if ((ifcElement == null) && (ValidateString(row.RowName)))
                {
                    IEnumerable<IfcElement> ifcElements = Model.InstancesWhere<IfcElement>(bs => bs.Description == row.RowName);
                    //check we have one, if >1 then no match
                    if ((ifcElements.Any()) && (ifcElements.Count() == 1))
                        ifcElement = ifcElements.FirstOrDefault();
                }

                if (ifcElement != null)
                {
                    //using statement will set the Model.OwnerHistoryAddObject to IfcRoot.OwnerHistory as OwnerHistoryAddObject is used upon any property changes, 
                    //then swaps the original OwnerHistoryAddObject back in the dispose, so set any properties within the using statement
                    using (COBieXBimEditScope context = new COBieXBimEditScope(Model, ifcElement.OwnerHistory))
                    {
                        IfcProduct placementRelToIfcProduct = ifcElement.ContainedInStructure as IfcProduct;
                        IfcRelContainedInSpatialStructure ifcRelContainedInSpatialStructure = Model.InstancesOfType<IfcRelContainedInSpatialStructure>().Where(rciss => rciss.RelatedElements.Contains(ifcElement)).FirstOrDefault();
                        if ((ifcRelContainedInSpatialStructure != null) &&
                            (ifcRelContainedInSpatialStructure.RelatingStructure != null)
                            )
                        {
                            placementRelToIfcProduct = ifcRelContainedInSpatialStructure.RelatingStructure as IfcProduct;
                            AddExtrudedRectangle(row, rowNext, ifcElement, placementRelToIfcProduct);
                        }
                        else
                        {
#if DEBUG
                            Console.WriteLine("COBieXBimCoordinate.AddBoundingBoxAsExtrudedAreaSolid: Cannot find Parent object placement");
#endif                        
                        }
                    }
                    
                }
                else
                {
#if DEBUG
                    Console.WriteLine("COBieXBimCoordinate.AddBoundingBoxAsExtrudedAreaSolid: Cannot find object to relate points too");
#endif
                }
            }
            
        }

        /// <summary>
        /// Add a Bounding Box extrusion onto the ifcProduct
        /// </summary>
        /// <param name="row">COBieCoordinateRow holding the data for one corner</param>
        /// <param name="rowNext">COBieCoordinateRow holding the data for the other corner</param>
        /// <param name="placementRelToIfcProduct">Product which is parent of ifcProduct passed product to add extrusion onto</param>
        /// <param name="ifcProduct">IfcProduct to add the extrusion onto</param>
        private void AddExtrudedRectangle(COBieCoordinateRow row, COBieCoordinateRow rowNext, IfcProduct ifcProduct, IfcProduct placementRelToIfcProduct)
        {
            if (ifcProduct != null)
            {
                COBieCoordinateRow lowerLeftRow, upperRightRow;
                if (row.Category.ToLower() == "box-lowerleft")
                {
                    lowerLeftRow = row;
                    upperRightRow = rowNext;
                }
                else
                {
                    lowerLeftRow = rowNext;
                    upperRightRow = row;
                }
                IfcLocalPlacement objectPlacement = CalcObjectPlacement(lowerLeftRow, placementRelToIfcProduct);
                if (objectPlacement != null)
                {
                    //set the object placement for the space
                    ifcProduct.ObjectPlacement = objectPlacement;

                    //get matrix to the space placement
                    Matrix3D matrix3D = ConvertMatrix3D(objectPlacement);
                    //invert matrix so we can convert row points back to the object space
                    matrix3D.Invert();
                    //lets get the points from the two rows
                    Point3D lowpt, highpt;
                    if ((GetPointFromRow(upperRightRow, out highpt)) &&
                         (GetPointFromRow(lowerLeftRow, out lowpt))
                        )
                    {
                        //transform the points back to object space
                        lowpt = matrix3D.Transform(lowpt);
                        highpt = matrix3D.Transform(highpt);
                        //in object space so we can use Rect3D as this will be aligned with coordinates systems X and Y
                        Rect3D bBox = new Rect3D();
                        bBox.Location = lowpt;
                        bBox.Union(highpt);
                        Point3D ctrPt = new Point3D(bBox.X + (bBox.SizeX / 2.0), bBox.Y + (bBox.SizeY / 2.0), bBox.Z + (bBox.SizeZ / 2.0));

                        //Create IfcRectangleProfileDef
                        IfcCartesianPoint IfcCartesianPointCtr = Model.New<IfcCartesianPoint>(cp => { cp.X = ctrPt.X; cp.Y = ctrPt.Y; cp.Z = 0.0; }); //centre point of 2D box
                        IfcDirection IfcDirectionXDir = Model.New<IfcDirection>(d => { d.X = 1.0; d.Y = 0; d.Z = 0.0; }); //default to X direction
                        IfcAxis2Placement2D ifcAxis2Placement2DCtr = Model.New<IfcAxis2Placement2D>(a2p => { a2p.Location = IfcCartesianPointCtr; a2p.RefDirection = IfcDirectionXDir; });
                        IfcRectangleProfileDef ifcRectangleProfileDef = Model.New<IfcRectangleProfileDef>(rpd => { rpd.ProfileType = IfcProfileTypeEnum.AREA; rpd.ProfileName = row.RowName; rpd.Position = ifcAxis2Placement2DCtr; rpd.XDim = bBox.SizeX; rpd.YDim = bBox.SizeY; });

                        //Create IfcExtrudedAreaSolid
                        IfcDirection IfcDirectionAxis = Model.New<IfcDirection>(d => { d.X = 0.0; d.Y = 0; d.Z = 1.0; }); //default to Z direction
                        IfcDirection IfcDirectionRefDir = Model.New<IfcDirection>(d => { d.X = 1.0; d.Y = 0; d.Z = 0.0; }); //default to X direction
                        IfcCartesianPoint IfcCartesianPointPosition = Model.New<IfcCartesianPoint>(cp => { cp.X = 0.0; cp.Y = 0.0; cp.Z = 0.0; }); //centre point of 2D box
                        IfcAxis2Placement3D ifcAxis2Placement3DPosition = Model.New<IfcAxis2Placement3D>(a2p3D => { a2p3D.Location = IfcCartesianPointPosition; a2p3D.Axis = IfcDirectionAxis; a2p3D.RefDirection = IfcDirectionRefDir; });
                        IfcDirection IfcDirectionExtDir = Model.New<IfcDirection>(d => { d.X = 0.0; d.Y = 0; d.Z = 1.0; }); //default to Z direction
                        IfcExtrudedAreaSolid ifcExtrudedAreaSolid = Model.New<IfcExtrudedAreaSolid>(eas => { eas.SweptArea = ifcRectangleProfileDef; eas.Position = ifcAxis2Placement3DPosition; eas.ExtrudedDirection = IfcDirectionExtDir; eas.Depth = bBox.SizeZ; });

                        //Create IfcShapeRepresentation
                        IfcShapeRepresentation ifcShapeRepresentation = Model.New<IfcShapeRepresentation>(sr => { sr.ContextOfItems = Model.IfcProject.ModelContext(); sr.RepresentationIdentifier = "Body"; sr.RepresentationType = "SweptSolid"; });
                        ifcShapeRepresentation.Items.Add_Reversible(ifcExtrudedAreaSolid);

                        //create IfcProductDefinitionShape
                        IfcProductDefinitionShape ifcProductDefinitionShape = Model.New<IfcProductDefinitionShape>(pds => { pds.Name = row.Name; pds.Description = row.SheetName; });
                        ifcProductDefinitionShape.Representations.Add_Reversible(ifcShapeRepresentation);

                        //Link to the IfcProduct
                        ifcProduct.Representation = ifcProductDefinitionShape;
                    }
                }
                else
                {
#if DEBUG
                    Console.WriteLine("Failed to add Object placement");
#endif
                }

            }
        }

        /// <summary>
        /// Calculate the ObjectPlacment for an IfcProduct from row data and the parent object
        /// </summary>
        /// <param name="row">COBieCoordinateRow holding the data</param>
        /// <param name="placementRelToIfcProduct">IfcProduct that the ObjectPlacment relates too, i.e. the parent of the ifcProduct ObjectPlacment we are calculating</param>
        /// <returns></returns>
        private IfcLocalPlacement CalcObjectPlacement(COBieCoordinateRow row, IfcProduct placementRelToIfcProduct)
        {
            Point3D locationPt;
            bool havePoint = GetPointFromRow(row, out locationPt);
            if (havePoint)
            {
                if ((placementRelToIfcProduct != null) && (placementRelToIfcProduct.ObjectPlacement is IfcLocalPlacement))
                {
                    //TEST, change the building position to see if the same point comes out in Excel sheet, it should be, and in test was.
                    //((IfcAxis2Placement3D)((IfcLocalPlacement)placementRelToIfcProduct.ObjectPlacement).RelativePlacement).SetNewLocation(10.0, 10.0, 0.0);
                    IfcLocalPlacement placementRelTo = (IfcLocalPlacement)placementRelToIfcProduct.ObjectPlacement;
                    Matrix3D matrix3D = ConvertMatrix3D(placementRelTo);
                    //we want to take off the translations and rotations caused by IfcLocalPlacement of the parent objects as we will add these to the new IfcLocalPlacement for this floor
                    matrix3D.Invert(); //so invert matrix to remove the translations to give the origin for the next IfcLocalPlacement
                    locationPt = matrix3D.Transform(locationPt); //get the point with relation to the last IfcLocalPlacement i.e the parent element
                    
                    //rotation around X,Y,Z axis for the matrix at the placementRelTo IfcLocalPlacement, we need to remove this from the row value so the object placement + this placement will equal the row rotation
                    double lastRotationZ, lastRotationY, lastRotationX;
                    matrix3D.Invert(); //invert matrix back to original to get rotations
                    TransformedBoundingBox.GetMatrixRotations(matrix3D, out lastRotationX, out lastRotationY, out lastRotationZ);
                    //convert to degrees
                    lastRotationZ = TransformedBoundingBox.RTD(lastRotationZ);
                    lastRotationZ = TransformedBoundingBox.MakeZRotationClockwise(lastRotationZ);//make clockwise
                    lastRotationY = TransformedBoundingBox.RTD(lastRotationY);
                    lastRotationX = TransformedBoundingBox.RTD(lastRotationX);

                    //lets account for the rotations obtained from the original matrix on the point extraction to the excel sheet
                    Matrix3D matrixRotation3D = new Matrix3D();
                    double rotationX, rotationY, rotationZ;
                    if (double.TryParse(row.YawRotation, out rotationX))
                    {
                        rotationX = (rotationX - lastRotationX) * -1; //switch rotation direction to match original direction on extracted xls sheet
                        Quaternion q = new Quaternion(new Vector3D(1, 0, 0), rotationX);
                        //matrixRotation3D.Rotate(q);
                        matrixRotation3D.RotatePrepend(q);
                    }
                    if (double.TryParse(row.ElevationalRotation, out rotationY))
                    {
                        rotationY = (rotationY - lastRotationY) * -1; //switch rotation direction to match original direction on extracted xls sheet, 
                        Quaternion q = new Quaternion(new Vector3D(0, 1, 0), rotationY);
                        //matrixRotation3D.Rotate(q);
                        matrixRotation3D.RotatePrepend(q);
                    }
                    if (double.TryParse(row.ClockwiseRotation, out rotationZ))
                    {
                        rotationZ = rotationZ - lastRotationZ;
                        rotationZ = 360.0 - rotationZ; //if anticlockwise rotation required, see TransformedBoundingBox structure for why
                        Quaternion q = new Quaternion(new Vector3D(0, 0, 1), rotationZ);
                        //matrixRotation3D.Rotate(q);
                        matrixRotation3D.RotatePrepend(q);
                    }

                    //set up the coordinates directions for this local placement, might need to look at the rotations of the parent placementRelTo on a merge, but for straight extraction we should be OK here
                    Vector3D ucsXAxis = matrixRotation3D.Transform(new Vector3D(1, 0, 0));
                    Vector3D ucsZAxis = matrixRotation3D.Transform(new Vector3D(0, 0, 1));
                    ucsXAxis.Normalize();
                    ucsZAxis.Normalize();
                    
                    //set up AxisPlacment
                    IfcAxis2Placement3D relativePlacemant = Model.New<IfcAxis2Placement3D>();
                    relativePlacemant.SetNewDirectionOf_XZ(ucsXAxis.X, ucsXAxis.Y, ucsXAxis.Z, ucsZAxis.X, ucsZAxis.Y, ucsZAxis.Z);
                    relativePlacemant.SetNewLocation(locationPt.X, locationPt.Y, locationPt.Z);
                    
                    //Set up Local Placement
                    IfcLocalPlacement objectPlacement = Model.New<IfcLocalPlacement>();
                    objectPlacement.PlacementRelTo = placementRelTo;
                    objectPlacement.RelativePlacement = relativePlacemant;

                    return objectPlacement;
                }
            }
            return null;
           
        }

        private bool GetPointFromRow(COBieCoordinateRow row, out Point3D point)
        {
            double x, y, z;
            if ((double.TryParse(row.CoordinateXAxis, out x)) &&
                (double.TryParse(row.CoordinateYAxis, out y)) &&
                (double.TryParse(row.CoordinateZAxis, out z))
                )
            {
                point = new Point3D(x, y, z);
                return true;
            }
            else
            {
                point = new Point3D();
                return false;
            }
        }

        /// <summary>
        /// Builds a windows Matrix3D from an ObjectPlacement
        /// Conversion fo c++ function CartesianTransform::ConvertMatrix3D from CartesianTransform.cpp
        /// </summary>
        /// <param name="objPlacement">IfcObjectPlacement object</param>
        /// <returns>Matrix3D</returns>
		protected Matrix3D ConvertMatrix3D(IfcObjectPlacement objPlacement)
		{
			if(objPlacement is IfcLocalPlacement)
			{
				IfcLocalPlacement locPlacement = (IfcLocalPlacement)objPlacement;
				if (locPlacement.RelativePlacement is IfcAxis2Placement3D)
				{
					IfcAxis2Placement3D axis3D = (IfcAxis2Placement3D)locPlacement.RelativePlacement;
					Vector3D ucsXAxis = new Vector3D(axis3D.RefDirection.DirectionRatios[0], axis3D.RefDirection.DirectionRatios[1], axis3D.RefDirection.DirectionRatios[2]);
					Vector3D ucsZAxis = new Vector3D(axis3D.Axis.DirectionRatios[0], axis3D.Axis.DirectionRatios[1], axis3D.Axis.DirectionRatios[2]);
					ucsXAxis.Normalize();
					ucsZAxis.Normalize();
					Vector3D ucsYAxis = Vector3D.CrossProduct(ucsZAxis, ucsXAxis);
					ucsYAxis.Normalize();
					Point3D ucsCentre = axis3D.Location.WPoint3D();

					Matrix3D ucsTowcs = new Matrix3D(	ucsXAxis.X, ucsXAxis.Y, ucsXAxis.Z, 0,
						ucsYAxis.X, ucsYAxis.Y, ucsYAxis.Z, 0,
						ucsZAxis.X, ucsZAxis.Y, ucsZAxis.Z, 0,
						ucsCentre.X, ucsCentre.Y, ucsCentre.Z , 1);
					if (locPlacement.PlacementRelTo != null)
					{
						return Matrix3D.Multiply(ucsTowcs, ConvertMatrix3D(locPlacement.PlacementRelTo));
					}
					else
						return ucsTowcs;

				}
				else //must be 2D
				{
					throw new NotImplementedException("Support for Placements other than 3D not implemented");
				}

			}
			else //probably a Grid
			{
				throw new NotImplementedException("Support for Placements other than Local not implemented");
			}
        }
        #endregion
    }
}
