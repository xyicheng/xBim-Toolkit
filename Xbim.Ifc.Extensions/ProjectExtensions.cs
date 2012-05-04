﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc.Extensions
// Filename:    ProjectExtensions.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc.GeometryResource;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.RepresentationResource;
using Xbim.XbimExtensions;

#endregion

namespace Xbim.Ifc.Extensions
{
    public enum ProjectUnits
    {
        SIUnitsUK
    }

    public static class ProjectExtensions
    {
        #region Unit Initialization

        /// <summary>
        ///   Sets up the default units as SI
        ///   Creates the GeometricRepresentationContext for a Model view, required by Ifc compliance
        /// </summary>
        /// <param name = "ifcProject"></param>
        public static void Initialize(this IfcProject ifcProject, ProjectUnits units)
        {
            IModel model = ModelManager.ModelOf(ifcProject);
            if (units == ProjectUnits.SIUnitsUK)
            {
                IfcUnitAssignment ua = model.New<IfcUnitAssignment>();
                ua.Units.Add_Reversible(model.New<IfcSIUnit>(s =>
                                                                 {
                                                                     s.UnitType = IfcUnitEnum.LENGTHUNIT;
                                                                     s.Name = IfcSIUnitName.METRE;
                                                                     s.Prefix = IfcSIPrefix.MILLI;
                                                                 }));
                ua.Units.Add_Reversible(model.New<IfcSIUnit>(s =>
                                                                 {
                                                                     s.UnitType = IfcUnitEnum.AREAUNIT;
                                                                     s.Name = IfcSIUnitName.SQUARE_METRE;
                                                                 }));
                ua.Units.Add_Reversible(model.New<IfcSIUnit>(s =>
                                                                 {
                                                                     s.UnitType = IfcUnitEnum.VOLUMEUNIT;
                                                                     s.Name = IfcSIUnitName.CUBIC_METRE;
                                                                 }));
                ua.Units.Add_Reversible(model.New<IfcSIUnit>(s =>
                                                                 {
                                                                     s.UnitType = IfcUnitEnum.SOLIDANGLEUNIT;
                                                                     s.Name = IfcSIUnitName.STERADIAN;
                                                                 }));
                ua.Units.Add_Reversible(model.New<IfcSIUnit>(s =>
                                                                 {
                                                                     s.UnitType = IfcUnitEnum.PLANEANGLEUNIT;
                                                                     s.Name = IfcSIUnitName.RADIAN;
                                                                 }));
                ua.Units.Add_Reversible(model.New<IfcSIUnit>(s =>
                                                                 {
                                                                     s.UnitType = IfcUnitEnum.MASSUNIT;
                                                                     s.Name = IfcSIUnitName.GRAM;
                                                                 }));
                ua.Units.Add_Reversible(model.New<IfcSIUnit>(s =>
                                                                 {
                                                                     s.UnitType = IfcUnitEnum.TIMEUNIT;
                                                                     s.Name = IfcSIUnitName.SECOND;
                                                                 }));
                ua.Units.Add_Reversible(model.New<IfcSIUnit>(s =>
                                                                 {
                                                                     s.UnitType =
                                                                         IfcUnitEnum.THERMODYNAMICTEMPERATUREUNIT;
                                                                     s.Name = IfcSIUnitName.DEGREE_CELSIUS;
                                                                 }));
                ua.Units.Add_Reversible(model.New<IfcSIUnit>(s =>
                                                                 {
                                                                     s.UnitType = IfcUnitEnum.LUMINOUSINTENSITYUNIT;
                                                                     s.Name = IfcSIUnitName.LUMEN;
                                                                 }));
                ifcProject.UnitsInContext = ua;
            }
            //Create the Mandatory Model View
            if (ModelContext(ifcProject) == null)
            {
                IfcCartesianPoint origin = model.New<IfcCartesianPoint>(p => p.SetXYZ(0, 0, 0));
                IfcAxis2Placement3D axis3D = model.New<IfcAxis2Placement3D>(a => a.Location = origin);
                IfcGeometricRepresentationContext gc = model.New<IfcGeometricRepresentationContext>(c =>
                                                                                                        {
                                                                                                            c.
                                                                                                                ContextType
                                                                                                                =
                                                                                                                "Model";
                                                                                                            c.
                                                                                                                ContextIdentifier
                                                                                                                =
                                                                                                                "Building Model";
                                                                                                            c.
                                                                                                                CoordinateSpaceDimension
                                                                                                                = 3;
                                                                                                            c.Precision
                                                                                                                =
                                                                                                                0.00001;
                                                                                                            c.
                                                                                                                WorldCoordinateSystem
                                                                                                                = axis3D;
                                                                                                        }
                    );
                ifcProject.RepresentationContexts.Add_Reversible(gc);

                IfcCartesianPoint origin2D = model.New<IfcCartesianPoint>(p => p.SetXY(0, 0));
                IfcAxis2Placement2D axis2D = model.New<IfcAxis2Placement2D>(a => a.Location = origin2D);
                IfcGeometricRepresentationContext pc = model.New<IfcGeometricRepresentationContext>(c =>
                {
                    c.
                        ContextType
                        =
                        "Plan";
                    c.
                        ContextIdentifier
                        =
                        "Building Plan View";
                    c.
                        CoordinateSpaceDimension
                        = 2;
                    c.Precision
                        =
                        0.00001;
                    c.
                        WorldCoordinateSystem
                        = axis2D;
                }
                    );
                ifcProject.RepresentationContexts.Add_Reversible(pc);

            }
        }

