using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common.Geometry;

namespace Xbim.ModelGeometry.Converter
{
    public class XbimProductShape
    {
        public XbimMatrix3D Placement;
        public int ShapeLabel;
        public int ProductLabel;
       
        public XbimProductShape(int prodId, int shapeId)
        {
            this.ProductLabel = prodId;
            this.ShapeLabel = shapeId;
        }
    }
}
