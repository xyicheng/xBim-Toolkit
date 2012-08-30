using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Xbim.COBie.Rows;
using Xbim.Ifc.Extensions;
using Xbim.Ifc.ExternalReferenceResource;
using Xbim.Ifc.HVACDomain;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.PropertyResource;
using Xbim.Ifc.SharedBldgElements;
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
        public COBieSheet<COBieTypeRow> Fill()
        {
            //Create new Sheet
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
                                                            //typeof(Xbim.Ifc.SharedBldgElements.IfcRampType), //IFC2x Edition 4.
                                                            typeof(IfcSlabType),
                                                            typeof(IfcStairFlightType),
                                                            //typeof(IfcStairType), //IFC2x Edition 4.
                                                            typeof(IfcWallType),
                                                            typeof(IfcDuctFittingType ),
                                                            typeof(Xbim.Ifc.ElectricalDomain.IfcJunctionBoxType ),
                                                            typeof(IfcPipeFittingType),
                                                            typeof(Xbim.Ifc.ElectricalDomain.IfcCableCarrierSegmentType),
                                                            typeof(Xbim.Ifc.ElectricalDomain.IfcCableSegmentType),
                                                            typeof(IfcDuctSegmentType),
                                                            typeof(IfcPipeSegmentType),
                                                            typeof(Xbim.Ifc.SharedComponentElements.IfcFastenerType),
                                                            typeof(IfcSpaceType),
                                                             };
            // get all IfcTypeObject objects from IFC file
            IEnumerable<IfcTypeObject> ifcTypeObjects = Model.InstancesOfType<IfcTypeObject>().GroupBy(TypeObj => TypeObj.Name).Select(g => g.First()).Where(ty => !excludedTypes.Contains(ty.GetType()));

            //Stopwatch StopW = new Stopwatch();
            //StopW.Start();
            //list of required attributes
            List<string> AttNames = new List<string> {  "AssetAccountingType", "Manufacturer", "ModelLabel", "WarrantyGuarantorParts", 
                                                        "WarrantyDurationParts", "WarrantyGuarantorLabor", "WarrantyDurationLabor", 
                                                        "ReplacementCost", "ServiceLifeDuration", "WarrantyDescription", "WarrantyDurationUnit" , "NominalLength", "NominalWidth",
                                                        "NominalHeight", "ModelReference", "Shape", "Colour", "Color", "Finish", "Grade", 
                                                        "Material", "Constituents", "Features", "Size", "AccessibilityPerformance", "CodePerformance", 
                                                        "SustainabilityPerformance", "Warranty Information"};
            Dictionary<string, string> DurationAndValue;
            foreach (IfcTypeObject to in ifcTypeObjects)
            {
                COBieTypeRow typ = new COBieTypeRow(types);

                //IfcOwnerHistory ifcOwnerHistory = to.OwnerHistory;

                typ.Name = to.Name;

                typ.CreatedBy = GetTelecomEmailAddress(to.OwnerHistory);
                typ.CreatedOn = GetCreatedOnDateAsFmtString(to.OwnerHistory);


                IfcRelAssociatesClassification ifcRAC = to.HasAssociations.OfType<IfcRelAssociatesClassification>().FirstOrDefault();
                if (ifcRAC != null)
                {
                    IfcClassificationReference ifcCR = (IfcClassificationReference)ifcRAC.RelatingClassification;
                    typ.Category = (string.IsNullOrEmpty(ifcCR.Name)) ? DEFAULT_STRING : ifcCR.Name.ToString();
                }
                else
                    typ.Category = DEFAULT_STRING;

                typ.Description = GetTypeObjDescription(to);



                typ.ExtSystem = GetIfcApplication().ApplicationFullName;
                typ.ExtObject = to.GetType().ToString().Substring(to.GetType().ToString().LastIndexOf('.') + 1);
                typ.ExtIdentifier = to.GlobalId;

                //get related object properties to extract from if main way fails
                IEnumerable<IfcPropertySingleValue> relAtts = GetTypeObjRelAttributes(to, AttNames);

                typ.AssetType = GetTypeObjAttribute(to, "Pset_Asset", "AssetAccountingType", relAtts);
                typ.Manufacturer = GetTypeObjAttribute(to, "Pset_ManufacturersTypeInformation", "Manufacturer", relAtts);
                typ.ModelNumber = GetTypeObjAttribute(to, "Pset_ManufacturersTypeInformation", "ModelLabel", relAtts);
                typ.WarrantyGuarantorParts = GetTypeObjAttribute(to, "Pset_Warranty", "WarrantyGuarantorParts", relAtts);
                typ.WarrantyDurationParts = GetTypeObjAttribute(to, "Pset_Warranty", "WarrantyDurationParts", relAtts);
                typ.WarrantyGuarantorLabour = GetTypeObjAttribute(to, "Pset_Warranty", "WarrantyGuarantorLabor", relAtts);
                DurationAndValue = GetDurationUnitAndValue(to, "Pset_Warranty", "WarrantyDurationLabor", relAtts);
                typ.WarrantyDurationLabour = DurationAndValue["VALUE"];  //GetTypeObjAttribute(to, "Pset_Warranty", "WarrantyDurationLabor", relAtts);
                typ.WarrantyDurationUnit = DurationAndValue["UNIT"];  //GetWarrantyDurationUnit(to, relAtts); 
                typ.ReplacementCost = GetTypeObjAttribute(to, "Pset_EconomicImpactValues", "ReplacementCost", relAtts);
                DurationAndValue = GetDurationUnitAndValue(to, "Pset_ServiceLife", "ServiceLifeDuration", relAtts);
                typ.ExpectedLife = DurationAndValue["VALUE"];// GetTypeObjAttribute(to, "Pset_ServiceLife", "ServiceLifeDuration", relAtts);
                typ.DurationUnit = DurationAndValue["UNIT"];  //GetDurationUnit(to, relAtts);
                typ.WarrantyDescription = GetTypeObjAttribute(to, "Pset_Warranty", "WarrantyDescription", relAtts);

                typ.NominalLength = GetTypeObjAttribute(to, "Pset_Specification", "NominalLength", relAtts);
                typ.NominalLength = ConvertNumberOrDefault(typ.NominalLength); //ensure we comply with a number value
                typ.NominalWidth = GetTypeObjAttribute(to, "Pset_Specification", "NominalWidth", relAtts);
                typ.NominalWidth = ConvertNumberOrDefault(typ.NominalWidth); //ensure we comply with a number value
                typ.NominalHeight = GetTypeObjAttribute(to, "Pset_Specification", "NominalHeight", relAtts);
                typ.NominalHeight = ConvertNumberOrDefault(typ.NominalHeight); //ensure we comply with a number value

                typ.ModelReference = GetTypeObjAttribute(to, "Pset_Specification", "ModelReference", relAtts);
                typ.Shape = GetTypeObjAttribute(to, "Pset_Specification", "Shape", relAtts);
                typ.Size = GetTypeObjAttribute(to, "Pset_Specification", "Size", relAtts);
                typ.Colour = GetTypeObjAttribute(to, "Pset_Specification", "Colour", relAtts);
                if (typ.Colour == DEFAULT_STRING) typ.Colour = GetTypeObjAttribute(to, "Pset_Specification", "Color", relAtts); //try US 'color'
                typ.Finish = GetTypeObjAttribute(to, "Pset_Specification", "Finish", relAtts);
                typ.Grade = GetTypeObjAttribute(to, "Pset_Specification", "Grade", relAtts);
                typ.Material = GetTypeObjAttribute(to, "Pset_Specification", "Material", relAtts);
                typ.Constituents = GetTypeObjAttribute(to, "Pset_Specification", "Constituents", relAtts);
                typ.Features = GetTypeObjAttribute(to, "Pset_Specification", "Features", relAtts);
                typ.AccessibilityPerformance = GetTypeObjAttribute(to, "Pset_Specification", "AccessibilityPerformance", relAtts);
                typ.CodePerformance = GetTypeObjAttribute(to, "Pset_Specification", "CodePerformance", relAtts);
                typ.SustainabilityPerformance = GetTypeObjAttribute(to, "Pset_Specification", "SustainabilityPerformance", relAtts);

                types.Rows.Add(typ);
            }
            //StopW.Stop();
            //Debug.WriteLine(StopW.Elapsed.ToString());
            return types;
        }

        /// <summary>
        /// return the Description for the passed IfcTypeObject object
        /// </summary>
        /// <param name="ds">IfcTypeObject</param>
        /// <returns>Description for Type Object</returns>
        private string GetTypeObjDescription(IfcTypeObject ds)
        {
            if (ds != null)
            {
                if (!string.IsNullOrEmpty(ds.Description)) return ds.Description;
                else if (!string.IsNullOrEmpty(ds.Name)) return ds.Name;
                else
                {
                    //if supports PredefinedType and no description or name then use the predefined type or ElementType if they exist
                    IEnumerable<PropertyInfo> pInfo = ds.GetType().GetProperties(); //get properties

                    if (pInfo.Where(p => p.Name == "PredefinedType").Count() == 1)
                    {
                        string temp = pInfo.First().GetValue(ds, null).ToString(); //get predefindtype as description

                        if (!string.IsNullOrEmpty(temp))
                        {
                            if (temp == "USERDEFINED")
                            {
                                //if used defined then the type description should be in ElementType, so see if property exists
                                if (pInfo.Where(p => p.Name == "ElementType").Count() == 1)
                                {
                                    temp = pInfo.First().GetValue(ds, null).ToString(); //get ElementType
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
        /// Get the IfcPropertySingleValue list for the passed in list of attribute names
        /// 
        /// </summary>
        /// <param name="TypeObj">IfcTypeObject </param>
        /// <param name="AttNames">list of attribute names</param>
        /// <returns>IEnumerable<IfcPropertySingleValue> list of IfcPropertySingleValue which are contained in AttNames</returns>
        private IEnumerable<IfcPropertySingleValue> GetTypeObjRelAttributes(IfcTypeObject TypeObj, List<string> AttNames)
        {
            Dictionary<string, string> keyList = new Dictionary<string, string>();
            //string key = "";
            //string value = DEFAULT_VAL;
            IEnumerable<IfcPropertySingleValue> objProperties = Enumerable.Empty<IfcPropertySingleValue>();
            var objTypeOf = TypeObj.ObjectTypeOf.FirstOrDefault(); //can hold zero or 1 ObjectTypeOf so test return
            if (objTypeOf != null)
            {
                foreach (IfcPropertySet pset in objTypeOf.RelatedObjects.First().GetAllPropertySets()) //has to have 1 ore more so get first and see what we get
                {
                    objProperties = objProperties.Concat(pset.HasProperties.Where<IfcPropertySingleValue>(p => AttNames.Contains(p.Name.ToString())));
                }
            }


            return objProperties;
        }

        /// <summary>
        /// Get the Attribute for a IfcTypeObject
        /// </summary>
        /// <param name="TypeObj">IfcTypeObject </param>
        /// <param name="propSetName">Property Set Name to retrieve IfcPropertySet Object</param>
        /// <param name="propName">Property Name held in IfcPropertySingleValue object</param>
        /// <param name="relAtts">List of IfcPropertySingleValue filtered to attribute names we require</param>
        /// <returns>NominalValue of IfcPropertySingleValue as a string</returns>
        private string GetTypeObjAttribute(IfcTypeObject TypeObj, string propSetName, string propName, IEnumerable<IfcPropertySingleValue> relAtts)
        {
            //try to get the property from the IfcTypeObject
            IfcPropertySingleValue pSngValue = TypeObj.GetPropertySingleValue(propSetName, propName);
            //if null then try and get from a first related object i.e. window type is associated with a window so look at window, should we do this or just return some default value here???
            if (pSngValue == null) pSngValue = relAtts.Where(p => p.Name == propName).FirstOrDefault();
            //if we have a value return the string for input to row field
            if ((pSngValue != null) && (pSngValue.NominalValue != null)) return pSngValue.NominalValue.ToString();
            else return DEFAULT_STRING; //nothing found return default

            //if null then try and get from all related object i.e. 
            //if (pSngValue == null)
            //{
            //    foreach (IfcPropertySingleValue pSV in relAtts.Where(p => p.Name == propName))
            //    {
            //        if (pSV != null)
            //        {
            //            pSngValue = pSV;
            //            break; //we have a value so break
            //        }
            //    }
            //}
        }

        /// <summary>
        /// Get the Time unit and value for the passed in property
        /// </summary>
        /// <param name="TypeObj">IfcTypeObject </param>
        /// <param name="propSetName">Property Set Name to retrieve IfcPropertySet Object</param>
        /// <param name="propName">Property Name held in IfcPropertySingleValue object</param>
        /// <param name="relAtts">List of IfcPropertySingleValue filtered to attribute names we require</param>
        /// <returns>Dictionary holding unit and value e.g. Year, 2.0</returns>
        private Dictionary<string, string> GetDurationUnitAndValue(IfcTypeObject TypeObj, string propSetName, string propName, IEnumerable<IfcPropertySingleValue> relAtts)
        {
            const string Default = "Second"; //n/a is not acceptable, so lets how a value which is obviously incorrect
            Dictionary<string, string> Ret = new Dictionary<string, string>() { { "UNIT", Default }, { "VALUE", Default } };
            //try to get the property from the IfcTypeObject
            IfcPropertySingleValue pSngValue = TypeObj.GetPropertySingleValue(propSetName, propName);
            //if null then try and get from a first related object i.e. window type is associated with a window so look at window, should we do this or just return some default value here???
            if (pSngValue == null) pSngValue = relAtts.Where(p => p.Name == propName).FirstOrDefault();
            double convert = 0.0;
            //Get the unit type
            if ((pSngValue != null) && (pSngValue.Unit is IfcConversionBasedUnit) && (pSngValue.Unit != null))
            {
                Ret["UNIT"] = ((IfcConversionBasedUnit)pSngValue.Unit).Name.ToString(); //get unit type name
                convert = (double)((IfcConversionBasedUnit)pSngValue.Unit).ConversionFactor.ValueComponent.Value; //get the conversion factor value to work out time period
            }
            else if ((pSngValue != null) && (pSngValue.NominalValue != null))   //no IfcConversionBasedUnit so go for "Warranty Information" prop. then value for passed in propName
            {
                if ((pSngValue != null) && (propName.Contains("Warranty")))
                {
                    IfcPropertySingleValue pSngUnit = relAtts.Where(p => p.Name == "Warranty Information").FirstOrDefault(); //appears in clinic file so grab
                    if (pSngUnit != null) Ret["UNIT"] = pSngUnit.NominalValue.Value.ToString();
                }
                else
                    Ret["UNIT"] = pSngValue.NominalValue.Value.ToString(); // value for passed in propName

            }
            //Get the time period value
            if ((pSngValue != null) && (pSngValue.NominalValue != null) && (pSngValue.NominalValue is IfcReal)) //if a number then we can calculate
            {
                double val = (double)pSngValue.NominalValue.Value; //get value
                if (convert > 0.0) val = val / convert; //convert if we have a convert value
                Ret["VALUE"] = val.ToString("F1");
            }
            else if ((pSngValue != null) && (pSngValue.NominalValue != null)) //no number value so just show value for passed in propName
                Ret["VALUE"] = pSngValue.NominalValue.Value.ToString();

            return Ret;
        }
        #endregion
    }
}
