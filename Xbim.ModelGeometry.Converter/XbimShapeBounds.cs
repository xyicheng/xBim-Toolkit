using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common.Geometry;
using Xbim.IO;

namespace Xbim.ModelGeometry.Converter
{
    public class XbimShapeBounds : IXbimShapeBounds
    {
        XbimShapeInstance _instance;
        XbimShapeGeometry _geometry;
        public XbimShapeBounds(XbimShapeInstance instance,  XbimShapeGeometry geometry)
        {
            _instance = instance;
            _geometry = geometry;
        }

        public int InstanceLabel
        {
            get { return _instance.InstanceLabel; }
        }

        public short IfcTypeId
        {
            get { return _instance.IfcTypeId; }
        }

        public int IfcProductLabel
        {
            get { return _instance.IfcProductLabel; }
        }

        public int StyleLabel
        {
            get { return _instance.StyleLabel; }
        }

        public int ShapeLabel
        {
            get { return _instance.ShapeLabel; }
        }

        public XbimMatrix3D Transformation
        {
            get { return _instance.Transformation; }
        }

        public XbimRect3D BoundingBox
        {
            get { return _geometry.BoundingBox; }
        }

        public uint Cost
        {
            get { return _geometry.Cost; }
        }

        public uint ReferenceCount
        {
            get { return _geometry.ReferenceCount; }
        }

      
    }
}
