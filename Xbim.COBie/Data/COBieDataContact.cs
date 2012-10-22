using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.Rows;
using Xbim.Ifc.ActorResource;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.UtilityResource;
using Xbim.XbimExtensions;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.SelectTypes;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the Contact tab.
    /// </summary>
    public class COBieDataContact : COBieData<COBieContactRow>
    {
        /// <summary>
        /// Data Contact constructor
        /// </summary>
        /// <param name="model">The context of the model being generated</param>
        public COBieDataContact(COBieContext context) : base(context)
        { }


        #region Methods

        /// <summary>
        /// Fill sheet rows for Contact sheet
        /// </summary>
        /// <returns>COBieSheet<COBieContactRow></returns>
        public override COBieSheet<COBieContactRow> Fill()
        {
            ProgressIndicator.ReportMessage("Starting Contacts...");

            ClearEMails(); //clear the email dictionary for a new file conversion

            //create new sheet
            COBieSheet<COBieContactRow> contacts = new COBieSheet<COBieContactRow>(Constants.WORKSHEET_CONTACT);

            IEnumerable<IfcPersonAndOrganization> ifcPersonAndOrganizations = Model.InstancesOfType<IfcPersonAndOrganization>();
            ProgressIndicator.Initialise("Creating Contacts", ifcPersonAndOrganizations.Count());

            foreach (IfcPersonAndOrganization ifcPersonAndOrganization in ifcPersonAndOrganizations)
            {
                ProgressIndicator.IncrementAndUpdate();
                COBieContactRow contact = new COBieContactRow(contacts);
                // get person and organization
                IfcOrganization ifcOrganization = ifcPersonAndOrganization.TheOrganization;
                IfcPerson ifcPerson = ifcPersonAndOrganization.ThePerson;
                contact.Email = GetTelecomEmailAddress(ifcPersonAndOrganization);

                //lets default the creator to that user who created the project for now, no direct link to OwnerHistory on IfcPersonAndOrganization, IfcPerson or IfcOrganization
                contact.CreatedBy = GetTelecomEmailAddress(Model.IfcProject.OwnerHistory);
                contact.CreatedOn = GetCreatedOnDateAsFmtString(Model.IfcProject.OwnerHistory);
                
                IfcActorRole ifcActorRole = null;
                if (ifcPerson.Roles != null)
                    ifcActorRole = ifcPerson.Roles.FirstOrDefault();
                if (ifcOrganization.Roles != null)
                    ifcActorRole = ifcOrganization.Roles.FirstOrDefault();
                if ((ifcActorRole != null) && (!string.IsNullOrEmpty(ifcActorRole.UserDefinedRole)))
                {
                    contact.Category = ifcActorRole.UserDefinedRole.ToString();
                }
                else
                    contact.Category = DEFAULT_STRING;
                
                contact.Company = (string.IsNullOrEmpty(ifcOrganization.Name)) ? DEFAULT_STRING : ifcOrganization.Name.ToString();
                contact.Phone = GetTelecomTelephoneNumber(ifcPersonAndOrganization);
                contact.ExtSystem = DEFAULT_STRING;   // TODO: Person is not a Root object so has no Owner. What should this be?
                
                contact.ExtObject = "IfcPersonAndOrganization";
                contact.ExtIdentifier = ifcPerson.Id;
                if (ifcPerson.Addresses != null)
                {
                    string department = ifcPerson.Addresses.PostalAddresses.Select(dept => dept.InternalLocation).Where(dept => !string.IsNullOrEmpty(dept)).FirstOrDefault();
                    if (string.IsNullOrEmpty(department))
                        department = ifcOrganization.Description.ToString(); //only place to match example files
                    contact.Department = (string.IsNullOrEmpty(department)) ? contact.Company : department;
                }
                else
                    contact.Department = contact.Company;

                contact.OrganizationCode = (string.IsNullOrEmpty(ifcOrganization.Name)) ? DEFAULT_STRING : ifcOrganization.Name.ToString();
                contact.GivenName = (string.IsNullOrEmpty(ifcPerson.GivenName)) ? DEFAULT_STRING : ifcPerson.GivenName.ToString();
                contact.FamilyName = (string.IsNullOrEmpty(ifcPerson.FamilyName)) ? DEFAULT_STRING : ifcPerson.FamilyName.ToString();
                if (ifcPerson.Addresses != null)
                    GetContactAddress(contact, ifcPerson.Addresses);
                else
                    GetContactAddress(contact, ifcOrganization.Addresses);

                contacts.AddRow(contact);
                
            }
            ProgressIndicator.Finalise();
            return contacts;
        }

        private static void GetContactAddress(COBieContactRow contact, AddressCollection addresses)
        {
            if ((addresses != null) && (addresses.PostalAddresses  != null))
            {
                IfcPostalAddress ifcPostalAddress = addresses.PostalAddresses.FirstOrDefault();
                if (ifcPostalAddress != null)
                {
                    contact.Street = (ifcPostalAddress.AddressLines != null) ? ifcPostalAddress.AddressLines.FirstOrDefault().Value.ToString() : DEFAULT_STRING;
                    contact.PostalBox = (string.IsNullOrEmpty(ifcPostalAddress.PostalBox)) ? DEFAULT_STRING : ifcPostalAddress.PostalBox.ToString();
                    contact.Town = (string.IsNullOrEmpty(ifcPostalAddress.Town)) ? DEFAULT_STRING : ifcPostalAddress.Town.ToString();
                    contact.StateRegion = (string.IsNullOrEmpty(ifcPostalAddress.Region)) ? DEFAULT_STRING : ifcPostalAddress.Region.ToString();
                    contact.PostalCode = (string.IsNullOrEmpty(ifcPostalAddress.PostalCode)) ? DEFAULT_STRING : ifcPostalAddress.PostalCode.ToString();
                    contact.Country = (string.IsNullOrEmpty(ifcPostalAddress.Country)) ? DEFAULT_STRING : ifcPostalAddress.Country.ToString();
                    return;
                }
            }
            contact.Street = DEFAULT_STRING;
            contact.PostalBox = DEFAULT_STRING;
            contact.Town = DEFAULT_STRING;
            contact.StateRegion = DEFAULT_STRING;
            contact.PostalCode = DEFAULT_STRING;
            contact.Country = DEFAULT_STRING;
        }

        #endregion
    }
}
