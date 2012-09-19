using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.COBie
{
    public class COBieWorkbook : List<ICOBieSheet<COBieRow>> 
    {
        public ICOBieSheet<COBieRow> this[string sheetName]
        {
            get
            {
                return this.Where(r => sheetName.Equals(r.SheetName)).FirstOrDefault();
            }
        }

    }
}
