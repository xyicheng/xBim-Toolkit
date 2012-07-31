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

namespace Xbim.COBie.COBieExtensions
{
    public class COBieQueries
    {

        IModel _model;
        const string DEFAULT_VAL = "n/a";

        #region Contact

        public COBieSheet<COBieContactRow> GetCOBieContactSheet(IModel model, COBieSheet<COBiePickListsRow> pickLists)
        {
            _model = model;

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcPersonAndOrganization> personsOrganizations = model.InstancesOfType<IfcPersonAndOrganization>();
            
            IEnumerable<IfcTelecomAddress> ifcTelecomAddresses = model.InstancesOfType<IfcTelecomAddress>();
            if (ifcTelecomAddresses == null) ifcTelecomAddresses = Enumerable.Empty<IfcTelecomAddress>();

            IfcOwnerHistory ifcOwnerHistory = model.InstancesOfType<IfcOwnerHistory>().FirstOrDefault();
            //IfcPostalAddress address = model.InstancesOfType<IfcPostalAddress>().FirstOrDefault();
            COBieSheet<COBieContactRow> contacts = new COBieSheet<COBieContactRow>();

            foreach (IfcPersonAndOrganization po in personsOrganizations)
            {
                COBieContactRow contact = new COBieContactRow();
                IfcPerson person = po.ThePerson;
                IfcOrganization organization = po.TheOrganization;

                IEnumerable<IfcTelecomAddress> telAddresses = Enumerable.Empty<IfcTelecomAddress>();
                if (organization.Addresses != null)
                    telAddresses = organization.Addresses.TelecomAddresses;

                contact.Email = "";
                contact.CreatedBy = "";
                foreach (IfcTelecomAddress ta in telAddresses)
                {
                    foreach (IfcLabel email in ta.ElectronicMailAddresses)
                    {
                        contact.Email = (email == null) ? "" : email.ToString() + ",";
                        contact.CreatedBy = (email == null) ? "" : email.ToString() + ",";
                    }                   
                }
                contact.Email = contact.Email.TrimEnd(',');
                contact.CreatedBy = contact.CreatedBy.TrimEnd(',');
                                
                contact.CreatedOn = (ifcOwnerHistory.CreationDate == null) ? "" : ifcOwnerHistory.CreationDate.ToString();

                contact.Category = "";
                foreach (COBiePickListsRow plRow in pickLists.Rows)
                    contact.Category = plRow.CategoryRole + ",";
                contact.Category = contact.Category.TrimEnd(',');

                contact.Company = (string.IsNullOrEmpty(organization.Name)) ? DEFAULT_VAL : organization.Name.ToString();

                contact.Phone = "";
                foreach (IfcTelecomAddress ta in telAddresses)
                {
                    foreach (IfcLabel phone in ta.TelephoneNumbers)
                        contact.Phone = (phone == null) ? "" : phone.ToString() + ",";
                }
                contact.Phone = contact.Phone.TrimEnd(',');

                contact.ExtSystem = GetIfcApplication().ApplicationFullName;
                contact.ExtObject = po.GetType().Name;
                contact.ExtIdentifier = person.Id;
                contact.Department = (organization.Addresses == null) ? DEFAULT_VAL : organization.Addresses.PostalAddresses.FirstOrDefault().InternalLocation.ToString();
                contact.OrganizationCode = (string.IsNullOrEmpty(organization.Name)) ? DEFAULT_VAL : organization.Name.ToString();
                contact.GivenName = (string.IsNullOrEmpty(person.GivenName)) ? DEFAULT_VAL : person.GivenName.ToString();
                contact.FamilyName = (string.IsNullOrEmpty(person.FamilyName)) ? DEFAULT_VAL : person.FamilyName.ToString();
                contact.Street = (person.Addresses == null) ? "" : person.Addresses.PostalAddresses.FirstOrDefault().AddressLines.ToString();
                contact.PostalBox = (person.Addresses == null) ? "" : person.Addresses.PostalAddresses.FirstOrDefault().PostalBox.ToString();
                contact.Town = (person.Addresses == null) ? "" : person.Addresses.PostalAddresses.FirstOrDefault().Town.ToString();
                contact.StateRegion = (person.Addresses == null) ? "" : person.Addresses.PostalAddresses.FirstOrDefault().Region.ToString();
                contact.PostalCode = (person.Addresses == null) ? "" : person.Addresses.PostalAddresses.FirstOrDefault().PostalCode.ToString();
                contact.Country = (person.Addresses == null) ? "" : person.Addresses.PostalAddresses.FirstOrDefault().Country.ToString();

                contacts.Rows.Add(contact);
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
            COBieSheet<COBieDocumentRow> documents = new COBieSheet<COBieDocumentRow>();

            foreach (IfcDocumentInformation di in docInfos)
            {
                COBieDocumentRow doc = new COBieDocumentRow();
                doc.Name = (di == null) ? "" : di.Name.ToString();

                doc.CreatedBy = "";
                foreach (IfcTelecomAddress address in ifcTelecomAddresses)
                    doc.CreatedBy = (address == null) ? "" : address.ToString() + ",";
                doc.CreatedBy = doc.CreatedBy.TrimEnd(',');
                                
                doc.CreatedOn = (ifcOwnerHistory.CreationDate == null) ? "" : ifcOwnerHistory.CreationDate.ToString();

                doc.Category = "";
                foreach (COBiePickListsRow plRow in pickLists.Rows)
                    doc.Category = (plRow == null) ? "" : plRow.ZoneType + ",";
                doc.Category = doc.Category.TrimEnd(',');

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

            IEnumerable<IfcTelecomAddress> ifcTelecomAddresses = model.InstancesOfType<IfcTelecomAddress>();
            if (ifcTelecomAddresses == null) ifcTelecomAddresses = Enumerable.Empty<IfcTelecomAddress>();

            IfcOwnerHistory ifcOwnerHistory = model.InstancesOfType<IfcOwnerHistory>().FirstOrDefault();
            COBieSheet<COBieImpactRow> impacts = new COBieSheet<COBieImpactRow>();

            foreach (IfcPropertySet ppt in ifcProperties)
            {
                COBieImpactRow impact = new COBieImpactRow();
                impact.Name = ppt.Name;

                impact.CreatedBy = "";
                foreach (IfcTelecomAddress address in ifcTelecomAddresses)
                    impact.CreatedBy = (address == null) ? "" : address.ToString() + ",";
                impact.CreatedBy = impact.CreatedBy.TrimEnd(',');

                impact.CreatedOn = (ifcOwnerHistory.CreationDate == null) ? "" : ifcOwnerHistory.CreationDate.ToString();

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
            COBieSheet<COBieIssueRow> issues = new COBieSheet<COBieIssueRow>();

            foreach (IfcApproval app in ifcApprovals)
            {
                COBieIssueRow issue = new COBieIssueRow();
                issue.Name = (approval == null) ? "" : approval.Name.ToString();

                issue.CreatedBy = "";
                foreach (IfcTelecomAddress address in ifcTelecomAddresses)
                    issue.CreatedBy = (address == null) ? "" : address.ToString() + ",";
                issue.CreatedBy = issue.CreatedBy.TrimEnd(',');

                issue.CreatedOn = (ifcOwnerHistory.CreationDate == null) ? "" : ifcOwnerHistory.CreationDate.ToString();

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

            IEnumerable<IfcTelecomAddress> ifcTelecomAddresses = model.InstancesOfType<IfcTelecomAddress>();
            if (ifcTelecomAddresses == null) ifcTelecomAddresses = Enumerable.Empty<IfcTelecomAddress>();

            IfcOwnerHistory ifcOwnerHistory = model.InstancesOfType<IfcOwnerHistory>().FirstOrDefault();
            
            IfcTypeObject typObj = model.InstancesOfType<IfcTypeObject>().FirstOrDefault();
            IfcConstructionEquipmentResource cer = model.InstancesOfType<IfcConstructionEquipmentResource>().FirstOrDefault();
            
            COBieSheet<COBieJobRow> jobs = new COBieSheet<COBieJobRow>();

            foreach (IfcTask task in ifcTasks)
            {
                COBieJobRow job = new COBieJobRow();
                job.Name = (task == null) ? "" : task.Name.ToString();

                job.CreatedBy = "";
                foreach (IfcTelecomAddress address in ifcTelecomAddresses)
                    job.CreatedBy = (address == null) ? "" : address.ToString() + ",";
                job.CreatedBy = job.CreatedBy.TrimEnd(',');

                job.CreatedOn = (ifcOwnerHistory.CreationDate == null) ? "" : ifcOwnerHistory.CreationDate.ToString();
                job.Category = (task == null) ? "" : task.ObjectType.ToString();
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

        public COBieSheet<COBiePickListsRow> GetCOBiePickListsSheet(string pickListsXMLFilePath)
        {
            // read xml document for picklists
            if (string.IsNullOrEmpty(pickListsXMLFilePath)) pickListsXMLFilePath = "PickLists.xml";
            XmlDocument xdoc = new XmlDocument();
            xdoc.Load(pickListsXMLFilePath);
            XmlNodeList items = xdoc.SelectNodes("//PickLists//Item");

            COBieSheet<COBiePickListsRow> pickLists = new COBieSheet<COBiePickListsRow>();

            foreach (XmlNode node in items)
            {
                COBiePickListsRow pickList = new COBiePickListsRow();
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

            IEnumerable<IfcTelecomAddress> ifcTelecomAddresses = model.InstancesOfType<IfcTelecomAddress>();
            if (ifcTelecomAddresses == null) ifcTelecomAddresses = Enumerable.Empty<IfcTelecomAddress>();

            IfcOwnerHistory ifcOwnerHistory = model.InstancesOfType<IfcOwnerHistory>().FirstOrDefault();
            
            COBieSheet<COBieResourceRow> resources = new COBieSheet<COBieResourceRow>();

            foreach (IfcConstructionEquipmentResource cer in ifcCer)
            {
                COBieResourceRow resource = new COBieResourceRow();
                resource.Name = (cer == null) ? "" : cer.Name.ToString();

                resource.CreatedBy = "";
                foreach (IfcTelecomAddress address in ifcTelecomAddresses)
                    resource.CreatedBy = (address == null) ? "" : address.ToString() + ",";
                resource.CreatedBy = resource.CreatedBy.TrimEnd(',');

                resource.CreatedOn = (ifcOwnerHistory.CreationDate == null) ? "" : ifcOwnerHistory.CreationDate.ToString();
                resource.Category = (cer == null) ? "" : cer.ObjectType.ToString();                
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

            IEnumerable<IfcTelecomAddress> ifcTelecomAddresses = model.InstancesOfType<IfcTelecomAddress>();
            if (ifcTelecomAddresses == null) ifcTelecomAddresses = Enumerable.Empty<IfcTelecomAddress>();

            IfcOwnerHistory ifcOwnerHistory = model.InstancesOfType<IfcOwnerHistory>().FirstOrDefault();
            COBieSheet<COBieFloorRow> floors = new COBieSheet<COBieFloorRow>();

            IfcClassification ifcClassification = model.InstancesOfType<IfcClassification>().FirstOrDefault();
            
            foreach (IfcBuildingStorey bs in buildingStories)
            {
                COBieFloorRow floor = new COBieFloorRow();               

                floor.Name = bs.Name.ToString();

                floor.CreatedBy = "";
                foreach (IfcTelecomAddress address in ifcTelecomAddresses)
                    floor.CreatedBy = (address == null) ? "" : address.ToString() + ",";
                floor.CreatedBy = floor.CreatedBy.TrimEnd(',');

                floor.CreatedOn = ifcOwnerHistory.CreationDate.ToString();

                floor.Category = "";
                foreach (COBiePickListsRow plRow in pickLists.Rows)
                    floor.Category = (plRow == null) ? "" : plRow.FloorType + ",";
                floor.Category = floor.Category.TrimEnd(',');

                floor.ExtSystem = GetIfcApplication().ApplicationFullName;
                floor.ExtObject = floor.GetType().Name;
                floor.ExtIdentifier = bs.GlobalId;
                floor.Description = GetFloorDescription(bs);
                floor.Elevation = bs.Elevation.ToString();
                floor.Height = DEFAULT_VAL;

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
            IEnumerable<IfcSpace> ifcSpaces = model.InstancesOfType<IfcSpace>();

            IEnumerable<IfcTelecomAddress> ifcTelecomAddresses = model.InstancesOfType<IfcTelecomAddress>();
            if (ifcTelecomAddresses == null) ifcTelecomAddresses = Enumerable.Empty<IfcTelecomAddress>();

            IfcOwnerHistory ifcOwnerHistory = model.InstancesOfType<IfcOwnerHistory>().FirstOrDefault();
            COBieSheet<COBieSpaceRow> spaces = new COBieSheet<COBieSpaceRow>();

            foreach (IfcSpace sp in ifcSpaces)
            {
                COBieSpaceRow space = new COBieSpaceRow();
                space.Name = sp.Name;

                space.CreatedBy = "";
                foreach (IfcTelecomAddress address in ifcTelecomAddresses)
                    space.CreatedBy = (address == null) ? "" : address.ToString() + ",";
                space.CreatedBy = space.CreatedBy.TrimEnd(',');

                space.CreatedOn = ifcOwnerHistory.CreationDate.ToString();

                space.Category = "";
                foreach (COBiePickListsRow plRow in pickLists.Rows)
                    space.Category = (plRow == null) ? "" : plRow.CategorySpace + ",";
                space.Category = space.Category.TrimEnd(',');

                space.FloorName = sp.Name;
                space.Description = GetSpaceDescription(sp);
                space.ExtSystem = GetIfcApplication().ApplicationFullName;
                space.ExtObject = sp.GetType().Name;
                space.ExtIdentifier = sp.GlobalId;
                space.RoomTag = GetSpaceDescription(sp);
                space.UsableHeight = DEFAULT_VAL;
                space.GrossArea = DEFAULT_VAL;
                space.NetArea = DEFAULT_VAL;
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

        #region Spare

        public COBieSheet<COBieSpareRow> GetCOBieSpareSheet(IModel model)
        {
            _model = model;

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcBuildingStorey> buildingStories = model.InstancesOfType<IfcBuildingStorey>();
            IfcTelecomAddress ifcTelecomAddres = model.InstancesOfType<IfcTelecomAddress>().FirstOrDefault();
            IfcOwnerHistory ifcOwnerHistory = model.InstancesOfType<IfcOwnerHistory>().FirstOrDefault();
            IfcConstructionProductResource cpr = model.InstancesOfType<IfcConstructionProductResource>().FirstOrDefault();
            IfcClassification classification = model.InstancesOfType<IfcClassification>().FirstOrDefault();
            IfcTypeObject typeObject = model.InstancesOfType<IfcTypeObject>().FirstOrDefault();

            COBieSheet<COBieSpareRow> spares = new COBieSheet<COBieSpareRow>();

            foreach (IfcBuildingStorey bs in buildingStories)
            {
                COBieSpareRow spare = new COBieSpareRow();
                spare.Name = (cpr == null) ? "" : cpr.Name.ToString();
                spare.CreatedBy = (ifcTelecomAddres == null) ? "" : ifcTelecomAddres.ElectronicMailAddresses[0].ToString();
                spare.CreatedOn = (ifcOwnerHistory.CreationDate == null) ? "" : ifcOwnerHistory.CreationDate.ToString();
                spare.Category = (classification == null) ? "" : classification.Name.ToString();
                spare.TypeName = (typeObject == null) ? "" : typeObject.Name.ToString();
                spare.Suppliers = "";
                spare.ExtSystem = GetIfcApplication().ApplicationFullName;
                spare.ExtObject = "";
                spare.ExtIdentifier = bs.GlobalId;
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

            IEnumerable<IfcTelecomAddress> ifcTelecomAddresses = model.InstancesOfType<IfcTelecomAddress>();
            if (ifcTelecomAddresses == null) ifcTelecomAddresses = Enumerable.Empty<IfcTelecomAddress>();
                        
            IfcOwnerHistory ifcOwnerHistory = model.InstancesOfType<IfcOwnerHistory>().FirstOrDefault();
            COBieSheet<COBieZoneRow> zones = new COBieSheet<COBieZoneRow>();

            foreach (IfcZone zn in ifcZones)
            {
                COBieZoneRow zone = new COBieZoneRow();
                zone.Name = zn.Name.ToString();

                zone.CreatedBy = "";
                foreach (IfcTelecomAddress address in ifcTelecomAddresses)
                    zone.CreatedBy = (address == null) ? "" : address.ToString() + ",";
                zone.CreatedBy = zone.CreatedBy.TrimEnd(',');
                
                zone.CreatedOn = ifcOwnerHistory.CreationDate.ToString();

                zone.Category = "";
                foreach (COBiePickListsRow plRow in pickLists.Rows)
                    zone.Category = (plRow == null) ? "" : plRow.ZoneType + ",";
                zone.Category = zone.Category.TrimEnd(',');

                IEnumerable<IfcSpace> spaces = (zn.IsGroupedBy == null) ? Enumerable.Empty<IfcSpace>() : zn.IsGroupedBy.RelatedObjects.OfType<IfcSpace>();
                zone.SpaceNames = "";
                foreach (IfcLabel lbl in spaces.Select(s => s.Name))
                    zone.SpaceNames = (lbl == null) ? "" : lbl.ToString() + ",";
                zone.SpaceNames = zone.SpaceNames.TrimEnd(',');
                
                zone.ExtSystem = GetIfcApplication().ApplicationFullName;
                zone.ExtObject = zn.GetType().Name;
                zone.ExtIdentifier = zn.GlobalId;
                zone.Description = (string.IsNullOrEmpty(zn.Description)) ? DEFAULT_VAL : zn.Description.ToString();
                                
                zones.Rows.Add(zone);
            }

            return zones;
        }

        #endregion

        #region Type

        public COBieSheet<COBieTypeRow> GetCOBieTypeSheet(IModel model)
        {
            _model = model;

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcTypeObject> ifcTypeObjects = model.InstancesOfType<IfcTypeObject>();
            IfcTelecomAddress ifcTelecomAddres = model.InstancesOfType<IfcTelecomAddress>().FirstOrDefault();
            IfcOwnerHistory ifcOwnerHistory = model.InstancesOfType<IfcOwnerHistory>().FirstOrDefault();
            COBieSheet<COBieTypeRow> types = new COBieSheet<COBieTypeRow>();

            foreach (IfcTypeObject to in ifcTypeObjects)
            {
                COBieTypeRow typ = new COBieTypeRow();
                typ.Name = to.Name;
                typ.CreatedBy = (ifcTelecomAddres == null) ? "" : ifcTelecomAddres.ElectronicMailAddresses[0].ToString();
                typ.CreatedOn = ifcOwnerHistory.CreationDate.ToString();
                typ.Category = GetTypeProductCategory(to);
                typ.Description = GetDoorStyleDescription(to);
                typ.AssetType = "";
                typ.Manufacturer = "";
                typ.ExtSystem = GetIfcApplication().ApplicationFullName;
                typ.ExtObject = to.GetType().ToString().Substring(to.GetType().ToString().LastIndexOf('.') + 1);
                typ.ExtIdentifier = to.GlobalId;
                typ.ReplacementCost = "";
                typ.ExpectedLife = "";
                typ.DurationUnit = "";
                typ.WarrantyDescription = GetTypeWarrantyDescription(to);
                typ.NominalLength = "";
                typ.NominalWidth = "";
                typ.NominalHeight = "";
                typ.ModelReference = "";
                typ.Shape = "";
                typ.Size = "";
                typ.Colour = "";
                typ.Finish = "";
                typ.Grade = "";
                typ.Material = "";
                typ.Constituents = "";
                typ.Features = "";
                typ.AccessibilityPerformance = "";
                typ.CodePerformance = "";
                typ.SustainabilityPerformance = "";

                types.Rows.Add(typ);

            //// get all IfcBuildingStory objects from IFC file
            //IEnumerable<IfcDoorStyle> ifcDoorStyles = model.InstancesOfType<IfcDoorStyle>();
            //List<COBieType> types = new List<COBieType>();

            //int ids = 0;
            //foreach (IfcDoorStyle ds in ifcDoorStyles)
            //{
            //    COBieType typ = new COBieType();
            //    typ.TypeId = ids++;
            //    typ.Name = ds.Name;
            //    typ.CreatedBy = "";
            //    typ.CreatedOn = DateTime.Now;
            //    typ.Category = GetDoorStyleCategory(ds);
            //    typ.Description = GetDoorStyleDescription(ds);
            //    typ.AssetType = "";
            //    typ.Manufacturer = "";
            //    typ.ExtSystem = GetIfcApplication().ApplicationFullName;
            //    typ.ExtObject = "IfcZone";
            //    typ.ExtIdentifier = ds.GlobalId;
            //    typ.ReplacementCost = "";
            //    typ.ExpectedLife = "";
            //    typ.DurationUnit = "";
            //    typ.WarrantyDescription = GetTypeWarrantyDescription(ds);
            //    typ.NominalLength = "";
            //    typ.NominalWidth = "";
            //    typ.NominalHeight = "";
            //    typ.ModelReference = "";
            //    typ.Shape = "";
            //    typ.Size = "";
            //    typ.Colour = "";
            //    typ.Finish = "";
            //    typ.Grade = "";
            //    typ.Material = "";
            //    typ.Constituents = "";
            //    typ.Features = "";
            //    typ.AccessibilityPerformance = "";
            //    typ.CodePerformance = "";
            //    typ.SustainabilityPerformance = "";

            //    types.Add(typ);
            }

            return types;
        }

        private string GetTypeProductCategory(IfcTypeObject ds)
        {
            return ds.Description;
        }

        private string GetDoorStyleDescription(IfcTypeObject ds)
        {
            if (ds != null)
            {
                if (!string.IsNullOrEmpty(ds.Description)) return ds.Description;
                else if (!string.IsNullOrEmpty(ds.Name)) return ds.Name;
            }
            return DEFAULT_VAL;
        }

        private string GetTypeWarrantyDescription(object ds)
        {
            string desc = "";
            int index = ds.GetType().ToString().LastIndexOf('.');
            desc = ds.GetType().ToString().Substring(index + 4);
            
            if (ds is IfcDistributionElementType)
                desc += " " + ((IfcDistributionElementType)ds).Name;
            else if (ds is IfcElementType)
                desc += " " + ((IfcElementType)ds).Name;

            return desc;
        }

        #endregion
        
        #region Component

        public COBieSheet<COBieComponentRow> GetCOBieComponentSheet(IModel model)
        {
            _model = model;

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcFlowTerminal> ifcFlowTerminals = model.InstancesOfType<IfcFlowTerminal>();
            IfcTelecomAddress ifcTelecomAddres = model.InstancesOfType<IfcTelecomAddress>().FirstOrDefault();
            IfcOwnerHistory ifcOwnerHistory = model.InstancesOfType<IfcOwnerHistory>().FirstOrDefault();
            COBieSheet<COBieComponentRow> components = new COBieSheet<COBieComponentRow>();

            foreach (IfcFlowTerminal ft in ifcFlowTerminals)
            {
                COBieComponentRow component = new COBieComponentRow();
                component.Name = ft.Name;
                component.CreatedBy = (ifcTelecomAddres == null) ? "" : ifcTelecomAddres.ElectronicMailAddresses[0].ToString();
                component.CreatedOn = ifcOwnerHistory.CreationDate.ToString();
                component.TypeName = ft.ObjectType.ToString();
                component.Space = "";
                component.Description = GetComponentDescription(ft);
                component.ExtSystem = GetIfcApplication().ApplicationFullName;
                component.ExtObject = "IfcFlowTerminal";
                component.ExtIdentifier = ft.GlobalId;
                component.SerialNumber = "";
                component.InstallationDate = "";
                component.WarrantyStartDate = "";
                component.TagNumber = ft.Tag.ToString();
                component.BarCode = "";
                component.AssetIdentifier = "";

                components.Rows.Add(component);
            }

            return components;
        }

        private string GetComponentDescription(IfcFlowTerminal ft)
        {
            if (ft != null)
            {
                if (!string.IsNullOrEmpty(ft.Description)) return ft.Description;
                else if (!string.IsNullOrEmpty(ft.Name)) return ft.Name;
            }
            return DEFAULT_VAL;
        }

        #endregion

        #region System

        public COBieSheet<COBieSystemRow> GetCOBieSystemSheet(IModel model)
        {
            _model = model;

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcSystem> ifcSystems = model.InstancesOfType<IfcSystem>();
            IfcTelecomAddress ifcTelecomAddres = model.InstancesOfType<IfcTelecomAddress>().FirstOrDefault();
            IfcOwnerHistory ifcOwnerHistory = model.InstancesOfType<IfcOwnerHistory>().FirstOrDefault();
            COBieSheet<COBieSystemRow> systems = new COBieSheet<COBieSystemRow>();

            int ids = 0;
            foreach (IfcSystem s in ifcSystems)
            {
                COBieSystemRow sys = new COBieSystemRow();
                sys.Name = s.Name;
                sys.CreatedBy = (ifcTelecomAddres == null) ? "" : ifcTelecomAddres.ElectronicMailAddresses[0].ToString();
                sys.CreatedOn = ifcOwnerHistory.CreationDate.ToString();
                sys.Category = GetSystemCategory(s);
                sys.ComponentName = s.Name;
                sys.ExtSystem = GetIfcApplication().ApplicationFullName;
                sys.ExtObject = "IfcSystem";
                sys.ExtIdentifier = s.GlobalId;
                sys.Description = GetSystemDescription(s);

                systems.Rows.Add(sys);
            }

            return systems;
        }

        private string GetSystemCategory(IfcSystem s)
        {
            return s.Description;
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
            IfcTelecomAddress ifcTelecomAddres = model.InstancesOfType<IfcTelecomAddress>().FirstOrDefault();
            COBieSheet<COBieAssemblyRow> assemblies = new COBieSheet<COBieAssemblyRow>();

            IfcOwnerHistory ifcOwnerHistory = model.InstancesOfType<IfcOwnerHistory>().FirstOrDefault();
            IfcProduct ifcProduct = model.InstancesOfType<IfcProduct>().FirstOrDefault();
            IfcClassification ifcClassification = model.InstancesOfType<IfcClassification>().FirstOrDefault();
            string applicationFullName = GetIfcApplication().ApplicationFullName;

            foreach (IfcRelAggregates ra in ifcRelAggregates)
            {
                COBieAssemblyRow assembly = new COBieAssemblyRow();
                assembly.Name = (ra.Name == null || ra.Name.ToString() == "") ? "AssemblyName" : ra.Name.ToString();
                assembly.CreatedBy = (ifcTelecomAddres == null) ? "" : ifcTelecomAddres.ElectronicMailAddresses[0].ToString();
                assembly.CreatedOn = ifcOwnerHistory.CreationDate.ToString();
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
            COBieSheet<COBieConnectionRow> connections = new COBieSheet<COBieConnectionRow>();

            IfcRelConnectsPorts relCP = model.InstancesOfType<IfcRelConnectsPorts>().FirstOrDefault();

            IfcProduct product = model.InstancesOfType<IfcProduct>().FirstOrDefault();

            int ids = 0;
            foreach (IfcRelConnectsElements c in ifcConnections)
            {
                COBieConnectionRow conn = new COBieConnectionRow();
                conn.Name = (string.IsNullOrEmpty(c.Name)) ? ids.ToString() : c.Name.ToString();
                conn.CreatedBy = (ifcTelecomAddres == null) ? "" : ifcTelecomAddres.ElectronicMailAddresses[0].ToString();
                conn.CreatedOn = ifcOwnerHistory.CreationDate.ToString();
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
            COBieSheet<COBieCoordinateRow> coordinates = new COBieSheet<COBieCoordinateRow>();

            IfcOwnerHistory ifcOwnerHistory = model.InstancesOfType<IfcOwnerHistory>().FirstOrDefault();
            IfcProduct ifcProduct = model.InstancesOfType<IfcProduct>().FirstOrDefault();
            IfcClassification ifcClassification = model.InstancesOfType<IfcClassification>().FirstOrDefault();
            string applicationFullName = GetIfcApplication().ApplicationFullName;

            foreach (IfcRelAggregates ra in ifcRelAggregates)
            {
                COBieCoordinateRow coordinate = new COBieCoordinateRow();
                coordinate.Name = (ifcBuildingStorey == null || ifcBuildingStorey.Name.ToString() == "") ? "CoordinateName" : ifcBuildingStorey.Name.ToString();
                coordinate.CreatedBy = (ifcTelecomAddres == null) ? "" : ifcTelecomAddres.ElectronicMailAddresses[0].ToString();
                coordinate.CreatedOn = ifcOwnerHistory.CreationDate.ToString();
                coordinate.Category = "PickList.CoordinateType";
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
            COBieSheet<COBieAttributeRow> attributes = new COBieSheet<COBieAttributeRow>();

            IfcOwnerHistory ifcOwnerHistory = model.InstancesOfType<IfcOwnerHistory>().FirstOrDefault();
            IfcProduct ifcProduct = model.InstancesOfType<IfcProduct>().FirstOrDefault();
            IfcClassification ifcClassification = model.InstancesOfType<IfcClassification>().FirstOrDefault();
            string applicationFullName = GetIfcApplication().ApplicationFullName;

            foreach (IfcObject obj in ifcObject)
            {
                COBieAttributeRow attribute = new COBieAttributeRow();
                attribute.Name = (ifcBuildingStorey == null || ifcBuildingStorey.Name.ToString() == "") ? "AttributeName" : ifcBuildingStorey.Name.ToString();
                attribute.CreatedBy = (ifcTelecomAddres == null) ? "" : ifcTelecomAddres.ElectronicMailAddresses[0].ToString();
                attribute.CreatedOn = ifcOwnerHistory.CreationDate.ToString();
                attribute.Category = "Requirement";
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
    }
}
