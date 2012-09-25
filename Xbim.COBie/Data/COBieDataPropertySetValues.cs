using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.PropertyResource;
using System.Collections;
using Xbim.COBie.Rows;
using Xbim.Ifc.MeasureResource;
using Xbim.XbimExtensions.DataProviders;
using Xbim.Ifc.ExternalReferenceResource;
using Xbim.Ifc.SelectTypes;



namespace Xbim.COBie.Data
{
    
    public class COBieDataPropertySetValues 
    {
        #region Fields
        Dictionary<IfcObject, Dictionary<IfcPropertySet, List<IfcSimpleProperty>>> _propSetsValuesObjects = null;
        Dictionary<IfcTypeObject, Dictionary<IfcPropertySet, List<IfcSimpleProperty>>> _propSetsValuesTypeObjects = null;
        Dictionary<IfcTypeObject, Dictionary<IfcPropertySet, List<IfcSimpleProperty>>> _propSetsValuesTypeObjectsFirstRelatedObject = null;
        List<IfcSimpleProperty> _ifcPropertySingleValues = null;

        /// <summary>
        /// List of property names that are to be excluded from Attributes sheet with equal compare
        /// </summary>
        protected List<string> _commonAttExcludesEq = new List<string>()
        {   "MethodOfMeasurement",  "Omniclass Number",     "Assembly Code",                "Assembly Description",     "Uniclass Description",     "Uniclass Code", 
            "Category Code",    "Category Description",     "Classification Description",   "Classification Code",      "Name",                     "Description", 
            "Hot Water Radius", "Host",                     "Limit Offset",                 "Recepticles",              "Mark"
            
            //"Zone Base Offset", "Upper Limit",   "Line Pattern", "Symbol","Window Inset", "Radius", "Phase Created","Phase", //old ones might need to put back in
        };

        

        /// <summary>
        /// List of property names that are to be excluded from Attributes sheet with start with compare
        /// </summary>
        protected List<string> _commonAttExcludesStartWith = new List<string>()
        {   "Omniclass Title",  "Half Oval",    "Level",    "Outside Diameter", "Outside Radius"
        };

        

        /// <summary>
        /// List of property names that are to be excluded from Attributes sheet with contains with compare
        /// </summary>
        protected List<string> _commonAttExcludesContains = new List<string>()
        {   "AssetAccountingType",  "GSA BIM Area",     "Height",   "Length",   "Size",     "Lighting Calculation Workplan",    "Offset",   "Omniclass"
        };

        
        #endregion

        #region Properties

        /// <summary>
        /// Exclude property single value names from selection in SetAttributes functions where the Name property equals an item in this list
        /// </summary>
        public List<string>  ExcludePropertyValueNames { get; private set; }

        /// <summary>
        /// Exclude property single value names from selection in SetAttributes functions which contain the strings held in this list
        /// </summary>
        public List<string> ExcludePropertyValueNamesWildcard { get; private set; }

        /// <summary>
        /// Exclude property single value names from selection in SetAttributes functions which contain the strings held in this list
        /// </summary>
        public List<string> ExcludePropertyValueNamesStartingWith { get; private set; }
        /// <summary>
        /// Exclude property set names from selection in SetAttributes functions
        /// </summary>
        public List<string> ExcludePropertySetNames { get; private set; }

        /// <summary>
        /// Value passed from sheet to attribute sheet
        /// </summary>
        public Dictionary<string, string> RowParameters
        { get; private set; }

        /// <summary>
        /// Filtered property single value list
        /// </summary>
        public List<IfcSimpleProperty> FilteredPropertySingleValues
        {
            get { return _ifcPropertySingleValues; }
        }
        
        #endregion

        #region Methods

                
        /// <summary>
        /// constructor for IfcObject types
        /// </summary>
        /// <param name="sourceRows"></param>
        public COBieDataPropertySetValues(IEnumerable<IfcObject> sourceRows)
        {
            _propSetsValuesObjects = sourceRows.ToDictionary(el => el, el => el.PropertySets
                .ToDictionary(ps => ps, ps => ps.HasProperties.OfType<IfcSimpleProperty>().ToList()));
            //set up lists
            SetListsUp();
        }

