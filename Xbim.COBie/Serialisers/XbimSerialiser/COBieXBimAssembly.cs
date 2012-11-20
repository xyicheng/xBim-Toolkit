using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.Rows;
using Xbim.XbimExtensions.Transactions;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.MaterialResource;

namespace Xbim.COBie.Serialisers.XbimSerialiser
{
    public class COBieXBimAssembly : COBieXBim
    {
        #region Fields
        private  IEnumerable<IfcElement> IfcElements { get; set; }
        private IEnumerable<IfcTypeObject> IfcTypeObjects { get; set; }
        private IEnumerable<IfcMaterialLayer> IfcMaterialLayers { get; set; }
        private IfcRelDecomposes LastIfcRelDecomposes { get; set; }
        private IfcMaterialLayerSet LastIfcMaterialLayerSet { get; set; }
        private COBieAssemblyRow LastRow { get; set; }
        #endregion
        
        public COBieXBimAssembly(COBieXBimContext xBimContext)
            : base(xBimContext)
        {
            
        }

        /// <summary>
        /// Add the IfcPersonAndOrganizations to the Model object
        /// </summary>
        /// <param name="cOBieSheet"></param>
        public void SerialiseAssembly(COBieSheet<COBieAssemblyRow> cOBieSheet)
        {

            using (Transaction trans = Model.BeginTransaction("Add Assembly"))
            {
                try
                {
                    IfcElements = Model.InstancesOfType<IfcElement>();
                    IfcTypeObjects = Model.InstancesOfType<IfcTypeObject>();
                    IfcMaterialLayers = Model.InstancesOfType<IfcMaterialLayer>();

                    ProgressIndicator.ReportMessage("Starting Assemblies...");
                    ProgressIndicator.Initialise("Creating Assemblies", cOBieSheet.RowCount);
                    for (int i = 0; i < cOBieSheet.RowCount; i++)
                    {
                        ProgressIndicator.IncrementAndUpdate();
                        COBieAssemblyRow row = cOBieSheet[i];
                        string objType = row.ExtObject.ToLower().Trim();
                        if ((objType == "ifcrelaggregates") ||
                            (objType == "ifcrelnests")
                            )
                            AddAssembly(row);
                        else
                            AddMaterial(row);
                        
                        LastRow = row;
                        

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
        /// Add the data to the IfcMaterialLayerSet object
        /// </summary>
        /// <param name="row">COBieAssemblyRow holding the data</param>
        private void AddMaterial(COBieAssemblyRow row)
        {
            //check we have a chance of creating the IfcMaterialLayerSet object
            if ((ValidateString(row.ParentName)) && (ValidateString(row.ChildNames)))
            {
                IfcMaterialLayerSet ifcMaterialLayerSet = null;
                IfcMaterialLayerSetUsage ifcMaterialLayerSetUsage = null;
                IfcRelAssociatesMaterial ifcRelAssociatesMaterial = null;
                IfcBuildingElementProxy ifcBuildingElementProxy = null;

                if ((LastIfcMaterialLayerSet != null) && IsContinuedRow(row)) //this row line is a continuation of objects from the line above
                    ifcMaterialLayerSet = LastIfcMaterialLayerSet;
                else
                {
                    ifcMaterialLayerSet = Model.New<IfcMaterialLayerSet>(mls => { mls.LayerSetName = row.ParentName; });
                    ifcMaterialLayerSetUsage = Model.New<IfcMaterialLayerSetUsage>(mlsu => { mlsu.ForLayerSet = ifcMaterialLayerSet; });
                    ifcBuildingElementProxy = Model.New<IfcBuildingElementProxy>(bep => { bep.Name = ("Place holder for material layer Set " + row.ParentName); });
                    ifcRelAssociatesMaterial = Model.New<IfcRelAssociatesMaterial>( ras => { ras.RelatingMaterial = ifcMaterialLayerSetUsage;
                                                                                    ras.RelatedObjects.Add_Reversible(ifcBuildingElementProxy);
                                                                                    });

                    //Add Created By, Created On and ExtSystem to Owner History. 
                    if ((ValidateString(row.CreatedBy)) && (Contacts.ContainsKey(row.CreatedBy)))
                        SetNewOwnerHistory(ifcRelAssociatesMaterial, row.ExtSystem, Contacts[row.CreatedBy], row.CreatedOn);
                    else
                        SetNewOwnerHistory(ifcRelAssociatesMaterial, row.ExtSystem, Model.DefaultOwningUser, row.CreatedOn);
                }

                //add the child objects
                AddChildObjects(ifcMaterialLayerSet, row.ChildNames);
            }
        }

        /// <summary>
        /// Add the data to the IfcRelDecomposes object
        /// </summary>
        /// <param name="row">COBieAssemblyRow holding the data</param>
        private void AddAssembly(COBieAssemblyRow row)
        {
            //check we have a chance of creating the IfcRelDecomposes object
            if ((ValidateString(row.ParentName)) && (ValidateString(row.ChildNames)))
            {
                IfcRelDecomposes ifcRelDecomposes = null;

                if ((LastIfcRelDecomposes != null) && IsContinuedRow(row)) //this row line is a continuation of objects from the line above
                {
                    ifcRelDecomposes = LastIfcRelDecomposes;
                }
                else
                {
                    if (row.ExtObject.ToLower().Trim() == "ifcrelaggregates")
                        ifcRelDecomposes = Model.New<IfcRelAggregates>();
                    else
                        ifcRelDecomposes = Model.New<IfcRelNests>();
                    

                    //Add Created By, Created On and ExtSystem to Owner History. 
                    if ((ValidateString(row.CreatedBy)) && (Contacts.ContainsKey(row.CreatedBy)))
                        SetNewOwnerHistory(ifcRelDecomposes, row.ExtSystem, Contacts[row.CreatedBy], row.CreatedOn);
                    else
                        SetNewOwnerHistory(ifcRelDecomposes, row.ExtSystem, Model.DefaultOwningUser, row.CreatedOn);
                }


                //using statement will set the Model.OwnerHistoryAddObject to IfcConstructionProductResource.OwnerHistory as OwnerHistoryAddObject is used upon any property changes, 
                //then swaps the original OwnerHistoryAddObject back in the dispose, so set any properties within the using statement
                using (COBieXBimEditScope context = new COBieXBimEditScope(Model, ifcRelDecomposes.OwnerHistory))
                {
                    if (ValidateString(row.Name)) ifcRelDecomposes.Name = row.Name;
                    if (ValidateString(row.Description)) ifcRelDecomposes.Description = row.Description;
                    if (! (AddParentObject(ifcRelDecomposes, row.ParentName) &&
                           AddChildObjects(ifcRelDecomposes, row.SheetName, row.ChildNames)
                           )
                        )
                    {
                        //failed to add parent or child so remove as not a valid IfcRelDecomposes object
		                Model.Delete(ifcRelDecomposes);
                        ifcRelDecomposes = null;
                    }
                    
                }
                //save for next row, might be a continuation line
                LastIfcRelDecomposes = ifcRelDecomposes;
            }
        }

        private bool IsContinuedRow(COBieAssemblyRow row)
        {
            if (ValidateString(row.Name))
            {
		        if (row.Name.Contains(" : continued ") )
                    return true;

                if (ValidateString(row.ChildNames))
                {
                    string name = row.Name.ToLower().Trim();
                    string lastname = LastRow.Name.ToLower().Trim();
                
                    List<string> splitChildNames = GetChildNames(row.ChildNames);
                    //if the row name holds the same string as the last row within it, and the child is a single entry, then we can assume it is lightly row be a continuation on the assembly/MaterialSet used in the last row
                    if ((name.Contains(lastname)) && (splitChildNames.Count() == 1)) 
                        return true;
                }
            }
            return false;
        }

        

        /// <summary>
        /// Add the parent objects to the IfcRelDecomposes
        /// </summary>
        /// <param name="ifcRelDecomposes">Either a IfcRelAggregates or IfcRelNests object</param>
        /// <param name="parentName">IfcObjectDefinition.Name value to search for, NOT case sensitive</param>
        /// <returns></returns>
        private bool AddParentObject(IfcRelDecomposes ifcRelDecomposes, string parentName)
        {
            string name = parentName.ToLower().Trim();
            IfcObjectDefinition RelatingObject = IfcElements.Where(obj => obj.Name.ToString().ToLower().Trim() == name).FirstOrDefault();
            if(RelatingObject == null) //try IfcTypeObjects
                RelatingObject = IfcTypeObjects.Where(obj => obj.Name.ToString().ToLower().Trim() == name).FirstOrDefault();

            if (RelatingObject != null)
            {
                ifcRelDecomposes.RelatingObject = RelatingObject;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Add the child objects to the IfcRelDecomposes
        /// </summary>
        /// <param name="ifcRelDecomposes">Either a IfcRelAggregates or IfcRelNests object</param>
        /// <param name="sheetName">SheetName the children come from</param>
        /// <param name="childNames">list of child object names separated by " : ", NOT case sensitive</param>
        private bool AddChildObjects(IfcRelDecomposes ifcRelDecomposes, string sheetName, string childNames)
        {
            bool returnValue = false;
            IEnumerable<IfcObjectDefinition> childObjs;
            if (sheetName.ToLower() == Constants.WORKSHEET_COMPONENT.ToLower()) 
                childObjs = IfcElements;
            else //if not components then it should by Type
                childObjs = IfcTypeObjects;

            List<string> splitChildNames = GetChildNames(childNames);
            
            foreach (string item in splitChildNames)
            {
                string name = item.ToLower().Trim();
                IfcObjectDefinition RelatedObject = childObjs.Where(obj => obj.Name.ToString().ToLower().Trim() == name).FirstOrDefault();
                if (RelatedObject != null)
                {
                    ifcRelDecomposes.RelatedObjects.Add_Reversible(RelatedObject);
                    returnValue = true;
                }
            }
            return returnValue;
        }

        /// <summary>
        /// Split a string via a delimiting ',' or ':'
        /// </summary>
        /// <param name="childNames">string to split</param>
        /// <returns>string[] split string list</returns>
        private static List<string> GetChildNames(string childNames)
        {
            char splitKey = ',';
            if (childNames.Contains(":")) 
                splitKey = ':';

            List<string> splitChildNames = SplitString(childNames, splitKey);
            return splitChildNames;
        }

        /// <summary>
        /// Add the child objects to the IfcMaterialLayerSet
        /// </summary>
        /// <param name="ifcMaterialLayerSet">IfcMaterialLayerSet object</param>
        /// <param name="childNames">list of child object names separated by " : ", NOT case sensitive</param>
        private bool AddChildObjects(IfcMaterialLayerSet ifcMaterialLayerSet, string childNames)
        {
            bool returnValue = false;
            List<string> splitChildNames = GetChildNames(childNames);

            foreach (string item in splitChildNames)
            {
                string name = item.ToLower().Trim();

                IfcMaterialLayer ifcMaterialLayer = IfcMaterialLayers.Where(ml => (ml.Material.Name != null) && (ml.Material.Name.ToString().ToLower().Trim() == name)).FirstOrDefault();
                if (ifcMaterialLayer != null)
                {
                    ifcMaterialLayerSet.MaterialLayers.Add_Reversible(ifcMaterialLayer);
                    returnValue = true;
                }
            }
            return returnValue;
        }

        
    }
}
