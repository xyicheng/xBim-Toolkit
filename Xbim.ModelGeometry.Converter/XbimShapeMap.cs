using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.ModelGeometry.Scene;

namespace Xbim.ModelGeometry.Converter
{
    public class XbimShapeMap : XbimShape
    {
        
       
        private XbimShapeGeometry shapeGeometry;
        

        public XbimShapeMap(int geometryLabel, XbimShapeGeometry shape, XbimMatrix3D transform, int stylelabel = 0)
            : base(shape.GeometryHash^transform.GetHashCode())
        {
            
            this.shapeGeometry = shape;
            if (shape.Transform.HasValue) 
                transform = shape.Transform.Value * transform;
            else
                this.transform = transform;
            this.geometryLabel = geometryLabel;
            this.styleLabel = stylelabel;
        }

        public override String Mesh
        {
            get
            {
                return shapeGeometry.Mesh;
            }
        }
        public override bool HasStyle
        {
            get { return shapeGeometry.HasStyle || this.styleLabel > 0; }
        }

        public override int StyleLabel
        {
            get { if (shapeGeometry.HasStyle) return shapeGeometry.StyleLabel; else return this.styleLabel; }
        }
    }
}
