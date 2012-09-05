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

namespace Xbim.COBie.Data
{
    public class COBieDataPropertySetValues : COBieData
    {
        #region Fields
        Dictionary<IfcObject, Dictionary<IfcPropertySet, List<IfcPropertySingleValue>>> _propSetsValuesObjects = null;
        Dictionary<IfcTypeObject, Dictionary<IfcPropertySet, List<IfcPropertySingleValue>>> _propSetsValuesTypeObjects= null;
        List<IfcPropertySingleValue> _ifcPropertySingleValues = null;
        #endregion

        #region Properties
        /// <summary>
        /// Exclude property single value names from selection
        /// </summary>
        public List<string>  ExcludePropertyValueNames { get; private set; }
        /// <summary>
        /// Exclude property single value names from selection which contain the strings held in this list
        /// </summary>
        public List<string> ExcludePropertyValueNamesWildcard { get; private set; }
        /// <summary>
        /// Exclude property set names from selection
        /// </summary>
        public List<string> ExcludePropertySetNames { get; private set; }

        /// <summary>
        /// Filter the returned values for function GetPropertySinglValues
        /// </summary>
        public List<string> FilterPropertyValueNames { get; private set; }
        /// <summary>
        /// Value passed from sheet to attribute sheet
        /// </summary>
        public Dictionary<string, string> RowParameters
        { get; private set; }

        /// <summary>
        /// Filtered property single value list
        /// </summary>
        public List<IfcPropertySingleValue> FilteredPropertySingleValues
        {
            get { return _ifcPropertySingleValues; }
        }
        
        #endregion

        #region Methods
        
        /// <summary>
        /// constructor for IfcObject types
        /// </summary>
        /// <param name="sourceRows"></param>
        public COBieDataPropertySetValues(List<IfcObject> sourceRows)
        {
            _propSetsValuesObjects = sourceRows.ToDictionary(el => el, el => el.PropertySets
                .ToDictionary(ps => ps, ps => ps.HasProperties.OfType<IfcPropertySingleValue>().ToList()));
            //set up lists
            SetListsUp();
        }

        /// <summary>
        /// constructor for IfcObject types
        /// </summary>
        /// <param name="sourceRows"></param>
        public COBieDataPropertySetValues(List<IfcTypeObject> sourceRows)
        {
            _propSetsValuesTypeObjects = sourceRows.Where(tObj => (tObj.HasPropertySets != null)).ToDictionary(tObj => tObj, tObj => tObj.HasPropertySets.OfType<IfcPropertySet>()
                .ToDictionary(ps => ps, ps => ps.HasProperties.OfType<IfcPropertySingleValue>().ToList()));
                                  
            //set up lists
           SetListsUp();           
        }

        /// <summary>
        /// Set the property lists up 
        /// </summary>
        private void SetListsUp()
        {
            _ifcPropertySingleValues = new List<IfcPropertySingleValue>();
            //Set up passed values dictionary
            RowParameters = new Dictionary<string, string>();
            RowParameters.Add("Sheet", DEFAULT_STRING);
            RowParameters.Add("Name", DEFAULT_STRING);
            RowParameters.Add("CreatedBy", DEFAULT_STRING);
            RowParameters.Add("CreatedOn", DEFAULT_STRING);
            RowParameters.Add("ExtSystem", DEFAULT_STRING);
            //set up lists
            ExcludePropertyValueNames = new List<string>();
            FilterPropertyValueNames = new List<string>();
            ExcludePropertyValueNamesWildcard = new List<string>();
            ExcludePropertySetNames = new List<string>();
            FilterPropertyValueNames = new List<string>();
            ExcludePropertyValueNamesWildcard.AddRange(_commonAttExcludes);
        }

        /// <summary>
        /// Indexer for COBieDataPropertySetValues class
        /// </summary>
        /// <param name="ifcObject"> IfcObject key for indexer</param>
        /// <returns></returns>
        public Dictionary<IfcPropertySet, List<IfcPropertySingleValue>> this[IfcObject ifcObject ]
        {
            get { return _propSetsValuesObjects[ifcObject]; }
        }

        /// <summary>
        /// Indexer for COBieDataPropertySetValues class
        /// </summary>
        /// <param name="ifcObject"> IfcObject key for indexer</param>
        /// <returns></returns>
        public Dictionary<IfcPropertySet, List<IfcPropertySingleValue>> this[IfcTypeObject ifcObject]
        {
            get { return _propSetsValuesTypeObjects[ifcObject]; }
        }

