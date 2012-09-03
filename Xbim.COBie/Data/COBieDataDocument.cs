using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.Rows;
using Xbim.Ifc.ActorResource;
using Xbim.Ifc.ExternalReferenceResource;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.UtilityResource;
using Xbim.XbimExtensions;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the Document tab.
    /// </summary>
    public class COBieDataDocument : COBieData
    {
        /// <summary>
        /// Data Document constructor
        /// </summary>
        /// <param name="model">IModel to read data from</param>
        public COBieDataDocument(IModel model)
        {
            Model = model;
        }

        #region Methods

        /// <summary>
        /// Fill sheet rows for Document sheet
        /// </summary>
        /// <returns>COBieSheet<COBieDocumentRow></returns>
        public COBieSheet<COBieDocumentRow> Fill()
        {
            //create new sheet
            COBieSheet<COBieDocumentRow> documents = new COBieSheet<COBieDocumentRow>(Constants.WORKSHEET_DOCUMENT);

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcDocumentInformation> docInfos = Model.InstancesOfType<IfcDocumentInformation>();
            //get the owner history
            IfcOwnerHistory ifcOwnerHistory = Model.InstancesOfType<IfcOwnerHistory>().FirstOrDefault();
            
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
                //foreach (COBiePickListsRow plRow in pickLists.Rows)
                //    doc.SheetName = (plRow == null) ? "" : plRow.SheetType + ",";
                //doc.SheetName = doc.SheetName.TrimEnd(',');

                doc.RowName = DEFAULT_STRING;
                doc.Directory = di.DocumentId.ToString();
                doc.File = di.DocumentId.ToString();
                doc.ExtSystem = GetIfcApplication().ApplicationFullName;
                doc.ExtObject = di.GetType().Name;
                doc.ExtIdentifier = di.DocumentId.ToString();
                doc.Description = di.Description.ToString();
                doc.Reference = di.Name.ToString();

                documents.Rows.Add(doc);
            }

            return documents;
        }

        //private string GetDocumentCategory(IfcBuildingStorey bs)
        //{
        //    return (bs.LongName == null) ? "Category" : bs.LongName.ToString();
        //}

        //private string GetDocumentDescription(IfcBuildingStorey bs)
        //{
        //    if (bs != null)
        //    {
        //        if (!string.IsNullOrEmpty(bs.LongName)) return bs.LongName;
        //        else if (!string.IsNullOrEmpty(bs.Description)) return bs.Description;
        //        else if (!string.IsNullOrEmpty(bs.Name)) return bs.Name;
        //    }
        //    return DEFAULT_VAL;
        //}

        #endregion

    }
}