        /// <summary>
        /// constructor for IfcObject types
        /// </summary>
        /// <param name="sourceRows"></param>
        public COBieDataPropertySetValues(IEnumerable<IfcTypeObject> sourceRows)
        {
            _propSetsValuesTypeObjects = sourceRows.Where(tObj => (tObj.HasPropertySets != null)).ToDictionary(tObj => tObj, tObj => tObj.HasPropertySets.OfType<IfcPropertySet>()
                .ToDictionary(ps => ps, ps => ps.HasProperties.OfType<IfcSimpleProperty>().ToList()));
            //===========get related properties==================

            //Get the first related object and property sets and property single values for the IfcTypeObject
            _propSetsValuesTypeObjectsFirstRelatedObject = sourceRows.ToDictionary(tObj => tObj, tObj => tObj.ObjectTypeOf.First().RelatedObjects.First().IsDefinedByProperties.Select(ps => ps.RelatingPropertyDefinition).OfType<IfcPropertySet>()
                                                                                                                       .ToDictionary(ps => ps, ps => ps.HasProperties.OfType<IfcSimpleProperty>().ToList()));

            //set up lists
            SetListsUp();           
        }



        /// <summary>
        /// Set the property lists up 
        /// </summary>
        private void SetListsUp()
        {
            _ifcPropertySingleValues = new List<IfcSimpleProperty>();
            //Set up passed values dictionary
            RowParameters = new Dictionary<string, string>();
            RowParameters.Add("Sheet", Constants.DEFAULT_STRING);
            RowParameters.Add("Name", Constants.DEFAULT_STRING);
            RowParameters.Add("CreatedBy", Constants.DEFAULT_STRING);
            RowParameters.Add("CreatedOn", Constants.DEFAULT_STRING);
            RowParameters.Add("ExtSystem", Constants.DEFAULT_STRING);
            //set up lists
            ExcludePropertyValueNames = new List<string>();
            ExcludePropertyValueNames.AddRange(_commonAttExcludesEq);
            ExcludePropertyValueNamesWildcard = new List<string>();
            ExcludePropertyValueNamesWildcard.AddRange(_commonAttExcludesContains);
            ExcludePropertyValueNamesStartingWith = new List<string>();
            ExcludePropertyValueNamesStartingWith.AddRange(_commonAttExcludesStartWith);
            ExcludePropertySetNames = new List<string>();
            
        }

        /// <summary>
        /// Indexer for COBieDataPropertySetValues class
        /// </summary>
        /// <param name="ifcObject"> IfcObject key for indexer</param>
        /// <returns></returns>
        public Dictionary<IfcPropertySet, List<IfcSimpleProperty>> this[IfcObject ifcObject]
        {
            get
            {
                if (_propSetsValuesObjects.ContainsKey(ifcObject))
                    return _propSetsValuesObjects[ifcObject];
                else
                    return null;
            }
        }

        /// <summary>
        /// Indexer for COBieDataPropertySetValues class
        /// </summary>
        /// <param name="ifcTypeObject"> IfcTypeObject key for indexer</param>
        /// <returns></returns>
        public Dictionary<IfcPropertySet, List<IfcSimpleProperty>> this[IfcTypeObject ifcTypeObject]
        {
            get
            {
                if (_propSetsValuesTypeObjects.ContainsKey(ifcTypeObject))
                    return _propSetsValuesTypeObjects[ifcTypeObject];
                else
                    return null;
            }
        }

