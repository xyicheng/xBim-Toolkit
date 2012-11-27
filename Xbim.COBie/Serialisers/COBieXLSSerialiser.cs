using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NPOI.HSSF.UserModel;
using NPOI.HSSF.Util;
using NPOI.SS.UserModel;
using Xbim.COBie;
using Xbim.COBie.Rows;
using Xbim.COBie.Contracts;


namespace Xbim.COBie.Serialisers
{
    /// <summary>
    /// Formats COBie data into an Excel XLS 
    /// </summary>
    public class COBieXLSSerialiser : ICOBieSerialiser
    {

        const string DefaultFileName = "Cobie.xls";
        const string DefaultTemplateFileName = @"Templates\COBie-UK-2012-template.xls";
        const string InstructionsSheet = "Instruction";
        const string ErrorsSheet = "Errors";

        Dictionary<COBieAllowedType, HSSFCellStyle> _cellStyles = new Dictionary<COBieAllowedType, HSSFCellStyle>();
        Dictionary<string, HSSFColor> _colours = new Dictionary<string, HSSFColor>();

        public COBieXLSSerialiser() : this(DefaultFileName, DefaultTemplateFileName)
        { }

        public COBieXLSSerialiser(string filename)
            : this(filename, DefaultTemplateFileName)
        { }

        public COBieXLSSerialiser(string fileName, string templateFileName)
        {
            FileName = fileName;
            TemplateFileName = templateFileName;
        }

        public string FileName { get; set; }
        public string TemplateFileName { get; set; }
        public HSSFWorkbook XlsWorkbook { get; private set; }

