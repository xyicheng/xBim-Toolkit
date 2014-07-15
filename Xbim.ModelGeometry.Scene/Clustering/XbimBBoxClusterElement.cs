using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common.Geometry;

namespace Xbim.ModelGeometry.Scene.Clustering
{
    /// <summary>
    /// This class is used to organise clusters of elements in case a scene needs to be split up.
    /// </summary>
    public class XbimBBoxClusterElement
    {
        public List<int> GeometryIds;
        public XbimRect3D Bound;
            
         
        public XbimBBoxClusterElement(int GeomteryId, XbimRect3D bound)
        {
            GeometryIds = new List<int>(1);
            this.GeometryIds.Add(GeomteryId);
            this.Bound = bound;
        }

        public void Add(XbimBBoxClusterElement OtherElement)
        {
            GeometryIds = new List<int>(OtherElement.GeometryIds.Count);
            GeometryIds.AddRange(OtherElement.GeometryIds);
            Bound.Union(OtherElement.Bound);
        }
    }
}
