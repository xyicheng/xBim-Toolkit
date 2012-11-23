using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xbim.COBie.Contracts;
using Xbim.COBie.Rows;
using Xbim.COBie.Serialisers.XbimSerialiser;
using Xbim.Ifc.Extensions;
using Xbim.Ifc.Kernel;
using Xbim.IO;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Transactions;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.UtilityResource;
using Xbim.XbimExtensions.Parser;

namespace Xbim.COBie.Serialisers
{
    public class COBieXBimSerialiser : ICOBieSerialiser , IDisposable
    {
        #region Fileds
        private Transaction _transaction;
        public COBieXBimContext XBimContext { get; private set; }
        #endregion

        #region Properties
        /// <summary>
        /// COBieWorkbook to convert to XBim Model Object
        /// </summary>
        public COBieWorkbook WorkBook 
        {
            get { return XBimContext.WorkBook; }
        }
        
        /// <summary>
        /// XBim Model Object
        /// </summary>
        public IModel Model
        {
            get { return XBimContext.Model; }
        }
        #endregion
        
        /// <summary>
        /// Constructor
        /// </summary>
        public COBieXBimSerialiser()
        {
            XBimContext = new COBieXBimContext(new XbimMemoryModel());
            _transaction = Model.BeginTransaction("COBieXBimSerialiser transaction");
           
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public COBieXBimSerialiser(ReportProgressDelegate progressHandler) 
        {
            XBimContext = new COBieXBimContext(new XbimMemoryModel(), progressHandler);
            _transaction = Model.BeginTransaction("COBieXBimSerialiser transaction");

        }


        #region Methods
        /// <summary>
        /// XBim Serialise
        /// </summary>
        /// <param name="workbook">COBieWorkbook to Serialise</param>
        public void Serialise(COBieWorkbook workbook)
        {
            
            XBimContext.Reset(); //clear out the dictionaries
            XBimContext.WorkBook = workbook;
            ModelSetUp();

            COBieXBimContact xBimContact = new COBieXBimContact(XBimContext);
            //xBimContact.SetDefaultUser(); //needed to avoid an extra IfcPersonAndOrganization in contacts list
            xBimContact.SerialiseContacts((COBieSheet<COBieContactRow>)WorkBook[Constants.WORKSHEET_CONTACT]);

            COBieXBimFacility xBimFacility = new COBieXBimFacility(XBimContext);
            xBimFacility.SerialiseFacility((COBieSheet<COBieFacilityRow>)WorkBook[Constants.WORKSHEET_FACILITY]);

            COBieXBimFloor xBimFloor = new COBieXBimFloor(XBimContext);
            xBimFloor.SerialiseFloor((COBieSheet<COBieFloorRow>)WorkBook[Constants.WORKSHEET_FLOOR]);
            
            COBieXBimSpace xBimSpace = new COBieXBimSpace(XBimContext);
            xBimSpace.SerialiseSpace((COBieSheet<COBieSpaceRow>)WorkBook[Constants.WORKSHEET_SPACE]);

            COBieXBimZone xBimZone = new COBieXBimZone(XBimContext);
            xBimZone.SerialiseZone((COBieSheet<COBieZoneRow>)WorkBook[Constants.WORKSHEET_ZONE]);

            COBieXBimType xBimType = new COBieXBimType(XBimContext);
            xBimType.SerialiseType((COBieSheet<COBieTypeRow>)WorkBook[Constants.WORKSHEET_TYPE]);

            COBieXBimComponent xBimComponent = new COBieXBimComponent(XBimContext);
            xBimComponent.SerialiseComponent((COBieSheet<COBieComponentRow>)WorkBook[Constants.WORKSHEET_COMPONENT]);

            COBieXBimSystem xBimSystem = new COBieXBimSystem(XBimContext);
            xBimSystem.SerialiseSystem((COBieSheet<COBieSystemRow>)WorkBook[Constants.WORKSHEET_SYSTEM]);

            COBieXBimAssembly xBimAssembly = new COBieXBimAssembly(XBimContext);
            xBimAssembly.SerialiseAssembly((COBieSheet<COBieAssemblyRow>)WorkBook[Constants.WORKSHEET_ASSEMBLY]);

            COBieXBimConnection xBimConnection = new COBieXBimConnection(XBimContext);
            xBimConnection.SerialiseConnection((COBieSheet<COBieConnectionRow>)WorkBook[Constants.WORKSHEET_CONNECTION]);
            
            COBieXBimSpare xBimSpare = new COBieXBimSpare(XBimContext);
            xBimSpare.SerialiseSpare((COBieSheet<COBieSpareRow>)WorkBook[Constants.WORKSHEET_SPARE]);

            COBieXBimResource xBimResource = new COBieXBimResource(XBimContext);
            xBimResource.SerialiseResource((COBieSheet<COBieResourceRow>)WorkBook[Constants.WORKSHEET_RESOURCE]);

            COBieXBimJob xBimJob = new COBieXBimJob(XBimContext);
            xBimJob.SerialiseJob((COBieSheet<COBieJobRow>)WorkBook[Constants.WORKSHEET_JOB]);

            COBieXBimImpact xBimImpact = new COBieXBimImpact(XBimContext);
            xBimImpact.SerialiseImpact((COBieSheet<COBieImpactRow>)WorkBook[Constants.WORKSHEET_IMPACT]);

            COBieXBimDocument xBimDocument = new COBieXBimDocument(XBimContext);
            xBimDocument.SerialiseDocument((COBieSheet<COBieDocumentRow>)WorkBook[Constants.WORKSHEET_DOCUMENT]);

            COBieXBimAttribute xBimAttribute = new COBieXBimAttribute(XBimContext);
            xBimAttribute.SerialiseAttribute((COBieSheet<COBieAttributeRow>)WorkBook[Constants.WORKSHEET_ATTRIBUTE]);
            
            COBieXBimCoordinate xBimCoordinate = new COBieXBimCoordinate(XBimContext);
            xBimCoordinate.SerialiseCoordinate((COBieSheet<COBieCoordinateRow>)WorkBook[Constants.WORKSHEET_COORDINATE]);
            
            COBieXBimIssue xBimIssue = new COBieXBimIssue(XBimContext);
            xBimIssue.SerialiseIssue((COBieSheet<COBieIssueRow>)WorkBook[Constants.WORKSHEET_ISSUE]);
            
            
            _transaction.Commit();
            
        }

        /// <summary>
        /// Set up the Model Object
        /// </summary>
        private void ModelSetUp()
        {
            using (Transaction trans = Model.BeginTransaction("Model initialization"))
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
                string version = fvi.ProductVersion;

                Model.DefaultOwningApplication.ApplicationIdentifier = fvi.Comments;
                Model.DefaultOwningApplication.ApplicationDeveloper.Name = fvi.CompanyName;
                Model.DefaultOwningApplication.ApplicationFullName = fvi.ProductName;
                Model.DefaultOwningApplication.Version = fvi.ProductVersion;
                //TODO add correct IfcPersonAndOrganization to DefaultOwningUser
                Model.DefaultOwningUser.ThePerson.FamilyName = "Unknown";
                Model.DefaultOwningUser.TheOrganization.Name = "Unknown";
                Model.Header.FileDescription.Description.Clear();
                Model.Header.FileDescription.Description.Add("ViewDefinition[CoordinationView]");
                Model.Header.FileName.AuthorName.Add("4Projects");
                Model.Header.FileName.AuthorizationName = "4Projects";
                IfcProject project = Model.New<IfcProject>();
                //set world coordinate system
                XBimContext.WCS.SetNewDirectionOf_XZ(1, 0, 0, 0, 0, 1);
                XBimContext.WCS.SetNewLocation(0, 0, 0);
                trans.Commit();
            }
        }

