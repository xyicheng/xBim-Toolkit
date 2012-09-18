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
                        
            foreach (IfcConstructionEquipmentResource ifcConstructionEquipmentResource in ifcCer)
            { 
                //if (ifcConstructionEquipmentResource == null) continue;

                COBieResourceRow resource = new COBieResourceRow(resources);
               
                resource.Name = (string.IsNullOrEmpty(ifcConstructionEquipmentResource.Name.ToString())) ? DEFAULT_STRING : ifcConstructionEquipmentResource.Name.ToString();
                resource.CreatedBy = GetTelecomEmailAddress(ifcConstructionEquipmentResource.OwnerHistory);
                resource.CreatedOn = GetCreatedOnDateAsFmtString(ifcConstructionEquipmentResource.OwnerHistory);
                resource.Category = (string.IsNullOrEmpty(ifcConstructionEquipmentResource.ObjectType.ToString())) ? DEFAULT_STRING : ifcConstructionEquipmentResource.ObjectType.ToString();
                resource.ExtSystem = ifcApplication.ApplicationFullName;
                resource.ExtObject = (ifcConstructionEquipmentResource == null) ? DEFAULT_STRING : ifcConstructionEquipmentResource.GetType().Name;
                resource.ExtIdentifier = ifcConstructionEquipmentResource.GlobalId;
                resource.Description = (string.IsNullOrEmpty(ifcConstructionEquipmentResource.Description)) ? DEFAULT_STRING : ifcConstructionEquipmentResource.Description.ToString();

                resources.Rows.Add(resource);
            }

            return resources;
        }
        #endregion
    }
}
