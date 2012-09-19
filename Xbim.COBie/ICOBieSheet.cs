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
        PropertyInfo[] Properties { get; }
        Dictionary<int, COBieColumn> Columns { get; }
        Dictionary<PropertyInfo, object[]> Attributes{ get; }
        List<PropertyInfo> KeyColumns { get; }
        List<PropertyInfo> ForeignKeyColumns { get; }
        string SheetName { get; }

        COBieErrorCollection Errors { get; }

        void Validate();
    }
}
