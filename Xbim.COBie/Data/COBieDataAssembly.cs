using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.COBie.Rows;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ExternalReferenceResource;
using Xbim.Ifc.ProductExtension;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the Assembly tab.
    /// </summary>
    public class COBieDataAssembly : COBieData
    {
        /// <summary>
        /// Data Assembly constructor
        /// </summary>
        /// <param name="model">IModel to read data from</param>
        public COBieDataAssembly(IModel model)
        {
            Model = model;
        }

        #region Methods

        /// <summary>
        /// Fill sheet rows for Assembly sheet
        /// </summary>
        /// <returns>COBieSheet<COBieAssemblyRow></returns>
        public COBieSheet<COBieAssemblyRow> Fill()
        {
            //Create new sheet
            COBieSheet<COBieAssemblyRow> assemblies = new COBieSheet<COBieAssemblyRow>(Constants.WORKSHEET_ASSEMBLY);

            //list or classes to exclude if the related object of the IfcRelAggregates is one of these types
            List<Type> excludedTypes = new List<Type>{  typeof(IfcSite),
                                                        typeof(IfcProject),
                                                        typeof(IfcBuilding),
                                                        typeof(IfcBuildingStorey)
                                                        };

            // get ifcRelAggregates objects from IFC file what are not in the excludedTypes type list
            IEnumerable<IfcRelAggregates> ifcRelAggregates = Model.InstancesOfType<IfcRelAggregates>();//.Where(ra => (ra.RelatingObject is IfcProduct) && !excludedTypes.Contains(ra.RelatingObject.GetType()));
            IEnumerable<IfcRelNests> ifcRelNests = Model.InstancesOfType<IfcRelNests>(); //.Where(ra => (ra.RelatingObject is IfcProduct) && !excludedTypes.Contains(ra.RelatingObject.GetType()));

            IEnumerable<IfcRelDecomposes> RelAll = (from ra in ifcRelAggregates
                                                      where ((ra.RelatingObject is IfcProduct) || (ra.RelatingObject is IfcTypeObject)) && !excludedTypes.Contains(ra.RelatingObject.GetType())
                                                      select ra as IfcRelDecomposes).Union
                                                      (from rn in ifcRelNests
                                                      where ((rn.RelatingObject is IfcProduct) || (rn.RelatingObject is IfcTypeObject)) && !excludedTypes.Contains(rn.RelatingObject.GetType())
                                                      select rn as IfcRelDecomposes);


            IfcClassification ifcClassification = Model.InstancesOfType<IfcClassification>().FirstOrDefault();
            string applicationFullName = GetIfcApplication().ApplicationFullName;
            //TODO: Assembly sheet part done.
            foreach (IfcRelDecomposes ra in RelAll)
            {
                COBieAssemblyRow assembly = new COBieAssemblyRow(assemblies);
                
                //assembly.Name = (string.IsNullOrEmpty(ra.Name)) ? "AssemblyName" : ra.Name.ToString();
                if (string.IsNullOrEmpty(ra.Name))
                {
                    if ((ra.RelatingObject is IfcProduct) || (ra.RelatingObject is IfcTypeObject))
                    {
                        assembly.Name = ra.RelatingObject.Name.ToString();
                    }
                    else
                        assembly.Name = "AssemblyName";
                }
                else
                    assembly.Name = ra.Name.ToString();

                
                assembly.CreatedBy = GetTelecomEmailAddress(ra.OwnerHistory);
                assembly.CreatedOn = GetCreatedOnDateAsFmtString(ra.OwnerHistory);

                assembly.SheetName = "SheetName:";
                assembly.ParentName = ra.RelatingObject.Name;
                string childNames = "";
                foreach (var item in ra.RelatedObjects)
	            {
                    childNames += item.Name + ", ";
	            }
                assembly.ChildNames = childNames.TrimEnd(',');

                assembly.AssemblyType = (ifcClassification == null) ? "" : ifcClassification.Name.ToString();
                assembly.ExtSystem = applicationFullName;
                assembly.ExtObject = "IfcRelAggregates";
                assembly.ExtIdentifier = string.IsNullOrEmpty(ra.GlobalId) ? DEFAULT_STRING : ra.GlobalId.ToString();
                assembly.Description = GetAssemblyDescription(ra);

                assemblies.Rows.Add(assembly);

                COBieCell testCell = assembly[7];
            }

            return assemblies;
        }

        private string GetAssemblyDescription(IfcRelDecomposes ra)
        {
            if (ra != null)
            {
                if (!string.IsNullOrEmpty(ra.Description)) return ra.Description;
                else if (!string.IsNullOrEmpty(ra.Name)) return ra.Name;
            }
            return DEFAULT_STRING;
        }

        #endregion
    }
}
