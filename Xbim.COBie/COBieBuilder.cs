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
using Xbim.COBie.Contracts;
using Xbim.COBie.Serialisers;



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
			if (Context.Model == null) { throw new ArgumentException("COBieReader context must contain a models."); }

            //set default date for this run
            Context.RunDate = DateTime.Now.ToString(Constants.DATE_FORMAT);

            // set all the properties
            COBieQueries cq = new COBieQueries(Context);

            //create pick list from the template sheet
            COBieSheet<COBiePickListsRow> CobiePickLists = null;
            if ((!string.IsNullOrEmpty(Context.TemplateFileName)) &&
                File.Exists(Context.TemplateFileName)
                )
            {
                COBieXLSDeserialiser deSerialiser = new COBieXLSDeserialiser(Context.TemplateFileName, Constants.WORKSHEET_PICKLISTS);
                COBieWorkbook wbook = deSerialiser.Deserialise();
                if (wbook.Count > 0) CobiePickLists = (COBieSheet<COBiePickListsRow>)wbook.First();
            }
            
            //fall back to xml file if not in template
            string pickListFileName = "PickLists.xml";
            if ((CobiePickLists == null) &&
                File.Exists(pickListFileName)
                )
                CobiePickLists = cq.GetCOBiePickListsSheet(pickListFileName);// create pick lists from xml
           
            //contact sheet first as it will fill contact information lookups for other sheets
            Workbook.Add(cq.GetCOBieContactSheet());
            Workbook.Add(cq.GetCOBieFacilitySheet()); 
            Workbook.Add(cq.GetCOBieFloorSheet());
            COBieSheet<COBieZoneRow> zonesheet = cq.GetCOBieZoneSheet();//we need zone before spaces as it sets a flag on departments property
            Workbook.Add(cq.GetCOBieSpaceSheet());
            Workbook.Add(zonesheet); 
            Workbook.Add(cq.GetCOBieTypeSheet());
            Workbook.Add(cq.GetCOBieComponentSheet());
            Workbook.Add(cq.GetCOBieSystemSheet());
            Workbook.Add(cq.GetCOBieAssemblySheet());
            Workbook.Add(cq.GetCOBieConnectionSheet());
            Workbook.Add(cq.GetCOBieSpareSheet());
            Workbook.Add(cq.GetCOBieResourceSheet());
            Workbook.Add(cq.GetCOBieJobSheet());            
            Workbook.Add(cq.GetCOBieImpactSheet());
            Workbook.Add(cq.GetCOBieDocumentSheet());
            Workbook.Add(cq.GetCOBieAttributeSheet());//we need to fill attributes here as it is populated by Components, Type, Space, Zone, Floors, Facility etc
            Workbook.Add(cq.GetCOBieCoordinateSheet());
            Workbook.Add(cq.GetCOBieIssueSheet());
            if (CobiePickLists != null) 
                Workbook.Add(CobiePickLists); 
            else
                Workbook.Add(new COBieSheet<COBiePickListsRow>(Constants.WORKSHEET_PICKLISTS)); //add empty pick list
           
            //clear sheet session values from context
            Context.EMails.Clear();
            

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
                    if (sheet.SheetName != Constants.WORKSHEET_PICKLISTS) //skip validation on picklist
                    {
                        sheet.Validate(Workbook);
                    }
                    
                    
                    
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

        private int GetCOBieSheetIndexBySheetName(string sheetName)
        {
            for (int i = 0; i < Workbook.Count; i++)
            {
                if (sheetName == Workbook[i].SheetName)
                    return i;
            }
            return -1;
        }

        
        private void GenerateCOBieData()
        {
            Initialise();
            Workbook.CreateIndices();
            PopulateErrors();			
        }

		/// <summary>
		/// Passes this instance of the COBieReader into the provided ICOBieFormatter
		/// </summary>
		/// <param name="serialiser">The object implementing the ICOBieFormatter interface.</param>
        public void Export(ICOBieSerialiser serialiser)
		{
			if (serialiser == null) { throw new ArgumentNullException("formatter", "Parameter passed to COBieReader.Export(ICOBieFormatter) must not be null."); }
            
            //remove the pick list sheet
            ICOBieSheet<COBieRow> PickList = Workbook.Where(wb => wb.SheetName == "PickLists").FirstOrDefault();
            if (PickList != null)
                Workbook.Remove(PickList);

            // Passes this 
			serialiser.Serialise(Workbook);
		}


        
    }
}
