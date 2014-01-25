using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.PresentationAppearanceResource;
using Xbim.Ifc2x3.GeometricConstraintResource;
using Xbim.Ifc2x3.RepresentationResource;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.PresentationResource;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.Kernel;
using Xbim.IO;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.ProfileResource;

namespace Xbim.Tests.Comparisons
{
    public class SampleModelsCreator
    {
        public static XbimModel TwoDisjointColumns
        {
            get
            {
                return ColumnsArrayModel("Disjoint", 1000, 500, 200, 0, 2000, 0, 1, 2, 1);
            }
        }

        public static XbimModel TwoTouchingColumns
        {
            get
            {
                return ColumnsArrayModel("Touches", 1000, 500, 500, 0, 500, 0, 1, 2, 1);
            }
        }

        public static XbimModel TwoIdenticalColumns
        {
            get
            {
                return ColumnsArrayModel("Identical", 1000, 500, 200, 0, 0, 0, 1, 2, 1);
            }
        }

        public static XbimModel TwoIntersectingColumns
        {
            get
            {
                return ColumnsArrayModel("Intersects", 1000, 500, 500, 0, 250, 0, 1, 2, 1);
            }
        }

        public static XbimModel Disjoint4x4x4cubes
        {
            get
            {
                return ColumnsArrayModel();
            }
        }

        public static XbimModel ColumnsArrayModel(string projectName = "Test revision", float height = 500f, float width = 500f, float depth = 500f, float colSpace = 2000f, float rowSpace = 2000f, float layerSpace = 2000f, int colNumber = 4, int rowNumber = 4, int layerNumber = 4)
        {
            XbimModel model = XbimModel.CreateTemporaryModel();
            using (var txn = model.BeginTransaction())
            {
                //general information and structures
                //create default directions and positions
                var upDirection = model.Instances.New<IfcDirection>(d => d.SetXYZ(0, 0, 1));
                var origin2D = model.Instances.New<IfcCartesianPoint>(p => p.SetXY(0, 0));
                var origin3D = model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(0, 0, 0));

                //create one context
                var context = model.Instances.New<IfcGeometricRepresentationContext>(rc =>
                {
                    rc.CoordinateSpaceDimension = 3;
                    rc.Precision = 0.000000001;
                    rc.WorldCoordinateSystem = model.Instances.New<IfcAxis2Placement3D>(pl =>
                    {
                        pl.Location = origin3D;
                        rc.ContextType = "Model";
                    });
                });
                var project = model.Instances.New<IfcProject>(p =>
                {
                    p.Name = projectName;
                    p.RepresentationContexts.Add_Reversible(context);
                    p.UnitsInContext = model.Instances.New<IfcUnitAssignment>(ua =>
                    {
                        ua.Units.Add_Reversible(model.Instances.New<IfcSIUnit>(u =>
                        {
                            u.UnitType = IfcUnitEnum.LENGTHUNIT;
                            u.Prefix = IfcSIPrefix.MILLI;
                            u.Name = IfcSIUnitName.METRE;
                        }));
                    });
                });

                var style = model.Instances.New<IfcPresentationStyleAssignment>(sa =>
                    sa.Styles.Add_Reversible(model.Instances.New<IfcSurfaceStyle>(ss =>
                    {
                        ss.Side = IfcSurfaceSide.BOTH;
                        ss.Styles.Add_Reversible(model.Instances.New<IfcSurfaceStyleRendering>(ssr =>
                        {
                            ssr.DiffuseColour = model.Instances.New<IfcColourRgb>(rgb =>
                            {
                                rgb.Red = 1.0;
                                rgb.Green = 0.0;
                                rgb.Blue = 0.0;
                            });
                            ssr.ReflectanceMethod = IfcReflectanceMethodEnum.PLASTIC;
                        }));
                    })));

                var site = model.Instances.New<IfcSite>(s => s.Name = "Default site");
                var building = model.Instances.New<IfcBuilding>(s => s.Name = "Default building");
                var storey = model.Instances.New<IfcBuildingStorey>(s => s.Name = "Default storey");
                var space = model.Instances.New<IfcSpace>(s => s.Name = "Default space");
                space.Representation = GetShape(model, context, width + (colNumber - 1) * colSpace, height + (layerNumber -1) * layerSpace, depth + (rowNumber -1) * rowSpace, null, origin3D);
                space.ObjectPlacement = model.Instances.New<IfcLocalPlacement>(lp => lp.RelativePlacement =
                                    model.Instances.New<IfcAxis2Placement3D>(rp => rp.Location = origin3D)
                                    );
                
                site.AddToSpatialDecomposition(building);
                building.AddToSpatialDecomposition(storey);
                storey.AddToSpatialDecomposition(space);
                project.AddDecomposingObjectToFirstAggregation(model, site);

                for (int layer = 0; layer < layerNumber; layer++)
                {
                    for (int col = 0; col < colNumber; col++)
                    {
                        for (int row = 0; row < rowNumber; row++)
                        {
                            var column = model.Instances.New<IfcColumn>(c =>
                            {
                                c.Name = layer.ToString() + row.ToString() + col.ToString();
                                c.Representation = model.Instances.New<IfcProductDefinitionShape>(pds =>
                                {
                                    pds.Representations.Add_Reversible(model.Instances.New<IfcShapeRepresentation>(sr =>
                                    {
                                        sr.RepresentationType = "SweptSolid";
                                        sr.RepresentationIdentifier = "body";
                                        sr.ContextOfItems = context;
                                        var represItem = model.Instances.New<IfcExtrudedAreaSolid>(ex =>
                                        {
                                            ex.SweptArea = model.Instances.New<IfcRectangleProfileDef>(prof =>
                                            {
                                                prof.Position = model.Instances.New<IfcAxis2Placement2D>(pos =>
                                                {
                                                    pos.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXY(width / 2f, depth / 2f));
                                                });
                                                prof.XDim = width;
                                                prof.YDim = depth;
                                            });
                                            ex.Position = model.Instances.New<IfcAxis2Placement3D>(pos =>
                                            {
                                                pos.Location = model.Instances.New<IfcCartesianPoint>(l => l.SetXYZ(col * colSpace, row * rowSpace, layer * layerSpace));
                                            });
                                            ex.ExtrudedDirection = upDirection;
                                            ex.Depth = height;
                                        });
                                        sr.Items.Add_Reversible(represItem);
                                        model.Instances.New<IfcStyledItem>(si =>
                                        {
                                            si.Item = represItem;
                                            si.Styles.Add_Reversible(style);
                                        });
                                    }));
                                });
                                storey.AddElement(c);
                                c.ObjectPlacement = model.Instances.New<IfcLocalPlacement>(lp => lp.RelativePlacement =
                                    model.Instances.New<IfcAxis2Placement3D>(rp => rp.Location = origin3D)
                                    );
                            });
                        }
                    }
                }


