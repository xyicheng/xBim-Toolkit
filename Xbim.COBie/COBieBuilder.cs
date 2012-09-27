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
    public class COBieBuilder
    {

		private COBieBuilder()
		{
			ResetWorksheets();
		}

		/// <summary>
		/// Constructor which also sets the Context
		/// </summary>
		/// <param name="context"></param>
		public COBieBuilder(COBieContext context) : this()
		{
            Context = context;
            GenerateCOBieData();
		}

        /// <summary>
        /// The context in which this COBie data is being built
        /// </summary>
        /// <remarks>Contains the source models, templates, environmental data and other parameters</remarks>
        public COBieContext Context { get; private set; }

        /// <summary>
        /// The set of COBie worksheets
        /// </summary>
        public COBieWorkbook Workbook { get; private set; }

		private void ResetWorksheets()
		{
            Workbook = new COBieWorkbook();
		}

        
		private void Initialise()
        {
			if (Context == null) { throw new InvalidOperationException("COBieReader can't initialise without a valid Context."); }
			if (Context.Models == null || Context.Models.Count == 0) { throw new ArgumentException("COBieReader context must contain one or more models."); }


            // set all the properties
            COBieQueries cq = new COBieQueries(Context);

            // create pick lists from xml
            // TODO: Need to populate somehow.
            //CobiePickLists = cq.GetCOBiePickListsSheet("PickLists.xml");


            // add to workbook and use workbook for error checking later

            //contact sheet first as it will fill contact information lookups for other sheets
            Workbook.Add(cq.GetCOBieContactSheet());
            Workbook.Add(cq.GetCOBieFacilitySheet()); //moved so it is called earlier as it now sets some global unit values used by other sheets
            Workbook.Add(cq.GetCOBieZoneSheet()); //we need zone before sheet as it sets a flag on departments property
            Workbook.Add(cq.GetCOBieSpaceSheet());
            Workbook.Add(cq.GetCOBieComponentSheet());
            Workbook.Add(cq.GetCOBieAssemblySheet());
            Workbook.Add(cq.GetCOBieConnectionSheet());
            Workbook.Add(cq.GetCOBieCoordinateSheet());
            Workbook.Add(cq.GetCOBieDocumentSheet());
             Workbook.Add(cq.GetCOBieFloorSheet());
            Workbook.Add(cq.GetCOBieImpactSheet());
            Workbook.Add(cq.GetCOBieIssueSheet());
            Workbook.Add(cq.GetCOBieJobSheet());            
            Workbook.Add(cq.GetCOBieResourceSheet());
            Workbook.Add(cq.GetCOBieSpareSheet());
            Workbook.Add(cq.GetCOBieSystemSheet());
            Workbook.Add(cq.GetCOBieTypeSheet());
            //we need to fill attributes last as it is populated by Components, Types etc
            Workbook.Add(cq.GetCOBieAttributeSheet());

            Workbook.Add(new COBieSheet<COBiePickListsRow>(Constants.WORKSHEET_PICKLISTS));

        }

        private void PopulateErrors()
        {
            try
            {                  
                
                COBieProgress progress = new COBieProgress(Context);
                progress.Initialise("Validating Workbooks", Workbook.Count, 0);
                for (int i = 0; i < Workbook.Count; i++)
                {

                    progress.IncrementAndUpdate();

                    var sheet = Workbook[i];
                    sheet.Validate();
                    
                }

                //ValidateForeignKeys(progress);
                progress.Finalise();
            }
            catch (Exception)
            {
                // TODO: Handle
                throw;
            }
        }

        private void ValidateForeignKeys(COBieProgress progress)
        {
            progress.Initialise("Validating Foreign Keys", Workbook.Count, 0);

            COBieErrorCollection errorFKCollection = new COBieErrorCollection();
            for (int i = 0; i < Workbook.Count; i++)
            {
                progress.IncrementAndUpdate();

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
                            object[] param = { attr.ColumnName };
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

        private void GenerateCOBieData()
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
