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

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Base class for the input of data into the Excel worksheets
    /// </summary>
    public abstract class COBieData
    {
        protected IModel Model { get; set; }
        
        public const string DEFAULT_STRING = "n/a";
        public const string DEFAULT_NUMERIC = "0";
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
                emails += string.IsNullOrEmpty(emails) ? "unknown@bimunknown.com" : "@bimunknown.com";
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
                return "0"; //default
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
            var query = from PSet in obj.PropertySets
                        from Props in PSet.HasProperties
                        where Props.Name.ToString() == "OmniClass Table 13 Category" || Props.Name.ToString() == "Category Code"
                        select Props.ToString().TrimEnd();
            string val = query.FirstOrDefault();

            if (!String.IsNullOrEmpty(val))
            {
                return val;
            }
            return COBieData.DEFAULT_STRING;
        }

        /// <summary>
        /// Retrieve Attribute data from other sheets
        /// </summary>
        /// <param name="obj">Object holding the additional properties(Attributes)</param>
        /// <param name="passedValues">Holder to pass values form calling sheet function</param>
        /// <param name="reqdProps">List of PropertySet names / Property Name pairs to extract</param>
        /// <param name="attributes">The attribute Sheet to add the properties to its rows</param>
        protected void SetAttributeSheet(IfcObject obj, Dictionary<string, string> passedValues, List<KeyValuePair<string, string>> reqdProps, ref COBieSheet<COBieAttributeRow> attributes)
        {
           
            foreach (KeyValuePair<string, string> item in reqdProps)
            {
                //IfcPropertySingleValue PropValue = sp.GetPropertySingleValue(item.Key.ToString(), item.Value.ToString());

                IfcPropertySingleValue PropValue = null;
                IfcPropertySet PropSet = obj.GetPropertySet(item.Key.ToString());
                if (PropSet != null)
                {

                    COBieAttributeRow attribute = new COBieAttributeRow(attributes);
                    PropValue = PropSet.HasProperties.Where<IfcPropertySingleValue>(p => p.Name == item.Value.ToString()).FirstOrDefault();

                    attribute.Name = item.Value.ToString();
                    

                    //Get category
                    //(((PropSet.HasAssociations.ToArray()[0] as IfcRelAssociatesClassification).RelatingClassification)as IfcClassificationReference).Name
                    IEnumerable<IfcClassificationReference> Cats = from IRAC in PropSet.HasAssociations
                                                                   where IRAC is IfcRelAssociatesClassification
                                                                   && ((IfcRelAssociatesClassification)IRAC).RelatingClassification is IfcClassificationReference
                                                                   select ((IfcRelAssociatesClassification)IRAC).RelatingClassification as IfcClassificationReference;
                    IfcClassificationReference Cat = Cats.FirstOrDefault();
                    attribute.Category = (Cat != null) ? Cat.Name.ToString() : DEFAULT_STRING;

                    //passed properties from the sheet
                    attribute.SheetName = passedValues["Sheet"];
                    attribute.RowName = passedValues["Name"];
                    attribute.CreatedBy = passedValues["CreatedBy"];
                    attribute.CreatedOn = passedValues["CreatedOn"];
                    attribute.ExtSystem = passedValues["ExtSystem"];

                    attribute.Value = DEFAULT_STRING;                  //set initially to default, saves the else statements
                    attribute.Unit = DEFAULT_STRING;
                    attribute.ExtIdentifier = PropSet.GlobalId;
                    attribute.ExtObject = item.Key.ToString();
                    attribute.Description = DEFAULT_STRING;
                    attribute.AllowedValues = DEFAULT_STRING;


                    if (PropValue != null)
                    {
                        if (PropValue.NominalValue != null)
                        {
                            attribute.Value = PropValue.NominalValue.Value.ToString();
                            double num;
                            if (double.TryParse(attribute.Value, out num))
                            {
                                attribute.Value = num.ToString("F3");
                            }
                        }

                        if ((PropValue.Unit != null) && (PropValue.Unit is IfcContextDependentUnit))
                        {
                            attribute.Unit = ((IfcContextDependentUnit)PropValue.Unit).Name.ToString();
                            attribute.AllowedValues = ((IfcContextDependentUnit)PropValue.Unit).UnitType.ToString();
                        }

                        attribute.Description = PropValue.Description.ToString();
                        if (string.IsNullOrEmpty(attribute.Description)) //if no description then just use name property
                        {
                            attribute.Description = attribute.Name;
                        }
                    }

                    attributes.Rows.Add(attribute);
                }
            }
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
