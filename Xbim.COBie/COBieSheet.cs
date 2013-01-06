﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Data;
using Xbim.COBie.Resources;
using System.Diagnostics;

namespace Xbim.COBie
{
    [Serializable()]
    public class COBieSheet<T> : ICOBieSheet<T> where T : COBieRow
    {
        

        #region Private Fields
		private Dictionary<int, COBieColumn> _columns;
        private COBieErrorCollection _errors = new COBieErrorCollection();
        private Dictionary<string, HashSet<string>> _indices;
	    #endregion


        #region Properties
        public string SheetName { get; private set; }
        public List<T> Rows { get; private set; }
        public Dictionary<int, COBieColumn> Columns { get { return _columns; } }
        public Dictionary<string, HashSet<string>> Indices {  get {  return _indices; } }
        public COBieErrorCollection Errors  { get { return _errors; } }
        public IEnumerable<COBieColumn> KeyColumns
        {
            get
            {
                return Columns.Where(c => COBieKeyType.CompoundKey.Equals(c.Value.KeyType)
                                          || COBieKeyType.CompoundKey_ForeignKey.Equals(c.Value.KeyType)
                                          || COBieKeyType.PrimaryKey.Equals(c.Value.KeyType)).Select(c => c.Value);
            }
        }
        public IEnumerable<COBieColumn> ForeignKeyColumns
        {
            get 
            { 
                return Columns.Where(c => COBieKeyType.ForeignKey.Equals(c.Value.KeyType) 
                                          || COBieKeyType.CompoundKey_ForeignKey.Equals(c.Value.KeyType)).Select(c => c.Value); 
            }
        } 

        public T this[int index] { get { return Rows[index]; } }

        public int RowCount { get { return Rows.Count; } }

        #endregion
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="sheetName">Sheet name for this sheet object</param>
        public COBieSheet(string sheetName)
        {
            Rows = new List<T>();
            _indices = new Dictionary<string, HashSet<string>>();
			SheetName = sheetName;
            PropertyInfo[]  properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(prop => prop.GetSetMethod() != null).ToArray();
            _columns = new Dictionary<int, COBieColumn>();
            // add column info 
            foreach (PropertyInfo propInfo in properties)
            {
                object[] attrs = propInfo.GetCustomAttributes(typeof(COBieAttributes), true);
                if (attrs != null && attrs.Length > 0)
                {
                    COBieAttributes attr = (COBieAttributes)attrs[0];
                    List<string> aliases = GetAliases(propInfo);
                    _columns.Add(attr.Order, new COBieColumn(propInfo, attr, aliases));
                }
            }
            
        }

        #region Methods
        
        /// <summary>
        /// Create a COBieRow of the correct type for this sheet, not it is not added to the Rows list
        /// </summary>
        /// <returns>Correct COBieRow type for this COBieSheet</returns>
        public T AddNewRow()
        {
           Object[] args = {this};
           AddRow((T)Activator.CreateInstance(typeof(T), args));
           return Rows.Last();
        }

        public void AddRow(T cOBieRow)
        {
            cOBieRow.RowNumber = Rows.Count() + 1;
            Rows.Add(cOBieRow);
        }

        /// <summary>
        /// Get the alias attribute name values and add to a list of strings
        /// </summary>
        /// <param name="propInfo">PropertyInfo for the column field</param>
        /// <returns>List of strings</returns>
        private List<string> GetAliases(PropertyInfo propInfo)
        {
            object[] attrs = propInfo.GetCustomAttributes(typeof(COBieAliasAttribute), true);

            if (attrs != null && attrs.Length > 0)
                return attrs.Cast<COBieAliasAttribute>().Select(s => s.Name).ToList<string>();
            else 
                return new List<string>();
        }

        /// <summary>
        /// Set the initial hash code for each row in the sheet, i.e when the workbook is created
        /// </summary>
        public void SetRowsHashCode()
        {
            foreach (COBieRow row in Rows)
            {
                //SetThe initial has vale for each row
                row.SetInitialRowHash();
            }
        }

