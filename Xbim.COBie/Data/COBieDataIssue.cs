using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.Rows;
using Xbim.Ifc2x3.ApprovalResource;
using Xbim.Ifc2x3.UtilityResource;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.Kernel;

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
            
            //IEnumerable<IfcPropertySet> ifcProperties = Model.Instances.OfType<IfcPropertySet>().Where(ps => ps.Name.ToString() == "Pset_Risk");
            

            // get all IfcApproval objects from IFC file
            IEnumerable<IfcApproval> ifcApprovals = Model.Instances.OfType<IfcApproval>();
            ProgressIndicator.Initialise("Creating Issues", ifcApprovals.Count());
            foreach (IfcApproval ifcApproval in ifcApprovals)
            {
                

                ProgressIndicator.IncrementAndUpdate();
                if (ifcApproval == null) continue; //skip if null

                COBieIssueRow issue = new COBieIssueRow(issues);
                issue.Name = (string.IsNullOrEmpty(ifcApproval.Name.ToString())) ? DEFAULT_STRING : ifcApproval.Name.ToString();

                //lets default the creator to that user who created the project for now, no direct link to OwnerHistory on IfcApproval
                issue.CreatedBy = GetTelecomEmailAddress((Model.IfcProject as IfcRoot).OwnerHistory);
                issue.CreatedOn = GetCreatedOnDateAsFmtString((Model.IfcProject as IfcRoot).OwnerHistory);
               
                issue.Type = DEFAULT_STRING;
                issue.Risk = DEFAULT_STRING;
                issue.Chance = DEFAULT_STRING;
                issue.Impact = DEFAULT_STRING;
                issue.SheetName1 = DEFAULT_STRING;
                issue.RowName1 = DEFAULT_STRING;
                issue.SheetName2 = DEFAULT_STRING;
                issue.RowName2 = DEFAULT_STRING;
                issue.Description = (string.IsNullOrEmpty(ifcApproval.Description.ToString())) ? DEFAULT_STRING : ifcApproval.Description.ToString();
                issue.Owner = issue.CreatedBy;
                issue.Mitigation = DEFAULT_STRING;
                issue.ExtSystem = DEFAULT_STRING;   // TODO: IfcApprocval is not a Root object so has no Owner. What should this be?
                issue.ExtObject = ifcApproval.GetType().Name;
                issue.ExtIdentifier = ifcApproval.Identifier.ToString();

                issues.Rows.Add(issue);
            }

            ProgressIndicator.Finalise();
            return issues;
        }
        #endregion
    }
}
