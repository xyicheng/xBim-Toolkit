using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.Rows;
using Xbim.Ifc.SelectTypes;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.PropertyResource;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.ExternalReferenceResource;

namespace Xbim.COBie.Data
{
    public class COBieDataAttributeBuilder :   IAttributeProvider
    {

        
        #region IAttributeProvider Implementation
        
        COBieSheet<COBieAttributeRow> _attributes;

        public void InitialiseAttributes(ref COBieSheet<COBieAttributeRow> attributeSheet)
        {
            _attributes = attributeSheet;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The property sets to use to create the attributes from
        /// </summary>
        public COBieDataPropertySetValues  PropertSetValues { get; private set; }

        /// <summary>
        /// Exclude property single value names from selection in SetAttributes functions where the Name property equals an item in this list
        /// </summary>
        public List<string> ExcludeAttributePropertyNames { get; private set; }

        /// <summary>
        /// Exclude property single value names from selection in SetAttributes functions which contain the strings held in this list
        /// </summary>
        public List<string> ExcludeAttributePropertyNamesWildcard { get; private set; }

        /// <summary>
        /// Exclude property single value names from selection in SetAttributes functions which contain the strings held in this list
        /// </summary>
        public List<string> ExcludeAttributePropertyNamesStartingWith { get; private set; }
        /// <summary>
        /// Exclude property set names from selection in SetAttributes functions
        /// </summary>
        public List<string> ExcludeAttributePropertySetNames { get; private set; }

        /// <summary>
        /// Value passed from sheet to attribute sheet
        /// </summary>
        public Dictionary<string, string> RowParameters
        { get; private set; }

        protected COBieContext Context { get; set; }
        
        #endregion
        
        public COBieDataAttributeBuilder(COBieContext context, COBieDataPropertySetValues propertSetValues)
        {
        
            Context = context;
            PropertSetValues = propertSetValues;


            //set up lists
            SetListsUp();
        }

        #region Methods

        /// <summary>
        /// Set the property lists up 
        /// </summary>
        private void SetListsUp()
        {
            //Set up passed values dictionary
            RowParameters = new Dictionary<string, string>();
            RowParameters.Add("Sheet", Constants.DEFAULT_STRING);
            RowParameters.Add("Name", Constants.DEFAULT_STRING);
            RowParameters.Add("CreatedBy", Constants.DEFAULT_STRING);
            RowParameters.Add("CreatedOn", Constants.DEFAULT_STRING);
            RowParameters.Add("ExtSystem", Constants.DEFAULT_STRING);
            //set up lists
            ExcludeAttributePropertyNames = new List<string>();
            ExcludeAttributePropertyNames.AddRange(Context.Exclude.Common.AttributesEqualTo);
            ExcludeAttributePropertyNamesWildcard = new List<string>();
            ExcludeAttributePropertyNamesWildcard.AddRange(Context.Exclude.Common.AttributesContain);
            ExcludeAttributePropertyNamesStartingWith = new List<string>();
            ExcludeAttributePropertyNamesStartingWith.AddRange(Context.Exclude.Common.AttributesStartWith);
            ExcludeAttributePropertySetNames = new List<string>();
           

        }

        /// <summary>
        /// Set values for attribute sheet
        /// </summary>
        /// <param name="ifcObject">ifcObject to extract properties from</param>
        /// <param name="_attributes">The attribute Sheet to add the properties to its rows</param>
        public void PopulateAttributesRows(IfcObject ifcObject)
        {

            foreach (KeyValuePair<IfcPropertySet, List<IfcSimpleProperty>> pairValues in PropertSetValues[ifcObject])
            {
                IfcPropertySet ps = pairValues.Key; //get Property Set
                //get all property attached to the property set
                //check property set exclude list
                if (!string.IsNullOrEmpty(ps.Name))
                {
                    if (ExcludeAttributePropertySetNames.Count() > 0)
                    {
                        if (ExcludeAttributePropertySetNames.Contains(ps.Name))
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
                ProcessAttributeRow( ps, pSVs);
            }

        }

        /// <summary>
        /// Set values for attribute sheet
        /// </summary>
        /// <param name="ifcTypeObject">ifcObject to extract properties from</param>
        /// <param name="_attributes">The attribute Sheet to add the properties to its rows</param>
        public void PopulateAttributesRows(IfcTypeObject ifcTypeObject)
        {
            if (PropertSetValues.PropSetsValuesTypeObjects.ContainsKey(ifcTypeObject))
            {
                foreach (KeyValuePair<IfcPropertySet, List<IfcSimpleProperty>> pairValues in PropertSetValues[ifcTypeObject])
                {
                    IfcPropertySet ps = pairValues.Key; //get Property Set
                    //get all property attached to the property set
                    //check property set exclude list
                    if (!string.IsNullOrEmpty(ps.Name))
                    {
                        if (ExcludeAttributePropertySetNames.Count() > 0)
                        {
                            if (ExcludeAttributePropertySetNames.Contains(ps.Name))
                            {
                                continue; //skip this loop iteration if property set name matches exclude list item
                            }
                        }
                    }

                    IEnumerable<IfcSimpleProperty> pSVs = pairValues.Value; //Get Property SetAttribSheet Property Single Values
                    //filter on ExcludePropertyValueNames and ExcludePropertyValueNamesWildcard
                    pSVs = FilterRows(pSVs);
                    //fill in the data to the attribute rows
                    ProcessAttributeRow( ps, pSVs);
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
            if (ExcludeAttributePropertyNames.Count() > 0)
            {
                //ExcludePropValNames = ExcludePropValNames.ConvertAll(d => d.ToLower()); //lowercase the strings in the list
                pSVs = from pVS in pSVs
                       where !ExcludeAttributePropertyNames.Contains(pVS.Name.ToString())
                       select pVS;
            }
            //filter out the Property names that contain a string from the list excPropWC
            if (ExcludeAttributePropertyNamesWildcard.Count() > 0)
            {
                //excPropWC = excPropWC.ConvertAll(d => d.ToLower()); //lowercase the strings in the list
                pSVs = from pVS in pSVs
                       where ((from item in ExcludeAttributePropertyNamesWildcard
                               where pVS.Name.ToString().Contains(item)
                               select item).Count() == 0)
                       select pVS;
            }
            //filter out the Property names that contain a string from the list excPropWC
            if (ExcludeAttributePropertyNamesStartingWith.Count() > 0)
            {
                //excPropWC = excPropWC.ConvertAll(d => d.ToLower()); //lowercase the strings in the list
                pSVs = from pVS in pSVs
                       where ((from item in ExcludeAttributePropertyNamesStartingWith
                               where ((pVS.Name != null) &&
                                       (pVS.Name.ToString().Length >= item.Length) &&
                                       (pVS.Name.ToString().Substring(0, item.Length) == item)
                                       ) //starts with the string
                               select item).Count() == 0)
                       select pVS;
            }

            return pSVs;
        }

        /// <summary>
        /// Add Rows to the attribute sheet
        /// </summary>
        /// <param name="_attributes">The attribute Sheet to add the properties to its rows</param>
        /// <param name="propertySet">IfcPropertySet which is holding the IfcPropertySingleValue</param>
        /// <param name="propertySetValues">IEnumerable list of IfcPropertySingleValue to extract to the attribute sheet</param>
        private void ProcessAttributeRow(IfcPropertySet propertySet, IEnumerable<IfcSimpleProperty> propertySetValues)
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

                    IEnumerable<COBieAttributeRow> TestRow = _attributes.Rows.Where(r => r.Name == name && r.SheetName == RowParameters["Sheet"] && r.RowName == RowParameters["Name"]);
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

                    COBieAttributeRow attribute = new COBieAttributeRow(_attributes);
                    attribute.Unit = Constants.DEFAULT_STRING; //set initially to default, saves the else statements
                    attribute.AllowedValues = Constants.DEFAULT_STRING;
                    attribute.Description = Constants.DEFAULT_STRING;

                    if (ifcPropertySingleValue != null) //as we can skip on ifcPropertySingleValue we need to split ifcPropertySingleValue testing
                    {
                        if ((ifcPropertySingleValue.Unit != null))
                        {
                            attribute.Unit = COBieData<COBieAttributeRow>.GetUnitName(ifcPropertySingleValue.Unit);
                            
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
                                attribute.Unit = COBieData<COBieAttributeRow>.GetUnitName(ifcPropertyEnumeratedValue.EnumerationReference.Unit);
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
                            attribute.Unit = COBieData<COBieAttributeRow>.GetUnitName(ifcPropertyBoundedValue.Unit);
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
                            attribute.Unit = COBieData<COBieAttributeRow>.GetUnitName(ifcPropertyListValue.Unit);


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
                    attribute.ExtSystem = (propertySet.OwnerHistory.OwningApplication != null) ? propertySet.OwnerHistory.OwningApplication.ApplicationFullName.ToString() : RowParameters["ExtSystem"];

                    value = NumberValueCheck(value, attribute);

                    attribute.Value = string.IsNullOrEmpty(value) ? Constants.DEFAULT_STRING : value;

                    attribute.Description = propertySetSimpleProperty.Description.ToString();
                    if (string.IsNullOrEmpty(attribute.Description)) //if no description then just use name property
                    {
                        attribute.Description = attribute.Name;
                    }

                    _attributes.AddRow(attribute);
                }
            }
        }

        private string NumberValueCheck(string value, COBieAttributeRow attribute)
        {
            double test;
            if (double.TryParse(value, out test))
            {
                //if we have a large number and the units is millimetres then lets change to metres
                //SquareMillemetres
                bool uSSqmeters = attribute.Unit.ToLower().Contains("squaremillimeters");
                bool uKSqMetres = (attribute.Unit.ToLower().Contains("squaremillimetres"));
                if ((uSSqmeters || uKSqMetres) &&
                    (test > 1000000) //if size is large
                    )
                {
                    test = test / 1000000.0;
                    if (uKSqMetres)
                        attribute.Unit = "squaremetres";
                    else
                        attribute.Unit = "squaremeters";

                }

                //Millimetres
                bool uSmeters = attribute.Unit.ToLower().Contains("millimeters");
                bool uKMetres = (attribute.Unit.ToLower().Contains("millimetres"));
                if ((uSmeters || uKMetres) &&
                    (test > 100000) //if size is large
                    )
                {
                    test = test / 1000.0;
                    if (uKMetres)
                        attribute.Unit = "metres";
                    else
                        attribute.Unit = "meters";

                }

                value = string.Format("{0:F4}", test); //format the number
            }
            return value;
        }

        private string GetEnumerationValues(IEnumerable<IfcValue> ifcValues)
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

            RelObjValues = (from dic in PropertSetValues.PropSetsValuesTypeObjectsFirstRelatedObject[ifcTypeObject]
                            from psetval in dic.Value //list of IfcPropertySingleValue
                            select psetval).ToList();

            PropertSetValues.AllPropertySingleValues.AddRange(RelObjValues);
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
