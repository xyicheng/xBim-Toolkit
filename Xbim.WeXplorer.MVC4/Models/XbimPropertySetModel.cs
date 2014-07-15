using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.PropertyResource;

namespace Xbim.WeXplorer.MVC4.Models
{
    public class XbimPropertySetModel : XbimRootModel
    {
        public IEnumerable<int> Properties
        {
            get 
            {
                foreach (var prop in _pSet.HasProperties.OfType<IfcPropertySingleValue>())
                {
                    yield return prop.EntityLabel;
                }
            }
        }

        private IfcPropertySet _pSet;
        public XbimPropertySetModel(IfcPropertySet pSet) : base(pSet)
        {
            _pSet = pSet;
        }
    }
}