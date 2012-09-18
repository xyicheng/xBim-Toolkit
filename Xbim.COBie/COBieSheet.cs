using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using System.Data;

namespace Xbim.COBie
{
    public class COBieSheet<T> : ICOBieSheet<T> where T : COBieRow
    {
		public string SheetName { get; set; }
        public List<T> Rows { get; set; }

        public Dictionary<int, COBieColumn> Columns { get { return _columns; } }
        public PropertyInfo[] Properties { get { return _properties; } }
        public Dictionary<PropertyInfo, object[]> Attributes { get { return _attributes; } }
        
        private Dictionary<int, COBieColumn> _columns;
        private PropertyInfo[] _properties;
        private Dictionary<PropertyInfo, object[]> _attributes;
        private List<PropertyInfo> _keyColumns = new List<PropertyInfo>();
        private List<PropertyInfo> _foreignKeyColumns = new List<PropertyInfo>();

        public List<PropertyInfo> KeyColumns
        {
            get { return _keyColumns; }
            
        }

        public List<PropertyInfo> ForeignKeyColumns
        {
            get { return _foreignKeyColumns; }

        }
                
        public COBieSheet(string sheetName)
        {
            Rows = new List<T>();
			SheetName = sheetName;
            _properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(prop => prop.GetSetMethod() != null).ToArray();
            _columns = new Dictionary<int, COBieColumn>();
            _attributes = new Dictionary<PropertyInfo, object[]>();

            // add column info 
            foreach (PropertyInfo propInfo in _properties)
            {
                object[] attrs = propInfo.GetCustomAttributes(typeof(COBieAttributes), true);
                if (attrs != null && attrs.Length > 0)
                {
                    COBieAttributes attr = (COBieAttributes)attrs[0];
                    List<string> aliases = GetAliases(propInfo);
                    _columns.Add(attr.Order, new COBieColumn(attr.ColumnName, attr.MaxLength, attr.AllowedType, attr.KeyType, aliases));
                    _attributes.Add(propInfo, attrs);
                    
                    if (attr.KeyType == COBieKeyType.CompoundKey || attr.KeyType == COBieKeyType.CompoundKey_ForeignKey || attr.KeyType == COBieKeyType.PrimaryKey)
                        _keyColumns.Add(propInfo);

                    if (attr.KeyType == COBieKeyType.ForeignKey || attr.KeyType == COBieKeyType.CompoundKey_ForeignKey)
                        _foreignKeyColumns.Add(propInfo);
                }
            }
        }

        public bool HasPrimaryKey
        {
            get
            {
                return _keyColumns.Any();
            }
        }

        

        public COBieErrorCollection ValidatePrimaryKey()
        {
            // E.g.
            // string query = "SELECT primaryColumnName, Count(primaryColumnName) as Total FROM tableName Group By primaryColumnName ;"; 
            var dup = Rows
                        .GroupBy(r => new { r.GetPrimaryKeyValue })
                        .Select(group => new { Result = group.Key, Count = group.Count() })
                        .OrderByDescending(x => x.Count);

            // check if we have count of any of the results > 1, if yes then that is an error as each result should be unique
            COBieErrorCollection errorColl = new COBieErrorCollection();
            foreach (var r in dup)
            {
                if (r.Count > 1)
                {
                    COBieError error = new COBieError(SheetName, string.Join(";", _keyColumns), r.Result.ToString() + " duplication", COBieError.ErrorTypes.PrimaryKey_Violation);
                    errorColl.Add(error);
                }
            }

            if (errorColl.Count > 0) return errorColl;
            else return null;
        }

        public COBieErrorCollection ValidateForeignKey(List<string> colMain, List<string> colForeign)
        {
            // E.g.
            // SELECT Facility.CreatedBy, Contact.Email FROM Contact
            // Left Outer Join Facility On Contact.Email = Facility.CreatedBy 
            // WHERE Facility.CreatedBy = null 

            var query = from cm in colMain
                        join cf in colForeign on cm equals cf into gj
                        from subpet in gj.DefaultIfEmpty()
                        select new { cm, ColForeign = (subpet == null ? String.Empty : subpet) };

            COBieErrorCollection errorColl = new COBieErrorCollection();
            foreach (var r in query)
            {
                if (string.IsNullOrEmpty(r.ColForeign))
                {
                    COBieError error = new COBieError(SheetName, "", r.cm.ToString() + " duplication", COBieError.ErrorTypes.Null_ForeignKey_Value);
                    errorColl.Add(error);
                }
            }

            if (errorColl.Count > 0) return errorColl;
            else return null;
        }

