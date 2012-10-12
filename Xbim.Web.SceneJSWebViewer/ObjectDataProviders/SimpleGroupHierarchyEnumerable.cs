using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;

namespace Xbim.SceneJSWebViewer.ObjectDataProviders
{
    public class SimpleGroupHierarchyEnumerable : IHierarchicalEnumerable 
    {
        IEnumerable<SimpleGroup> _groups;

        public SimpleGroupHierarchyEnumerable(IEnumerable<SimpleGroup> groups)
        {
            _groups = groups;
        }

        public IHierarchyData GetHierarchyData(object enumeratedItem)
        {
            SimpleGroup group = enumeratedItem as SimpleGroup;
            if (group == null) return null;
            return group;
        }

        public System.Collections.IEnumerator GetEnumerator()
        {
            return _groups.GetEnumerator();
        }
    }
}