        #endregion

        public static void SetOrChangeSIUnit(this IfcProject ifcProject, IfcUnitEnum unitType, IfcSIUnitName siUnitName,
                                             IfcSIPrefix siUnitPrefix)
        {
            IModel model = ModelManager.ModelOf(ifcProject);
            if (ifcProject.UnitsInContext == null)
            {
                ifcProject.UnitsInContext = model.New<IfcUnitAssignment>();
            }

            IfcUnitAssignment unitsAssignment = ifcProject.UnitsInContext;
            unitsAssignment.SetOrChangeSIUnit(unitType, siUnitName, siUnitPrefix);
        }

        public static void SetOrChangeConversionUnit(this IfcProject ifcProject, IfcUnitEnum unitType,
                                                     ConversionBasedUnit conversionUnit)
        {
            IModel model = ModelManager.ModelOf(ifcProject);
            if (ifcProject.UnitsInContext == null)
            {
                ifcProject.UnitsInContext = model.New<IfcUnitAssignment>();
            }

            IfcUnitAssignment unitsAssignment = ifcProject.UnitsInContext;
            unitsAssignment.SetOrChangeConversionUnit(unitType, conversionUnit);
        }


        public static IfcGeometricRepresentationContext ModelContext(this IfcProject proj)
        {
            return
                proj.RepresentationContexts.Where<IfcGeometricRepresentationContext>(r => r.ContextType == "Model").
                    FirstOrDefault();
        }

        public static IfcGeometricRepresentationContext PlanContext(this IfcProject proj)
        {
            return
                proj.RepresentationContexts.Where<IfcGeometricRepresentationContext>(r => r.ContextType == "Plan").
                    FirstOrDefault();
        }


        #region Decomposition methods

        /// <summary>
        ///   Adds Site to the IsDecomposedBy Collection.
        /// </summary>
        public static void AddSite(this IfcProject ifcProject, IfcSite site)
        {
            IEnumerable<IfcRelDecomposes> decomposition = ifcProject.IsDecomposedBy;
            if (decomposition.Count() == 0) //none defined create the relationship
            {
                IfcRelAggregates relSub = ModelManager.ModelOf(ifcProject).New<IfcRelAggregates>();
                relSub.RelatingObject = ifcProject;
                relSub.RelatedObjects.Add_Reversible(site);
            }
            else
            {
                decomposition.First().RelatedObjects.Add_Reversible(site);
            }
        }

        public static IEnumerable<IfcSite> GetSites(this IfcProject ifcProject)
        {
            IEnumerable<IfcRelAggregates> aggregate = ifcProject.IsDecomposedBy.OfType<IfcRelAggregates>();
            List<IfcSite> sites = new List<IfcSite>();
            foreach (IfcRelAggregates rel in aggregate)
            {
                foreach (IfcObjectDefinition definition in rel.RelatedObjects)
                {
                    if (definition is IfcSite) sites.Add(definition as IfcSite);
                }
            }
            return sites;
        }

        /// <summary>
        ///   Adds Building to the IsDecomposedBy Collection.
        /// </summary>
        public static void AddBuilding(this IfcProject ifcProject, IfcBuilding building)
        {
            IEnumerable<IfcRelDecomposes> decomposition = ifcProject.IsDecomposedBy;
            if (decomposition.Count() == 0) //none defined create the relationship
            {
                IfcRelAggregates relSub = ModelManager.ModelOf(ifcProject).New<IfcRelAggregates>();
                relSub.RelatingObject = ifcProject;
                relSub.RelatedObjects.Add_Reversible(building);
            }
            else
            {
                decomposition.First().RelatedObjects.Add_Reversible(building);
            }
        }

        #endregion
    }
}