        /// <summary>
        /// Dispose of the Model Object and close transaction
        /// </summary>
        void IDisposable.Dispose()
        {
            if (Model != null)
            {
                //commit transaction of the xBim document
                if (_transaction != null)
                    _transaction.Commit();

                //close model server if it is the case
                if (Model is XbimModelServer)
                {
                    (Model as XbimModelServer).Dispose();
                    XBimContext.Model = null;
                }

                if (Model is XbimMemoryModel) 
                    XBimContext.Model = null;

            }
        }

        /// <summary>
        /// Validate Model Object Foe Errors
        /// </summary>
        /// <param name="vf"></param>
        /// <returns></returns>
        public String Validate(ValidationFlags vf)
        {
            StringWriter sw = new StringWriter();
            int errors = Model.Validate(sw, null);
            if (errors > 0)
                return sw.ToString();
            else
                return null;
        }

        /// <summary>
        /// Save Model Object To A File
        /// </summary>
        /// <param name="fileName"></param>
        public void Save(string fileName)
        {
            //string error = Validate(ValidationFlags.All);
            Model.Header.FileName.Name = Path.GetFileName(fileName);
            IfcOutputStream oStream = new Xbim.IO.IfcOutputStream(new StreamWriter(fileName));
            oStream.Store(Model);
        }
        #endregion
        
    }

    
}
