using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.COBie.Rows;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ExternalReferenceResource;

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

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcRelAggregates> ifcRelAggregates = Model.InstancesOfType<IfcRelAggregates>();

            
            IfcProduct ifcProduct = Model.InstancesOfType<IfcProduct>().FirstOrDefault();
            IfcClassification ifcClassification = Model.InstancesOfType<IfcClassification>().FirstOrDefault();
            string applicationFullName = GetIfcApplication().ApplicationFullName;

            foreach (IfcRelAggregates ra in ifcRelAggregates)
            {
                COBieAssemblyRow assembly = new COBieAssemblyRow(assemblies);

                //IfcOwnerHistory ifcOwnerHistory = ra.OwnerHistory;

                assembly.Name = (ra.Name == null || ra.Name.ToString() == "") ? "AssemblyName" : ra.Name.ToString();

                assembly.CreatedBy = GetTelecomEmailAddress(ra.OwnerHistory);
                assembly.CreatedOn = GetCreatedOnDateAsFmtString(ra.OwnerHistory);

                assembly.SheetName = "SheetName:";
                assembly.ParentName = ifcProduct.Name;
                assembly.ChildNames = ifcProduct.Name;
                assembly.AssemblyType = (ifcClassification == null) ? "" : ifcClassification.Name.ToString();
                assembly.ExtSystem = applicationFullName;
                assembly.ExtObject = "IfcRelAggregates";
                assembly.ExtIdentifier = string.IsNullOrEmpty(ra.GlobalId) ? DEFAULT_VAL : ra.GlobalId.ToString();
                assembly.Description = GetAssemblyDescription(ra);

                assemblies.Rows.Add(assembly);

                COBieCell testCell = assembly[7];
            }

            return assemblies;
        }

        private string GetAssemblyDescription(IfcRelAggregates ra)
        {
            if (ra != null)
            {
                if (!string.IsNullOrEmpty(ra.Description)) return ra.Description;
                else if (!string.IsNullOrEmpty(ra.Name)) return ra.Name;
            }
            return DEFAULT_VAL;
        }

        #endregion
    }
}