         /// <summary>
        /// Get the related entity properties for the IfcTypeObject
        /// </summary>
        /// <param name="ifcTypeObject"> IfcTypeObject </param>
        /// <returns>Dictionary of IfcPropertySet keyed to List of IfcPropertySingleValue</returns>
        public Dictionary<IfcPropertySet, List<IfcSimpleProperty>> GetRelatedProperties(IfcTypeObject ifcTypeObject)
        {
            if (_propSetsValuesTypeObjectsFirstRelatedObject.ContainsKey(ifcTypeObject))
             return _propSetsValuesTypeObjectsFirstRelatedObject[ifcTypeObject];
            else
             return null;
        }
       
         
        /// <summary>
        /// Set the single property values held for the IfcObject into _ifcPropertySingleValues field
        /// </summary>
        /// <param name="ifcObject">IfcObject holding the single property values</param>
        public void SetFilteredPropertySingleValues (IfcObject ifcObject)
        {
            _ifcPropertySingleValues = (from dic in _propSetsValuesObjects[ifcObject]
                        from psetval in dic.Value //list of IfcPropertySingleValue
                        select psetval).ToList();
        }

        /// <summary>
        /// Set the single property values held for the IfcObject into _ifcPropertySingleValues field
        /// </summary>
        /// <param name="ifcObject">IfcObject holding the single property values</param>
        public void SetFilteredPropertySingleValues(IfcTypeObject ifcObject)
        {
            _ifcPropertySingleValues.Clear(); //reset

            if (_propSetsValuesTypeObjects.ContainsKey(ifcObject))
            {
                _ifcPropertySingleValues = (from dic in _propSetsValuesTypeObjects[ifcObject]
                                                from psetval in dic.Value //list of IfcPropertySingleValue
                                                select psetval).ToList();
            }
            //fall back to related items to get the information from
            if (_ifcPropertySingleValues.Count() == 0)
            {
                //get first related object properties of the passed in object
                GetRelatedObjectProperties(ifcObject); 
            }
        }

        
        /// <summary>
        /// Set the single property values held for the IfcObject into _ifcPropertySingleValues field 
        /// filtered by a IfcPropertySet name
        /// </summary>
        /// <param name="ifcTypeObject">IfcObject holding the single property values</param>
        /// <param name="propertySetName">IfcPropertySetName</param>
        public void SetFilteredPropertySingleValues(IfcTypeObject ifcTypeObject, string propertySetName)
        {
            _ifcPropertySingleValues.Clear(); //reset

            if (_propSetsValuesTypeObjects.ContainsKey(ifcTypeObject))
            {
                _ifcPropertySingleValues = (from dic in _propSetsValuesTypeObjects[ifcTypeObject]
                                                where (dic.Key.Name == propertySetName)
                                                from psetval in dic.Value //list of IfcPropertySingleValue
                                                select psetval).ToList();
            }
            //fall back to related items to get the information from
            if (_ifcPropertySingleValues.Count() == 0)
            {
                //get first related object properties of the passed in object
                GetRelatedObjectProperties(ifcTypeObject); //we do not filter on property set name here, just go for all of them
            }
        }

               
        /// <summary>
        /// Get the property value where the property name equals the passed in value  
        /// </summary>
        /// <param name="PropertyValueName">IfcPropertySingleValue name</param>
        /// <param name="containsString">Do Contains text match on PropertyValueName if true, exact match if false</param>
        /// <returns></returns>
        public string GetFilteredPropertySingleValueValue(string PropertyValueName, bool containsString)
        {
            IfcPropertySingleValue ifcPropertySingleValue = null;
            if (containsString)
                ifcPropertySingleValue = _ifcPropertySingleValues.Where(p => p.Name.ToString().Contains(PropertyValueName)).OfType<IfcPropertySingleValue>().FirstOrDefault();
            else
                ifcPropertySingleValue = _ifcPropertySingleValues.Where(p => p.Name == PropertyValueName).OfType<IfcPropertySingleValue>().FirstOrDefault();

            //return a string value
            if ((ifcPropertySingleValue != null) && 
                (ifcPropertySingleValue.NominalValue != null) && 
                (!string.IsNullOrEmpty(ifcPropertySingleValue.NominalValue.Value.ToString())) && 
                (ifcPropertySingleValue.Name.ToString() != ifcPropertySingleValue.NominalValue.Value.ToString()) //name and vale are not the same
                ) 
            return ifcPropertySingleValue.NominalValue.Value.ToString();
            else
                return Constants.DEFAULT_STRING;
        

        }

