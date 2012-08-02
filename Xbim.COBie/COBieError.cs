using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.COBie
{
    public class COBieError
    {
        // this will contain all the cells that are mandatory but not supplied and user needs to fill in
        // as we need to generate spreadsheet even if there is missing mandatory data

        public COBieError()
        {
        }

        public COBieError(string sheetName, string fieldName, string errorDescription)
        {
            SheetName = sheetName;
            FieldName = fieldName;
            ErrorDescription = errorDescription;
        }

        public COBieError(string sheetName, string fieldName, string errorDescription, ErrorTypes errorType)
        {
            SheetName = sheetName;
            FieldName = fieldName;
            ErrorDescription = errorDescription;
            ErrorType = errorType;
        }

        public COBieError(string fieldName, string errorDescription)
        {
            FieldName = fieldName;
            ErrorDescription = errorDescription;
        }

        public string SheetName { get; set; }
        public string FieldName { get; set; }
        public string ErrorDescription { get; set; }
        public ErrorTypes ErrorType { get; set; }

        public enum ErrorTypes
        {
            Value_Out_of_Bounds,
            AlphaNumeric_Value_Expected,
            Email_Value_Expected,
            ISODate_Value_Expected,
            Numeric_Value_Expected,
            Text_Value_Expected,
            PrimaryKey_Violation,
            None
        }
    }
}
