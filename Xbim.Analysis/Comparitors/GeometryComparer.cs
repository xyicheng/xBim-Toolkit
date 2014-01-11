using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.Kernel;
using Xbim.ModelGeometry.Converter;

namespace Xbim.Analysis.Comparitors
{
    public class GeometryComparer : IModelComparer
    {
        public Dictionary<IfcRoot, ChangeType> Compare(IEnumerable<IfcRoot> Baseline, IEnumerable<IfcRoot> Delta)
        {
            throw new NotImplementedException(); //need to do this
        }

        private Dictionary<Int32, Int32> map = new Dictionary<Int32, Int32>();
        public Dictionary<Int32, Int32> GetMap() { return map; }

        public Dictionary<IfcRoot, ChangeType> Compare(Xbim3DModelContext baseContext, Xbim3DModelContext revisionContext, double oneMillimetre)
        {
            List<XbimProductShape> baseShapes = baseContext.ProductShapes.ToList();
            List<XbimProductShape> revisedShapes = revisionContext.ProductShapes.ToList();
            //Create our dictionary for return
            Dictionary<IfcRoot, ChangeType> changes = new Dictionary<IfcRoot, ChangeType>();

            //check intersections of boounding boxes
            //below code is mock and will be replaced by octree

            foreach (var shape in revisedShapes)
            {
                IEnumerable<XbimProductShape> hits = baseShapes.Where(s => s.BoundingBox.IsSimilar(shape.BoundingBox, oneMillimetre*10));
                int hitCount = hits.Count();
                //Martin the code below is random just to see what is happening and to show you how Lloyds frameowrk works
                //I am working through this and I am finding complexities
                //what if one object has two more matches? I have put in unknown state just to see results, but really this might be a clash
                //unless as in this case it is a window in an opening
                if (hitCount == 1)
                {
                    changes.Add(shape.Product, ChangeType.Matched);
                    map.Add(Math.Abs(shape.Product.EntityLabel), Math.Abs(hits.First().Product.EntityLabel));
                }
                if(hitCount > 1)
                    changes.Add(shape.Product, ChangeType.Unknown);
            }
            return changes;
        }
    }
}
