using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Xbim.COBie.Rows;
using Xbim.XbimExtensions;
using System.Linq;
using System.Reflection;



namespace Xbim.COBie
{ 
	/// <summary>
	/// Interrogates IFC models and builds COBie-format objects from the models
	/// </summary>
    public class COBieReader
    {
		/// <summary>
		/// Default constructor
		/// </summary>
		public COBieReader()
		{
			ResetWorksheets();
		}

		/// <summary>
		/// Constructor which also sets the Context
		/// </summary>
		/// <param name="context"></param>
		public COBieReader(COBieContext context) : this()
		{
			Context = context;
			GenerateCOBieData();
		}

        public COBieWorkbook Workbook { get; set; }

		/// <summary>
		/// The context
		/// </summary>
		public COBieContext Context { get; set; }

		// Worksheets

		/// <summary>
		/// Contacts
		/// </summary>
		public COBieSheet<COBieContactRow> CobieContacts { get; set; }

		/// <summary>
		/// Assemblies
		/// </summary>
		public COBieSheet<COBieAssemblyRow> CobieAssemblies { get; set; }

		/// <summary>
		/// Components
		/// </summary>
		public COBieSheet<COBieComponentRow> CobieComponents { get; set; }

		/// <summary>
		/// Connections
		/// </summary>
		public COBieSheet<COBieConnectionRow> CobieConnections { get; set; }

		/// <summary>
		/// Coordinates
		/// </summary>
		public COBieSheet<COBieCoordinateRow> CobieCoordinates { get; set; }

		/// <summary>
		/// Documents
		/// </summary>
		public COBieSheet<COBieDocumentRow> CobieDocuments { get; set; }

		/// <summary>
		/// Facilities
		/// </summary>
		public COBieSheet<COBieFacilityRow> CobieFacilities { get; set; }

		/// <summary>
		/// Floors
		/// </summary>
		public COBieSheet<COBieFloorRow> CobieFloors { get; set; }

		/// <summary>
		/// Impacts
		/// </summary>
		public COBieSheet<COBieImpactRow> CobieImpacts { get; set; }

		/// <summary>
		/// Issues
		/// </summary>
		public COBieSheet<COBieIssueRow> CobieIssues { get; set; }

		/// <summary>
		/// Jobs
		/// </summary>
		public COBieSheet<COBieJobRow> CobieJobs { get; set; }

		/// <summary>
		/// PickLists
		/// </summary>
		public COBieSheet<COBiePickListsRow> CobiePickLists { get; set; }

		/// <summary>
		/// Resources
		/// </summary>
		public COBieSheet<COBieResourceRow> CobieResources { get; set; }

		/// <summary>
		/// Spaces
		/// </summary>
		public COBieSheet<COBieSpaceRow> CobieSpaces { get; set; }

		/// <summary>
		/// Spares
		/// </summary>
		public COBieSheet<COBieSpareRow> CobieSpares { get; set; }

		/// <summary>
		/// Systems
		/// </summary>
		public COBieSheet<COBieSystemRow> CobieSystems { get; set; }

		/// <summary>
		/// Types
		/// </summary>
		public COBieSheet<COBieTypeRow> CobieTypes { get; set; }

		/// <summary>
		/// Zones
		/// </summary>
		public COBieSheet<COBieZoneRow> CobieZones { get; set; }

		/// <summary>
		/// Attributes
		/// </summary>
		public COBieSheet<COBieAttributeRow> CobieAttributes { get; set; }

       
		/// <summary>
		/// Errors
		/// </summary>
		public List<COBieError> CobieErrors { get; set; }

		/// <summary>
		/// Adds an error to the errors collection
		/// </summary>
		/// <param name="cobieError"></param>
        public void AddCOBieError(COBieError cobieError)
        {
            CobieErrors.Add(cobieError);
        }

