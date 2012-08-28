using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.Rows;
using Xbim.Ifc.ConstructionMgmtDomain;
using Xbim.XbimExtensions;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the Resource tab.
    /// </summary>
    public class COBieDataResource : COBieData
    {

        /// <summary>
        /// Data Resource constructor
        /// </summary>
        /// <param name="model">IModel to read data from</param>
        public COBieDataResource(IModel model)
        {
            Model = model;
        }

        #region Methods

        /// <summary>
        /// Fill sheet rows for Resource sheet
        /// </summary>
        /// <returns>COBieSheet<COBieResourceRow></returns>
        public COBieSheet<COBieResourceRow> Fill()
        {
            //create new sheet 
            COBieSheet<COBieResourceRow> resources = new COBieSheet<COBieResourceRow>(Constants.WORKSHEET_RESOURCE);

            // get all IfcConstructionEquipmentResource objects from IFC file
            IEnumerable<IfcConstructionEquipmentResource> ifcCer = Model.InstancesOfType<IfcConstructionEquipmentResource>();
                        
            foreach (IfcConstructionEquipmentResource cer in ifcCer)
            {
                COBieResourceRow resource = new COBieResourceRow(resources);
                
                resource.Name = (cer == null) ? "" : cer.Name.ToString();

                resource.CreatedBy = GetTelecomEmailAddress(cer.OwnerHistory);
                resource.CreatedOn = GetCreatedOnDateAsFmtString(cer.OwnerHistory);

                resource.Category = (cer == null) ? "" : cer.ObjectType.ToString();
                //IfcRelAssociatesClassification ifcRAC = to.HasAssociations.OfType<IfcRelAssociatesClassification>().FirstOrDefault();
                //IfcClassificationReference ifcCR = (IfcClassificationReference)ifcRAC.RelatingClassification;
                //resource.Category = ifcCR.Name;

                resource.ExtSystem = GetIfcApplication().ApplicationFullName;
                resource.ExtObject = cer.GetType().Name;
                resource.ExtIdentifier = cer.GlobalId;
                resource.Description = (cer == null) ? "" : cer.Description.ToString();

                resources.Rows.Add(resource);
            }

            return resources;
        }
        #endregion
    }
}
