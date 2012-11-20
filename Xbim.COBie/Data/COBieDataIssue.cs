using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.Rows;
using Xbim.Ifc.ApprovalResource;
using Xbim.Ifc.UtilityResource;
using Xbim.XbimExtensions;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ControlExtension;
using Xbim.Ifc.PropertyResource;
using Xbim.Ifc.ProcessExtensions;
using Xbim.Ifc.ActorResource;
using Xbim.Ifc.MeasureResource;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the Issue tab.
    /// </summary>
    public class COBieDataIssue : COBieData<COBieIssueRow>
    {
        /// <summary>
        /// Data Issue constructor
        /// </summary>
        /// <param name="model">The context of the model being generated</param>
        public COBieDataIssue(COBieContext context) : base(context)
        { }

        #region Methods
        /// <summary>
        /// Fill sheet rows for Issue sheet
        /// </summary>
        /// <returns>COBieSheet<COBieIssueRow></returns>
        public override COBieSheet<COBieIssueRow> Fill()
        {
            ProgressIndicator.ReportMessage("Starting Issues...");

            //create new sheet
            COBieSheet<COBieIssueRow> issues = new COBieSheet<COBieIssueRow>(Constants.WORKSHEET_ISSUE);
            
            //IEnumerable<IfcPropertySet> ifcProperties = Model.InstancesOfType<IfcPropertySet>().Where(ps => ps.Name.ToString() == "Pset_Risk");
            

            // get all IfcApproval objects from IFC file
            IEnumerable<IfcApproval> ifcApprovals = Model.InstancesOfType<IfcApproval>();
            ProgressIndicator.Initialise("Creating Issues", ifcApprovals.Count());
            foreach (IfcApproval ifcApproval in ifcApprovals)
            {
                ProgressIndicator.IncrementAndUpdate();
                COBieIssueRow issue = new COBieIssueRow(issues);
                //get the associated property setIfcPropertySet
                var ifcPropertySet = Model.InstancesOfType<IfcRelAssociatesApproval>()
                                   .Where(ral => ral.RelatingApproval == ifcApproval)
                                   .SelectMany(ral => ral.RelatedObjects.OfType<IfcPropertySet>())
                                   .Where(ps => ps.Name == "Pset_Risk")
                                   .FirstOrDefault();

                List<IfcSimpleProperty> propertyList = new List<IfcSimpleProperty>();
                if (ifcPropertySet != null)
                    propertyList = ifcPropertySet.HasProperties.OfType<IfcSimpleProperty>().ToList();

                issue.Name = (string.IsNullOrEmpty(ifcApproval.Name)) ? DEFAULT_STRING : ifcApproval.Name.ToString();

                //lets default the creator to that user who created the project for now, no direct link to OwnerHistory on IfcApproval
                if (ifcPropertySet != null)
                {
                    //use "Pset_Risk" Property Set as source for this
                    issue.CreatedBy = GetTelecomEmailAddress(ifcPropertySet.OwnerHistory);
                    issue.CreatedOn = GetCreatedOnDateAsFmtString(ifcPropertySet.OwnerHistory);
                }
                else
                {
                    //if property set is null use project defaults
                    issue.CreatedBy = GetTelecomEmailAddress(Model.IfcProject.OwnerHistory);
                    issue.CreatedOn = GetCreatedOnDateAsFmtString(Model.IfcProject.OwnerHistory);
                }
                Interval propValues = GetPropertyEnumValue(propertyList, "RiskType");
                issue.Type = propValues.Value;

                propValues = GetPropertyEnumValue(propertyList, "RiskRating");
                issue.Risk = propValues.Value;

                propValues = GetPropertyEnumValue(propertyList, "AssessmentOfRisk");
                issue.Chance = propValues.Value;

                propValues = GetPropertyEnumValue(propertyList, "RiskConsequence");
                issue.Impact = propValues.Value;
                //GetIt(typeof(IfcApproval));
                //Risk assessment has to be on a task so we should have one
                List<IfcRoot> IfcRoots = GetIfcObjects(ifcApproval);
                issue.SheetName1 = (IfcRoots.Count > 0) ? GetSheetByObjectType(IfcRoots[0].GetType()) : DEFAULT_STRING;
                issue.RowName1 = (IfcRoots.Count > 0) ? IfcRoots[0].Name.ToString() : DEFAULT_STRING;

                //assuming that this row is a person associated with the ifcApproval, but might be a task
                string email = GetContact(ifcApproval);
                if (email == DEFAULT_STRING) //if no email, see if we have another ifcobject
                {
                    issue.SheetName2 = (IfcRoots.Count > 1) ? GetSheetByObjectType(IfcRoots[1].GetType()) : DEFAULT_STRING;
                    issue.RowName2 = (IfcRoots.Count > 1) ? IfcRoots[1].Name.ToString() : DEFAULT_STRING;
                }
                else
                {
                    issue.SheetName2 = (email != DEFAULT_STRING) ? Constants.WORKSHEET_CONTACT : DEFAULT_STRING;
                    issue.RowName2 = (email != DEFAULT_STRING) ? email : DEFAULT_STRING;
                }
                 
                issue.Description = (string.IsNullOrEmpty(ifcApproval.Description.ToString())) ? DEFAULT_STRING : ifcApproval.Description.ToString();

                propValues = GetPropertyEnumValue(propertyList, "RiskOwner");
                issue.Owner = propValues.Value;

                propValues = GetPropertyValue(propertyList, "PreventiveMeassures");
                issue.Mitigation = propValues.Value;

                issue.ExtSystem = (ifcPropertySet != null) ? GetExternalSystem(ifcPropertySet) : DEFAULT_STRING;
                issue.ExtObject = ifcApproval.GetType().Name;
                issue.ExtIdentifier = ifcApproval.Identifier.ToString();

                issues.AddRow(issue);
            }

            ProgressIndicator.Finalise();
            return issues;
        }
        /// <summary>
        /// get all the IfcRoot objects attached to the ifcApproval
        /// </summary>
        /// <param name="ifcApproval">IfcApproval Object</param>
        /// <returns>List of IfcRoot Objects</returns>
        private List<IfcRoot> GetIfcObjects(IfcApproval ifcApproval)
        {
            List<IfcRoot> ifcRootObjs = new List<IfcRoot>();
            IEnumerable<IfcRoot> IfcRoots = Model.InstancesWhere<IfcRelAssociatesApproval>(ral => ral.RelatingApproval.EntityLabel == ifcApproval.EntityLabel)
                                            .SelectMany(ral => ral.RelatedObjects).OfType<IfcRoot>();
            foreach (IfcRoot item in IfcRoots)
            {
                if (item.GetType() != typeof(IfcPropertySet))
                {
                    ifcRootObjs.Add(item);
                }
            }
            
            return ifcRootObjs;
        }

        

        private string GetContact(IfcApproval ifcApproval)
        {
            string eMail = DEFAULT_STRING;

            IfcPersonAndOrganization IfcPersonAndOrganization = Model.InstancesOfType<IfcApprovalActorRelationship>()
                                                       .Where(aar => aar.Approval.EntityLabel == ifcApproval.EntityLabel)
                                                       .Select(aar => aar.Actor).OfType<IfcPersonAndOrganization>().FirstOrDefault();
        
            if(IfcPersonAndOrganization != null)
            {
                eMail = GetTelecomEmailAddress(IfcPersonAndOrganization);
            }

            return eMail;
        }

        #endregion
    }
}
