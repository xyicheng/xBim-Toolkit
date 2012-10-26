using System;
using System.Collections.ObjectModel;

namespace Xbim.COBie
{
    public class COBieColumnCollection : KeyedCollection<string, COBieColumn>
    {
        protected override string GetKeyForItem(COBieColumn item)
        {
            return item.ColumnName;
        }
    }
}
