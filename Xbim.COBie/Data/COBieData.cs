using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.UtilityResource;
using Xbim.Ifc.MeasureResource;
using Xbim.XbimExtensions;
using Xbim.Ifc.ActorResource;
using Xbim.COBie.Rows;
using Xbim.Ifc.PropertyResource;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.Extensions;
using Xbim.Ifc.ExternalReferenceResource;
using Xbim.Ifc.SelectTypes;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Base class for the input of data into the Excel worksheets
    /// </summary>
    public abstract  class COBieData
    {
        protected IModel Model { get; set; }
        
        public const string DEFAULT_STRING = "n/a";
        public const string DEFAULT_NUMERIC = "0";
        /// <summary>
        /// Common exclude PropertySingleValue name containing any of the strings 
        /// </summary>
        public List<string> _commonAttExcludes = new List<string>() {"Zone Base Offset", "Upper Limit", "Roomtag", 
                            "Line Pattern", "Symbol", "Width","Window Inset", 
                            "Radius", "Phase Created","Phase", "Outside Radius", 
                            "Outside Diameter", "Omniclass", "Offset", "Mark",
                            "Recepticles", "Limit Offset", "Lighting Calculation Workplan",
                            "Size", "Level", "Host", "Hot Water Radius", "Length", "Height",
                            "Half Oval", "GSA BIM Area", "AssetAccountingType", "Description",
                            "Name", "Classification Description", "Classification Code",
                            "Category Description","Category Code", "Uniclass Description",
                            "Uniclass Code", "Assembly Description", "Assembly Code",
                            "Omniclass Title", "Omniclass Number", "MethodOfMeasurement"};
        
         
        #region Methods

        /// <summary>
        /// Extract the Created On date from the passed entity
        /// </summary>
        /// <param name="rootEntity">Entity to extract the Create On date</param>
        /// <returns></returns>
        protected string GetCreatedOnDateAsFmtString(IfcOwnerHistory ownerHistory)
        {
            const string strFormat = "yyyy-MM-dd HH:mm:ss";

            int createdOnTStamp = (int)ownerHistory.CreationDate;
            if (createdOnTStamp <= 0)
            {
                return DateTime.Now.ToString(strFormat); //we have to return a date to comply. so now is used
            }
            else
            {
                //to remove trailing decimal seconds use a set format string as "o" option is to long.

                //We have a day light saving problem with the comparison with other COBie Export Programs. if we convert to local time we get a match
                //but if the time stamp is Coordinated Universal Time (UTC), then daylight time should be ignored. see http://msdn.microsoft.com/en-us/library/bb546099.aspx
                //IfcTimeStamp.ToDateTime(CreatedOnTStamp).ToLocalTime()...; //test to see if corrects 1 hour difference, and yes it did, but should we?

                return IfcTimeStamp.ToDateTime(createdOnTStamp).ToString(strFormat);
            }

        }

        /// <summary>
        /// Returns the application that created the file
        /// </summary>
        /// <returns>IfcApplication</returns>
        protected IfcApplication GetIfcApplication()
        {
            IfcApplication app = Model.InstancesOfType<IfcApplication>().FirstOrDefault();
            return app;
        }

        /// <summary>
        /// Extract the email address lists for the owner of the IfcOwnerHistory passed
        /// </summary>
        /// <param name="ifcOwnerHistory">Entity to extract the email addresses for</param>
        /// <returns>string of comma delimited addresses</returns>
        protected string GetTelecomEmailAddress(IfcOwnerHistory ifcOwnerHistory)
        {
            string emails = "";

            IfcPerson ifcP = ifcOwnerHistory.OwningUser.ThePerson;
            IEnumerable<IfcTelecomAddress> ifcTelecomAddresses = (ifcP.Addresses == null) ? null : ifcP.Addresses.TelecomAddresses;
            if (ifcTelecomAddresses == null) ifcTelecomAddresses = Enumerable.Empty<IfcTelecomAddress>();
            foreach (IfcTelecomAddress address in ifcTelecomAddresses)
            {
                if ((address != null) && (address.ElectronicMailAddresses != null))
                    emails += address.ElectronicMailAddresses[0].ToString() + ",";
            }
            emails = emails.TrimEnd(',');

            if (emails == "")
            {
                IfcOrganization ifcO = ifcOwnerHistory.OwningUser.TheOrganization;
                ifcTelecomAddresses = (ifcP.Addresses == null) ? null : ifcO.Addresses.TelecomAddresses;
                if (ifcTelecomAddresses == null) ifcTelecomAddresses = Enumerable.Empty<IfcTelecomAddress>();

                foreach (IfcTelecomAddress address in ifcTelecomAddresses)
                {
                    if ((address != null) && (address.ElectronicMailAddresses != null))
                        emails += address.ElectronicMailAddresses[0].ToString() + ",";
                }
                emails = emails.TrimEnd(',');
            }
            //if still no email lets make one up
            if (string.IsNullOrEmpty(emails))
            {
                if (!(string.IsNullOrEmpty(ifcP.GivenName))) emails = ifcP.GivenName;
                if (!(string.IsNullOrEmpty(ifcP.GivenName) && string.IsNullOrEmpty(ifcP.FamilyName))) emails += ".";
                if (!(string.IsNullOrEmpty(ifcP.FamilyName))) emails += ifcP.FamilyName;
                emails += string.IsNullOrEmpty(emails) ? "unknown@unknown" : "@unknown";
            }

            return emails;
        }

        /// <summary>
        /// Converts string to formatted string and if it fails then passes 0 back, mainly to check that we always have a number returned
        /// </summary>
        /// <param name="num">string to convert</param>
        /// <returns>string converted to a formatted string using "F2" as formatter</returns>
        protected string ConvertNumberOrDefault(string num)
        {
            double temp;

            if (double.TryParse(num, out temp))
            {
                return temp.ToString("F2"); // two decimal places
            }
            else
            {
                return DEFAULT_NUMERIC; 
            }

        }

        /// <summary>
        /// Get Category method
        /// </summary>
        /// <param name="obj">Object to try and extract method from</param>
        /// <returns></returns>
        public string GetCategory(IfcObject obj)
        {
            //Try by relationship first
            IfcRelAssociatesClassification ifcRAC = obj.HasAssociations.OfType<IfcRelAssociatesClassification>().FirstOrDefault();
            if (ifcRAC != null)
            {
                IfcClassificationReference ifcCR = (IfcClassificationReference)ifcRAC.RelatingClassification;
                return ifcCR.Name;
            }
            //Try by PropertySet as fallback
            var query = from pSet in obj.PropertySets
                        from props in pSet.HasProperties
                        where props.Name.ToString() == "OmniClass Table 13 Category" || props.Name.ToString() == "Category Code"
                        select props.ToString().TrimEnd();
            string val = query.FirstOrDefault();

            if (!String.IsNullOrEmpty(val))
            {
                return val;
            }
            return COBieData.DEFAULT_STRING;
        }
        /// <summary>
        /// Get Category method for property sets
        /// </summary>
        /// <param name="propSet">IfcPropertySet</param>
        /// <returns>Category as string </returns>
        private static string GetCategory(IfcPropertySet propSet)
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
            return COBieData.DEFAULT_STRING;
        }

        
        /// <summary>
        /// Retrieve Attribute data from other sheets filtered via reqdProps
        /// </summary>
        /// <param name="obj">Object holding the additional properties(Attributes)</param>
        /// <param name="passedValues">Holder to pass values form calling sheet function</param>
        /// <param name="reqdProps">List of PropertySet names / Property Name pairs to extract</param>
        /// <param name="attributes">The attribute Sheet to add the properties to its rows</param>
        //protected void SetAttributeSheet(Object obj, Dictionary<string, string> passedValues, List<KeyValuePair<string, string>> reqdProps, ref COBieSheet<COBieAttributeRow> attributes)
        //{
            
        //    foreach (KeyValuePair<string, string> item in reqdProps)
        //    {
        //        IfcPropertySingleValue PropValue = null;
        //        IfcPropertySet PropSet = null;
        //        if (obj is IfcObject)
        //            PropSet = ((IfcObject)obj).GetPropertySet(item.Key.ToString());
        //        else if (obj is IfcTypeObject)
        //            PropSet = ((IfcTypeObject)obj).GetPropertySet(item.Key.ToString());
        //        else
        //            throw new ArgumentException("Incorrect type passed to COBieData:SetAttributeSheet method");

        //        if (PropSet != null)
        //        {

        //            COBieAttributeRow attribute = new COBieAttributeRow(attributes);
        //            PropValue = PropSet.HasProperties.Where<IfcPropertySingleValue>(p => p.Name == item.Value.ToString()).FirstOrDefault();

        //            attribute.Name = item.Value.ToString();
                    

        //            //Get category
        //            string Cat = GetCategory(PropSet);
        //            attribute.Category = (Cat == DEFAULT_STRING) ? "Requirement" : Cat;
        //            attribute.ExtIdentifier = PropSet.GlobalId;
        //            attribute.ExtObject = item.Key.ToString();

        //            GetAttributsCommon(passedValues, PropValue, ref attribute);

        //            attributes.Rows.Add(attribute);
        //        }
        //    }
        //}

        

        

        /// <summary>
        /// Retrieve Attribute data from other sheets, retrieving all properties attached to object (obj)
        /// </summary>
        /// <param name="obj">IfcObject holding the additional properties(Attributes)</param>
        /// <param name="passedValues">Holder to pass values form calling sheet function</param>
        /// <param name="excProp">List of propertSinglalue names to exclude</param>
        /// <param name = "excPropWC">List of propertSinglalue part names to exclude, name holds the part name to match</param>
        /// <param name="excPropSet">List of PropertySet names to exclude</param>
        /// <param name="attributes">The attribute Sheet to add the properties to its rows</param>
        protected void SetAttributeSheet(IfcObject obj, Dictionary<string, string> passedValues, List<string> excProp, List<string> excPropWC, List<string> excPropSet, ref COBieSheet<COBieAttributeRow> attributes)
        {
            

            IEnumerable<IfcPropertySet> pSets = obj.PropertySets; 
            //process the IfcPropertySet sets
            if (pSets != null)
            {
                if ((excPropSet != null) && (excPropSet.Count() > 0))
                {
                    //excPropSet = excPropSet.ConvertAll(d => d.ToLower()); //lowercase the strings in the list
                    pSets = from ps in pSets
                               where !excPropSet.Contains(ps.Name.ToString())
                               select ps;
                }
                SetAttributsCommon(passedValues, excProp, excPropWC, ref attributes, pSets); 
            }
        }


        /// <summary>
        /// Retrieve Attribute data from other sheets, retrieving all properties attached to object (obj)
        /// </summary>
        /// <param name="obj">IfcTypeObject holding the additional properties(Attributes)</param>
        /// <param name="passedValues">Holder to pass values form calling sheet function</param>
        /// <param name="excProp">List of propertSinglalue names to exclude</param>
        /// <param name = "excPropWC">List of propertSinglalue part names to exclude, name holds the part name to match</param>
        /// <param name = "excPropSet">List of PropertySet names to exclude</param>
        /// <param name="attributes">The attribute Sheet to add the properties to its rows</param>
        protected void SetAttributeSheet(IfcTypeObject obj, Dictionary<string, string> passedValues, List<string> excProp, List<string> excPropWC, List<string> excPropSet, ref COBieSheet<COBieAttributeRow> attributes)
        {
            

            var pSetsAll = obj.HasPropertySets;
            
            if (pSetsAll != null)
            {
                IEnumerable<IfcPropertySet> pSets = pSetsAll.OfType<IfcPropertySet>();
                if ((excPropSet != null) && (excPropSet.Count() > 0))
                {
                   //excPropSet = excPropSet.ConvertAll(d => d.ToLower()); //lowercase the strings in the list
                    pSets = from ps in pSets
                               where !excPropSet.Contains(ps.Name.ToString())
                               select ps;
                }
                //process the IfcPropertySet sets
                SetAttributsCommon(passedValues, excProp, excPropWC, ref attributes, pSets);
            }
        }
        
         

        /// <summary>
        /// Set Values to common attribute values
        /// </summary>
        /// <param name="passedValues">Holder to pass values form calling sheet function</param>
        /// <param name="excProp">List of propertSinglalue names to exclude</param>
        /// <param name = "excPropWC">List of propertSinglalue part names to exclude, if name holds the part name</param>
        /// <param name="attributes">The attribute Sheet to add the properties to its rows</param>
        /// <param name="pSets"></param>
        private void SetAttributsCommon(Dictionary<string, string> passedValues, List<string> excProp, List<string> excPropWC, ref COBieSheet<COBieAttributeRow> attributes, IEnumerable<IfcPropertySet> pSets)
        { 
            if (excPropWC == null) excPropWC = new List<string>();     //if null create to place default string           
            excPropWC = excPropWC.Concat(_commonAttExcludes).ToList(); //common exclude PropertySingleValue name containing this part string                

            foreach (IfcPropertySet ps in pSets)
            {
               
                //get all property attached to the property set
                IEnumerable<IfcPropertySingleValue> pSVs = ps.HasProperties.OfType<IfcPropertySingleValue>();
                                                           
                if ((excProp != null) && (excProp.Count() > 0))
                {
                    //excProp = excProp.ConvertAll(d => d.ToLower()); //lowercase the strings in the list
                    pSVs = from pVS in pSVs
                               where !excProp.Contains(pVS.Name.ToString())
                               select pVS;
                }
                //filter out the Property names that contain a string from the list excPropWC
                if ((excPropWC != null) && (excPropWC.Count() > 0))
                {
                    //excPropWC = excPropWC.ConvertAll(d => d.ToLower()); //lowercase the strings in the list
                    pSVs = from pVS in pSVs
                           where ((from item in excPropWC
                                   where pVS.Name.ToString().Contains(item)
                                   select item).Count() == 0)
                           select pVS;
                }

                //bool skip = false;
                foreach (IfcPropertySingleValue pSV in pSVs)
                {
                    if (pSV != null)
                    {
                        string PSVvalue = "";
                        //string pSVKey = "";

                        if (pSV.NominalValue != null)
                        {
                           
                            PSVvalue = pSV.NominalValue.Value.ToString();
                            double num;
                            if (double.TryParse(PSVvalue, out num)) PSVvalue = num.ToString("F3");
                            if ((string.IsNullOrEmpty(PSVvalue)) || (string.Compare(PSVvalue,pSV.Name.ToString(), true) == 0) || (string.Compare(PSVvalue,"default", true) == 0))
                            {
                                continue; //skip to next loop item
                            }
                            
                        }

                        //pSVKey = pSV.Name.ToString();
                        //if (string.IsNullOrEmpty(PSVKey)) //if property name is blank then skip
                        //{
                        //    continue; //skip to next loop item
                        //}

                        //foreach (string str in excPropWC)
                        //{
                        //    if (pSVKey.Contains(str))
                        //    {
                        //        skip = true;
                        //        break; //found one so stop searching
                        //    }

                        //}
                        //if (skip == true)
                        //{
                        //    skip = false;
                        //    continue; //skip to next PVS
                        //}
                        

                        COBieAttributeRow attribute = new COBieAttributeRow(attributes);

                        attribute.Name = pSV.Name.ToString(); ;

                        //Get category
                        string cat = GetCategory(ps);
                        attribute.Category = (cat == DEFAULT_STRING) ? "Requirement" : cat;
                        attribute.ExtIdentifier = ps.GlobalId;
                        attribute.ExtObject = ps.Name;

                        //GetAttributsCommon(passedValues, PSV, ref attribute);
                        //passed properties from the sheet
                        attribute.SheetName = passedValues["Sheet"];
                        attribute.RowName = passedValues["Name"];
                        attribute.CreatedBy = passedValues["CreatedBy"];
                        attribute.CreatedOn = passedValues["CreatedOn"];
                        attribute.ExtSystem = passedValues["ExtSystem"];

                        attribute.Value = PSVvalue;                 
                        attribute.Unit = DEFAULT_STRING; //set initially to default, saves the else statements
                        attribute.Description = DEFAULT_STRING;
                        attribute.AllowedValues = DEFAULT_STRING;
                        if ((pSV.Unit != null) && (pSV.Unit is IfcContextDependentUnit))
                        {
                            attribute.Unit = ((IfcContextDependentUnit)pSV.Unit).Name.ToString();
                            attribute.AllowedValues = ((IfcContextDependentUnit)pSV.Unit).UnitType.ToString();
                        }
                        attribute.Description = pSV.Description.ToString();
                        if (string.IsNullOrEmpty(attribute.Description)) //if no description then just use name property
                        {
                            attribute.Description = attribute.Name;
                        }

                        attributes.Rows.Add(attribute);

                    }
                }
            }
        }

        
        /// <summary>
        /// Set values for attribute sheet
        /// </summary>
        /// <param name="passedValues">Holder to pass values form calling sheet function</param>
        /// <param name="excProp">List of propertSinglalue names to exclude</param>
        /// <param name = "excPropWC">List of propertSinglalue part names to exclude, if name holds the part name</param>
        /// <param name="attributes">The attribute Sheet to add the properties to its rows</param>
        /// <param name="dictPSets">Dictionary of (IfcProperySet, List(IfcPropertySingleValue))  </IfcProperySet></param>
        protected void SetAttribSheet(Dictionary<string, string> passedValues, List<string> excProp, List<string> excPropWC, ref COBieSheet<COBieAttributeRow> attributes, Dictionary<IfcPropertySet, List<IfcPropertySingleValue>> dictPSets)
        {
            if (excPropWC == null) excPropWC = new List<string>();     //if null create to place default string           
            excPropWC = excPropWC.Concat(_commonAttExcludes).ToList(); //common exclude PropertySingleValue name containing this part string                

            foreach (KeyValuePair<IfcPropertySet, List<IfcPropertySingleValue>> pairValues in dictPSets)
            {
                IfcPropertySet ps = pairValues.Key; //get Property Set

                //get all property attached to the property set
                IEnumerable<IfcPropertySingleValue> pSVs = pairValues.Value; //Get Property SetAttribSheet Property Single Values

                //filter for excluded properties, full name
                if ((excProp != null) && (excProp.Count() > 0))
                {
                    //excProp = excProp.ConvertAll(d => d.ToLower()); //lowercase the strings in the list
                    pSVs = from pVS in pSVs
                           where !excProp.Contains(pVS.Name.ToString())
                           select pVS;
                }
                //filter out the Property names that contain a string from the list excPropWC
                if ((excPropWC != null) && (excPropWC.Count() > 0))
                {
                    //excPropWC = excPropWC.ConvertAll(d => d.ToLower()); //lowercase the strings in the list
                    pSVs = from pVS in pSVs
                           where ((from item in excPropWC
                                   where pVS.Name.ToString().Contains(item)
                                   select item).Count() == 0)
                           select pVS;
                }

                //construct the rows
                foreach (IfcPropertySingleValue pSV in pSVs)
                {
                    if (pSV != null)
                    {
                        //test value and format or filter depending on value
                        string PSVvalue = "";
                        if (pSV.NominalValue != null)
                        {

                            PSVvalue = pSV.NominalValue.Value.ToString();
                            double num;
                            if (double.TryParse(PSVvalue, out num)) PSVvalue = num.ToString("F3");
                            if ((string.IsNullOrEmpty(PSVvalue)) || (string.Compare(PSVvalue, pSV.Name.ToString(), true) == 0) || (string.Compare(PSVvalue, "default", true) == 0))
                            {
                                continue; //skip to next loop item
                            }

                        }
                        COBieAttributeRow attribute = new COBieAttributeRow(attributes);

                        attribute.Name = pSV.Name.ToString(); ;

                        //Get category
                        string cat = GetCategory(ps);
                        attribute.Category = (cat == DEFAULT_STRING) ? "Requirement" : cat;
                        attribute.ExtIdentifier = ps.GlobalId;
                        attribute.ExtObject = ps.Name;

                        //passed properties from the sheet
                        attribute.SheetName = passedValues["Sheet"];
                        attribute.RowName = passedValues["Name"];
                        attribute.CreatedBy = passedValues["CreatedBy"];
                        attribute.CreatedOn = passedValues["CreatedOn"];
                        attribute.ExtSystem = passedValues["ExtSystem"];

                        attribute.Value = PSVvalue;
                        attribute.Unit = DEFAULT_STRING; //set initially to default, saves the else statements
                        attribute.Description = DEFAULT_STRING;
                        attribute.AllowedValues = DEFAULT_STRING;
                        if ((pSV.Unit != null) && (pSV.Unit is IfcContextDependentUnit))
                        {
                            attribute.Unit = ((IfcContextDependentUnit)pSV.Unit).Name.ToString();
                            attribute.AllowedValues = ((IfcContextDependentUnit)pSV.Unit).UnitType.ToString();
                        }
                        attribute.Description = pSV.Description.ToString();
                        if (string.IsNullOrEmpty(attribute.Description)) //if no description then just use name property
                        {
                            attribute.Description = attribute.Name;
                        }

                        attributes.Rows.Add(attribute);

                    }
                }
            }
        }
        /// <summary>
        /// Get the associated Type for a IfcObject, so a Door can be of type "Door Type A"
        /// </summary>
        /// <param name="obj">IfcObject to get associated type information from</param>
        /// <returns>string holding the type information</returns>
        protected string GetTypeName(IfcObject obj)
        {
            var elType = obj.IsDefinedBy.OfType<IfcRelDefinesByType>().FirstOrDefault();
            return (elType != null) ? elType.RelatingType.Name.ToString() : DEFAULT_STRING;
        }

        #endregion
    }

    #region IComparer Classes
    /// <summary>
    /// ICompare class for IfcLabels, used to order by 
    /// </summary>
    public class CompareIfcLabel : IComparer<IfcLabel?>
    {
        public int Compare(IfcLabel? x, IfcLabel? y)
        {
            return string.Compare(x.ToString(), y.ToString(), true); //ignore case set to true
        }
    }

    /// <summary>
    /// ICompare class for String, used to order by 
    /// </summary>
    public class CompareString : IComparer<string>
    {
        public int Compare(string x, string y)
        {
            return string.Compare(x, y, true); //ignore case set to true
        }
    }

    #endregion
}
