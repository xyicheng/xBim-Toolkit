using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using Xbim.COBie.Rows;
using Xbim.XbimExtensions;
using System.Linq;
using System.Data.SQLite;

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

		/// <summary>
		/// Gets errors found for the cell on the sheet
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="sheetName"></param>
		/// <returns></returns>
        public COBieError GetCobieError(COBieCell cell, string sheetName)
        {
            int maxLength = cell.CobieCol.ColumnLength;
            COBieAllowedType allowedType = cell.CobieCol.AllowedType;
            COBieError err = new COBieError(sheetName, cell.CobieCol.ColumnName, "", COBieError.ErrorTypes.None);
            if (cell.CellValue.Length > maxLength)
            {
                err.ErrorDescription = "Value must be under " + maxLength + " characters";
                err.ErrorType = COBieError.ErrorTypes.Value_Out_of_Bounds;
            }
            if (allowedType == COBieAllowedType.AlphaNumeric && !COBieCell.RegExAlphaNumeric.IsMatch(cell.CellValue))
            {
                err.ErrorDescription = "Value must be alpha-numeric";
                err.ErrorType = COBieError.ErrorTypes.AlphaNumeric_Value_Expected;
            }
            if (allowedType == COBieAllowedType.Email && !COBieCell.RegExEmail.IsMatch(cell.CellValue))
            {
                err.ErrorDescription = "Value must be a valid email address";
                err.ErrorType = COBieError.ErrorTypes.Email_Value_Expected;
            }
            
            DateTime dt;
            DateTime.TryParse(cell.CellValue, out dt);
            if (allowedType == COBieAllowedType.ISODate && dt == DateTime.MinValue) err.ErrorDescription = "Value must be a valid iso date";

            double d;
            double.TryParse(cell.CellValue, out d);
            if (allowedType == COBieAllowedType.Numeric && d == 0) err.ErrorDescription = "Value must be a valid double";

            return err;
        }
        
		private void Intialise()
        {
			if (Context == null) { throw new InvalidOperationException("COBieReader can't initialise without a valid Context."); }
			if (Context.Models == null || Context.Models.Count == 0) { throw new ArgumentException("COBieReader context must contain one or more models."); }

			IModel model = Context.Models.First();

            // set all the properties
            COBieQueries cq = new COBieQueries(model);

            // create pick lists from xml
            //CobiePickLists = cq.GetCOBiePickListsSheet("PickLists.xml");

            // populate all sheets from model
            CobieSpaces = cq.GetCOBieSpaceSheet();
            CobieComponents = cq.GetCOBieComponentSheet();
            CobieAssemblies = cq.GetCOBieAssemblySheet();
            CobieConnections = cq.GetCOBieConnectionSheet();
            CobieContacts = cq.GetCOBieContactSheet();
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


            CreateCOBieDB();
        }

        private void PopulateErrors()
        {
            try
            {
                CobieErrors = new List<COBieError>();

                List<COBieError> errors;
                CobieAssemblies.Validate(out errors);
                CobieAttributes.Validate(out errors);
                CobieComponents.Validate(out errors);
                CobieConnections.Validate(out errors);
                CobieContacts.Validate(out errors);
                CobieCoordinates.Validate(out errors);
                CobieDocuments.Validate(out errors);
                CobieFacilities.Validate(out errors);
                CobieFloors.Validate( out errors);
                CobieImpacts.Validate(out errors);
                CobieIssues.Validate(out errors);
                CobieJobs.Validate(out errors);
                CobiePickLists.Validate(out errors);
                CobieResources.Validate(out errors);
                CobieSpaces.Validate(out errors);
                CobieSpares.Validate(out errors);
                CobieSystems.Validate(out errors);
                CobieTypes.Validate(out errors);
                CobieZones.Validate(out errors);                
            }
            catch (Exception)
            {
                // TODO: Handle
                throw;
            }
        }

        private bool HasDuplicateFloorValues(COBieSheet<COBieFloorRow> sheet, string val)
        {
            int count = 0;
            foreach (COBieFloorRow row in CobieFloors.Rows)
            {
                if (row.Name == val) count++; 
            }
            if (count > 1) return true;

            return false;
        }

        public void GenerateCOBieData()
        {
            Intialise();

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

        public DataTable ToDataTable(object[] objectArray, string tableName)
        {
            if (objectArray == null || objectArray.Length <= 0) return null;

            // we are here means we have data to convert to dataset
            DataSet ds = new DataSet();
            
            XmlSerializer xmlSerializer = new XmlSerializer(objectArray.GetType());
            StringWriter writer = new StringWriter();
            xmlSerializer.Serialize(writer, objectArray);
            StringReader reader = new StringReader(writer.ToString());
            writer.Close();

            ds.ReadXml(reader);
            ds.Tables[0].TableName = tableName;
            
            return ds.Tables[0];
        }

        //public string ToXML(object[] objectArray, string tableName, XmlTextWriter textWriter)
        //{
        //    string str = "";
                                 
        //    textWriter.WriteStartDocument();

        //    // start of sheet
        //    textWriter.WriteStartElement(tableName); // start node for sheet name
            
        //    // write atributes and nodes
        //    foreach (COBieAssemblyRow ca in objectArray) // each array item is a row
        //    {                
        //        textWriter.WriteAttributeString("pk", ca.Name.IsPrimaryKey.ToString());
        //        //textWriter.WriteAttributeString("attr", ca.);
        //        textWriter.WriteAttributeString("maxLen", "255");
        //        textWriter.WriteAttributeString("attrType", "AlphaNumeric");
        //        textWriter.WriteString(ca.Name.CellValue);
        //    }            
            
        //    // end of sheet
        //    textWriter.WriteEndElement();

        //    textWriter.WriteEndDocument();
        //    textWriter.Close();  

            

        //    return str;
        //}

        #region Extract data form IModel

       
        #endregion

        // create SQLite DB with all data
        private void CreateCOBieDB()
        {
            string dbName = "COBieDB.db";
            string connectionString = "Data Source=" + dbName + ";Version=3;New=False;Compress=True;";

            // cretae database
            SQLiteConnection.CreateFile(dbName);
                        
            // create tables
            CobieContacts.CreateEmptyTable(dbName, "CobieContacts", connectionString);
            CobieAssemblies.CreateEmptyTable(dbName, "CobieAssemblies", connectionString);
            CobieComponents.CreateEmptyTable(dbName, "CobieComponents", connectionString);
            CobieConnections.CreateEmptyTable(dbName, "CobieConnections", connectionString);
            CobieCoordinates.CreateEmptyTable(dbName, "CobieCoordinates", connectionString);
            CobieDocuments.CreateEmptyTable(dbName, "CobieDocuments", connectionString);
            CobieFacilities.CreateEmptyTable(dbName, "CobieFacilities", connectionString);
            CobieFloors.CreateEmptyTable(dbName, "CobieFloors", connectionString);
            CobieImpacts.CreateEmptyTable(dbName, "CobieImpacts", connectionString);
            CobieIssues.CreateEmptyTable(dbName, "CobieIssues", connectionString);
            CobieJobs.CreateEmptyTable(dbName, "CobieJobs", connectionString);
            CobiePickLists.CreateEmptyTable(dbName, "CobiePickLists", connectionString);
            CobieResources.CreateEmptyTable(dbName, "CobieResources", connectionString);
            CobieSpaces.CreateEmptyTable(dbName, "CobieSpaces", connectionString);
            CobieSpares.CreateEmptyTable(dbName, "CobieSpares", connectionString);
            CobieSystems.CreateEmptyTable(dbName, "CobieSystems", connectionString);
            CobieTypes.CreateEmptyTable(dbName, "CobieTypes", connectionString);
            CobieZones.CreateEmptyTable(dbName, "CobieZones", connectionString);
            CobieAttributes.CreateEmptyTable(dbName, "CobieAttributes", connectionString);

            // insert values
            CobieContacts.InsertValuesInDB(dbName, "CobieContacts", connectionString);
            CobieAssemblies.InsertValuesInDB(dbName, "CobieAssemblies", connectionString);
            CobieComponents.InsertValuesInDB(dbName, "CobieComponents", connectionString);
            CobieConnections.InsertValuesInDB(dbName, "CobieConnections", connectionString);
            CobieCoordinates.InsertValuesInDB(dbName, "CobieCoordinates", connectionString);
            CobieDocuments.InsertValuesInDB(dbName, "CobieDocuments", connectionString);
            CobieFacilities.InsertValuesInDB(dbName, "CobieFacilities", connectionString);
            CobieFloors.InsertValuesInDB(dbName, "CobieFloors", connectionString);
            CobieImpacts.InsertValuesInDB(dbName, "CobieImpacts", connectionString);
            CobieIssues.InsertValuesInDB(dbName, "CobieIssues", connectionString);
            CobieJobs.InsertValuesInDB(dbName, "CobieJobs", connectionString);
            CobiePickLists.InsertValuesInDB(dbName, "CobiePickLists", connectionString);
            CobieResources.InsertValuesInDB(dbName, "CobieResources", connectionString);
            CobieSpaces.InsertValuesInDB(dbName, "CobieSpaces", connectionString);
            CobieSpares.InsertValuesInDB(dbName, "CobieSpares", connectionString);
            CobieSystems.InsertValuesInDB(dbName, "CobieSystems", connectionString);
            CobieTypes.InsertValuesInDB(dbName, "CobieTypes", connectionString);
            CobieZones.InsertValuesInDB(dbName, "CobieZones", connectionString);
            CobieAttributes.InsertValuesInDB(dbName, "CobieAttributes", connectionString);
            
        }

        private void ExecuteQuery(string txtQuery, string connectionString)
        {
            SQLiteConnection cn;
            using (cn = new SQLiteConnection(connectionString))
            {
                using (SQLiteCommand cmd = cn.CreateCommand())
                {
                    cmd.CommandText = txtQuery;
                    cmd.CommandType = CommandType.Text;
                    cn.Open();
                    cmd.ExecuteNonQuery();
                    cn.Close();
                }
            }
        }
    }
}
