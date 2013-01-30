using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Xbim.ModelGeometry.Scene
{
    public class XbimMeshFragmentCollection : List<XbimMeshFragment>
    {
       
        public XbimMeshFragmentCollection(XbimMeshFragmentCollection value)
            :base(value)
        {
           
        }
        public XbimMeshFragmentCollection()
        {

        }
        /// <summary>
        /// Returns true if the collection contains a fragment for the specified type
        /// </summary>
        /// <returns></returns>
        public bool Contains<T>()
        {
            foreach (var frag in this)
                if(typeof(T).IsAssignableFrom (frag.EntityType ))
                    return true;
            return false;
        }

        public XbimMeshFragmentCollection OfType<T>()
        {
            XbimMeshFragmentCollection results = new XbimMeshFragmentCollection();
            results.AddRange( this.Where(frag=>typeof(T).IsAssignableFrom(frag.EntityType)));
            return results;
        }

        public XbimMeshFragmentCollection Excluding<T>()
        {
            XbimMeshFragmentCollection results = new XbimMeshFragmentCollection();
            results.AddRange(this.Where(frag => !(typeof(T).IsAssignableFrom(frag.EntityType))));
            return results;
        }
    }
}
