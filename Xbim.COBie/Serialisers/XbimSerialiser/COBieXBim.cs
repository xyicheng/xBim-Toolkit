using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.UtilityResource;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.Kernel;
using Xbim.XbimExtensions;
using Xbim.Ifc.GeometryResource;
using Xbim.Ifc.ActorResource;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.ExternalReferenceResource;
using Xbim.Ifc.PropertyResource;
using Xbim.Ifc.Extensions;
using Xbim.Ifc.SelectTypes;

namespace Xbim.COBie.Serialisers.XbimSerialiser
{
    public class COBieXBim
    {
        #region Properties
        /// <summary>
        /// Context object holding Model, WorkBook etc...
        /// </summary>
        protected COBieXBimContext XBimContext { get; set; }

        /// <summary>
        /// Model from COBieXBimContext object
        /// </summary>
        public IModel Model
        {
            get { return XBimContext.Model; }
        } 
        /// <summary>
        /// World coordinate system from COBieXBimContext object
        /// </summary>
        public IfcAxis2Placement3D WCS
        {
            get { return XBimContext.WCS; }
        }

        /// <summary>
        /// WorkBook from COBieXBimContext object
        /// </summary>
        public COBieWorkbook WorkBook
        {
            get { return XBimContext.WorkBook; }
        }

        /// <summary>
        /// Contacts from COBieXBimContext object
        /// </summary>
        public Dictionary<string, IfcPersonAndOrganization> Contacts
        {
            get { return XBimContext.Contacts; }
        }
        #endregion

        public COBieXBim(COBieXBimContext xBimContext)
        {
            XBimContext = xBimContext;
        }

        #region Methods
        /// <summary>
        /// Set a new owner history to the 
        /// </summary>
        /// <param name="ifcRoot">Object to add the owner history</param>
        /// <param name="externalSystem">Application used to modify/create</param>
        /// <param name="createdBy">IfcPersonAndOrganization object</param>
        /// <param name="createdOn">Date the object was created on</param>
        protected void SetNewOwnerHistory(IfcRoot ifcRoot, string externalSystem, IfcPersonAndOrganization createdBy, string createdOn)
        { 
            ifcRoot.OwnerHistory = CreateOwnerHistory(ifcRoot,  externalSystem, createdBy, createdOn);
        }

