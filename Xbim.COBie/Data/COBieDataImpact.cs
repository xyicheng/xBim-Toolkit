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
    public class COBieDataImpact : COBieData
    {
        /// <summary>
        /// Data Impact constructor
        /// </summary>
        /// <param name="model">IModel to read data from</param>
        public COBieDataImpact(IModel model)
        {
            Model = model;
        }

        #region Methods
        /// <summary>
        /// Fill sheet rows for Impact sheet
        /// </summary>
        /// <returns>COBieSheet<COBieImpactRow></returns>
        public COBieSheet<COBieImpactRow> Fill()
        {
            //create new sheet
            COBieSheet<COBieImpactRow> impacts = new COBieSheet<COBieImpactRow>(Constants.WORKSHEET_IMPACT);

            // get all IfcPropertySet objects from IFC file

            IEnumerable<IfcPropertySet> ifcProperties = Model.InstancesOfType<IfcPropertySet>().Where(ps => ps.Name.ToString() == "Pset_EnvironmentalImpactValues");
                        
            foreach (IfcPropertySet ppt in ifcProperties)
            {
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
                impact.ExtSystem = GetIfcApplication().ApplicationFullName;
                impact.ExtObject = impact.GetType().Name;
                impact.ExtIdentifier = ppt.GlobalId;
                impact.Description = (ppt.Description == null) ? DEFAULT_STRING : ppt.Description.ToString();

                impacts.Rows.Add(impact);
            }

            return impacts;
        }
        #endregion
    }
}
