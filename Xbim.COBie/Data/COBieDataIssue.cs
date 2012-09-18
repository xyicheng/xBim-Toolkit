using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.Rows;
using Xbim.Ifc.ApprovalResource;
using Xbim.Ifc.UtilityResource;
using Xbim.XbimExtensions;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the Issue tab.
    /// </summary>
    public class COBieDataIssue : COBieData
    {
        /// <summary>
        /// Data Issue constructor
        /// </summary>
        /// <param name="model">IModel to read data from</param>
        public COBieDataIssue(IModel model)
        {
            Model = model;
        }

        #region Methods
        /// <summary>
        /// Fill sheet rows for Issue sheet
        /// </summary>
        /// <returns>COBieSheet<COBieIssueRow></returns>
        public COBieSheet<COBieIssueRow> Fill()
        {
            //create new sheet
            COBieSheet<COBieIssueRow> issues = new COBieSheet<COBieIssueRow>(Constants.WORKSHEET_ISSUE);
            
            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcApproval> ifcApprovals = Model.InstancesOfType<IfcApproval>();

            //IEnumerable<IfcTelecomAddress> ifcTelecomAddresses = Model.InstancesOfType<IfcTelecomAddress>();
            //if (ifcTelecomAddresses == null) ifcTelecomAddresses = Enumerable.Empty<IfcTelecomAddress>();

            IfcOwnerHistory ifcOwnerHistory = Model.InstancesOfType<IfcOwnerHistory>().FirstOrDefault();
            IfcApproval approval = Model.InstancesOfType<IfcApproval>().FirstOrDefault();
            
            foreach (IfcApproval app in ifcApprovals)
            {
                COBieIssueRow issue = new COBieIssueRow(issues);
                issue.Name = (approval == null) ? "" : approval.Name.ToString();
                                
                issue.CreatedBy = GetTelecomEmailAddress(ifcOwnerHistory);
                issue.CreatedOn = GetCreatedOnDateAsFmtString(ifcOwnerHistory);

                issue.Type = "";
                issue.Risk = "";
                issue.Chance = "";
                issue.Impact = "";
                issue.SheetName1 = "";
                //foreach (COBiePickListsRow plRow in pickLists.Rows)
                //{
                //    issue.Type = plRow.IssueCategory + ",";
                //    issue.Risk = plRow.IssueRisk + ",";
                //    issue.Chance = plRow.IssueChance + ",";
                //    issue.Impact = plRow.IssueImpact + ",";
                //    issue.SheetName1 = plRow.SheetType + ",";
                //}
                //issue.Type = issue.Type.TrimEnd(',');
                //issue.Risk = issue.Risk.TrimEnd(',');
                //issue.Chance = issue.Chance.TrimEnd(',');
                //issue.Impact = issue.Impact.TrimEnd(',');
                //issue.SheetName1 = issue.SheetName1.TrimEnd(',');

                issue.RowName1 = DEFAULT_STRING;
                issue.SheetName2 = "";
                issue.RowName2 = DEFAULT_STRING;
                issue.Description = (approval == null) ? DEFAULT_STRING : approval.Description.ToString();
                issue.Owner = issue.CreatedBy;
                issue.Mitigation = "";
                issue.ExtSystem = ifcApplication.ApplicationFullName;
                issue.ExtObject = app.GetType().Name;
                issue.ExtIdentifier = app.Identifier.ToString();

                issues.Rows.Add(issue);
            }

            return issues;
        }
        #endregion
    }
}
