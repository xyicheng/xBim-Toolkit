using System;
using Xbim.IO;
using Xbim.XbimExtensions;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Common.Geometry;
using Xbim.ModelGeometry.Converter;
using Xbim.XbimExtensions.Interfaces;

namespace CodeExamples.BoundingBox
{
    public class BoundingBoxExample : ISample
    {
        public void Run()
        {
            //to start we need an ifc file, here it is Clinic_Example.ifc
            string ifcFile = @"IfcFiles/Clinic_Example.ifc";
            string xbimFile = Path.ChangeExtension(ifcFile, "xBIM");  //will generate if not existing
            
            if (File.Exists(ifcFile))
            {
                using (XbimModel model = new XbimModel())
                {
                    if (File.Exists(xbimFile))
                    {
                        //assume the xbim file has the geometry already generated from ifc file, as below
                        model.Open(xbimFile, XbimDBAccess.Read);
                    }
                    else
                    {
                        //create the xbim file from the ifc file
                        model.CreateFrom(ifcFile, xbimFile, delegate(int percentProgress, object userState)
                        {
                            Console.Write("\rReading File {0}", percentProgress);
                        });

                        model.Open(xbimFile, XbimDBAccess.ReadWrite); //readwrite as we need to add the geometry
                        //add the the geometry information to the model
                        int total = (int)model.Instances.CountOf<IfcProduct>();
                        ReportProgressDelegate progDelegate = delegate(int percentProgress, object userState)
                        {
                            Console.Write("\rGeometry {0} / {1}", total, (total * percentProgress / 100));
                        };
                        XbimMesher.GenerateGeometry(model, null, progDelegate);
                    }
                    
                     //get all the IfcDoors in the model
                     IEnumerable<IfcDoor> ifcDoors = model.IfcProducts.OfType<IfcDoor>(); //get all the ifcdoors for this model
                     if (ifcDoors.Any()) 
                     {
                         IfcDoor ifcDoor = ifcDoors.First(); //we use the first door to get the bounding box from
                         XbimGeometryData geoData = model.GetGeometryData(ifcDoor, XbimGeometryType.BoundingBox).FirstOrDefault();
                         if (geoData != null)
                         {
                             XbimRect3D boundBox = XbimRect3D.FromArray(geoData.ShapeData);//size information for the IfcDoor, but the information is for the bounding box which encloses the door 

                             //if want want in World space 
                             XbimMatrix3D worldMatrix = XbimMatrix3D.FromArray(geoData.DataArray2);
                             //if we want to convert to World space we can use the geoData.Transform property and create the world matrix
                             XbimPoint3D MinPtOCS = new XbimPoint3D(boundBox.X, boundBox.Y, boundBox.Z);
                             XbimPoint3D MaxPtOCS = new XbimPoint3D(boundBox.X + boundBox.SizeX, boundBox.Y + boundBox.SizeY, boundBox.Z + boundBox.SizeZ);
                             //transformed values, may no longer a valid bounding box in the new space if any Pitch or Yaw, i.e. stairs ceiling supports
                             XbimPoint3D MinPtWCS = worldMatrix.Transform(MinPtOCS);
                             XbimPoint3D MaxPtWCS = worldMatrix.Transform(MaxPtOCS);
                             //if you product is at any angle to the World space then the bounding box can be recalculated, 
                            //a example of this can be found here https://sbpweb.svn.codeplex.com/svn/SBPweb.Workbench/Workbench%20Framework%202.0.0.x/Presentation/Windows.WPF/Utils/Maths.cs 
                            //in the TransformBounds function
                            Console.WriteLine("\n-------------Bounding Box Information-------------");
                            Console.WriteLine("Entity Type = {0}", IfcMetaData.GetType(geoData.IfcTypeId).Name);
                            Console.WriteLine("Entity Label = {0}", Math.Abs(ifcDoor.EntityLabel).ToString());
                            Console.WriteLine("Size X = {0:F2}", boundBox.SizeX.ToString());
                            Console.WriteLine("Size Y = {0:F2}", boundBox.SizeY.ToString());
                            Console.WriteLine("Size Z = {0:F2}", boundBox.SizeZ.ToString());
                            Console.WriteLine("Object space minimum point {0}", MinPtOCS);
                            Console.WriteLine("Object space maximum point {0}", MaxPtOCS);
                            Console.WriteLine("World space minimum point {0}", MinPtWCS);
                            Console.WriteLine("World space maximum point {0}", MaxPtWCS);
                            Console.WriteLine("---------------------------------------------");
                         }
                     }
                     else
                     {
                         Console.WriteLine(string.Format("Failed to find any IfcDoor's in {0}", ifcFile));
                         return; //exit
                     }
                }
            }
            else
            {
                Console.WriteLine(string.Format("Failed to find {0} in executable directory", ifcFile));
            }
            Console.WriteLine("\nFinished");
          
        }

        
         
    }
}
