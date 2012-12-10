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

        internal void SetInitialHashCode()
        {
            foreach (ICOBieSheet<COBieRow> item in this)
            {
                item.SetRowsHashCode();
            }
        }

        /// <summary>
        /// Runs validation rules on each sheet and updates the Errors collection
        /// on each sheet.
        /// </summary>
        public void Validate(Action<int> progressCallback = null)
        {
            // Enumerates the sheets and validates each
            foreach (var sheet in this)
            {
                if (sheet.SheetName != Constants.WORKSHEET_PICKLISTS) //skip validation on picklist
                {
                    sheet.Validate(this);
                }

                // Progress bar support
                if (progressCallback != null)
                {
                    // Callback with the index of the last processed sheet
                    progressCallback(this.IndexOf(sheet));
                }
            }
        }
    }
}
