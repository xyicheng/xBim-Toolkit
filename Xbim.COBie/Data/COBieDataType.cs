using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xbim.COBie.Rows;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.ExternalReferenceResource;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.Ifc2x3.SharedFacilitiesElements;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the Type tab.
    /// </summary>
    public class COBieDataType : COBieData<COBieTypeRow>, IAttributeProvider
    {

        
        
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
            
            //group the types by name as we need to filter duplicate items in for each loop
            IEnumerable<IfcTypeObject> ifcTypeObjects = Model.Instances.OfType<IfcTypeObject>()
                .Select(type => type)
                .Where(type => !Context.TypeObjectExcludeTypes.Contains(type.GetType()))
                .GroupBy(type => type.Name).Distinct().SelectMany(g => g);

            
            
            //set up property set helper class
            COBieDataPropertySetValues allPropertyValues = new COBieDataPropertySetValues(ifcTypeObjects); //properties helper class
            COBieDataAttributeBuilder attributeBuilder = new COBieDataAttributeBuilder(Context, allPropertyValues);
            attributeBuilder.InitialiseAttributes(ref _attributes);
            attributeBuilder.ExcludeAttributePropertyNames.AddRange(Context.TypeAttExcludesEq);//we do not want for the attribute sheet so filter them out
            attributeBuilder.ExcludeAttributePropertyNamesWildcard.AddRange(Context.TypeAttExcludesContains);//we do not want for the attribute sheet so filter them out
            attributeBuilder.ExcludeAttributePropertySetNames.AddRange(Context.TypeAttExcludesPropertSetEq); //exclude the property set from selection of values
            attributeBuilder.RowParameters["Sheet"] = "Type";

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
                attributeBuilder.RowParameters["Name"] = typeRow.Name;
                attributeBuilder.RowParameters["CreatedBy"] = typeRow.CreatedBy;
                attributeBuilder.RowParameters["CreatedOn"] = typeRow.CreatedOn;
                attributeBuilder.RowParameters["ExtSystem"] = typeRow.ExtSystem;
                attributeBuilder.PopulateAttributesRows(type); //fill attribute sheet rows
                
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
            allPropertyValues.SetAllPropertySingleValues(type, "Pset_Asset");
            typeRow.AssetType =     GetAssetType(type, allPropertyValues); 
            allPropertyValues.SetAllPropertySingleValues(type, "Pset_ManufacturersTypeInformation");
            string manufacturer =   allPropertyValues.GetPropertySingleValueValue("Manufacturer", false);
            typeRow.Manufacturer =  ((manufacturer == DEFAULT_STRING) || (!IsEmailAddress(manufacturer))) ? Constants.DEFAULT_EMAIL : manufacturer;
            typeRow.ModelNumber =   GetModelNumber(type, allPropertyValues);

            
            allPropertyValues.SetAllPropertySingleValues(type, "Pset_Warranty");
            typeRow.WarrantyGuarantorParts =    GetWarrantyGuarantorParts(type, allPropertyValues);
            string warrantyDurationPart =       allPropertyValues.GetPropertySingleValueValue("WarrantyDurationParts", false);
            typeRow.WarrantyDurationParts =     ((warrantyDurationPart == DEFAULT_STRING) || (!IsNumeric(warrantyDurationPart)) ) ? DEFAULT_NUMERIC : warrantyDurationPart;
            typeRow.WarrantyGuarantorLabor =    GetWarrantyGuarantorLabor(type, allPropertyValues);
            typeRow.WarrantyDescription =       GetWarrantyDescription(type, allPropertyValues);
            Interval warrantyDuration =         GetDurationUnitAndValue(allPropertyValues.GetPropertySingleValue("WarrantyDurationLabor")); 
            typeRow.WarrantyDurationLabor =     (!IsNumeric(warrantyDuration.Value)) ? DEFAULT_NUMERIC : warrantyDuration.Value;
            typeRow.WarrantyDurationUnit =      (string.IsNullOrEmpty(warrantyDuration.Unit)) ? "Year" : warrantyDuration.Unit; //redundant column via matrix sheet states set as year
            typeRow.ReplacementCost =           GetReplacementCost(type, allPropertyValues); 

            allPropertyValues.SetAllPropertySingleValues(type, "Pset_ServiceLife");
            Interval serviceDuration =  GetDurationUnitAndValue(allPropertyValues.GetPropertySingleValue("ServiceLifeDuration"));
            typeRow.ExpectedLife =      GetExpectedLife(type, serviceDuration, allPropertyValues);
            typeRow.DurationUnit =      serviceDuration.Unit;

            allPropertyValues.SetAllPropertySingleValues(type, "Pset_Specification");
            typeRow.NominalLength =                 GetNominalLength(type, allPropertyValues);
            typeRow.NominalWidth =                  GetNominalWidth(type, allPropertyValues);
            typeRow.NominalHeight =                 GetNominalHeight(type, allPropertyValues);
            typeRow.ModelReference =                GetModelReference(type, allPropertyValues);
            typeRow.Shape =                         allPropertyValues.GetPropertySingleValueValue("Shape", false);
            typeRow.Size =                          allPropertyValues.GetPropertySingleValueValue("Size", false);
            typeRow.Color =                         GetColour(type, allPropertyValues);
            typeRow.Finish =                        allPropertyValues.GetPropertySingleValueValue("Finish", false);
            typeRow.Grade =                         allPropertyValues.GetPropertySingleValueValue("Grade", false);
            typeRow.Material =                      allPropertyValues.GetPropertySingleValueValue("Material", false);
            typeRow.Constituents =                  GetConstituents(type, allPropertyValues);
            typeRow.Features =                      allPropertyValues.GetPropertySingleValueValue("Features", false);
            typeRow.AccessibilityPerformance =      GetAccessibilityPerformance(type, allPropertyValues);
            typeRow.CodePerformance =               GetCodePerformance(type, allPropertyValues);
            typeRow.SustainabilityPerformance =     GetSustainabilityPerformance(type, allPropertyValues); 
        }

        /// <summary>
        /// Get the Asset Type from the property set property if nothing found then default to Moveable/Fixed decided on object type
        /// </summary>
        /// <param name="ifcTypeObject">IfcTypeObject Object</param>
        /// <param name="allPropertyValues">COBieDataPropertySetValues object holding the property sets</param>
        /// <returns>String holding Asset Type</returns>
        private string GetAssetType(IfcTypeObject ifcTypeObject, COBieDataPropertySetValues allPropertyValues)
        {
            string value = allPropertyValues.GetPropertySingleValueValue("AssetAccountingType", false);
            if (value == DEFAULT_STRING)
	        {
                if (ifcTypeObject is IfcFurnitureType)
                    value = "Moveable";                   
                else
                    value = "Fixed";
	        }
            return value;
        }

        /// <summary>
        /// Get the Sustainability Performance for the IfcTypeObject
        /// </summary>
        /// <param name="ifcTypeObject">IfcTypeObject object</param>
        /// <param name="allPropertyValues">COBieDataPropertySetValues object holds all the properties for all the IfcSpace</param>
        /// <returns>property value as string or default value</returns>
        private string GetSustainabilityPerformance(IfcTypeObject ifcTypeObject, COBieDataPropertySetValues allPropertyValues)
        {
            string value = allPropertyValues.GetPropertySingleValueValue("SustainabilityPerformance", false);
            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            if (value == DEFAULT_STRING)
            {
                allPropertyValues.SetAllPropertySingleValues(ifcTypeObject);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("SustainabilityPerformance", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("Environmental", true);

                //reset back to property set "Pset_Specification"
                allPropertyValues.SetAllPropertySingleValues(ifcTypeObject, "Pset_Specification");
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
            string value = allPropertyValues.GetPropertySingleValueValue("CodePerformance", false);
            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            if (value == DEFAULT_STRING)
            {
                allPropertyValues.SetAllPropertySingleValues(ifcTypeObject);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("CodePerformance", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("Regulation", true);

                //reset back to property set "Pset_Specification"
                allPropertyValues.SetAllPropertySingleValues(ifcTypeObject, "Pset_Specification");
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
            string value = allPropertyValues.GetPropertySingleValueValue("AccessibilityPerformance", false);
            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            if (value == DEFAULT_STRING)
            {
                allPropertyValues.SetAllPropertySingleValues(ifcTypeObject);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("AccessibilityPerformance", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("Access", true);

                //reset back to property set "Pset_Specification"
                allPropertyValues.SetAllPropertySingleValues(ifcTypeObject, "Pset_Specification");
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
            string value = allPropertyValues.GetPropertySingleValueValue("Constituents", false);
            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            if (value == DEFAULT_STRING)
            {
                allPropertyValues.SetAllPropertySingleValues(ifcTypeObject);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("constituents", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("parts", true);

                //reset back to property set "Pset_Specification"
                allPropertyValues.SetAllPropertySingleValues(ifcTypeObject, "Pset_Specification");
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
            string value = allPropertyValues.GetPropertySingleValueValue("Colour", false);
            if (value == DEFAULT_STRING)
                value = allPropertyValues.GetPropertySingleValueValue("Color", false);
            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            if (value == DEFAULT_STRING)
            {
                allPropertyValues.SetAllPropertySingleValues(ifcTypeObject);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("Colour", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("Color", true);

                //reset back to property set "Pset_Specification"
                allPropertyValues.SetAllPropertySingleValues(ifcTypeObject, "Pset_Specification");
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
            string value = allPropertyValues.GetPropertySingleValueValue("ModelReference", false);
            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            if (value == DEFAULT_STRING)
            {
                allPropertyValues.SetAllPropertySingleValues(ifcTypeObject);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("ModelReference", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("Reference", true);

                //reset back to property set "Pset_Specification"
                allPropertyValues.SetAllPropertySingleValues(ifcTypeObject, "Pset_Specification");
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
            string value = allPropertyValues.GetPropertySingleValueValue("NominalHeight", false);
            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            if (value == DEFAULT_STRING)
            {
                allPropertyValues.SetAllPropertySingleValues(ifcTypeObject);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("NominalHeight", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("OverallHeight", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("Height", true);

                //reset back to property set "Pset_Specification"
                allPropertyValues.SetAllPropertySingleValues(ifcTypeObject, "Pset_Specification");
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
            string value = allPropertyValues.GetPropertySingleValueValue("NominalWidth", false);
            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            if (value == DEFAULT_STRING)
            {
                allPropertyValues.SetAllPropertySingleValues(ifcTypeObject);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("NominalWidth", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("OverallWidth", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("Width", true);
                //reset back to property set "Pset_Specification"
                allPropertyValues.SetAllPropertySingleValues(ifcTypeObject, "Pset_Specification");
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
            string value = allPropertyValues.GetPropertySingleValueValue("NominalLength", false);
            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            if (value == DEFAULT_STRING)
            {
                allPropertyValues.SetAllPropertySingleValues(ifcTypeObject);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("NominalLength", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("OverallLength", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("Length", true);

                //reset back to property set "Pset_Specification"
                allPropertyValues.SetAllPropertySingleValues(ifcTypeObject, "Pset_Specification");
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
            allPropertyValues.SetAllPropertySingleValues(ifcTypeObject);
            if (value == DEFAULT_STRING)
                value = allPropertyValues.GetPropertySingleValueValue("ServiceLifeDuration", true);
            if (value == DEFAULT_STRING)
                value = allPropertyValues.GetPropertySingleValueValue(" Expected", true);
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
            allPropertyValues.SetAllPropertySingleValues(ifcTypeObject, "Pset_EconomicImpactValues");
            string value = allPropertyValues.GetPropertySingleValueValue("ReplacementCost", false);

            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            if (value == DEFAULT_STRING)
            {
                allPropertyValues.SetAllPropertySingleValues(ifcTypeObject);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("ReplacementCost", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("Replacement Cost", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("Cost", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("Replacement", true);
                //reset back to property set "Pset_Warranty"
                allPropertyValues.SetAllPropertySingleValues(ifcTypeObject, "Pset_Warranty");
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
            string value = allPropertyValues.GetPropertySingleValueValue("WarrantyDescription", false);

            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            if (value == DEFAULT_STRING)
            {
                allPropertyValues.SetAllPropertySingleValues(ifcTypeObject);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("WarrantyDescription", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("WarrantyIdentifier", true);

                //reset back to property set "Pset_Warranty"
                allPropertyValues.SetAllPropertySingleValues(ifcTypeObject, "Pset_Warranty"); 
            }
            return (string.IsNullOrEmpty(value)) ? DEFAULT_STRING : value;
        }

        /// <summary>
        /// Get the Warranty Guarantor Labour for the IfcTypeObject
        /// </summary>
        /// <param name="ifcTypeObject">IfcTypeObject object</param>
        /// <param name="allPropertyValues">COBieDataPropertySetValues object holds all the properties for all the IfcSpace</param>
        /// <returns>property value as string or default value</returns>
        private string GetWarrantyGuarantorLabor(IfcTypeObject ifcTypeObject, COBieDataPropertySetValues allPropertyValues)
        {
            string value = allPropertyValues.GetPropertySingleValueValue("WarrantyGuarantorLabor", false);
            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            if (value == DEFAULT_STRING)
            {
                allPropertyValues.SetAllPropertySingleValues(ifcTypeObject);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("WarrantyGuarantorParts", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("PointOfContact", true);

                //reset back to property set "Pset_Warranty"
                allPropertyValues.SetAllPropertySingleValues(ifcTypeObject, "Pset_Warranty"); 
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
            string value = allPropertyValues.GetPropertySingleValueValue("WarrantyGuarantorParts", false);
            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            if (value == DEFAULT_STRING)
            {
                allPropertyValues.SetAllPropertySingleValues(ifcTypeObject);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("WarrantyGuarantorParts", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("PointOfContact", true);

                //reset back to property set "Pset_Warranty"
                allPropertyValues.SetAllPropertySingleValues(ifcTypeObject, "Pset_Warranty"); 
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
            string value = allPropertyValues.GetPropertySingleValueValue("ModelLabel", false);
            //Fall back to wild card properties
            //get the property single values for this ifcTypeObject
            if (value == DEFAULT_STRING)
            {
                allPropertyValues.SetAllPropertySingleValues(ifcTypeObject);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("ArticleNumber", true);
                if (value == DEFAULT_STRING)
                    value = allPropertyValues.GetPropertySingleValueValue("ModelLabel", true);

                //reset back to property set "Pset_Asset"
                allPropertyValues.SetAllPropertySingleValues(ifcTypeObject, "Pset_Asset"); 
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
                result.Unit = GetUnitName(typeValue.Unit);            
            
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

                if (!string.IsNullOrEmpty(classificationRef.Location))
                {
                    return classificationRef.Location;
                }
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
