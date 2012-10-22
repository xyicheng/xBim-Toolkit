using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.COBie.Rows;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ExternalReferenceResource;
using Xbim.Ifc.ElectricalDomain;
using Xbim.Ifc.PropertyResource;
using Xbim.Ifc.MeasureResource;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the System tab.
    /// </summary>
    public class COBieDataSystem : COBieData<COBieSystemRow>
    {
        /// <summary>
        /// Data System constructor
        /// </summary>
        /// <param name="model">The context of the model being generated</param>
        public COBieDataSystem(COBieContext context) : base(context)
        { }

        #region Methods

        /// <summary>
        /// Fill sheet rows for System sheet
        /// </summary>
        /// <returns>COBieSheet<COBieSystemRow></returns>
        public override COBieSheet<COBieSystemRow> Fill()
        {
            ProgressIndicator.ReportMessage("Starting Systems...");

            //Create new sheet
            COBieSheet<COBieSystemRow> systems = new COBieSheet<COBieSystemRow>(Constants.WORKSHEET_SYSTEM);

            // get all IfcSystem, IfcGroup and IfcElectricalCircuit objects from IFC file
            IEnumerable<IfcGroup> ifcGroups = Model.InstancesOfType<IfcGroup>().Where(ifcg => ifcg is IfcSystem); //get anything that is IfcSystem or derived from it eg IfcElectricalCircuit
            //IEnumerable<IfcSystem> ifcSystems = Model.InstancesOfType<IfcSystem>();
            //IEnumerable<IfcElectricalCircuit> ifcElectricalCircuits = Model.InstancesOfType<IfcElectricalCircuit>();
            //ifcGroups = ifcGroups.Union(ifcSystems);
            //ifcGroups = ifcGroups.Union(ifcElectricalCircuits);

            //Alternative method of extraction
            List<string> PropertyNames = new List<string> { "Circuit Number", "System Name" };

            IEnumerable<IfcPropertySet> ifcPropertySets = from ps in Model.InstancesOfType<IfcPropertySet>()
                                                          from psv in ps.HasProperties.OfType<IfcPropertySingleValue>()
                                                          where PropertyNames.Contains(psv.Name)
                                                          select ps;

            ProgressIndicator.Initialise("Creating Systems", ifcGroups.Count() + ifcPropertySets.Count());

            foreach (IfcGroup ifcGroup in ifcGroups)
            {
                ProgressIndicator.IncrementAndUpdate();

                IEnumerable<IfcProduct> ifcProducts = (ifcGroup.IsGroupedBy == null) ? Enumerable.Empty<IfcProduct>() : ifcGroup.IsGroupedBy.RelatedObjects.OfType<IfcProduct>();

                foreach (IfcProduct product in ifcProducts)
                {
                    COBieSystemRow sys = new COBieSystemRow(systems);

                    sys.Name = ifcGroup.Name;

                    sys.CreatedBy = GetTelecomEmailAddress(ifcGroup.OwnerHistory);
                    sys.CreatedOn = GetCreatedOnDateAsFmtString(ifcGroup.OwnerHistory);

                    sys.Category = GetCategory(ifcGroup);
                    sys.ComponentNames = product.Name;
                    sys.ExtSystem = GetExternalSystem(product);
                    sys.ExtObject = ifcGroup.GetType().Name;
                    sys.ExtIdentifier = product.GlobalId;
                    sys.Description = GetSystemDescription(ifcGroup);

                    systems.AddRow(sys);
                }

            }

            
            foreach (IfcPropertySet ifcPropertySet in ifcPropertySets)
            {
                ProgressIndicator.IncrementAndUpdate();
                string name =  "";
                IfcRelDefinesByProperties ifcRelDefinesByProperties = ifcPropertySet.PropertyDefinitionOf.FirstOrDefault(); //one or zero 
                IfcPropertySingleValue ifcPropertySingleValue = ifcPropertySet.HasProperties.OfType<IfcPropertySingleValue>().Where(psv => PropertyNames.Contains(psv.Name)).FirstOrDefault();
                if ((ifcPropertySingleValue != null) && (ifcPropertySingleValue.NominalValue != null) && (!string.IsNullOrEmpty(ifcPropertySingleValue.NominalValue.ToString())))
                    name = ifcPropertySingleValue.NominalValue.ToString();
                else //try for "System Classification" Not in matrix but looks a good candidate
                {
                    IfcPropertySingleValue ifcPropertySVClassification = ifcPropertySet.HasProperties.OfType<IfcPropertySingleValue>().Where(psv => psv.Name == "System Classification").FirstOrDefault(); 
                    if ((ifcPropertySVClassification != null) && (ifcPropertySVClassification.NominalValue != null) && (!string.IsNullOrEmpty(ifcPropertySVClassification.NominalValue.ToString())))
                        name = ifcPropertySVClassification.NominalValue.ToString();
                }
                
                foreach (IfcObject ifcObject in ifcRelDefinesByProperties.RelatedObjects)
                {
                    if (ifcObject != null)
                    {
                        COBieSystemRow sys = new COBieSystemRow(systems);
                        //OK if we have no name lets just guess at the first value as we need a value
                        if (string.IsNullOrEmpty(name))
                        {
                            //get first text value held in NominalValue
                            var names = ifcPropertySet.HasProperties.OfType<IfcPropertySingleValue>().Where(psv => (psv.NominalValue != null) && (!string.IsNullOrEmpty(psv.NominalValue.ToString()))).Select(psv => psv.NominalValue).FirstOrDefault();
                            if (names != null)
                            {
                                name = names.ToString();
                            }
                            else
                            {
                                //OK last chance, lets take the property name that is not in the filter list of strings, ie. != "Circuit Number", "System Name" or "System Classification" from above 
                                IfcPropertySingleValue propname = ifcPropertySet.HasProperties.OfType<IfcPropertySingleValue>().Where(psv => !PropertyNames.Contains(psv.Name)).FirstOrDefault();
                                name = propname.Name.ToString();
                            }
                            
                        }
                        sys.Name = string.IsNullOrEmpty(name) ? DEFAULT_STRING : name;

                        sys.CreatedBy = GetTelecomEmailAddress(ifcObject.OwnerHistory);
                        sys.CreatedOn = GetCreatedOnDateAsFmtString(ifcObject.OwnerHistory);
                        
                        sys.Category = (ifcPropertySingleValue.Name == "Circuit Number") ? "circuit" : GetCategory(ifcObject); //per matrix v9
                        sys.ComponentNames = ifcObject.Name;
                        sys.ExtSystem = GetExternalSystem(ifcPropertySet);
                        sys.ExtObject = ifcPropertySingleValue.GetType().Name;
                        sys.ExtIdentifier = DEFAULT_STRING; //used IfcPropertySingleValue, has no GlobalId
                        sys.Description = string.IsNullOrEmpty(name) ? DEFAULT_STRING : name; ;

                        systems.AddRow(sys);
                    }
                }
            }
            ProgressIndicator.Finalise();
            return systems;
        }

        private string GetSystemDescription(IfcGroup ifcGroup)
        {
            if (ifcGroup != null)
            {
                if (!string.IsNullOrEmpty(ifcGroup.Description)) return ifcGroup.Description;
                else if (!string.IsNullOrEmpty(ifcGroup.Name)) return ifcGroup.Name;
            }
            return Constants.DEFAULT_STRING;
        }
        #endregion
    }
}
