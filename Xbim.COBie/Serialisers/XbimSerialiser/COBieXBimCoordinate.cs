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
            using (Transaction trans = Model.BeginTransaction("Add Floor"))
            {

                try
                {
                    
                   //------------------------------TESTING----------------------------------
                    var xxx = Model.InstancesOfType<IfcLocalPlacement>().Last();
                    Matrix3D matrix = ConvertMatrix3D(xxx);

                    var yyy = Model.InstancesOfType<IfcGeometricRepresentationContext>();
                    //-----------------------------------------------------------------------

                    for (int i = 0; i < cOBieSheet.RowCount; i++)
                    {
                        COBieCoordinateRow row = cOBieSheet[i];
                        COBieCoordinateRow rowNext = null;

                        //do floor placement point
                        if ((ValidateString(row.Category)) &&  
                            (row.Category.ToLower() == "point")
                            )
                        {
                            AddFloorPlacment(row);
                            continue; //work done, next loop please
                        }
                        //do bounding box items
                        if ((ValidateString(row.Category)) && 
                            (row.Category.Contains("box-"))
                            )
                        {    
                            i = i++; //set to get next row

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
#if DEBUG
                                Console.WriteLine("{0} : {1} == {2} : {3} ", row.SheetName, row.RowName, rowNext.SheetName, rowNext.RowName);
#endif                            
                            }
                            else
                            {
#if DEBUG
                                Console.WriteLine("*************Failed to find pair*************");
#endif
                                i = i--; //set back in case next is point, as two box points failed
                            }

                            
                        }
                       
                    }

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
        /// <param name="row"></param>
        private void AddFloorPlacment(COBieCoordinateRow row)
        {
            IfcBuildingStorey ifcBuildingStorey = Model.InstancesWhere<IfcBuildingStorey>(bs => bs.Name == row.RowName).FirstOrDefault();
            if (ifcBuildingStorey != null)
            {
                IfcProduct placementRelToIfcProduct = ifcBuildingStorey.SpatialStructuralElementParent as IfcProduct;
                IfcLocalPlacement objectPlacement = SetObjectPlacement(row, placementRelToIfcProduct);
                if (objectPlacement != null)
                {
                    ifcBuildingStorey.ObjectPlacement = objectPlacement;
                }
            }
        }

        private IfcLocalPlacement SetObjectPlacement(COBieCoordinateRow row, IfcProduct placementRelToIfcProduct)
        {
            double x, y, z;
            if ((double.TryParse(row.CoordinateXAxis, out x)) &&
                (double.TryParse(row.CoordinateYAxis, out y)) &&
                (double.TryParse(row.CoordinateZAxis, out z))
                )
            {
                Point3D locationPt = new Point3D(x, y, z);
                if ((placementRelToIfcProduct != null) && (placementRelToIfcProduct.ObjectPlacement is IfcLocalPlacement))
                {
                    //TEST, change the building position to see if the same point comes out in Excel sheet, it should be, and in test was.
                    //((IfcAxis2Placement3D)((IfcLocalPlacement)placementRelToIfcProduct.ObjectPlacement).RelativePlacement).SetNewLocation(10.0, 10.0, 0.0);
                    IfcLocalPlacement placementRelTo = (IfcLocalPlacement)placementRelToIfcProduct.ObjectPlacement;
                    Matrix3D matrix3D = ConvertMatrix3D(placementRelTo);
                    //we want to take off the translations and rotations caused by IfcLocalPlacement of the parent objects as we will add these to the new IfcLocalPlacement for this floor
                    matrix3D.Invert(); //so invert matrix to remove the translations to give the origin for the next IfcLocalPlacement
                    locationPt = matrix3D.Transform(locationPt); //get the point with relation to the last IfcLocalPlacement i.e the parent element

                    Matrix3D matrixRotation3D = new Matrix3D();
                    double rotationX, rotationY, rotationZ;
                    if (double.TryParse(row.YawRotation, out rotationX))
                    {
                        Quaternion q = new Quaternion(new Vector3D(1, 0, 0), rotationX);
                        matrixRotation3D.Rotate(q);

                    }
                    if (double.TryParse(row.ElevationalRotation, out rotationY))
                    {
                        Quaternion q = new Quaternion(new Vector3D(0, 1, 0), rotationY);
                        matrixRotation3D.Rotate(q);
                    }
                    if (double.TryParse(row.ClockwiseRotation, out rotationZ))
                    {
                        rotationZ = 360.0 - rotationZ; //if anticlockwise rotation required
                        Quaternion q = new Quaternion(new Vector3D(0, 0, 1), rotationZ);
                        matrixRotation3D.Rotate(q);
                    }
                    Vector3D ucsXAxis = matrixRotation3D.Transform(new Vector3D(1, 0, 0));
                    Vector3D ucsZAxis = matrixRotation3D.Transform(new Vector3D(0, 0, 1));
                    ucsXAxis.Normalize();
                    ucsZAxis.Normalize();
                    //IfcDirection refDirection = Model.New<IfcDirection>(dir => dir.SetXYZ(ucsXAxis.X, ucsXAxis.Y, ucsXAxis.Z));
                    //IfcDirection axis = Model.New<IfcDirection>(dir => dir.SetXYZ(ucsZAxis.X, ucsZAxis.Y, ucsZAxis.Z)); 
                    IfcAxis2Placement3D relativePlacemant = Model.New<IfcAxis2Placement3D>();
                    relativePlacemant.SetNewDirectionOf_XZ(ucsXAxis.X, ucsXAxis.Y, ucsXAxis.Z, ucsZAxis.X, ucsZAxis.Y, ucsZAxis.Z);
                    relativePlacemant.SetNewLocation(locationPt.X, locationPt.Y, locationPt.Z);
                    IfcLocalPlacement objectPlacement = Model.New<IfcLocalPlacement>();
                    objectPlacement.PlacementRelTo = placementRelTo;
                    objectPlacement.RelativePlacement = relativePlacemant;
                    return objectPlacement;
                }
            }
            return null;
           
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
