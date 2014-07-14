using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.ModelGeometry.Scene;
using Xbim.Presentation;
using Xbim.IO;
using Xbim.Ifc2x3;
using Xbim.XbimExtensions;
using System.IO;
using Xbim.XbimExtensions.Interfaces;
using Xbim.ModelGeometry.Converter;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Common.Geometry;

namespace CodeExamples.Geometry
{
    public class GeometryExample : ISample
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
                    XbimScene<XbimMeshGeometry3D, WpfMaterial> scene = GetModelScene(model);

                    IEnumerable<IfcDoor> ifcDoors = model.IfcProducts.OfType<IfcDoor>(); //get all the ifcdoors for this model
                    if (ifcDoors.Any())
                    {
                        IfcDoor ifcDoor = ifcDoors.First(); //we use the first door
                        uint entLbl = ifcDoor.EntityLabel;
                        
                        foreach (var layer in scene.Layers)//loop material layers
                        {
                            var hidden = layer.Hidden as XbimMeshGeometry3D; //stored the points in Hidden
                            if (hidden != null)
                            {
                                foreach (var m in hidden.Meshes) //display simple count information
                                {
                                    if (m.EntityLabel != entLbl) continue; //skip doors that do not match label

                                    int startIndex = m.StartPosition;
                                    int endIndex = m.EndPosition;
                                    int startTriIndex = m.StartTriangleIndex;
                                    int endTriIndex = m.EndTriangleIndex;
                                    List<XbimPoint3D> vertex = hidden.Positions.GetRange(startIndex, (endIndex - startIndex) + 1);
                                    List<XbimVector3D> normals = hidden.Normals.GetRange(startIndex, (endIndex - startIndex) + 1);
                                    List<int> triangleIndexs = hidden.TriangleIndices.GetRange(startIndex, (endTriIndex - startTriIndex) + 1);
                                    Console.WriteLine("\n-------------Geometry Triangle Information-------------");
                                    Console.WriteLine("Entity Type = IfcDoor");
                                    Console.WriteLine("Entity Label = {0}", entLbl);
                                    Console.WriteLine("Layer = {0}", layer.Name);
                                    Console.WriteLine("Vertex count = {0}", vertex.Count);
                                    Console.WriteLine("Normal count = {0}", normals.Count);
                                    Console.WriteLine("TriangleIndexs count = {0}", triangleIndexs.Count);
                                    Console.WriteLine("---------------------------------------------------------");
                                } 
                            }
                        }
                    }
                }
            }
        }

        

        /// <summary>
        /// Generate a Scene
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public XbimScene<XbimMeshGeometry3D, WpfMaterial> GetModelScene(XbimModel model)
        {
            //we need a class which implements IXbimRenderMaterial for the material, you might also want to implement a class for Material depending on your needs, 
            //using the WpfMaterial class that then used the Media3D material class as implemented by the XBimXplorer for convenience.

            XbimScene<XbimMeshGeometry3D, WpfMaterial> scene = new XbimScene<XbimMeshGeometry3D, WpfMaterial>(model);
            XbimGeometryHandleCollection handles = new XbimGeometryHandleCollection(model.GetGeometryHandles()
                                                       .Exclude(IfcEntityNameEnum.IFCFEATUREELEMENT));
            foreach (var layerContent in handles.GroupByBuildingElementTypes())
            {
                string elementTypeName = layerContent.Key;
                XbimGeometryHandleCollection layerHandles = layerContent.Value;
                IEnumerable<XbimGeometryData> geomColl = model.GetGeometryData(layerHandles);
                XbimColour colour = scene.LayerColourMap[elementTypeName];
                XbimMeshLayer<XbimMeshGeometry3D, WpfMaterial> layer = new XbimMeshLayer<XbimMeshGeometry3D, WpfMaterial>(model, colour) { Name = elementTypeName };
                //add all content initially into the hidden field
                foreach (var geomData in geomColl)
                {
                    layer.AddToHidden(geomData, model);
                }
                scene.Add(layer);
            }
            return scene;
        }

        
    }
}
