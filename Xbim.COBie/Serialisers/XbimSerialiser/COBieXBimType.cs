using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.Rows;
using Xbim.XbimExtensions.Transactions;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.PropertyResource;
using Xbim.XbimExtensions;
using System.Reflection;
using Xbim.Ifc.MaterialResource;
using Xbim.Ifc.SharedBldgServiceElements;

namespace Xbim.COBie.Serialisers.XbimSerialiser
{
    public class COBieXBimType : COBieXBim
    {
        public COBieXBimType(COBieXBimContext xBimContext)
            : base(xBimContext)
        {
           
        }

        #region Methods
        /// <summary>
        /// Create and setup objects held in the Type COBieSheet
        /// </summary>
        /// <param name="cOBieSheet">COBieSheet of COBieTypeRow to read data from</param>
        public void SerialiseType(COBieSheet<COBieTypeRow> cOBieSheet)
        {
            using (Transaction trans = Model.BeginTransaction("Add Type"))
            {
                try
                {
                    ProgressIndicator.ReportMessage("Starting Types...");
                    ProgressIndicator.Initialise("Creating Types", cOBieSheet.RowCount);
                    for (int i = 0; i < cOBieSheet.RowCount; i++)
                    {
                        ProgressIndicator.IncrementAndUpdate();
                        COBieTypeRow row = cOBieSheet[i];
                        if ((ValidateString(row.ExtObject)) &&
                            (row.ExtObject.ToLower().Contains("ifcmaterial"))
                            )
                            AddMaterial(row);
                        else
                            AddType(row);
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
        /// Add the data to the Material object
        /// </summary>
        /// <param name="row">COBieTypeRow holding the data</param>
        
        private void AddMaterial(COBieTypeRow row)
        {
            
            if (ValidateString(row.Name))
            {
                IfcMaterial ifcMaterial = null;
                //we will skip over the IfcMaterialLayerSets and allow the assembly sheet to create them
                if ((row.ExtObject.ToLower() == "ifcmaterial") ||
                    (row.ExtObject.ToLower() == "ifcmateriallayer")
                    )
                {
                    string name = GetMaterialName(row.Name);
                    ifcMaterial = Model.InstancesWhere<IfcMaterial>(m => m.Name.ToString().ToLower() == name.ToLower()).FirstOrDefault();
                    if (ifcMaterial == null)
                        ifcMaterial = Model.New<IfcMaterial>(m => { m.Name = name; });
                }
                if ((ifcMaterial != null) && (row.ExtObject.ToLower() == "ifcmateriallayer"))
                {
                    double matThick = 0.0;
                    if ((ValidateString(row.NominalWidth)) &&
                        (!double.TryParse(row.NominalWidth, out matThick))
                        )
                        matThick = 0.0;

                    IfcMaterialLayer ifcMaterialLayer = Model.New<IfcMaterialLayer>(ml => { ml.Material = ifcMaterial; ml.LayerThickness = matThick; });
                } 
            }
        }

        /// <summary>
        /// Add the data to the Type object
        /// </summary>
        /// <param name="row">COBieTypeRow holding the data</param>
        private void AddType(COBieTypeRow row)
        {
            //IfcTypeObject ifcTypeObject = Model.New<IfcTypeObject>();
            IfcTypeObject ifcTypeObject = GetTypeInstance(row.ExtObject, Model);

            if (ifcTypeObject != null)
            {
                //Add Created By, Created On and ExtSystem to Owner History
                if ((ValidateString(row.CreatedBy)) && (Contacts.ContainsKey(row.CreatedBy)))
                    SetNewOwnerHistory(ifcTypeObject, row.ExtSystem, Contacts[row.CreatedBy], row.CreatedOn);
                else
                    SetNewOwnerHistory(ifcTypeObject, row.ExtSystem, Model.DefaultOwningUser, row.CreatedOn);

                //using statement will set the Model.OwnerHistoryAddObject to ifcTypeObject.OwnerHistory as OwnerHistoryAddObject is used upon any property changes, 
                //then swaps the original OwnerHistoryAddObject back in the dispose, so set any properties within the using statement
                using (COBieXBimEditScope context = new COBieXBimEditScope(Model, ifcTypeObject.OwnerHistory))
                {
                    string name = row.Name;
                    //Add Name
                    if (ValidateString(row.Name)) ifcTypeObject.Name = row.Name;

                    //Add Category
                    AddCategory(row.Category, ifcTypeObject);

                    //Add GlobalId
                    AddGlobalId(row.ExtIdentifier, ifcTypeObject);

                    //Add Description
                    if (ValidateString(row.Description)) ifcTypeObject.Description = row.Description;

                    if (ValidateString(row.AssetType))
                        AddPropertySingleValue(ifcTypeObject, "Pset_Asset", "Type Asset Fixed or Movable Properties From COBie", "AssetAccountingType", "Asset Type Fixed or Movable", new IfcLabel(row.AssetType));
                    if (ValidateString(row.Manufacturer))
                        AddPropertySingleValue(ifcTypeObject, "Pset_ManufacturersTypeInformation", "Manufacturers Properties From COBie", "Manufacturer", "Manufacturer Contact for " + name, new IfcLabel(row.Manufacturer));
                    if (ValidateString(row.ModelNumber))
                        AddPropertySingleValue(ifcTypeObject, "Pset_ManufacturersTypeInformation", null, "ModelLabel", "Model Number for " + name, new IfcLabel(row.ModelNumber));
                    if (ValidateString(row.WarrantyGuarantorParts))
                        AddPropertySingleValue(ifcTypeObject, "Pset_Warranty", "Warranty Information", "WarrantyGuarantorParts", "Warranty Contact for " + name, new IfcLabel(row.WarrantyGuarantorParts));
                    if (ValidateString(row.WarrantyDurationParts))
                        AddPropertySingleValue(ifcTypeObject, "Pset_Warranty", null, "WarrantyDurationParts", "Warranty length for " + name, new IfcLabel(row.WarrantyDurationParts));
                    if (ValidateString(row.WarrantyGuarantorLabor))
                        AddPropertySingleValue(ifcTypeObject, "Pset_Warranty", null, "WarrantyGuarantorLabor", "Warranty Labour Contact for " + name, new IfcLabel(row.WarrantyGuarantorLabor));
                    if (ValidateString(row.WarrantyDescription))
                        AddPropertySingleValue(ifcTypeObject, "Pset_Warranty", null, "WarrantyDescription", "Warranty Description for" + name, new IfcLabel(row.WarrantyDescription));

                    if (ValidateString(row.WarrantyDurationLabor))
                    {
                        IfcPropertySingleValue ifcPropertySingleValue = AddPropertySingleValue(ifcTypeObject, "Pset_Warranty", null, "WarrantyDurationLabor", "Labour Warranty length for " + name, new IfcLabel(row.WarrantyDurationLabor));
                        //WarrantyDurationUnit
                        if (ValidateString(row.WarrantyDurationUnit))
                            ifcPropertySingleValue.Unit = GetDurationUnit(row.WarrantyDurationUnit);
                    }

                    if (ValidateString(row.ReplacementCost))
                    {
                        double? value = GetDoubleFromString(row.ReplacementCost);
                        if (value != null)
                            AddPropertySingleValue(ifcTypeObject, "Pset_EconomicImpactValues", "Economic Impact Values", "ReplacementCost", "Replacement Cost for" + name, new IfcReal((double)value));
                    }
                    if (ValidateString(row.ExpectedLife))
                    {
                        IfcPropertySingleValue ifcPropertySingleValue = AddPropertySingleValue(ifcTypeObject, "Pset_ServiceLife", "Service Life", "ServiceLifeDuration", "Service Life length for " + name, new IfcLabel(row.ExpectedLife));
                        if (ValidateString(row.DurationUnit))
                            ifcPropertySingleValue.Unit = GetDurationUnit(row.DurationUnit);
                    }

                    if (ValidateString(row.NominalLength))
                    {
                        double? value = GetDoubleFromString(row.NominalLength);
                        if (value != null)
                            AddPropertySingleValue(ifcTypeObject, "Pset_Specification", "Specification Properties", "NominalLength", "Nominal Length Value for " + name, new IfcReal((double)value));
                    }
                    if (ValidateString(row.NominalWidth))
                    {
                        double? value = GetDoubleFromString(row.NominalWidth);
                        if (value != null)
                            AddPropertySingleValue(ifcTypeObject, "Pset_Specification", null, "NominalWidth", "Nominal Width Value for " + name, new IfcReal((double)value));
                    }
                    if (ValidateString(row.NominalHeight))
                    {
                        double? value = GetDoubleFromString(row.NominalHeight);
                        if (value != null)
                            AddPropertySingleValue(ifcTypeObject, "Pset_Specification", null, "NominalHeight", "Nominal Height Value for " + name, new IfcReal((double)value));
                    }

                    if (ValidateString(row.ModelReference))
                        AddPropertySingleValue(ifcTypeObject, "Pset_Specification", null, "ModelReference", "Model Reference Value for " + name, new IfcLabel(row.ModelReference));

                    if (ValidateString(row.Shape))
                        AddPropertySingleValue(ifcTypeObject, "Pset_Specification", null, "Shape", "Shape Value for " + name, new IfcLabel(row.Shape));

                    if (ValidateString(row.Size))
                        AddPropertySingleValue(ifcTypeObject, "Pset_Specification", null, "Size", "Size Value for " + name, new IfcLabel(row.Size));

                    if (ValidateString(row.Color))
                        AddPropertySingleValue(ifcTypeObject, "Pset_Specification", null, "Color", "Color Value for " + name, new IfcLabel(row.Color));

                    if (ValidateString(row.Finish))
                        AddPropertySingleValue(ifcTypeObject, "Pset_Specification", null, "Finish", "Finish Value for " + name, new IfcLabel(row.Finish));

                    if (ValidateString(row.Grade))
                        AddPropertySingleValue(ifcTypeObject, "Pset_Specification", null, "Grade", "Grade Value for " + name, new IfcLabel(row.Grade));

                    if (ValidateString(row.Material))
                        AddPropertySingleValue(ifcTypeObject, "Pset_Specification", null, "Material", "Material Value for " + name, new IfcLabel(row.Material));

                    if (ValidateString(row.Constituents))
                        AddPropertySingleValue(ifcTypeObject, "Pset_Specification", null, "Constituents", "Constituents Value for " + name, new IfcLabel(row.Constituents));

                    if (ValidateString(row.Features))
                        AddPropertySingleValue(ifcTypeObject, "Pset_Specification", null, "Features", "Features Value for " + name, new IfcLabel(row.Features));

                    if (ValidateString(row.AccessibilityPerformance))
                        AddPropertySingleValue(ifcTypeObject, "Pset_Specification", null, "AccessibilityPerformance", "Accessibility Performance Value for " + name, new IfcLabel(row.AccessibilityPerformance));

                    if (ValidateString(row.CodePerformance))
                        AddPropertySingleValue(ifcTypeObject, "Pset_Specification", null, "CodePerformance", "Code Performance Value for " + name, new IfcLabel(row.CodePerformance));

                    if (ValidateString(row.SustainabilityPerformance))
                        AddPropertySingleValue(ifcTypeObject, "Pset_Specification", null, "SustainabilityPerformance", "Sustainability Performance Value for " + name, new IfcLabel(row.SustainabilityPerformance));


                }
            }
            else
            {
#if DEBUG
                Console.WriteLine("Failed to create type {0} of {1}", row.Name, row.ExtObject);
#endif
            }


        }

        /// <summary>
        /// Create an Instance of a Object Type
        /// </summary>
        /// <param name="typeName">string name to create instance of</param>
        /// <param name="model">Model object</param>
        /// <returns>IfcTypeObject object of the type passed in of IfcTypeObject if failed to create passed in type</returns>
        public static IfcTypeObject GetTypeInstance(string typeName, IModel model)
        {
            typeName = typeName.Trim().ToUpper();
            
            IfcType ifcType;
            IfcTypeObject ifcTypeObject = null;
            if (IfcInstances.IfcTypeLookup.TryGetValue(typeName, out ifcType))
            {
                MethodInfo method = typeof(IModel).GetMethod("New", Type.EmptyTypes);
                MethodInfo generic = method.MakeGenericMethod(ifcType.Type);
                var newObj = generic.Invoke(model, null);
                if (newObj is IfcTypeObject)
                    ifcTypeObject = (IfcTypeObject)newObj;
            }
            if (ifcTypeObject == null) //if we cannot make a object assume base IfcTypeObject
                ifcTypeObject = model.New<IfcTypeObject>();
            return ifcTypeObject;
        }

        
        #endregion

    }
}
