using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Xbim.COBie
{
	/// <summary>
	/// Abstract base class for Rows
	/// </summary>
    abstract public class COBieRow
    {
		public Dictionary<int, COBieColumn> Columns;
        public PropertyInfo[] Properties;

		/// <summary>
		/// Instantiates the COBieRow
		/// </summary>
		public COBieRow()
		{
			Columns = new Dictionary<int, COBieColumn>();
			Properties = null;
		}
   
		/// <summary>
		/// Returns the item at the given index
		/// </summary>
		/// <param name="i">The index</param>
		/// <returns>A COBieCell or null</returns>
        public COBieCell this[int i]
        {
            get
            {
                foreach (PropertyInfo propInfo in Properties)
                {
                    object[] attrs = propInfo.GetCustomAttributes(typeof(COBieAttributes), true);
                    if (attrs != null && attrs.Length > 0)
                    {
                        if (((COBieAttributes)attrs[0]).Order == i) // return (COBieCell)propInfo.GetValue(this, null);
                        {
                            //COBieCell cell = (COBieCell)propInfo.GetValue(this, null);
                            PropertyInfo pinfo = this.GetType().GetProperty(propInfo.Name);

							object pVal = pinfo.GetValue(this, null);
							
							COBieCell cell;

							if (pVal != null)
							{
								cell = new COBieCell(pinfo.GetValue(this, null).ToString());
								cell.COBieState = ((COBieAttributes)attrs[0]).State;
								cell.CobieCol = Columns[((COBieAttributes)attrs[0]).Order];
							}
							else
							{
								cell = new COBieCell("n/a");
							}


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
                foreach (PropertyInfo propInfo in Properties)
                {
                    object[] attrs = propInfo.GetCustomAttributes(typeof(COBieAttributes), true);
                    if (attrs != null && attrs.Length > 0)
                    {
                        if (((COBieAttributes)attrs[0]).ColumnName == name) // return (COBieCell)propInfo.GetValue(this, null);
                        {
                            //COBieCell cell = (COBieCell)propInfo.GetValue(this, null);

                            PropertyInfo pinfo = this.GetType().GetProperty(propInfo.Name);
                            COBieCell cell = new COBieCell(pinfo.GetValue(this, null).ToString());
                            cell.COBieState = ((COBieAttributes)attrs[0]).State;
                            cell.CobieCol = Columns[((COBieAttributes)attrs[0]).Order];



                            return cell;
                        }
                    }

                }

                return null;
            }
        }
    }
}
