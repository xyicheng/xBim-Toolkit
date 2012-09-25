using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xbim.COBie.Rows;
using Xbim.Ifc.ElectricalDomain;
using Xbim.Ifc.Extensions;
using Xbim.Ifc.ExternalReferenceResource;
using Xbim.Ifc.HVACDomain;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.PropertyResource;
using Xbim.Ifc.SharedBldgElements;
using Xbim.Ifc.SharedComponentElements;
using Xbim.XbimExtensions;
using Xbim.Ifc.SelectTypes;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the Type tab.
    /// </summary>
    public class COBieDataType : COBieData<COBieTypeRow>, IAttributeProvider
    {

        #region Fields

        /// <summary>
        /// List of property names that are to be excluded from the Attributes generated from the Type sheet with equal compare
        /// </summary>
        private List<string> _typeAttExcludesEq = new List<string>() 
        {   "SustainabilityPerformanceCodePerformance",     "AccessibilityPerformance",     "Features",     "Constituents",     "Material",     "Grade", 
            "Finish",   "Color",    "Size",     "Shape",    "ModelReference",   "NominalHeight",    "NominalWidth", "NominalLength",    "WarrantyName",
            "WarrantyDescription",  "DurationUnit",         "ServiceLifeType",  "ServiceLifeDuration",  "ExpectedLife",     "LifeCyclePhase",   "Cost",
            "ReplacementCost",  "WarrantyDurationUnit", "WarrantyDurationLabor",    "WarrantyGuarantorLabor",   "WarrantyDurationParts",    
            "WarrantyGuarantorParts",   "ModelLabel",   "ModelNumber",  "Manufacturer", "IsFixed",  "AssetType", "CodePerformance", "SustainabilityPerformance"
        
        };
 
        /// <summary>
        /// List of property names that are to be excluded from the Attributes generated from the Type sheet with contains compare
        /// </summary>
        private List<string> _typeAttExcludesContains = new List<string>() { "Roomtag", "RoomTag", "GSA BIM Area" }; //"Tag",
        
        #endregion

        #region Properties

        /// <summary>
        /// List of property names that are to be excluded from the Attributes generated from the Type sheet with equal compare
        /// </summary>
        public List<string> TypeAttExcludesEq
        {
            get { return _typeAttExcludesEq; }
        }

       
        /// <summary>
        /// List of property names that are to be excluded from the Attributes generated from the Type sheet with contains compare
        /// </summary>
        public List<string> TypeAttExcludesContains
        {
            get { return _typeAttExcludesContains; }
        }
        
        #endregion
        
        /// <summary>
        /// Data Type constructor
        /// </summary>
        /// <param name="model">The context of the model being generated</param>
        public COBieDataType(COBieContext context) : base(context)
        { }

        #region Methods

        /// <summary>
        /// Fill sheet rows for Type sheet
        /// </summary>
        /// <returns>COBieSheet<COBieTypeRow></returns>
        public override COBieSheet<COBieTypeRow> Fill()
        {
            ProgressIndicator.ReportMessage("Starting Types...");

            // Create new Sheet
            COBieSheet<COBieTypeRow> types = new COBieSheet<COBieTypeRow>(Constants.WORKSHEET_TYPE);
            
            
            // get all IfcTypeObject objects from IFC file
            //IEnumerable<IfcTypeObject> ifcTypeObjects = Model.InstancesOfType<IfcTypeObject>()
            //    .Select(type => type)
            //    .Where(type => !TypeObjectExcludeTypes.Contains(type.GetType()));

            //group the types by name as we need to filter duplicate items in foreach loop
            IEnumerable<IfcTypeObject> ifcTypeObjects = Model.InstancesOfType<IfcTypeObject>()
                .Select(type => type)
                .Where(type => !TypeObjectExcludeTypes.Contains(type.GetType()))
                .GroupBy(type => type.Name).SelectMany(g => g);

            
            
            //set up property set helper class
            COBieDataPropertySetValues allPropertyValues = new COBieDataPropertySetValues(ifcTypeObjects); //properties helper class
            allPropertyValues.ExcludePropertyValueNames.AddRange(TypeAttExcludesEq);//we do not want for the attribute sheet so filter them out
            allPropertyValues.ExcludePropertyValueNamesWildcard.AddRange(TypeAttExcludesContains);//we do not want for the attribute sheet so filter them out
            allPropertyValues.ExcludePropertySetNames.Add(" BaseQuantities"); //exclude the property set from selection of values
            allPropertyValues.RowParameters["Sheet"] = "Type";

            ProgressIndicator.Initialise("Creating Types", ifcTypeObjects.Count());
            COBieTypeRow lastRow = null;
            foreach (IfcTypeObject type in ifcTypeObjects)
            {
                ProgressIndicator.IncrementAndUpdate();
                
                
                COBieTypeRow typeRow = new COBieTypeRow(types);
                
                // TODO: Investigate centralising this common code.
                typeRow.Name = type.Name;
                typeRow.CreatedBy = GetTelecomEmailAddress(type.OwnerHistory);
                typeRow.CreatedOn = GetCreatedOnDateAsFmtString(type.OwnerHistory);
                typeRow.Category = GetCategory(type, allPropertyValues);
                typeRow.Description = GetTypeObjDescription(type);

                typeRow.ExtSystem = GetExternalSystem(type);
                typeRow.ExtObject = type.GetType().Name;
                typeRow.ExtIdentifier = type.GlobalId;

                
            
                FillPropertySetsValues(allPropertyValues, type, typeRow);
                //not duplicate so add to sheet
                if (CheckForDuplicateRow(lastRow, typeRow)) 
                {
                    types.Rows.Add(typeRow);
                    lastRow = typeRow; //save this row to test on next loop
                }
                // Provide Attribute sheet with our context
                //fill in the attribute information
                allPropertyValues.RowParameters["Name"] = typeRow.Name;
                allPropertyValues.RowParameters["CreatedBy"] = typeRow.CreatedBy;
                allPropertyValues.RowParameters["CreatedOn"] = typeRow.CreatedOn;
                allPropertyValues.RowParameters["ExtSystem"] = typeRow.ExtSystem;
                allPropertyValues.PopulateAttributesRows(type, ref _attributes); //fill attribute sheet rows
                
            }
            ProgressIndicator.Finalise();
            return types;
        }

        private static bool CheckForDuplicateRow(COBieTypeRow lastRow, COBieTypeRow typeRow)
        {
            bool AddRecord = true;
            //test to see if we have a duplicate
            if (lastRow != null) //filter out first loop
            {
                if (string.Equals(lastRow.Name, typeRow.Name)) //only test sizing if names the same
                {
                    if ((!string.Equals(lastRow.NominalLength, typeRow.NominalLength)) || //use or's to skip further tests on a true not equal
                        (!string.Equals(lastRow.NominalWidth, typeRow.NominalWidth)) ||
                        (!string.Equals(lastRow.NominalHeight, typeRow.NominalHeight)) ||
                        (!string.Equals(lastRow.ModelNumber, typeRow.ModelNumber)) ||
                        (!string.Equals(lastRow.ModelReference, typeRow.ModelReference)) ||
                        (!string.Equals(lastRow.Size, typeRow.Size)) ||
                        (!string.Equals(lastRow.Manufacturer, typeRow.Manufacturer))
                         )
                        AddRecord = true; //one of the values do not match so record OK
                    else
                        AddRecord = false;//skip this record
                }
            }
            return AddRecord;
        }

        private void FillPropertySetsValues(COBieDataPropertySetValues allPropertyValues, IfcTypeObject type, COBieTypeRow typeRow)
        {
               
            //get related object properties to extract from if main way fails
            allPropertyValues.SetFilteredPropertySingleValues(type, "Pset_Asset");
            typeRow.AssetType =     allPropertyValues.GetFilteredPropertySingleValueValue("AssetAccountingType", false); 
            allPropertyValues.SetFilteredPropertySingleValues(type, "Pset_ManufacturersTypeInformation");
            string manufacturer = allPropertyValues.GetFilteredPropertySingleValueValue("Manufacturer", false);
            typeRow.Manufacturer = ((manufacturer == DEFAULT_STRING) || (!IsEmailAddress(manufacturer))) ? Constants.DEFAULT_EMAIL : manufacturer;
            typeRow.ModelNumber =   GetModelNumber(type, allPropertyValues);

            
            allPropertyValues.SetFilteredPropertySingleValues(type, "Pset_Warranty");
            typeRow.WarrantyGuarantorParts =    GetWarrantyGuarantorParts(type, allPropertyValues);
            string warrantyDurationPart =       allPropertyValues.GetFilteredPropertySingleValueValue("WarrantyDurationParts", false);
            typeRow.WarrantyDurationParts =     ((warrantyDurationPart == DEFAULT_STRING) || (!IsNumeric(warrantyDurationPart)) ) ? DEFAULT_NUMERIC : warrantyDurationPart;
            typeRow.WarrantyGuarantorLabor =    GetWarrantyGuarantorLabor(type, allPropertyValues);
            typeRow.WarrantyDescription =       GetWarrantyDescription(type, allPropertyValues);
            Interval warrantyDuration =         GetDurationUnitAndValue(allPropertyValues.GetFilteredPropertySingleValue("WarrantyDurationLabor")); 
            typeRow.WarrantyDurationLabor =     (!IsNumeric(warrantyDuration.Value)) ? DEFAULT_NUMERIC : warrantyDuration.Value;
            typeRow.WarrantyDurationUnit =      (string.IsNullOrEmpty(warrantyDuration.Unit)) ? "Year" : warrantyDuration.Unit; //redundant column via matrix sheet states set as year
            typeRow.ReplacementCost =           GetReplacementCost(type, allPropertyValues); 

            allPropertyValues.SetFilteredPropertySingleValues(type, "Pset_ServiceLife");
            Interval serviceDuration =  GetDurationUnitAndValue(allPropertyValues.GetFilteredPropertySingleValue("ServiceLifeDuration"));
            typeRow.ExpectedLife =      GetExpectedLife(type, serviceDuration, allPropertyValues);
            typeRow.DurationUnit =      serviceDuration.Unit;

            allPropertyValues.SetFilteredPropertySingleValues(type, "Pset_Specification");
            typeRow.NominalLength = GetNominalLength(type, allPropertyValues);
            typeRow.NominalWidth = GetNominalWidth(type, allPropertyValues);
            typeRow.NominalHeight =  GetNominalHeight(type, allPropertyValues);
            typeRow.ModelReference = GetModelReference(type, allPropertyValues);
            typeRow.Shape =                         allPropertyValues.GetFilteredPropertySingleValueValue("Shape", false);
            typeRow.Size =                          allPropertyValues.GetFilteredPropertySingleValueValue("Size", false);
            typeRow.Color =                         GetColour(type, allPropertyValues);
            typeRow.Finish =                        allPropertyValues.GetFilteredPropertySingleValueValue("Finish", false);
            typeRow.Grade =                         allPropertyValues.GetFilteredPropertySingleValueValue("Grade", false);
            typeRow.Material =                      allPropertyValues.GetFilteredPropertySingleValueValue("Material", false);
            typeRow.Constituents =                  GetConstituents(type, allPropertyValues);
            typeRow.Features =                      allPropertyValues.GetFilteredPropertySingleValueValue("Features", false);
            typeRow.AccessibilityPerformance =      GetAccessibilityPerformance(type, allPropertyValues);
            typeRow.CodePerformance =               GetCodePerformance(type, allPropertyValues);
            typeRow.SustainabilityPerformance =     GetSustainabilityPerformance(type, allPropertyValues); 
        }

        /// <summary>
        /// Get the Sustainability Performance for the IfcTypeObject
        /// </summary>
        /// <param name="ifcTypeObject">IfcTypeObject object</param>
        /// <param name="allPropertyValues">COBieDataPropertySetValues object holds all the properties for all the IfcSpace</param>
        /// <returns>property value as string or default value</returns>
        private string GetSustainabilityPerformance(IfcTypeObject ifcTypeObject, COBieDataPropertySetValues allPropertyValues)
        {
            string value = allPropertyValues.GetFilteredPropertySingleValueValue("SustainabilityPerformance", false);
            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            if (value == DEFAULT_STRING)
            {
                allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("SustainabilityPerformance", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("Environmental", true);

                //reset back to property set "Pset_Specification"
                allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject, "Pset_Specification");
            }
            return (string.IsNullOrEmpty(value)) ? DEFAULT_STRING : value;
        }

        /// <summary>
        /// Get the Code Performance for the IfcTypeObject
        /// </summary>
        /// <param name="ifcTypeObject">IfcTypeObject object</param>
        /// <param name="allPropertyValues">COBieDataPropertySetValues object holds all the properties for all the IfcSpace</param>
        /// <returns>property value as string or default value</returns>
        private string GetCodePerformance(IfcTypeObject ifcTypeObject, COBieDataPropertySetValues allPropertyValues)
        {
            string value = allPropertyValues.GetFilteredPropertySingleValueValue("CodePerformance", false);
            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            if (value == DEFAULT_STRING)
            {
                allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("CodePerformance", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("Regulation", true);

                //reset back to property set "Pset_Specification"
                allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject, "Pset_Specification");
            }
            return (string.IsNullOrEmpty(value)) ? DEFAULT_STRING : value;
        }


        /// <summary>
        /// Get the Accessibility Performance for the IfcTypeObject
        /// </summary>
        /// <param name="ifcTypeObject">IfcTypeObject object</param>
        /// <param name="allPropertyValues">COBieDataPropertySetValues object holds all the properties for all the IfcSpace</param>
        /// <returns>property value as string or default value</returns>
        private string GetAccessibilityPerformance(IfcTypeObject ifcTypeObject, COBieDataPropertySetValues allPropertyValues)
        {
            string value = allPropertyValues.GetFilteredPropertySingleValueValue("AccessibilityPerformance", false);
            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            if (value == DEFAULT_STRING)
            {
                allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("AccessibilityPerformance", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("Access", true);

                //reset back to property set "Pset_Specification"
                allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject, "Pset_Specification");
            }
            return (string.IsNullOrEmpty(value)) ? DEFAULT_STRING : value;
        }

        /// <summary>
        /// Get the Constituents for the IfcTypeObject
        /// </summary>
        /// <param name="ifcTypeObject">IfcTypeObject object</param>
        /// <param name="allPropertyValues">COBieDataPropertySetValues object holds all the properties for all the IfcSpace</param>
        /// <returns>property value as string or default value</returns>
        private string GetConstituents(IfcTypeObject ifcTypeObject, COBieDataPropertySetValues allPropertyValues)
        {
            string value = allPropertyValues.GetFilteredPropertySingleValueValue("Constituents", false);
            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            if (value == DEFAULT_STRING)
            {
                allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("constituents", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("parts", true);

                //reset back to property set "Pset_Specification"
                allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject, "Pset_Specification");
            }
            return (string.IsNullOrEmpty(value)) ? DEFAULT_STRING : value;
        }

        /// <summary>
        /// Get the Colour for the IfcTypeObject
        /// </summary>
        /// <param name="ifcTypeObject">IfcTypeObject object</param>
        /// <param name="allPropertyValues">COBieDataPropertySetValues object holds all the properties for all the IfcSpace</param>
        /// <returns>property value as string or default value</returns>
        private string GetColour(IfcTypeObject ifcTypeObject, COBieDataPropertySetValues allPropertyValues)
        {
            string value = allPropertyValues.GetFilteredPropertySingleValueValue("Colour", false);
            if (value == DEFAULT_STRING)
                value = allPropertyValues.GetFilteredPropertySingleValueValue("Color", false);
            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            if (value == DEFAULT_STRING)
            {
                allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("Colour", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("Color", true);

                //reset back to property set "Pset_Specification"
                allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject, "Pset_Specification");
            }
            return (string.IsNullOrEmpty(value)) ? DEFAULT_STRING : value;
        }

        /// <summary>
        /// Get the Model Reference for the IfcTypeObject
        /// </summary>
        /// <param name="ifcTypeObject">IfcTypeObject object</param>
        /// <param name="allPropertyValues">COBieDataPropertySetValues object holds all the properties for all the IfcSpace</param>
        /// <returns>property value as string or default value</returns>
        private string GetModelReference(IfcTypeObject ifcTypeObject, COBieDataPropertySetValues allPropertyValues)
        {
            string value = allPropertyValues.GetFilteredPropertySingleValueValue("ModelReference", false);
            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            if (value == DEFAULT_STRING)
            {
                allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("ModelReference", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("Reference", true);

                //reset back to property set "Pset_Specification"
                allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject, "Pset_Specification");
            }
            return (string.IsNullOrEmpty(value)) ? DEFAULT_STRING : value;
        }

        /// <summary>
        /// Get the Nominal Height for the IfcTypeObject
        /// </summary>
        /// <param name="ifcTypeObject">IfcTypeObject object</param>
        /// <param name="allPropertyValues">COBieDataPropertySetValues object holds all the properties for all the IfcSpace</param>
        /// <returns>property value as string or default value</returns>
        private string GetNominalHeight(IfcTypeObject ifcTypeObject, COBieDataPropertySetValues allPropertyValues)
        {
            string value = allPropertyValues.GetFilteredPropertySingleValueValue("NominalHeight", false);
            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            if (value == DEFAULT_STRING)
            {
                allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("NominalHeight", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("Height", true);

                //reset back to property set "Pset_Specification"
                allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject, "Pset_Specification");
            }
            return ConvertNumberOrDefault(value);
        }


        /// <summary>
        /// Get the Nominal Width for the IfcTypeObject
        /// </summary>
        /// <param name="ifcTypeObject">IfcTypeObject object</param>
        /// <param name="allPropertyValues">COBieDataPropertySetValues object holds all the properties for all the IfcSpace</param>
        /// <returns>property value as string or default value</returns>
        private string GetNominalWidth(IfcTypeObject ifcTypeObject, COBieDataPropertySetValues allPropertyValues)
        {
            string value = allPropertyValues.GetFilteredPropertySingleValueValue("NominalWidth", false);
            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            if (value == DEFAULT_STRING)
            {
                allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("NominalWidth", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("Width", true);

                //reset back to property set "Pset_Specification"
                allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject, "Pset_Specification");
            }
            return ConvertNumberOrDefault(value);
        }

        /// <summary>
        /// Get the Nominal Length for the IfcTypeObject
        /// </summary>
        /// <param name="ifcTypeObject">IfcTypeObject object</param>
        /// <param name="allPropertyValues">COBieDataPropertySetValues object holds all the properties for all the IfcSpace</param>
        /// <returns>property value as string or default value</returns>
        private string GetNominalLength(IfcTypeObject ifcTypeObject, COBieDataPropertySetValues allPropertyValues)
        {
            string value = allPropertyValues.GetFilteredPropertySingleValueValue("NominalLength", false);
            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            if (value == DEFAULT_STRING)
            {
                allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("NominalLength", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("OverallLength", true);

                //reset back to property set "Pset_Specification"
                allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject, "Pset_Specification");
            }
            return ConvertNumberOrDefault(value);
        }



        /// <summary>
        /// Get the Expected Life for the IfcTypeObject
        /// </summary>
        /// <param name="ifcTypeObject">IfcTypeObject object</param>
        /// <param name="allPropertyValues">COBieDataPropertySetValues object holds all the properties for all the IfcSpace</param>
        /// <returns>property value as string or default value</returns>
        private string GetExpectedLife(IfcTypeObject ifcTypeObject, Interval serviceDuration, COBieDataPropertySetValues allPropertyValues)
        {
            string value = serviceDuration.Value;

            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject);
            if (value == DEFAULT_STRING)
                value = allPropertyValues.GetFilteredPropertySingleValueValue("ServiceLifeDuration", true);
            if (value == DEFAULT_STRING)
                value = allPropertyValues.GetFilteredPropertySingleValueValue(" Expected", true);
            return ((string.IsNullOrEmpty(value)) || (value == DEFAULT_STRING) || (!IsNumeric(value))) ? DEFAULT_NUMERIC : value;
        }


        /// <summary>
        /// Get the Replacement Cost for the IfcTypeObject
        /// </summary>
        /// <param name="ifcTypeObject">IfcTypeObject object</param>
        /// <param name="allPropertyValues">COBieDataPropertySetValues object holds all the properties for all the IfcSpace</param>
        /// <returns>property value as string or default value</returns>
        private string GetReplacementCost(IfcTypeObject ifcTypeObject, COBieDataPropertySetValues allPropertyValues)
        {
            allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject, "Pset_EconomicImpactValues");
            string value = allPropertyValues.GetFilteredPropertySingleValueValue("ReplacementCost", false);

            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            if (value == DEFAULT_STRING)
            {
                allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("ReplacementCost", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("Replacement Cost", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("Cost", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("Replacement", true);
                //reset back to property set "Pset_Warranty"
                allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject, "Pset_Warranty");
            }
            return ((string.IsNullOrEmpty(value)) || (value == DEFAULT_STRING) || (!IsNumeric(value))) ? DEFAULT_NUMERIC : value;

        }

        /// <summary>
        /// Get the Warranty Description for the IfcTypeObject
        /// </summary>
        /// <param name="ifcTypeObject">IfcTypeObject object</param>
        /// <param name="allPropertyValues">COBieDataPropertySetValues object holds all the properties for all the IfcSpace</param>
        /// <returns>property value as string or default value</returns>
        private string GetWarrantyDescription(IfcTypeObject ifcTypeObject, COBieDataPropertySetValues allPropertyValues)
        {
            string value = allPropertyValues.GetFilteredPropertySingleValueValue("WarrantyDescription", false);

            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            if (value == DEFAULT_STRING)
            {
                allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("WarrantyDescription", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("WarrantyIdentifier", true);

                //reset back to property set "Pset_Warranty"
                allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject, "Pset_Warranty"); 
            }
            return (string.IsNullOrEmpty(value)) ? DEFAULT_STRING : value;
        }

        /// <summary>
        /// Get the Warranty Guarantor Labor for the IfcTypeObject
        /// </summary>
        /// <param name="ifcTypeObject">IfcTypeObject object</param>
        /// <param name="allPropertyValues">COBieDataPropertySetValues object holds all the properties for all the IfcSpace</param>
        /// <returns>property value as string or default value</returns>
        private string GetWarrantyGuarantorLabor(IfcTypeObject ifcTypeObject, COBieDataPropertySetValues allPropertyValues)
        {
            string value = allPropertyValues.GetFilteredPropertySingleValueValue("WarrantyGuarantorLabor", false);
            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            if (value == DEFAULT_STRING)
            {
                allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("WarrantyGuarantorParts", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("PointOfContact", true);

                //reset back to property set "Pset_Warranty"
                allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject, "Pset_Warranty"); 
            }
            return (((string.IsNullOrEmpty(value)) || (value == DEFAULT_STRING)) || (!IsEmailAddress(value))) ? Constants.DEFAULT_EMAIL : value;
        }

        /// <summary>
        /// Get the Warranty Guarantor Parts for the IfcTypeObject
        /// </summary>
        /// <param name="ifcTypeObject">IfcTypeObject object</param>
        /// <param name="allPropertyValues">COBieDataPropertySetValues object holds all the properties for all the IfcSpace</param>
        /// <returns>property value as string or default value</returns>
        private string GetWarrantyGuarantorParts(IfcTypeObject ifcTypeObject, COBieDataPropertySetValues allPropertyValues)
        {
            string value = allPropertyValues.GetFilteredPropertySingleValueValue("WarrantyGuarantorParts", false);
            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            if (value == DEFAULT_STRING)
            {
                allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("WarrantyGuarantorParts", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("PointOfContact", true);

                //reset back to property set "Pset_Warranty"
                allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject, "Pset_Warranty"); 
            }
            return (((string.IsNullOrEmpty(value)) || (value == DEFAULT_STRING)) || (!IsEmailAddress(value))) ? Constants.DEFAULT_EMAIL : value;
        }

        /// <summary>
        /// Get the Model Number for the IfcTypeObject
        /// </summary>
        /// <param name="ifcTypeObject">IfcTypeObject object</param>
        /// <param name="allPropertyValues">COBieDataPropertySetValues object holds all the properties for all the IfcSpace</param>
        /// <returns>property value as string or default value</returns>
        private string GetModelNumber(IfcTypeObject ifcTypeObject, COBieDataPropertySetValues allPropertyValues)
        {
            string value = allPropertyValues.GetFilteredPropertySingleValueValue("ModelLabel", false);
            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            if (value == DEFAULT_STRING)
            {
                allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("ArticleNumber", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetFilteredPropertySingleValueValue("ModelLabel", true);

                //reset back to property set "Pset_Asset"
                allPropertyValues.SetFilteredPropertySingleValues(ifcTypeObject, "Pset_Asset"); 
            }
            return (string.IsNullOrEmpty(value)) ? DEFAULT_STRING : value;
        }

        /// <summary>
        /// Return the Description for the passed IfcTypeObject object
        /// </summary>
        /// <param name="type">IfcTypeObject</param>
        /// <returns>Description for Type Object</returns>
        private string GetTypeObjDescription(IfcTypeObject type)
        {
            if (type != null)
            {
                if (!string.IsNullOrEmpty(type.Description)) return type.Description;
                else if (!string.IsNullOrEmpty(type.Name)) return type.Name;
                else
                {
                    //if supports PredefinedType and no description or name then use the predefined type or ElementType if they exist
                    IEnumerable<PropertyInfo> pInfo = type.GetType().GetProperties(); //get properties

                    if (pInfo.Where(p => p.Name == "PredefinedType").Count() == 1)
                    {
                        // TODO: Looks Wrong
                        string temp = pInfo.First().GetValue(type, null).ToString(); //get predefindtype as description

                        if (!string.IsNullOrEmpty(temp))
                        {
                            if (temp == "USERDEFINED")
                            {
                                //if used defined then the type description should be in ElementType, so see if property exists
                                if (pInfo.Where(p => p.Name == "ElementType").Count() == 1)
                                {
                                    temp = pInfo.First().GetValue(type, null).ToString(); //get ElementType
                                    if (!string.IsNullOrEmpty(temp)) return temp;
                                }
                            }
                            if (temp == "NOTDEFINED") //if not defined then give up and return default
                            {
                                return DEFAULT_STRING;
                            }

                            return temp;
                        }

                    }
                }
            }
            return DEFAULT_STRING;
        }

        /// <summary>
        /// Get the list or properties matching the passed in list of attribute names 
        /// </summary>
        /// <param name="typeObj">IfcTypeObject </param>
        /// <param name="attNames">list of attribute names</param>
        /// <returns>List of IfcPropertySingleValue which are contained in AttNames</returns>
        private IEnumerable<IfcPropertySingleValue> GetTypeObjRelAttributes(IfcTypeObject typeObj, List<string> attNames)
        {
            IEnumerable<IfcPropertySingleValue> properties = Enumerable.Empty<IfcPropertySingleValue>();
            // can hold zero or 1 ObjectTypeOf (IfcRelDefinesByType- holds list of objects of this type in RelatedObjects property) so test return
            var typeInstanceRel = typeObj.ObjectTypeOf.FirstOrDefault(); 
            if (typeInstanceRel != null)
            {
                // TODO: Check usage of GetAllProperties - duplicates Properties from Type?
                foreach (IfcPropertySet pset in typeInstanceRel.RelatedObjects.First().GetAllPropertySets()) 
                {
                    //has to have 1 or more object that are of this type, so get first and see what we get
                    properties = properties.Concat(pset.HasProperties.Where<IfcPropertySingleValue>(p => attNames.Contains(p.Name.ToString())));
                }
            }


            return properties;
        }

        

        /// <summary>
        /// Get the Time unit and value for the passed in property
        /// </summary>
        /// <param name="typeObject">IfcTypeObject </param>
        /// <param name="psetName">Property Set Name to retrieve IfcPropertySet Object</param>
        /// <param name="propertyName">Property Name held in IfcPropertySingleValue object</param>
        /// <param name="psetValues">List of IfcPropertySingleValue filtered to attribute names we require</param>
        /// <returns>Dictionary holding unit and value e.g. Year, 2.0</returns>
        private Interval GetDurationUnitAndValue( IfcPropertySingleValue typeValue)
        {
            const string DefaultUnit = "Year"; // n/a is not acceptable, so create a valid default

            Interval result = new Interval() { Value = DEFAULT_NUMERIC, Unit = DefaultUnit };
            // try to get the property from the Type first
            //IfcPropertySingleValue typeValue = typeObject.GetPropertySingleValue(psetName, propertyName);

            //// TODO: Check this logic
            //// if null then try and get from first instance of this type
            //if (typeValue == null) 
            //    typeValue = psetValues.Where(p => p.Name == propertyName).FirstOrDefault();

            if (typeValue == null)
                return result;

            //Get the unit type
            if (typeValue.Unit != null) 
                result.Unit = GetUnit(typeValue.Unit);            
            
            //Get the time period value
            if ((typeValue.NominalValue != null) && (typeValue.NominalValue is IfcReal)) //if a number then we can calculate
            {
                double val = (double)typeValue.NominalValue.Value; 
                result.Value = val.ToString("F1");
            }
            else if (typeValue.NominalValue != null) //no number value so just show value for passed in propName
                result.Value = typeValue.NominalValue.Value.ToString();

            return result;
        }

        

        /// <summary>
        /// Get the Category for the IfcTypeObject
        /// </summary>
        /// <param name="type">IfcTypeObject</param>
        /// <returns>string of the category</returns>
        public string GetCategory(IfcTypeObject type, COBieDataPropertySetValues allPropertyValues)
        {
            
            //Try by relationship first
            IfcRelAssociatesClassification classification = type.HasAssociations.OfType<IfcRelAssociatesClassification>().FirstOrDefault();
            if (classification != null)
            {
                IfcClassificationReference classificationRef = (IfcClassificationReference)classification.RelatingClassification;
                
                if (!string.IsNullOrEmpty(classificationRef.Name))
                {
                    return classificationRef.Name;
                }
                if (!string.IsNullOrEmpty(classificationRef.ItemReference))
                {
                    return classificationRef.ItemReference;
                }
                if ((classificationRef.ReferencedSource != null) && (!string.IsNullOrEmpty(classificationRef.ReferencedSource.Name)))
                {
                    return classificationRef.ReferencedSource.Name;
                }
            }
  
            //Try by PropertySet as fallback
            //filter list for front end category
            List<string> categoriesCode = new List<string>() { "OmniClass Table 13 Category",  "OmniClass Number", "OmniClass_Number", "Assembly_Code",  "Assembly Code", 
                                                             "Uniclass Code", "Uniclass_Code",  "Category_Code" ,"Category Code",  "Classification Code", "Classification_Code" };
            //filter list for back end category
            List<string> categoriesDesc = new List<string>() { "OmniClass Title", "OmniClass_Title","Assembly_Description","Assembly Description","UniclassDescription", 
                                                             "Uniclass_Description","Category Description", "Category_Description", "Classification Description", "Classification_Description" };
            List<string> categoriesTest = new List<string>();
            categoriesCode.AddRange(categoriesDesc);
            
            IEnumerable<IfcPropertySingleValue> properties = Enumerable.Empty<IfcPropertySingleValue>();

            Dictionary<IfcPropertySet, List<IfcSimpleProperty>> propertysets = allPropertyValues[type];
            if (propertysets != null)
            {
                 properties = (from dic in propertysets
                             from psetval in dic.Value
                              where categoriesTest.Contains(psetval.Name.ToString())
                               select psetval).OfType<IfcPropertySingleValue>();
            }
            //second fall back on objects defined by this type, see if they hold a category on the first related object to this type
            if (!properties.Any())
            {
                propertysets = allPropertyValues.GetRelatedProperties(type);
                if (propertysets != null)
                {
                    properties = (from dic in propertysets
                             from psetval in dic.Value
                                 where categoriesTest.Contains(psetval.Name.ToString())
                                  select psetval).OfType<IfcPropertySingleValue>();
                }
            }
            string value = "";
            if (properties.Any())
            {
                string code = properties.Where(p => p.NominalValue != null && categoriesCode.Contains(p.Name)).Select(p => p.NominalValue.ToString()).FirstOrDefault();
                string description = properties.Where(p => p.NominalValue != null && categoriesDesc.Contains(p.Name)).Select(p => p.NominalValue.ToString()).FirstOrDefault();
                if (!string.IsNullOrEmpty(code)) value += code;
                if (!string.IsNullOrEmpty(description)) value += ": " +  description;
            }

            if (string.IsNullOrEmpty(value))
                return Constants.DEFAULT_STRING;
            else
                return value;
        }
        #endregion

        COBieSheet<COBieAttributeRow> _attributes;

        public void InitialiseAttributes(ref COBieSheet<COBieAttributeRow> attributeSheet)
        {
            _attributes = attributeSheet;
        }
    }

    public struct Interval
    {
        public string Value { get; set; }
        public string Unit { get; set; }
    }
}
