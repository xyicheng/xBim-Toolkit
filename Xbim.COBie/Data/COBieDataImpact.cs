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

                impact.ImpactType = DEFAULT_STRING;
                impact.ImpactStage = DEFAULT_STRING;
                impact.SheetName = DEFAULT_STRING;
                
                impact.RowName = DEFAULT_STRING;
                impact.Value = DEFAULT_STRING;
                impact.ImpactUnit = DEFAULT_STRING;
                impact.LeadInTime = DEFAULT_STRING;
                impact.Duration = DEFAULT_STRING;
                impact.LeadOutTime = DEFAULT_STRING;
                impact.ExtSystem = GetExternalSystem(ppt);
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
