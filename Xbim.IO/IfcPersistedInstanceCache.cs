using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.IO
{
    public class IfcPersistedInstanceCache : Dictionary<long, IPersistIfcEntity>, IIfcInstanceCache
    {
    }
}