		private void ResetWorksheets()
		{
			CobieContacts = new COBieSheet<COBieContactRow>(Constants.WORKSHEET_CONTACT);
			CobieAssemblies = new COBieSheet<COBieAssemblyRow>(Constants.WORKSHEET_ASSEMBLY);
			CobieComponents = new COBieSheet<COBieComponentRow>(Constants.WORKSHEET_COMPONENT);
			CobieConnections = new COBieSheet<COBieConnectionRow>(Constants.WORKSHEET_CONNECTION);
			CobieCoordinates = new COBieSheet<COBieCoordinateRow>(Constants.WORKSHEET_COORDINATE);
			CobieDocuments = new COBieSheet<COBieDocumentRow>(Constants.WORKSHEET_DOCUMENT);
			CobieFacilities = new COBieSheet<COBieFacilityRow>(Constants.WORKSHEET_FACILITY);
			CobieFloors = new COBieSheet<COBieFloorRow>(Constants.WORKSHEET_FLOOR);
			CobieImpacts = new COBieSheet<COBieImpactRow>(Constants.WORKSHEET_IMPACT);
			CobieIssues = new COBieSheet<COBieIssueRow>(Constants.WORKSHEET_ISSUE);
			CobieJobs = new COBieSheet<COBieJobRow>(Constants.WORKSHEET_JOB);
			CobiePickLists = new COBieSheet<COBiePickListsRow>(Constants.WORKSHEET_PICKLISTS);
			CobieResources = new COBieSheet<COBieResourceRow>(Constants.WORKSHEET_RESOURCE);
			CobieSpaces = new COBieSheet<COBieSpaceRow>(Constants.WORKSHEET_SPACE);
			CobieSpares = new COBieSheet<COBieSpareRow>(Constants.WORKSHEET_SPARE);
			CobieSystems = new COBieSheet<COBieSystemRow>(Constants.WORKSHEET_SYSTEM);
			CobieTypes = new COBieSheet<COBieTypeRow>(Constants.WORKSHEET_TYPE);
			CobieZones = new COBieSheet<COBieZoneRow>(Constants.WORKSHEET_ZONE);
			CobieAttributes = new COBieSheet<COBieAttributeRow>(Constants.WORKSHEET_ATTRIBUTE);

			CobieErrors = new List<COBieError>();

            
		}

        
		private void Initialise()
        {
			if (Context == null) { throw new InvalidOperationException("COBieReader can't initialise without a valid Context."); }
			if (Context.Models == null || Context.Models.Count == 0) { throw new ArgumentException("COBieReader context must contain one or more models."); }

			IModel model = Context.Models.First();

            // set all the properties
            COBieQueries cq = new COBieQueries(model);

            // create pick lists from xml
            // TODO: Need to populate somehow.
            //CobiePickLists = cq.GetCOBiePickListsSheet("PickLists.xml");

            //contact sheet first as it will fill contact information lookups for other sheets
            CobieContacts = cq.GetCOBieContactSheet();

            CobieSpaces = cq.GetCOBieSpaceSheet();
            CobieComponents = cq.GetCOBieComponentSheet();
            CobieAssemblies = cq.GetCOBieAssemblySheet();
            CobieConnections = cq.GetCOBieConnectionSheet();
            CobieCoordinates = cq.GetCOBieCoordinateSheet();
            CobieDocuments = cq.GetCOBieDocumentSheet();
            CobieFacilities = cq.GetCOBieFacilitySheet();
            CobieFloors = cq.GetCOBieFloorSheet();
            CobieImpacts = cq.GetCOBieImpactSheet();
            CobieIssues = cq.GetCOBieIssueSheet();
            CobieJobs = cq.GetCOBieJobSheet();            
            CobieResources = cq.GetCOBieResourceSheet();
            CobieSpares = cq.GetCOBieSpareSheet();
            CobieSystems = cq.GetCOBieSystemSheet();
            CobieTypes = cq.GetCOBieTypeSheet();
            CobieZones = cq.GetCOBieZoneSheet();
            //we need to fill this one last as the calls to the above sheet add data for the AttributeSheet
            CobieAttributes = cq.GetCOBieAttributeSheet();


            // add to workbook and use workbook for error checking later
            Workbook = new COBieWorkbook();
            Workbook.Add(CobieContacts);
            Workbook.Add(CobieAssemblies);
            Workbook.Add(CobieComponents);
            Workbook.Add(CobieConnections);
            Workbook.Add(CobieCoordinates);
            Workbook.Add(CobieDocuments);
            Workbook.Add(CobieFacilities);
            Workbook.Add(CobieFloors);
            Workbook.Add(CobieImpacts);
            Workbook.Add(CobieIssues);
            Workbook.Add(CobieJobs);
            Workbook.Add(CobiePickLists);
            Workbook.Add(CobieResources);
            Workbook.Add(CobieSpaces);
            Workbook.Add(CobieSpares);
            Workbook.Add(CobieSystems);
            Workbook.Add(CobieTypes);
            Workbook.Add(CobieZones);
            Workbook.Add(CobieAttributes);

            

        }

