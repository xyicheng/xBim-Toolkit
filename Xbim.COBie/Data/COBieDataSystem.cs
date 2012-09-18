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

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the System tab.
    /// </summary>
    public class COBieDataSystem : COBieData
    {
        /// <summary>
        /// Data System constructor
        /// </summary>
        /// <param name="model">IModel to read data from</param>
        public COBieDataSystem(IModel model)
        {
            Model = model;
        }

        #region Methods

        /// <summary>
        /// Fill sheet rows for System sheet
        /// </summary>
        /// <returns>COBieSheet<COBieSystemRow></returns>
        public COBieSheet<COBieSystemRow> Fill()
        {
            //Create new sheet
            COBieSheet<COBieSystemRow> systems = new COBieSheet<COBieSystemRow>(Constants.WORKSHEET_SYSTEM);

            // get all IfcSystem, IfcGroup and IfcElectricalCircuit objects from IFC file
            IEnumerable<IfcGroup> ifcGroups = Model.InstancesOfType<IfcGroup>().Where(ifcg => ifcg is IfcSystem); //get anything that is IfcSystem or derived from it
            //IEnumerable<IfcSystem> ifcSystems = Model.InstancesOfType<IfcSystem>();
            //IEnumerable<IfcElectricalCircuit> ifcElectricalCircuits = Model.InstancesOfType<IfcElectricalCircuit>();
            //ifcGroups = ifcGroups.Union(ifcSystems);
            //ifcGroups = ifcGroups.Union(ifcElectricalCircuits);


            foreach (IfcGroup ifcGroup in ifcGroups)
            {
               
                IEnumerable<IfcProduct> ifcProducts = (ifcGroup.IsGroupedBy == null) ? Enumerable.Empty<IfcProduct>() : ifcGroup.IsGroupedBy.RelatedObjects.OfType<IfcProduct>();

                foreach (IfcProduct product in ifcProducts)
                {
                    COBieSystemRow sys = new COBieSystemRow(systems);

                    sys.Name = ifcGroup.Name;

                    sys.CreatedBy = GetTelecomEmailAddress(ifcGroup.OwnerHistory);
                    sys.CreatedOn = GetCreatedOnDateAsFmtString(ifcGroup.OwnerHistory);

                    sys.Category = GetCategory(ifcGroup);
                    sys.ComponentNames = product.Name;
                    sys.ExtSystem = ifcApplication.ApplicationFullName;
                    sys.ExtObject = ifcGroup.GetType().Name;
                    sys.ExtIdentifier = product.GlobalId;
                    sys.Description = GetSystemDescription(ifcGroup);

                    systems.Rows.Add(sys);
                }

            }

            //Alternative method of extraction
            List<string> PropertyNames = new List<string> { "Circuit Number", "System Name" };

            IEnumerable<IfcPropertySet> ifcPropertySets = from ps in Model.InstancesOfType<IfcPropertySet>()
                                                          from psv in ps.HasProperties.OfType<IfcPropertySingleValue>()
                                                          where PropertyNames.Contains(psv.Name)
                                                          select ps;
            foreach (IfcPropertySet ifcPropertySet in ifcPropertySets)
            {
                IfcRelDefinesByProperties ifcRelDefinesByProperties = ifcPropertySet.PropertyDefinitionOf.FirstOrDefault(); //one or zero 
                IfcPropertySingleValue ifcPropertySingleValue = ifcPropertySet.HasProperties.OfType<IfcPropertySingleValue>().Where(psv => PropertyNames.Contains(psv.Name)).FirstOrDefault();
                foreach (IfcObject ifcObject in ifcRelDefinesByProperties.RelatedObjects)
                {
                    if (ifcObject != null)
                    {
                        COBieSystemRow sys = new COBieSystemRow(systems);
                        string name = ifcPropertySingleValue.NominalValue.ToString();
                        sys.Name = string.IsNullOrEmpty(name) ? DEFAULT_STRING : name;

                        sys.CreatedBy = GetTelecomEmailAddress(ifcObject.OwnerHistory);
                        sys.CreatedOn = GetCreatedOnDateAsFmtString(ifcObject.OwnerHistory);
                        
                        sys.Category = (ifcPropertySingleValue.Name == "Circuit Number") ? "circuit" : GetCategory(ifcObject); //per matrix v9
                        sys.ComponentNames = ifcObject.Name;
                        sys.ExtSystem = ifcApplication.ApplicationFullName;
                        sys.ExtObject = ifcPropertySingleValue.GetType().Name;
                        sys.ExtIdentifier = DEFAULT_STRING; //used IfcPropertySingleValue, has no GlobalId
                        sys.Description = string.IsNullOrEmpty(name) ? DEFAULT_STRING : name; ;

                    systems.Rows.Add(sys);
                    }
                }
            }   

            return systems;
        }

        private string GetSystemDescription(IfcGroup ifcGroup)
        {
            if (ifcGroup != null)
            {
                if (!string.IsNullOrEmpty(ifcGroup.Description)) return ifcGroup.Description;
                else if (!string.IsNullOrEmpty(ifcGroup.Name)) return ifcGroup.Name;
            }
            return DEFAULT_STRING;
        }
        #endregion
    }
}
