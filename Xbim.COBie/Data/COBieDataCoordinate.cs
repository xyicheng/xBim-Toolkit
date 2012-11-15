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
using WVector = System.Windows.Vector;

using Xbim.ModelGeometry.Scene;
using Xbim.ModelGeometry;
using System.IO;
using System.Windows.Media.Media3D;






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
            
            //Get buildings and spaces
            IEnumerable<IfcBuildingStorey> ifcBuildingStoreys = Model.Instances.OfType<IfcBuildingStorey>();
            IEnumerable<IfcSpace> ifcSpaces = Model.Instances.OfType<IfcSpace>().OrderBy(ifcSpace => ifcSpace.Name, new CompareIfcLabel());
            IEnumerable<IfcProduct> ifcProducts = ifcBuildingStoreys.Union<IfcProduct>(ifcSpaces); //add spaces

            //get component products as shown in Component sheet
            IEnumerable<IfcRelAggregates> relAggregates = Model.Instances.OfType<IfcRelAggregates>();
            IEnumerable<IfcRelContainedInSpatialStructure> relSpatial = Model.Instances.OfType<IfcRelContainedInSpatialStructure>();
            IEnumerable<IfcProduct> ifcElements = ((from x in relAggregates
                                                   from y in x.RelatedObjects
                                                   where !Context.ComponentExcludeTypes.Contains(y.GetType())
                                                   select y).Union(from x in relSpatial
                                                                   from y in x.RelatedElements
                                                                   where !Context.ComponentExcludeTypes.Contains(y.GetType())
                                                                   select y)).GroupBy(el => el.Name).Select(g => g.First()).OfType<IfcProduct>();
            ifcProducts = ifcProducts.Union(ifcElements);

            ProgressIndicator.Initialise("Creating Coordinates", ifcProducts.Count());
            TransformGraph transGraph = GetTransformGraph();

            foreach (IfcProduct ifcProduct in ifcProducts)
            {
                ProgressIndicator.IncrementAndUpdate();

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
                    Matrix3D worldMatrix = transGraph.ProductNodes[ifcProduct.EntityLabel].WorldMatrix();
                    ifcCartesianPointLower = new IfcCartesianPoint(worldMatrix.OffsetX, worldMatrix.OffsetY, worldMatrix.OffsetZ); //get the offset from the world coordinates system 0,0,0 point, i.e. origin point of this object in world space
                    coordinate.SheetName = "Floor"; 
                    coordinate.Category = "point";
                    //ifcCartesianPoint = (ifcProduct.ObjectPlacement as IfcLocalPlacement).RelativePlacement.Location;
                }
                else 
                {
                    Rect3D boundBox = transGraph.ProductNodes[ifcProduct.EntityLabel].BoundingBox;
                    Matrix3D worldMatrix = transGraph.ProductNodes[ifcProduct.EntityLabel].WorldMatrix();
                    //do the transform in the next call to the structure TransformedBoundingBox constructor
                    TransformedBoundingBox tranBox = new TransformedBoundingBox(boundBox, worldMatrix);
                    ClockwiseRotation = tranBox.ClockwiseRotation;
                    ElevationalRotation = tranBox.ElevationalRotation;
                    YawRotation = tranBox.YawRotation;
                    if (ifcProduct is IfcSpace)
                        coordinate.SheetName = "Space";
                    else
                        coordinate.SheetName = "Component";

                    coordinate.Category = "box-lowerleft"; //and box-upperright, so two values required when we do this
                    //set points
                    ifcCartesianPointLower = new IfcCartesianPoint(tranBox.MinPt);
                    ifcCartesianPointUpper = new IfcCartesianPoint(tranBox.MaxPt);
                    
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

                coordinates.Rows.Add(coordinate);
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

                    coordinates.Rows.Add(coordinateUpper);
                }
            }
            ProgressIndicator.Finalise();
            return coordinates;
        }

        
        /// <summary>
        /// Get the Transform Graph for this model
        /// </summary>
        /// <returns>TransformGraph object</returns>
        private TransformGraph GetTransformGraph()
        {
            TransformGraph graph = null;
              
            if (Context.COBieGlobalValues.ContainsKey("FILENAME"))
            {
                string cacheFile = Context.COBieGlobalValues["FILENAME"];
                if (!string.IsNullOrEmpty(cacheFile))
                {
                    cacheFile = Path.ChangeExtension(cacheFile, ".xbimGC");
                    //if no Geometry file than create it
                    if (!File.Exists(cacheFile)) GenerateGeometry(cacheFile);
                    //now we have a file read it into the XbimSceneStream
                    if (File.Exists(cacheFile))
                    {
                        XbimSceneStream scene = new XbimSceneStream(Model, cacheFile);
                        graph = scene.Graph; //the graph holds product boundary box's so we will return it
                        scene.Close();
                    }
                }
            }
            return graph;

            //No xbimGC required but no bounding boxes returned, might use if speed is required
            //Xbim.ModelGeometry.Scene.TransformGraph graph = new Xbim.ModelGeometry.Scene.TransformGraph(Model);
            //graph.AddProducts(Model.Instances.OfType<IfcProduct>());
            //System.Windows.Media.Media3D.Matrix3D m3d = graph.ProductNodes[5260].WorldMatrix();
           

        }

        /// <summary>
        /// Create the xbimGC file
        /// </summary>
        /// <param name="cacheFile">file path to write file too</param>
        private void GenerateGeometry(string cacheFile)
        {
            //now convert the geometry
            IEnumerable<IfcProduct> toDraw = Model.IfcProducts.Cast<IfcProduct>(); //get all products for this model to place in return graph
            string modelName = Model.DatabaseName;
            Model.Close();
            Model.Open(modelName,XbimDBAccess.ReadWrite);           
            ProgressIndicator.Initialise("Create Geometry", 0);
            XbimScene.ConvertGeometry(toDraw, delegate(int percentProgress, object userState)
            {

                Context.UpdateStatus("Creating Geometry", percentProgress);
               
            }, false);
            Model.Close();
            Model.Open(modelName, XbimDBAccess.Read);          
            ProgressIndicator.Finalise();
        }

        /// <summary>
        /// Tests on structure TransformedBoundingBox
        /// </summary>
        //public void Test() //please keep for now as may use Vector calculations over matrix 
        //{
        //    Quaternion planangle = new Quaternion(new Vector3D(0.0, 0.0, 1.0), 45); // degree rotation on z axis
        //    Quaternion Yawangle = new Quaternion(new Vector3D(1.0, 0.0, 0.0), -35.005); // degree rotation on x axis
        //    Quaternion Pitchangle = new Quaternion(new Vector3D(0.0, 1.0, 0.0), 25.0); // degree rotation on y axis

        //    Matrix3D matrix = new Matrix3D(); // Get Identity matrix
        //    //matrix.RotateAt(Yawangle, new Point3D(0.0, 0.0, 0.0)); //rotate  on x axis
        //    matrix.RotateAt(Pitchangle, new Point3D(0.0, 0.0, 0.0)); //rotate  on y axis
        //    //matrix.RotateAt(planangle, new Point3D(0.0, 0.0, 0.0)); //rotate  on z axis
        //    Rect3D boundBox = new Rect3D(0.0, 0.0, 0.0, 20, 10, 5.0);

        //    Vector3D WorldX = new Vector3D(1.0, 0.0, 0.0);
        //    Vector3D WorldY = new Vector3D(0.0, 1.0, 0.0);
        //    Vector3D WorldZ = new Vector3D(0.0, 0.0, 1.0);
        //    TransformedBoundingBox tranBox = new TransformedBoundingBox(boundBox, matrix);
        //    Console.WriteLine("MinPt = {0}", tranBox.MinPt);
        //    Console.WriteLine("MaxPt = {0}", tranBox.MaxPt);
        //    Console.WriteLine("Rotation Clockwise on Plan (Z)  = {0}", tranBox.ClockwiseRotation);
        //    Console.WriteLine("Rotation Elevation (Y)  = {0}", tranBox.ElevationalRotation);
        //    Console.WriteLine("Rotation Yaw (X)  = {0}", tranBox.YawRotation);
        //    Console.WriteLine("-------------------------------");
        //    //double xxx = -Math.Asin(matrix.M31);
        //    //double rotationY = Math.Atan2(-matrix.M31, (Math.Sqrt(Math.Pow(matrix.M32, 2) + Math.Pow(matrix.M33, 2)))); //Math.Asin(matrix.M31); 
            
        //    //----------angle calculations via vector----------
        //    Vector3D RotationVector = new Vector3D(1.0, 0.0, 0.0);
        //    Vector3D YawRotationVector = new Vector3D(0.0, 1.0, 0.0);
        //    RotationVector = matrix.Transform(RotationVector);
        //    YawRotationVector = matrix.Transform(YawRotationVector);
        //    //ensure they are 1 unit in length
        //    RotationVector.Normalize();
        //    YawRotationVector.Normalize();

        //    //Note Counter Clockwise
        //    Vector3D flattenRot = new Vector3D(RotationVector.X, RotationVector.Y, 0.0); //remove the Z value so we only look at plan rotation
        //    flattenRot.Normalize();
        //    double RotProduct = Vector3D.DotProduct(WorldX, flattenRot);//dot product to the X Axis

        //    double rot = Math.Acos(Vector3D.DotProduct(WorldX, flattenRot)); //dot product to the X Axis

        //    double pitch = Math.Acos(Vector3D.DotProduct(WorldZ, RotationVector)); //dot product to the Z axis
        //    double yaw = Math.Acos(Vector3D.DotProduct(WorldZ, YawRotationVector));//dot product to the Z axis

        //    rot = rot * (180 / Math.PI);
        //    if (RotProduct < 0) rot = 360.0 - rot; //if dot product is negative then in 180 to 360 range

        //    yaw = 90.0 - (yaw * (180 / Math.PI));
        //    pitch = 90.0 - (pitch * (180 / Math.PI));

        //    Console.WriteLine("Rotation DotProduct  = {0}", RotProduct);
        //    Console.WriteLine("Plan Rotation = {0}", rot);
        //    Console.WriteLine("Elevation Rotation = {0}", pitch);
        //    Console.WriteLine("Yaw Rotation = {0}", yaw);

        //}
       
        #endregion
    }

    /// <summary>
    /// Structure to transform a bounding box values to world space
    /// </summary>
    struct TransformedBoundingBox 
    {
        public TransformedBoundingBox(Rect3D boundBox, Matrix3D matrix) : this()
	    {
            //Object space values
            MinPt = new Point3D(boundBox.X, boundBox.Y, boundBox.Z);
            MaxPt = new Point3D(boundBox.X + boundBox.SizeX, boundBox.Y + boundBox.SizeY, boundBox.Z + boundBox.SizeZ);
            //make assumption that the X direction will be the longer length hence the orientation will be along the x axis
           
            //transformed values, no longer a valid bounding box in the new space if any Pitch or Yaw
            MinPt = matrix.Transform(MinPt);
            MaxPt = matrix.Transform(MaxPt);
           
            //--------Calculate rotations from matrix-------
            //calculations from http://forums.codeguru.com/archive/index.php/t-329530.html and http://planning.cs.uiuc.edu/node103.html#eqn:angfrommat and http://nghiaho.com/?page_id=846
            //rotation around Z axis
            double rotationZ = Math.Atan2(matrix.M21, matrix.M11);
            rotationZ = RTD(rotationZ);
            //if negative then in 0 to 180.0 range counter clockwise from the 0.0 degree (assuming using RH rule), but we want clockwise so...
            if (rotationZ < 0)
                rotationZ = 360.0 + rotationZ; //rotZ is negative
            else //if positive then in 180 to 360 range counter clockwise measured from the 360 degree (assuming using RH rule) 
                rotationZ = Math.Abs(rotationZ);
            //zero anything out of range
            if (!((rotationZ > 0.0) && (rotationZ < 360.0))) rotationZ = 0.0;

            double rotationY = -Math.Asin(matrix.M31);  //Math.Atan2(-matrix.M31, (Math.Sqrt(Math.Pow(matrix.M32, 2) + Math.Pow(matrix.M33, 2))))
            double rotationX = Math.Atan2(matrix.M32, matrix.M33);
            
            ClockwiseRotation = rotationZ;
            ElevationalRotation = RTD(rotationY);
            YawRotation = RTD(rotationX);
            
	    }

        /// <summary>
        /// Radians to Degrees
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public double RTD (double value)
        {
            return value * (180 / Math.PI);
        }
        /// <summary>
        /// Minimum point, classed as origin point
        /// </summary>
        public Point3D MinPt { get; set; }
        /// <summary>
        /// Maximum point of the rectangle
        /// </summary>
        public Point3D MaxPt { get; set; }
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
