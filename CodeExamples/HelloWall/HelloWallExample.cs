using System;
using System.Collections.Generic;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.XbimExtensions.Transactions;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.ProfileResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.RepresentationResource;
using Xbim.Ifc2x3.GeometricConstraintResource;
using System.Runtime.CompilerServices;
using Xbim.IO;
using System.IO;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc.SelectTypes;
using Xbim.Ifc2x3.PresentationOrganizationResource;
using Xbim.Ifc2x3.PresentationAppearanceResource;
using CodeExamples;
using Xbim.XbimExtensions.Interfaces;
using Xbim.Ifc2x3.UtilityResource;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.QuantityResource;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.Ifc2x3.DateTimeResource;
using Xbim.Ifc2x3.ExternalReferenceResource;
using Xbim.Ifc2x3.TimeSeriesResource;
using Xbim.Ifc2x3.ActorResource;
using Xbim.Ifc2x3.CostResource; // we need this to use extension methods in VS 2005


namespace CodeExamples.HelloWall
{
    public class HelloWallExample : ISample
    {
        /// <summary>
        /// This sample demonstrates the minimum steps to create a compliant IFC model that contains a single standard case wall
        /// </summary>
        /// <param name="args"></param>
        public void Run()
        {
            //first create and initialise a model called Hello Wall
            Console.WriteLine("Initialising the IFC Project....");
            XbimModel model = CreateandInitModel("HelloWall");
            if (model != null)
            {
                IfcBuilding building = CreateBuilding(model, "Default Building", 2000);


                IfcWallStandardCase wall = CreateWall(model, 4000, 300, 2400);
                if (wall != null) AddPropertiesToWall(model, wall);
                using (XbimReadWriteTransaction txn = model.BeginTransaction("Add Wall"))
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
                        model.SaveAs("HelloWall.ifc",XbimStorageType.IFC);
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
            {
                Console.WriteLine("Failed to initialise the model");
            }

            Console.WriteLine("Press any key to exit....");
            Console.ReadKey();

        }

        private IfcBuilding CreateBuilding(XbimModel model, string name, double elevHeight)
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

                model.IfcProject.AddBuilding(building);
                //validate and commit changes
                if (model.Validate(txn.Modified(),Console.Out) == 0)
                {
                    txn.Commit();
                    return building;
                }
                
            }
            return null;
        }
        


        /// <summary>
        /// Sets up the basic parameters any model must provide, units, ownership etc
        /// </summary>
        /// <param name="projectName">Name of the project</param>
        /// <returns></returns>
        private XbimModel CreateandInitModel(string projectName)
        {
            XbimModel model = XbimModel.CreateModel(projectName + ".xBIM"); ; //create an empty model
            
            //Begin a transaction as all changes to a model are transacted
            using (XbimReadWriteTransaction txn = model.BeginTransaction("Initialise Model"))
            {
                //do once only initialisation of model application and editor values
                model.DefaultOwningUser.ThePerson.GivenName = "John";
                model.DefaultOwningUser.ThePerson.FamilyName = "Bloggs";
                model.DefaultOwningUser.TheOrganization.Name = "Department of Building";
                model.DefaultOwningApplication.ApplicationIdentifier = "Construction Software inc.";
                model.DefaultOwningApplication.ApplicationDeveloper.Name = "Construction Programmers Ltd.";
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
        private IfcWallStandardCase CreateWall(XbimModel model, double length, double width, double height)
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
                

                //validate write any errors to the console and commit if ok, otherwise abort
     
                if  (model.Validate(txn.Modified(),Console.Out)==0)
                {
                    txn.Commit();
                    return wall;
                }
            }
            return null;
        }

        /// <summary>
        /// Add some properties to the wall,
        /// </summary>
        /// <param name="model">XbimModel</param>
        private void AddPropertiesToWall(XbimModel model, IfcWallStandardCase wall)
        {
            using (XbimReadWriteTransaction txn = model.BeginTransaction("Create Wall"))
            {
                IfcOwnerHistory ifcOwnerHistory = model.IfcProject.OwnerHistory; //we just use the project owner history for the properties, saves creating one
                CreateElementQuantity(model, wall, ifcOwnerHistory);
                CreateSimpleProperty(model, wall, ifcOwnerHistory);

                //if (model.Validate(txn.Modified(), Console.Out) == 0)
                //{
                    txn.Commit();
                //}
                    var xx = model.Instances.OfType<IfcPropertyReferenceValue>();
            }
        }

