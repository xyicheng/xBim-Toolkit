using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.COBie.Rows;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.UtilityResource;
using Xbim.Ifc2x3.ExternalReferenceResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.GeometricConstraintResource;
using Xbim.Ifc2x3.RepresentationResource;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.ProfileResource;


using Xbim.ModelGeometry.Scene;
using Xbim.ModelGeometry;
using System.IO;
using Xbim.Common.Geometry;






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
            //IEnumerable<XbimGeometryData> transGraph = GetTransformGraph();
            
            COBieSheet<COBieCoordinateRow> coordinates = new COBieSheet<COBieCoordinateRow>(Constants.WORKSHEET_COORDINATE);
            int count = 0;
            //if (transGraph.Any())
            //{
                ProgressIndicator.ReportMessage("Starting Coordinates...");


                //Create new sheet
                
                //Get buildings and spaces
                IEnumerable<IfcBuildingStorey> ifcBuildingStoreys = Model.Instances.OfType<IfcBuildingStorey>();
                IEnumerable<IfcSpace> ifcSpaces = Model.Instances.OfType<IfcSpace>().OrderBy(ifcSpace => ifcSpace.Name, new CompareIfcLabel());
                IEnumerable<IfcProduct> ifcProducts = ifcBuildingStoreys.Union<IfcProduct>(ifcSpaces); //add spaces

                //get component products as shown in Component sheet
                IEnumerable<IfcRelAggregates> relAggregates = Model.Instances.OfType<IfcRelAggregates>();
                IEnumerable<IfcRelContainedInSpatialStructure> relSpatial = Model.Instances.OfType<IfcRelContainedInSpatialStructure>();
                IEnumerable<IfcProduct> ifcElements = ((from x in relAggregates
                                                        from y in x.RelatedObjects
                                                        where !Context.Exclude.ObjectType.Component.Contains(y.GetType())
                                                        select y).Union(from x in relSpatial
                                                                        from y in x.RelatedElements
                                                                        where !Context.Exclude.ObjectType.Component.Contains(y.GetType())
                                                                        select y)).OfType<IfcProduct>();  //.GroupBy(el => el.Name).Select(g => g.First())
                ifcProducts = ifcProducts.Union(ifcElements);

                ProgressIndicator.Initialise("Creating Coordinates", ifcProducts.Count());
            
                foreach (IfcProduct ifcProduct in ifcProducts)
                {
                    count++;
                    ProgressIndicator.IncrementAndUpdate();
                    //if no name to link the row name too skip it, as no way to link back to the parent object
                    //if (string.IsNullOrEmpty(ifcProduct.Name))
                    //    continue;

                    COBieCoordinateRow coordinate = new COBieCoordinateRow(coordinates);
                    
                    coordinate.Name = (string.IsNullOrEmpty(ifcProduct.Name.ToString())) ? DEFAULT_STRING : ifcProduct.Name.ToString();// (ifcBuildingStorey == null || ifcBuildingStorey.Name.ToString() == "") ? "CoordinateName" : ifcBuildingStorey.Name.ToString();

                    coordinate.CreatedBy = GetTelecomEmailAddress(ifcProduct.OwnerHistory);
                    coordinate.CreatedOn = GetCreatedOnDateAsFmtString(ifcProduct.OwnerHistory);

                    
                    coordinate.RowName = coordinate.Name;
                    IfcCartesianPoint ifcCartesianPointLower = null;
                    IfcCartesianPoint ifcCartesianPointUpper = null;
                    double ClockwiseRotation = 0.0;
                    double ElevationalRotation = 0.0;
                    double YawRotation = 0.0;

                    if (ifcProduct is IfcBuildingStorey)
                    {
                        XbimGeometryData geoData = Model.GetGeometryData(ifcProduct, XbimGeometryType.BoundingBox).FirstOrDefault();
                        
                        if (geoData != null)
                        {
                            XbimMatrix3D worldMatrix = geoData.Transform;
                            ifcCartesianPointLower = new IfcCartesianPoint(worldMatrix.OffsetX, worldMatrix.OffsetY, worldMatrix.OffsetZ); //get the offset from the world coordinates system 0,0,0 point, i.e. origin point of this object in world space
                        }
                        coordinate.SheetName = "Floor";
                        coordinate.Category = "point";
                    }
                    else
                    {
                        XbimGeometryData geoData = Model.GetGeometryData(ifcProduct, XbimGeometryType.BoundingBox).FirstOrDefault();
                        if (geoData != null)
                        {
                            XbimRect3D boundBox = XbimRect3D.FromArray(geoData.ShapeData);
                            XbimMatrix3D worldMatrix = geoData.Transform;
                            //do the transform in the next call to the structure TransformedBoundingBox constructor
                            TransformedBoundingBox tranBox = new TransformedBoundingBox(boundBox, worldMatrix);
                            ClockwiseRotation = tranBox.ClockwiseRotation;
                            ElevationalRotation = tranBox.ElevationalRotation;
                            YawRotation = tranBox.YawRotation;
                            //set points
                            ifcCartesianPointLower = new IfcCartesianPoint(tranBox.MinPt);
                            ifcCartesianPointUpper = new IfcCartesianPoint(tranBox.MaxPt);
                        }
                        else
                        {
                            continue;//no info so skip
                        }
                        if (ifcProduct is IfcSpace)
                            coordinate.SheetName = "Space";
                        else
                            coordinate.SheetName = "Component";

                        coordinate.Category = "box-lowerleft"; //and box-upperright, so two values required when we do this


                    }



                    coordinate.CoordinateXAxis = (ifcCartesianPointLower != null) ? string.Format("{0:F4}", (double)ifcCartesianPointLower[0]) : "0.0";
                    coordinate.CoordinateYAxis = (ifcCartesianPointLower != null) ? string.Format("{0:F4}", (double)ifcCartesianPointLower[1]) : "0.0";
                    coordinate.CoordinateZAxis = (ifcCartesianPointLower != null) ? string.Format("{0:F4}", (double)ifcCartesianPointLower[2]) : "0.0";
                    coordinate.ExtSystem = GetExternalSystem(ifcProduct);
                    coordinate.ExtObject = ifcProduct.GetType().Name;
                    coordinate.ExtIdentifier = ifcProduct.GlobalId.ToString();
                    coordinate.ClockwiseRotation = ClockwiseRotation.ToString("F4");
                    coordinate.ElevationalRotation = ElevationalRotation.ToString("F4");
                    coordinate.YawRotation = YawRotation.ToString("F4");

                    coordinates.AddRow(coordinate);
                    if (ifcCartesianPointUpper != null) //we need a second row for upper point
                    {
                        COBieCoordinateRow coordinateUpper = new COBieCoordinateRow(coordinates);
                        coordinateUpper.Name = coordinate.Name;
                        coordinateUpper.CreatedBy = coordinate.CreatedBy;
                        coordinateUpper.CreatedOn = coordinate.CreatedOn;
                        coordinateUpper.RowName = coordinate.RowName;
                        coordinateUpper.SheetName = coordinate.SheetName;
                        coordinateUpper.Category = "box-upperright";
                        coordinateUpper.CoordinateXAxis = (ifcCartesianPointUpper != null) ? string.Format("{0:F4}", (double)ifcCartesianPointUpper[0]) : "0.0";
                        coordinateUpper.CoordinateYAxis = (ifcCartesianPointUpper != null) ? string.Format("{0:F4}", (double)ifcCartesianPointUpper[1]) : "0.0";
                        coordinateUpper.CoordinateZAxis = (ifcCartesianPointUpper != null) ? string.Format("{0:F4}", (double)ifcCartesianPointUpper[2]) : "0.0";
                        coordinateUpper.ExtSystem = coordinate.ExtSystem;
                        coordinateUpper.ExtObject = coordinate.ExtObject;
                        coordinateUpper.ExtIdentifier = coordinate.ExtIdentifier;
                        coordinateUpper.ClockwiseRotation = coordinate.ClockwiseRotation;
                        coordinateUpper.ElevationalRotation = coordinate.ElevationalRotation;
                        coordinateUpper.YawRotation = coordinate.YawRotation;

                        coordinates.AddRow(coordinateUpper);
                    }
                }

            coordinates.OrderBy(s => s.Name);
            
            ProgressIndicator.Finalise();
                

            return coordinates;
        }

        
        /// <summary>
        /// Get the Transform Graph for this model
        /// </summary>
        /// <returns>TransformGraph object</returns>
        private IEnumerable<XbimGeometryData> GetTransformGraph()
        {
            //get the Transform Graph which holds bounding boxes and Triangulated Geometry
            return Model.GetGeometryData(XbimGeometryType.BoundingBox);
        }
        #endregion
    }

    /// <summary>
    /// Structure to transform a bounding box values to world space
    /// </summary>
    public struct TransformedBoundingBox 
    {
        public TransformedBoundingBox(XbimRect3D boundBox, XbimMatrix3D matrix)
            : this()
	    {
            //Object space values
            
            MinPt = new XbimPoint3D(boundBox.X, boundBox.Y, boundBox.Z);
            MaxPt = new XbimPoint3D(boundBox.X + boundBox.SizeX, boundBox.Y + boundBox.SizeY, boundBox.Z + boundBox.SizeZ);
            //make assumption that the X direction will be the longer length hence the orientation will be along the x axis
            //transformed values, no longer a valid bounding box in the new space if any Pitch or Yaw
            MinPt = matrix.Transform(MinPt);
            MaxPt = matrix.Transform(MaxPt);

            
            //--------Calculate rotations from matrix-------
            //rotation around X,Y,Z axis
            double rotationZ, rotationY, rotationX;
            GetMatrixRotations(matrix, out rotationX, out rotationY, out rotationZ);
            
            //adjust Z to get clockwise rotation
            ClockwiseRotation = RTD(rotationZ * -1); //use * -1 to make a clockwise rotation
            ElevationalRotation = RTD(rotationY);
            YawRotation = RTD(rotationX);
            
	    }
        /// <summary>
        /// Get Euler angles from matrix
        /// derived for here i think!! http://khayyam.kaplinski.com/2011_06_01_archive.html
        /// </summary>
        /// <param name="m">XbimMatrix3D Matrix</param>
        /// <param name="rotationX">out parameter - X rotation in radians</param>
        /// <param name="rotationY">out parameter - Y rotation in radians</param>
        /// <param name="rotationZ">out parameter - Z rotation in radians</param>
        public static void GetMatrixRotations(XbimMatrix3D m, out double rotationX, out double rotationY, out double rotationZ)
        {
            double aX, aZ, aX2, aZ2;
            double aY = Math.Asin(m.M31);
            double aY2 = Math.PI - aY;
            if (Math.Abs(m.M31) != 1)
            {
                double C = Math.Cos(aY);
                double C2 = Math.Cos(aY2);
                double trX = m.M33 / C;
                double trY = -m.M32 / C;
                aX = Math.Atan2(trY, trX);
                aX2 = Math.Atan2(-m.M32 / C2, m.M33 / C2);
                trX = m.M11 / C;
                trY = -m.M21 / C;
                aZ = Math.Atan2(trY, trX);
                aZ2 = Math.Atan2(-m.M21 / C2, m.M11 / C2);
            }
            else
            {
                aX = aX2 = 0;
                double trX = m.M22;
                double trY = m.M12;
                aZ = aZ2 = Math.Atan2(trY, trX);
            }
            if ((aX * aX + aY * aY + aZ * aZ) < (aX2 * aX2 + aY2 * aY2 + aZ2 * aZ2))
            {
                rotationX = aX;
                rotationY = aY;
                rotationZ = aZ;
            }
            else
            {
                rotationX = aX2;
                rotationY = aY2;
                rotationZ = aZ2;
            }

        }
        

        /// <summary>
        /// Radians to Degrees
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double RTD (double value)
        {
            return value * (180 / Math.PI);
        }

        /// <summary>
        /// Degrees to Radians
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static double DTR(double value)
        {
            return value * (Math.PI / 180);
        }
        /// <summary>
        /// Minimum point, classed as origin point
        /// </summary>
        public XbimPoint3D MinPt { get; set; }
        /// <summary>
        /// Maximum point of the rectangle
        /// </summary>
        public XbimPoint3D MaxPt { get; set; }
        /// <summary>
        /// Clockwise rotation of the IfcProduct
        /// </summary>
        public double ClockwiseRotation { get; set; }
        /// <summary>
        /// Elevation rotation of the IfcProduct
        /// </summary>
        public double ElevationalRotation  { get; set; }
        /// <summary>
        /// Yaw rotation of the IfcProduct
        /// </summary>
        public double YawRotation { get; set; }

        
        
        
    }
}
