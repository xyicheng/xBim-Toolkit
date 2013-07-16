using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Kernel;

namespace Xbim.Analysis
{
    public interface IModelComparer
    {
        Dictionary<IfcProduct, ChangeType> Compare(IEnumerable<IfcProduct> Baseline, IEnumerable<IfcProduct> Delta);
        Dictionary<Int32, Int32> GetMap();
    }
    public enum ChangeType { 
        Added,
        Deleted,
        Modified,
        Matched,
        Unknown
    }
}
