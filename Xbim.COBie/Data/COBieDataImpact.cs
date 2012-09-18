using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.Rows;
using Xbim.Ifc.Kernel;
using Xbim.XbimExtensions;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the Impact tab.
    /// </summary>
    public class COBieDataImpact : COBieData<COBieImpactRow>
    {

        /// <summary>
        /// Data Impact constructor
        /// </summary>
        /// <param name="model">The context of the model being generated</param>
        public COBieDataImpact(COBieContext context) : base(context)
        { }

        #region Methods

        /// <summary>
        /// Fill sheet rows for Impact sheet
        /// </summary>
        /// <returns>COBieSheet<COBieImpactRow></returns>
        public override COBieSheet<COBieImpactRow> Fill()
        {
            ProgressIndicator.ReportMessage("Starting Impacts...");

            //create new sheet
            COBieSheet<COBieImpactRow> impacts = new COBieSheet<COBieImpactRow>(Constants.WORKSHEET_IMPACT);

            // get all IfcPropertySet objects from IFC file

            IEnumerable<IfcPropertySet> ifcProperties = Model.InstancesOfType<IfcPropertySet>().Where(ps => ps.Name.ToString() == "Pset_EnvironmentalImpactValues");

            ProgressIndicator.Initialise("Creating Impacts", ifcProperties.Count());

            foreach (IfcPropertySet ppt in ifcProperties)
            {
                ProgressIndicator.IncrementAndUpdate();

                COBieImpactRow impact = new COBieImpactRow(impacts);

                impact.Name = ppt.Name;

                impact.CreatedBy = GetTelecomEmailAddress(ppt.OwnerHistory);
                impact.CreatedOn = GetCreatedOnDateAsFmtString(ppt.OwnerHistory);

                impact.ImpactType = "";
                impact.ImpactStage = "";
                impact.SheetName = "";
                //foreach (COBiePickListsRow plRow in pickLists.Rows)
                //{
                //    impact.ImpactType = (plRow == null) ? "" : plRow.ImpactType + ",";
                //    impact.ImpactStage = (plRow == null) ? "" : plRow.ImpactStage + ",";
                //    impact.SheetName = (plRow == null) ? "" : plRow.SheetType + ",";
                //}
                //impact.ImpactType = impact.ImpactType.TrimEnd(',');
                //impact.ImpactStage = impact.ImpactStage.TrimEnd(',');
                //impact.SheetName = impact.SheetName.TrimEnd(',');

                impact.RowName = DEFAULT_STRING;
                impact.Value = "";
                impact.ImpactUnit = "";
                impact.LeadInTime = "";
                impact.Duration = "";
                impact.LeadOutTime = "";
                impact.ExtSystem = ifcApplication.ApplicationFullName;
                impact.ExtObject = impact.GetType().Name;
                impact.ExtIdentifier = ppt.GlobalId;
                impact.Description = (ppt.Description == null) ? DEFAULT_STRING : ppt.Description.ToString();

                impacts.Rows.Add(impact);
            }
            ProgressIndicator.Finalise();

            return impacts;
        }
        #endregion
    }
}