        /// <summary>
        /// Set an existing or create a new owner history if no match is found
        /// </summary>
        /// <param name="ifcRoot">Object to add the owner history</param>
        /// <param name="externalSystem">Application used to modify/create</param>
        /// <param name="createdBy">IfcPersonAndOrganization object</param>
        /// <param name="createdOn">Date the object was created on</param>
        protected void SetOwnerHistory(IfcRoot ifcRoot, string externalSystem, IfcPersonAndOrganization createdBy, string createdOn)
        {
            IfcTimeStamp stamp = null;
            if (ValidateString(createdOn))
            {
                DateTime dateTime;
                if (DateTime.TryParse(createdOn, out dateTime))
                    stamp = IfcTimeStamp.ToTimeStamp(dateTime);
            }
            IfcOwnerHistory ifcOwnerHistory = Model.InstancesOfType<IfcOwnerHistory>().Where(oh => (oh.CreationDate == stamp) &&
                                                                           (oh.OwningUser.EntityLabel == createdBy.EntityLabel) &&
                                                                           ((oh.OwningApplication.ApplicationFullName.ToString().ToLower() == externalSystem.ToLower()) ||
                                                                           (oh.LastModifyingApplication.ApplicationFullName.ToString().ToLower() == externalSystem.ToLower())
                                                                           )
                                                                           ).FirstOrDefault();
            if (ifcOwnerHistory != null) 
                ifcRoot.OwnerHistory = ifcOwnerHistory;
            else
                ifcRoot.OwnerHistory = CreateOwnerHistory(ifcRoot, externalSystem, createdBy, createdOn);
        }
        /// <summary>
        /// Create the owner history
        /// </summary>
        /// <param name="ifcRoot">Object to add the owner history</param>
        /// <param name="externalSystem">Application used to modify/create</param>
        /// <param name="createdBy">IfcPersonAndOrganization object</param>
        /// <param name="createdOn">Date the object was created on</param>
        protected IfcOwnerHistory CreateOwnerHistory(IfcRoot ifcRoot, string externalSystem, IfcPersonAndOrganization createdBy, string createdOn)
        {
            IfcApplication ifcApplication = null;
            if (ValidateString(externalSystem))
            {
                //create an organization for the external system
                IfcOrganization ifcOrganization = null;
                string orgName = externalSystem.Split(' ').FirstOrDefault();
                ifcOrganization = Model.InstancesOfType<IfcOrganization>().Where(o => o.Name == orgName).FirstOrDefault();
                if (ifcOrganization == null)
                {
                    ifcOrganization = Model.New<IfcOrganization>();
                    ifcOrganization.Name = orgName;
                }
                ifcApplication = Model.InstancesOfType<IfcApplication>().Where(app => app.ApplicationFullName == externalSystem).FirstOrDefault();
                if (ifcApplication == null)
                    ifcApplication = Model.New<IfcApplication>(app => { app.ApplicationFullName = externalSystem; app.ApplicationDeveloper = ifcOrganization; app.Version = new IfcLabel(""); });

            }

            IfcTimeStamp stamp = null;
            if (ValidateString(createdOn))
            {
                DateTime dateTime;
                if (DateTime.TryParse(createdOn, out dateTime))
                    stamp = IfcTimeStamp.ToTimeStamp(dateTime);
            }

            return Model.New<IfcOwnerHistory>(oh =>
            {
                oh.OwningUser = createdBy;
                oh.OwningApplication = ifcApplication;
                oh.LastModifyingApplication = ifcApplication;
                oh.CreationDate = stamp;
                oh.ChangeAction = IfcChangeActionEnum.NOCHANGE;
            });

        }



        /// <summary>
        /// Check for empty, null of DEFAULT_STRING
        /// </summary>
        /// <param name="value">string to validate</param>
        /// <returns></returns>
        protected bool ValidateString(string value)
        {
            return ((!string.IsNullOrEmpty(value)) && (value != Constants.DEFAULT_STRING));
        }

        /// <summary>
        /// Get the IfcBuilding Object
        /// </summary>
        /// <returns>IfcBuilding Object</returns>
        protected IfcBuilding GetBuilding()
        {
            IEnumerable<IfcBuilding> ifcBuildings = Model.InstancesOfType<IfcBuilding>();
            if ((ifcBuildings.Count() > 1) || (ifcBuildings.Count() == 0))
                throw new Exception(string.Format("COBieXBimSerialiser: Expecting one IfcBuilding, Found {0}", ifcBuildings.Count().ToString()));
            return ifcBuildings.FirstOrDefault();
        }
        /// <summary>
        /// Get the IfcSite Object
        /// </summary>
        /// <returns>IfcSite Object</returns>
        protected IfcSite GetSite()
        {
            IEnumerable<IfcSite> ifcSites = Model.InstancesOfType<IfcSite>();
            if ((ifcSites.Count() > 1) || (ifcSites.Count() == 0))
                throw new Exception(string.Format("COBieXBimSerialiser: Expecting one IfcSite, Found {0}", ifcSites.Count().ToString()));
            return ifcSites.FirstOrDefault();
        }
        /// <summary>
        /// Convert a String to a Double
        /// </summary>
        /// <param name="num">string to convert</param>
        /// <returns>double or null</returns>
        protected double? GetDoubleFromString(string num)
        {
            double test;
            if (double.TryParse(num, out test))
                return test;
            else
                return null;
        }

