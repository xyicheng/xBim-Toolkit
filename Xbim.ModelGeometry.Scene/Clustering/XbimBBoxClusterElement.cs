using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.ModelGeometry.Scene.Clustering
{
    /// <summary>
    /// This class is used to organise clusters of elements in case a scene needs to be split up.
    /// </summary>
    public class XbimBBoxClusterElement
    {
        public List<int> GeometryIds = new List<int>();
        public Common.Geometry.XbimRect3D Bound;
            
        public XbimBBoxClusterElement(int GeomteryId, Common.Geometry.XbimRect3D bound)
        {
            this.GeometryIds.Add(GeomteryId);
            this.Bound = bound;
        }

        public void Add(XbimBBoxClusterElement OtherElement)
        {
            GeometryIds.AddRange(OtherElement.GeometryIds);
            Bound.Union(OtherElement.Bound);
        }
    }
}
