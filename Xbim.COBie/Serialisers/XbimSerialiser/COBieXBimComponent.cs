using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.Rows;
using Xbim.XbimExtensions.Transactions;
using Xbim.Ifc.Kernel;
using Xbim.XbimExtensions;
using System.Reflection;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.Extensions;

namespace Xbim.COBie.Serialisers.XbimSerialiser
{
    public class COBieXBimComponent : COBieXBim
    {

        #region Properties
        public  IEnumerable<IfcTypeObject> IfcTypeObjects { get; private set; }
        public  IEnumerable<IfcSpace> IfcSpaces { get; private set; }
        public IEnumerable<IfcBuildingStorey> IfcBuildingStoreys { get; private set; }
        
        #endregion

        public COBieXBimComponent(COBieXBimContext xBimContext)
            : base(xBimContext)
        {

        }

        #region Methods
        /// <summary>
        /// Create and setup objects held in the Component COBieSheet
        /// </summary>
        /// <param name="cOBieSheet">COBieSheet of COBieComponentRow to read data from</param>
        public void SerialiseComponent(COBieSheet<COBieComponentRow> cOBieSheet)
        {
            using (Transaction trans = Model.BeginTransaction("Add Component"))
            {

                try
                {
                    IfcTypeObjects = Model.InstancesOfType<IfcTypeObject>();
                    IfcSpaces = Model.InstancesOfType<IfcSpace>();
                    IfcBuildingStoreys = Model.InstancesOfType<IfcBuildingStorey>();
                    ProgressIndicator.ReportMessage("Starting Components...");
                    ProgressIndicator.Initialise("Creating Components", cOBieSheet.RowCount);
                    for (int i = 0; i < cOBieSheet.RowCount; i++)
                    {
                        ProgressIndicator.IncrementAndUpdate();
                        COBieComponentRow row = cOBieSheet[i]; 
                        AddComponent(row);
                    }

                    ProgressIndicator.Finalise();
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
        /// Add the components and fill with data from COBieComponentRow
        /// </summary>
        /// <param name="row">COBieComponentRow holding the data</param>
        private void AddComponent(COBieComponentRow row)
        {
           

            //we need the ExtObject to exist to create the object
            if (ValidateString(row.ExtObject))
            {
                //Create object using reflection
                IfcType ifcType;
                if (IfcInstances.IfcTypeLookup.TryGetValue(row.ExtObject.ToUpper(), out ifcType))
                {
                    MethodInfo method = typeof(IModel).GetMethod("New", Type.EmptyTypes);
                    MethodInfo generic = method.MakeGenericMethod(ifcType.Type);
                    var eleObj = generic.Invoke(Model, null);
                    if (eleObj is IfcElement)
                    {
                        IfcElement ifcElement = (IfcElement)eleObj;

                        //Add Created By, Created On and ExtSystem to Owner History. 
                        if ((ValidateString(row.CreatedBy)) && (Contacts.ContainsKey(row.CreatedBy)))
                            SetNewOwnerHistory(ifcElement, row.ExtSystem, Contacts[row.CreatedBy], row.CreatedOn);
                        else
                            SetNewOwnerHistory(ifcElement, row.ExtSystem, Model.DefaultOwningUser, row.CreatedOn);
                        //using statement will set the Model.OwnerHistoryAddObject to ifcElement.OwnerHistory as OwnerHistoryAddObject is used upon any property changes, 
                        //then swaps the original OwnerHistoryAddObject back in the dispose, so set any properties within the using statement
                        using (COBieXBimEditScope context = new COBieXBimEditScope(Model, ifcElement.OwnerHistory))
                        {
                            //Add Name
                            string name = row.Name;
                            if (ValidateString(row.Name)) ifcElement.Name = row.Name;

                            //Add description
                            if (ValidateString(row.Description)) ifcElement.Description = row.Description;

                            //Add GlobalId
                            AddGlobalId(row.ExtIdentifier, ifcElement);

                            //Add Property Set Properties
                            if (ValidateString(row.SerialNumber))
                                AddPropertySingleValue(ifcElement, "Pset_Component", "Component Properties From COBie", "SerialNumber", "Serial Number for " + name, new IfcLabel(row.SerialNumber));
                            if (ValidateString(row.InstallationDate))
                                AddPropertySingleValue(ifcElement, "Pset_Component", null, "InstallationDate", "Installation Date for " + name, new IfcLabel(row.InstallationDate));
                            if (ValidateString(row.WarrantyStartDate))
                                AddPropertySingleValue(ifcElement, "Pset_Component", null, "WarrantyStartDate", "Warranty Start Date for " + name, new IfcLabel(row.WarrantyStartDate));
                            if (ValidateString(row.TagNumber))
                                AddPropertySingleValue(ifcElement, "Pset_Component", null, "TagNumber", "Tag Number for " + name, new IfcLabel(row.TagNumber));
                            if (ValidateString(row.BarCode))
                                AddPropertySingleValue(ifcElement, "Pset_Component", null, "BarCode", "Bar Code for " + name, new IfcLabel(row.BarCode));
                            if (ValidateString(row.AssetIdentifier))
                                AddPropertySingleValue(ifcElement, "Pset_Component", null, "AssetIdentifier", "Asset Identifier for " + name, new IfcLabel(row.AssetIdentifier));
                            //set up relationship of the component to the type the component is
                            if (ValidateString(row.TypeName))
                            {
                                IfcTypeObject ifcTypeObject = IfcTypeObjects.Where(to => to.Name.ToString().ToLower() == row.TypeName.ToLower()).FirstOrDefault();
                                if (ifcTypeObject != null)
                                    ifcElement.SetDefiningType(ifcTypeObject, Model);
                            }
                            //set up relationship of the component to the space
                            if (ValidateString(row.Space))
                            {
                                IfcSpace ifcSpace = IfcSpaces.Where(space => space.Name == row.Space).FirstOrDefault();
                                if (ifcSpace != null)
                                    ifcSpace.AddElement(ifcElement);
                                else //assume that it has a building association
                                {
                                    IfcBuildingStorey ifcBuildingStorey = IfcBuildingStoreys.Where(bs => bs.Name == row.Space).FirstOrDefault();
                                    if (ifcBuildingStorey != null)
                                        ifcBuildingStorey.AddElement(ifcElement);
                                    else
                                        GetBuilding().AddElement(ifcElement); //default to building, probably give incorrect bounding box as we do not know what the element parent was

                                }


                            }
                        }
                    }
                }
            
            }
        }
        #endregion
    }
}