        public List<string> GetForeignKeyValues(string foreignColName)
        {
            List<string> colValues = new List<string>();
            foreach (T row in Rows)
            {
                // loop through each column, get its attributes and check if column value matches the attributes constraints
                foreach (PropertyInfo propInfo in Properties)
                {
                    object[] attrs = Attributes[propInfo];
                    if (attrs != null && attrs.Length > 0)
                    {                        
                        COBieAttributes attr = (COBieAttributes)attrs[0];
                        if (attr.ColumnName == foreignColName)
                        {
                            Type cobieType = row.GetType();
                            string val = (propInfo.GetValue(row, null) == null) ? "" : propInfo.GetValue(row, null).ToString();

                            colValues.Add(val);
                        }
                        
                    }
                }
            }
            if (colValues.Count > 0)
                return colValues;
            else
                return null;
        }

        public List<T> GetRows()
        {
            return Rows;
        }

        private List<string> GetAliases(PropertyInfo propInfo)
        {
            object[] attrs = propInfo.GetCustomAttributes(typeof(COBieAliasAttribute), true);

            if (attrs != null && attrs.Length > 0)
            {
                return attrs.Cast<COBieAliasAttribute>().Select(s => s.Name).ToList<string>();
            }
            else
            {
                return new List<string>();
            }
            
        }

        public COBieErrorCollection Validate()
        {
            COBieErrorCollection errorColl = new COBieErrorCollection();

            List<COBieCell> pkColumnValues = new List<COBieCell>();

            foreach (T row in Rows)
            {
                // loop through each column, get its attributes and check if column value matches the attributes constraints
                foreach (PropertyInfo propInfo in Properties)
                {
                    object[] attrs = Attributes[propInfo];
                    if (attrs != null && attrs.Length > 0)
                    {
                        Type cobieType = row.GetType();
                        string val = (propInfo.GetValue(row, null) == null) ? "" : propInfo.GetValue(row, null).ToString();

                        COBieCell cell = new COBieCell(val);
                        COBieAttributes attr = (COBieAttributes)attrs[0];
                        cell.COBieState = attr.State;
                        cell.CobieCol = new COBieColumn(attr.ColumnName, attr.MaxLength, attr.AllowedType, attr.KeyType);
                        COBieError err = GetCobieError(cell, SheetName);

                        // if this cell is set as primary key then add its value to array for later checking for primary key voilation
                        if (cell.CobieCol.KeyType == COBieKeyType.PrimaryKey)
                        {
                            pkColumnValues.Add(cell);
                        }
                        else if (cell.CobieCol.KeyType == COBieKeyType.ForeignKey)
                        {
                            // check if the value does exist in the foreign column values

                        }

                        if (err.ErrorType != COBieError.ErrorTypes.None) errorColl.Add(err);
                    }
                }
            }

            // check for primary key errors (i.e. if cell value matches any other cell's value)
            foreach (COBieCell cell in pkColumnValues)
            {
                if (HasDuplicateValues(cell.CellValue, pkColumnValues))
                {
                    COBieError err = new COBieError();
                    err.ErrorDescription = cell.CellValue + " duplication";
                    err.ErrorType = COBieError.ErrorTypes.PrimaryKey_Violation;
                    errorColl.Add(err);
                }
            }

            return errorColl;
        }

        private bool HasDuplicateValues(string cellValue, List<COBieCell> pkColumnValues)
        {
            int count = 0;
            foreach (COBieCell cell in pkColumnValues)
            {
                if (cellValue == cell.CellValue) count++;
            }
            if (count > 1) return true;           

            return false;
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
            if (allowedType == COBieAllowedType.AlphaNumeric && !cell.IsAlphaNumeric())
            {
                err.ErrorDescription = "Value must be alpha-numeric";
                err.ErrorType = COBieError.ErrorTypes.AlphaNumeric_Value_Expected;
            }
            if (allowedType == COBieAllowedType.Email && !cell.IsEmailAddress())
            {
                err.ErrorDescription = "Value must be a valid email address";
                err.ErrorType = COBieError.ErrorTypes.Email_Value_Expected;
            }

            if (allowedType == COBieAllowedType.ISODate)
            {
                DateTime dt;
                DateTime.TryParse(cell.CellValue, out dt);
                if (dt == DateTime.MinValue) err.ErrorDescription = "Value must be a valid iso date";
            }

            if (allowedType == COBieAllowedType.Numeric)
            {
                double d;
                double.TryParse(cell.CellValue, out d);
                if (d == 0) err.ErrorDescription = "Value must be a valid double";
            }
            
            return err;
        }




       
    }  

    


}
