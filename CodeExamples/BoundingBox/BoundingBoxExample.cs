using System;
using System.Windows.Media.Media3D;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions;

namespace CodeExamples.BoundingBox
{
    public class BoundingBoxExample : ISample
    {

        public void Run()
        {
            string filesName = @"Clinic_Example.ifc";

            using (IModel model = new XbimFileModelServer())
            {
                //Create the XBim file
                model.Open(filesName, delegate(int percentProgress, object userState)
                {
                    Console.Write("\rReading File {0}", percentProgress);
                }
                );

                //create the Geometry 
                GeometryWorker geoBoxs = new GeometryWorker(model, filesName);
                //get the Transform Graph which holds bounding boxes and Triangulated Geometry
                TransformGraph transGraph = geoBoxs.GetTransformGraph();
                //say we want the bounding box of a IfcProduct, using its Entity label we can extract it from the TransformGraph ProductNodes property
                long productEntityLable = 175644; //some IfcProduct in this case, from ifc file #175644=IFCWALL('0ayGa9swL6Jvmb9DQx3xNk',#33,'Basic Wall:Exterior - Insul Panel on Mtl. Stud:307047',$,'Basic Wall:Exterior - Insul Panel on Mtl. Stud:130954',#175368,#175643,'307047');
                //if ait exists in the graph ProductNodes keys
                if (transGraph.ProductNodes.ContainsKey(productEntityLable))
                {
                    //get the TransformNode
                    TransformNode transformNode = transGraph.ProductNodes[productEntityLable];
                    //from the TransformNode we can get the products bounding box in object space as a Rect3D
                    Rect3D boundBox = transformNode.BoundingBox;
                    //Object space max and min point values of the box
                    Point3D MinPtOCS = new Point3D(boundBox.X, boundBox.Y, boundBox.Z);
                    Point3D MaxPtOCS = new Point3D(boundBox.X + boundBox.SizeX, boundBox.Y + boundBox.SizeY, boundBox.Z + boundBox.SizeZ);

                    //if we want to convert to World space we can use the WorldMatrix method from the TransformNode
                    Matrix3D worldMatrix = transformNode.WorldMatrix();
                    //transformed values, may no longer a valid bounding box in the new space if any Pitch or Yaw, i.e. stairs ceiling supports
                    Point3D MinPtWCS = worldMatrix.Transform(MinPtOCS);
                    Point3D MaxPtWCS = worldMatrix.Transform(MaxPtOCS);
                    //if you product is at any angle to the World space then the bounding box can be recalculated, 
                    //a example of this can be found here https://sbpweb.svn.codeplex.com/svn/SBPweb.Workbench/Workbench%20Framework%202.0.0.x/Presentation/Windows.WPF/Utils/Maths.cs 
                    //in the TransformBounds function
                    Console.WriteLine("\n---------------------------------------------");
                    Console.WriteLine("Entity Label = {0}", productEntityLable);
                    Console.WriteLine("Object space minimum point {0}", MinPtOCS);
                    Console.WriteLine("Object space maximum point {0}", MaxPtOCS);
                    Console.WriteLine("World space minimum point {0}", MinPtWCS);
                    Console.WriteLine("World space maximum point {0}", MaxPtWCS);
                    Console.WriteLine("---------------------------------------------");

                }


                Console.WriteLine("\nFinished");
            }
            Console.ReadKey();
        }
    }
}
