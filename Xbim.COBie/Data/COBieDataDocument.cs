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
using Xbim.Ifc.Kernel;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the Document tab.
    /// </summary>
    public class COBieDataDocument : COBieData<COBieDocumentRow>
    {
        /// <summary>
        /// Data Document constructor
        /// </summary>
        /// <param name="model">The context of the model being generated</param>
        public COBieDataDocument(COBieContext context) : base(context)
        { }

        #region Methods

        /// <summary>
        /// Fill sheet rows for Document sheet
        /// </summary>
        /// <returns>COBieSheet<COBieDocumentRow></returns>
        public override COBieSheet<COBieDocumentRow> Fill()
        {
            ProgressIndicator.ReportMessage("Starting Documents...");

            //create new sheet
            COBieSheet<COBieDocumentRow> documents = new COBieSheet<COBieDocumentRow>(Constants.WORKSHEET_DOCUMENT);

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcDocumentInformation> docInfos = Model.InstancesOfType<IfcDocumentInformation>();
            //get the owner history
            IfcOwnerHistory ifcOwnerHistory = Model.InstancesOfType<IfcOwnerHistory>().FirstOrDefault();

            ProgressIndicator.Initialise("Creating Documents", docInfos.Count());

            foreach (IfcDocumentInformation di in docInfos)
            {
                ProgressIndicator.IncrementAndUpdate();

                COBieDocumentRow doc = new COBieDocumentRow(documents);
                
                
                doc.Name = (di == null) ? "" : di.Name.ToString();

                //no IfcOwnerHistory so take the project OwnerHistory as default
                if (Model.IfcProject.OwnerHistory != null)
                {
                    doc.CreatedBy = GetTelecomEmailAddress(Model.IfcProject.OwnerHistory);
                    doc.CreatedOn = GetCreatedOnDateAsFmtString(Model.IfcProject.OwnerHistory);
                }
                
                //IfcRelAssociatesClassification ifcRAC = di.HasAssociations.OfType<IfcRelAssociatesClassification>().FirstOrDefault();
                //IfcClassificationReference ifcCR = (IfcClassificationReference)ifcRAC.RelatingClassification;
                doc.Category = di.Purpose.ToString();

                doc.ApprovalBy = di.IntendedUse.ToString();
                doc.Stage = di.Scope.ToString();

                //get the first associated document to extract the objects the document refers to
                IfcRelAssociatesDocument ifcRelAssociatesDocument = DocumentInformationForObjects(di).FirstOrDefault();
                
                RelatedObjectInformation relatedObjectInfo = GetRelatedObjectInformation(ifcRelAssociatesDocument);
                doc.SheetName = relatedObjectInfo.SheetName;
                doc.RowName = relatedObjectInfo.Name;
                doc.ExtObject = relatedObjectInfo.ExtObject;
                doc.ExtIdentifier = relatedObjectInfo.ExtIdentifier;
                doc.ExtSystem = relatedObjectInfo.ExtSystem;

                FileInformation fileInfo = GetFileInformation(ifcRelAssociatesDocument);
                doc.File = fileInfo.Name;
                doc.Directory = fileInfo.Location;

                
                doc.Description = di.Description.ToString();
                doc.Reference = di.Name.ToString();

                documents.Rows.Add(doc);
            }

            ProgressIndicator.Finalise();
            return documents;
        }

        /// <summary>
        /// Get the file information for the document attached to the ifcRelAssociatesDocument
        /// </summary>
        /// <param name="ifcRelAssociatesDocument">IfcRelAssociatesDocument object</param>
        /// <returns>FileInformation structure </returns>
        private FileInformation GetFileInformation(IfcRelAssociatesDocument ifcRelAssociatesDocument)
        {
            FileInformation DocInfo = new FileInformation() { Name = DEFAULT_STRING, Location = DEFAULT_STRING };
            string value = "";
                
            if (ifcRelAssociatesDocument != null)
            {
                //test for single document
                IfcDocumentReference ifcDocumentReference = ifcRelAssociatesDocument.RelatingDocument as IfcDocumentReference;
                if (ifcDocumentReference != null)
                {
                    value = ifcDocumentReference.ItemReference.ToString();
                    if (!string.IsNullOrEmpty(value)) DocInfo.Name = value;
                    value = ifcDocumentReference.Location.ToString();
                    if (!string.IsNullOrEmpty(value)) DocInfo.Location = value;
                }
                else //test for a document list
                {
                    IfcDocumentInformation ifcDocumentInformation = ifcRelAssociatesDocument.RelatingDocument as IfcDocumentInformation;
                    if (ifcDocumentInformation != null)
                    {
                        IEnumerable<IfcDocumentReference> ifcDocumentReferences = ifcDocumentInformation.DocumentReferences;
                        List<string> strNameValues = new List<string>();
                        List<string> strLocationValues = new List<string>();
                        foreach (IfcDocumentReference docRef in ifcDocumentReferences)
                        {
                            //get file name
                            value = docRef.ItemReference.ToString();
                            if (!string.IsNullOrEmpty(value)) strNameValues.Add(value);
                            //get file location
                            value = docRef.Location.ToString();
                            if ((!string.IsNullOrEmpty(value)) && (!strNameValues.Contains(value))) strLocationValues.Add(value);
                        }
                        //set values to return
                        if (strNameValues.Count > 0) DocInfo.Name = string.Join(" : ", strNameValues);
                        if (strLocationValues.Count > 0) DocInfo.Location = string.Join(" : ", strLocationValues);
                        
                       
                    }
                }
            }
            return DocInfo;
        }
       
        /// <summary>
        /// Get the related object information for the document
        /// </summary>
        /// <param name="ifcRelAssociatesDocument">IfcRelAssociatesDocument object</param>
        /// <returns>RelatedObjectInformation structure</returns>
        private RelatedObjectInformation GetRelatedObjectInformation(IfcRelAssociatesDocument ifcRelAssociatesDocument)
        {
            RelatedObjectInformation objectInfo = new RelatedObjectInformation { SheetName = DEFAULT_STRING, Name = DEFAULT_STRING, ExtIdentifier = DEFAULT_STRING, ExtObject = DEFAULT_STRING  };
            if (ifcRelAssociatesDocument != null)
            {
                IfcRoot relatedObject = ifcRelAssociatesDocument.RelatedObjects.FirstOrDefault();
                if (relatedObject != null)
                {
                    string value = GetSheetByObjectType(relatedObject);
                    
                    if (!string.IsNullOrEmpty(value)) objectInfo.SheetName = value;
                    value = relatedObject.Name.ToString();
                    if (!string.IsNullOrEmpty(value)) objectInfo.Name = value;
                    objectInfo.ExtObject = relatedObject.GetType().Name;
                    objectInfo.ExtIdentifier = relatedObject.GlobalId;
                    objectInfo.ExtSystem = GetExternalSystem(relatedObject);
                }
            }
            return objectInfo;
        }


        

        /// <summary>
        /// Missing Inverse method on  IfcDocumentInformation, need to be implemented on IfcDocumentInformation class
        /// </summary>
        /// <param name="ifcDocumentInformation">IfcDocumentInformation object</param>
        /// <returns>IEnumerable of IfcRelAssociatesDocument objects</returns>
        public  IEnumerable<IfcRelAssociatesDocument> DocumentInformationForObjects (IfcDocumentInformation ifcDocumentInformation )
        {
            return Model.InstancesWhere<IfcRelAssociatesDocument>(irad => irad.RelatingDocument == ifcDocumentInformation);
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

        public struct FileInformation
        {
            public string Name { get; set; }
            public string Location { get; set; }
        }

        public struct RelatedObjectInformation
        {
            public string SheetName { get; set; }
            public string Name { get; set; }
            public string ExtObject { get; set; }
            public string ExtIdentifier { get; set; }
            public string ExtSystem { get; set; }
        }
    }
}
