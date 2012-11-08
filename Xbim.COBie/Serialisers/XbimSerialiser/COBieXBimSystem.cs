using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.Rows;
using Xbim.XbimExtensions.Transactions;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.UtilityResource;
using Xbim.Ifc.Extensions;

namespace Xbim.COBie.Serialisers.XbimSerialiser
{
    public class COBieXBimSystem : COBieXBim
    {
        #region Properties
        public IfcSystem IfcSystemObj { get; set; }
        #endregion
        

        public COBieXBimSystem(COBieXBimContext xBimContext)
            : base(xBimContext)
        {
            IfcSystemObj = null;
        }

        #region Methods
        // <summary>
        /// Create and setup objects held in the System COBieSheet
        /// </summary>
        /// <param name="cOBieSheet">COBieSheet of COBieSystemRow to read data from</param>
        public void SerialiseSystem(COBieSheet<COBieSystemRow> cOBieSheet)
        {
            using (Transaction trans = Model.BeginTransaction("Add System"))
            {

                try
                {
                    
                    for (int i = 0; i < cOBieSheet.RowCount; i++)
                    {
                        COBieSystemRow row = cOBieSheet[i];
                        if (ValidateString(row.Name))
                        {
                            if ((IfcSystemObj == null) ||
                                (row.Name.ToLower() != IfcSystemObj.Name.ToString().ToLower())
                                )
                            {
                                AddSystem(row);
                                AddProduct(row);
                            }
                            else
                            {
                                AddProduct(row);
                            }
                        }
                    }

                    trans.Commit();

                }
                catch (Exception)
                {
                    trans.Rollback();
                    //TODO: Catch with logger?
                    throw;
                }
            }
        }

        /// <summary>
        /// Add system group and fill with data from COBieSystemRow
        /// </summary>
        /// <param name="row">COBieSystemRow holding the data</param>
        private void AddSystem(COBieSystemRow row)
        {
            IfcSystemObj = Model.New<IfcSystem>();
            IfcSystemObj.Name = row.Name;
            //Add Created By, Created On and ExtSystem to Owner History. 
            if ((ValidateString(row.CreatedBy)) && (Contacts.ContainsKey(row.CreatedBy)))
                SetNewOwnerHistory(IfcSystemObj, row.ExtSystem, Contacts[row.CreatedBy], row.CreatedOn);
            else
                SetNewOwnerHistory(IfcSystemObj, row.ExtSystem, Model.DefaultOwningUser, row.CreatedOn);
            //using statement will set the Model.OwnerHistoryAddObject to IfcSystemObj.OwnerHistory as OwnerHistoryAddObject is used upon any property changes, 
            //then swaps the original OwnerHistoryAddObject back in the dispose, so set any properties within the using statement
            using (COBieXBimEditScope context = new COBieXBimEditScope(Model, IfcSystemObj.OwnerHistory))
            {
                //Add Category
                AddCategory(row.Category, IfcSystemObj);
                //Add Description
                if (ValidateString(row.Description)) IfcSystemObj.Description = row.Description;
            }
            
        }

        /// <summary>
        /// Add products to system group and fill with data from COBieSystemRow
        /// </summary>
        /// <param name="row">COBieSystemRow holding the data</param>
        private void AddProduct(COBieSystemRow row)
        {
            IfcProduct ifcProduct = null;
            //find product via GlobalId
            if (ValidateString(row.ExtIdentifier))
            {
                IfcGloballyUniqueId id = new IfcGloballyUniqueId(row.ExtIdentifier);
                ifcProduct = Model.InstancesOfType<IfcProduct>().Where(p => p.GlobalId == id).FirstOrDefault(); 
            }
            //if we fail to get IfcProduct from GlobalId then try name
            if ((ifcProduct == null) && (ValidateString(row.Name)))
            {
               ifcProduct = Model.InstancesOfType<IfcProduct>().Where(p => p.Name.ToString().ToLower() == row.Name.ToLower()).FirstOrDefault(); 
            }

            //if we have found product then add to the IfcSystem group
            if (ifcProduct != null)
            {
                IfcSystemObj.AddObjectToGroup(ifcProduct);
            }
        }

        #endregion

    }
}