        /// <summary>
        /// Get the property value where the property name equals the passed in value 
        /// </summary>
        /// <param name="PropertyValueName"></param>
        /// <returns></returns>
        public IfcPropertySingleValue GetFilteredPropertySingleValue(string PropertyValueName)
        {
            return _ifcPropertySingleValues.Where(p => p.Name == PropertyValueName).OfType<IfcPropertySingleValue>().FirstOrDefault();
        }

        /// <summary>
        /// Set values for attribute sheet
        /// </summary>
        /// <param name="ifcObject">ifcObject to extract properties from</param>
        /// <param name="attributes">The attribute Sheet to add the properties to its rows</param>
        public void PopulateAttributesRows(IfcObject ifcObject, ref COBieSheet<COBieAttributeRow> attributes)
        {

            foreach (KeyValuePair<IfcPropertySet, List<IfcSimpleProperty>> pairValues in _propSetsValuesObjects[ifcObject])
            {
                IfcPropertySet ps = pairValues.Key; //get Property Set
                //get all property attached to the property set
                 //check property set exclude list
                if (!string.IsNullOrEmpty(ps.Name))
                {
                    if (ExcludePropertySetNames.Count() > 0)
                    {
                        if (ExcludePropertySetNames.Contains(ps.Name))
                        {
                            continue; //skip this loop iteration if property set name matches exclude list item
                        }
                    }
                }

                //Get Property SetAttribSheet Property Single Values
                IEnumerable<IfcSimpleProperty> pSVs = pairValues.Value; 
                //filter on ExcludePropertyValueNames and ExcludePropertyValueNamesWildcard
                pSVs = FilterRows(pSVs);
                //fill in the data to the attribute rows
                ProcessAttributeRow(attributes, ps, pSVs);
            }
            
        }

        /// <summary>
        /// Set values for attribute sheet
        /// </summary>
        /// <param name="ifcTypeObject">ifcObject to extract properties from</param>
        /// <param name="attributes">The attribute Sheet to add the properties to its rows</param>
        public void PopulateAttributesRows(IfcTypeObject ifcTypeObject, ref COBieSheet<COBieAttributeRow> attributes)
        {
            if (_propSetsValuesTypeObjects.ContainsKey(ifcTypeObject))
            {
                foreach (KeyValuePair<IfcPropertySet, List<IfcSimpleProperty>> pairValues in _propSetsValuesTypeObjects[ifcTypeObject])
                {
                    IfcPropertySet ps = pairValues.Key; //get Property Set
                    //get all property attached to the property set
                    //check property set exclude list
                    if (!string.IsNullOrEmpty(ps.Name))
                    {
                        if (ExcludePropertySetNames.Count() > 0)
                        {
                            if (ExcludePropertySetNames.Contains(ps.Name))
                            {
                                continue; //skip this loop iteration if property set name matches exclude list item
                            }
                        }
                    }

                    IEnumerable<IfcSimpleProperty> pSVs = pairValues.Value; //Get Property SetAttribSheet Property Single Values
                    //filter on ExcludePropertyValueNames and ExcludePropertyValueNamesWildcard
                    pSVs = FilterRows(pSVs);
                    //fill in the data to the attribute rows
                    ProcessAttributeRow(attributes, ps, pSVs);
                }
            }

        }
        /// <summary>
        /// Apply filter lists to propertySingleValue selection
        /// </summary>
        /// <param name="pSVs">IEnumerable of IfcPropertySingleValue</param>
        /// <returns>IEnumerable of IfcPropertySingleValue</returns>
        private IEnumerable<IfcSimpleProperty> FilterRows(IEnumerable<IfcSimpleProperty> pSVs)
        {
            //filter for excluded properties, full name
            if (ExcludePropertyValueNames.Count() > 0)
            {
                //ExcludePropValNames = ExcludePropValNames.ConvertAll(d => d.ToLower()); //lowercase the strings in the list
                pSVs = from pVS in pSVs
                       where !ExcludePropertyValueNames.Contains(pVS.Name.ToString())
                       select pVS;
            }
            //filter out the Property names that contain a string from the list excPropWC
            if (ExcludePropertyValueNamesWildcard.Count() > 0)
            {
                //excPropWC = excPropWC.ConvertAll(d => d.ToLower()); //lowercase the strings in the list
                pSVs = from pVS in pSVs
                       where ((from item in ExcludePropertyValueNamesWildcard
                               where pVS.Name.ToString().Contains(item)
                               select item).Count() == 0)
                       select pVS;
            }
            //filter out the Property names that contain a string from the list excPropWC
            if (ExcludePropertyValueNamesStartingWith.Count() > 0)
            {
                //excPropWC = excPropWC.ConvertAll(d => d.ToLower()); //lowercase the strings in the list
                pSVs = from pVS in pSVs
                       where ((from item in ExcludePropertyValueNamesStartingWith
                               where ((pVS.Name != null) && 
                                       (pVS.Name.ToString().Length >= item.Length) &&  
                                       (pVS.Name.ToString().Substring(0, item.Length) == item )
                                       ) //starts with the string
                               select item).Count() == 0)
                       select pVS;
            }
            
            return pSVs;
        }

