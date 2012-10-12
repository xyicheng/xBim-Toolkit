using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Xbim.SceneJSWebViewer.XML
{
    public class Group
    {
        public string groupName;
        public List<GroupItem> groupItems; 
    }

    public class GroupItem
    {
        public string objName;
        public string objTypeName;
        public string objGuid;
    }
}