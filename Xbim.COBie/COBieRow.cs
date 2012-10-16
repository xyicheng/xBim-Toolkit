using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ExternalReferenceResource;
using Xbim.COBie.Data;

namespace Xbim.COBie
{
	/// <summary>
	/// Abstract base class for Rows
	/// </summary>
    [Serializable()]
    public abstract class COBieRow
    {
        public ICOBieSheet<COBieRow> ParentSheet;


        public COBieRow(ICOBieSheet<COBieRow> parentSheet)
        {
            ParentSheet = parentSheet;
        }

        public string GetPrimaryKeyValue
        {
            get
            {
                int c = ParentSheet.KeyColumns.Count();
                if (c == 1)
                    return ParentSheet.KeyColumns.First().PropertyInfo.GetValue(this, null) as String;
                else if (c > 1)
                {
                    List<string> keyValues = new List<string>(c);
                    foreach (var keyProp in ParentSheet.KeyColumns)
                    {
                        if (keyProp.PropertyInfo.GetValue(this, null) != null)
                            keyValues.Add(keyProp.PropertyInfo.GetValue(this, null).ToString());
                        else
                            keyValues.Add("");
                        
                    }
                    return string.Join(";", keyValues);
                }
                return null;
            }
        }

        public COBieCell this[int i]
        {
            get
            {
                COBieColumn cobieColumn = ParentSheet.Columns.Where(idxcol => idxcol.Key == i).Select(idxcol => idxcol.Value).FirstOrDefault();
                if (cobieColumn != null)
                {
                    object pVal = cobieColumn.PropertyInfo.GetValue(this, null);
                    COBieCell thiscell = new COBieCell();
                    thiscell.CellValue = (pVal != null) ? pVal.ToString() : Constants.DEFAULT_STRING;
                    thiscell.COBieState = cobieColumn.AttributeState;
                    thiscell.CobieCol = cobieColumn;
                    return thiscell;
                }
                
                
                return null;
            }
            set
            {
                COBieColumn cobieColumn = ParentSheet.Columns.Where(idxcol => idxcol.Key == i).Select(idxcol => idxcol.Value).FirstOrDefault();
                if (cobieColumn != null)
                    cobieColumn.PropertyInfo.SetValue(this, value.CellValue, null);

            }
            
        }

        

        public COBieCell this[string name]
        {
            get
            {   
                COBieColumn cobieColumn = ParentSheet.Columns.Where(idxcol => idxcol.Value.ColumnName == name).Select(idxcol => idxcol.Value).FirstOrDefault();
                if (cobieColumn != null)
                {
                    object pVal = cobieColumn.PropertyInfo.GetValue(this, null);
                    COBieCell thiscell = new COBieCell();
                    thiscell.CellValue = (pVal != null) ? pVal.ToString() : Constants.DEFAULT_STRING ;
                    thiscell.COBieState = cobieColumn.AttributeState;
                    thiscell.CobieCol = cobieColumn;
                    return thiscell;
                }
                return null;
            }
        }

        public int RowCount
        {
            get
            {
                return ParentSheet.Columns.Count;
            }
        }

    }
}
