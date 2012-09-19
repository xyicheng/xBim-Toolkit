using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Data;
using Xbim.COBie.Resources;

namespace Xbim.COBie
{
    public class COBieSheet<T> : ICOBieSheet<T> where T : COBieRow
    {
		public string SheetName { get; private set; }
        public List<T> Rows { get; private set; }

        public Dictionary<int, COBieColumn> Columns { get { return _columns; } }
        public PropertyInfo[] Properties { get { return _properties; } }
        public Dictionary<PropertyInfo, object[]> Attributes { get { return _attributes; } }
        
        private Dictionary<int, COBieColumn> _columns;
        private PropertyInfo[] _properties;
        private Dictionary<PropertyInfo, object[]> _attributes;
        private List<PropertyInfo> _keyColumns = new List<PropertyInfo>();
        private List<PropertyInfo> _foreignKeyColumns = new List<PropertyInfo>();

        private COBieErrorCollection _errors = new COBieErrorCollection();

        public COBieErrorCollection Errors
        {
            get
            {
                return _errors;
            }
        }

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

        public T this[int index]
        {
            get
            {
                return Rows[index];
            }
        }

        public int RowCount
        {
            get
            {
                return Rows.Count;
            }
        }

        public bool HasPrimaryKey
        {
            get
            {
                return _keyColumns.Any();
            }
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

        public void Validate()
        {
            ValidateFields();
            ValidatePrimaryKeys();
            //ValidateForeignKeys();

        }

        public void ValidateFields()
        {
            int r = 0;
            foreach(T row in Rows)
            {
                r++;
                for(int col = 0 ; col < row.RowCount ; col++)
                {
                    COBieError err = GetCobieError(row[col], SheetName, r, col);
                    if (err != null)
                        _errors.Add(err);
                }
            }
        }

        private void ValidatePrimaryKeys()
        {
            var dupes = Rows
                        .GroupBy(r => r.GetPrimaryKeyValue)
                        .Where(grp => grp.Count() > 1)
                        .SelectMany(grp => grp);

            foreach (var dupe in dupes)
            {
                // TODO: need to identify the row number so we can assign error message
                string description = ErrorDescription.PrimaryKey_Violation + ": " + dupe.GetPrimaryKeyValue;
                    COBieError error = new COBieError(SheetName, string.Join(";", _keyColumns.Select(s=>s.Name)), description, COBieError.ErrorTypes.PrimaryKey_Violation);
                    _errors.Add(error);
            }

        }

        private void ValidateForeignKey(List<string> colMain, List<string> colForeign)
        {
            // E.g.
            // SELECT Facility.CreatedBy, Contact.Email FROM Contact
            // Left Outer Join Facility On Contact.Email = Facility.CreatedBy 
            // WHERE Facility.CreatedBy = null 

            var query = from cm in colMain
                        join cf in colForeign on cm equals cf into gj
                        from subpet in gj.DefaultIfEmpty()
                        select new { cm, ColForeign = (subpet == null ? String.Empty : subpet) };


            
            foreach (var r in query)
            {
                if (string.IsNullOrEmpty(r.ColForeign))
                {
                    string description = ErrorDescription.Null_ForeignKey_Value + ": " + r.cm.ToString();
                    COBieError error = new COBieError(SheetName, "", description, COBieError.ErrorTypes.Null_ForeignKey_Value);
                    _errors.Add(error);
                }
            }

        }

        //public List<string> GetForeignKeyValues(string foreignColName)
        //{
        //    List<string> colValues = new List<string>();
        //    foreach (T row in Rows)
        //    {
        //        // loop through each column, get its attributes and check if column value matches the attributes constraints
        //        foreach (PropertyInfo propInfo in Properties)
        //        {
        //            object[] attrs = Attributes[propInfo];
        //            if (attrs != null && attrs.Length > 0)
        //            {
        //                COBieAttributes attr = (COBieAttributes)attrs[0];
        //                if (attr.ColumnName == foreignColName)
        //                {
        //                    Type cobieType = row.GetType();
        //                    string val = (propInfo.GetValue(row, null) == null) ? "" : propInfo.GetValue(row, null).ToString();

        //                    colValues.Add(val);
        //                }

        //            }
        //        }
        //    }
        //    if (colValues.Count > 0)
        //        return colValues;
        //    else
        //        return null;
        //}
            


        private COBieError GetCobieError(COBieCell cell, string sheetName, int row, int col)
        {
            int maxLength = cell.CobieCol.ColumnLength;
            COBieAllowedType allowedType = cell.CobieCol.AllowedType;
            COBieAttributeState state = cell.COBieState;
            COBieError err = new COBieError(sheetName, cell.CobieCol.ColumnName, "", COBieError.ErrorTypes.None, col, row);
      

            if (state == COBieAttributeState.Required && string.IsNullOrEmpty(cell.CellValue))
            {
                err.ErrorDescription = ErrorDescription.Text_Value_Expected;
                err.ErrorType = COBieError.ErrorTypes.Text_Value_Expected;
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

            else if (allowedType == COBieAllowedType.ISODate)
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


       
    }  

}
