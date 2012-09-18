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
                int c = ParentSheet.KeyColumns.Count;
                if (c == 1)
                    return ParentSheet.KeyColumns[0].GetValue(this, null).ToString();
                else if (c > 1)
                {
                    List<string> keyValues = new List<string>(c);
                    foreach (var keyProp in ParentSheet.KeyColumns)
                    {
                        keyValues.Add(keyProp.GetValue(this, null).ToString());
                    }
                    return string.Join(";", keyValues);
                }
                return null;
            }
        }

        public string GetForeignKeyValue(int colIndex)
        {
            return ParentSheet.ForeignKeyColumns[colIndex].GetValue(this, null).ToString();
        }

        public COBieCell this[int i]
        {
            get
            {
                foreach (PropertyInfo propInfo in ParentSheet.Properties)
                {
                    object[] attrs = ParentSheet.Attributes[propInfo];
                    if (attrs != null && attrs.Length > 0)
                    {
                        if (((COBieAttributes)attrs[0]).Order == i)
                        {
                            object pVal = propInfo.GetValue(this, null);
                            COBieCell cell;

                            if (pVal != null)
                            {
                                cell = new COBieCell(pVal.ToString());
                            }
                            else
                            {
                                cell = new COBieCell(COBieData.DEFAULT_STRING);
                            }
                            cell.COBieState = ((COBieAttributes)attrs[0]).State;
                            cell.CobieCol = ParentSheet.Columns[((COBieAttributes)attrs[0]).Order];
                            return cell;
                        }
                    }
                }
                return null;
            }
        }

        public COBieCell this[string name]
        {
            get
            {
                foreach (PropertyInfo propInfo in ParentSheet.Properties)
                {
                    object[] attrs = ParentSheet.Attributes[propInfo];
                    if (attrs != null && attrs.Length > 0)
                    {
                        if (((COBieAttributes)attrs[0]).ColumnName == name)
                        {
                            COBieCell cell = new COBieCell(propInfo.GetValue(this, null).ToString());
                            cell.COBieState = ((COBieAttributes)attrs[0]).State;
                            cell.CobieCol = ParentSheet.Columns[((COBieAttributes)attrs[0]).Order];

                            return cell;
                        }
                    }

                }
                return null;
            }
        }
    }
}
