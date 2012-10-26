using System;
using System.Collections.Generic;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.Ifc.SharedBldgElements;
using Xbim.XbimExtensions.Transactions;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.Extensions;
using Xbim.Ifc.ProfileResource;
using Xbim.Ifc.GeometryResource;
using Xbim.Ifc.GeometricModelResource;
using Xbim.Ifc.RepresentationResource;
using Xbim.Ifc.GeometricConstraintResource;
using System.Runtime.CompilerServices;
using Xbim.IO;
using System.IO;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.MaterialResource;
using Xbim.Ifc.SelectTypes;
using Xbim.Ifc.PresentationOrganizationResource;
using Xbim.Ifc.PresentationAppearanceResource; // we need this to use extension methods in VS 2005


namespace SimpleHelloWall
{
    class Program
    {
        private static IfcBuilding CreateBuilding(IModel model, string name, double elevHeight)
        {
            using (Transaction txn = model.BeginTransaction("Create Building"))
            {
                IfcBuilding building = model.New<IfcBuilding>();
                building.Name = name;
                building.OwnerHistory.OwningUser = model.DefaultOwningUser;
                building.OwnerHistory.OwningApplication = model.DefaultOwningApplication;
                building.ElevationOfRefHeight = elevHeight;
                building.CompositionType = IfcElementCompositionEnum.ELEMENT;

                building.ObjectPlacement = model.New<IfcLocalPlacement>();
                IfcLocalPlacement localPlacement = building.ObjectPlacement as IfcLocalPlacement;

                if (localPlacement.RelativePlacement == null)
                    localPlacement.RelativePlacement = model.New<IfcAxis2Placement3D>();
                IfcAxis2Placement3D placement = localPlacement.RelativePlacement as IfcAxis2Placement3D;
                placement.SetNewLocation(0.0, 0.0, 0.0);

                model.IfcProject.AddBuilding(building);
                //validate and commit changes
                if (model.Validate(Console.Out) == 0)
                {
                    txn.Commit();
                    return building;
                }
                else txn.Rollback();
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
            IModel model = CreateandInitModel("Hello Wall");
            if (model != null)
            {
                IfcBuilding building = CreateBuilding(model, "Default Building", 2000);
                

                IfcWallStandardCase wall = CreateWall(model, 4000, 300, 2400);
                using (Transaction txn = model.BeginTransaction("Add Wall"))
                {
                    building.AddElement(wall);
                    txn.Commit();
                }
                
                if (wall != null)
                {
                    try
                    {
                        Console.WriteLine("Standard Wall successfully created....");
                        //write the Ifc File
                        IfcOutputStream ifcOut = new IfcOutputStream(new StreamWriter("HelloWall.ifc")); //create a stream to output the ifc data to
                        ifcOut.Store(model); //store the model data
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
        static IModel CreateandInitModel(string projectName)
        {
            IModel model = new Xbim.IO.XbimMemoryModel(); //create an empty model

            //Begin a transaction as all changes to a model are transacted
            using (Transaction txn = model.BeginTransaction("Initialise Model"))
            {
                //do once only initialisation of model application and editor values
                model.DefaultOwningUser.ThePerson.FamilyName = "Bloggs";
                model.DefaultOwningUser.TheOrganization.Name = "Model Author";
                model.DefaultOwningApplication.ApplicationIdentifier = "Dodgy Software inc.";
                model.DefaultOwningApplication.ApplicationDeveloper.Name = "Dodgy Programmers Ltd.";
                model.DefaultOwningApplication.ApplicationFullName = "Ifc sample programme";
                model.DefaultOwningApplication.Version = "2.0.1";

                //set up a project and initialise the defaults

                IfcProject project = model.New<IfcProject>();
                project.Initialize(ProjectUnits.SIUnitsUK);
                project.Name = "testProject";
                project.OwnerHistory.OwningUser = model.DefaultOwningUser;
                project.OwnerHistory.OwningApplication = model.DefaultOwningApplication;

                //validate and commit changes
                if (model.Validate(Console.Out) == 0)
                {
                    txn.Commit();
                    return model;
                }
                else txn.Rollback();
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
        static IfcWallStandardCase CreateWall(IModel model, double length, double width, double height)
        {
            //
            //begin a transaction
            using (Transaction txn = model.BeginTransaction("Create Wall"))
            {
                IfcWallStandardCase wall = model.New<IfcWallStandardCase>();
                wall.Name = "A Standard rectangular wall";

                // required parameters for IfcWall
                wall.OwnerHistory.OwningUser = model.DefaultOwningUser;
                wall.OwnerHistory.OwningApplication = model.DefaultOwningApplication;

                //represent wall as a rectangular profile
                IfcRectangleProfileDef rectProf = model.New<IfcRectangleProfileDef>();
                rectProf.ProfileType = IfcProfileTypeEnum.AREA;
                rectProf.XDim = width;
                rectProf.YDim = length;

                IfcCartesianPoint insertPoint = model.New<IfcCartesianPoint>();
                insertPoint.SetXY(0, 400); //insert at arbitrary position
                rectProf.Position = model.New<IfcAxis2Placement2D>();
                rectProf.Position.Location = insertPoint;

                //model as a swept area solid
                IfcExtrudedAreaSolid body = model.New<IfcExtrudedAreaSolid>();
                body.Depth = height;
                body.SweptArea = rectProf;
                body.ExtrudedDirection = model.New<IfcDirection>();
                body.ExtrudedDirection.SetXYZ(0, 0, 1);

                //parameters to insert the geometry in the model
                IfcCartesianPoint origin = model.New<IfcCartesianPoint>();
                origin.SetXYZ(0, 0, 0);
                body.Position = model.New<IfcAxis2Placement3D>();
                body.Position.Location = origin;
             
                //Create a Definition shape to hold the geometry
                IfcShapeRepresentation shape = model.New<IfcShapeRepresentation>();
                shape.ContextOfItems = model.IfcProject.ModelContext();
                shape.RepresentationType = "SweptSolid";
                shape.RepresentationIdentifier = "Body";
                shape.Items.Add_Reversible(body);

                //Create a Product Definition and add the model geometry to the wall
                IfcProductDefinitionShape rep = model.New<IfcProductDefinitionShape>();
                rep.Representations.Add_Reversible(shape);                
                wall.Representation = rep;

                //now place the wall into the model
                IfcLocalPlacement lp = model.New<IfcLocalPlacement>();
                IfcAxis2Placement3D ax3d = model.New<IfcAxis2Placement3D>();
                ax3d.Location = origin;
                ax3d.RefDirection = model.New<IfcDirection>();
                ax3d.RefDirection.SetXYZ(0, 1, 0);
                ax3d.Axis = model.New<IfcDirection>();
                ax3d.Axis.SetXYZ(0, 0, 1);
                lp.RelativePlacement = ax3d;
                wall.ObjectPlacement = lp;


                // Where Clause: The IfcWallStandard relies on the provision of an IfcMaterialLayerSetUsage 
                IfcMaterialLayerSetUsage ifcMaterialLayerSetUsage = model.New<IfcMaterialLayerSetUsage>();
                IfcMaterialLayerSet ifcMaterialLayerSet = model.New<IfcMaterialLayerSet>();
                IfcMaterialLayer ifcMaterialLayer = model.New<IfcMaterialLayer>();
                ifcMaterialLayer.LayerThickness = 10;
                ifcMaterialLayerSet.MaterialLayers.Add_Reversible(ifcMaterialLayer);
                ifcMaterialLayerSetUsage.ForLayerSet = ifcMaterialLayerSet;
                ifcMaterialLayerSetUsage.LayerSetDirection = IfcLayerSetDirectionEnum.AXIS2;
                ifcMaterialLayerSetUsage.DirectionSense = IfcDirectionSenseEnum.NEGATIVE;
                ifcMaterialLayerSetUsage.OffsetFromReferenceLine = 150;
                
                // Add material to wall
                IfcMaterial material = model.New<IfcMaterial>();
                material.Name = "some material";
                IfcRelAssociatesMaterial ifcRelAssociatesMaterial = model.New<IfcRelAssociatesMaterial>();
                ifcRelAssociatesMaterial.RelatingMaterial = material;
                ifcRelAssociatesMaterial.RelatedObjects.Add_Reversible(wall);

                ifcRelAssociatesMaterial.RelatingMaterial = ifcMaterialLayerSetUsage;

                // IfcPresentationLayerAssignment is required for CAD presentation in IfcWall or IfcWallStandardCase
                IfcPresentationLayerAssignment ifcPresentationLayerAssignment = model.New<IfcPresentationLayerAssignment>();
                ifcPresentationLayerAssignment.Name = "some ifcPresentationLayerAssignment";
                ifcPresentationLayerAssignment.AssignedItems.Add(shape);


                // linear segment as IfcPolyline with two points is required for IfcWall
                IfcPolyline ifcPolyline = model.New<IfcPolyline>();
                IfcCartesianPoint startPoint = model.New<IfcCartesianPoint>();
                startPoint.SetXY(0, 0);
                IfcCartesianPoint endPoint = model.New<IfcCartesianPoint>();
                endPoint.SetXY(4000, 0);
                ifcPolyline.Points.Add_Reversible(startPoint);
                ifcPolyline.Points.Add_Reversible(endPoint);

                IfcShapeRepresentation shape2d = model.New<IfcShapeRepresentation>();
                shape2d.ContextOfItems = model.IfcProject.ModelContext();
                shape2d.RepresentationIdentifier = "Axis";
                shape2d.RepresentationType = "Curve2D";
                shape2d.Items.Add_Reversible(ifcPolyline);
                rep.Representations.Add_Reversible(shape2d);
                

                //validate write any errors to the console and commit if ok, otherwise abort
                string err = model.Validate(ValidationFlags.All);
                if  (string.IsNullOrEmpty(err))
                {
                    txn.Commit();
                    return wall;
                }
                else
                {
                    Console.WriteLine(err);
                    txn.Rollback();
                }
            }
            return null;
        }

        

    }
}