        /// <summary>
        /// Iterator function for looping COBieDataPropertySetValues
        /// </summary>
        /// <returns></returns>
        public IEnumerable GetObjects()
        {
            foreach (KeyValuePair<IfcObject, Dictionary<IfcPropertySet, List<IfcPropertySingleValue>>> item in _propSetsValuesObjects)
            {
                yield return item;
            } 
        }

        /// <summary>
        /// Iterator function for looping COBieDataPropertySetValues
        /// </summary>
        /// <returns></returns>
        public IEnumerable GetTypeObjects()
        {
            foreach (KeyValuePair<IfcTypeObject, Dictionary<IfcPropertySet, List<IfcPropertySingleValue>>> item in _propSetsValuesTypeObjects)
            {
                yield return item;
            }
        }

        /// <summary>
        /// Get a names property set from the IfcObject
        /// </summary>
        /// <param name="ifcObject"></param>
        /// <param name="propertySetName"></param>
        /// <returns></returns>
        public IfcPropertySet GetPropertySet(IfcObject ifcObject, string propertySetName)
        {
            return (from pset in _propSetsValuesObjects[ifcObject]
                   where pset.Key.Name.ToString() == propertySetName
                   select pset.Key).FirstOrDefault();
        }

        /// <summary>
        /// Get a names property set from the IfcObject
        /// </summary>
        /// <param name="ifcObject"></param>
        /// <param name="propertySetName"></param>
        /// <returns></returns>
        public IfcPropertySet GetPropertySet(IfcTypeObject ifcObject, string propertySetName)
        {
            return (from pset in _propSetsValuesTypeObjects[ifcObject]
                    where pset.Key.Name.ToString() == propertySetName
                    select pset.Key).FirstOrDefault();
        }

