using System;
using System.Collections.Generic;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.IO;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.ProfileResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.RepresentationResource;
using Xbim.Ifc2x3.GeometricConstraintResource;
using System.Runtime.CompilerServices;
using System.IO;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.Ifc2x3.PresentationOrganizationResource;
using Xbim.Ifc2x3.PresentationAppearanceResource; // we need this to use extension methods in VS 2005


namespace SimpleHelloWall
{
    class Program
    {
        private static IfcBuilding CreateBuilding(XbimModel model, string name, double elevHeight)
        {
            using (XbimReadWriteTransaction txn = model.BeginTransaction("Create Building"))
            {
                IfcBuilding building = model.Instances.New<IfcBuilding>();
                building.Name = name;
                building.OwnerHistory.OwningUser = model.DefaultOwningUser;
                building.OwnerHistory.OwningApplication = model.DefaultOwningApplication;
                //building.ElevationOfRefHeight = elevHeight;
                building.CompositionType = IfcElementCompositionEnum.ELEMENT;

                building.ObjectPlacement = model.Instances.New<IfcLocalPlacement>();
                IfcLocalPlacement localPlacement = building.ObjectPlacement as IfcLocalPlacement;

                if (localPlacement.RelativePlacement == null)
                    localPlacement.RelativePlacement = model.Instances.New<IfcAxis2Placement3D>();
                IfcAxis2Placement3D placement = localPlacement.RelativePlacement as IfcAxis2Placement3D;
                placement.SetNewLocation(0.0, 0.0, 0.0);
                IfcProject project = model.IfcProject;



                if (project != null)
                    project.AddBuilding(building);

                //validate and commit changes
                if (model.Validate(txn.Modified(), Console.Out) == 0)
                {
                    txn.Commit();
                    return building;
                }



            }
            return null;
        }
        /// <summary>
        /// This sample demonstrates the minimum steps to create a compliant IFC model that contains a single standard case wall
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            //first create and initialise a model called Hello Wall
            Console.WriteLine("Initialising the IFC Project....");
            XbimModel model = CreateandInitModel("Hello Wall");
            if (model != null)
            {
                IfcBuilding building = CreateBuilding(model, "Default Building", 2000);


                IfcWallStandardCase wall = CreateWall(model, 4000, 300, 2400, building);
                

                if (wall != null)
                {
                    try
                    {
                        Console.WriteLine("Standard Wall successfully created....");
                        //write the Ifc File
                        model.SaveAs("HelloWall.ifc");
                        model.Close();
                        Console.WriteLine("HelloWall.ifc has been successfully written");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Failed to save HelloWall.ifc");
                        Console.WriteLine(e.Message);
                    }
                }
            }
            else
                Console.WriteLine("Failed to initialise the model");


            



            Console.WriteLine("Press any key to exit....");
            Console.ReadKey();

        }