        protected IfcBuildingStorey GetBuildingStory (string name)
        {
            IEnumerable<IfcBuildingStorey> ifcBuildingStoreys = Model.InstancesOfType<IfcBuildingStorey>().Where(bs => bs.Name == name);
            if ((ifcBuildingStoreys.Count() > 1) || (ifcBuildingStoreys.Count() == 0))
                throw new Exception(string.Format("COBieXBimSerialiser: Expecting one IfcBuildingStorey with name of {1}, Found {0}", ifcBuildingStoreys.Count().ToString(), name));
            return ifcBuildingStoreys.FirstOrDefault();
        }

        /// <summary>
        /// Add Category via the IfcRelAssociatesClassification object
        /// </summary>
        /// <param name="category">Category for this IfcRoot Object</param>
        /// <param name="ifcRoot">IfcRoot derived object to all category to</param>
        protected void AddCategory(string category, IfcRoot ifcRoot)
        {
            if (ValidateString(category))
            {
                string itemReference = "";
                string name = "";
                string[] splitCategory = category.Split(':');
                //see if we have a split category name like "11-11: Assembly Facilities"
                if (splitCategory.Count() == 2)
                {
                    itemReference = splitCategory.First().Trim();
                    name = splitCategory.Last().Trim();
                }
                else
                    name = category.Trim();

            
                //check to see if we have a IfcRelAssociatesClassification associated with this category
                IfcRelAssociatesClassification ifcRelAssociatesClassification = Model.InstancesOfType<IfcRelAssociatesClassification>()
                                    .Where(rac => rac.RelatingClassification != null 
                                        && (rac.RelatingClassification is IfcClassificationReference)
                                        && (   ((((IfcClassificationReference)rac.RelatingClassification).ItemReference.ToString().ToLower() == itemReference.ToLower())
                                            &&  (((IfcClassificationReference)rac.RelatingClassification).Name.ToString().ToLower() == name.ToLower())
                                               )
                                            || (((IfcClassificationReference)rac.RelatingClassification).Location.ToString().ToLower() == category.ToLower())
                                            )
                                        )
                                    .FirstOrDefault();
                if (ifcRelAssociatesClassification == null)
                {
                    //check we have a IfcClassificationReference holding the category
                    IfcClassificationReference ifcClassificationReference = Model.InstancesOfType<IfcClassificationReference>()
                                    .Where(cr =>(  (cr.ItemReference.ToString().ToLower() == itemReference.ToLower())
                                                && (cr.Name.ToString().ToLower() == name.ToLower())
                                                )
                                            || (cr.Location.ToString().ToLower() == category.ToLower())
                                          )
                                    .FirstOrDefault();

                    if (ifcClassificationReference == null) //create if required
                        ifcClassificationReference = Model.New<IfcClassificationReference>(ifcCR => { ifcCR.ItemReference = itemReference; ifcCR.Name = name; ifcCR.Location = category; });
                    //create new IfcRelAssociatesClassification holding a IfcClassificationReference with Location set to the category value
                    ifcRelAssociatesClassification = Model.New<IfcRelAssociatesClassification>(ifcRAC => { ifcRAC.RelatingClassification = ifcClassificationReference; ifcRAC.RelatedObjects.Add(ifcRoot); });
                }
                else
                {
                    //we have a IfcRelAssociatesClassification so just add this IfcRoot object to the RelatedObjects collection
                    ifcRelAssociatesClassification.RelatedObjects.Add_Reversible(ifcRoot);
                }
            }
        }

        /// <summary>
        /// Add GlobalId to the IfcRoot Object
        /// </summary>
        /// <param name="extId">string representing the global Id</param>
        /// <param name="ifcRoot">IfcRoot derived object to add GlobalId too</param>
        protected void AddGlobalId(string extId, IfcRoot ifcRoot)
        {
            if (ValidateString(extId))
            {
                IfcGloballyUniqueId id = new IfcGloballyUniqueId(extId);
                ifcRoot.GlobalId = id;
            }
        }

