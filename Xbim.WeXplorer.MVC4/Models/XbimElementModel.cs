using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.Extensions;

namespace Xbim.WeXplorer.MVC4.Models
{
    public class XbimElementModel : XbimRootModel
    {
        private IfcElement Element { get { return _root as IfcElement; } }
        public XbimElementModel(IfcElement element) : base(element)
        {
        }

        public IEnumerable<int> PropertySets {
            get
            {
                var pSets = Element.GetAllPropertySets();
                foreach (var pSet in pSets)
                    yield return Math.Abs(pSet.EntityLabel);
            }
        }
    }
}