        /// <summary>
        /// Build Indexed dictionaries of values in each Keyed Columns.
        /// </summary>
        /// <remarks>Permits omptimised validation</remarks>
        public void BuildIndices()
        {
            // Add Indices first. We may have no rows of data, but should still have indices
            foreach (COBieColumn column in KeyColumns)
            {
                if (!_indices.ContainsKey(column.ColumnName)) //no column key so add to dictionary
                    _indices.Add(column.ColumnName, new HashSet<string>());
            }

            foreach (COBieRow row in Rows)
            {
                foreach (COBieColumn cobieColumn in KeyColumns)
                {
                    string columnName = cobieColumn.ColumnName;
                    if (!string.IsNullOrEmpty(columnName))
                    {
                        string rowValue = row[columnName].CellValue.Trim();
                        if (rowValue != null)
                        {
                            if (!string.IsNullOrEmpty(rowValue))
                            {
                                if (!_indices[columnName].Contains(rowValue)) //add value to HashSet, if not existing
                                    _indices[columnName].Add(rowValue);
                            } 
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Validate the sheet
        /// </summary>
        /// <param name="workbook"></param>
        public void Validate(COBieWorkbook workbook)
        {
            _errors.Clear();

            ValidatePrimaryKeysUnique();
            ValidateFields();
            ValidateForeignKeys(workbook);
        }

        /// <summary>
        /// Validate the existence of the Foreign Key value on the referencing sheet, if not add error
        /// </summary>
        /// <param name="context">COBieContext object holding global values for this model</param>
        private void ValidateForeignKeys(COBieWorkbook workbook)
        {
            int rowIndex = 1;
            foreach (COBieRow row in Rows)
            {
                foreach (COBieColumn foreignKeyColumn in ForeignKeyColumns)
                {
                    // TODO: COBieColumn chould own the relationship rather than creating a new one each time.
                    COBieColumnRelationship cobieReference = new COBieColumnRelationship(workbook, foreignKeyColumn);

                    if (!string.IsNullOrEmpty(foreignKeyColumn.ReferenceColumnName))
                    {
                        
                        COBieCell cell = row[foreignKeyColumn.ColumnOrder];
                        
                        string foreignKeyValue = cell.CellValue;

                        // Don't validate nulls. Will be reported by the Foreign Key null value check, so just skip here 
                        if (!string.IsNullOrEmpty(foreignKeyValue))
                        {
                            bool isValid = false;

                            bool isPickList = (cobieReference.SheetName == Constants.WORKSHEET_PICKLISTS);

                            if (isPickList)
                            {
                                isValid = PickListMatch(cobieReference, cell);
                            }
                            else
                            {
                                isValid = ForeignKeyMatch(cobieReference, cell);
                            }
                            //report no match
                            if (!isValid)
                            {
                                string errorDescription = BuildErrorMessage(cobieReference, isPickList);

                                COBieError.ErrorTypes errorType = isPickList == true ? COBieError.ErrorTypes.PickList_Violation : COBieError.ErrorTypes.ForeignKey_Violation;

                                COBieError error = new COBieError(SheetName, foreignKeyColumn.ColumnName, errorDescription, errorType, 
                                    row.InitialRowHashValue, foreignKeyColumn.ColumnOrder, rowIndex);
                                _errors.Add(error);
                            }
                        }
                    }
                }
                rowIndex++;
            }

        }

        private static string BuildErrorMessage(COBieColumnRelationship cobieReference, bool isPickList)
        {
            string errFieldName = cobieReference.ColumnName;
            //get the correct Pick list column name depending on template for the category columns only, for now

            if (isPickList && (cobieReference.ColumnName.Contains("Category")))
                errFieldName = ErrorDescription.ResourceManager.GetString(cobieReference.ColumnName.Replace("-", "")); //strip out the "-" to get the resource, (resource did not like the '-' in the name)
            if (string.IsNullOrEmpty(errFieldName)) //if resource not found then reset back to field name
                errFieldName = cobieReference.ColumnName;

            string errorDescription = String.Format(ErrorDescription.PickList_Violation, cobieReference.SheetName, errFieldName);
            return errorDescription;
        }

        /// <summary>
        /// Match the Foreign Key with the primary key field
        /// </summary>
        /// <param name="reference">The COBie Index to cross reference</param>
        /// <param name="cell">The COBie Cell to validate</param>
        /// <returns>bool</returns>
        private bool ForeignKeyMatch(COBieColumnRelationship reference, COBieCell cell)
        {
            if(reference.HasKeyMatch(cell.CellValue))
                return true;

            if (cell.COBieColumn.AllowsMultipleValues == true)
            {
                foreach (string value in cell.CellValues)
                {
                    if (!reference.HasKeyMatch(value))
                        return false;
                }
                return true;
            }

            return false;

        }

        
       
        /// <summary>
        /// Match either side of a : delimited string or all of the string including the delimiter
        /// </summary>
        /// <param name="hashSet">List of strings</param>
        /// <param name="foreignKeyValue">string to match</param>
        /// <returns>true if a match, false if none</returns>
        private bool PickListMatch(COBieColumnRelationship reference, COBieCell cell)
        {
            if (cell.CellValue == Constants.DEFAULT_STRING) 
                return false;

            if (reference.HasKeyMatch(cell.CellValue))
                return true;

            // There are no current cases where PickLists can have Many to Many mappings - only One to Many. So don't worry about MultipleValues.

            // Due to the way some Categories/Classifications in Pick lists are compound keys (e.g. 11-11 11 14: Exhibition Hall ... where the code and name are stored separately in IFC)
            // we need to special case partial matches, since we may have the code, name, or code:name (perhaps with differing white space)

            if (cell.CellValue.Contains(":")) //assume category split
            {
                return reference.HasPartialMatch(cell.CellValue, ':');  
            }
            

            return false;
        }


        /// <summary>
        /// Validate the row columns against the attributes set for each column 
        /// </summary>
        public void ValidateFields()
        {
            int r = 0;
            foreach(T row in Rows)
            {
                r++;
                for(int col = 0 ; col < row.RowCount ; col++)
                {
                    COBieError err = GetCobieError(row[col], SheetName, r, col, row.InitialRowHashValue);
                    if (err != null)
                        _errors.Add(err);
                }
            }
        }

        /// <summary>
        /// Validate the Primary Keys only exist once in the sheet 
        /// </summary>
        private void ValidatePrimaryKeysUnique()
        {
            var dupes = Rows
                    .Select((v, i) => new { row = v, index = i }) //get COBieRow and its index in the list
                    .GroupBy(r => r.row.GetPrimaryKeyValue.ToLower().Trim(), (key, group) => new {rowkey = key, rows = group }) //group by the primary key value(s) joint as a delimited string
                    .Where(grp => grp.rows.Count() > 1); 
                 

            List<string> keyColList = new List<string>();
            foreach (COBieColumn col in KeyColumns)
            {
                keyColList.Add(col.ColumnName);
            }
            string keyCols = string.Join(",", keyColList);

            foreach (var dupe in dupes)
            {
                List<string> indexList = new List<string>();
                foreach (var row in dupe.rows)
                {
                    indexList.Add((row.index + 2).ToString());
                }
                string rowIndexList = string.Join(",", indexList);
                foreach (var row in dupe.rows)
                {
                    string errorDescription = String.Format(ErrorDescription.PrimaryKey_Violation, keyCols, rowIndexList);
                    COBieError error = new COBieError(SheetName, keyCols, errorDescription, COBieError.ErrorTypes.PrimaryKey_Violation, 
                        row.row.InitialRowHashValue, KeyColumns.First().ColumnOrder, (row.index + 1));
                    _errors.Add(error);
                   
                }
                
                
            }

        }


        /// <summary>
        /// Validating of the column attributes
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="sheetName"></param>
        /// <param name="row"></param>
        /// <param name="col"></param>
        /// <returns>COBieError object</returns>
        private COBieError GetCobieError(COBieCell cell, string sheetName, int row, int col, string initialRowHash)
        {
            int maxLength = cell.COBieColumn.ColumnLength;
            COBieAllowedType allowedType = cell.COBieColumn.AllowedType;
            COBieAttributeState state = cell.COBieColumn.AttributeState;
            COBieError err = new COBieError(sheetName, cell.COBieColumn.ColumnName, "", COBieError.ErrorTypes.None, initialRowHash, col, row);
      
            // If field is required and cell value is empty
            if (string.IsNullOrEmpty(cell.CellValue) && 
                    (state == COBieAttributeState.Required_PrimaryKey || 
                     state == COBieAttributeState.Required_CompoundKeyPart ||
                     state == COBieAttributeState.Required_Information))
            {
                err.ErrorDescription = ErrorDescription.Text_Value_Expected;
                err.ErrorType = COBieError.ErrorTypes.Text_Value_Expected;
            }
            // Required Referenced values should have values
            else if (string.IsNullOrEmpty(cell.CellValue) && 
                    (state == COBieAttributeState.Required_Reference_ForeignKey || 
                     state == COBieAttributeState.Required_Reference_PickList ||
                     state == COBieAttributeState.Required_Reference_PrimaryKey))
            {
                err.ErrorDescription = ErrorDescription.Null_ForeignKey_Value;
                err.ErrorType = COBieError.ErrorTypes.Null_ForeignKey_Value;
            }
            else if (( (state == COBieAttributeState.Required_Information) || 
                       (state == COBieAttributeState.Required_IfSpecified) ) &&
                     (cell.CellValue == Constants.DEFAULT_STRING)
                    ) //if a required value but marked as n/a then do not class as error
            {
                return null;
            }
            else if (cell.CellValue.Length > maxLength)
            {
                err.ErrorDescription = String.Format(ErrorDescription.Value_Out_of_Bounds, maxLength);
                err.ErrorType = COBieError.ErrorTypes.Value_Out_of_Bounds;
            }
            else if (allowedType == COBieAllowedType.AlphaNumeric && !cell.IsAlphaNumeric())
            {
                err.ErrorDescription = ErrorDescription.AlphaNumeric_Value_Expected;
                err.ErrorType = COBieError.ErrorTypes.AlphaNumeric_Value_Expected;
            }
            else if (allowedType == COBieAllowedType.Email && !cell.IsEmailAddress())
            {
                err.ErrorDescription = ErrorDescription.Email_Value_Expected;
                err.ErrorType = COBieError.ErrorTypes.Email_Value_Expected;
            }

            else if ((allowedType == COBieAllowedType.ISODate) ||
                    (allowedType == COBieAllowedType.ISODateTime) 
                    )
            {
                DateTime dt;
                
                if (DateTime.TryParse(cell.CellValue, out dt) == false)
                {
                    err.ErrorDescription = ErrorDescription.ISODate_Value_Expected;
                    err.ErrorType = COBieError.ErrorTypes.ISODate_Value_Expected;
                }
            }

            if (allowedType == COBieAllowedType.Numeric)
            { 
                double d;
                
                if (double.TryParse(cell.CellValue, out d) == false)
                {
                    err.ErrorDescription = ErrorDescription.Numeric_Value_Expected;
                    err.ErrorType = COBieError.ErrorTypes.Numeric_Value_Expected;
                }
            }

            if (err.ErrorType != COBieError.ErrorTypes.None)
            {
                err.FieldValue = cell.CellValue;
                return err;
            }
            else
            {
                return null;
            }
        }
        #endregion

       
    }  

}