        /// <summary>
        /// Set or change a Property Set description value
        /// </summary>
        /// <param name="obj">Object holding the property</param>
        /// <param name="pSetName">Property set name</param>
        /// <param name="pSetDescription">Property set description</param>
        private static void SetPropertySetDescription(IfcObject obj, string pSetName, string pSetDescription)
        {
            if (!string.IsNullOrEmpty(pSetDescription))
            {
            IfcRelDefinesByProperties ifcRelDefinesByProperties = obj.IsDefinedByProperties.Where(r => r.RelatingPropertyDefinition.Name == pSetName).FirstOrDefault();
            if (ifcRelDefinesByProperties != null)
                ifcRelDefinesByProperties.RelatingPropertyDefinition.Description = pSetDescription;
            }
        }

        /// <summary>
        /// Add a property single value
        /// </summary>
        /// <param name="ifcObject">Object to add property too</param>
        /// <param name="pSetName">Property set name to add property single value too</param>
        /// <param name="pSetDescription">Property set description or null to leave unaltered</param>
        /// <param name="propertyName">Property single value name</param>
        /// <param name="propertyDescription">Property single value description or null to not set</param>
        /// <param name="value">IfcValue select type to set on property single value</param>
        protected IfcPropertySingleValue AddPropertySingleValue(IfcObject ifcObject, string pSetName, string pSetDescription, string propertyName, string propertyDescription, IfcValue value)
        {
            IfcPropertySingleValue ifcPropertySingleValue = ifcObject.SetPropertySingleValue(pSetName, propertyName, value);
            if (!string.IsNullOrEmpty(propertyDescription))
                ifcPropertySingleValue.Description = propertyDescription; //description to property Single Value
            //set description for the property set, nice to have but optional
            if (!string.IsNullOrEmpty(pSetDescription))
            SetPropertySetDescription(ifcObject, pSetName, pSetDescription);
            return ifcPropertySingleValue;
        }

        /// <summary>
        /// Add a property set or use existing if it exists
        /// </summary>
        /// <param name="ifcObject">IfcObject to add property set too</param>
        /// <param name="pSetName">name of the property set</param>
        protected IfcPropertySet AddPropertySet(IfcObject ifcObject, string pSetName, string pSetDescription)
        {
            IfcPropertySet ifcPropertySet = ifcObject.GetPropertySet(pSetName);
            if (ifcPropertySet == null)
            {   
                ifcPropertySet = Model.New<IfcPropertySet>();
                ifcPropertySet.Name = pSetName;
                ifcPropertySet.Description = pSetDescription;
                //set relationship
                IfcRelDefinesByProperties ifcRelDefinesByProperties = Model.New<IfcRelDefinesByProperties>();
                ifcRelDefinesByProperties.RelatingPropertyDefinition = ifcPropertySet;
                ifcRelDefinesByProperties.RelatedObjects.Add_Reversible(ifcObject);
            }
            return ifcPropertySet;
        }

