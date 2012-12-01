﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions.Transactions;
using Xbim.COBie.Rows;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ExternalReferenceResource;
using Xbim.Ifc.ProductExtension;
using Xbim.XbimExtensions;
using Xbim.Ifc.ApprovalResource;
using Xbim.Ifc.ConstructionMgmtDomain;

namespace Xbim.COBie.Serialisers.XbimSerialiser
{
    public class COBieXBimDocument : COBieXBim
    {


        #region Properties
        public IEnumerable<IfcTypeObject> IfcTypeObjects { get; private set; }
        public IEnumerable<IfcElement> IfcElements { get; private set; }
        #endregion

        public COBieXBimDocument(COBieXBimContext xBimContext)
            : base(xBimContext)
        {

        }

        #region Properties

        /// <summary>
        /// Add the IfcDocumentInformation to the Model object
        /// </summary>
        /// <param name="cOBieSheet">COBieSheet of COBieDocumentRow to read data from</param>
        public void SerialiseDocument(COBieSheet<COBieDocumentRow> cOBieSheet)
        {

            using (Transaction trans = Model.BeginTransaction("Add Document"))
            {
                try
                {
                    ProgressIndicator.ReportMessage("Starting Documents...");
                    ProgressIndicator.Initialise("Creating Documents", cOBieSheet.RowCount);
                    for (int i = 0; i < cOBieSheet.RowCount; i++)
                    {
                        ProgressIndicator.IncrementAndUpdate();
                        COBieDocumentRow row = cOBieSheet[i];
                        AddDocument(row);
                    }
                    ProgressIndicator.Finalise();
                    trans.Commit();
                }
                catch (Exception)
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        /// <summary>
        /// Add the data to the Document Information object
        /// </summary>
        /// <param name="row">COBieDocumentRow holding the data</param>
        private void AddDocument(COBieDocumentRow row)
        {
            IfcDocumentInformation ifcDocumentInformation = Model.New<IfcDocumentInformation>();
            IfcRelAssociatesDocument ifcRelAssociatesDocument = Model.New<IfcRelAssociatesDocument>();
            //Add Created By, Created On and ExtSystem to Owner History. 
            if ((ValidateString(row.CreatedBy)) && (Contacts.ContainsKey(row.CreatedBy)))
            {
                SetNewOwnerHistory(ifcRelAssociatesDocument, row.ExtSystem, Contacts[row.CreatedBy], row.CreatedOn);
                ifcDocumentInformation.DocumentOwner = Contacts[row.CreatedBy];
            }
            else
                SetNewOwnerHistory(ifcRelAssociatesDocument, row.ExtSystem, Model.DefaultOwningUser, row.CreatedOn);

            //using statement will set the Model.OwnerHistoryAddObject to IfcConstructionProductResource.OwnerHistory as OwnerHistoryAddObject is used upon any property changes, 
            //then swaps the original OwnerHistoryAddObject back in the dispose, so set any properties within the using statement
            using (COBieXBimEditScope context = new COBieXBimEditScope(Model, ifcRelAssociatesDocument.OwnerHistory))
            {
                //create relationship between Document and ifcObjects it relates too
                ifcRelAssociatesDocument.RelatingDocument = ifcDocumentInformation;
                //Add Name
                if (ValidateString(row.Name)) ifcDocumentInformation.Name = row.Name;

                //Add Category
                if (ValidateString(row.Category)) ifcDocumentInformation.Purpose = row.Category;

                //Add approved By
                if (ValidateString(row.ApprovalBy)) ifcDocumentInformation.IntendedUse = row.ApprovalBy;
                
                //Add Stage
                if (ValidateString(row.Stage)) ifcDocumentInformation.Scope = row.Stage;

                //Add GlobalId
                AddGlobalId(row.ExtIdentifier, ifcRelAssociatesDocument);

                //Add Object Relationship
                AddObjectRelationship(row, ifcRelAssociatesDocument);

                //Add Document reference
                AddDocumentReference(row, ifcDocumentInformation);
                
                //add Description
                if (ValidateString(row.Description)) ifcDocumentInformation.Description = row.Description;

                //Add Reference
                if (ValidateString(row.Reference)) ifcDocumentInformation.DocumentId = row.Reference;
               
            }
        }

        /// <summary>
        /// Add the document references to the IfcDocumentInformation object
        /// </summary>
        /// <param name="row">COBieDocumentRow holding row data</param>
        /// <param name="ifcDocumentInformation">IfcDocumentInformation object to add references too</param>
        private void AddDocumentReference(COBieDocumentRow row, IfcDocumentInformation ifcDocumentInformation)
        {
            if (ValidateString(row.File))
            {
                //get locations, assume we have the same number of locations and document names
                List<string> strLocationValues = null;
                if (ValidateString(row.Directory)) strLocationValues = SplitString(row.Directory, ':');
                List<string> strNameValues = SplitString(row.File, ':');
                //see if we have a location match to every document name
                if ((strLocationValues != null) && (strNameValues.Count != strLocationValues.Count))
                {
                    strLocationValues = null; //cannot match locations to document so just supply document names
                }
                //create the Document References
                if (strNameValues.Count > 0)
                {
                    IfcDocumentReference[] ifcDocumentReferences = new IfcDocumentReference[strNameValues.Count];
                    int i = 0;
                    foreach (string name in strNameValues)
                    {
                        ifcDocumentReferences[i] = Model.New<IfcDocumentReference>(dr => { dr.Name = name; });
                        if (strLocationValues != null)
                            ifcDocumentReferences[i].Location = strLocationValues[i];
                        i++;
                    }
                    ifcDocumentInformation.SetDocumentReferences(true, ifcDocumentReferences);
                }

            }
        }

        /// <summary>
        /// Add the object the document relates too
        /// </summary>
        /// <param name="row">COBieDocumentRow holding row data</param>
        /// <param name="ifcRelAssociatesDocument">IfcRelAssociatesDocument object to hold relationship</param>
        private void AddObjectRelationship(COBieDocumentRow row, IfcRelAssociatesDocument ifcRelAssociatesDocument)
        {
            if ((ValidateString(row.SheetName)) &&  (ValidateString(row.RowName)))
            {
                string rowName = row.RowName.ToLower().Trim();
                IfcRoot ifcRoot = null;
                switch (row.SheetName.ToLower().Trim())
                {
                    case "type":
                        //get all types, one time only
                        if (IfcTypeObjects == null)
                            IfcTypeObjects = Model.InstancesOfType<IfcTypeObject>();
                        ifcRoot = IfcTypeObjects.Where(to => to.Name.ToString().ToLower().Trim() == rowName).FirstOrDefault();
                        break;
                    case "component":
                        //get all types, one time only
                        if (IfcElements == null)
                            IfcElements = Model.InstancesOfType<IfcElement>();
                        ifcRoot = IfcElements.Where(to => to.Name.ToString().ToLower().Trim() == rowName).FirstOrDefault();
                        break;
                    case "job":
                        ifcRoot = Model.InstancesWhere<IfcProcess>(to => to.Name.ToString().ToLower().Trim() == rowName).FirstOrDefault();
                        break;
                    case "assembly":
                        ifcRoot = Model.InstancesWhere<IfcRelDecomposes>(to => to.Name.ToString().ToLower().Trim() == rowName).FirstOrDefault();
                        break;
                    case "connection":
                        ifcRoot = Model.InstancesWhere<IfcRelConnects>(to => to.Name.ToString().ToLower().Trim() == rowName).FirstOrDefault();
                        break;
                    case "facility":
                        ifcRoot = Model.InstancesWhere<IfcBuilding>(to => to.Name.ToString().ToLower().Trim() == rowName).FirstOrDefault();
                        if (ifcRoot == null)
                            ifcRoot = Model.InstancesWhere<IfcSite>(to => to.Name.ToString().ToLower().Trim() == rowName).FirstOrDefault();
                        if (ifcRoot == null)
                            ifcRoot = Model.InstancesWhere<IfcProject>(to => to.Name.ToString().ToLower().Trim() == rowName).FirstOrDefault();
                        break;
                    case "floor":
                        ifcRoot = Model.InstancesWhere<IfcBuildingStorey>(to => to.Name.ToString().ToLower().Trim() == rowName).FirstOrDefault();
                        break;
                    case "resources":
                        ifcRoot = Model.InstancesWhere<IfcConstructionEquipmentResource>(to => to.Name.ToString().ToLower().Trim() == rowName).FirstOrDefault();
                        break;
                    case "space":
                        ifcRoot = Model.InstancesWhere<IfcSpace>(to => to.Name.ToString().ToLower().Trim() == rowName).FirstOrDefault();
                        break;
                    case "spare":
                        ifcRoot = Model.InstancesWhere<IfcConstructionProductResource>(to => to.Name.ToString().ToLower().Trim() == rowName).FirstOrDefault();
                        break;
                    case "system":
                        ifcRoot = Model.InstancesWhere<IfcGroup>(to => to.Name.ToString().ToLower().Trim() == rowName).FirstOrDefault();
                        break;
                    case "zone":
                        ifcRoot = Model.InstancesWhere<IfcZone>(to => to.Name.ToString().ToLower().Trim() == rowName).FirstOrDefault();
                        break;
                    //case "document": //not derived from IfcRoot
                    //    ifcRoot = Model.InstancesWhere<IfcDocumentInformation>(to => to.Name.ToString().ToLower().Trim() == rowName).FirstOrDefault();
                    //    break;
                    //case "contact": //not derived from IfcRoot
                    //    ifcRoot = Model.InstancesWhere<IfcPersonAndOrganization>(to => to.Name.ToString().ToLower().Trim() == rowName).FirstOrDefault();
                    //    break;
                    //case "issue": //not derived from IfcRoot
                    //    ifcRoot = Model.InstancesWhere<IfcApproval>(to => to.Name.ToString().ToLower().Trim() == rowName).FirstOrDefault();
                    //    break;
                    default:
                        break;
                }

                //add to the document relationship object
                if (ifcRoot != null)
                    ifcRelAssociatesDocument.RelatedObjects.Add_Reversible(ifcRoot);

            }
        }
        
        #endregion
    }
}