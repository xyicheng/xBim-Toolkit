using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.IO;

namespace Xbim.IO.GroupingAndStyling
{
    public interface IGeomHandlesGrouping
    {
        Dictionary<string, XbimGeometryHandleCollection> GroupLayers(XbimGeometryHandleCollection handles);
    }
}
