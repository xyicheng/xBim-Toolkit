using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.Rows;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.SharedBldgElements;
using Xbim.Ifc.SharedBldgServiceElements;
using Xbim.XbimExtensions;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the Component tab.
    /// </summary>
    public class COBieDataComponent : COBieData
    {
        /// <summary>
        /// Data Component constructor
        /// </summary>
        /// <param name="model">IModel to read data from</param>
        public COBieDataComponent(IModel model)
        {
            Model = model;
        }

        #region Methods

        /// <summary>
        /// Fill sheet rows for Component sheet
        /// </summary>
        /// <returns>COBieSheet<COBieComponentRow></returns>
        public COBieSheet<COBieComponentRow> Fill()
        {
            //Create new sheet
           COBieSheet<COBieComponentRow> components = new COBieSheet<COBieComponentRow>(Constants.WORKSHEET_COMPONENT);

            List<Type> excludedTypes = new List<Type>{  typeof(IfcWall),
                                                            typeof(IfcWallStandardCase),
                                                            typeof(IfcWall),
                                                            typeof(IfcWallStandardCase),
                                                            typeof(IfcSlab),
                                                            typeof(IfcBeam),
                                                            typeof(IfcSpace),
                                                            typeof(IfcBuildingStorey),
                                                            typeof(IfcBuilding),
                                                            typeof(IfcSite),
                                                            typeof(IfcProject),
                                                            typeof(IfcColumn),
                                                            typeof(IfcMember),
                                                            typeof(IfcPlate),
                                                            typeof(IfcRailing),
                                                            typeof(IfcStairFlight),
                                                            typeof(IfcCurtainWall),
                                                            typeof(IfcRampFlight),
                                                            typeof(IfcVirtualElement),
                                                            typeof(IfcFeatureElement),
                                                            typeof(Xbim.Ifc.SharedComponentElements.IfcFastener),
                                                            typeof(Xbim.Ifc.SharedComponentElements.IfcMechanicalFastener),
                                                            typeof(IfcElementAssembly),
                                                            typeof(Xbim.Ifc.StructuralElementsDomain.IfcBuildingElementPart),
                                                            typeof(Xbim.Ifc.StructuralElementsDomain.IfcReinforcingBar),
                                                            typeof(Xbim.Ifc.StructuralElementsDomain.IfcReinforcingMesh),
                                                            typeof(Xbim.Ifc.StructuralElementsDomain.IfcTendon),
                                                            typeof(Xbim.Ifc.StructuralElementsDomain.IfcTendonAnchor),
                                                            typeof(Xbim.Ifc.StructuralElementsDomain.IfcFooting),
                                                            typeof(Xbim.Ifc.StructuralElementsDomain.IfcPile),
                                                            typeof(IfcRamp),
                                                            typeof(IfcRoof),
                                                            typeof(IfcStair),
                                                            typeof(IfcFlowFitting),
                                                            typeof(IfcFlowSegment),
                                                            typeof(IfcDistributionPort) };

         
            IEnumerable<IfcRelAggregates> relAggregates = Model.InstancesOfType<IfcRelAggregates>();
            IEnumerable<IfcRelContainedInSpatialStructure> relSpatial = Model.InstancesOfType<IfcRelContainedInSpatialStructure>();
             
            var q1 = from x in relAggregates
                     from y in x.RelatedObjects
                     where !excludedTypes.Contains(y.GetType())
                     select y;

            var q2 = from x in relSpatial
                     from y in x.RelatedElements
                     where !excludedTypes.Contains(y.GetType())
                     select y;

            var res = q1.Concat(q2).GroupBy(el => el.Name)
                                   .Select(g => g.First())
                                   .ToList();

            foreach (var obj in res)
            {
                COBieComponentRow component = new COBieComponentRow(components);

                IfcElement el = obj as IfcElement;
                //IfcOwnerHistory ifcOwnerHistory = el.OwnerHistory;
                component.Name = el.Name;
                component.CreatedBy = GetTelecomEmailAddress(el.OwnerHistory);
                component.CreatedOn = GetCreatedOnDateAsFmtString(el.OwnerHistory);
                component.TypeName = el.ObjectType.ToString();
                component.Space = component.GetComponentRelatedSpace(el);
                component.Description = component.GetComponentDescription(el);
                component.ExtSystem = GetIfcApplication().ApplicationFullName;
                component.ExtObject = el.GetType().Name;
                component.ExtIdentifier = el.GlobalId;
                component.SerialNumber = "";
                component.InstallationDate = "";
                component.WarrantyStartDate = "";
                //component.TagNumber = el.Tag.ToString();
                component.BarCode = "";
                component.AssetIdentifier = "";

                components.Rows.Add(component);
            }

            return components;
        }
        #endregion
    }
}
