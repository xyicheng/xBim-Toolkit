using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.Presentation
{
    public interface IXbimViewModel
    {
        IEnumerable Children { get; }
        string Name {get;}
        int EntityLabel { get; }
        IPersistIfcEntity Entity { get; }
    }
}
