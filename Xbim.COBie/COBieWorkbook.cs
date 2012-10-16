using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.COBie
{
    [Serializable()]
    public class COBieWorkbook : List<ICOBieSheet<COBieRow>> 
    {
        public ICOBieSheet<COBieRow> this[string sheetName]
        {
            get
            {
                return this.Where(r => sheetName.Equals(r.SheetName)).FirstOrDefault();
            }
        }


        internal void CreateIndices()
        {
            foreach (ICOBieSheet<COBieRow> item in this)
            {
                item.BuildIndices();
            }
        }

        
    }
}
