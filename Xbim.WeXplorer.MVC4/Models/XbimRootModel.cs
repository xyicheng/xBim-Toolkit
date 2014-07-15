using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Xbim.Ifc2x3.Kernel;

namespace Xbim.WeXplorer.MVC4.Models
{
    public class XbimRootModel : IXbimJsonResult
    {
        public string Type { get { return _root.GetType().Name; } }

        public int? Label { get { return _root.EntityLabel; } }

        public string Name { get { return _root.Name; } }

        protected IfcRoot _root;
        public XbimRootModel(IfcRoot root)
        {
            _root = root;
        }
    }
}