        /// <summary>
        /// Add Rows to the attribute sheet
        /// </summary>
        /// <param name="attributes">The attribute Sheet to add the properties to its rows</param>
        /// <param name="propertySet">IfcPropertySet which is holding the IfcPropertySingleValue</param>
        /// <param name="propertySetValues">IEnumerable list of IfcPropertySingleValue to extract to the attribute sheet</param>
        private void ProcessAttributeRow(COBieSheet<COBieAttributeRow> attributes, IfcPropertySet propertySet, IEnumerable<IfcSimpleProperty> propertySetValues)
        {
            //construct the rows
            foreach (IfcSimpleProperty propertySetSimpleProperty in propertySetValues)
            {
                if (propertySetSimpleProperty != null)
                {
                    string value = "";
                    string name = propertySetSimpleProperty.Name.ToString();


                      
                    if (string.IsNullOrEmpty(name))
                    {
                        continue; //skip to next loop item
                    }

                    IEnumerable<COBieAttributeRow> TestRow = attributes.Rows.Where(r => r.Name == name && r.SheetName == RowParameters["Sheet"] && r.RowName == RowParameters["Name"]);
                    if (TestRow.Any()) continue; //skip to next loop item
                    
                    //check what type we of property we have
                    IfcPropertySingleValue ifcPropertySingleValue = propertySetSimpleProperty as IfcPropertySingleValue;
                    //get value
                    if (ifcPropertySingleValue != null)
                    {
                        if (ifcPropertySingleValue.NominalValue != null)
                        {
                            value = ifcPropertySingleValue.NominalValue.Value.ToString();
                            double num;
                            if (double.TryParse(value, out num)) value = num.ToString("F3");
                            if ((string.IsNullOrEmpty(value)) || (string.Compare(value, ifcPropertySingleValue.Name.ToString(), true) == 0) || (string.Compare(value, "default", true) == 0))
                            {
                                continue; //skip to next loop item
                            }
                            
                        }
                       
                    }

                    COBieAttributeRow attribute = new COBieAttributeRow(attributes);
                    attribute.Unit = Constants.DEFAULT_STRING; //set initially to default, saves the else statements
                    attribute.AllowedValues = Constants.DEFAULT_STRING;
                    attribute.Description = Constants.DEFAULT_STRING;
                    
                    if (ifcPropertySingleValue != null) //as we can skip on ifcPropertySingleValue we need to split ifcPropertySingleValue testing
                    {
                        if ((ifcPropertySingleValue.Unit != null))
                        {
                            attribute.Unit = COBieData<COBieAttributeRow>.GetUnit(ifcPropertySingleValue.Unit);
                            if (ifcPropertySingleValue.Unit is IfcNamedUnit)
                                attribute.AllowedValues = ((IfcNamedUnit)ifcPropertySingleValue.Unit).UnitType.ToString();
                            
                        }
                    }

                    //Process properties that are not IfcPropertySingleValue
                    IfcPropertyEnumeratedValue ifcPropertyEnumeratedValue = propertySetSimpleProperty as IfcPropertyEnumeratedValue;
                    if (ifcPropertyEnumeratedValue != null)
                    {
                        string EnumValuesHeld = "";
                        if (ifcPropertyEnumeratedValue.EnumerationValues != null)
                        {
                           value = GetEnumerationValues(ifcPropertyEnumeratedValue.EnumerationValues);
                        }
                        
                        //get  the unit and all possible values held in the Enumeration
                        if (ifcPropertyEnumeratedValue.EnumerationReference != null)
                        {
                            if (ifcPropertyEnumeratedValue.EnumerationReference.Unit != null)
                            {
                                attribute.Unit = COBieData<COBieAttributeRow>.GetUnit(ifcPropertyEnumeratedValue.EnumerationReference.Unit);
                            }
                            EnumValuesHeld = GetEnumerationValues(ifcPropertyEnumeratedValue.EnumerationReference.EnumerationValues);
                            if (!string.IsNullOrEmpty(EnumValuesHeld)) attribute.AllowedValues = EnumValuesHeld;
                        }
                    }

                    IfcPropertyBoundedValue ifcPropertyBoundedValue = propertySetSimpleProperty as IfcPropertyBoundedValue;
                    if (ifcPropertyBoundedValue != null)
                    {
                        //combine upper and lower into the value field
                        if (ifcPropertyBoundedValue.UpperBoundValue != null)
                            value = ifcPropertyBoundedValue.UpperBoundValue.ToString();
                        if (ifcPropertyBoundedValue.LowerBoundValue != null)
                        {
                            if (!string.IsNullOrEmpty(value))
                                value += " : " + ifcPropertyBoundedValue.LowerBoundValue.ToString();
                            else
                                value = ifcPropertyBoundedValue.LowerBoundValue.ToString();
                        }
                        
                        if ((ifcPropertyBoundedValue.Unit != null))
                        {
                            attribute.Unit = COBieData<COBieAttributeRow>.GetUnit(ifcPropertyBoundedValue.Unit);
                            if (ifcPropertySingleValue.Unit is IfcNamedUnit)
                                attribute.AllowedValues = ((IfcNamedUnit)ifcPropertySingleValue.Unit).UnitType.ToString();
                            
                        }
                        
                    }

                    IfcPropertyTableValue ifcPropertyTableValue = propertySetSimpleProperty as IfcPropertyTableValue;
                    if (ifcPropertyTableValue != null)
                    {
                        throw new NotImplementedException("ProcessAttributeRow: IfcPropertyTableValue not implemented");
                    }

                    IfcPropertyReferenceValue ifcPropertyReferenceValue = propertySetSimpleProperty as IfcPropertyReferenceValue;
                    if (ifcPropertyReferenceValue != null)
                    {
                        if (ifcPropertyReferenceValue.UsageName != null)
                            attribute.Description = (string.IsNullOrEmpty(ifcPropertyReferenceValue.UsageName.ToString())) ? Constants.DEFAULT_STRING : ifcPropertyReferenceValue.UsageName.ToString();

                        if (ifcPropertyReferenceValue.PropertyReference != null)
                        {
                            value = ifcPropertyReferenceValue.PropertyReference.ToString();
                            attribute.Unit = ifcPropertyReferenceValue.PropertyReference.GetType().Name;
                        }
                        
                    }
                    IfcPropertyListValue ifcPropertyListValue = propertySetSimpleProperty as IfcPropertyListValue;
                    if (ifcPropertyListValue != null)
                    {
                        if (ifcPropertyListValue.ListValues != null)
                        {
                            value = GetEnumerationValues(ifcPropertyListValue.ListValues);
                        }
                        
                        //get  the unit and all possible values held in the Enumeration
                        if (ifcPropertyListValue.Unit != null)
                            attribute.Unit = COBieData<COBieAttributeRow>.GetUnit(ifcPropertyListValue.Unit);
                        

                    }
                 
                            
                    attribute.Name = propertySetSimpleProperty.Name.ToString();

                    //Get category
                    string cat = GetCategory(propertySet);
                    attribute.Category = (cat == Constants.DEFAULT_STRING) ? "Requirement" : cat;
                    attribute.ExtIdentifier = propertySet.GlobalId;
                    attribute.ExtObject = propertySet.Name;

                    //passed properties from the sheet
                    attribute.SheetName = RowParameters["Sheet"];
                    attribute.RowName = RowParameters["Name"];
                    attribute.CreatedBy = RowParameters["CreatedBy"];
                    attribute.CreatedOn = RowParameters["CreatedOn"];
                    attribute.ExtSystem = RowParameters["ExtSystem"];
                    
                    double test;
                    if (double.TryParse(value, out test))
                    {
                        if ((attribute.Unit.ToLower().Contains("millimeters") || (attribute.Unit.ToLower().Contains("millimetres")) &&
                            (test > 1000000) //if size is large
                            )
                        {
                            test = test / 1000000.0;
                            attribute.Unit = "squaremetres";
                        }
                       
                        value = string.Format("{0:F4}", test); //format the number
                    }
                    
                    attribute.Value = string.IsNullOrEmpty(value) ? Constants.DEFAULT_STRING : value;
                    

                    


                    attribute.Description = propertySetSimpleProperty.Description.ToString();
                    if (string.IsNullOrEmpty(attribute.Description)) //if no description then just use name property
                    {
                        attribute.Description = attribute.Name;
                    }
                    
                    attributes.Rows.Add(attribute);
                }
            }
        }

        private string GetEnumerationValues(IEnumerable<IfcValue> ifcValues )
        {
            List<string> EnumValues = new List<string>();
            foreach (var item in ifcValues)
            {
                EnumValues.Add(item.Value.ToString());
            }
            return string.Join(" : ", EnumValues);
        }

        /// <summary>
        /// Get the first related object properties, fall back to where the IfcTypeObject has no properties
        /// </summary>
        /// <param name="ifcTypeObject">Object to get related object from</param>
        private void GetRelatedObjectProperties(IfcTypeObject ifcTypeObject)
        {
            List<IfcSimpleProperty> RelObjValues = new List<IfcSimpleProperty>(); 

            RelObjValues = (from dic in _propSetsValuesTypeObjectsFirstRelatedObject[ifcTypeObject]
                                from psetval in dic.Value //list of IfcPropertySingleValue
                                select psetval).ToList();

            _ifcPropertySingleValues.AddRange(RelObjValues);
        }

        /// <summary>
        /// Get Category method for property sets
        /// </summary>
        /// <param name="propSet">IfcPropertySet</param>
        /// <returns>Category as string </returns>
        protected string GetCategory(IfcPropertySet propSet)
        {
            IEnumerable<IfcClassificationReference> cats = from IRAC in propSet.HasAssociations
                                                           where IRAC is IfcRelAssociatesClassification
                                                           && ((IfcRelAssociatesClassification)IRAC).RelatingClassification is IfcClassificationReference
                                                           select ((IfcRelAssociatesClassification)IRAC).RelatingClassification as IfcClassificationReference;
            IfcClassificationReference cat = cats.FirstOrDefault();
            if (cat != null)
            {
                return cat.Name.ToString();
            }
            //Try by PropertySet as fallback
            var query = from props in propSet.HasProperties
                        where props.Name.ToString() == "OmniClass Table 13 Category" || props.Name.ToString() == "Category Code"
                        select props.ToString().TrimEnd();
            string val = query.FirstOrDefault();

            if (!String.IsNullOrEmpty(val))
            {
                return val;
            }
            return Constants.DEFAULT_STRING;
        }   
        #endregion
        
    }

        
           
}
