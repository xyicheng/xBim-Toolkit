using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using NPOI.HSSF.UserModel;
using NPOI.HSSF.Util;
using NPOI.SS.UserModel;
using Xbim.COBie.Rows;

namespace Xbim.COBie
{
	/// <summary>
	/// Pick List, indexed on the column name, values indexed on the row name
	/// </summary>
    public class COBiePickListReader 
    {
        const string sheetName = "PickLists";
        public string TemplateFileName { get; set; }
        public HSSFWorkbook XlsWorkbook { get; private set; }

        public COBiePickListReader(string templateFileName)
        {
            TemplateFileName = templateFileName;
        }

        public COBieSheet<COBiePickListsRow> Read()
        {
            COBieSheet<COBiePickListsRow> pickLists = new COBieSheet<COBiePickListsRow>(Constants.WORKSHEET_PICKLISTS);
            int COBieColumnCount = pickLists.Columns.Count;    
            try
            {
                using (FileStream excelFile = File.Open(TemplateFileName, FileMode.Open, FileAccess.Read))
                {
                    XlsWorkbook = new HSSFWorkbook(excelFile, true);
                }
                ISheet excelSheet = XlsWorkbook.GetSheet(sheetName);
                if (excelSheet != null)
                {
                    int rownumber = 0;
                    int columnCount = 0;
                    foreach (IRow row in excelSheet)
                    {
                        if (rownumber == 0) //this will be the headers so get how many we have
                        {
                            foreach (ICell cell in row)
                            {
                                columnCount++;
                            }
                        }
                        else
                        {
                            COBiePickListsRow pickList = new COBiePickListsRow(pickLists);
                            for (int i = 0; i < columnCount; i++)
                            {
                                string cellValue = ""; //default value
                                ICell cell = row.GetCell(i);
                                if (cell != null)
                                    cellValue = cell.StringCellValue;
                                if (i < COBieColumnCount)
                                    pickList[i] = new COBieCell(cellValue);
                            }
                            pickLists.Rows.Add(pickList);
                        }
                        rownumber++;
                    } 
                }
            }
            catch (FileNotFoundException )
            {
                //TODO: Report this
                throw;
            }
            catch (Exception)
            {
                throw;
            }
            return pickLists;
        }


    }
}
