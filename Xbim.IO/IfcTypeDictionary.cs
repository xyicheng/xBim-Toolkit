using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.IO
{
    public class IfcTypeDictionary : KeyedCollection<Type, IfcType>
    {
        protected override Type GetKeyForItem(IfcType item)
        {
            return item.Type;
        }

        public IfcType this[IPersistIfc ent]
        {
            get { return this[ent.GetType()]; }
        }

        public IfcType this[string ifcTypeName]
        {
            get { return IfcInstances.IfcTypeLookup[ifcTypeName]; }
        }

        public IfcType Add(string ifcTypeName)
        {
            IfcType ret = IfcInstances.IfcTypeLookup[ifcTypeName];
            this.Add(ret);
            return ret;
        }
    }
}
