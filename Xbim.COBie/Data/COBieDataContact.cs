using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.Rows;
using Xbim.Ifc.ActorResource;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.UtilityResource;
using Xbim.XbimExtensions;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the Contact tab.
    /// </summary>
    public class COBieDataContact : COBieData
    {
        /// <summary>
        /// Data Contact constructor
        /// </summary>
        /// <param name="model">IModel to read data from</param>
        public COBieDataContact(IModel model)
        {
            Model = model;
        }

        #region Methods

        /// <summary>
        /// Fill sheet rows for Contact sheet
        /// </summary>
        /// <returns>COBieSheet<COBieContactRow></returns>
        public COBieSheet<COBieContactRow> Fill()
        {
            //create new sheet
            COBieSheet<COBieContactRow> contacts = new COBieSheet<COBieContactRow>(Constants.WORKSHEET_CONTACT);

            IEnumerable<IfcOwnerHistory> ifcOwnerHistories = Model.InstancesOfType<IfcOwnerHistory>();

            foreach (IfcOwnerHistory oh in ifcOwnerHistories)
            {
                COBieContactRow contact = new COBieContactRow(contacts);

                // get person and organization
                IfcOrganization organization = oh.OwningUser.TheOrganization;
                IfcPerson person = oh.OwningUser.ThePerson;

                contact.Email = GetTelecomEmailAddress(oh);
                // check if this email is already in the contacts (as it only needs to exist once)
                bool emailExists = false;
                foreach (COBieContactRow c in contacts.Rows)
                {
                    if (c.Email == contact.Email)
                        emailExists = true;
                }

                // check if it belongs to ActorRole, if yes then add to contacts
                IEnumerable<IfcActorRole> ifcRoles = organization.Roles;
                if (emailExists == false)
                {
                    if (ifcRoles != null)
                    {
                        IfcActorRole ifcAR = ifcRoles.FirstOrDefault();
                        contact.Category = ifcAR.UserDefinedRole.ToString();

                        contact.CreatedBy = GetTelecomEmailAddress(oh);
                        contact.CreatedOn = GetCreatedOnDateAsFmtString(oh);

                        contact.Company = (string.IsNullOrEmpty(oh.OwningUser.TheOrganization.Name)) ? DEFAULT_VAL : oh.OwningUser.TheOrganization.Name.ToString();

                        IEnumerable<IfcTelecomAddress> telAddresses = Enumerable.Empty<IfcTelecomAddress>();
                        if (organization.Addresses != null)
                            telAddresses = organization.Addresses.TelecomAddresses;

                        contact.Phone = "";
                        foreach (IfcTelecomAddress ta in telAddresses)
                        {
                            foreach (IfcLabel phone in ta.TelephoneNumbers)
                                contact.Phone = (phone == null) ? "" : phone.ToString() + ",";
                        }
                        contact.Phone = contact.Phone.TrimEnd(',');

                        contact.ExtSystem = GetIfcApplication().ApplicationFullName;
                        contact.ExtObject = "IfcPersonAndOrganization";
                        contact.ExtIdentifier = person.Id;
                        contact.Department = (organization.Addresses == null || organization.Addresses.PostalAddresses == null || organization.Addresses.PostalAddresses.Count() == 0) ? DEFAULT_VAL : organization.Addresses.PostalAddresses.FirstOrDefault().InternalLocation.ToString();
                        if ((contact.Department == DEFAULT_VAL || contact.Department == "") && organization.Description != null) contact.Department = organization.Description;

                        // guideline say it should be organization.Name but example spreadsheet uses organization.Id
                        contact.OrganizationCode = (string.IsNullOrEmpty(organization.Id)) ? DEFAULT_VAL : organization.Id.ToString();

                        contact.GivenName = (string.IsNullOrEmpty(person.GivenName)) ? DEFAULT_VAL : person.GivenName.ToString();
                        contact.FamilyName = (string.IsNullOrEmpty(person.FamilyName)) ? DEFAULT_VAL : person.FamilyName.ToString();
                        contact.Street = (person.Addresses == null) ? "" : person.Addresses.PostalAddresses.FirstOrDefault().AddressLines.FirstOrDefault().Value.ToString();
                        contact.PostalBox = (person.Addresses == null) ? "" : person.Addresses.PostalAddresses.FirstOrDefault().PostalBox.ToString();
                        contact.Town = (person.Addresses == null) ? "" : person.Addresses.PostalAddresses.FirstOrDefault().Town.ToString();
                        contact.StateRegion = (person.Addresses == null) ? "" : person.Addresses.PostalAddresses.FirstOrDefault().Region.ToString();
                        contact.PostalCode = (person.Addresses == null) ? "" : person.Addresses.PostalAddresses.FirstOrDefault().PostalCode.ToString();
                        contact.Country = (person.Addresses == null) ? "" : person.Addresses.PostalAddresses.FirstOrDefault().Country.ToString();

                        contacts.Rows.Add(contact);
                    }
                }
            }

            return contacts;
        }

        #endregion
    }
}
