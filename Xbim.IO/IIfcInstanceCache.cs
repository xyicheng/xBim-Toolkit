using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.IO
{
    public interface IIfcInstanceCache
    {

        bool Contains(XbimExtensions.Interfaces.IPersistIfcEntity instance);
        bool Contains(long p);
        long Count { get; }

        long HighestLabel { get;}
        /// <summary>
        /// Sets the highest label
        /// </summary>
        /// <param name="nextLabel"></param>
        void SetHighestLabel_Reversable(long nextLabel);

        XbimExtensions.Interfaces.IPersistIfcEntity CreateNew_Reversable(Type t);

        long InstancesOfTypeCount(Type t);
    }
}