        /// <summary>
        /// Sets up the basic parameters any model must provide, units, ownership etc
        /// </summary>
        /// <param name="projectName">Name of the project</param>
        /// <returns></returns>
        static XbimModel CreateandInitModel(string projectName)
        {
            XbimModel model = XbimModel.CreateModel("HelloWall.xBIM"); //create an empty model
           

            //Begin a transaction as all changes to a model are transacted
            using (XbimReadWriteTransaction txn = model.BeginTransaction("Initialise Model"))
            {
                //do once only initialisation of model application and editor values
                model.DefaultOwningUser.ThePerson.FamilyName = "Bloggs";
                model.DefaultOwningUser.TheOrganization.Name = "Model Author";
                model.DefaultOwningApplication.ApplicationIdentifier = "Dodgy Software inc.";
                model.DefaultOwningApplication.ApplicationDeveloper.Name = "Dodgy Programmers Ltd.";
                model.DefaultOwningApplication.ApplicationFullName = "Ifc sample programme";
                model.DefaultOwningApplication.Version = "2.0.1";

                //set up a project and initialise the defaults

                IfcProject project = model.Instances.New<IfcProject>();
                project.Initialize(ProjectUnits.SIUnitsUK);
                project.Name = "testProject";
                project.OwnerHistory.OwningUser = model.DefaultOwningUser;
                project.OwnerHistory.OwningApplication = model.DefaultOwningApplication;

                //validate and commit changes
                if (model.Validate(txn.Modified(), Console.Out) == 0)
                {
                    txn.Commit();
                    return model;
                }
                
            }
            return null; //failed so return nothing

        }
        /// <summary>
        /// This creates a wall and it's geometry, many geometric representations are possible and extruded rectangular footprint is chosen as this is commonly used for standard case walls
        /// </summary>
        /// <param name="model"></param>
        /// <param name="length">Length of the rectangular footprint</param>
        /// <param name="width">Width of the rectangular footprint (width of the wall)</param>
        /// <param name="height">Height to extrude the wall, extrusion is vertical</param>
        /// <returns></returns>
        static IfcWallStandardCase CreateWall(XbimModel model, double length, double width, double height, IfcBuilding building)
        {
            //
            //begin a transaction
            using (XbimReadWriteTransaction txn = model.BeginTransaction("Create Wall"))
            {
                IfcWallStandardCase wall = model.Instances.New<IfcWallStandardCase>();
                wall.Name = "A Standard rectangular wall";

                // required parameters for IfcWall
                wall.OwnerHistory.OwningUser = model.DefaultOwningUser;
                wall.OwnerHistory.OwningApplication = model.DefaultOwningApplication;

                //represent wall as a rectangular profile
                IfcRectangleProfileDef rectProf = model.Instances.New<IfcRectangleProfileDef>();
                rectProf.ProfileType = IfcProfileTypeEnum.AREA;
                rectProf.XDim = width;
                rectProf.YDim = length;

                IfcCartesianPoint insertPoint = model.Instances.New<IfcCartesianPoint>();
                insertPoint.SetXY(0, 400); //insert at arbitrary position
                rectProf.Position = model.Instances.New<IfcAxis2Placement2D>();
                rectProf.Position.Location = insertPoint;

                //model as a swept area solid
                IfcExtrudedAreaSolid body = model.Instances.New<IfcExtrudedAreaSolid>();
                body.Depth = height;
                body.SweptArea = rectProf;
                body.ExtrudedDirection = model.Instances.New<IfcDirection>();
                body.ExtrudedDirection.SetXYZ(0, 0, 1);

                //parameters to insert the geometry in the model
                IfcCartesianPoint origin = model.Instances.New<IfcCartesianPoint>();
                origin.SetXYZ(0, 0, 0);
                body.Position = model.Instances.New<IfcAxis2Placement3D>();
                body.Position.Location = origin;
             
                //Create a Definition shape to hold the geometry
                IfcShapeRepresentation shape = model.Instances.New<IfcShapeRepresentation>();
                shape.ContextOfItems = model.IfcProject.ModelContext();
                shape.RepresentationType = "SweptSolid";
                shape.RepresentationIdentifier = "Body";
                shape.Items.Add_Reversible(body);

                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape rep = model.Instances.New<IfcProductDefinitionShape>();
                rep.Representations.Add_Reversible(shape);                
                wall.Representation = rep;

                //now place the wall into the model
                IfcLocalPlacement lp = model.Instances.New<IfcLocalPlacement>();
                IfcAxis2Placement3D ax3d = model.Instances.New<IfcAxis2Placement3D>();
                ax3d.Location = origin;
                ax3d.RefDirection = model.Instances.New<IfcDirection>();
                ax3d.RefDirection.SetXYZ(0, 1, 0);
                ax3d.Axis = model.Instances.New<IfcDirection>();
                ax3d.Axis.SetXYZ(0, 0, 1);
                lp.RelativePlacement = ax3d;
                wall.ObjectPlacement = lp;


                // Where Clause: The IfcWallStandard relies on the provision of an IfcMaterialLayerSetUsage 
                IfcMaterialLayerSetUsage ifcMaterialLayerSetUsage = model.Instances.New<IfcMaterialLayerSetUsage>();
                IfcMaterialLayerSet ifcMaterialLayerSet = model.Instances.New<IfcMaterialLayerSet>();
                IfcMaterialLayer ifcMaterialLayer = model.Instances.New<IfcMaterialLayer>();
                ifcMaterialLayer.LayerThickness = 10;
                ifcMaterialLayerSet.MaterialLayers.Add_Reversible(ifcMaterialLayer);
                ifcMaterialLayerSetUsage.ForLayerSet = ifcMaterialLayerSet;
                ifcMaterialLayerSetUsage.LayerSetDirection = IfcLayerSetDirectionEnum.AXIS2;
                ifcMaterialLayerSetUsage.DirectionSense = IfcDirectionSenseEnum.NEGATIVE;
                ifcMaterialLayerSetUsage.OffsetFromReferenceLine = 150;
                
                // Add material to wall
                IfcMaterial material = model.Instances.New<IfcMaterial>();
                material.Name = "some material";
                IfcRelAssociatesMaterial ifcRelAssociatesMaterial = model.Instances.New<IfcRelAssociatesMaterial>();
                ifcRelAssociatesMaterial.RelatingMaterial = material;
                ifcRelAssociatesMaterial.RelatedObjects.Add_Reversible(wall);

                ifcRelAssociatesMaterial.RelatingMaterial = ifcMaterialLayerSetUsage;

                // IfcPresentationLayerAssignment is required for CAD presentation in IfcWall or IfcWallStandardCase
                IfcPresentationLayerAssignment ifcPresentationLayerAssignment = model.Instances.New<IfcPresentationLayerAssignment>();
                ifcPresentationLayerAssignment.Name = "some ifcPresentationLayerAssignment";
                ifcPresentationLayerAssignment.AssignedItems.Add(shape);


                // linear segment as IfcPolyline with two points is required for IfcWall
                IfcPolyline ifcPolyline = model.Instances.New<IfcPolyline>();
                IfcCartesianPoint startPoint = model.Instances.New<IfcCartesianPoint>();
                startPoint.SetXY(0, 0);
                IfcCartesianPoint endPoint = model.Instances.New<IfcCartesianPoint>();
                endPoint.SetXY(4000, 0);
                ifcPolyline.Points.Add_Reversible(startPoint);
                ifcPolyline.Points.Add_Reversible(endPoint);

                IfcShapeRepresentation shape2d = model.Instances.New<IfcShapeRepresentation>();
                shape2d.ContextOfItems = model.IfcProject.ModelContext();
                shape2d.RepresentationIdentifier = "Axis";
                shape2d.RepresentationType = "Curve2D";
                shape2d.Items.Add_Reversible(ifcPolyline);
                rep.Representations.Add_Reversible(shape2d);

                building.AddElement(wall);

                //validate write any errors to the console and commit if ok, otherwise abort
                int err = model.Validate(txn.Modified(), Console.Out);
                if  (err == 0)
                {
                    txn.Commit();
                    return wall;
                }
                else
                {
                    Console.WriteLine(err);
                    
                }
            }
            return null;
        }

        

    }
}
