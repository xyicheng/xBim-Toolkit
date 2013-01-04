using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.UtilityResource;

namespace Xbim.DOM
{
    public interface IXbimRoot
    {
        IfcRoot AsRoot { get; }
        string GlobalId { get; }
        Guid Guid { get; set; }
        void SetGlobalId(Guid guid);
        XbimDocument Document { get; }
    }
}