                txn.Commit();
            }
            return model;
        }

        private static IfcProductDefinitionShape GetShape(
            XbimModel model, IfcRepresentationContext context, 
            float width, float height, float depth, 
            IfcPresentationStyleAssignment style, IfcCartesianPoint location)
        {
            return
                model.Instances.New<IfcProductDefinitionShape>(pds =>
                {
                    pds.Representations.Add_Reversible(model.Instances.New<IfcShapeRepresentation>(sr =>
                    {
                        sr.RepresentationType = "SweptSolid";
                        sr.RepresentationIdentifier = "body";
                        sr.ContextOfItems = context;
                        var represItem = model.Instances.New<IfcExtrudedAreaSolid>(ex =>
                        {
                            ex.SweptArea = model.Instances.New<IfcRectangleProfileDef>(prof =>
                            {
                                prof.Position = model.Instances.New<IfcAxis2Placement2D>(pos =>
                                {
                                    pos.Location = model.Instances.New<IfcCartesianPoint>(p => p.SetXY(width / 2f, depth / 2f));
                                });
                                prof.XDim = width;
                                prof.YDim = depth;
                            });
                            ex.Position = model.Instances.New<IfcAxis2Placement3D>(pos =>
                            {
                                pos.Location = location;
                            });
                            ex.ExtrudedDirection = model.Instances.New<IfcDirection>(d => d.SetXYZ(0, 0, 1));
                            ex.Depth = height;
                        });
                        sr.Items.Add_Reversible(represItem);
                        if (style != null)
                            model.Instances.New<IfcStyledItem>(si =>
                            {
                                si.Item = represItem;
                                si.Styles.Add_Reversible(style);
                            });
                    }));
                })
                ;
        }
    }
}