        /// <summary>
        /// Formats the COBie data into an Excel XLS file
        /// </summary>
        /// <param name="cobie"></param>
        public void Serialise(COBieWorkbook workbook)
        {
            if (workbook == null) { throw new ArgumentNullException("COBie", "COBieXLSSerialiser.Serialise does not accept null as the COBie data parameter."); }

            // Load template file
            FileStream excelFile = File.Open(TemplateFileName, FileMode.Open, FileAccess.Read);

            XlsWorkbook = new HSSFWorkbook(excelFile, true);

            CreateFormats();

            foreach (var sheet in workbook)
            {
                WriteSheet(sheet);
            }

            UpdateInstructions();

            ReportErrors(workbook);

            using (FileStream exportFile = File.Open(FileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                XlsWorkbook.Write(exportFile);
            }
        }

        /// <summary>
        /// Writes the Excel worksheet for this COBie sheet
        /// </summary>
        /// <param name="sheet"></param>
        private void WriteSheet(ICOBieSheet<COBieRow> sheet)
        {

            ISheet excelSheet = XlsWorkbook.GetSheet(sheet.SheetName) ?? XlsWorkbook.CreateSheet(sheet.SheetName);

            var datasetHeaders = sheet.Columns.Values.ToList();
            var sheetHeaders = GetTargetHeaders(excelSheet);
            ValidateHeaders(datasetHeaders, sheetHeaders, sheet.SheetName);


            // Enumerate rows
            for (int r = 0; r < sheet.RowCount; r++)
            {
                if (r >= UInt16.MaxValue)
                {
                    // TODO: Warn overflow of XLS 2003 worksheet
                    break;
                }

                COBieRow row = sheet[r];

                // GET THE ROW + 1 - This stops us overwriting the headers of the worksheet
                IRow excelRow = excelSheet.GetRow(r + 1) ?? excelSheet.CreateRow(r + 1);

                for (int c = 0; c < sheet.Columns.Count; c++)
                {
                    COBieCell cell = row[c];

                    ICell excelCell = excelRow.GetCell(c) ?? excelRow.CreateCell(c);

                    SetCellValue(excelCell, cell);
                    FormatCell(excelCell, cell);
                }
            }

            if ((sheet.RowCount == 0) &&
                (_colours.ContainsKey("Grey"))
                )
            {
                excelSheet.TabColorIndex = _colours["Grey"].GetIndex();
            }

            HighlightErrors(excelSheet, sheet);

            RecalculateSheet(excelSheet);
        }

        /// <summary>
        /// Creates an excel comment in each cell with an associated error
        /// </summary>
        /// <param name="excelSheet"></param>
        /// <param name="sheet"></param>
        private void HighlightErrors(ISheet excelSheet, ICOBieSheet<COBieRow> sheet)
        {
            // The patriarch is a container for comments on a sheet
            HSSFPatriarch patr = (HSSFPatriarch)excelSheet.CreateDrawingPatriarch();

            foreach (var error in sheet.Errors)
            {
                if (error.Row > 0 && error.Column >= 0)
                {
                    IRow excelRow = excelSheet.GetRow(error.Row);
                    if (excelRow != null)
                    {
                        ICell excelCell = excelRow.GetCell(error.Column);
                        if (excelCell != null)
                        {
                            if (excelCell.CellComment == null)
                            {
                                // A client anchor is attached to an excel worksheet. It anchors against a top-left and bottom-right cell.
                                // Create a comment 3 columns wide and 3 rows height
                                IComment comment = patr.CreateCellComment(new HSSFClientAnchor(0, 0, 0, 0, error.Column, error.Row, error.Column + 3, error.Row + 3));
                                comment.String = new HSSFRichTextString(error.ErrorDescription);
                                comment.Author = "XBim";
                                excelCell.CellComment = comment;
                            }
                            else
                            {
                                excelCell.CellComment.String = new HSSFRichTextString(excelCell.CellComment.String.ToString() + " Also " + error.ErrorDescription);
                            }
                            
                            
                        }
                        
                    }
                }
            }
        }

        private void CreateFormats()
        {
            CreateColours();
            // TODO : Date hardwired to Yellow/Required for now. Only Date is set up for now.
            CreateFormat(COBieAllowedType.ISODate, "yyyy-MM-dd", "Yellow");
            CreateFormat(COBieAllowedType.ISODateTime, "yyyy-MM-ddThh:mm:ss", "Yellow");
        }

        private void CreateColours()
        {
            CreateColours("Yellow", 0xFF, 0xFF, 0x99);
            CreateColours("Purple", 0xCC, 0x99, 0xFF);
            CreateColours("Green", 0xCC, 0xFF, 0xCC);
            CreateColours("Puce", 0xFF, 0xCC, 0x99);
            CreateColours("Grey", 0x96, 0x96, 0x96);
        }

        private void CreateColours(string colourName, byte red, byte green, byte blue)
        {
            HSSFPalette palette = XlsWorkbook.GetCustomPalette();
            HSSFColor colour = palette.FindSimilarColor(red, green, blue);
            if (colour == null)
            {
                // First 64 are system colours
                if  (NPOI.HSSF.Record.PaletteRecord.STANDARD_PALETTE_SIZE < 64 )
                {
                     NPOI.HSSF.Record.PaletteRecord.STANDARD_PALETTE_SIZE = 64; 
                }
                NPOI.HSSF.Record.PaletteRecord.STANDARD_PALETTE_SIZE++;
                colour = palette.AddColor(red, green, blue);
            }
            _colours.Add(colourName, colour);
        }

        private void CreateFormat(COBieAllowedType type, string formatString, string colourName)
        {
            HSSFCellStyle cellStyle;
            cellStyle = XlsWorkbook.CreateCellStyle() as HSSFCellStyle;
 
            HSSFDataFormat dataFormat = XlsWorkbook.CreateDataFormat() as HSSFDataFormat;
            cellStyle.DataFormat = dataFormat.GetFormat(formatString);
            
            cellStyle.FillForegroundColor = _colours[colourName].GetIndex();
            cellStyle.FillPattern = FillPatternType.SOLID_FOREGROUND;

            cellStyle.BorderBottom = BorderStyle.THIN;
            cellStyle.BorderLeft = BorderStyle.THIN;
            cellStyle.BorderRight = BorderStyle.THIN;
            cellStyle.BorderTop = BorderStyle.THIN;

            // TODO:maybe clone from the template?
            _cellStyles.Add(type, cellStyle);
        }

        private void UpdateInstructions()
        {
            ISheet instructionsSheet = XlsWorkbook.GetSheet(InstructionsSheet);

            if (instructionsSheet != null)
            {
                RecalculateSheet(instructionsSheet);
            }
        }

        private void ReportErrors(COBieWorkbook workbook)
        {
            ISheet errorsSheet = XlsWorkbook.GetSheet(ErrorsSheet) ?? XlsWorkbook.CreateSheet(ErrorsSheet); 

            foreach(var sheet in workbook.OrderBy(w=>w.SheetName))
            {
                if(sheet.SheetName != Constants.WORKSHEET_PICKLISTS)
                {
                    // Ensure the validation is up to date
                     sheet.Validate(workbook);
                }

                WriteErrors(errorsSheet, sheet.Errors);  
            }
        }


        int _row = 0;
        private void WriteErrors(ISheet errorsSheet, COBieErrorCollection errorCollection)
        {
            // Write Header

            var summary = errorCollection
                          .GroupBy(row => new {row.SheetName, row.FieldName, row.ErrorType} )
                          .Select(grp => new { grp.Key.SheetName, grp.Key.ErrorType, grp.Key.FieldName, Count = grp.Count() })
                          .OrderBy(r => r.SheetName);


            foreach(var error in summary)
            {
                
                IRow excelRow = errorsSheet.GetRow(_row + 1) ?? errorsSheet.CreateRow(_row + 1);
                int col = 0;

                ICell excelCell = excelRow.GetCell(col) ?? excelRow.CreateCell(col);
                excelCell.SetCellValue(error.SheetName);
                col++;

                excelCell = excelRow.GetCell(col) ?? excelRow.CreateCell(col);
                excelCell.SetCellValue(error.FieldName);
                col++;

                excelCell = excelRow.GetCell(col) ?? excelRow.CreateCell(col);
                excelCell.SetCellValue(error.ErrorType.ToString());
                col++;

                excelCell = excelRow.GetCell(col) ?? excelRow.CreateCell(col);
                excelCell.SetCellValue(error.Count);
                col++;
                
                _row++;
            }
            for (int c = 0 ; c < 3 ; c++)
            {
                errorsSheet.AutoSizeColumn(c);
            }

        }

        private void ValidateHeaders(List<COBieColumn> columns, List<string> sheetHeaders, string sheetName)
        {
            if (columns.Count != sheetHeaders.Count)
            {
                Console.WriteLine("Mis-matched number of columns in '{2}. {0} vs {1}", columns.Count, sheetHeaders.Count, sheetName);
            }

            for (int i = 0; i < columns.Count; i++)
            {
                if (!columns[i].IsMatch(sheetHeaders[i]))
                {
                    Console.WriteLine(@"{2} column {3}
Mismatch: {0}
          {1}",
              columns[i].ColumnName, sheetHeaders[i], sheetName, i);
                }
            }
        }



        
        private List<string> GetTargetHeaders(ISheet excelSheet)
        {
            List<string> headers = new List<string>();

            IRow headerRow = excelSheet.GetRow(0);
            if (headerRow == null)
                return headers;

            foreach (ICell cell in headerRow.Cells)
            {
                headers.Add(cell.StringCellValue);
            }
            return headers;

        }

        private void FormatCell(ICell excelCell, COBieCell cell)
        {
            HSSFCellStyle style;
            if (_cellStyles.TryGetValue(cell.CobieCol.AllowedType, out style))
            {
                excelCell.CellStyle = style;
            }

        }

        private void SetCellValue(ICell excelCell, COBieCell cell)
        {
            if (SetCellTypedValue(excelCell, cell) == false)
            {
                excelCell.SetCellValue(cell.CellValue);
            }
        }

        private bool SetCellTypedValue(ICell excelCell, COBieCell cell)
        {
            bool processed = false;

            try
            {
                if (String.IsNullOrEmpty(cell.CellValue) || cell.CellValue == Constants.DEFAULT_STRING)
                {
                    return false;
                }

                // We need to set the value in the most appropriate overload of SetCellValue, so the parsing/formatting is correct
                switch (cell.CobieCol.AllowedType)
                {
                    case COBieAllowedType.ISODateTime:
                    case COBieAllowedType.ISODate:
                        DateTime date;
                        if (DateTime.TryParse(cell.CellValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out date))
                        {
                            excelCell.SetCellValue(date);
                            processed = true;
                        }
                        break;

                    case COBieAllowedType.Numeric:
                        Double val;
                        if (Double.TryParse(cell.CellValue, out val))
                        {
                            excelCell.SetCellValue(val);
                            processed = true;
                        }
                        break;

                    default:
                        break;
                }
            }
            catch (SystemException)
            { /* Carry on */ }

            return processed;
        }


        
        private static void RecalculateSheet(ISheet excelSheet)
        {
            // Ensures the spreadsheet formulas will be recalulated the next time the file is opened
            excelSheet.ForceFormulaRecalculation = true;
            excelSheet.SetActiveCell(1, 0);

        }
    }
}
