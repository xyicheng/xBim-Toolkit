using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;


namespace Xbim.COBie
{
    public interface ICOBieSheet<out T> where T : COBieRow
    {

        T this[int i] { get; }
        int RowCount { get; }
        string SheetName { get; }
        Dictionary<int, COBieColumn> Columns { get; }
        IEnumerable<COBieColumn> KeyColumns { get; }
        IEnumerable<COBieColumn> ForeignKeyColumns { get; }
        Dictionary<string, HashSet<string>> Indices { get; }
        COBieErrorCollection Errors { get; }

        void Validate(COBieWorkbook workbook);
        void BuildIndices();
    }
}
