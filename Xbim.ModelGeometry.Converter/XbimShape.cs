using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.RepresentationResource;
using Xbim.ModelGeometry.Scene;

namespace Xbim.ModelGeometry.Converter
{
    public class XbimShape
    {
        private int shapeLabel;
        private List<int> representationItemLabels = new List<int>();
       
        public XbimShape(int p)
        {
            this.shapeLabel = p;
        }
        public void Add(int item)
        {
            representationItemLabels.Add(item);
        }
       // public IfcGeometricRepresentationContext Context;

        public int ShapeLabel { get { return shapeLabel; } }
    }
}
