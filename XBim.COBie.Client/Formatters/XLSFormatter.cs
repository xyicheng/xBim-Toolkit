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

        /// <summary>
        /// Formats the COBie data into an Excel XLS file
        /// </summary>
        /// <param name="cobie"></param>
        public void Format(COBieReader cobie)
        {
            if (cobie == null) { throw new ArgumentNullException("cobie", "XLSFormatter.Format does not accept null as the COBie data parameter."); }

            // Load template file
            FileStream excelFile = File.Open(TemplateFileName, FileMode.Open, FileAccess.Read);

            HSSFWorkbook workbook = new HSSFWorkbook(excelFile, true);            

            // Generically write the sheet
            WriteSheet<COBieContactRow>(cobie.CobieContacts, workbook);
            WriteSheet<COBieFacilityRow>(cobie.CobieFacilities, workbook);
            WriteSheet<COBieFloorRow>(cobie.CobieFloors, workbook);
            WriteSheet<COBieSpaceRow>(cobie.CobieSpaces, workbook);
            WriteSheet<COBieZoneRow>(cobie.CobieZones, workbook);
            WriteSheet<COBieTypeRow>(cobie.CobieTypes, workbook);
            WriteSheet<COBieComponentRow>(cobie.CobieComponents, workbook);
            WriteSheet<COBieSystemRow>(cobie.CobieSystems, workbook);
            WriteSheet<COBieAssemblyRow>(cobie.CobieAssemblies, workbook);
            WriteSheet<COBieConnectionRow>(cobie.CobieConnections, workbook);
            WriteSheet<COBieSpareRow>(cobie.CobieSpares, workbook);
            WriteSheet<COBieResourceRow>(cobie.CobieResources, workbook);
            WriteSheet<COBieJobRow>(cobie.CobieJobs, workbook);
            WriteSheet<COBieImpactRow>(cobie.CobieImpacts, workbook);
            WriteSheet<COBieDocumentRow>(cobie.CobieDocuments, workbook);
            WriteSheet<COBieAttributeRow>(cobie.CobieAttributes, workbook);
            WriteSheet<COBieCoordinateRow>(cobie.CobieCoordinates, workbook);
            WriteSheet<COBieIssueRow>(cobie.CobieIssues, workbook);
            WriteSheet<COBiePickListsRow>(cobie.CobiePickLists, workbook);

            UpdateInstructions(workbook);

            using (FileStream exportFile = File.Open(FileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                workbook.Write(exportFile);
            }
        }

        private void UpdateInstructions(HSSFWorkbook workbook)
        {
            NPOI.SS.UserModel.ISheet instructionsSheet = workbook.GetSheet(InstructionsSheet);

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
        private void WriteSheet<TCOBieRowType>(COBieSheet<TCOBieRowType> sheet, HSSFWorkbook workbook) where TCOBieRowType : COBieRow
        {
            
            NPOI.SS.UserModel.ISheet excelSheet = workbook.GetSheet(sheet.SheetName) ?? workbook.CreateSheet(sheet.SheetName);

            // Enumerate rows
            for (int r = 0; r < sheet.Rows.Count; r++)
            {
                TCOBieRowType row = sheet.Rows[r];

                // GET THE ROW + 1 - This stops us overwriting the headers of the worksheet
                NPOI.SS.UserModel.IRow excelRow = excelSheet.GetRow(r + 1) ?? excelSheet.CreateRow(r + 1);

                for (int c = 0; c < sheet.Columns.Count; c++)
                {
                    COBieCell cell = row[c];

                    NPOI.SS.UserModel.ICell excelCell = excelRow.GetCell(c) ?? excelRow.CreateCell(c);
                 
                    SetCellValue(excelCell, cell);
                }
            }

            RecalculateSheet(excelSheet);
        }

        private void SetCellValue(NPOI.SS.UserModel.ICell excelCell, COBieCell cell)
        {
            try
            {
                if (String.IsNullOrEmpty(cell.CellValue) || cell.CellValue == COBieData.DEFAULT_STRING)
                {
                    excelCell.SetCellValue(cell.CellValue);
                    return;
                }

                // We need to set the value in the most appropriate overload of SetCellValue, so the parsing/formatting is correct
                switch (cell.CobieCol.AllowedType)
                {
                    case COBieAllowedType.ISODate:
                        excelCell.SetCellValue(DateTime.Parse(cell.CellValue, CultureInfo.InvariantCulture));
                        break;

                    case COBieAllowedType.Numeric:
                        excelCell.SetCellValue(Double.Parse(cell.CellValue, CultureInfo.InvariantCulture));
                        break;

                    default:
                        excelCell.SetCellValue(cell.CellValue);
                        break;
                }
            }
            catch (SystemException)
            {
                excelCell.SetCellValue(cell.CellValue);
            }
        }

        
        private static void RecalculateSheet(NPOI.SS.UserModel.ISheet excelSheet)
        {
            // Ensures the spreadsheet formulas will be recalulated the next time the file is opened
            excelSheet.ForceFormulaRecalculation = true;
            excelSheet.SetActiveCell(1, 0);

        }
    }
}
