using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.COBie.Rows;
using Xbim.Ifc.ConstructionMgmtDomain;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ExternalReferenceResource;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the Spare tab.
    /// </summary>
    public class COBieDataSpare : COBieData
    {

        /// <summary>
        /// Data Spare constructor
        /// </summary>
        /// <param name="model">IModel to read data from</param>
        public COBieDataSpare(IModel model)
        {
            Model = model;
        }

        #region Methods

        /// <summary>
        /// Fill sheet rows for Spare sheet
        /// </summary>
        /// <returns>COBieSheet<COBieSpareRow></returns>
        public COBieSheet<COBieSpareRow> Fill()
        {
          
            //Create new sheet
            COBieSheet<COBieSpareRow> spares = new COBieSheet<COBieSpareRow>(Constants.WORKSHEET_SPARE);
                        // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcConstructionProductResource> ifcConstructionProductResources = Model.InstancesOfType<IfcConstructionProductResource>();

            IfcTypeObject typeObject = Model.InstancesOfType<IfcTypeObject>().FirstOrDefault();

            
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
                //foreach (COBiePickListsRow plRow in pickLists.Rows)
                //    spare.ExtObject = (plRow == null) ? "" : plRow.ObjType + ",";
                //spare.ExtObject = spare.ExtObject.TrimEnd(',');

                spare.ExtIdentifier = cpr.GlobalId;
                spare.Description = (cpr == null) ? "" : cpr.Description.ToString();
                spare.SetNumber = "";
                spare.PartNumber = "";

                spares.Rows.Add(spare);
            }

            return spares;
        }
        #endregion
    }
}
