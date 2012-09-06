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
    public class COBieDataType : COBieData
    {
        /// <summary>
        /// Data Type constructor
        /// </summary>
        /// <param name="model">IModel to read data from</param>
        public COBieDataType(IModel model)
        {
            Model = model;
        }

        #region Methods

        /// <summary>
        /// Fill sheet rows for Type sheet
        /// </summary>
        /// <returns>COBieSheet<COBieTypeRow></returns>
        public COBieSheet<COBieTypeRow> Fill(ref COBieSheet<COBieAttributeRow> attributes)
        {
            // Create new Sheet
            COBieSheet<COBieTypeRow> types = new COBieSheet<COBieTypeRow>(Constants.WORKSHEET_TYPE);
            
            //TODO: after IfcRampType and IfcStairType are implemented then add to excludedTypes list
            List<Type> excludedTypes = new List<Type>{  typeof(IfcTypeProduct),
                                                            typeof(IfcElementType),
                                                            typeof(IfcBeamType),
                                                            typeof(IfcColumnType),
                                                            typeof(IfcCurtainWallType),
                                                            typeof(IfcMemberType),
                                                            typeof(IfcPlateType),
                                                            typeof(IfcRailingType),
                                                            typeof(IfcRampFlightType),
                                                            typeof(IfcSlabType),
                                                            typeof(IfcStairFlightType),
                                                            typeof(IfcWallType),
                                                            typeof(IfcDuctFittingType ),
                                                            typeof(IfcJunctionBoxType ),
                                                            typeof(IfcPipeFittingType),
                                                            typeof(IfcCableCarrierSegmentType),
                                                            typeof(IfcCableSegmentType),
                                                            typeof(IfcDuctSegmentType),
                                                            typeof(IfcPipeSegmentType),
                                                            typeof(IfcFastenerType),
                                                            typeof(IfcSpaceType),
                                                            //typeof(Xbim.Ifc.SharedBldgElements.IfcRampType), //IFC2x Edition 4.
                                                            //typeof(IfcStairType), //IFC2x Edition 4.
                                                             };
            // get all IfcTypeObject objects from IFC file
            IEnumerable<IfcTypeObject> ifcTypeObjects = Model.InstancesOfType<IfcTypeObject>()
                .Select(type => type)
                .Where(type => !excludedTypes.Contains(type.GetType()));

            COBieDataPropertySetValues allPropertyValues = new COBieDataPropertySetValues(ifcTypeObjects.ToList()); //properties helper class
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

            foreach (IfcTypeObject type in ifcTypeObjects)
            {
                COBieTypeRow typeRow = new COBieTypeRow(types);

                // TODO: Investigate centralising this common code.
                typeRow.Name = type.Name;
                typeRow.CreatedBy = GetTelecomEmailAddress(type.OwnerHistory);
                typeRow.CreatedOn = GetCreatedOnDateAsFmtString(type.OwnerHistory);
                typeRow.Category = GetCategory(type);
                typeRow.Description = GetTypeObjDescription(type);

                typeRow.ExtSystem = GetIfcApplication().ApplicationFullName;
                typeRow.ExtObject = type.GetType().Name;
                typeRow.ExtIdentifier = type.GlobalId;

                FillPropertySetsValues(candidateProperties, type, typeRow);

                types.Rows.Add(typeRow);
                
                // Provide Attribute sheet with our context
                Dictionary<string, string> sourceData = new Dictionary<string, string>(){{"Sheet", "Type"}, 
                                                                                          {"Name", typeRow.Name},
                                                                                          {"CreatedBy", typeRow.CreatedBy},
                                                                                          {"CreatedOn", typeRow.CreatedOn},
                                                                                          {"ExtSystem", typeRow.ExtSystem}
                                                                                          };
                //add *ALL* the attributes to the passed attributes sheet except property names that we've handled, or are on the exclusion list
                SetAttributeSheet(type, sourceData, excludePropertyValueNames, excludePropertyValueNamesWildcard, null, ref attributes);
            }
           
            return types;
        }

        private void FillPropertySetsValues(List<string> candidateProperties, IfcTypeObject type, COBieTypeRow typeRow)
        {
            //get related object properties to extract from if main way fails
            IEnumerable<IfcPropertySingleValue> attributes = GetTypeObjRelAttributes(type, candidateProperties);

            typeRow.AssetType = GetAttribute(type, "Pset_Asset", "AssetAccountingType", attributes);
            typeRow.Manufacturer = GetAttribute(type, "Pset_ManufacturersTypeInformation", "Manufacturer", attributes);
            typeRow.ModelNumber = GetAttribute(type, "Pset_ManufacturersTypeInformation", "ModelLabel", attributes);
            // TODO: Get properties from PSets rather than one by one.
            typeRow.WarrantyGuarantorParts = GetAttribute(type, "Pset_Warranty", "WarrantyGuarantorParts", attributes);
            typeRow.WarrantyDurationParts = GetAttribute(type, "Pset_Warranty", "WarrantyDurationParts", attributes);
            typeRow.WarrantyGuarantorLabor = GetAttribute(type, "Pset_Warranty", "WarrantyGuarantorLabor", attributes);
            typeRow.WarrantyDescription = GetAttribute(type, "Pset_Warranty", "WarrantyDescription", attributes);
            Interval warrantyDuration = GetDurationUnitAndValue(type, "Pset_Warranty", "WarrantyDurationLabor", attributes);
            typeRow.WarrantyDurationLabor = warrantyDuration.Value;
            typeRow.WarrantyDurationUnit = warrantyDuration.Unit;

            typeRow.ReplacementCost = GetAttribute(type, "Pset_EconomicImpactValues", "ReplacementCost", attributes);
            Interval serviceDuration = GetDurationUnitAndValue(type, "Pset_ServiceLife", "ServiceLifeDuration", attributes);
            typeRow.ExpectedLife = serviceDuration.Value;
            typeRow.DurationUnit = serviceDuration.Unit;
            

            typeRow.NominalLength = ConvertNumberOrDefault(GetAttribute(type, "Pset_Specification", "NominalLength", attributes));
            typeRow.NominalWidth = ConvertNumberOrDefault(GetAttribute(type, "Pset_Specification", "NominalWidth", attributes));
            typeRow.NominalHeight = ConvertNumberOrDefault(GetAttribute(type, "Pset_Specification", "NominalHeight", attributes));
            typeRow.ModelReference = GetAttribute(type, "Pset_Specification", "ModelReference", attributes);
            typeRow.Shape = GetAttribute(type, "Pset_Specification", "Shape", attributes);
            typeRow.Size = GetAttribute(type, "Pset_Specification", "Size", attributes);
            typeRow.Color = GetAttribute(type, "Pset_Specification", "Colour", attributes);
            if (typeRow.Color == DEFAULT_STRING)
                typeRow.Color = GetAttribute(type, "Pset_Specification", "Color", attributes);
            typeRow.Finish = GetAttribute(type, "Pset_Specification", "Finish", attributes);
            typeRow.Grade = GetAttribute(type, "Pset_Specification", "Grade", attributes);
            typeRow.Material = GetAttribute(type, "Pset_Specification", "Material", attributes);
            typeRow.Constituents = GetAttribute(type, "Pset_Specification", "Constituents", attributes);
            typeRow.Features = GetAttribute(type, "Pset_Specification", "Features", attributes);
            typeRow.AccessibilityPerformance = GetAttribute(type, "Pset_Specification", "AccessibilityPerformance", attributes);
            typeRow.CodePerformance = GetAttribute(type, "Pset_Specification", "CodePerformance", attributes);
            typeRow.SustainabilityPerformance = GetAttribute(type, "Pset_Specification", "SustainabilityPerformance", attributes);
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
        /// Get the Attribute for a IfcTypeObject
        /// </summary>
        /// <param name="typeObj">IfcTypeObject </param>
        /// <param name="propSetName">Property Set Name to retrieve IfcPropertySet Object</param>
        /// <param name="propName">Property Name held in IfcPropertySingleValue object</param>
        /// <param name="relAtts">List of IfcPropertySingleValue filtered to attribute names we require</param>
        /// <returns>NominalValue of IfcPropertySingleValue as a string</returns>
        private string GetAttribute(IfcTypeObject typeObj, string propSetName, string propName, IEnumerable<IfcPropertySingleValue> relAtts)
        {
            // try to get the property from the IfcTypeObject
            IfcPropertySingleValue singleValue = typeObj.GetPropertySingleValue(propSetName, propName);
            // if null then try and get from a first related object i.e. window type is associated with a window so look at window
            // TODO: should we do this or just return some default value here???
            if (singleValue == null) 
                singleValue = relAtts.Where(p => p.Name == propName).FirstOrDefault();
            //if we have a value return the string for input to row field
            if ((singleValue != null) && (singleValue.NominalValue != null)) 
                return singleValue.NominalValue.ToString();
            else 
                return DEFAULT_STRING; //nothing found return default


        }

        /// <summary>
        /// Get the Time unit and value for the passed in property
        /// </summary>
        /// <param name="typeObject">IfcTypeObject </param>
        /// <param name="psetName">Property Set Name to retrieve IfcPropertySet Object</param>
        /// <param name="propertyName">Property Name held in IfcPropertySingleValue object</param>
        /// <param name="psetValues">List of IfcPropertySingleValue filtered to attribute names we require</param>
        /// <returns>Dictionary holding unit and value e.g. Year, 2.0</returns>
        private Interval GetDurationUnitAndValue(IfcTypeObject typeObject, string psetName, string propertyName, IEnumerable<IfcPropertySingleValue> psetValues)
        {
            const string DefaultUnit = "Year"; // n/a is not acceptable, so create a valid default

            Interval result = new Interval() { Value = DEFAULT_NUMERIC, Unit = DefaultUnit };
            // try to get the property from the Type first
            IfcPropertySingleValue typeValue = typeObject.GetPropertySingleValue(psetName, propertyName);

            // TODO: Check this logic
            // if null then try and get from first instance of this type
            if (typeValue == null) 
                typeValue = psetValues.Where(p => p.Name == propertyName).FirstOrDefault();

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

            return DEFAULT_STRING;
        }
        #endregion
    }

    public struct Interval
    {
        public string Value { get; set; }
        public string Unit { get; set; }
    }
}
