using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace Xbim.COBie
{
    public interface ICOBieSheet<out T> where T : COBieRow
    {
        PropertyInfo[] Properties { get; }
        Dictionary<int, COBieColumn> Columns { get; }
        Dictionary<PropertyInfo, object[]> Attributes{ get; }
        List<PropertyInfo> KeyColumns { get; }
    }
}