        private static void CreateSimpleProperty(XbimModel model, IfcWallStandardCase wall, IfcOwnerHistory ifcOwnerHistory)
        {
            IfcPropertySingleValue ifcPropertySingleValue = model.Instances.New<IfcPropertySingleValue>(psv =>
            {
                psv.Name = "IfcPropertySingleValue:Time";
                psv.Description = "";
                psv.NominalValue = new IfcTimeMeasure(150.0);
                psv.Unit = model.Instances.New<IfcSIUnit>(siu =>
                {
                    siu.UnitType = IfcUnitEnum.TIMEUNIT;
                    siu.Name = IfcSIUnitName.SECOND;
                    siu.Dimensions = model.Instances.New<IfcDimensionalExponents>(de =>
                    {
                        de.LengthExponent = 0;
                        de.MassExponent = 0;
                        de.TimeExponent = 1;
                        de.ElectricCurrentExponent = 0;
                        de.ThermodynamicTemperatureExponent = 0;
                        de.AmountOfSubstanceExponent = 0;
                        de.LuminousIntensityExponent = 0;
                    });
                });
            });
            IfcPropertyEnumeratedValue ifcPropertyEnumeratedValue = model.Instances.New<IfcPropertyEnumeratedValue>(pev =>
            {
                pev.Name = "IfcPropertyEnumeratedValue:Music";
                pev.EnumerationReference = model.Instances.New<IfcPropertyEnumeration>(pe =>
                    {
                        pe.Name = "Notes";
                        pe.EnumerationValues.Add(new IfcLabel("Do"));
                        pe.EnumerationValues.Add(new IfcLabel("Re"));
                        pe.EnumerationValues.Add(new IfcLabel("Mi"));
                        pe.EnumerationValues.Add(new IfcLabel("Fa"));
                        pe.EnumerationValues.Add(new IfcLabel("So"));
                        pe.EnumerationValues.Add(new IfcLabel("La"));
                        pe.EnumerationValues.Add(new IfcLabel("Ti"));
                    });
                pev.EnumerationValues.Add(new IfcLabel("Do"));
                pev.EnumerationValues.Add(new IfcLabel("Re"));
                pev.EnumerationValues.Add(new IfcLabel("Mi"));

            });
            IfcPropertyBoundedValue ifcPropertyBoundedValue = model.Instances.New<IfcPropertyBoundedValue>(pbv => 
            {
                pbv.Name = "IfcPropertyBoundedValue:Mass";
                pbv.Description = "";
                pbv.UpperBoundValue = new IfcMassMeasure(5000.0);
                pbv.LowerBoundValue = new IfcMassMeasure(1000.0);
                pbv.Unit = model.Instances.New<IfcSIUnit>(siu =>
                {
                    siu.UnitType = IfcUnitEnum.MASSUNIT;
                    siu.Name = IfcSIUnitName.GRAM;
                    siu.Prefix = IfcSIPrefix.KILO;
                    siu.Dimensions = model.Instances.New<IfcDimensionalExponents>(de =>
                    {
                        de.LengthExponent = 0;
                        de.MassExponent = 1;
                        de.TimeExponent = 0;
                        de.ElectricCurrentExponent = 0;
                        de.ThermodynamicTemperatureExponent = 0;
                        de.AmountOfSubstanceExponent = 0;
                        de.LuminousIntensityExponent = 0;
                    });
                });
            });

            List<IfcReal> definingValues = new List<IfcReal>() { new IfcReal(100.0), new IfcReal(200.0), new IfcReal(400.0), new IfcReal(800.0), new IfcReal(1600.0), new IfcReal(3200.0), };
            List<IfcReal> definedValues = new List<IfcReal>() { new IfcReal(20.0), new IfcReal(42.0), new IfcReal(46.0), new IfcReal(56.0), new IfcReal(60.0), new IfcReal(65.0), };
            IfcPropertyTableValue ifcPropertyTableValue = model.Instances.New<IfcPropertyTableValue>(ptv =>
            {
                ptv.Name = "IfcPropertyTableValue:Sound";
                foreach (var item in definingValues)
	            {
                    ptv.DefiningValues.Add(item);
	            }
                foreach (var item in definedValues)
                {
                    ptv.DefinedValues.Add(item);
                }
                ptv.DefinedUnit = model.Instances.New<IfcContextDependentUnit>(cd =>
                {
                    cd.Dimensions = model.Instances.New<IfcDimensionalExponents>(de =>
                    {
                        de.LengthExponent = 0;
                        de.MassExponent = 0;
                        de.TimeExponent = 0;
                        de.ElectricCurrentExponent = 0;
                        de.ThermodynamicTemperatureExponent = 0;
                        de.AmountOfSubstanceExponent = 0;
                        de.LuminousIntensityExponent = 0;
                    });
                    cd.UnitType = IfcUnitEnum.FREQUENCYUNIT;
                    cd.Name = "dB";
                });
                
                
            });

            List<IfcLabel> listValues = new List<IfcLabel>() { new IfcLabel("Red"), new IfcLabel("Green"), new IfcLabel("Blue"), new IfcLabel("Pink"), new IfcLabel("White"), new IfcLabel("Black"), };
            IfcPropertyListValue ifcPropertyListValue = model.Instances.New<IfcPropertyListValue>(plv =>
            {
                plv.Name = "IfcPropertyListValue:Colours";
                foreach (var item in listValues)
                {
                    plv.ListValues.Add(item);
                }
            });

            IfcMaterial IfcMaterial = model.Instances.New<IfcMaterial>(m =>
            {
                m.Name = "Brick";
            });
            IfcPropertyReferenceValue ifcPRValueMaterial = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:Material";
                prv.PropertyReference = IfcMaterial;
            });

            IfcPropertyReferenceValue ifcPRValuePerson = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:Person";
                prv.PropertyReference = ifcOwnerHistory.OwningUser.ThePerson;
            });

            IfcDateAndTime ifcDateAndTime = model.Instances.New<IfcDateAndTime>(dt =>
            {
                dt.DateComponent = model.Instances.New<IfcCalendarDate>(cd =>
                    {
                        cd.DayComponent = 25;
                        cd.MonthComponent = 3;
                        cd.YearComponent = 2013;
                    });
                dt.TimeComponent = model.Instances.New<IfcLocalTime>(lt =>
                    {
                        lt.HourComponent = 10;
                        lt.MinuteComponent = 30;
                        lt.SecondComponent = 0;
                    });
            });
            IfcPropertyReferenceValue ifcPRValueDateTime = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:DateAndTime";
                prv.PropertyReference = ifcDateAndTime;
            });

            IfcMaterialList ifcMaterialList = model.Instances.New<IfcMaterialList>(ml =>
                {
                    ml.Materials.Add(IfcMaterial);
                    ml.Materials.Add(model.Instances.New<IfcMaterial>(m =>{m.Name = "Cavity";}));
                    ml.Materials.Add(model.Instances.New<IfcMaterial>(m => { m.Name = "Block"; }));
                });
            IfcPropertyReferenceValue ifcPRValueMatList = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:MaterialList";
                prv.PropertyReference = ifcMaterialList;
            });

            IfcPropertyReferenceValue ifcPRValueOrg = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:Organization";
                prv.PropertyReference = ifcOwnerHistory.OwningUser.TheOrganization;
            });

            IfcPropertyReferenceValue ifcPRValueDate = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:Date";
                prv.PropertyReference = ifcDateAndTime.DateComponent;
            });

            IfcPropertyReferenceValue ifcPRValueTime = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:Time";
                prv.PropertyReference = ifcDateAndTime.TimeComponent;
            });

            IfcPropertyReferenceValue ifcPRValuePersonOrg = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:PersonOrganization";
                prv.PropertyReference = ifcOwnerHistory.OwningUser;
            });

            IfcMaterialLayer ifcMaterialLayer = model.Instances.New<IfcMaterialLayer>(ml =>
            {
                ml.Material = IfcMaterial;
                ml.LayerThickness = 100.0;
            });
            IfcPropertyReferenceValue ifcPRValueMatLayer = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:MaterialLayer";
                prv.PropertyReference = ifcMaterialLayer;
            });

            IfcDocumentReference ifcDocumentReference = model.Instances.New<IfcDocumentReference>(dr =>
            {
                dr.Name = "Document";
                dr.Location = "c://Documents//TheDoc.Txt";
            });
            IfcPropertyReferenceValue ifcPRValueRef = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:Document";
                prv.PropertyReference = ifcDocumentReference;
            });

            IfcRegularTimeSeries ifcTimeSeries = model.Instances.New<IfcRegularTimeSeries>(ts =>
            {
                ts.Name = "Regular Time Series";
                ts.Description = "Time series of events";
                ts.StartTime = model.Instances.New<IfcCalendarDate>(cd =>
                {
                    cd.DayComponent = 01;
                    cd.MonthComponent = 1;
                    cd.YearComponent = 2013;
                }); 
                ts.EndTime = model.Instances.New<IfcCalendarDate>(cd =>
                {
                    cd.DayComponent = 01;
                    cd.MonthComponent = 3;
                    cd.YearComponent = 2013;
                });
                ts.TimeSeriesDataType = IfcTimeSeriesDataTypeEnum.CONTINUOUS;
                ts.DataOrigin = IfcDataOriginEnum.MEASURED;
                ts.TimeStep = 604800; //7 days in secs
            });

            IfcPropertyReferenceValue ifcPRValueTimeSeries = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:TimeSeries";
                prv.PropertyReference = ifcTimeSeries;
            });

            IfcPostalAddress ifcAddress = model.Instances.New<IfcPostalAddress>(a =>
            {
                a.InternalLocation = "Room 101";
                a.SetAddressLines(new string[] { "12 New road", "DoxField" });
                a.Town = "Sunderland";
                a.PostalCode = "DL01 6SX";
            });
            IfcPropertyReferenceValue ifcPRValueAddress = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:Address";
                prv.PropertyReference = ifcAddress;
            });
            IfcTelecomAddress IfcTelecomAddress = model.Instances.New<IfcTelecomAddress>(a =>
            {
                a.SetTelephoneNumbers(new string[]{"01325 6589965"});
                a.SetElectronicMailAddress(new string[]{"bob@bobsworks.com"});
            });
            IfcPropertyReferenceValue ifcPRValueTelecom = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:Telecom";
                prv.PropertyReference = IfcTelecomAddress;
            });

            IfcCostValue ifcCostValue = model.Instances.New<IfcCostValue>(cv =>
            {
                cv.Name = "Cost Value";
                cv.Description = "";
                cv.Value = new IfcMonetaryMeasure(155.0);
                cv.ApplicableDate = model.Instances.New<IfcCalendarDate>(cd =>
                {
                    cd.DayComponent = 02;
                    cd.MonthComponent = 02;
                    cd.YearComponent = 2013;
                });
                cv.FixedUntilDate = model.Instances.New<IfcCalendarDate>(cd =>
                {
                    cd.DayComponent = 31;
                    cd.MonthComponent = 12;
                    cd.YearComponent = 2013;
                });
                cv.CostType = "Annual rate of return";
                cv.Condition = "";
            });
            IfcPropertyReferenceValue ifcPRValueCostValue = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:CostValue";
                prv.PropertyReference = ifcCostValue;
            });


            IfcEnvironmentalImpactValue IfcEnvironmentalImpactValue = model.Instances.New<IfcEnvironmentalImpactValue>(cv =>
            {
                cv.Name = "Environmental Impact";
                cv.Description = "";
                cv.Value = model.Instances.New<IfcMeasureWithUnit>(mwu =>
                    {
                        mwu.ValueComponent = new IfcReal(111.0);
                        mwu.UnitComponent = model.Instances.New<IfcSIUnit>(siu =>
                        {
                            siu.UnitType = IfcUnitEnum.LENGTHUNIT;
                            siu.Name = IfcSIUnitName.METRE;
                            siu.Dimensions = model.Instances.New<IfcDimensionalExponents>(de =>
                            {
                                de.LengthExponent = 1;
                                de.MassExponent = 0;
                                de.TimeExponent = 0;
                                de.ElectricCurrentExponent = 0;
                                de.ThermodynamicTemperatureExponent = 0;
                                de.AmountOfSubstanceExponent = 0;
                                de.LuminousIntensityExponent = 0;
                            });
                        });
                    });
                cv.ApplicableDate = model.Instances.New<IfcCalendarDate>(cd =>
                {
                    cd.DayComponent = 02;
                    cd.MonthComponent = 02;
                    cd.YearComponent = 2013;
                });
                cv.FixedUntilDate = model.Instances.New<IfcCalendarDate>(cd =>
                {
                    cd.DayComponent = 31;
                    cd.MonthComponent = 12;
                    cd.YearComponent = 2013;
                });
                cv.ImpactType = "Embodied energy";
                cv.Category = IfcEnvironmentalImpactCategoryEnum.MANUFACTURE;
            });
            IfcPropertyReferenceValue ifcPRValueEnvironmentalImpact = model.Instances.New<IfcPropertyReferenceValue>(prv =>
            {
                prv.Name = "IfcPropertyReferenceValue:EnvironmentalImpact";
                prv.PropertyReference = IfcEnvironmentalImpactValue;
            });

            //lets create the IfcElementQuantity
            IfcPropertySet ifcPropertySet = model.Instances.New<IfcPropertySet>(ps =>
            {
                ps.OwnerHistory = ifcOwnerHistory;
                ps.Name = "Test:IfcPropertySet";
                ps.Description = "Property Set";
                ps.HasProperties.Add(ifcPropertySingleValue);
                ps.HasProperties.Add(ifcPropertyEnumeratedValue);
                ps.HasProperties.Add(ifcPropertyBoundedValue);
                ps.HasProperties.Add(ifcPropertyTableValue);
                ps.HasProperties.Add(ifcPropertyListValue);
                ps.HasProperties.Add(ifcPRValueMaterial);
                ps.HasProperties.Add(ifcPRValuePerson);
                ps.HasProperties.Add(ifcPRValueDateTime);
                ps.HasProperties.Add(ifcPRValueMatList);
                ps.HasProperties.Add(ifcPRValueOrg);
                ps.HasProperties.Add(ifcPRValueDate);
                ps.HasProperties.Add(ifcPRValueTime);
                ps.HasProperties.Add(ifcPRValuePersonOrg);
                ps.HasProperties.Add(ifcPRValueMatLayer);
                ps.HasProperties.Add(ifcPRValueRef);
                ps.HasProperties.Add(ifcPRValueTimeSeries);
                ps.HasProperties.Add(ifcPRValueAddress);
                ps.HasProperties.Add(ifcPRValueTelecom);
                ps.HasProperties.Add(ifcPRValueCostValue);
                ps.HasProperties.Add(ifcPRValueEnvironmentalImpact);
                
            });

            //need to create the relationship
            IfcRelDefinesByProperties ifcRelDefinesByProperties = model.Instances.New<IfcRelDefinesByProperties>(rdbp =>
            {
                rdbp.OwnerHistory = ifcOwnerHistory;
                rdbp.Name = "Property Association";
                rdbp.Description = "IfcPropertySet associated to wall";
                rdbp.RelatedObjects.Add(wall);
                rdbp.RelatingPropertyDefinition = ifcPropertySet;
            });
        }

        private static void CreateElementQuantity(XbimModel model, IfcWallStandardCase wall, IfcOwnerHistory ifcOwnerHistory)
        {
            //Create a IfcElementQuantity
            //first we need a IfcPhysicalSimpleQuantity,first will use IfcQuantityArea
            IfcQuantityArea ifcQuantityArea = model.Instances.New<IfcQuantityArea>(qa =>
            {
                qa.Name = "IfcQuantityArea:Area";
                qa.Description = "";
                qa.Unit = model.Instances.New<IfcSIUnit>(siu =>
                {
                    siu.UnitType = IfcUnitEnum.AREAUNIT;
                    siu.Prefix = IfcSIPrefix.MILLI;
                    siu.Name = IfcSIUnitName.SQUARE_METRE;
                    siu.Dimensions = model.Instances.New<IfcDimensionalExponents>(de =>
                    {
                        de.LengthExponent = 1;
                        de.MassExponent = 0;
                        de.TimeExponent = 0;
                        de.ElectricCurrentExponent = 0;
                        de.ThermodynamicTemperatureExponent = 0;
                        de.AmountOfSubstanceExponent = 0;
                        de.LuminousIntensityExponent = 0;
                    });

                });
                qa.AreaValue = 100.0;

            });
            //next quantity IfcQuantityCount using IfcContextDependentUnit
            IfcContextDependentUnit ifcContextDependentUnit = model.Instances.New<IfcContextDependentUnit>(cd =>
                {
                    cd.Dimensions = model.Instances.New<IfcDimensionalExponents>(de =>
                        {
                            de.LengthExponent = 1;
                            de.MassExponent = 0;
                            de.TimeExponent = 0;
                            de.ElectricCurrentExponent = 0;
                            de.ThermodynamicTemperatureExponent = 0;
                            de.AmountOfSubstanceExponent = 0;
                            de.LuminousIntensityExponent = 0;
                        });
                    cd.UnitType = IfcUnitEnum.LENGTHUNIT;
                    cd.Name = "Elephants";
                });
                IfcQuantityCount ifcQuantityCount = model.Instances.New<IfcQuantityCount>(qc =>
                {
                    qc.Name = "IfcQuantityCount:Elephant";
                    qc.CountValue = 12;
                    qc.Unit = ifcContextDependentUnit;
                });


             //next quantity IfcQuantityLength using IfcConversionBasedUnit
            IfcConversionBasedUnit ifcConversionBasedUnit = model.Instances.New<IfcConversionBasedUnit>(cbu =>
            {
                cbu.ConversionFactor = model.Instances.New<IfcMeasureWithUnit>(mu =>
                {
                    mu.ValueComponent = new IfcRatioMeasure(25.4);
                    mu.UnitComponent = model.Instances.New<IfcSIUnit>(siu =>
                    {
                        siu.UnitType = IfcUnitEnum.LENGTHUNIT;
                        siu.Prefix = IfcSIPrefix.MILLI;
                        siu.Name = IfcSIUnitName.METRE;
                    });
                    
                });
                cbu.Dimensions = model.Instances.New<IfcDimensionalExponents>(de =>
                {
                    de.LengthExponent = 1;
                    de.MassExponent = 0;
                    de.TimeExponent = 0;
                    de.ElectricCurrentExponent = 0;
                    de.ThermodynamicTemperatureExponent = 0;
                    de.AmountOfSubstanceExponent = 0;
                    de.LuminousIntensityExponent = 0;
                });
                cbu.UnitType = IfcUnitEnum.LENGTHUNIT;
                cbu.Name = "Inch";
            });
            IfcQuantityLength ifcQuantityLength = model.Instances.New<IfcQuantityLength>(qa =>
            {
                qa.Name = "IfcQuantityLength:Length";
                qa.Description = "";
                qa.Unit = ifcConversionBasedUnit;
                qa.LengthValue = 24.0;
            });

            //lets create the IfcElementQuantity
            IfcElementQuantity ifcElementQuantity = model.Instances.New<IfcElementQuantity>(eq =>
            {
                eq.OwnerHistory = ifcOwnerHistory;
                eq.Name = "Test:IfcElementQuantity";
                eq.Description = "Measurement quantity";
                eq.Quantities.Add(ifcQuantityArea);
                eq.Quantities.Add(ifcQuantityCount);
                eq.Quantities.Add(ifcQuantityLength);
            });

            //need to create the relationship
            IfcRelDefinesByProperties ifcRelDefinesByProperties = model.Instances.New<IfcRelDefinesByProperties>(rdbp =>
            {
                rdbp.OwnerHistory = ifcOwnerHistory;
                rdbp.Name = "Area Association";
                rdbp.Description = "IfcElementQuantity associated to wall";
                rdbp.RelatedObjects.Add(wall);
                rdbp.RelatingPropertyDefinition = ifcElementQuantity;
            });
        }

        

    }
}
