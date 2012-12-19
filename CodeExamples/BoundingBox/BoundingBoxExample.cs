using System;
using System.Windows.Media.Media3D;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc2x3.Kernel;
using Xbim.ModelGeometry;

namespace CodeExamples.BoundingBox
{
    public class BoundingBoxExample : ISample
    {

        public void Run()
        {
            string filesName = @"Clinic_Example.ifc";
            long productEntityLabel = 48284; //some IfcProduct
                        
            if (File.Exists(filesName))
            {
                using (XbimModel model = new XbimModel())
                {
                    string xbimFile = Path.ChangeExtension(filesName, "xBIM");
                    if (Path.GetExtension(filesName).ToLower() == ".xbim")
                    {
                        xbimFile = filesName;
                    }
                    else
                    {
                        model.CreateFrom(filesName, xbimFile, delegate(int percentProgress, object userState)
                        {
                            Console.Write("\rReading File {0}", percentProgress);
                        }
                    );
                    }
                    model.Open(xbimFile, XbimDBAccess.ReadWrite);
                    
                    //create the Geometry 
                    IEnumerable<IfcProduct> toDraw = model.IfcProducts.Cast<IfcProduct>(); //get all products for this model to place in return graph
                    XbimScene.ConvertGeometry(toDraw, delegate(int percentProgress, object userState)
                    {
                        Console.Write("\rCreating Geometry {0}", percentProgress);
                    }, false);
                    
                    //get the Transform Graph which holds bounding boxes and Triangulated Geometry
                    IEnumerable<XbimGeometryData> transGraph = model.GetGeometryData(XbimGeometryType.BoundingBox);
                    if (transGraph.Any())
                    {
                        //say we want the bounding box of a IfcProduct, using its Entity label we can extract it from the TransformGraph ProductNodes property
                        //if it exists in the graph ProductNodes keys
                        XbimGeometryData geoData = transGraph.Where(gd => gd.IfcProductLabel == productEntityLabel).FirstOrDefault();
                        if (geoData != null)
                        {
                            Matrix3D worldMatrix = new Matrix3D().FromArray(geoData.TransformData);
                            //Get the bounding box from the XbimGeometryData data
                            Rect3D boundBox = new Rect3D().FromArray(geoData.ShapeData);
                            //if we want to convert to World space we can use the geoData.TransformData property and create the world matrix
                            Point3D MinPtOCS = new Point3D(boundBox.X, boundBox.Y, boundBox.Z);
                            Point3D MaxPtOCS = new Point3D(boundBox.X + boundBox.SizeX, boundBox.Y + boundBox.SizeY, boundBox.Z + boundBox.SizeZ);
                            //transformed values, may no longer a valid bounding box in the new space if any Pitch or Yaw, i.e. stairs ceiling supports
                            Point3D MinPtWCS = worldMatrix.Transform(MinPtOCS);
                            Point3D MaxPtWCS = worldMatrix.Transform(MaxPtOCS);
                            //if you product is at any angle to the World space then the bounding box can be recalculated, 
                            //a example of this can be found here https://sbpweb.svn.codeplex.com/svn/SBPweb.Workbench/Workbench%20Framework%202.0.0.x/Presentation/Windows.WPF/Utils/Maths.cs 
                            //in the TransformBounds function
                            Console.WriteLine("\n---------------------------------------------");
                            Console.WriteLine("Entity Type = {0}", IfcMetaData.GetType(geoData.IfcTypeId).Name);
                            Console.WriteLine("Entity Label = {0}", productEntityLabel);
                            Console.WriteLine("Object space minimum point {0}", MinPtOCS);
                            Console.WriteLine("Object space maximum point {0}", MaxPtOCS);
                            Console.WriteLine("World space minimum point {0}", MinPtWCS);
                            Console.WriteLine("World space maximum point {0}", MaxPtWCS);
                            Console.WriteLine("---------------------------------------------");

                        }
                        else
                        {
                            Console.WriteLine("Failed to find product label {0} in transform graph", productEntityLabel);
                        }
                        model.Close();
                    }
                }
            }
            else
            {
                Console.WriteLine(string.Format("Failed to find {0} in executable directory", filesName));
            }
            Console.WriteLine("\nFinished");
            
            Console.ReadKey();
        }
    }
}
