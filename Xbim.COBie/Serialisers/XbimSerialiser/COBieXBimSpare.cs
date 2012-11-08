using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.Rows;
using Xbim.XbimExtensions.Transactions;
using Xbim.Ifc.ConstructionMgmtDomain;
using Xbim.Ifc.Extensions;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.MeasureResource;

namespace Xbim.COBie.Serialisers.XbimSerialiser
{
    public class COBieXBimSpare : COBieXBim
    {

        #region Properties
        public IEnumerable<IfcTypeObject> IfcTypeObjects { get; private set; }
        #endregion

        public COBieXBimSpare(COBieXBimContext xBimContext)
            : base(xBimContext)
        {

        }

        /// <summary>
        /// Add the IfcConstructionProductResource to the Model object
        /// </summary>
        /// <param name="cOBieSheet">COBieSheet of COBieSpareRow to read data from</param>
        public void SerialiseSpare(COBieSheet<COBieSpareRow> cOBieSheet)
        {

            using (Transaction trans = Model.BeginTransaction("Add Spare"))
            {
                try
                {
                    IfcTypeObjects = Model.InstancesOfType<IfcTypeObject>();
                    
                    for (int i = 0; i < cOBieSheet.RowCount; i++)
                    {
                        COBieSpareRow row = cOBieSheet[i];
                        AddSpare(row);
                    }
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
        /// Add the data to the IfcConstructionProductResource object
        /// </summary>
        /// <param name="row">COBieSpareRow holding the data</param>
        private void AddSpare(COBieSpareRow row)
        {
            IfcConstructionProductResource ifcConstructionProductResource = Model.New<IfcConstructionProductResource>();
            //Add Created By, Created On and ExtSystem to Owner History. 
            if ((ValidateString(row.CreatedBy)) && (Contacts.ContainsKey(row.CreatedBy)))
                SetNewOwnerHistory(ifcConstructionProductResource, row.ExtSystem, Contacts[row.CreatedBy], row.CreatedOn);
            else
                SetNewOwnerHistory(ifcConstructionProductResource, row.ExtSystem, Model.DefaultOwningUser, row.CreatedOn);

            //using statement will set the Model.OwnerHistoryAddObject to IfcConstructionProductResource.OwnerHistory as OwnerHistoryAddObject is used upon any property changes, 
            //then swaps the original OwnerHistoryAddObject back in the dispose, so set any properties within the using statement
            using (COBieXBimEditScope context = new COBieXBimEditScope(Model, ifcConstructionProductResource.OwnerHistory)) 
            {
                //Add Name
                if (ValidateString(row.Name)) ifcConstructionProductResource.Name = row.Name;

                //Add Category
                AddCategory(row.Category, ifcConstructionProductResource);

                //Add Type Relationship
                if (ValidateString(row.TypeName))
                {
                    List<string> typeNames = SplitString(row.TypeName);
                    IEnumerable<IfcTypeObject> ifcTypeObjects = IfcTypeObjects.Where(to => typeNames.Contains(to.Name.ToString().Trim()));
                    SetRelAssignsToResource(ifcConstructionProductResource, ifcTypeObjects);
                }
                //Add GlobalId
                AddGlobalId(row.ExtIdentifier, ifcConstructionProductResource);

                //add Description
                if (ValidateString(row.Description)) ifcConstructionProductResource.Description = row.Description;

                if (ValidateString(row.Suppliers))
                    AddPropertySingleValue(ifcConstructionProductResource, "Pset_Spare_COBie", "Spare Properties From COBie", "Suppliers", "Suppliers Contact Details", new IfcLabel(row.Suppliers));

                if (ValidateString(row.SetNumber))
                    AddPropertySingleValue(ifcConstructionProductResource, "Pset_Spare_COBie", null, "SetNumber", "Set Number", new IfcLabel(row.SetNumber));

                if (ValidateString(row.PartNumber))
                    AddPropertySingleValue(ifcConstructionProductResource, "Pset_Spare_COBie", null, "PartNumber", "Part Number", new IfcLabel(row.PartNumber));
                
            }
        }

        /// <summary>
        /// Create the relationships between the Resource and the types it relates too
        /// </summary>
        /// <param name="processObj">IfcResource Object</param>
        /// <param name="typeObjs">IEnumerable of IfcTypeObject, list of IfcTypeObjects </param>
        public void SetRelAssignsToResource(IfcResource resourceObj, IEnumerable<IfcTypeObject> typeObjs)
        {
            //find any existing relationships to this type
            IfcRelAssignsToResource processRel = Model.InstancesWhere<IfcRelAssignsToResource>(rd => rd.RelatingResource == resourceObj).FirstOrDefault();
            if (processRel == null) //none defined create the relationship
            {
                processRel = Model.New<IfcRelAssignsToResource>();
                processRel.RelatingResource = resourceObj;
                
                
            }
            //add the type objects
            foreach (IfcTypeObject type in typeObjs)
            {
                processRel.RelatedObjects.Add_Reversible(type);
            }
        }

    }


}