        /// <summary>
        /// Set the single property values held for the IfcObject into _ifcPropertySingleValues field
        /// </summary>
        /// <param name="ifcObject">IfcObject holding the single property values</param>
        public void SetFilteredPropertySingleValues (IfcObject ifcObject)
        {
            if (FilterPropertyValueNames.Count() > 0)
                _ifcPropertySingleValues = (from dic in _propSetsValuesObjects[ifcObject]
                        from psetval in dic.Value //list of IfcPropertySingleValue
                            where FilterPropertyValueNames.Contains(psetval.Name)
                        select psetval).ToList();
            else
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
                if (FilterPropertyValueNames.Count() > 0)
                    _ifcPropertySingleValues = (from dic in _propSetsValuesTypeObjects[ifcObject]
                                                from psetval in dic.Value //list of IfcPropertySingleValue
                                                where FilterPropertyValueNames.Contains(psetval.Name)
                                                select psetval).ToList();
                else
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
        /// Get the first related object properties, fall back to where the IfcTypeObject has no properties
        /// </summary>
        /// <param name="ifcTypeObject">Object to get related object from</param>
        private void GetRelatedObjectProperties(IfcTypeObject ifcTypeObject)
        {
            var RelatedProperties = (from DefProp in
                                         (from RelObj in ifcTypeObject.ObjectTypeOf
                                          where (RelObj != null)
                                          select RelObj.RelatedObjects.First())
                                     from IsDef in DefProp.IsDefinedByProperties
                                     select IsDef.RelatingPropertyDefinition).OfType<IfcPropertySet>();

            if ((FilterPropertyValueNames != null) || (FilterPropertyValueNames.Count() > 0))
            {
                List<IfcPropertySingleValue> RelObjValues = (from Pset in RelatedProperties
                                                             from psetval in Pset.HasProperties.OfType<IfcPropertySingleValue>()
                                                             where FilterPropertyValueNames.Contains(psetval.Name)
                                                             select psetval).ToList();
                _ifcPropertySingleValues.AddRange(RelObjValues);

            }
            else
            {
                List<IfcPropertySingleValue> RelObjValues = (from Pset in RelatedProperties
                                                             from psetval in Pset.HasProperties.OfType<IfcPropertySingleValue>()
                                                             select psetval).ToList();

                _ifcPropertySingleValues.AddRange(RelObjValues);
            }
        }

        /// <summary>
        /// Set the single property values held for the IfcObject into _ifcPropertySingleValues field 
        /// filtered by a IfcPropertySet name
        /// </summary>
        /// <param name="ifcObject">IfcObject holding the single property values</param>
        /// <param name="propertySetName">IfcPropertySetName</param>
        public void SetFilteredPropertySingleValues(IfcTypeObject ifcObject, string propertySetName)
        {
            _ifcPropertySingleValues.Clear(); //reset

            if (_propSetsValuesTypeObjects.ContainsKey(ifcObject))
            {
                if (FilterPropertyValueNames.Count() > 0)
                    _ifcPropertySingleValues = (from dic in _propSetsValuesTypeObjects[ifcObject]
                                                where (dic.Key.Name == propertySetName)
                                                from psetval in dic.Value //list of IfcPropertySingleValue
                                                where FilterPropertyValueNames.Contains(psetval.Name)
                                                select psetval).ToList();
                else
                    _ifcPropertySingleValues = (from dic in _propSetsValuesTypeObjects[ifcObject]
                                                where (dic.Key.Name == propertySetName)
                                                from psetval in dic.Value //list of IfcPropertySingleValue
                                                select psetval).ToList();
            }
            //fall back to related items to get the information from
            if (_ifcPropertySingleValues.Count() == 0)
            {
                //get first related object properties of the passed in object
                GetRelatedObjectProperties(ifcObject, propertySetName);
            }
        }

        /// <summary>
        /// Get the first related object properties, fall back to where the IfcTypeObject has no properties
        /// filtered by a IfcPropertySet name
        /// </summary>
        /// <param name="ifcTypeObject">Object to get related object from</param>
        /// <param name="propertySetName">IfcPropertySetName</param>
        private void GetRelatedObjectProperties(IfcTypeObject ifcTypeObject, string propertySetName)
        {
            var RelatedPropertieSets = (from DefProp in
                                         (from RelObj in ifcTypeObject.ObjectTypeOf
                                          where (RelObj != null)
                                          select RelObj.RelatedObjects.First())
                                     from IsDef in DefProp.IsDefinedByProperties
                                     select IsDef.RelatingPropertyDefinition).OfType<IfcPropertySet>();

            if ((FilterPropertyValueNames != null) || (FilterPropertyValueNames.Count() > 0))
            {
                List<IfcPropertySingleValue> RelObjValues = (from Pset in RelatedPropertieSets
                                                             where (Pset.Name == propertySetName)
                                                             from psetval in Pset.HasProperties.OfType<IfcPropertySingleValue>()
                                                             where FilterPropertyValueNames.Contains(psetval.Name)
                                                             select psetval).ToList();
                _ifcPropertySingleValues.AddRange(RelObjValues);

            }
            else
            {
                List<IfcPropertySingleValue> RelObjValues = (from Pset in RelatedPropertieSets
                                                             where (Pset.Name == propertySetName)
                                                             from psetval in Pset.HasProperties.OfType<IfcPropertySingleValue>()
                                                             select psetval).ToList();

                _ifcPropertySingleValues.AddRange(RelObjValues);
            }
        }
        /// <summary>
        /// Get the property value where the property name equals the passed in value 
        /// </summary>
        /// <param name="PropertyValueName"></param>
        /// <returns></returns>
        public string GetFilteredPropertySingleValues(string PropertyValueName )
        {
            IfcPropertySingleValue ifcPropertySingleValue = _ifcPropertySingleValues.Where(p => p.Name == PropertyValueName).FirstOrDefault();
            return ((ifcPropertySingleValue != null) && (ifcPropertySingleValue.NominalValue != null)) ? ifcPropertySingleValue.NominalValue.Value.ToString() : DEFAULT_STRING;
            
        }

        /// <summary>
        /// Set values for attribute sheet
        /// </summary>
        /// <param name="ifcObject">ifcObject to extract properties from</param>
        /// <param name="attributes">The attribute Sheet to add the properties to its rows</param>
        public void SetAttributesRows(IfcObject ifcObject, ref COBieSheet<COBieAttributeRow> attributes)
        {

            foreach (KeyValuePair<IfcPropertySet, List<IfcPropertySingleValue>> pairValues in _propSetsValuesObjects[ifcObject])
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

                IEnumerable<IfcPropertySingleValue> pSVs = pairValues.Value; //Get Property SetAttribSheet Property Single Values

                pSVs = FilterRows(pSVs);

                ProcessAttributeRow(attributes, ps, pSVs);
            }
            
        }

