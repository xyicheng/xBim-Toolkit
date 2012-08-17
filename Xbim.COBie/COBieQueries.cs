using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.UtilityResource;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.SelectTypes;
using Xbim.Ifc.SharedBldgElements;
using Xbim.Ifc.SharedFacilitiesElements;
using Xbim.Ifc.HVACDomain;
using Xbim.Ifc.PlumbingFireProtectionDomain;
using Xbim.Ifc.SharedBldgServiceElements;
using Xbim.Ifc.ActorResource;
using Xbim.Ifc.ExternalReferenceResource;
using Xbim.Ifc.GeometryResource;
using Xbim.Ifc.ApprovalResource;
using Xbim.Ifc.ProcessExtensions;
using Xbim.Ifc.ConstructionMgmtDomain;
using System.Xml;
using Xbim.COBie.Rows;
using Xbim.Ifc.QuantityResource;
using Xbim.Ifc.PropertyResource;
using Xbim.Ifc.Extensions;
using Xbim.Ifc.MaterialResource;
using System.Reflection;
using System.Diagnostics;
using System.Timers;

namespace Xbim.COBie
{
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

    public class COBieQueries
    {

        IModel _model;
        const string DEFAULT_VAL = "n/a";
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
            //return (CreatedOnTStamp <= 0) ? "Unknown" : IfcTimeStamp.ToFormattedString(CreatedOnTStamp);
            if (CreatedOnTStamp <= 0)
            {
                return DateTime.Now.ToString(strFormat);
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

        #endregion

        #region Contact

        public COBieSheet<COBieContactRow> GetCOBieContactSheet(IModel model, COBieSheet<COBiePickListsRow> pickLists)
        {
            _model = model;

            // get all IfcBuildingStory objects from IFC file
            //IEnumerable<IfcPersonAndOrganization> personsOrganizations = model.InstancesOfType<IfcPersonAndOrganization>();
            IEnumerable<IfcOwnerHistory> ifcOwnerHistories = model.InstancesOfType<IfcOwnerHistory>();
            
            //IfcPostalAddress address = model.InstancesOfType<IfcPostalAddress>().FirstOrDefault();
            COBieSheet<COBieContactRow> contacts = new COBieSheet<COBieContactRow>(Constants.WORKSHEET_CONTACT);

            foreach (IfcOwnerHistory oh in ifcOwnerHistories)
            {
                COBieContactRow contact = new COBieContactRow(contacts);

                // get person and organization
                IfcOrganization organization = oh.OwningUser.TheOrganization;
                IfcPerson person = oh.OwningUser.ThePerson;

                contact.Email = GetTelecomEmailAddress(oh);
                // check if this email is alerady in the contacts (as it only needs to exist once)
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

        #region Document

        public COBieSheet<COBieDocumentRow> GetCOBieDocumentSheet(IModel model, COBieSheet<COBiePickListsRow> pickLists)
        {
            _model = model;

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcDocumentInformation> docInfos = model.InstancesOfType<IfcDocumentInformation>();

            IEnumerable<IfcTelecomAddress> ifcTelecomAddresses = model.InstancesOfType<IfcTelecomAddress>();
            if (ifcTelecomAddresses == null) ifcTelecomAddresses = Enumerable.Empty<IfcTelecomAddress>();

            IfcOwnerHistory ifcOwnerHistory = model.InstancesOfType<IfcOwnerHistory>().FirstOrDefault();
            COBieSheet<COBieDocumentRow> documents = new COBieSheet<COBieDocumentRow>(Constants.WORKSHEET_DOCUMENT);

            foreach (IfcDocumentInformation di in docInfos)
            {
                COBieDocumentRow doc = new COBieDocumentRow(documents);
                doc.Name = (di == null) ? "" : di.Name.ToString();

                doc.CreatedBy = GetTelecomEmailAddress(ifcOwnerHistory);
                doc.CreatedOn = GetCreatedOnDateAsFmtString(ifcOwnerHistory);
                
                //IfcRelAssociatesClassification ifcRAC = di.HasAssociations.OfType<IfcRelAssociatesClassification>().FirstOrDefault();
                //IfcClassificationReference ifcCR = (IfcClassificationReference)ifcRAC.RelatingClassification;
                doc.Category = "";

                doc.ApprovalBy = di.IntendedUse.ToString();
                doc.Stage = di.Scope.ToString();

                doc.SheetName = "";
                foreach (COBiePickListsRow plRow in pickLists.Rows)
                    doc.SheetName = (plRow == null) ? "" : plRow.SheetType + ",";
                doc.SheetName = doc.SheetName.TrimEnd(',');

                doc.RowName = DEFAULT_VAL;
                doc.Directory = di.DocumentId.ToString();
                doc.File = di.DocumentId.ToString();
                doc.ExtSystem = GetIfcApplication().ApplicationFullName;
                doc.ExtObject = di.GetType().Name.ToString();
                doc.ExtIdentifier = di.DocumentId.ToString();
                doc.Description = di.Description.ToString();
                doc.Reference = di.Name.ToString();

                documents.Rows.Add(doc);
            }

            return documents;
        }

        private string GetDocumentCategory(IfcBuildingStorey bs)
        {
            return (bs.LongName == null) ? "Category" : bs.LongName.ToString();
        }

        private string GetDocumentDescription(IfcBuildingStorey bs)
        {
            if (bs != null)
            {
                if (!string.IsNullOrEmpty(bs.LongName)) return bs.LongName;
                else if (!string.IsNullOrEmpty(bs.Description)) return bs.Description;
                else if (!string.IsNullOrEmpty(bs.Name)) return bs.Name;
            }
            return DEFAULT_VAL;
        }

        #endregion

        #region Impact

        public COBieSheet<COBieImpactRow> GetCOBieImpactSheet(IModel model, COBieSheet<COBiePickListsRow> pickLists)
        {
            _model = model;

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcPropertySet> ifcProperties = model.InstancesOfType<IfcPropertySet>();

            COBieSheet<COBieImpactRow> impacts = new COBieSheet<COBieImpactRow>(Constants.WORKSHEET_IMPACT);

            foreach (IfcPropertySet ppt in ifcProperties)
            {
                COBieImpactRow impact = new COBieImpactRow(impacts);

                //IfcOwnerHistory ifcOwnerHistory = ppt.OwnerHistory;

                impact.Name = ppt.Name;

                impact.CreatedBy = GetTelecomEmailAddress(ppt.OwnerHistory);
                impact.CreatedOn = GetCreatedOnDateAsFmtString(ppt.OwnerHistory); 

                impact.ImpactType = "";
                impact.ImpactStage = "";
                impact.SheetName = "";
                foreach (COBiePickListsRow plRow in pickLists.Rows)
                {
                    impact.ImpactType = (plRow == null) ? "" : plRow.ImpactType + ",";
                    impact.ImpactStage = (plRow == null) ? "" : plRow.ImpactStage + ",";
                    impact.SheetName = (plRow == null) ? "" : plRow.SheetType + ",";
                }
                impact.ImpactType = impact.ImpactType.TrimEnd(',');
                impact.ImpactStage = impact.ImpactStage.TrimEnd(',');
                impact.SheetName = impact.SheetName.TrimEnd(',');

                impact.RowName = DEFAULT_VAL;
                impact.Value = "";
                impact.ImpactUnit = "";
                impact.LeadInTime = "";
                impact.Duration = "";
                impact.LeadOutTime = "";
                impact.ExtSystem = GetIfcApplication().ApplicationFullName;
                impact.ExtObject = impact.GetType().Name;
                impact.ExtIdentifier = ppt.GlobalId;
                impact.Description = (ppt.Description == null) ? DEFAULT_VAL : ppt.Description.ToString();

                impacts.Rows.Add(impact);
            }

            return impacts;
        }

        #endregion

        #region Issue

        public COBieSheet<COBieIssueRow> GetCOBieIssueSheet(IModel model, COBieSheet<COBiePickListsRow> pickLists)
        {
            _model = model;

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcApproval> ifcApprovals = model.InstancesOfType<IfcApproval>();

            IEnumerable<IfcTelecomAddress> ifcTelecomAddresses = model.InstancesOfType<IfcTelecomAddress>();
            if (ifcTelecomAddresses == null) ifcTelecomAddresses = Enumerable.Empty<IfcTelecomAddress>();

            IfcOwnerHistory ifcOwnerHistory = model.InstancesOfType<IfcOwnerHistory>().FirstOrDefault();
            IfcApproval approval = model.InstancesOfType<IfcApproval>().FirstOrDefault();
            COBieSheet<COBieIssueRow> issues = new COBieSheet<COBieIssueRow>(Constants.WORKSHEET_ISSUE);

            foreach (IfcApproval app in ifcApprovals)
            {
                COBieIssueRow issue = new COBieIssueRow(issues);
                issue.Name = (approval == null) ? "" : approval.Name.ToString();

                //TODO: See if we can get CreatedOn and CreatedBy from app object
                issue.CreatedBy = GetTelecomEmailAddress(ifcOwnerHistory);
                issue.CreatedOn = GetCreatedOnDateAsFmtString(ifcOwnerHistory);              

                issue.Type = "";
                issue.Risk = "";
                issue.Chance = "";
                issue.Impact = "";
                issue.SheetName1 = "";
                foreach (COBiePickListsRow plRow in pickLists.Rows)
                {
                    issue.Type = plRow.IssueCategory + ",";
                    issue.Risk = plRow.IssueRisk + ",";
                    issue.Chance = plRow.IssueChance + ",";
                    issue.Impact = plRow.IssueImpact + ",";
                    issue.SheetName1 = plRow.SheetType + ",";
                }
                issue.Type = issue.Type.TrimEnd(',');
                issue.Risk = issue.Risk.TrimEnd(',');
                issue.Chance = issue.Chance.TrimEnd(',');
                issue.Impact = issue.Impact.TrimEnd(',');
                issue.SheetName1 = issue.SheetName1.TrimEnd(',');
                
                issue.RowName1 = DEFAULT_VAL;
                issue.SheetName2 = "";
                issue.RowName2 = DEFAULT_VAL;
                issue.Description = (approval == null) ? DEFAULT_VAL : approval.Description.ToString();
                issue.Owner = issue.CreatedBy;
                issue.Mitigation = "";
                issue.ExtSystem = GetIfcApplication().ApplicationFullName;
                issue.ExtObject = app.GetType().Name;
                issue.ExtIdentifier = app.Identifier.ToString();                

                issues.Rows.Add(issue);
            }

            return issues;
        }

        #endregion

        #region Job

        public COBieSheet<COBieJobRow> GetCOBieJobSheet(IModel model, COBieSheet<COBiePickListsRow> pickLists)
        {
            _model = model;

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcTask> ifcTasks = model.InstancesOfType<IfcTask>();

            IfcTypeObject typObj = model.InstancesOfType<IfcTypeObject>().FirstOrDefault();
            IfcConstructionEquipmentResource cer = model.InstancesOfType<IfcConstructionEquipmentResource>().FirstOrDefault();

            COBieSheet<COBieJobRow> jobs = new COBieSheet<COBieJobRow>(Constants.WORKSHEET_JOB);

            foreach (IfcTask task in ifcTasks)
            {
                COBieJobRow job = new COBieJobRow(jobs);

                //IfcOwnerHistory ifcOwnerHistory = task.OwnerHistory;

                job.Name = (task == null) ? "" : task.Name.ToString();

                job.CreatedBy = GetTelecomEmailAddress(task.OwnerHistory);
                job.CreatedOn = GetCreatedOnDateAsFmtString(task.OwnerHistory);
                
                //job.Category = (task == null) ? "" : task.ObjectType.ToString();
                IfcRelAssociatesClassification ifcRAC = task.HasAssociations.OfType<IfcRelAssociatesClassification>().FirstOrDefault();
                if (ifcRAC != null)
                {
                    IfcClassificationReference ifcCR = (IfcClassificationReference)ifcRAC.RelatingClassification;
                    job.Category = ifcCR.Name;
                }
                else
                    job.Category = "";

                job.Status = (task == null) ? "" : task.Status.ToString();

                job.TypeName = (task.ObjectType == null) ? "" : task.ObjectType.ToString();
                job.Description = (task == null) ? "" : task.Description.ToString();
                job.Duration = "";

                job.DurationUnit = "";
                job.TaskStartUnit = "";
                job.FrequencyUnit = "";
                foreach (COBiePickListsRow plRow in pickLists.Rows)
                {
                    job.DurationUnit = (plRow == null) ? "" : plRow.DurationUnit + ",";
                    job.TaskStartUnit = (plRow == null) ? "" : plRow.DurationUnit + ",";
                    job.FrequencyUnit = (plRow == null) ? "" : plRow.DurationUnit + ",";
                }
                job.DurationUnit = job.DurationUnit.TrimEnd(',');
                job.TaskStartUnit = job.TaskStartUnit.TrimEnd(',');
                job.FrequencyUnit = job.FrequencyUnit.TrimEnd(',');
                
                job.Start = "";
                job.Frequency = "";
                
                job.ExtSystem = GetIfcApplication().ApplicationFullName;
                job.ExtObject = task.GetType().Name;
                job.ExtIdentifier = task.GlobalId;
                job.TaskNumber = (task == null) ? "" : task.GlobalId.ToString();
                job.Priors = (task == null) ? "" : task.Name.ToString();
                job.ResourceNames = (cer == null) ? "" : cer.Name.ToString();

                jobs.Rows.Add(job);
            }

            return jobs;
        }

        #endregion

        #region PickLists

        // Populate PickLists by row

        //public COBieSheet<COBiePickListsRow> GetCOBiePickListsSheet(string pickListsXMLFilePath)
        //{
        //    // read xml document for picklists
        //    if (string.IsNullOrEmpty(pickListsXMLFilePath)) pickListsXMLFilePath = "PickLists.xml";
        //    XmlDocument xdoc = new XmlDocument();
        //    xdoc.Load(pickListsXMLFilePath);
        //    XmlNodeList items = xdoc.SelectNodes("//PickLists//Item");

        //    COBieSheet<COBiePickListsRow> pickLists = new COBieSheet<COBiePickListsRow>();

        //    foreach (XmlNode node in items)
        //    {
        //        COBiePickListsRow pickList = new COBiePickListsRow();
        //        XmlElement itemEle = (XmlElement)node;

        //        pickList.ApprovalBy = itemEle.GetElementsByTagName("ApprovalBy")[0].InnerText;
        //        pickList.AreaUnit = itemEle.GetElementsByTagName("AreaUnit")[0].InnerText;
        //        pickList.AssetType = itemEle.GetElementsByTagName("AssetType")[0].InnerText;
        //        pickList.CategoryFacility = itemEle.GetElementsByTagName("Category-Facility")[0].InnerText;
        //        pickList.CategorySpace = itemEle.GetElementsByTagName("Category-Space")[0].InnerText;
        //        pickList.CategoryElement = itemEle.GetElementsByTagName("Category-Element")[0].InnerText;
        //        pickList.CategoryProduct = itemEle.GetElementsByTagName("Category-Product")[0].InnerText;
        //        pickList.CategoryRole = itemEle.GetElementsByTagName("Category-Role")[0].InnerText;
        //        pickList.CoordinateSheet = itemEle.GetElementsByTagName("CoordinateSheet")[0].InnerText;
        //        pickList.ConnectionType = itemEle.GetElementsByTagName("ConnectionType")[0].InnerText;
        //        pickList.CoordinateType = itemEle.GetElementsByTagName("CoordinateType")[0].InnerText;
        //        pickList.DocumentType = itemEle.GetElementsByTagName("DocumentType")[0].InnerText;
        //        pickList.DurationUnit = itemEle.GetElementsByTagName("DurationUnit")[0].InnerText;
        //        pickList.FloorType = itemEle.GetElementsByTagName("FloorType")[0].InnerText;
        //        pickList.IssueCategory = itemEle.GetElementsByTagName("IssueCategory")[0].InnerText;
        //        pickList.IssueChance = itemEle.GetElementsByTagName("IssueChance")[0].InnerText;
        //        pickList.IssueImpact = itemEle.GetElementsByTagName("IssueImpact")[0].InnerText;
        //        pickList.IssueRisk = itemEle.GetElementsByTagName("IssueRisk")[0].InnerText;
        //        pickList.JobStatusType = itemEle.GetElementsByTagName("JobStatusType")[0].InnerText;
        //        pickList.JobType = itemEle.GetElementsByTagName("JobType")[0].InnerText;
        //        pickList.ObjAttribute = itemEle.GetElementsByTagName("objAttribute")[0].InnerText;
        //        pickList.ObjAttributeType = itemEle.GetElementsByTagName("objAttributeType")[0].InnerText;
        //        pickList.ObjComponent = itemEle.GetElementsByTagName("objComponent")[0].InnerText;
        //        pickList.ObjConnection = itemEle.GetElementsByTagName("objConnection")[0].InnerText;
        //        pickList.ObjContact = itemEle.GetElementsByTagName("objContact")[0].InnerText;
        //        pickList.ObjCoordinate = itemEle.GetElementsByTagName("objCoordinate")[0].InnerText;
        //        pickList.ObjDocument = itemEle.GetElementsByTagName("objDocument")[0].InnerText;
        //        pickList.ObjFacility = itemEle.GetElementsByTagName("objFacility")[0].InnerText;
        //        pickList.ObjFloor = itemEle.GetElementsByTagName("objFloor")[0].InnerText;
        //        pickList.ObjIssue = itemEle.GetElementsByTagName("objIssue")[0].InnerText;
        //        pickList.ObjJob = itemEle.GetElementsByTagName("objJob")[0].InnerText;
        //        pickList.ObjProject = itemEle.GetElementsByTagName("objProject")[0].InnerText;
        //        pickList.ObjResource = itemEle.GetElementsByTagName("objResource")[0].InnerText;
        //        pickList.ObjSite = itemEle.GetElementsByTagName("objSite")[0].InnerText;
        //        pickList.ObjSpace = itemEle.GetElementsByTagName("objSpace")[0].InnerText;
        //        pickList.ObjSpare = itemEle.GetElementsByTagName("objSpare")[0].InnerText;
        //        pickList.ObjSystem = itemEle.GetElementsByTagName("objSystem")[0].InnerText;
        //        pickList.ObjType = itemEle.GetElementsByTagName("objType")[0].InnerText;
        //        pickList.ObjWarranty = itemEle.GetElementsByTagName("objWarranty")[0].InnerText;
        //        pickList.ObjZone = itemEle.GetElementsByTagName("objZone")[0].InnerText;
        //        pickList.ResourceType = itemEle.GetElementsByTagName("ResourceType")[0].InnerText;
        //        pickList.SheetType = itemEle.GetElementsByTagName("SheetType")[0].InnerText;
        //        pickList.SpareType = itemEle.GetElementsByTagName("SpareType")[0].InnerText;
        //        pickList.StageType = itemEle.GetElementsByTagName("StageType")[0].InnerText;
        //        pickList.ZoneType = itemEle.GetElementsByTagName("ZoneType")[0].InnerText;
        //        pickList.LinearUnit = itemEle.GetElementsByTagName("LinearUnit")[0].InnerText;
        //        pickList.VolumeUnit = itemEle.GetElementsByTagName("VolumeUnit")[0].InnerText;
        //        pickList.CostUnit = itemEle.GetElementsByTagName("CostUnit")[0].InnerText;
        //        pickList.AssemblyType = itemEle.GetElementsByTagName("AssemblyType")[0].InnerText;
        //        pickList.ImpactType = itemEle.GetElementsByTagName("ImpactType")[0].InnerText;
        //        pickList.ImpactStage = itemEle.GetElementsByTagName("ImpactStage")[0].InnerText;
        //        pickList.ImpactUnit = itemEle.GetElementsByTagName("ImpactUnit")[0].InnerText;
        //        pickList.ObjAssembly = itemEle.GetElementsByTagName("objAssembly")[0].InnerText;
        //        pickList.ObjImpact = itemEle.GetElementsByTagName("objImpact")[0].InnerText;

        //        pickLists.Rows.Add(pickList);
        //    }

        //    return pickLists;
        //}

        // Populate PickLists by column
        public COBieSheet<COBiePickListsRow> GetCOBiePickListsSheet(string pickListsXMLFilePath)
        {
            // read xml document for picklists
            if (string.IsNullOrEmpty(pickListsXMLFilePath)) pickListsXMLFilePath = "PickLists.xml";
            XmlDocument xdoc = new XmlDocument();
            xdoc.Load(pickListsXMLFilePath);
            XmlNodeList items = xdoc.SelectNodes("//PickLists//Item");

            COBieSheet<COBiePickListsRow> pickLists = new COBieSheet<COBiePickListsRow>(Constants.WORKSHEET_PICKLISTS);

            foreach (XmlNode node in items)
            {
                COBiePickListsRow pickList = new COBiePickListsRow(pickLists);
                XmlElement itemEle = (XmlElement)node;

                pickList.ApprovalBy = itemEle.GetElementsByTagName("ApprovalBy")[0].InnerText;
                pickList.AreaUnit = itemEle.GetElementsByTagName("AreaUnit")[0].InnerText;
                pickList.AssetType = itemEle.GetElementsByTagName("AssetType")[0].InnerText;
                pickList.CategoryFacility = itemEle.GetElementsByTagName("Category-Facility")[0].InnerText;
                pickList.CategorySpace = itemEle.GetElementsByTagName("Category-Space")[0].InnerText;
                pickList.CategoryElement = itemEle.GetElementsByTagName("Category-Element")[0].InnerText;
                pickList.CategoryProduct = itemEle.GetElementsByTagName("Category-Product")[0].InnerText;
                pickList.CategoryRole = itemEle.GetElementsByTagName("Category-Role")[0].InnerText;
                pickList.CoordinateSheet = itemEle.GetElementsByTagName("CoordinateSheet")[0].InnerText;
                pickList.ConnectionType = itemEle.GetElementsByTagName("ConnectionType")[0].InnerText;
                pickList.CoordinateType = itemEle.GetElementsByTagName("CoordinateType")[0].InnerText;
                pickList.DocumentType = itemEle.GetElementsByTagName("DocumentType")[0].InnerText;
                pickList.DurationUnit = itemEle.GetElementsByTagName("DurationUnit")[0].InnerText;
                pickList.FloorType = itemEle.GetElementsByTagName("FloorType")[0].InnerText;
                pickList.IssueCategory = itemEle.GetElementsByTagName("IssueCategory")[0].InnerText;
                pickList.IssueChance = itemEle.GetElementsByTagName("IssueChance")[0].InnerText;
                pickList.IssueImpact = itemEle.GetElementsByTagName("IssueImpact")[0].InnerText;
                pickList.IssueRisk = itemEle.GetElementsByTagName("IssueRisk")[0].InnerText;
                pickList.JobStatusType = itemEle.GetElementsByTagName("JobStatusType")[0].InnerText;
                pickList.JobType = itemEle.GetElementsByTagName("JobType")[0].InnerText;
                pickList.ObjAttribute = itemEle.GetElementsByTagName("objAttribute")[0].InnerText;
                pickList.ObjAttributeType = itemEle.GetElementsByTagName("objAttributeType")[0].InnerText;
                pickList.ObjComponent = itemEle.GetElementsByTagName("objComponent")[0].InnerText;
                pickList.ObjConnection = itemEle.GetElementsByTagName("objConnection")[0].InnerText;
                pickList.ObjContact = itemEle.GetElementsByTagName("objContact")[0].InnerText;
                pickList.ObjCoordinate = itemEle.GetElementsByTagName("objCoordinate")[0].InnerText;
                pickList.ObjDocument = itemEle.GetElementsByTagName("objDocument")[0].InnerText;
                pickList.ObjFacility = itemEle.GetElementsByTagName("objFacility")[0].InnerText;
                pickList.ObjFloor = itemEle.GetElementsByTagName("objFloor")[0].InnerText;
                pickList.ObjIssue = itemEle.GetElementsByTagName("objIssue")[0].InnerText;
                pickList.ObjJob = itemEle.GetElementsByTagName("objJob")[0].InnerText;
                pickList.ObjProject = itemEle.GetElementsByTagName("objProject")[0].InnerText;
                pickList.ObjResource = itemEle.GetElementsByTagName("objResource")[0].InnerText;
                pickList.ObjSite = itemEle.GetElementsByTagName("objSite")[0].InnerText;
                pickList.ObjSpace = itemEle.GetElementsByTagName("objSpace")[0].InnerText;
                pickList.ObjSpare = itemEle.GetElementsByTagName("objSpare")[0].InnerText;
                pickList.ObjSystem = itemEle.GetElementsByTagName("objSystem")[0].InnerText;
                pickList.ObjType = itemEle.GetElementsByTagName("objType")[0].InnerText;
                pickList.ObjWarranty = itemEle.GetElementsByTagName("objWarranty")[0].InnerText;
                pickList.ObjZone = itemEle.GetElementsByTagName("objZone")[0].InnerText;
                pickList.ResourceType = itemEle.GetElementsByTagName("ResourceType")[0].InnerText;
                pickList.SheetType = itemEle.GetElementsByTagName("SheetType")[0].InnerText;
                pickList.SpareType = itemEle.GetElementsByTagName("SpareType")[0].InnerText;
                pickList.StageType = itemEle.GetElementsByTagName("StageType")[0].InnerText;
                pickList.ZoneType = itemEle.GetElementsByTagName("ZoneType")[0].InnerText;
                pickList.LinearUnit = itemEle.GetElementsByTagName("LinearUnit")[0].InnerText;
                pickList.VolumeUnit = itemEle.GetElementsByTagName("VolumeUnit")[0].InnerText;
                pickList.CostUnit = itemEle.GetElementsByTagName("CostUnit")[0].InnerText;
                pickList.AssemblyType = itemEle.GetElementsByTagName("AssemblyType")[0].InnerText;
                pickList.ImpactType = itemEle.GetElementsByTagName("ImpactType")[0].InnerText;
                pickList.ImpactStage = itemEle.GetElementsByTagName("ImpactStage")[0].InnerText;
                pickList.ImpactUnit = itemEle.GetElementsByTagName("ImpactUnit")[0].InnerText;
                pickList.ObjAssembly = itemEle.GetElementsByTagName("objAssembly")[0].InnerText;
                pickList.ObjImpact = itemEle.GetElementsByTagName("objImpact")[0].InnerText;

                pickLists.Rows.Add(pickList);
            }

            return pickLists;
        }

        private string GetPickListCategory(IfcBuildingStorey bs)
        {
            return (bs.LongName == null) ? "Category" : bs.LongName.ToString();
        }

        private string GetPickListDescription(IfcBuildingStorey bs)
        {
            if (bs != null)
            {
                if (!string.IsNullOrEmpty(bs.LongName)) return bs.LongName;
                else if (!string.IsNullOrEmpty(bs.Description)) return bs.Description;
                else if (!string.IsNullOrEmpty(bs.Name)) return bs.Name;
            }
            return DEFAULT_VAL;
        }

        #endregion

        #region Resource

        public COBieSheet<COBieResourceRow> GetCOBieResourceSheet(IModel model)
        {
            _model = model;

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcConstructionEquipmentResource> ifcCer = model.InstancesOfType<IfcConstructionEquipmentResource>();

            COBieSheet<COBieResourceRow> resources = new COBieSheet<COBieResourceRow>(Constants.WORKSHEET_RESOURCE);

            foreach (IfcConstructionEquipmentResource cer in ifcCer)
            {
                COBieResourceRow resource = new COBieResourceRow(resources);
                //IfcOwnerHistory ifcOwnerHistory = cer.OwnerHistory;

                resource.Name = (cer == null) ? "" : cer.Name.ToString();

                resource.CreatedBy = GetTelecomEmailAddress(cer.OwnerHistory);
                resource.CreatedOn = GetCreatedOnDateAsFmtString(cer.OwnerHistory); 
                
                resource.Category = (cer == null) ? "" : cer.ObjectType.ToString();                
                //IfcRelAssociatesClassification ifcRAC = to.HasAssociations.OfType<IfcRelAssociatesClassification>().FirstOrDefault();
                //IfcClassificationReference ifcCR = (IfcClassificationReference)ifcRAC.RelatingClassification;
                //resource.Category = ifcCR.Name;

                resource.ExtSystem = GetIfcApplication().ApplicationFullName;
                resource.ExtObject = cer.GetType().Name;
                resource.ExtIdentifier = cer.GlobalId;
                resource.Description = (cer == null) ? "" : cer.Description.ToString();

                resources.Rows.Add(resource);
            }

            return resources;
        }

        #endregion

        #region Floor

        public COBieSheet<COBieFloorRow> GetCOBieFloorSheet(IModel model, COBieSheet<COBiePickListsRow> pickLists)
        {
            _model = model;

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcBuildingStorey> buildingStories = model.InstancesOfType<IfcBuildingStorey>();

            COBieSheet<COBieFloorRow> floors = new COBieSheet<COBieFloorRow>(Constants.WORKSHEET_FLOOR);

            IfcClassification ifcClassification = model.InstancesOfType<IfcClassification>().FirstOrDefault();
            
            foreach (IfcBuildingStorey bs in buildingStories)
            {
                COBieFloorRow floor = new COBieFloorRow(floors);

                //IfcOwnerHistory ifcOwnerHistory = bs.OwnerHistory;

                floor.Name = bs.Name.ToString();

                floor.CreatedBy = GetTelecomEmailAddress(bs.OwnerHistory);
                floor.CreatedOn = GetCreatedOnDateAsFmtString(bs.OwnerHistory);
                                
                IfcRelAssociatesClassification ifcRAC = bs.HasAssociations.OfType<IfcRelAssociatesClassification>().FirstOrDefault();
                if (ifcRAC != null)
                {
                    IfcClassificationReference ifcCR = (IfcClassificationReference)ifcRAC.RelatingClassification;
                    floor.Category = ifcCR.Name;
                }
                else
                    floor.Category = "";

                floor.ExtSystem = GetIfcApplication().ApplicationFullName;
                floor.ExtObject = bs.GetType().Name;
                floor.ExtIdentifier = bs.GlobalId;
                floor.Description = GetFloorDescription(bs);
                floor.Elevation = bs.Elevation.ToString();
                IEnumerable<IfcQuantityLength> qLen = bs.IsDefinedByProperties.Select(p => p.RelatedObjects.OfType<IfcQuantityLength>()).FirstOrDefault();
                floor.Height = (qLen.FirstOrDefault() == null) ? "0" : qLen.FirstOrDefault().LengthValue.ToString();

                floors.Rows.Add(floor);                
            }
            
            return floors;
        }

        private string GetFloorDescription(IfcBuildingStorey bs)
        {
            if (bs != null)
            {
                if (!string.IsNullOrEmpty(bs.LongName)) return bs.LongName;
                else if (!string.IsNullOrEmpty(bs.Description)) return bs.Description;
                else if (!string.IsNullOrEmpty(bs.Name)) return bs.Name;
            }
            return DEFAULT_VAL;
        }

        #endregion

        #region Space

        public COBieSheet<COBieSpaceRow> GetCOBieSpaceSheet(IModel model, COBieSheet<COBiePickListsRow> pickLists)
        {
            _model = model;

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcSpace> ifcSpaces = model.InstancesOfType<IfcSpace>();//.OrderBy(ifcSpace => ifcSpace.Name, new CompareIfcLabel());

            COBieSheet<COBieSpaceRow> spaces = new COBieSheet<COBieSpaceRow>(Constants.WORKSHEET_SPACE);

            foreach (IfcSpace sp in ifcSpaces)
            {
                COBieSpaceRow space = new COBieSpaceRow(spaces);

                //IfcOwnerHistory ifcOwnerHistory = sp.OwnerHistory;
                
                space.Name = sp.Name;

                space.CreatedBy = GetTelecomEmailAddress(sp.OwnerHistory);
                space.CreatedOn = GetCreatedOnDateAsFmtString(sp.OwnerHistory); 
                
                space.Category = space.GetCategory(sp);

                space.FloorName = sp.SpatialStructuralElementParent.Name.ToString();
                space.Description = GetSpaceDescription(sp);
                space.ExtSystem = GetIfcApplication().ApplicationFullName;
                space.ExtObject = sp.GetType().Name;
                space.ExtIdentifier = sp.GlobalId;
                space.RoomTag = GetSpaceDescription(sp);
                //Do Usable Height
                IfcLengthMeasure usableHt = sp.GetHeight();
                if (usableHt != null) space.UsableHeight = ((double)usableHt).ToString("F3");
                else space.UsableHeight = DEFAULT_VAL;
                
                //Do Gross Areas 
                IfcAreaMeasure grossAreaValue = sp.GetGrossFloorArea();
                //if we fail on try GSA keys
                IfcQuantityArea spArea = null; 
                if (grossAreaValue == null) spArea = sp.GetQuantity<IfcQuantityArea>("GSA Space Areas", "GSA BIM Area");
                
                if (grossAreaValue != null) space.GrossArea = ((double)grossAreaValue).ToString("F3");
                else if ((spArea is IfcQuantityArea) && (spArea.AreaValue != null)) space.GrossArea = ((double)spArea.AreaValue).ToString("F3");
                else space.GrossArea = DEFAULT_VAL;

                //Do Net Areas 
                IfcAreaMeasure netAreaValue = sp.GetNetFloorArea();  //this extension has the GSA built in so no need to get again
                if (netAreaValue != null) space.NetArea = ((double)netAreaValue).ToString("F3");
                else space.NetArea = DEFAULT_VAL;       

                spaces.Rows.Add(space);
            }

            return spaces;
        }

       
        private string GetSpaceCategory(IfcSpace sp)
        {
            return sp.LongName;
        }

        private string GetSpaceDescription(IfcSpace sp)
        {
            if (sp != null)
            {
                if (!string.IsNullOrEmpty(sp.LongName)) return sp.LongName;
                else if (!string.IsNullOrEmpty(sp.Description)) return sp.Description;
                else if (!string.IsNullOrEmpty(sp.Name)) return sp.Name;
            }
            return DEFAULT_VAL;
        }

        #endregion

        #region Facility

        public COBieSheet<COBieFacilityRow> GetCOBieFacilitySheet(IModel model, COBieSheet<COBiePickListsRow> pickLists)
        {
            _model = model;

            IfcProject ifcProject = _model.IfcProject;
            IfcSite ifcSite = _model.InstancesOfType<IfcSite>().FirstOrDefault();
            IfcBuilding ifcBuilding = _model.InstancesOfType<IfcBuilding>().FirstOrDefault();
            IfcMonetaryUnit ifcMonetaryUnit = _model.InstancesOfType<IfcMonetaryUnit>().FirstOrDefault();
            IfcElementQuantity ifcElementQuantity = _model.InstancesOfType<IfcElementQuantity>().FirstOrDefault();
                        
            //IEnumerable<IfcTelecomAddress> ifcTelecomAddresses = model.InstancesOfType<IfcTelecomAddress>();
            //if (ifcTelecomAddresses == null) ifcTelecomAddresses = Enumerable.Empty<IfcTelecomAddress>();

            //IfcOwnerHistory ifcOwnerHistory = ifcBuilding.OwnerHistory; //model.InstancesOfType<IfcOwnerHistory>().FirstOrDefault();
            COBieSheet<COBieFacilityRow> facilities = new COBieSheet<COBieFacilityRow>(Constants.WORKSHEET_FACILITY);

            COBieFacilityRow facility = new COBieFacilityRow(facilities);

            facility.Name = ifcBuilding.Name.ToString();

            facility.CreatedBy = GetTelecomEmailAddress(ifcBuilding.OwnerHistory);
            facility.CreatedOn = GetCreatedOnDateAsFmtString(ifcBuilding.OwnerHistory); 
            
            //facility.Category = "";
            //foreach (COBiePickListsRow plRow in pickLists.Rows)
            //    if (plRow != null)
            //        facility.Category += plRow.CategoryFacility + ",";
            //facility.Category = facility.Category.TrimEnd(',');
            IfcRelAssociatesClassification ifcRAC = ifcBuilding.HasAssociations.OfType<IfcRelAssociatesClassification>().FirstOrDefault();
            if (ifcRAC != null)
            {
                IfcClassificationReference ifcCR = (IfcClassificationReference)ifcRAC.RelatingClassification;
                facility.Category = ifcCR.Name;
            }
            else 
                facility.Category = "";

            facility.ProjectName = GetFacilityProjectName(ifcProject);
            facility.SiteName = GetFacilitySiteName(ifcSite);
            facility.LinearUnits = GetLinearUnits();
            facility.AreaUnits = GetAreaUnits();
            facility.VolumeUnits = GetVolumeUnits();
            facility.CurrencyUnit = (ifcMonetaryUnit == null) ? DEFAULT_VAL : ifcMonetaryUnit.Currency.ToString();
            facility.AreaMeasurement = (ifcElementQuantity == null) ? "" : ifcElementQuantity.MethodOfMeasurement.ToString();
            facility.ExtSystem = GetIfcApplication().ApplicationFullName;

            facility.ExtProjectObject = "IfcProject";
            facility.ExtProjectIdentifier = ifcProject.GlobalId;
            
            facility.ExtSiteObject = "IfcSite";
            facility.ExtSiteIdentifier = ifcSite.GlobalId;

            facility.ExtFacilityObject = "IfcBuilding";
            facility.ExtFacilityIdentifier = ifcBuilding.GlobalId;
            
            facility.Description = GetFacilityDescription(ifcBuilding);
            facility.ProjectDescription = GetFacilityProjectDescription(ifcProject);
            facility.SiteDescription = GetFacilitySiteDescription(ifcSite);
            facility.Phase = _model.IfcProject.Phase;

            facilities.Rows.Add(facility);

            return facilities;
        }

        private string GetFacilityDescription(IfcBuilding ifcBuilding)
        {
            if (ifcBuilding != null)
            {
                if (!string.IsNullOrEmpty(ifcBuilding.LongName)) return ifcBuilding.LongName;
                else if (!string.IsNullOrEmpty(ifcBuilding.Description)) return ifcBuilding.Description;
                else if (!string.IsNullOrEmpty(ifcBuilding.Name)) return ifcBuilding.Name;
            }
            return DEFAULT_VAL;
        }

        private string GetFacilityProjectDescription(IfcProject ifcProject)
        {
            if (ifcProject != null)
            {
                if (!string.IsNullOrEmpty(ifcProject.LongName)) return ifcProject.LongName;
                else if (!string.IsNullOrEmpty(ifcProject.Description)) return ifcProject.Description;
                else if (!string.IsNullOrEmpty(ifcProject.Name)) return ifcProject.Name;
            }
            return "Project Description";
        }

        private string GetFacilitySiteDescription(IfcSite ifcSite)
        {
            if (ifcSite != null)
            {
                if (!string.IsNullOrEmpty(ifcSite.LongName)) return ifcSite.LongName;
                else if (!string.IsNullOrEmpty(ifcSite.Description)) return ifcSite.Description;
                else if (!string.IsNullOrEmpty(ifcSite.Name)) return ifcSite.Name;
            }
            return "Site Description";
        }

        private string GetFacilitySiteName(IfcSite ifcSite)
        {
            if (ifcSite != null)
            {
                if (!string.IsNullOrEmpty(ifcSite.Name)) return ifcSite.Name;
                else if (!string.IsNullOrEmpty(ifcSite.LongName)) return ifcSite.LongName;
                else if (!string.IsNullOrEmpty(ifcSite.GlobalId)) return ifcSite.GlobalId;
            }
            return "Site Name";
        }

        private string GetFacilityProjectName(IfcProject ifcProject)
        {
            if (ifcProject != null)
            {
                if (!string.IsNullOrEmpty(ifcProject.Name)) return ifcProject.Name;
                else if (!string.IsNullOrEmpty(ifcProject.LongName)) return ifcProject.LongName;
                else if (!string.IsNullOrEmpty(ifcProject.GlobalId)) return ifcProject.GlobalId;
            }
            return "Site Name";
        }

        private string GetLinearUnits()
        {
            IEnumerable<IfcUnitAssignment> unitAssignments = _model.InstancesOfType<IfcUnitAssignment>();
            foreach (IfcUnitAssignment ua in unitAssignments)
            {
                UnitSet us = ua.Units;
                foreach (IfcUnit u in us)
                {
                    if (u is IfcSIUnit)
                    {
                        if (((IfcSIUnit)u).UnitType == IfcUnitEnum.LENGTHUNIT)
                        {
                            if (((IfcSIUnit)u).Name.ToString().ToLower() == "milli") return "millimetres";
                            if (((IfcSIUnit)u).Name.ToString().ToLower() == "metre") return "metres";
                            if (((IfcSIUnit)u).Name.ToString().ToLower() == "inch") return "inches";
                        }
                    }
                }
            }
            return "feet";
        }

        private string GetAreaUnits()
        {
            IEnumerable<IfcUnitAssignment> unitAssignments = _model.InstancesOfType<IfcUnitAssignment>();
            foreach (IfcUnitAssignment ua in unitAssignments)
            {
                UnitSet us = ua.Units;
                foreach (IfcUnit u in us)
                {
                    if (u is IfcSIUnit)
                    {
                        if (((IfcSIUnit)u).UnitType == IfcUnitEnum.AREAUNIT)
                            if (((IfcSIUnit)u).Name.ToString().ToLower() == "square_metre") return "squaremetres";
                    }
                    else if (u is IfcConversionBasedUnit)
                    {
                        if (((IfcConversionBasedUnit)u).UnitType == IfcUnitEnum.AREAUNIT)
                            if (((IfcConversionBasedUnit)u).Name.ToString().ToLower() == "square_metre") return "squaremetres";
                    }
                }
            }

            return "squarefeet";
        }

        private string GetVolumeUnits()
        {
            IEnumerable<IfcUnitAssignment> unitAssignments = _model.InstancesOfType<IfcUnitAssignment>();
            foreach (IfcUnitAssignment ua in unitAssignments)
            {
                UnitSet us = ua.Units;
                foreach (IfcUnit u in us)
                {
                    if (u is IfcSIUnit)
                    {
                        if (((IfcSIUnit)u).UnitType == IfcUnitEnum.VOLUMEUNIT)
                            if (((IfcSIUnit)u).Name.ToString().ToLower() == "cubic_metre") return "cubicmetres";
                    }
                    else if (u is IfcConversionBasedUnit)
                    {
                        if (((IfcConversionBasedUnit)u).UnitType == IfcUnitEnum.VOLUMEUNIT)
                            if (((IfcConversionBasedUnit)u).Name.ToString().ToLower() == "cubic_metre") return "cubicmetres";
                    }
                }
            }
            return "cubicfeet";
        }

        #endregion

        #region Spare

        public COBieSheet<COBieSpareRow> GetCOBieSpareSheet(IModel model, COBieSheet<COBiePickListsRow> pickLists)
        {
            _model = model;

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcConstructionProductResource> ifcConstructionProductResources = model.InstancesOfType<IfcConstructionProductResource>();
            
            IfcTypeObject typeObject = model.InstancesOfType<IfcTypeObject>().FirstOrDefault();

            COBieSheet<COBieSpareRow> spares = new COBieSheet<COBieSpareRow>(Constants.WORKSHEET_SPARE);

            foreach (IfcConstructionProductResource cpr in ifcConstructionProductResources)
            {
                COBieSpareRow spare = new COBieSpareRow(spares);

                //IfcOwnerHistory ifcOwnerHistory = cpr.OwnerHistory;

                spare.Name = (string.IsNullOrEmpty(cpr.Name)) ? "" : cpr.Name.ToString();

                spare.CreatedBy = GetTelecomEmailAddress(cpr.OwnerHistory);
                spare.CreatedOn = GetCreatedOnDateAsFmtString(cpr.OwnerHistory);

                IfcRelAssociatesClassification ifcRAC = cpr.HasAssociations.OfType<IfcRelAssociatesClassification>().FirstOrDefault();
                if (ifcRAC != null)
                {
                    IfcClassificationReference ifcCR = (IfcClassificationReference)ifcRAC.RelatingClassification;
                    spare.Category = ifcCR.Name;
                }
                else
                spare.Category = "";

                spare.TypeName = (typeObject == null) ? "" : typeObject.Name.ToString();
                spare.Suppliers = "";
                spare.ExtSystem = GetIfcApplication().ApplicationFullName;
                
                spare.ExtObject = "";
                foreach (COBiePickListsRow plRow in pickLists.Rows)
                    spare.ExtObject = (plRow == null) ? "" : plRow.ObjType + ",";
                spare.ExtObject = spare.ExtObject.TrimEnd(',');

                spare.ExtIdentifier = cpr.GlobalId;
                spare.Description = (cpr == null) ? "" : cpr.Description.ToString();
                spare.SetNumber = "";
                spare.PartNumber = "";

                spares.Rows.Add(spare);
            }

            return spares;
        }

        #endregion

        #region Zone

        public COBieSheet<COBieZoneRow> GetCOBieZoneSheet(IModel model, COBieSheet<COBiePickListsRow> pickLists)
        {
            _model = model;

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcZone> ifcZones = model.InstancesOfType<IfcZone>();

            COBieSheet<COBieZoneRow> zones = new COBieSheet<COBieZoneRow>(Constants.WORKSHEET_ZONE);

            foreach (IfcZone zn in ifcZones)
            {
                // create zone for each space found
                IEnumerable<IfcSpace> spaces = (zn.IsGroupedBy == null) ? Enumerable.Empty<IfcSpace>() : zn.IsGroupedBy.RelatedObjects.OfType<IfcSpace>();
                foreach (IfcSpace sp in spaces)
                {
                    COBieZoneRow zone = new COBieZoneRow(zones);

                    //IfcOwnerHistory ifcOwnerHistory = zn.OwnerHistory;

                    zone.Name = zn.Name.ToString();

                    zone.CreatedBy = GetTelecomEmailAddress(zn.OwnerHistory);
                    zone.CreatedOn = GetCreatedOnDateAsFmtString(zn.OwnerHistory); 


                    IfcRelAssociatesClassification ifcRAC = zn.HasAssociations.OfType<IfcRelAssociatesClassification>().FirstOrDefault();
                    if (ifcRAC != null)
                    {
                        IfcClassificationReference ifcCR = (IfcClassificationReference)ifcRAC.RelatingClassification;
                        zone.Category = ifcCR.Name;
                    }
                    else
                        zone.Category = "";

                    zone.SpaceNames = sp.Name;

                    zone.ExtSystem = GetIfcApplication().ApplicationFullName;
                    zone.ExtObject = zn.GetType().Name;
                    zone.ExtIdentifier = zn.GlobalId;
                    zone.Description = (string.IsNullOrEmpty(zn.Description)) ? zn.Name.ToString() : zn.Description.ToString(); //if IsNullOrEmpty on Description then output Name

                    zones.Rows.Add(zone);
                }
                
            }
            //Check to see if we have any zones within the spaces
            IEnumerable<IfcSpace> ifcSpaces = model.InstancesOfType<IfcSpace>();//.OrderBy(ifcSpace => ifcSpace.Name, new CompareIfcLabel());
            foreach (IfcSpace sp in ifcSpaces)
            {
                IEnumerable<IfcPropertySingleValue> spProperties = Enumerable.Empty<IfcPropertySingleValue>();
                foreach (IfcPropertySet pset in sp.GetAllPropertySets()) //was just looking at sp.GetPropertySet("PSet_Revit_Other") but 2012-08-07-COBieResponsibilityMatrix-v07.xlsx appears to want all
                {
                    spProperties = spProperties.Concat(pset.HasProperties.Where<IfcPropertySingleValue>(p => p.Name.ToString().Contains("ZoneName")));
                }

                //TODO: COBieResponsibilityMatrix-v07 states that Zone - "Repeated names consolidated", have to implemented as not clear on what is required
                //spProperties = spProperties.OrderBy(p => p.Name.ToString(), new CompareString()); //consolidate test, Concat as looping spaces then sort then dump to COBieZoneRow foreach
                
                foreach (IfcPropertySingleValue spProp in spProperties)
                {
                    COBieZoneRow zone = new COBieZoneRow(zones);
                    zone.Name = spProp.NominalValue.ToString();

                    zone.CreatedBy = GetTelecomEmailAddress(sp.OwnerHistory);
                    zone.CreatedOn = GetCreatedOnDateAsFmtString(sp.OwnerHistory);

                    zone.Category = spProp.Name;
                    zone.SpaceNames = sp.Name;

                    zone.ExtSystem = GetIfcApplication().ApplicationFullName;
                    zone.ExtObject = spProp.GetType().Name;
                    zone.ExtIdentifier = DEFAULT_VAL;
                    zone.Description = (string.IsNullOrEmpty(spProp.NominalValue.ToString())) ? DEFAULT_VAL : spProp.NominalValue.ToString();;

                    zones.Rows.Add(zone);
                }
            }

            return zones;
        }

        #endregion

        #region Type

        public COBieSheet<COBieTypeRow> GetCOBieTypeSheet(IModel model, COBieSheet<COBiePickListsRow> pickLists)
        {
            _model = model;
            //TODO: after IfcRampType and IfcStairType are implemented then add to excludedTypes list
            List<Type> excludedTypes = new List<Type>{  typeof(IfcTypeProduct),
                                                            typeof(IfcElementType),
                                                            typeof(IfcBeamType),
                                                            typeof(IfcColumnType),
                                                            typeof(IfcCurtainWallType),
                                                            typeof(IfcMemberType),
                                                            typeof(IfcPlateType),
                                                            typeof(IfcRailingType),
                                                            typeof(IfcRampFlightType),
                                                            //typeof(Xbim.Ifc.SharedBldgElements.IfcRampType), //IFC2x Edition 4.
                                                            typeof(IfcSlabType),
                                                            typeof(IfcStairFlightType),
                                                            //typeof(IfcStairType), //IFC2x Edition 4.
                                                            typeof(IfcWallType),
                                                            typeof(IfcDuctFittingType ),
                                                            typeof(Xbim.Ifc.ElectricalDomain.IfcJunctionBoxType ),
                                                            typeof(IfcPipeFittingType),
                                                            typeof(Xbim.Ifc.ElectricalDomain.IfcCableCarrierSegmentType),
                                                            typeof(Xbim.Ifc.ElectricalDomain.IfcCableSegmentType),
                                                            typeof(IfcDuctSegmentType),
                                                            typeof(IfcPipeSegmentType),
                                                            typeof(Xbim.Ifc.SharedComponentElements.IfcFastenerType),
                                                            typeof(IfcSpaceType),
                                                             };
            // get all IfcTypeObject objects from IFC file
            IEnumerable<IfcTypeObject> ifcTypeObjects = model.InstancesOfType<IfcTypeObject>().GroupBy(TypeObj => TypeObj.Name).Select(g => g.First()).Where(ty => !excludedTypes.Contains(ty.GetType()));

            //IEnumerable<IfcMaterial> ifcMaterial = _model.InstancesOfType<IfcMaterial>();
            //IEnumerable<IfcMaterialLayerSet> ifcMaterialLayerSet = _model.InstancesOfType<IfcMaterialLayerSet>();

            //var matLaySet = from x in ifcMaterialLayerSet
            //         from y in x.MaterialLayers
            //         select y.Material;

            //ifcMaterial = ifcMaterial.Concat(matLaySet).GroupBy(mat => mat.Name)
            //                       .Select(g => g.First())
            //                       .ToList();

           


            COBieSheet<COBieTypeRow> types = new COBieSheet<COBieTypeRow>(Constants.WORKSHEET_TYPE);
            //Stopwatch StopW = new Stopwatch();
            //StopW.Start();
            //list of required attributes
            List<string> AttNames = new List<string> {  "AssetAccountingType", "Manufacturer", "ModelLabel", "WarrantyGuarantorParts", 
                                                        "WarrantyDurationParts", "WarrantyGuarantorLabor", "WarrantyDurationLabor", 
                                                        "ReplacementCost", "ServiceLifeDuration", "WarrantyDescription", "WarrantyDurationUnit" , "NominalLength", "NominalWidth",
                                                        "NominalHeight", "ModelReference", "Shape", "Colour", "Color", "Finish", "Grade", 
                                                        "Material", "Constituents", "Features", "Size", "AccessibilityPerformance", "CodePerformance", 
                                                        "SustainabilityPerformance"};
            foreach (IfcTypeObject to in ifcTypeObjects)
            {
                COBieTypeRow typ = new COBieTypeRow(types);

                //IfcOwnerHistory ifcOwnerHistory = to.OwnerHistory;

                typ.Name = to.Name;

                typ.CreatedBy = GetTelecomEmailAddress(to.OwnerHistory);
                typ.CreatedOn = GetCreatedOnDateAsFmtString(to.OwnerHistory);


                IfcRelAssociatesClassification ifcRAC = to.HasAssociations.OfType<IfcRelAssociatesClassification>().FirstOrDefault();
                if (ifcRAC != null)
                {
                    IfcClassificationReference ifcCR = (IfcClassificationReference)ifcRAC.RelatingClassification;
                    typ.Category = (string.IsNullOrEmpty(ifcCR.Name)) ? DEFAULT_VAL : ifcCR.Name.ToString();
                }
                else
                    typ.Category = DEFAULT_VAL;

                typ.Description = GetTypeObjDescription(to);

               

                typ.ExtSystem = GetIfcApplication().ApplicationFullName;
                typ.ExtObject = to.GetType().ToString().Substring(to.GetType().ToString().LastIndexOf('.') + 1);
                typ.ExtIdentifier = to.GlobalId;

                //get related object properties to extract from if main way fails
                IEnumerable<IfcPropertySingleValue> relAtts = GetTypeObjRelAttributes(to, AttNames);

                typ.AssetType = GetTypeObjAttribute(to, "Pset_Asset", "AssetAccountingType", relAtts);
                typ.Manufacturer = GetTypeObjAttribute(to, "Pset_ManufacturersTypeInformation", "Manufacturer", relAtts);
                typ.ModelNumber = GetTypeObjAttribute(to, "Pset_ManufacturersTypeInformation", "ModelLabel", relAtts);
                typ.WarrantyGuarantorParts = GetTypeObjAttribute(to, "Pset_Warranty", "WarrantyGuarantorParts", relAtts);
                typ.WarrantyDurationParts = GetTypeObjAttribute(to, "Pset_Warranty", "WarrantyDurationParts", relAtts);
                typ.WarrantyGuarantorLabour = GetTypeObjAttribute(to, "Pset_Warranty", "WarrantyGuarantorLabor", relAtts);
                typ.WarrantyDurationLabour = GetTypeObjAttribute(to, "Pset_Warranty", "WarrantyDurationLabor", relAtts);
                typ.WarrantyDurationUnit = GetWarrantyDurationUnit(model); 
                typ.ReplacementCost = GetTypeObjAttribute(to, "Pset_EconomicImpactValues", "ReplacementCost", relAtts);
                typ.ExpectedLife = GetTypeObjAttribute(to, "Pset_ServiceLife", "ServiceLifeDuration", relAtts);
                typ.DurationUnit = GetDurationUnit(model);
                typ.WarrantyDescription = GetTypeObjAttribute(to, "Pset_Warranty", "WarrantyDescription", relAtts); 

                typ.NominalLength = GetTypeObjAttribute(to, "Pset_Specification", "NominalLength", relAtts);
                typ.NominalLength = ConvertNumberOrDefault(typ.NominalLength); //ensure we comply with a number value
                typ.NominalWidth = GetTypeObjAttribute(to, "Pset_Specification", "NominalWidth", relAtts);
                typ.NominalWidth = ConvertNumberOrDefault(typ.NominalWidth); //ensure we comply with a number value
                typ.NominalHeight = GetTypeObjAttribute(to, "Pset_Specification", "NominalHeight", relAtts);
                typ.NominalHeight = ConvertNumberOrDefault(typ.NominalHeight); //ensure we comply with a number value

                typ.ModelReference = GetTypeObjAttribute(to, "Pset_Specification", "ModelReference", relAtts);
                typ.Shape = GetTypeObjAttribute(to, "Pset_Specification", "Shape", relAtts);
                typ.Size = GetTypeObjAttribute(to, "Pset_Specification", "Size", relAtts);
                typ.Colour = GetTypeObjAttribute(to, "Pset_Specification", "Colour", relAtts);
                if (typ.Colour == DEFAULT_VAL) typ.Colour = GetTypeObjAttribute(to, "Pset_Specification", "Color", relAtts); //try US 'color'
                typ.Finish = GetTypeObjAttribute(to, "Pset_Specification", "Finish", relAtts);
                typ.Grade = GetTypeObjAttribute(to, "Pset_Specification", "Grade", relAtts);
                typ.Material = GetTypeObjAttribute(to, "Pset_Specification", "Material", relAtts);
                typ.Constituents = GetTypeObjAttribute(to, "Pset_Specification", "Constituents", relAtts);
                typ.Features = GetTypeObjAttribute(to, "Pset_Specification", "Features", relAtts);
                typ.AccessibilityPerformance = GetTypeObjAttribute(to, "Pset_Specification", "AccessibilityPerformance", relAtts);
                typ.CodePerformance = GetTypeObjAttribute(to, "Pset_Specification", "CodePerformance", relAtts);
                typ.SustainabilityPerformance = GetTypeObjAttribute(to, "Pset_Specification", "SustainabilityPerformance", relAtts);

                types.Rows.Add(typ);
            }
            //StopW.Stop();
            //Debug.WriteLine(StopW.Elapsed.ToString());
            return types;
        }

        private string GetDurationUnit(IModel model)
        {
            IEnumerable<IfcConversionBasedUnit> ifcConversionBasedUnit = model.InstancesOfType<IfcConversionBasedUnit>().Where(u => u.UnitType == IfcUnitEnum.TIMEUNIT);
            IfcConversionBasedUnit TimeBaeUnit = ifcConversionBasedUnit.FirstOrDefault();
            if ((TimeBaeUnit != null) && (TimeBaeUnit.Name != null))
            {
                return TimeBaeUnit.Name.ToString();
            }
            return "0";
        }

        /// <summary>
        /// Get the Time unit used in the model
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        private string GetWarrantyDurationUnit(IModel model)
        {
            return GetDurationUnit(model);
            //IEnumerable<IfcConversionBasedUnit> ifcConversionBasedUnit = model.InstancesOfType<IfcConversionBasedUnit>().Where(u => u.UnitType == IfcUnitEnum.TIMEUNIT);
            //IfcConversionBasedUnit TimeBaeUnit = ifcConversionBasedUnit.FirstOrDefault();
            //if ((TimeBaeUnit != null) && (TimeBaeUnit.Name != null))
            //{
            //    return TimeBaeUnit.Name.ToString();
            //}
            //return "0";
        }


        /// <summary>
        /// Get the Attribute for a IfcTypeObject
        /// </summary>
        /// <param name="TypeObj">IfcTypeObject </param>
        /// <param name="propSetName">Property Set Name to retrieve IfcPropertySet Object</param>
        /// <param name="propName">Property Name held in IfcPropertySingleValue object</param>
        /// <param name="relAtts">List of IfcPropertySingleValue filtered to attribute names we require</param>
        /// <returns>NominalValue of IfcPropertySingleValue as a string</returns>
        private string GetTypeObjAttribute(IfcTypeObject TypeObj, string propSetName, string propName, IEnumerable<IfcPropertySingleValue> relAtts)
        {
            //try to get the property from the IfcTypeObject
            IfcPropertySingleValue pSngValue = TypeObj.GetPropertySingleValue(propSetName, propName);
            //if null then try and get from a first related object i.e. window type is associated with a window so look at window, should we do this or just return some default value here???
            if (pSngValue == null) pSngValue = relAtts.Where(p => p.Name == propName).FirstOrDefault();
            //if we have a value return the string for input to row field
            if ((pSngValue != null) && (pSngValue.NominalValue != null)) return pSngValue.NominalValue.ToString();
            else return DEFAULT_VAL; //nothing found return default

            //if null then try and get from all related object i.e. 
            //if (pSngValue == null)
            //{
            //    foreach (IfcPropertySingleValue pSV in relAtts.Where(p => p.Name == propName))
            //    {
            //        if (pSV != null)
            //        {
            //            pSngValue = pSV;
            //            break; //we have a value so break
            //        }
            //    }
            //}
        }

        /// <summary>
        /// Converts string to formatted string and if it fails then passes 0 back, mainly to check that we always have a number returned
        /// </summary>
        /// <param name="num">string to convert</param>
        /// <returns>string converted to a formatted string using "F2" as formatter</returns>
        private string ConvertNumberOrDefault(string num)
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
        /// Get the IfcPropertySingleValue list for the passed in list of attribute names
        /// 
        /// </summary>
        /// <param name="TypeObj">IfcTypeObject </param>
        /// <param name="AttNames">list of attribute names</param>
        /// <returns>IEnumerable<IfcPropertySingleValue> list of IfcPropertySingleValue which are contained in AttNames</returns>
        private IEnumerable<IfcPropertySingleValue> GetTypeObjRelAttributes(IfcTypeObject TypeObj, List<string> AttNames)
        {
            Dictionary<string, string> keyList = new Dictionary<string, string>();
            //string key = "";
            //string value = DEFAULT_VAL;
            IEnumerable<IfcPropertySingleValue> objProperties = Enumerable.Empty<IfcPropertySingleValue>();
            var objTypeOf = TypeObj.ObjectTypeOf.FirstOrDefault(); //can hold zero or 1 ObjectTypeOf so test return
            if (objTypeOf != null)
            {
                foreach (IfcPropertySet pset in objTypeOf.RelatedObjects.First().GetAllPropertySets()) //has to have 1 ore more so get first and see what we get
                {
                    objProperties = objProperties.Concat(pset.HasProperties.Where<IfcPropertySingleValue>(p => AttNames.Contains(p.Name.ToString())));
                }
            }


            return objProperties;
        }

        private string GetTypeObjDescription(IfcTypeObject ds)
        {
            if (ds != null)
            {
                if (!string.IsNullOrEmpty(ds.Description)) return ds.Description;
                else if (!string.IsNullOrEmpty(ds.Name)) return ds.Name;
                else 
                {
                    //if supports PredefinedType and no description or name then use the predefined type or ElementType if they exist
                    IEnumerable<PropertyInfo> pInfo = ds.GetType().GetProperties(); //get properties

                    if (pInfo.Where(p => p.Name == "PredefinedType").Count() == 1)
                    {
                        string temp = pInfo.First().GetValue(ds, null).ToString(); //get predefindtype as description

                        if (!string.IsNullOrEmpty(temp))
                        {
                            if (temp == "USERDEFINED")
                            {
                                //if used defined then the type description should be in ElementType, so see if property exists
                                if (pInfo.Where(p => p.Name == "ElementType").Count() == 1)
                                {
                                    temp = pInfo.First().GetValue(ds, null).ToString(); //get ElementType
                                    if (!string.IsNullOrEmpty(temp)) return temp; 
                                }
                            }
                            if (temp == "NOTDEFINED") //if not defined then give up and return default
                            {
                                return DEFAULT_VAL;
                            }

                            return temp;
                        }
	                   
                    }
                }
            }
            return DEFAULT_VAL;
        }

        //private string GetTypeWarrantyDescription(object ds)
        //{
        //    string desc = "";
        //    int index = ds.GetType().ToString().LastIndexOf('.');
        //    desc = ds.GetType().ToString().Substring(index + 4);
            
        //    if (ds is IfcDistributionElementType)
        //        desc += " " + ((IfcDistributionElementType)ds).Name;
        //    else if (ds is IfcElementType)
        //        desc += " " + ((IfcElementType)ds).Name;

        //    return desc;
        //}

        #endregion

        #region Component

        public COBieSheet<COBieComponentRow> GetCOBieComponentSheet(IModel model)
        {
            _model = model;

            List<Type> excludedTypes = new List<Type>{  typeof(IfcWall),
                                                            typeof(IfcWallStandardCase),
                                                            typeof(IfcWall),
                                                            typeof(IfcWallStandardCase),
                                                            typeof(IfcSlab),
                                                            typeof(IfcBeam),
                                                            typeof(IfcSpace),
                                                            typeof(IfcBuildingStorey),
                                                            typeof(IfcBuilding),
                                                            typeof(IfcSite),
                                                            typeof(IfcProject),
                                                            typeof(IfcColumn),
                                                            typeof(IfcMember),
                                                            typeof(IfcPlate),
                                                            typeof(IfcRailing),
                                                            typeof(IfcStairFlight),
                                                            typeof(IfcCurtainWall),
                                                            typeof(IfcRampFlight),
                                                            typeof(IfcVirtualElement),
                                                            typeof(IfcFeatureElement),
                                                            typeof(Xbim.Ifc.SharedComponentElements.IfcFastener),
                                                            typeof(Xbim.Ifc.SharedComponentElements.IfcMechanicalFastener),
                                                            typeof(IfcElementAssembly),
                                                            typeof(Xbim.Ifc.StructuralElementsDomain.IfcBuildingElementPart),
                                                            typeof(Xbim.Ifc.StructuralElementsDomain.IfcReinforcingBar),
                                                            typeof(Xbim.Ifc.StructuralElementsDomain.IfcReinforcingMesh),
                                                            typeof(Xbim.Ifc.StructuralElementsDomain.IfcTendon),
                                                            typeof(Xbim.Ifc.StructuralElementsDomain.IfcTendonAnchor),
                                                            typeof(Xbim.Ifc.StructuralElementsDomain.IfcFooting),
                                                            typeof(Xbim.Ifc.StructuralElementsDomain.IfcPile),
                                                            typeof(IfcRamp),
                                                            typeof(IfcRoof),
                                                            typeof(IfcStair),
                                                            typeof(IfcFlowFitting),
                                                            typeof(IfcFlowSegment),
                                                            typeof(IfcDistributionPort) };

            COBieSheet<COBieComponentRow> components = new COBieSheet<COBieComponentRow>(Constants.WORKSHEET_COMPONENT);

            IEnumerable<IfcRelAggregates> relAggregates = _model.InstancesOfType<IfcRelAggregates>();
            IEnumerable<IfcRelContainedInSpatialStructure> relSpatial = _model.InstancesOfType<IfcRelContainedInSpatialStructure>();

            var q1 = from x in relAggregates
                     from y in x.RelatedObjects
                     where !excludedTypes.Contains(y.GetType())
                     select y;

            var q2 = from x in relSpatial
                     from y in x.RelatedElements
                     where !excludedTypes.Contains(y.GetType())
                     select y;

            var res = q1.Concat(q2).GroupBy(el => el.Name)
                                   .Select(g => g.First())
                                   .ToList();

            foreach (var obj in res)
            {
                COBieComponentRow component = new COBieComponentRow(components);

                IfcElement el = obj as IfcElement;
                //IfcOwnerHistory ifcOwnerHistory = el.OwnerHistory;
                component.Name = el.Name;
                component.CreatedBy = GetTelecomEmailAddress(el.OwnerHistory);
                component.CreatedOn = GetCreatedOnDateAsFmtString(el.OwnerHistory);
                component.TypeName = el.ObjectType.ToString();
                component.Space = component.GetComponentRelatedSpace(el);
                component.Description = component.GetComponentDescription(el);
                component.ExtSystem = GetIfcApplication().ApplicationFullName;
                component.ExtObject = el.GetType().Name;
                component.ExtIdentifier = el.GlobalId;
                component.SerialNumber = "";
                component.InstallationDate = "";
                component.WarrantyStartDate = "";
                //component.TagNumber = el.Tag.ToString();
                component.BarCode = "";
                component.AssetIdentifier = "";

                components.Rows.Add(component);
            }

            return components;
        }

        #endregion

        #region System

        public COBieSheet<COBieSystemRow> GetCOBieSystemSheet(IModel model)
        {
            _model = model;

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcSystem> ifcSystems = model.InstancesOfType<IfcSystem>();

            COBieSheet<COBieSystemRow> systems = new COBieSheet<COBieSystemRow>(Constants.WORKSHEET_SYSTEM);

            foreach (IfcSystem s in ifcSystems)
            {
                IEnumerable<IfcProduct> ifcProducts = (s.IsGroupedBy == null) ? Enumerable.Empty<IfcProduct>() : s.IsGroupedBy.RelatedObjects.OfType<IfcProduct>();

                foreach (IfcProduct product in ifcProducts)
                {
                    COBieSystemRow sys = new COBieSystemRow(systems);

                    //IfcOwnerHistory ifcOwnerHistory = s.OwnerHistory;

                    sys.Name = s.Name;

                    sys.CreatedBy = GetTelecomEmailAddress(s.OwnerHistory);
                    sys.CreatedOn = GetCreatedOnDateAsFmtString(s.OwnerHistory);

                    IfcRelAssociatesClassification ifcRAC = s.HasAssociations.OfType<IfcRelAssociatesClassification>().FirstOrDefault();
                    if (ifcRAC != null)
                    {
                        IfcClassificationReference ifcCR = (IfcClassificationReference)ifcRAC.RelatingClassification;
                        sys.Category = ifcCR.Name;
                    }
                    else
                        sys.Category = "";

                    sys.ComponentName = product.Name;
                    sys.ExtSystem = GetIfcApplication().ApplicationFullName;
                    sys.ExtObject = "IfcSystem";
                    sys.ExtIdentifier = product.GlobalId;
                    sys.Description = GetSystemDescription(s);

                    systems.Rows.Add(sys);
                }
                
            }

            return systems;
        }

        private string GetSystemDescription(IfcSystem s)
        {
            if (s != null)
            {
                if (!string.IsNullOrEmpty(s.Description)) return s.Description;
                else if (!string.IsNullOrEmpty(s.Name)) return s.Name;
            }
            return DEFAULT_VAL;
        }

        #endregion

        #region Assembly

        public COBieSheet<COBieAssemblyRow> GetCOBieAssemblySheet(IModel model)
        {
            _model = model;

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcRelAggregates> ifcRelAggregates = model.InstancesOfType<IfcRelAggregates>();

            COBieSheet<COBieAssemblyRow> assemblies = new COBieSheet<COBieAssemblyRow>(Constants.WORKSHEET_ASSEMBLY);
                        
            IfcProduct ifcProduct = model.InstancesOfType<IfcProduct>().FirstOrDefault();
            IfcClassification ifcClassification = model.InstancesOfType<IfcClassification>().FirstOrDefault();
            string applicationFullName = GetIfcApplication().ApplicationFullName;

            foreach (IfcRelAggregates ra in ifcRelAggregates)
            {
                COBieAssemblyRow assembly = new COBieAssemblyRow(assemblies);

                //IfcOwnerHistory ifcOwnerHistory = ra.OwnerHistory;

                assembly.Name = (ra.Name == null || ra.Name.ToString() == "") ? "AssemblyName" : ra.Name.ToString();

                assembly.CreatedBy = GetTelecomEmailAddress(ra.OwnerHistory);
                assembly.CreatedOn = GetCreatedOnDateAsFmtString(ra.OwnerHistory);

                assembly.SheetName = "SheetName:";
                assembly.ParentName = ifcProduct.Name;
                assembly.ChildNames = ifcProduct.Name;
                assembly.AssemblyType = (ifcClassification == null) ? "" : ifcClassification.Name.ToString();
                assembly.ExtSystem = applicationFullName;
                assembly.ExtObject = "IfcRelAggregates";
                assembly.ExtIdentifier = string.IsNullOrEmpty(ra.GlobalId) ? DEFAULT_VAL : ra.GlobalId.ToString();
                assembly.Description = GetAssemblyDescription(ra);

                assemblies.Rows.Add(assembly);

                COBieCell testCell = assembly[7];
            }

            return assemblies;
        }

        //public List<COBieAssemblyRow> GetCOBieAssemblySheet(IModel model)
        //{
        //    _model = model;

        //    // get all IfcBuildingStory objects from IFC file
        //    IEnumerable<IfcRelAggregates> ifcRelAggregates = model.InstancesOfType<IfcRelAggregates>();
        //    IfcTelecomAddress ifcTelecomAddres = model.InstancesOfType<IfcTelecomAddress>().FirstOrDefault();
        //    List<COBieAssemblyRow> assemblies = new List<COBieAssemblyRow>();

        //    IfcOwnerHistory ifcOwnerHistory = model.InstancesOfType<IfcOwnerHistory>().FirstOrDefault();
        //    IfcProduct ifcProduct = model.InstancesOfType<IfcProduct>().FirstOrDefault();
        //    IfcClassification ifcClassification = model.InstancesOfType<IfcClassification>().FirstOrDefault();
        //    string applicationFullName = GetIfcApplication().ApplicationFullName;

        //    foreach (IfcRelAggregates ra in ifcRelAggregates)
        //    {
        //        COBieAssemblyRow assembly = new COBieAssemblyRow();
        //        assembly.Name = (ra.Name == null || ra.Name.ToString() == "") ? "AssemblyName" : ra.Name.ToString();
        //        assembly.CreatedBy = (ifcTelecomAddres == null) ? "" : ifcTelecomAddres.ElectronicMailAddresses[0].ToString();
        //        assembly.CreatedOn = ifcOwnerHistory.CreationDate.ToString();
        //        assembly.SheetName = "SheetName:";
        //        assembly.ParentName = ifcProduct.Name;
        //        assembly.ChildNames = ifcProduct.Name;
        //        assembly.AssemblyType = (ifcClassification == null) ? "" : ifcClassification.Name.ToString();
        //        assembly.ExtSystem = applicationFullName;
        //        assembly.ExtObject = "IfcRelAggregates";
        //        assembly.ExtIdentifier = string.IsNullOrEmpty(ra.GlobalId) ? "n/a" : ra.GlobalId.ToString();
        //        assembly.Description = GetAssemblyDescription(ra);

        //        assemblies.Add(assembly);

        //        COBieCell testCell = assembly[7];
        //    }

        //    return assemblies;
        //}

        private string GetAssemblyDescription(IfcRelAggregates ra)
        {
            if (ra != null)
            {
                if (!string.IsNullOrEmpty(ra.Description)) return ra.Description;
                else if (!string.IsNullOrEmpty(ra.Name)) return ra.Name;
            }
            return DEFAULT_VAL;
        }

        #endregion

        #region Connection

        public COBieSheet<COBieConnectionRow> GetCOBieConnectionSheet(IModel model)
        {
            _model = model;

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcRelConnectsElements> ifcConnections = model.InstancesOfType<IfcRelConnectsElements>();
            IfcTelecomAddress ifcTelecomAddres = model.InstancesOfType<IfcTelecomAddress>().FirstOrDefault();
            IfcOwnerHistory ifcOwnerHistory = model.InstancesOfType<IfcOwnerHistory>().FirstOrDefault();
            COBieSheet<COBieConnectionRow> connections = new COBieSheet<COBieConnectionRow>(Constants.WORKSHEET_CONNECTION);

            IfcRelConnectsPorts relCP = model.InstancesOfType<IfcRelConnectsPorts>().FirstOrDefault();

            IfcProduct product = model.InstancesOfType<IfcProduct>().FirstOrDefault();

            int ids = 0;
            foreach (IfcRelConnectsElements c in ifcConnections)
            {
                COBieConnectionRow conn = new COBieConnectionRow(connections);
                conn.Name = (string.IsNullOrEmpty(c.Name)) ? ids.ToString() : c.Name.ToString();
                conn.CreatedBy = GetTelecomEmailAddress(c.OwnerHistory);
                conn.CreatedOn = GetCreatedOnDateAsFmtString(c.OwnerHistory);
                conn.ConnectionType = c.Description;
                conn.SheetName = "";
                conn.RowName1 = (product == null) ? "" : product.Name.ToString();
                conn.RowName2 = (product == null) ? "" : product.Name.ToString();
                conn.RealizingElement = (relCP == null) ? "" : relCP.RealizingElement.Description.ToString();
                conn.PortName1 = (relCP == null) ? "" : relCP.RelatingPort.Description.ToString();
                conn.PortName2 = (relCP == null) ? "" : relCP.RelatedPort.Description.ToString();
                conn.ExtSystem = GetIfcApplication().ApplicationFullName;
                conn.ExtObject = "";
                conn.ExtIdentifier = c.GlobalId;
                conn.Description = (string.IsNullOrEmpty(c.Description)) ? DEFAULT_VAL : c.Description.ToString();

                connections.Rows.Add(conn);

                ids++;
            }

            return connections;
        }

        #endregion

        #region Coordinate

        public COBieSheet<COBieCoordinateRow> GetCOBieCoordinateSheet(IModel model)
        {
            _model = model;

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcRelAggregates> ifcRelAggregates = model.InstancesOfType<IfcRelAggregates>();
            IfcBuildingStorey ifcBuildingStorey = model.InstancesOfType<IfcBuildingStorey>().FirstOrDefault();
            IfcTelecomAddress ifcTelecomAddres = model.InstancesOfType<IfcTelecomAddress>().FirstOrDefault();
            IfcCartesianPoint ifcCartesianPoint = model.InstancesOfType<IfcCartesianPoint>().FirstOrDefault();
            COBieSheet<COBieCoordinateRow> coordinates = new COBieSheet<COBieCoordinateRow>(Constants.WORKSHEET_COORDINATE);

            IfcOwnerHistory ifcOwnerHistory = model.InstancesOfType<IfcOwnerHistory>().FirstOrDefault();
            IfcProduct ifcProduct = model.InstancesOfType<IfcProduct>().FirstOrDefault();
            IfcClassification ifcClassification = model.InstancesOfType<IfcClassification>().FirstOrDefault();
            string applicationFullName = GetIfcApplication().ApplicationFullName;

            foreach (IfcRelAggregates ra in ifcRelAggregates)
            {
                COBieCoordinateRow coordinate = new COBieCoordinateRow(coordinates);
                coordinate.Name = (ifcBuildingStorey == null || ifcBuildingStorey.Name.ToString() == "") ? "CoordinateName" : ifcBuildingStorey.Name.ToString();

                coordinate.CreatedBy = GetTelecomEmailAddress(ra.OwnerHistory);
                coordinate.CreatedOn = GetCreatedOnDateAsFmtString(ra.OwnerHistory);

                //IfcRelAssociatesClassification ifcRAC = ra.HasAssociations.OfType<IfcRelAssociatesClassification>().FirstOrDefault();
                //IfcClassificationReference ifcCR = (IfcClassificationReference)ifcRAC.RelatingClassification;
                coordinate.Category = "";
                
                coordinate.SheetName = "PickList.SheetType";
                coordinate.RowName = DEFAULT_VAL;
                coordinate.CoordinateXAxis = ifcCartesianPoint[0].ToString();
                coordinate.CoordinateYAxis = ifcCartesianPoint[1].ToString();
                coordinate.CoordinateZAxis = ifcCartesianPoint[2].ToString();
                coordinate.ExtSystem = applicationFullName;
                coordinate.ExtObject = GetExtObject(model);
                coordinate.ExtIdentifier = "PickList.objType";
                coordinate.ClockwiseRotation = DEFAULT_VAL;
                coordinate.ElevationalRotation = DEFAULT_VAL;
                coordinate.YawRotation = DEFAULT_VAL;

                coordinates.Rows.Add(coordinate);
            }

            return coordinates;
        }

        private string GetExtObject(IModel model)
        {
            IfcBuildingStorey ifcBuildingStorey = model.InstancesOfType<IfcBuildingStorey>().FirstOrDefault();
            IfcSpace ifcSpace = model.InstancesOfType<IfcSpace>().FirstOrDefault();
            IfcProduct ifcProduct = model.InstancesOfType<IfcProduct>().FirstOrDefault();

            if (string.IsNullOrEmpty(ifcBuildingStorey.GlobalId)) return ifcBuildingStorey.GlobalId.ToString();
            else if (string.IsNullOrEmpty(ifcSpace.GlobalId)) return ifcSpace.GlobalId.ToString();
            else if (string.IsNullOrEmpty(ifcProduct.GlobalId)) return ifcProduct.GlobalId.ToString();

            return DEFAULT_VAL;
        }

        #endregion

        #region Attribute

        public COBieSheet<COBieAttributeRow> GetCOBieAttributeSheet(IModel model)
        {
            _model = model;

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcObject> ifcObject = model.InstancesOfType<IfcObject>();
            IfcBuildingStorey ifcBuildingStorey = model.InstancesOfType<IfcBuildingStorey>().FirstOrDefault();
            IfcTelecomAddress ifcTelecomAddres = model.InstancesOfType<IfcTelecomAddress>().FirstOrDefault();
            IfcCartesianPoint ifcCartesianPoint = model.InstancesOfType<IfcCartesianPoint>().FirstOrDefault();
            COBieSheet<COBieAttributeRow> attributes = new COBieSheet<COBieAttributeRow>(Constants.WORKSHEET_ATTRIBUTE);

            IfcOwnerHistory ifcOwnerHistory = model.InstancesOfType<IfcOwnerHistory>().FirstOrDefault();
            IfcProduct ifcProduct = model.InstancesOfType<IfcProduct>().FirstOrDefault();
            IfcClassification ifcClassification = model.InstancesOfType<IfcClassification>().FirstOrDefault();
            string applicationFullName = GetIfcApplication().ApplicationFullName;

            foreach (IfcObject obj in ifcObject)
            {
                COBieAttributeRow attribute = new COBieAttributeRow(attributes);
                attribute.Name = (ifcBuildingStorey == null || ifcBuildingStorey.Name.ToString() == "") ? "AttributeName" : ifcBuildingStorey.Name.ToString();

                attribute.CreatedBy = GetTelecomEmailAddress(obj.OwnerHistory);
                attribute.CreatedOn = GetCreatedOnDateAsFmtString(obj.OwnerHistory);

                IfcRelAssociatesClassification ifcRAC = obj.HasAssociations.OfType<IfcRelAssociatesClassification>().FirstOrDefault();
                if (ifcRAC != null)
                {
                    IfcClassificationReference ifcCR = (IfcClassificationReference)ifcRAC.RelatingClassification;
                    attribute.Category = ifcCR.Name;
                }
                attribute.Category = "";

                attribute.SheetName = "PickList.SheetType";
                attribute.RowName = DEFAULT_VAL;
                attribute.Value = "";
                attribute.Unit = "";
                attribute.ExtSystem = applicationFullName;
                attribute.ExtObject = "PickList.objType";
                attribute.ExtIdentifier = obj.GlobalId;
                attribute.Description = "";
                attribute.AllowedValues = "";
                
                attributes.Rows.Add(attribute);
            }

            return attributes;
        }

        #endregion

        private IfcApplication GetIfcApplication()
        {
            IfcApplication app = _model.InstancesOfType<IfcApplication>().FirstOrDefault();
            return app;
        }
        /// <summary>
        /// Extract the email address lists for the owner of the IfcOwnerHistory passed
        /// </summary>
        /// <param name="ifcOwnerHistory">Entity to extract the email addresses for</param>
        /// <returns>string of comma delimited addresses</returns>
        private string GetTelecomEmailAddress(IfcOwnerHistory ifcOwnerHistory)
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
       
    }
}
