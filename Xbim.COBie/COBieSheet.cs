using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Data.SQLite;
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
                }
            }
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

        public void Validate(out List<COBieError> errors)
        {
            errors = new List<COBieError>();
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

                        if (err.ErrorType != COBieError.ErrorTypes.None) errors.Add(err);
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
                    errors.Add(err);
                }
            }

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

        public void CreateEmptyTable(string dbName, string tableName, string connectionString)
        {
            // delete table if exist first
            ExecuteQuery("DROP TABLE IF EXISTS '" + tableName + "';", connectionString);

            string createStatement = "";
            string colNames = "";
            foreach (PropertyInfo propInfo in Properties)
                colNames = colNames + propInfo.Name + " VARCHAR(256),";                
            
            colNames = colNames.TrimEnd(',');
                        
            if (!string.IsNullOrEmpty(colNames))
            {
                createStatement = "create table " + tableName + " (" + colNames + ");";
                ExecuteQuery(createStatement, connectionString);
            }
            
        }

        public void InsertValuesInDB(string dbName, string tableName, string connectionString)
        {
            if (IsTableExist(dbName, tableName, connectionString))
            {
                foreach (T row in Rows)
                {
                    // loop through each column, get its attributes and check if column value matches the attributes constraints
                    string insertStatement = "INSERT INTO " + tableName;
                    string insertColumns = "(";
                    string insertColumnValues = "(";

                    foreach (PropertyInfo propInfo in Properties)
                    {
                        object[] attrs = Attributes[propInfo];
                        if (attrs != null && attrs.Length > 0)
                        {
                            COBieAttributes attr = (COBieAttributes)attrs[0];

                            string val = (propInfo.GetValue(row, null) == null) ? "" : propInfo.GetValue(row, null).ToString();
                            val = val.Replace("'", "''");
                            insertColumns = insertColumns + attr.ColumnName + ",";
                            insertColumnValues = insertColumnValues + "'" + val + "',";
                        }
                    }

                    insertColumns = insertColumns.TrimEnd(',');
                    insertColumns = insertColumns + ")";
                    insertColumnValues = insertColumnValues.TrimEnd(',');
                    insertColumnValues = insertColumnValues + ")";                    

                    insertStatement = insertStatement + insertColumns + " VALUES " + insertColumnValues + ";";

                    ExecuteQuery(insertStatement, connectionString);
                }
            }
            
        }

        private void ExecuteQuery(string txtQuery, string connectionString)
        {
            SQLiteConnection cn;
            using (cn = new SQLiteConnection(connectionString))
            {
                using (SQLiteCommand cmd = cn.CreateCommand())
                {
                    cmd.CommandText = txtQuery;
                    //cmd.CommandType = CommandType.Text;
                    cn.Open();
                    cmd.ExecuteNonQuery();
                    cn.Close();
                }
            }
        }

        private bool IsTableExist(string dbName, string tableName, string connectionString)
        {
            string query = "SELECT name FROM sqlite_master WHERE type='table' AND name='" + tableName + "';";
            SQLiteConnection cn;
            bool blnResult = false;
            using (cn = new SQLiteConnection(connectionString))
            {
                using (SQLiteCommand cmd = cn.CreateCommand())
                {
                    cmd.CommandText = query;
                    cmd.CommandType = CommandType.Text;
                    cn.Open();
                    SQLiteDataReader dr = cmd.ExecuteReader();
                    
                    if (dr != null)
                    {
                        blnResult = dr.HasRows;
                    }
                    dr.Close();
                    cn.Close();
                }
            }

            return blnResult;
        }

        public List<COBieCell> GetPrimaryKeyErrorCells(string connectionString, string tableName)
        {
            List<COBieCell> errorCells = new List<COBieCell>();

            string primaryColumnName = "";
            foreach (PropertyInfo propInfo in Properties)
            {
                object[] attrs = Attributes[propInfo];
                if (attrs != null && attrs.Length > 0)
                {
                    COBieAttributes attr = (COBieAttributes)attrs[0];
                    if (attr.KeyType == COBieKeyType.PrimaryKey)
                    {
                        primaryColumnName = propInfo.Name;
                        break;
                    }
                    
                }
            }

            string query = "SELECT " + primaryColumnName + ", Count(" + primaryColumnName + ") as Total FROM " + tableName + " Group By " + primaryColumnName + ";";
            SQLiteConnection cn;
            
            using (cn = new SQLiteConnection(connectionString))
            {
                using (SQLiteCommand cmd = cn.CreateCommand())
                {
                    cmd.CommandText = query;
                    cmd.CommandType = CommandType.Text;
                    cn.Open();
                    SQLiteDataReader dr = cmd.ExecuteReader();

                    if (dr != null)
                    {
                        // find out if any of the count columns has more than 1 value, if yes then that value is duplicate
                        
                    }
                    dr.Close();
                    cn.Close();
                }
            }

            return errorCells;
        }
    }  

    


}