        /// <summary>
        /// Set values for attribute sheet
        /// </summary>
        /// <param name="ifcObject">ifcObject to extract properties from</param>
        /// <param name="attributes">The attribute Sheet to add the properties to its rows</param>
        public void SetAttributesRows(IfcTypeObject ifcObject, ref COBieSheet<COBieAttributeRow> attributes)
        {

            foreach (KeyValuePair<IfcPropertySet, List<IfcPropertySingleValue>> pairValues in _propSetsValuesTypeObjects[ifcObject])
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

                IEnumerable<IfcPropertySingleValue> pSVs = pairValues.Value; //Get Property SetAttribSheet Property Single Values

                pSVs = FilterRows(pSVs);

                ProcessAttributeRow(attributes, ps, pSVs);
            }

        }
        /// <summary>
        /// Apply filter lists to propertySingleValue selection
        /// </summary>
        /// <param name="pSVs">IEnumerable of IfcPropertySingleValue</param>
        /// <returns>IEnumerable of IfcPropertySingleValue</returns>
        private IEnumerable<IfcPropertySingleValue> FilterRows(IEnumerable<IfcPropertySingleValue> pSVs)
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
            return pSVs;
        }

        /// <summary>
        /// Add Rows to the attribute sheet
        /// </summary>
        /// <param name="attributes">The attribute Sheet to add the properties to its rows</param>
        /// <param name="propertySet">IfcPropertySet which is holding the IfcPropertySingleValue</param>
        /// <param name="propertySetValues">IEnumerable list of IfcPropertySingleValue to extract to the attribute sheet</param>
        private void ProcessAttributeRow(COBieSheet<COBieAttributeRow> attributes, IfcPropertySet propertySet, IEnumerable<IfcPropertySingleValue> propertySetValues)
        {
            //construct the rows
            foreach (IfcPropertySingleValue propertySetSingleValue in propertySetValues)
            {
                if (propertySetSingleValue != null)
                {
                    string value = "";
                    string name = propertySetSingleValue.Name.ToString();

                    if (string.IsNullOrEmpty(name))
                    {
                        continue; //skip to next loop item
                    }

                    if (propertySetSingleValue.NominalValue != null)
                    {
                        value = propertySetSingleValue.NominalValue.Value.ToString();
                        double num;
                        if (double.TryParse(value, out num)) value = num.ToString("F3");
                        if ((string.IsNullOrEmpty(value)) || (string.Compare(value, propertySetSingleValue.Name.ToString(), true) == 0) || (string.Compare(value, "default", true) == 0))
                        {
                            continue; //skip to next loop item
                        }

                    }
                    COBieAttributeRow attribute = new COBieAttributeRow(attributes);

                    attribute.Name = propertySetSingleValue.Name.ToString();

                    //Get category
                    string cat = GetCategory(propertySet);
                    attribute.Category = (cat == DEFAULT_STRING) ? "Requirement" : cat;
                    attribute.ExtIdentifier = propertySet.GlobalId;
                    attribute.ExtObject = propertySet.Name;

                    //passed properties from the sheet
                    attribute.SheetName = RowParameters["Sheet"];
                    attribute.RowName = RowParameters["Name"];
                    attribute.CreatedBy = RowParameters["CreatedBy"];
                    attribute.CreatedOn = RowParameters["CreatedOn"];
                    attribute.ExtSystem = RowParameters["ExtSystem"];

                    attribute.Value = value;
                    attribute.Unit = DEFAULT_STRING; //set initially to default, saves the else statements
                    attribute.Description = DEFAULT_STRING;
                    attribute.AllowedValues = DEFAULT_STRING;
                    if ((propertySetSingleValue.Unit != null) && (propertySetSingleValue.Unit is IfcContextDependentUnit))
                    {
                        attribute.Unit = ((IfcContextDependentUnit)propertySetSingleValue.Unit).Name.ToString();
                        attribute.AllowedValues = ((IfcContextDependentUnit)propertySetSingleValue.Unit).UnitType.ToString();
                    }
                    attribute.Description = propertySetSingleValue.Description.ToString();
                    if (string.IsNullOrEmpty(attribute.Description)) //if no description then just use name property
                    {
                        attribute.Description = attribute.Name;
                    }

                    attributes.Rows.Add(attribute);
                }
            }
        }
        #endregion
        
    }

        
           
}
