using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.PropertyResource;



namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to extract all the property sets and there associated properties for a list of either IfcObjects or IfcTypeObjects
    /// </summary>
    public class COBieDataPropertySetValues 
    {
        #region Fields
        
        private Dictionary<IfcObject, Dictionary<IfcPropertySet, List<IfcSimpleProperty>>> _propSetsValuesObjects = null;
        private Dictionary<IfcTypeObject, Dictionary<IfcPropertySet, List<IfcSimpleProperty>>> _propSetsValuesTypeObjects = null;
        private Dictionary<IfcTypeObject, Dictionary<IfcPropertySet, List<IfcSimpleProperty>>> _propSetsValuesTypeObjectsFirstRelatedObject = null;
        
        private List<IfcSimpleProperty> _ifcPropertySingleValues = null; //result set store
 
        #endregion

        #region Properties
        /// <summary>
        /// Dictionary keyed on the Object IfcObject, holding a Dictionary keyed on property set holding the SinglePropertyValues held by that set related to the object
        /// </summary>
        public Dictionary<IfcObject, Dictionary<IfcPropertySet, List<IfcSimpleProperty>>> PropSetsValuesObjects
        {
            get { return _propSetsValuesObjects; }
        }

        /// <summary>
        /// Dictionary keyed on the Object IfcTypeObject, holding a Dictionary keyed on property set holding the SinglePropertyValues held by that set related to the object
        /// </summary>
        public Dictionary<IfcTypeObject, Dictionary<IfcPropertySet, List<IfcSimpleProperty>>> PropSetsValuesTypeObjects
        {
            get { return _propSetsValuesTypeObjects; }
        }

        /// <summary>
        /// Dictionary keyed on the Object IfcTypeObject, holding a Dictionary keyed on property set holding the SinglePropertyValues held by that set related to the object property  RelatedObjects.First()
        /// </summary>
        public Dictionary<IfcTypeObject, Dictionary<IfcPropertySet, List<IfcSimpleProperty>>> PropSetsValuesTypeObjectsFirstRelatedObject
        {
            get { return _propSetsValuesTypeObjectsFirstRelatedObject; }
        }
       
        /// <summary>
        /// Property single value list of all properties associated to a IfcObject or IfcTypeObject
        /// </summary>
        public List<IfcSimpleProperty> AllPropertySingleValues
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
            try
            {
                _propSetsValuesObjects = sourceRows.ToDictionary(el => el, el => el.PropertySets
                                .ToDictionary(ps => ps, ps => ps.HasProperties.OfType<IfcSimpleProperty>().ToList()));
            }
            catch (System.Exception e)
            {
                
                throw e;
            }
            
            
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
            //set up property value list
            _ifcPropertySingleValues = new List<IfcSimpleProperty>();
            
                  
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
        public void SetAllPropertySingleValues (IfcObject ifcObject)
        {
            _ifcPropertySingleValues = (from dic in _propSetsValuesObjects[ifcObject]
                        from psetval in dic.Value //list of IfcPropertySingleValue
                        select psetval).ToList();
        }

        /// <summary>
        /// Set the single property values held for the IfcObject into _ifcPropertySingleValues field
        /// </summary>
        /// <param name="ifcObject">IfcObject holding the single property values</param>
        public void SetAllPropertySingleValues(IfcTypeObject ifcObject)
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
        public void SetAllPropertySingleValues(IfcTypeObject ifcTypeObject, string propertySetName)
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
        /// Get the property value where the property name equals the passed in value. 
        /// Always use  SetAllPropertySingleValues before calling this method
        /// </summary>
        /// <param name="PropertyValueName">IfcPropertySingleValue name</param>
        /// <param name="containsString">Do Contains text match on PropertyValueName if true, exact match if false</param>
        /// <returns></returns>
        public string GetPropertySingleValueValue(string PropertyValueName, bool containsString)
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
        /// Always use  SetAllPropertySingleValues before calling this method
        /// </summary>
        /// <param name="PropertyValueName"></param>
        /// <returns></returns>
        public IfcPropertySingleValue GetPropertySingleValue(string PropertyValueName)
        {
            return _ifcPropertySingleValues.Where(p => p.Name == PropertyValueName).OfType<IfcPropertySingleValue>().FirstOrDefault();
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
        
        #endregion
        
    }

        
           
}
