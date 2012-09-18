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
            
            
            // get all IfcTypeObject objects from IFC file
            IEnumerable<IfcTypeObject> ifcTypeObjects = Model.InstancesOfType<IfcTypeObject>()
                .Select(type => type)
                .Where(type => !TypeObjectExcludeTypes.Contains(type.GetType()));

            
            // Well known property names to seek out the data
            List<string> candidateProperties = new List<string> {  "AssetAccountingType", "Manufacturer", "ModelLabel", "WarrantyGuarantorParts", 
                                                        "WarrantyDurationParts", "WarrantyGuarantorLabor", "WarrantyDurationLabor", 
                                                        "ReplacementCost", "ServiceLifeDuration", "WarrantyDescription", "WarrantyDurationUnit" , "NominalLength", "NominalWidth",
                                                        "NominalHeight", "ModelReference", "Shape", "Colour", "Color", "Finish", "Grade", 
                                                        "Material", "Constituents", "Features", "Size", "AccessibilityPerformance", "CodePerformance", 
                                                        "SustainabilityPerformance", "Warranty Information"};

            // Additional Type values to exclude from attribute sheetWarrantyGuarantorParts
            List<string> excludePropertyValueNames = new List<string> {"WarrantyName","DurationUnit","ServiceLifeType","LifeCyclePhase",
                                                         "Cost","ModelNumber","IsFixed","AssetType"
                                                        };
            excludePropertyValueNames.AddRange(candidateProperties); //add the attributes from the type sheet to exclude from the attribute sheet
            //list of attributes to exclude form attribute sheet
            List<string> excludePropertyValueNamesWildcard = new List<string> { "Roomtag", "RoomTag", "Tag", "GSA BIM Area" };

            //set up property set helper class
            COBieDataPropertySetValues allPropertyValues = new COBieDataPropertySetValues(ifcTypeObjects.ToList()); //properties helper class
            allPropertyValues.ExcludePropertyValueNames.AddRange(excludePropertyValueNames);
            allPropertyValues.FilterPropertyValueNames.AddRange(candidateProperties);
            allPropertyValues.ExcludePropertyValueNamesWildcard.AddRange(excludePropertyValueNamesWildcard);
            allPropertyValues.ExcludePropertySetNames.Add(" BaseQuantities");
            allPropertyValues.RowParameters["Sheet"] = "Type";

            ProgressIndicator.Initialise("Creating Types", ifcTypeObjects.Count());

            foreach (IfcTypeObject type in ifcTypeObjects)
            {
                ProgressIndicator.IncrementAndUpdate();

                COBieTypeRow typeRow = new COBieTypeRow(types);

                // TODO: Investigate centralising this common code.
                typeRow.Name = type.Name;
                typeRow.CreatedBy = GetTelecomEmailAddress(type.OwnerHistory);
                typeRow.CreatedOn = GetCreatedOnDateAsFmtString(type.OwnerHistory);
                typeRow.Category = GetCategory(type);
                typeRow.Description = GetTypeObjDescription(type);

                typeRow.ExtSystem = ifcApplication.ApplicationFullName;
                typeRow.ExtObject = type.GetType().Name;
                typeRow.ExtIdentifier = type.GlobalId;

                FillPropertySetsValues(allPropertyValues, type, typeRow);

                types.Rows.Add(typeRow);
                
                // Provide Attribute sheet with our context
                //fill in the attribute information
                allPropertyValues.RowParameters["Name"] = typeRow.Name;
                allPropertyValues.RowParameters["CreatedBy"] = typeRow.CreatedBy;
                allPropertyValues.RowParameters["CreatedOn"] = typeRow.CreatedOn;
                allPropertyValues.RowParameters["ExtSystem"] = typeRow.ExtSystem;
                allPropertyValues.SetAttributesRows(type, ref _attributes); //fill attribute sheet rows
            }
            ProgressIndicator.Finalise();
            return types;
        }

        private void FillPropertySetsValues(COBieDataPropertySetValues allPropertyValues, IfcTypeObject type, COBieTypeRow typeRow)
        {
               
            //get related object properties to extract from if main way fails
            allPropertyValues.SetFilteredPropertySingleValues(type, "Pset_Asset");
            typeRow.AssetType =     allPropertyValues.GetFilteredPropertySingleValueValue("AssetAccountingType", false); 
            allPropertyValues.SetFilteredPropertySingleValues(type, "Pset_ManufacturersTypeInformation");
            typeRow.Manufacturer =  allPropertyValues.GetFilteredPropertySingleValueValue("Manufacturer", false);
            typeRow.ModelNumber =   GetModelNumber(type, allPropertyValues);
            
            
            allPropertyValues.SetFilteredPropertySingleValues(type, "Pset_Warranty");
            typeRow.WarrantyGuarantorParts =    GetWarrantyGuarantorParts(type, allPropertyValues);
            typeRow.WarrantyDurationParts =     allPropertyValues.GetFilteredPropertySingleValueValue("WarrantyDurationParts", false);
            typeRow.WarrantyGuarantorLabor =    GetWarrantyGuarantorLabor(type, allPropertyValues);
            typeRow.WarrantyDescription =       GetWarrantyDescription(type, allPropertyValues);
            Interval warrantyDuration =         GetDurationUnitAndValue(allPropertyValues.GetFilteredPropertySingleValue("WarrantyDurationLabor")); 
            typeRow.WarrantyDurationLabor =     warrantyDuration.Value;
            typeRow.WarrantyDurationUnit =      warrantyDuration.Unit;
            typeRow.ReplacementCost =           GetReplacementCost(type, allPropertyValues); 

            allPropertyValues.SetFilteredPropertySingleValues(type, "Pset_ServiceLife");
            Interval serviceDuration =  GetDurationUnitAndValue(allPropertyValues.GetFilteredPropertySingleValue("ServiceLifeDuration"));
            typeRow.ExpectedLife =      GetExpectedLife(type, serviceDuration, allPropertyValues);
            typeRow.DurationUnit =      serviceDuration.Unit;

            allPropertyValues.SetFilteredPropertySingleValues(type, "Pset_Specification");
            typeRow.NominalLength =                 GetNominalLength(type, allPropertyValues);
            typeRow.NominalWidth =                  GetNominalWidth(type, allPropertyValues);
            typeRow.NominalHeight =                 GetNominalHeight(type, allPropertyValues);
            typeRow.ModelReference =                GetModelReference(type, allPropertyValues);
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
            return value;
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
            return value;
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
            return value;
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
            return value;
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
            return value;
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
            return value;
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
            return value;
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
            return value;

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
            return value; 
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
            return value; 
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
            return value; 
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
            return value; 
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
            if ((typeValue.Unit != null) && (typeValue.Unit is IfcConversionBasedUnit))
            {
                IfcConversionBasedUnit conversionBasedUnit = typeValue.Unit as IfcConversionBasedUnit;
                result.Unit = conversionBasedUnit.Name.ToString(); 
            }
            
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
        public string GetCategory(IfcTypeObject type)
        {
            List<string> categories = new List<string> { "OmniClass Table 13 Category", "Category Code" };

            //Try by relationship first
            IfcRelAssociatesClassification classification = type.HasAssociations.OfType<IfcRelAssociatesClassification>().FirstOrDefault();
            if (classification != null)
            {
                IfcClassificationReference classificationRef = (IfcClassificationReference)classification.RelatingClassification;
                return classificationRef.Name;
            }

            //Try by PropertySet as fallback

            var query = from propSet in type.GetAllPropertySets()  
                        from props in propSet.HasProperties
                        where categories.Contains(props.Name.ToString()) 
                        select props.ToString().TrimEnd();
            string val = query.FirstOrDefault();

            //second fall back on objects defined by this type, see if they hold a category on the first related object to this type
            if (string.IsNullOrEmpty(val))
            {
                //get first object defined by this type and try and get category from this object 
                IEnumerable<IfcPropertySingleValue> relAtts = GetTypeObjRelAttributes(type, categories);
                IfcPropertySingleValue singleValue = relAtts.Where(p => categories.Contains(p.Name)).FirstOrDefault();
                if ((singleValue != null) && (singleValue.NominalValue != null)) 
                    return singleValue.NominalValue.ToString();
            }
            else
            {
                return val;
            }

            return Constants.DEFAULT_STRING;
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
