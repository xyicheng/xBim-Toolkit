using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.Rows;
using Xbim.XbimExtensions.Transactions;
using Xbim.Ifc.ConstructionMgmtDomain;

namespace Xbim.COBie.Serialisers.XbimSerialiser
{
    public class COBieXBimResource : COBieXBim
    {
        public COBieXBimResource(COBieXBimContext xBimContext)
            : base(xBimContext)
        {
        }
        
        /// <summary>
        /// Add the IfcConstructionEquipmentResource to the Model object
        /// </summary>
        /// <param name="cOBieSheet">COBieSheet of COBieResourceRow to read data from</param>
        public void SerialiseResource(COBieSheet<COBieResourceRow> cOBieSheet)
        {

            using (Transaction trans = Model.BeginTransaction("Add Resource"))
            {
                try
                {
                    //IfcTypeObjects = Model.InstancesOfType<IfcTypeObject>();

                    ProgressIndicator.ReportMessage("Starting Resources...");
                    ProgressIndicator.Initialise("Creating Resources", cOBieSheet.RowCount);
                    for (int i = 0; i < cOBieSheet.RowCount; i++)
                    {
                        ProgressIndicator.IncrementAndUpdate();
                        COBieResourceRow row = cOBieSheet[i];
                        AddResource(row);
                    }
                    ProgressIndicator.Finalise();
                    trans.Commit();
                }
                catch (Exception)
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Add the data to the IfcConstructionEquipmentResource object
        /// </summary>
        /// <param name="row">COBieResourceRow holding the data</param>
        private void AddResource(COBieResourceRow row)
        {
            //we are merging so check for an existing item name, assume the same item as should be the same building
            if (CheckIfExistOnMerge<IfcConstructionEquipmentResource>(row.Name))
            {
                return;//we have it so no need to create
            }
            IfcConstructionEquipmentResource ifcConstructionEquipmentResource = Model.New<IfcConstructionEquipmentResource>();
            //Add Created By, Created On and ExtSystem to Owner History. 
            if ((ValidateString(row.CreatedBy)) && (Contacts.ContainsKey(row.CreatedBy)))
                SetNewOwnerHistory(ifcConstructionEquipmentResource, row.ExtSystem, Contacts[row.CreatedBy], row.CreatedOn);
            else
                SetNewOwnerHistory(ifcConstructionEquipmentResource, row.ExtSystem, Model.DefaultOwningUser, row.CreatedOn);

            //using statement will set the Model.OwnerHistoryAddObject to ifcConstructionEquipmentResource.OwnerHistory as OwnerHistoryAddObject is used upon any property changes, 
            //then swaps the original OwnerHistoryAddObject back in the dispose, so set any properties within the using statement
            using (COBieXBimEditScope context = new COBieXBimEditScope(Model, ifcConstructionEquipmentResource.OwnerHistory))
            {
                //Add Name
                if (ValidateString(row.Name)) ifcConstructionEquipmentResource.Name = row.Name;

                //Add Category
                if (ValidateString(row.Category)) ifcConstructionEquipmentResource.ObjectType = row.Category;

                //Add GlobalId
                AddGlobalId(row.ExtIdentifier, ifcConstructionEquipmentResource);

                //add Description
                if (ValidateString(row.Description)) ifcConstructionEquipmentResource.Description = row.Description;
            }
        }
        
    }
}
