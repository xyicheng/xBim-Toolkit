using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using System.Data;
using System.Xml.Serialization;
using System.IO;
using Xbim.XbimExtensions.Parser;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ActorResource;
using Xbim.Ifc.ProductExtension;
using System.Xml;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Reflection;

namespace Xbim.COBie.COBieExtensions
{
   

    public class COBieCell
    {
        public COBieCell()
        {

        }

        public COBieCell(string cellValue)
        {
            CellValue = cellValue;
        }

        public string CellValue { get; set; }        
        public COBieColumn CobieCol {get; set;}
        public COBieAttributeState COBieState { get; set; }

        
                
        public static Regex RegExAlphaNumeric = new Regex("^[a-zA-Z0-9]*$");
        public static Regex RegExEmail = new Regex("[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?");
    }

    public class COBieReader
    {

        public DataSet COBieDataSheets { get; set; }

        private List<COBieError> _cobieErrors = new List<COBieError>();
        public List<COBieError> CobieErrors { get { return _cobieErrors; } }

        public void AddCOBieError(COBieError cobieError)
        {
            _cobieErrors.Add(cobieError);
        }

        public COBieError GetCobieError(COBieCell cell, string sheetName)
        {
            int maxLength = cell.CobieCol.ColumnLength;
            COBieAllowedType allowedType = cell.CobieCol.AllowedType;
            COBieError err = new COBieError(sheetName, cell.CobieCol.ColumnName, "", COBieError.ErrorTypes.None);
            if (cell.CellValue.Length > maxLength)
            {
                err.ErrorDescription = "Value must be under 255 characters";
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
        
        COBieSheet<COBieContactRow> _cobieContracts = new COBieSheet<COBieContactRow>();

        //COBieSheet<COBieAssemblyRow> _cobieAssemblySheet = new COBieSheet<COBieAssemblyRow>();


        COBieSheet<COBieAssemblyRow> _cobieAssemblies = new COBieSheet<COBieAssemblyRow>();
        COBieSheet<COBieComponentRow> _cobieComponents = new COBieSheet<COBieComponentRow>();
        COBieSheet<COBieConnectionRow> _cobieConnections = new COBieSheet<COBieConnectionRow>();
        COBieSheet<COBieCoordinateRow> _cobieCoordinates = new COBieSheet<COBieCoordinateRow>();
        COBieSheet<COBieDocumentRow> _cobieDocuments = new COBieSheet<COBieDocumentRow>();
        COBieSheet<COBieFacilityRow> _cobieFacilities = new COBieSheet<COBieFacilityRow>();
        COBieSheet<COBieFloorRow> _cobieFloors = new COBieSheet<COBieFloorRow>();
        COBieSheet<COBieImpactRow> _cobieImpacts = new COBieSheet<COBieImpactRow>();
        COBieSheet<COBieIssueRow> _cobieIssues = new COBieSheet<COBieIssueRow>();
        COBieSheet<COBieJobRow> _cobieJobs = new COBieSheet<COBieJobRow>();
        COBieSheet<COBiePickListsRow> _cobiePickLists = new COBieSheet<COBiePickListsRow>();
        COBieSheet<COBieResourceRow> _cobieResources = new COBieSheet<COBieResourceRow>();
        COBieSheet<COBieSpaceRow> _cobieSpaces = new COBieSheet<COBieSpaceRow>();
        COBieSheet<COBieSpareRow> _cobieSpares = new COBieSheet<COBieSpareRow>();
        COBieSheet<COBieSystemRow> _cobieSystems = new COBieSheet<COBieSystemRow>();
        COBieSheet<COBieTypeRow> _cobieTypes = new COBieSheet<COBieTypeRow>();
        COBieSheet<COBieZoneRow> _cobieZones = new COBieSheet<COBieZoneRow>();
        COBieSheet<COBieAttributeRow> _cobieAttributes = new COBieSheet<COBieAttributeRow>();
                
        private void IntialiseFromModel(IModel model, string pickListsXMLFilePath)
        {
            // set all the properties
            COBieQueries cq = new COBieQueries();

            // create pick lists from xml
            _cobiePickLists = cq.GetCOBiePickListsSheet(pickListsXMLFilePath);

            // populate all sheets from model
            _cobieAssemblies = cq.GetCOBieAssemblySheet(model);
            _cobieAttributes = cq.GetCOBieAttributeSheet(model);
            _cobieComponents = cq.GetCOBieComponentSheet(model);
            _cobieConnections = cq.GetCOBieConnectionSheet(model);
            _cobieContracts = cq.GetCOBieContactSheet(model, _cobiePickLists);
            _cobieCoordinates = cq.GetCOBieCoordinateSheet(model);
            _cobieDocuments = cq.GetCOBieDocumentSheet(model, _cobiePickLists);
            Populate_COBieFacilities(model);
            _cobieFloors = cq.GetCOBieFloorSheet(model, _cobiePickLists);
            _cobieImpacts = cq.GetCOBieImpactSheet(model, _cobiePickLists);
            _cobieIssues = cq.GetCOBieIssueSheet(model, _cobiePickLists);
            _cobieJobs = cq.GetCOBieJobSheet(model, _cobiePickLists);
            
            _cobieResources = cq.GetCOBieResourceSheet(model);
            _cobieSpaces = cq.GetCOBieSpaceSheet(model, _cobiePickLists);
            _cobieSpares = cq.GetCOBieSpareSheet(model);
            _cobieSystems = cq.GetCOBieSystemSheet(model);
            _cobieTypes = cq.GetCOBieTypeSheet(model);
            _cobieZones = cq.GetCOBieZoneSheet(model, _cobiePickLists);
            
            
            
            
            
            
            

            //_cobieAssemblySheet = cq.GetCOBieAssemblySheet(model);

            

            
            
        }

        private void PopulateErrors()
        {
            try
            {
                _cobieErrors = new List<COBieError>();

                List<COBieError> errors;
                _cobieAssemblies.Validate(out errors);
                _cobieAttributes.Validate(out errors);
                _cobieComponents.Validate(out errors);
                _cobieConnections.Validate(out errors);
                _cobieContracts.Validate(out errors);
                _cobieCoordinates.Validate(out errors);
                _cobieDocuments.Validate(out errors);
                _cobieFacilities.Validate(out errors);
                _cobieFloors.Validate( out errors);
                _cobieImpacts.Validate(out errors);
                _cobieIssues.Validate(out errors);
                _cobieJobs.Validate(out errors);
                _cobiePickLists.Validate(out errors);
                _cobieResources.Validate(out errors);
                _cobieSpaces.Validate(out errors);
                _cobieSpares.Validate(out errors);
                _cobieSystems.Validate(out errors);
                _cobieTypes.Validate(out errors);
                _cobieZones.Validate(out errors);                
                
                               
                
                

                //// loop through all the sheets and preopare error dataset
                //if (_cobieFloors.Rows.Count > 0)
                //{
                //    // loop through each floor row
                //    IEnumerable<PropertyInfo> Properties = typeof(COBieFloorRow).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                //                                    .Where(prop => prop.GetSetMethod() != null);
                    
                //    foreach (COBieFloorRow row in _cobieFloors.Rows)
                //    {
                //        // loop through each column, get its attributes and check if column value matches the attributes constraints
                //        foreach (PropertyInfo propInfo in Properties)
                //        {
                //            COBieCell cell = row[propInfo.Name];
                //            COBieError err = GetCobieError(cell, "COBieFloor");

                //            // check for primary key
                //            if (HasDuplicateFloorValues(_cobieFloors, cell.CellValue))
                //            {
                //                err.ErrorDescription = cell.CellValue + " duplication";
                //                err.ErrorType = COBieError.ErrorTypes.PrimaryKey_Violation;
                //            }

                //            if (err.ErrorType != COBieError.ErrorTypes.None) _cobieErrors.Add(err);
                //        }
                //    }
                //}
                
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
            foreach (COBieFloorRow row in _cobieFloors.Rows)
            {
                if (row.Name == val) count++; 
            }
            if (count > 1) return true;

            return false;
        }

        public DataSet GetCOBieData(IModel model, string pickListsXMLFilePath)
        {
            //COBieContact cobieContact = new COBieContact(model);
            //COBieContact[] cobieContacts = { cobieContact };

            //DataSet ds = ToDataSet(cobieContacts);
            IntialiseFromModel(model, pickListsXMLFilePath);

            PopulateErrors();


            DataSet dsSheets = new DataSet();

            // DataTable dt;

            // xml 
            string filePath = "cobieData.xml";
            XmlTextWriter textWriter = new XmlTextWriter(filePath, null);   

            //if (_cobieContracts.Count > 0)
            //{
            //    dt = ToDataTable(_cobieContracts.ToArray(), "COBieContract");
            //    dsSheets.Tables.Add(dt.Copy());
            //}

            //if (_cobieAssemblies.Count > 0)
            //{
            //    dt = ToDataTable(_cobieAssemblies.ToArray(), "COBieAssemblyRow");
            //    dsSheets.Tables.Add(dt.Copy());

            //    //ToXML(_cobieAssemblies.ToArray(), "COBieAssemblyRow", textWriter);

            //}

            

            //if (_cobieComponents.Count > 0)
            //{
            //    dt = ToDataTable(_cobieComponents.ToArray(), "COBieComponent");
            //    dsSheets.Tables.Add(dt.Copy());
            //}

            //if (_cobieFacilities.Count > 0)
            //{
            //    dt = ToDataTable(_cobieFacilities.ToArray(), "COBieFacility");
            //    dsSheets.Tables.Add(dt.Copy());
            //}

            //if (_cobieFloors.Count > 0)
            //{
            //    dt = ToDataTable(_cobieFloors.ToArray(), "COBieFloor");
            //    dsSheets.Tables.Add(dt.Copy());
            //}

            //if (_cobieSpaces.Count > 0)
            //{
            //    dt = ToDataTable(_cobieSpaces.ToArray(), "COBieSpace");
            //    dsSheets.Tables.Add(dt.Copy());
            //}

            //if (_cobieZones.Count > 0)
            //{
            //    dt = ToDataTable(_cobieZones.ToArray(), "COBieZone");
            //    dsSheets.Tables.Add(dt.Copy());
            //}

            //if (_cobieTypes.Count > 0)
            //{
            //    dt = ToDataTable(_cobieTypes.ToArray(), "COBieType");
            //    dsSheets.Tables.Add(dt.Copy());
            //}

            //if (_cobieSystems.Count > 0)
            //{
            //    dt = ToDataTable(_cobieTypes.ToArray(), "COBieSystem");
            //    dsSheets.Tables.Add(dt.Copy());
            //}

            //if (_cobieConnections.Count > 0)
            //{
            //    dt = ToDataTable(_cobieTypes.ToArray(), "COBieConnection");
            //    dsSheets.Tables.Add(dt.Copy());
            //}

            return dsSheets;
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

        

        private void Populate_COBieFacilities(IModel model)
        {
            COBieFacilityRow facility = new COBieFacilityRow();
            facility.InitFacility(model);
            // should only be 1 facility in 1 spreadsheet            
            //_cobieFacilities.Add(facility);
        }

        #endregion
    }
}