        /// <summary>
        /// Add a property set or use existing if it exists
        /// </summary>
        /// <param name="ifcObject">IfcObject to add property set too</param>
        /// <param name="pSetName">name of the property set</param>
        protected IfcPropertySet AddPropertySet(IfcTypeObject ifcObject, string pSetName, string pSetDescription)
        {
            IfcPropertySet ifcPropertySet = ifcObject.GetPropertySet(pSetName);
            if (ifcPropertySet == null)
            {
                ifcPropertySet = Model.New<IfcPropertySet>();
                ifcPropertySet.Name = pSetName;
                ifcPropertySet.Description = pSetDescription;
                //set relationship
                ifcObject.AddPropertySet(ifcPropertySet);
            }
            return ifcPropertySet;
        }
        /// <summary>
        /// Add a property single value or use existing if it exists
        /// </summary>
        /// <param name="ifcPropertySet">IfcPropertySet object</param>
        /// <param name="propertyName">Single Value Property Name</param>
        /// <param name="propertyDescription">Single Value Property Description</param>
        /// <param name="value">IfcValue object</param>
        protected IfcPropertySingleValue AddPropertySingleValue(IfcPropertySet ifcPropertySet, string propertyName, string propertyDescription, IfcValue value, IfcUnit unit)
        {
            //see if we have this property single value, if so overwrite the value
            IfcPropertySingleValue ifcPropertySingleValue = ifcPropertySet.HasProperties.Where<IfcPropertySingleValue>(p => p.Name == propertyName).FirstOrDefault();
            if (ifcPropertySingleValue == null)
            {
                ifcPropertySingleValue = Model.New<IfcPropertySingleValue>(psv => { psv.Name = propertyName; psv.Description = propertyDescription;  psv.NominalValue = value; });
                ifcPropertySet.HasProperties.Add_Reversible(ifcPropertySingleValue);
            }
            ifcPropertySingleValue.NominalValue = value;
            ifcPropertySingleValue.Unit = unit;
            //add to property set
            ifcPropertySet.HasProperties.Add_Reversible(ifcPropertySingleValue);
            return ifcPropertySingleValue;
        }

        /// <summary>
        /// Add a new Property Enumerated Value or use existing
        /// </summary>
        /// <param name="ifcPropertySet">IfcPropertySet object</param>
        /// <param name="propertyName">Property Enumerated Value Name</param>
        /// <param name="propertyDescription">Property Enumerated Value Description</param>
        /// <param name="values">Property Enumerated Value list of values</param>
        /// <param name="enumValues">Property Enumerated Value possible enumeration values</param>
        /// <param name="unit">Unit for the enumValues values</param>
        /// <returns></returns>
        protected IfcPropertyEnumeratedValue AddPropertyEnumeratedValue(IfcPropertySet ifcPropertySet, string propertyName, string propertyDescription, IfcValue[] values, IfcValue[] enumValues, IfcUnit unit)
        {
            IfcPropertyEnumeratedValue ifcPropertyEnumeratedValue = ifcPropertySet.HasProperties.Where<IfcPropertyEnumeratedValue>(p => p.Name == propertyName).FirstOrDefault();

            if (ifcPropertyEnumeratedValue != null)
            {
                ifcPropertyEnumeratedValue.EnumerationValues.Clear_Reversible();
                ifcPropertyEnumeratedValue.EnumerationReference.EnumerationValues.Clear_Reversible();
            }
            else
            {
                ifcPropertyEnumeratedValue = Model.New<IfcPropertyEnumeratedValue>();
                ifcPropertyEnumeratedValue.EnumerationReference = Model.New<IfcPropertyEnumeration>();
            }
            //fill values
            ifcPropertyEnumeratedValue.Name = propertyName;
            if (!string.IsNullOrEmpty(propertyDescription))
            {
                 ifcPropertyEnumeratedValue.Description = propertyDescription;
            }
           
            foreach (IfcValue ifcValue in values)
            {
                ifcPropertyEnumeratedValue.EnumerationValues.Add_Reversible(ifcValue);
            }
            foreach (IfcValue ifcValue in enumValues)
            {
                ifcPropertyEnumeratedValue.EnumerationReference.EnumerationValues.Add_Reversible(ifcValue);
            }
            //add unit
            if (unit != null)
            {
                ifcPropertyEnumeratedValue.EnumerationReference.Unit = unit;
            }

            //add to property set
            ifcPropertySet.HasProperties.Add_Reversible(ifcPropertyEnumeratedValue);
            return ifcPropertyEnumeratedValue;
        }

