using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.Rows;
using Xbim.XbimExtensions.Transactions;
using Xbim.Ifc.Extensions;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.PropertyResource;
using Xbim.Ifc.SelectTypes;
using Xbim.Ifc.UtilityResource;
using Xbim.Ifc.ExternalReferenceResource;

namespace Xbim.COBie.Serialisers.XbimSerialiser
{
    public class COBieXBimAttribute : COBieXBim
    {

        #region Properties
        public IEnumerable<IfcBuildingStorey> IfcBuildingStoreys { get; private set; }
        public IEnumerable<IfcSpace> IfcSpaces { get; private set; }
        public IEnumerable<IfcTypeObject> IfcTypeObjects { get; private set; }
        public IEnumerable<IfcElement> IfcElements { get; private set; }
        public IEnumerable<IfcZone> IfcZones { get; private set; }
        public IEnumerable<IfcBuilding> IfcBuildings { get; private set; }
        private IfcObjectDefinition CurrentObject { get; set; }
        #endregion

        public COBieXBimAttribute(COBieXBimContext xBimContext)
            : base(xBimContext)
        {
            CurrentObject = null;
            
        }

        #region Methods

        /// <summary>
        /// Add Properties back to component and type objects
        /// </summary>
        /// <param name="cOBieSheet">COBieSheet of COBieAttributeRow to read data from</param>
        public void SerialiseAttribute(COBieSheet<COBieAttributeRow> cOBieSheet)
        {
            using (Transaction trans = Model.BeginTransaction("Add Attribute"))
            {
                try
                {
                    var sortedRows =  cOBieSheet.Rows.OrderBy(r => r.SheetName).ThenBy(r => r.RowName);
                    ProgressIndicator.ReportMessage("Starting Attributes...");
                    ProgressIndicator.Initialise("Creating Attributes", cOBieSheet.RowCount);
                    foreach (COBieAttributeRow row in sortedRows)
                    {
                        ProgressIndicator.IncrementAndUpdate();
                        AddAttribute(row);
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
        /// Add the properties to the row object
        /// </summary>
        /// <param name="row">COBieAttributeRow holding the data</param>
        private void AddAttribute(COBieAttributeRow row)
        {
            //need a sheet and a row to be able to attach property to an object
            if ((ValidateString(row.RowName)) && (ValidateString(row.SheetName)))
            {
                switch (row.SheetName.ToLower())
                {
                    case "facility":
                        //set list if first time
                        if (IfcBuildings == null) IfcBuildings = Model.InstancesOfType<IfcBuilding>();
                        if (!((CurrentObject is IfcBuilding) && (CurrentObject.Name == row.RowName)))
                            CurrentObject = IfcBuildings.Where(b => b.Name.ToString().ToLower() == row.RowName.ToLower()).FirstOrDefault();
                        break;
                    case "floor":
                        if (IfcBuildingStoreys == null) IfcBuildingStoreys = Model.InstancesOfType<IfcBuildingStorey>();
                        if (!((CurrentObject is IfcBuildingStorey) && (CurrentObject.Name == row.RowName)))
                            CurrentObject = IfcBuildingStoreys.Where(b => b.Name.ToString().ToLower() == row.RowName.ToLower()).FirstOrDefault();
                        break;
                    case "space":
                        if (IfcSpaces == null) IfcSpaces = Model.InstancesOfType<IfcSpace>();
                        if (!((CurrentObject is IfcSpace) && (CurrentObject.Name == row.RowName)))
                            CurrentObject = IfcSpaces.Where(b => b.Name.ToString().ToLower() == row.RowName.ToLower()).FirstOrDefault();
                        break;
                    case "type":
                        if (IfcTypeObjects == null) IfcTypeObjects = Model.InstancesOfType<IfcTypeObject>();
                        if (!((CurrentObject is IfcTypeObject) && (CurrentObject.Name == row.RowName)))
                            CurrentObject = IfcTypeObjects.Where(b => b.Name.ToString().ToLower() == row.RowName.ToLower()).FirstOrDefault();
                        break;
                    case "component":
                        if (IfcElements == null) IfcElements = Model.InstancesOfType<IfcElement>();
                        if (!((CurrentObject is IfcElement) && (CurrentObject.Name == row.RowName)))
                            CurrentObject = IfcElements.Where(b => b.Name.ToString().ToLower() == row.RowName.ToLower()).FirstOrDefault();
                        break;
                    case "zone":
                        if (IfcZones == null) IfcZones = Model.InstancesOfType<IfcZone>();
                        if (!((CurrentObject is IfcZone) && (CurrentObject.Name == row.RowName)))
                            CurrentObject = IfcZones.Where(b => b.Name.ToString().ToLower() == row.RowName.ToLower()).FirstOrDefault();
                        break;
                    default:
                        break;
                }
               
                if (CurrentObject != null)
                {
                    string pSetName = "";
                    if ((ValidateString(row.Name)) &&
                        (!string.IsNullOrEmpty(row.Value)) &&
                        (!string.IsNullOrEmpty(row.ExtObject))
                        )
                    {

                        pSetName = row.ExtObject;
                        if (!pSetName.Contains("PSet_")) pSetName = "PSet_" + pSetName;
                        IfcPropertySet ifcPropertySet = null;

                        if (CurrentObject is IfcObject)
                            ifcPropertySet = AddPropertySet((IfcObject)CurrentObject, pSetName, "");
                        else if (CurrentObject is IfcTypeObject)
                            ifcPropertySet = AddPropertySet((IfcTypeObject)CurrentObject, pSetName, "");
                        
                        //set the unit used for property
                        IfcUnit ifcUnit = null;
                        if (ValidateString(row.Unit))
                        {
                            ifcUnit = GetDurationUnit(row.Unit); //see if time unit
                            //see if we can convert to a IfcSIUnit
                            if (ifcUnit == null) 
                                ifcUnit = GetSIUnit(row.Unit);
                            //OK set as a user defined
                            if (ifcUnit == null)
                                ifcUnit = SetContextDependentUnit(row.Unit);
                        }

                        if ((ValidateString(row.AllowedValues)) &&
                            (row.AllowedValues.Contains(":") ||
                            row.AllowedValues.Contains(",")
                            )
                            )//have a IfcPropertyEnumeratedValue
                        {
                            IfcValue[] ifcValues = GetValueArray(row.Value);
                            IfcValue[] ifcValueEnums = GetValueArray(row.AllowedValues);
                            AddPropertyEnumeratedValue(ifcPropertySet, row.Name, "", ifcValues, ifcValueEnums, ifcUnit);
                        }
                        else
                        {
                            IfcValue ifcValue;
                            double number;
                            if (double.TryParse(row.Value, out number))
                                ifcValue = new IfcReal((double)number);
                            else
                                ifcValue = new IfcLabel(row.Value);
                            AddPropertySingleValue(ifcPropertySet, row.Name, "", ifcValue, ifcUnit);
                        }

                        //Add Category****
                        if (ValidateString(row.Category))
                        {
                            SetCategory(ifcPropertySet, row.Category);
                        }

                        //****************Note need this as last call Add OwnerHistory*************
                        if (ifcPropertySet != null) 
                        {
                            if ((ValidateString(row.CreatedBy)) && (Contacts.ContainsKey(row.CreatedBy)))
                            {
                                SetOwnerHistory(ifcPropertySet, row.ExtSystem, Contacts[row.CreatedBy], row.CreatedOn);
                            }
                        }
                        //****************Note need SetOwnerHistory above to be last call, as XBim changes to default on any property set or changed, cannot use edit context as property set used more than once per row******
                    }
                    
                }
                
            }
            
        }

        

        /// <summary>
        /// Set Category to the property set
        /// </summary>
        /// <param name="ifcRoot">IfcRoot Object (IfcPropertySet)</param>
        /// <param name="category">string, category Name</param>
        private void SetCategory(IfcRoot ifcRoot, string category)
        {
            IfcRelAssociatesClassification ifcRelAssociatesClassification = Model.InstancesWhere<IfcRelAssociatesClassification>(r => (r.RelatingClassification is IfcClassificationReference) && ((IfcClassificationReference)r.RelatingClassification).Name.ToString().ToLower() == category.ToLower()).FirstOrDefault();
            //create if none found
            if (ifcRelAssociatesClassification == null)
            {
                ifcRelAssociatesClassification = Model.New<IfcRelAssociatesClassification>();
                IfcClassificationReference ifcClassificationReference = Model.New<IfcClassificationReference>();
                ifcClassificationReference.Name = category;
                ifcRelAssociatesClassification.RelatingClassification = ifcClassificationReference;
            }
            //add this IfcRoot object if not already associated
            if (!ifcRelAssociatesClassification.RelatedObjects.Contains(ifcRoot))
            {
                ifcRelAssociatesClassification.RelatedObjects.Add_Reversible(ifcRoot);
            }
            
        }
        #endregion
    }
}
