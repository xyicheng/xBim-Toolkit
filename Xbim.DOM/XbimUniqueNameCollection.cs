using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Kernel;

namespace Xbim.DOM
{
    /// <summary>
    /// Collection that can contain just elements with unique name
    /// </summary>
    public class XbimUniqueNameCollection<T>: KeyedCollection<string, T> where T : IXbimRoot
    {
        public string TypeOfCollection { get { return typeof(T).ToString(); } }
        public XbimUniqueNameCollection<T> This { get { return this; } }

        // The parameterless constructor of the base class creates a 
        // KeyedCollection with an internal dictionary. For this code 
        // example, no other constructors are exposed.
        public XbimUniqueNameCollection() : base () { }

        protected override string GetKeyForItem(T item)
        {
          
            return (item as IXbimRoot).AsRoot.Name;
            
        }

        public T GetByName(string name)
        {
            if (Contains(name))
            {
                return this[name];
            }
            else
            {
                return default(T);
            }
        }

    }
}
