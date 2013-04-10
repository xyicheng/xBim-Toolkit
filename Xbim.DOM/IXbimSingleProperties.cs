using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.DOM.PropertiesQuantities;

namespace Xbim.DOM
{
    public interface IXbimSingleProperties : IBimSingleProperties
    {
        XbimSimplePropertySets PropertySets { get; }

        
    }
}