        private void PopulateErrors()
        {
            try
            {                  
                // populate general errors
                COBieErrorCollection errorCollection = new COBieErrorCollection();
                for (int i = 0; i < Workbook.Count; i++)
                {
                    Type type = Workbook[i].GetType();
                    MethodInfo methodInfo = type.GetMethod("Validate");
                    var result = methodInfo.Invoke(Workbook[i], null);

                    if (result != null)
                    {
                        COBieErrorCollection errorCol = (COBieErrorCollection)result;
                        foreach (COBieError err in errorCol)
                            errorCollection.Add(err);
                    }
                }

                // populate primary key errors
                COBieErrorCollection errorPKCollection = new COBieErrorCollection();
                for (int i = 0; i < Workbook.Count; i++)
                {
                    Type type = Workbook[i].GetType();
                    MethodInfo methodInfo = type.GetMethod("ValidatePrimaryKey");
                    var result = methodInfo.Invoke(Workbook[i], null);

                    if (result != null)
                    {
                        COBieErrorCollection errorCol = (COBieErrorCollection)result;
                        foreach (COBieError err in errorCol)
                            errorPKCollection.Add(err);
                    }
                }

                // populate foreign key errors
                //COBieErrorCollection errorFKCollection = new COBieErrorCollection();
                //for (int i = 0; i < Workbook.Count; i++)
                //{
                //    Type type = Workbook[i].GetType();
                //    MethodInfo methodInfo = type.GetMethod("ValidateForeignKey");
                //    var result = methodInfo.Invoke(Workbook[i], null);
                                        
                //    // result will have all the rows for a sheet
                //    if (result != null)
                //    {
                //        COBieErrorCollection errorCol = (COBieErrorCollection)result;
                //        foreach (COBieError err in errorCol)
                //            errorFKCollection.Add(err);

                //        // pass all the rows and check the column with atribute type foreignkey
                //        ValidateForeignKey((List<COBieRow>)result);

                //    }
                //}          

                COBieErrorCollection errorFKCollection = new COBieErrorCollection();
                for (int i = 0; i < Workbook.Count; i++)
                {
                    List<PropertyInfo> foreignKeyColumns = Workbook[i].ForeignKeyColumns;

                    if (foreignKeyColumns != null && foreignKeyColumns.Count > 0)
                    {
                        foreach (PropertyInfo propInfo in foreignKeyColumns)
                        {
                            object[] attrs = propInfo.GetCustomAttributes(typeof(COBieAttributes), true);
                            if (attrs != null && attrs.Length > 0)
                            {
                                COBieAttributes attr = (COBieAttributes)attrs[0];
                                // we know its a foreign key column, get what sheet and column it is refering to
                                string sheetCol = attr.ReferenceColumnName;
                                int index = sheetCol.IndexOf('.');
                                string foreignSheetName = sheetCol.Substring(0, index);
                                string foreignColName = sheetCol.Substring(index + 1, sheetCol.Length - (index + 1));

                                int foreignSheetIndex = GetCOBieSheetIndexBySheetName(foreignSheetName);

                                // now we have the foreignKey Column, get that workbook sheet  
                                // Workbook[i] = the one we are checking now
                                // Workbook[foreignSheetIndex] = sheet with foreign key column

                                // get foreignkey column values from one worksheet and check if they exist in other worksheet
                                Type type = Workbook[i].GetType();
                                MethodInfo methodInfo = type.GetMethod("GetForeignKeyValues");
                                object[] param = {attr.ColumnName};
                                var result = methodInfo.Invoke(Workbook[i], param);

                                List<string> colMain = new List<string>();
                                if (result != null)
                                    colMain = (List<string>)result;


                                Type typeF = Workbook[foreignSheetIndex].GetType();
                                MethodInfo methodInfoF = typeF.GetMethod("GetForeignKeyValues");
                                object[] paramF = { foreignColName };
                                var resultF = methodInfoF.Invoke(Workbook[foreignSheetIndex], paramF);

                                List<string> colForeign = new List<string>();
                                if (resultF != null)
                                    colForeign = (List<string>)resultF;

                                // send the 2 lists to check foreign key constraint
                                MethodInfo methodInfo3 = type.GetMethod("ValidateForeignKey");
                                object[] param3 = { colMain, colForeign };
                                var result3 = methodInfo3.Invoke(Workbook[i], param3);

                                if (result3 != null)
                                {
                                    COBieErrorCollection errorCol = (COBieErrorCollection)result3;
                                    foreach (COBieError err in errorCol)
                                        errorFKCollection.Add(err);
                                }
                            }
                        }
                    }
                    
                    
                }
                
            }
            catch (Exception)
            {
                // TODO: Handle
                throw;
            }
        }

        private int GetCOBieSheetIndexBySheetName(string sheetName)
        {
            for (int i = 0; i < Workbook.Count; i++)
            {
                if (sheetName == Workbook[i].SheetName)
                    return i;
            }
            return -1;
        }

        public COBieErrorCollection ValidateForeignKey(List<COBieRow> Rows)
        {
            // E.g.
            // SELECT Facility.CreatedBy, Contact.Email FROM Contact
            // Left Outer Join Facility On Contact.Email = Facility.CreatedBy 
            // WHERE Facility.CreatedBy = null 

            

            return null;
        }

        public void GenerateCOBieData()
        {
            Initialise();
           
            PopulateErrors();			
        }

		/// <summary>
		/// Passes this instance of the COBieReader into the provided ICOBieFormatter
		/// </summary>
		/// <param name="formatter">The object implementing the ICOBieFormatter interface.</param>
		public void Export(ICOBieFormatter formatter)
		{
			if (formatter == null) { throw new ArgumentNullException("formatter", "Parameter passed to COBieReader.Export(ICOBieFormatter) must not be null."); }

			// Passes this 
			formatter.Format(this);
		}


        
    }
}
