using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection; 

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

                        // if this cell is set as primary key then check its value with other cells
                        if (cell.CobieCol.KeyType == COBieKeyType.PrimaryKey)
                        {
                            if (HasDuplicateValues(cell))
                            {
                                err.ErrorDescription = cell.CellValue + " duplication";
                                err.ErrorType = COBieError.ErrorTypes.PrimaryKey_Violation;
                            }
                        }

                        if (err.ErrorType != COBieError.ErrorTypes.None) errors.Add(err);
                    }
                }
            }
        }

        private bool HasDuplicateValues(COBieCell cell)
        {
            int count = 0;
            string val = cell.CellValue;
            string colName = cell.CobieCol.ColumnName;
            foreach (T row in Rows)
            {
                //if (row.Name == val) count++;
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

            DateTime dt;
            DateTime.TryParse(cell.CellValue, out dt);
            if (allowedType == COBieAllowedType.ISODate && dt == DateTime.MinValue) err.ErrorDescription = "Value must be a valid iso date";

            double d;
            double.TryParse(cell.CellValue, out d);
            if (allowedType == COBieAllowedType.Numeric && d == 0) err.ErrorDescription = "Value must be a valid double";

            return err;
        }
    }  
}