        /// <summary>
        /// Set or change a Property Set description value
        /// </summary>
        /// <param name="obj">IfcTypeObject holding the property</param>
        /// <param name="pSetName">Property set name</param>
        /// <param name="pSetDescription">Property set description</param>
        private void SetPropertySetDescription(IfcTypeObject obj, string pSetName, string pSetDescription)
        {
            if (!string.IsNullOrEmpty(pSetDescription))
            {
                IfcPropertySet ifcPropertySet = obj.HasPropertySets.OfType<IfcPropertySet>().Where(r => r.Name == pSetName).FirstOrDefault();
                if (ifcPropertySet != null)
                    ifcPropertySet.Description = pSetDescription;
            }
        }

        /// <summary>
        /// Add a property single value
        /// </summary>
        /// <param name="IfcTypeObject">Object to add property too</param>
        /// <param name="pSetName">Property set name to add property single value too</param>
        /// <param name="pSetDescription">Property set description or null to leave unaltered</param>
        /// <param name="propertyName">Property single value name</param>
        /// <param name="propertyDescription">Property single value description or null to not set</param>
        /// <param name="value">IfcValue select type to set on property single value</param>
        protected IfcPropertySingleValue AddPropertySingleValue(IfcTypeObject ifcObject, string pSetName, string pSetDescription, string propertyName, string propertyDescription, IfcValue value)
        {
            IfcPropertySingleValue ifcPropertySingleValue = ifcObject.SetPropertySingleValue(pSetName, propertyName, value);
            if (!string.IsNullOrEmpty(propertyDescription))
                ifcPropertySingleValue.Description = propertyDescription; //description to property Single Value
            //set description for the property set, nice to have but optional
            if (!string.IsNullOrEmpty(pSetDescription))
                SetPropertySetDescription(ifcObject, pSetName, pSetDescription);
            return ifcPropertySingleValue;
        }

