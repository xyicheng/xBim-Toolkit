using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie;
using System.IO;
using NPOI.HSSF.UserModel;
using Xbim.COBie.Rows;
using NPOI.SS.Format;
using System.Globalization;
using Xbim.COBie.Data;
using NPOI.SS.UserModel;
using NPOI.HSSF.Util;

namespace XBim.COBie.Client.Formatters
{
    /// <summary>
    /// Formats COBie data into an Excel XLS 
    /// </summary>
    public class XLSFormatter : ICOBieFormatter
    {

        const string DefaultFileName = "Cobie.xls";
        const string DefaultTemplateFileName = @"Templates\COBie-UK-2012-template.xls";
        const string InstructionsSheet = "Instruction";

        Dictionary<COBieAllowedType, HSSFCellStyle> _cellStyles = new Dictionary<COBieAllowedType, HSSFCellStyle>();
        Dictionary<string, HSSFColor> _colours = new Dictionary<string, HSSFColor>();

        public XLSFormatter() : this(DefaultFileName, DefaultTemplateFileName)
        { }

        public XLSFormatter(string filename)
            : this(filename, DefaultTemplateFileName)
        { }

        public XLSFormatter(string fileName, string templateFileName)
        {
            FileName = fileName;
            TemplateFileName = templateFileName;
        }

        public string FileName { get; set; }
        public string TemplateFileName { get; set; }
        public HSSFWorkbook Workbook { get; private set; }

        /// <summary>
        /// Formats the COBie data into an Excel XLS file
        /// </summary>
        /// <param name="cobie"></param>
        public void Format(COBieReader cobie)
        {
            if (cobie == null) { throw new ArgumentNullException("cobie", "XLSFormatter.Format does not accept null as the COBie data parameter."); }

            // Load template file
            FileStream excelFile = File.Open(TemplateFileName, FileMode.Open, FileAccess.Read);

            Workbook = new HSSFWorkbook(excelFile, true);

            CreateFormats();

            // Generically write the sheet
            WriteSheet<COBieContactRow>(cobie.CobieContacts);
            WriteSheet<COBieFacilityRow>(cobie.CobieFacilities);
            WriteSheet<COBieFloorRow>(cobie.CobieFloors);
            WriteSheet<COBieSpaceRow>(cobie.CobieSpaces);
            WriteSheet<COBieZoneRow>(cobie.CobieZones);
            WriteSheet<COBieTypeRow>(cobie.CobieTypes);
            WriteSheet<COBieComponentRow>(cobie.CobieComponents);
            WriteSheet<COBieSystemRow>(cobie.CobieSystems);
            WriteSheet<COBieAssemblyRow>(cobie.CobieAssemblies);
            WriteSheet<COBieConnectionRow>(cobie.CobieConnections);
            WriteSheet<COBieSpareRow>(cobie.CobieSpares);
            WriteSheet<COBieResourceRow>(cobie.CobieResources);
            WriteSheet<COBieJobRow>(cobie.CobieJobs);
            WriteSheet<COBieImpactRow>(cobie.CobieImpacts);
            WriteSheet<COBieDocumentRow>(cobie.CobieDocuments);
            WriteSheet<COBieAttributeRow>(cobie.CobieAttributes);
            WriteSheet<COBieCoordinateRow>(cobie.CobieCoordinates);
            WriteSheet<COBieIssueRow>(cobie.CobieIssues);
            WriteSheet<COBiePickListsRow>(cobie.CobiePickLists);

            UpdateInstructions();

            using (FileStream exportFile = File.Open(FileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                Workbook.Write(exportFile);
            }
        }

        private void CreateFormats()
        {
            CreateColours();
            // TODO : Date hardwired to Yellow/Required for now. Only Date is set up for now.
            CreateFormat(COBieAllowedType.ISODate, "yyyy-MM-ddThh:mm:ss", "Yellow");
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
            HSSFPalette palette = Workbook.GetCustomPalette();
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
            cellStyle = Workbook.CreateCellStyle() as HSSFCellStyle;
 
            HSSFDataFormat dataFormat = Workbook.CreateDataFormat() as HSSFDataFormat;
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
            ISheet instructionsSheet = Workbook.GetSheet(InstructionsSheet);

            if (instructionsSheet != null)
            {
                RecalculateSheet(instructionsSheet);
            }
        }

        /// <summary>
        /// Writes the Excel worksheet for this COBie sheet
        /// </summary>
        /// <typeparam name="TCOBieRowType">The type of the row that this COBie sheet contains</typeparam>
        /// <param name="sheet">The COBie sheet which contains the data to be written to the Excel worksheet</param>
        /// <param name="workbook">The Excel workbook</param>
        private void WriteSheet<TCOBieRowType>(COBieSheet<TCOBieRowType> sheet) where TCOBieRowType : COBieRow
        {
            
            ISheet excelSheet = Workbook.GetSheet(sheet.SheetName) ?? Workbook.CreateSheet(sheet.SheetName);

            var datasetHeaders = sheet.Columns.Values.ToList();
            var sheetHeaders = GetTargetHeaders(excelSheet);
            ValidateHeaders(datasetHeaders, sheetHeaders, sheet.SheetName);


            // Enumerate rows
            for (int r = 0; r < sheet.Rows.Count; r++)
            {
                if (r >= UInt16.MaxValue)
                {
                    // TODO: Warn overflow of XLS 2003 worksheet
                    break;
                }

                TCOBieRowType row = sheet.Rows[r];

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

            RecalculateSheet(excelSheet);
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
                if (String.IsNullOrEmpty(cell.CellValue) || cell.CellValue == COBieData.DEFAULT_STRING)
                {
                    return false;
                }

                // We need to set the value in the most appropriate overload of SetCellValue, so the parsing/formatting is correct
                switch (cell.CobieCol.AllowedType)
                {
                    case COBieAllowedType.ISODate:
                        DateTime date;
                        if (DateTime.TryParse(cell.CellValue, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out date))
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
