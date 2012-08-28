using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.UtilityResource;
using Xbim.Ifc.MeasureResource;
using Xbim.XbimExtensions;
using Xbim.Ifc.ActorResource;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Base class for the input of data into the Excel worksheets
    /// </summary>
    public abstract class COBieData
    {
        protected IModel Model { get; set; }
        //protected IModel _model = null;
        public const string DEFAULT_VAL = "n/a";

        #region Methods

        /// <summary>
        /// Extract the Created On date from the passed entity
        /// </summary>
        /// <param name="rootEntity">Entity to extract the Create On date</param>
        /// <returns></returns>
        protected string GetCreatedOnDateAsFmtString(IfcOwnerHistory ownerHistory)
        {
            const string strFormat = "yyyy-MM-ddTHH:mm:ss";

            int CreatedOnTStamp = (int)ownerHistory.CreationDate;
            if (CreatedOnTStamp <= 0)
            {
                return DateTime.Now.ToString(strFormat); //we have to return a date to comply. so now is used
            }
            else
            {
                //to remove trailing decimal seconds use a set format string as "o" option is to long.

                //We have a day light saving problem with the comparison with other COBie Export Programs. if we convert to local time we get a match
                //but if the time stamp is Coordinated Universal Time (UTC), then daylight time should be ignored. see http://msdn.microsoft.com/en-us/library/bb546099.aspx
                //IfcTimeStamp.ToDateTime(CreatedOnTStamp).ToLocalTime()...; //test to see if corrects 1 hour difference, and yes it did, but should we?

                return IfcTimeStamp.ToDateTime(CreatedOnTStamp).ToString(strFormat);
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