        /// <summary>
        /// Get the Duration unit, default year on no match
        /// </summary>
        /// <param name="requiredUnit">string of year, month or week</param>
        protected IfcConversionBasedUnit GetDurationUnit(string requiredUnit)
        {
            switch (requiredUnit.ToLower())
            {
                case "year":
                    return XBimContext.IfcConversionBasedUnitYear;
                    
                case "month":
                    return XBimContext.IfcConversionBasedUnitMonth;
                   
                case "week":
                    return XBimContext.IfcConversionBasedUnitWeek;

                case "minute":
                    return XBimContext.IfcConversionBasedUnitMinute;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Create a user defined unit via IfcContextDependentUnit
        /// </summary>
        /// <param name="unitName"></param>
        /// <returns></returns>
        protected IfcContextDependentUnit SetContextDependentUnit(string unitName)
        {
           return Model.New<IfcContextDependentUnit>(cdu => 
            {
                cdu.Name = unitName;
                cdu.UnitType = IfcUnitEnum.USERDEFINED;
                cdu.Dimensions = XBimContext.DimensionalExponentSingleUnit;
            });
        }

        protected IfcSIUnit GetSIUnit(string value)
        {
            IfcSIUnitName? returnUnit;
            IfcSIPrefix? returnPrefix;
            IfcSIUnit ifcSIUnit = null;

            if (GetUnitEnumerations(value, out returnUnit, out returnPrefix))
            {
                IEnumerable<IfcSIUnit> ifcSIUnits = Model.InstancesWhere<IfcSIUnit>(siu => siu.Name == (IfcSIUnitName)returnUnit);
                if (ifcSIUnits.Any())
                {
                    if (returnPrefix != null)
                    {
                        ifcSIUnit = ifcSIUnits.Where(siu => siu.Prefix == (IfcSIPrefix)returnPrefix).FirstOrDefault();
                    }
                    else
                    {
                        ifcSIUnit = ifcSIUnits.FirstOrDefault();
                    }
                }
                else
                {
                    IfcUnitEnum IfcUnitEnum = MappingUnitType((IfcSIUnitName)returnUnit); //get the unit type for the IfcSIUnitName
                    ifcSIUnit = Model.New<IfcSIUnit>(si =>
                                            {
                                                si.UnitType = IfcUnitEnum;
                                                si.Name = (IfcSIUnitName)returnUnit;
                                            });
                    if (returnPrefix != null)
                    {
                        ifcSIUnit.Prefix = (IfcSIPrefix)returnPrefix;
                    }
                }
            }
            return ifcSIUnit;
        }
        
        private IfcUnitEnum MappingUnitType (IfcSIUnitName ifcSIUnitName)
        {
            switch (ifcSIUnitName)
            {
                case IfcSIUnitName.AMPERE:
                    return IfcUnitEnum.ELECTRICCURRENTUNIT;
                case IfcSIUnitName.BECQUEREL:
                    return IfcUnitEnum.RADIOACTIVITYUNIT;
                case IfcSIUnitName.CANDELA:
                    return IfcUnitEnum.LUMINOUSINTENSITYUNIT;
                case IfcSIUnitName.COULOMB:
                    return IfcUnitEnum.ELECTRICCHARGEUNIT;
                case IfcSIUnitName.CUBIC_METRE:
                    return IfcUnitEnum.VOLUMEUNIT;
                case IfcSIUnitName.DEGREE_CELSIUS:
                    return IfcUnitEnum.THERMODYNAMICTEMPERATUREUNIT;
                case IfcSIUnitName.FARAD:
                    return IfcUnitEnum.ELECTRICCAPACITANCEUNIT;
                case IfcSIUnitName.GRAM:
                    return IfcUnitEnum.MASSUNIT;
                case IfcSIUnitName.GRAY:
                    return IfcUnitEnum.ABSORBEDDOSEUNIT;
                case IfcSIUnitName.HENRY:
                    return IfcUnitEnum.INDUCTANCEUNIT;
                case IfcSIUnitName.HERTZ:
                    return IfcUnitEnum.FREQUENCYUNIT;
                case IfcSIUnitName.JOULE:
                    return IfcUnitEnum.ENERGYUNIT;
                case IfcSIUnitName.KELVIN:
                    return IfcUnitEnum.THERMODYNAMICTEMPERATUREUNIT;
                case IfcSIUnitName.LUMEN:
                    return IfcUnitEnum.LUMINOUSFLUXUNIT;
                case IfcSIUnitName.LUX:
                    return IfcUnitEnum.ILLUMINANCEUNIT;
                case IfcSIUnitName.METRE:
                    return IfcUnitEnum.LENGTHUNIT;
                case IfcSIUnitName.MOLE:
                    return IfcUnitEnum.AMOUNTOFSUBSTANCEUNIT;
                case IfcSIUnitName.NEWTON:
                    return IfcUnitEnum.FORCEUNIT;
                case IfcSIUnitName.OHM:
                    return IfcUnitEnum.ELECTRICRESISTANCEUNIT;
                case IfcSIUnitName.PASCAL:
                    return IfcUnitEnum.PRESSUREUNIT;
                case IfcSIUnitName.RADIAN:
                    return IfcUnitEnum.PLANEANGLEUNIT;
                case IfcSIUnitName.SECOND:
                    return IfcUnitEnum.TIMEUNIT;
                case IfcSIUnitName.SIEMENS:
                    return IfcUnitEnum.ELECTRICCONDUCTANCEUNIT;
                case IfcSIUnitName.SIEVERT:
                    return IfcUnitEnum.RADIOACTIVITYUNIT;
                case IfcSIUnitName.SQUARE_METRE:
                    return IfcUnitEnum.AREAUNIT;
                case IfcSIUnitName.STERADIAN:
                    return IfcUnitEnum.SOLIDANGLEUNIT;
                case IfcSIUnitName.TESLA:
                    return IfcUnitEnum.MAGNETICFLUXDENSITYUNIT;
                case IfcSIUnitName.VOLT:
                    return IfcUnitEnum.ELECTRICVOLTAGEUNIT;
                case IfcSIUnitName.WATT:
                    return IfcUnitEnum.POWERUNIT;
                case IfcSIUnitName.WEBER:
                    return IfcUnitEnum.MAGNETICFLUXUNIT;
                default:
                    return IfcUnitEnum.USERDEFINED;
                   
            }
        

        }
        /// <summary>
        /// See if a string can be converted to the IfcSIUnitName / IfcSIPrefix combination
        /// </summary>
        /// <param name="value">string to evaluate</param>
        /// <param name="returnUnit">IfcSIUnitName? object to pass found value out</param>
        /// <param name="returnPrefix">IfcSIPrefix? object to pass found value out</param>
        /// <returns>bool, success or failed</returns>
        protected bool GetUnitEnumerations(string value, out IfcSIUnitName? returnUnit, out IfcSIPrefix? returnPrefix)
        {
            value = value.ToLower();
            if (value.Contains("meter")) value = value.Replace("meter", "metre");
            //remove the last letter if 's', but DEGREE_CELSIUS in IfcSIUnitName so do not do removal for CELSIUS
            if ((!value.Contains("celsius")) && (value.Last() == 's'))
            {
                value = value.Substring(0, (value.Length - 1));
            }


            string sqText = "";
            string baseUnit = "";
            string testPrefix = "";

            string prefixUnit = "";
            string unitName = "";

            string[] ifcSIUnitNames = Enum.GetNames(typeof(IfcSIUnitName));
            string[] ifcSIPrefixs = Enum.GetNames(typeof(IfcSIPrefix));
            //check for "_" ifcSIUnitName held in the value
            foreach (string name in ifcSIUnitNames)
            {
                if (name.Contains("_"))
                {
                    //see COBieData.GetUnitName method for the setting of the names via the enum's
                    string[] split = name.Split('_');   //see if _ delimited value such as SQUARE_METRE
                    if (split.Length > 1) sqText = split.First().ToLower();
                    baseUnit = split.Last().ToLower();
                    if (value.Contains(sqText) && value.Contains(baseUnit))
                    {
                        unitName = name; //we used an underscore to set the name
                        break;
                    }
                    else
                    {
                        sqText = "";
                        baseUnit = "";
                    }
                }

            }
            //see if we had prefixes in the string name
            foreach (string prefix in ifcSIPrefixs)
            {
                testPrefix = sqText + prefix; //add the front end of any "_" unit name from above, or default will be ""
                if ((value.Length >= testPrefix.Length) && (testPrefix.ToLower() == value.Substring(0, testPrefix.Length))) //see if the front end matched
                {
                    prefixUnit = prefix;
                    break;
                }

            }
            if (string.IsNullOrEmpty(unitName)) //no "_" unit name was used so lets test for the rest
            {
                foreach (string name in ifcSIUnitNames)
                {
                    if ((!name.Contains("_")) && //skip under scores
                        value.Contains(name.ToLower()) && //holds the text, but two value that conflict are STERADIAN  and RADIAN  so
                        (name.Length == (value.Length - prefixUnit.Length)) //check length by adding any prefix(default ""), we know no underscore so no need to add sqText value
                        )
                    {
                        unitName = name;
                        break;
                    }
                }
            }

            //convert the strings to the enumeration type
            IfcSIUnitName ifcSIUnitName;
            if (Enum.TryParse(unitName.ToUpper(), out ifcSIUnitName))
                returnUnit = ifcSIUnitName;
            else
                returnUnit = null; //we need a value


            IfcSIPrefix ifcSIPrefix;
            if (Enum.TryParse(prefixUnit.ToUpper(), out ifcSIPrefix))
                returnPrefix = ifcSIPrefix;
            else
                returnPrefix = null;
            return (returnUnit != null);
        }

        /// <summary>
        /// split strings on a ":"
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected List<string> SplitString(string value)
        {
            string[] values = value.Split(':');
            for (int i = 0; i < values.Count(); i++)
            {
                values[i] = values[i].Trim();
            }
            return values.ToList<string>();
        }

        #endregion

    }
}
