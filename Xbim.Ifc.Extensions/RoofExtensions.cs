using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.SharedBldgElements;
using Xbim.Ifc.Kernel;

namespace Xbim.Ifc.Extensions
{
    public static class RoofExtensions
    {
        public static IEnumerable<IfcSlab> GetSlabs(this IfcRoof roof)
        {
            IEnumerable<IfcRelDecomposes> slabs = roof.IsDecomposedBy;
            return slabs.SelectMany(rel=>rel.RelatedObjects.OfType<IfcSlab>());
        }
    }
}
