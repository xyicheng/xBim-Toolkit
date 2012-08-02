using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;


namespace Xbim.COBie
{
    public class COBieSheet<CobieRowType> where CobieRowType : COBieRow
    {
        public List<CobieRowType> Rows { get; set; }
        //public Dictionary<int, COBieColumn> Columns;

        public COBieSheet()
        {
            Rows = new List<CobieRowType>();
        }

        public void Validate(out List<COBieError> errors)
        {
            errors = new List<COBieError>();

            // loop through all the sheets and preopare error dataset
            if (Rows.Count > 0)
            {
                // loop through each floor row
                Type t = Rows[0].GetType();
                IEnumerable<PropertyInfo> Properties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                .Where(prop => prop.GetSetMethod() != null);

                foreach (CobieRowType row in Rows)
                {
                    // loop through each column, get its attributes and check if column value matches the attributes constraints
                    foreach (PropertyInfo propInfo in Properties)
                    {
                        object[] attrs = propInfo.GetCustomAttributes(typeof(COBieAttributes), true);
                        if (attrs != null && attrs.Length > 0)
                        {
                            Type cobieType = row.GetType();
                            PropertyInfo pinfo = cobieType.GetProperty(propInfo.Name);
                            string val = (pinfo.GetValue(row, null) == null) ? "" : pinfo.GetValue(row, null).ToString();
                            COBieCell cell = new COBieCell(val);
                            cell.COBieState = ((COBieAttributes)attrs[0]).State;
                            cell.CobieCol = new COBieColumn(((COBieAttributes)attrs[0]).ColumnName, ((COBieAttributes)attrs[0]).MaxLength, ((COBieAttributes)attrs[0]).AllowedType, ((COBieAttributes)attrs[0]).KeyType);
                            COBieError err = GetCobieError(cell, "COBieFloor");

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
        }

        private bool HasDuplicateValues(COBieCell cell)
        {
            int count = 0;
            string val = cell.CellValue;
            string colName = cell.CobieCol.ColumnName;
            foreach (CobieRowType row in Rows)
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
