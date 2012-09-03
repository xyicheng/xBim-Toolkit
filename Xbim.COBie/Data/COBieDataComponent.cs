using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.Rows;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.SharedBldgElements;
using Xbim.Ifc.SharedBldgServiceElements;
using Xbim.Ifc.Extensions;
using Xbim.XbimExtensions;
using Xbim.Ifc.PropertyResource;

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
        public COBieSheet<COBieComponentRow> Fill(ref COBieSheet<COBieAttributeRow> attributes)
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

            //first try property single values only get ProertySingleValues for each element and add to Dictionary
            //Dictionary<IfcElement, List<IfcPropertySingleValue>> propSetsVals = res.OfType<IfcElement>().ToDictionary(el => el, el => el.PropertySets.SelectMany(ps => ps.HasProperties.OfType<IfcPropertySingleValue>()).ToList<IfcPropertySingleValue>()); 
            //get property sets and property single values
            Dictionary<IfcElement, Dictionary<IfcPropertySet, List<IfcPropertySingleValue>>> propSetsVals2 = res.OfType<IfcElement>().ToDictionary(el => el, el => el.PropertySets.ToDictionary(ps => ps, ps => ps.HasProperties.OfType<IfcPropertySingleValue>().ToList())); 


            List<string> AttNames = new List<string> { "SerialNumber",
                                                       "InstallationDate",
                                                       "WarrantyStartDate",
                                                       "TagNumber",
                                                       "BarCode",
                                                       "AssetIdentifier"};
            List<string> ExcludeAtts = AttNames;
            ExcludeAtts.Add("Circuit NumberSystem Type");
            ExcludeAtts.Add("System Name");

            foreach (var obj in res)
            {
                COBieComponentRow component = new COBieComponentRow(components);

                IfcElement el = obj as IfcElement;
                //IfcOwnerHistory ifcOwnerHistory = el.OwnerHistory;
                component.Name = el.Name;
                component.CreatedBy = GetTelecomEmailAddress(el.OwnerHistory);
                component.CreatedOn = GetCreatedOnDateAsFmtString(el.OwnerHistory);
                //el.IsDefinedBy.ToArray()[1] as IfcRelDefinesByType).RelatingType.Name
                
                component.TypeName = GetTypeName(el);
                component.Space = GetComponentRelatedSpace(el);
                component.Description = GetComponentDescription(el);
                component.ExtSystem = GetIfcApplication().ApplicationFullName;
                component.ExtObject = el.GetType().Name;
                component.ExtIdentifier = el.GlobalId;
                //get the PropertySingleValues for the required properties
                var pSVs = (from dic in propSetsVals2[el]
                            from pset in dic.Value
                            where AttNames.Contains(pset.Name)
                            select new { Name = pset.Name, Value = ((pset != null) && (pset.NominalValue != null)) ? pset.NominalValue.Value.ToString() : DEFAULT_STRING }).ToList();
                //var pSVs = (from pSet in el.IsDefinedByProperties
                //                where pSet.RelatingPropertyDefinition is IfcPropertySet
                //           from pSV in ((IfcPropertySet)pSet.RelatingPropertyDefinition).HasProperties.OfType<IfcPropertySingleValue>()
                //                where AttNames.Contains(pSV.Name)
                //                     select new { Name = pSV.Name, Value = ((pSV != null) && (pSV.NominalValue != null)) ? pSV.NominalValue.Value.ToString() : DEFAULT_STRING }).ToList();

                var item = pSVs.Where(p => p.Name == "SerialNumber").FirstOrDefault();
                component.SerialNumber = (item != null) ? item.Value : DEFAULT_STRING;//SetProperty(PSVs, "SerialNumber");
                item = pSVs.Where(p => p.Name == "InstallationDate").FirstOrDefault();
                component.InstallationDate = (item != null) ? item.Value : DEFAULT_STRING; //SetProperty(PSVs, "InstallationDate");
                item = pSVs.Where(p => p.Name == "WarrantyStartDate").FirstOrDefault();
                component.WarrantyStartDate = (item != null) ? item.Value : DEFAULT_STRING; //SetProperty(PSVs, "WarrantyStartDate");
                item = pSVs.Where(p => p.Name == "TagNumber").FirstOrDefault();
                component.TagNumber = (item != null) ? item.Value : DEFAULT_STRING; //SetProperty(PSVs, "TagNumber");
                item = pSVs.Where(p => p.Name == "BarCode").FirstOrDefault();
                component.BarCode = (item != null) ? item.Value : DEFAULT_STRING; //SetProperty(PSVs, "BarCode");
                item = pSVs.Where(p => p.Name == "AssetIdentifier").FirstOrDefault();
                component.AssetIdentifier = (item != null) ? item.Value : DEFAULT_STRING;

                components.Rows.Add(component);
                //----------fill in the attribute information for floor-----------
                //pass data from this sheet info as Dictionary
                Dictionary<string, string> passedValues = new Dictionary<string, string>(){{"Sheet", "Component"}, 
                                                                                          {"Name", component.Name},
                                                                                          {"CreatedBy", component.CreatedBy},
                                                                                          {"CreatedOn", component.CreatedOn},
                                                                                          {"ExtSystem", component.ExtSystem}
                                                                                          };//required property date <PropertySetName, PropertyName>
                
                //add *ALL* the attributes to the passed attributes sheet except property names that match the passed List<string>
                //SetAttributeSheet(el, passedValues, ExcludeAtts, null, null, ref attributes);
                SetAttribSheet(passedValues, ExcludeAtts, null, ref attributes, propSetsVals2[el]);
            }

            return components;
        }

        
        /// <summary>
        /// Return the NominalValue of the property with the propName passed to method
        /// </summary>
        /// <param name="PSVs">IEnumerable<IfcPropertySingleValue> List</param>
        /// <param name="propName">Property name to extract value from PSVs list </param>
        /// <returns>property value string</returns>
        private static string SetProperty(IEnumerable<IfcPropertySingleValue> PSVs, string propName)
        {
            IfcPropertySingleValue Psv = PSVs.Where(p => p.Name == propName).FirstOrDefault();
            return ((Psv != null) && (Psv.NominalValue != null)) ? Psv.NominalValue.Value.ToString() : DEFAULT_STRING;
        }

        internal string GetComponentRelatedSpace(IfcElement el)
        {
            if (el != null && el.ContainedInStructure.Count() > 0)
            {
                var owningSpace = el.ContainedInStructure.First().RelatingStructure;
                if (owningSpace.GetType() == typeof(IfcSpace))
                    return owningSpace.Name.ToString();
            }
            return Constants.DEFAULT_VAL;
        }

        internal string GetComponentDescription(IfcElement el)
        {
            if (el != null)
            {
                if (!string.IsNullOrEmpty(el.Description)) return el.Description;
                else if (!string.IsNullOrEmpty(el.Name)) return el.Name;
            }
            return Constants.DEFAULT_VAL;
        }
        #endregion
    }
}
