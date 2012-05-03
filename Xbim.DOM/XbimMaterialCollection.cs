using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace Xbim.DOM
{
    public class XbimMaterialCollection : KeyedCollection<string,XbimMaterial>
    {
        protected override string GetKeyForItem(XbimMaterial item)
        {
            return item.Name;
        }

        public XbimMaterial GetMaterialByName(string name)
        {
            if (!Contains(name)) return null;
            return this[name];
        }
    }
}
