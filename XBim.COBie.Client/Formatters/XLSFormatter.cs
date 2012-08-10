using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie;
using System.IO;
using NPOI.HSSF.UserModel;
using Xbim.COBie.Rows;

namespace XBim.COBie.Client.Formatters
{
    /// <summary>
    /// Formats COBie data into an Excel XLS 
    /// </summary>
    public class XLSFormatter : ICOBieFormatter
    {
        /// <summary>
        /// Formats the COBie data into an Excel XLS file
        /// </summary>
        /// <param name="cobie"></param>
        public void Format(COBieReader cobie)
        {
            if (cobie == null) { throw new ArgumentNullException("cobie", "XLSFormatter.Format does not accept null as the COBie data parameter."); }

            // Load file
            FileStream excelFile = File.Open(@"Templates\COBie-UK-2012-template.xls", FileMode.Open, FileAccess.Read);

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
            // Etc ...

            using (FileStream exportFile = File.Open("COBie.xls", FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                workbook.Write(exportFile);
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
            if (sheet.Rows.Count != 0)
            {
                NPOI.SS.UserModel.ISheet excelSheet = workbook.GetSheet(sheet.SheetName) ?? workbook.CreateSheet(sheet.SheetName);

                // Enumerate rows
                for (int r = 0; r < sheet.Rows.Count; r++)
                {
                    TCOBieRowType row = sheet.Rows[r];

                    // GET THE ROW + 1 - This stops us overwriting the headers of the worksheet!!!
                    NPOI.SS.UserModel.IRow excelRow = excelSheet.GetRow(r + 1) ?? excelSheet.CreateRow(r + 1);

                    for (int c = 0; c < sheet.Columns.Count; c++)
                    {
                        COBieCell cell = row[c];

                        NPOI.SS.UserModel.ICell excelCell = excelRow.GetCell(c) ?? excelRow.CreateCell(c);

                        excelCell.SetCellValue(cell.CellValue);
                    }
                }
            }
        }
    }
}
