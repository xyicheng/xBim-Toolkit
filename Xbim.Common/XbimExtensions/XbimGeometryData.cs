using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.XbimExtensions
{
    public class XbimGeometryData
    {
        readonly public long IfcProductLabel;
        readonly public XbimGeometryType GeometryType;
        readonly public byte[] ShapeData;
        readonly public byte[] TransformData;
        readonly public long IfcRepresentationLabel;

        public XbimGeometryData(long productLabel, XbimGeometryType type, byte[] shape, byte[] transform, long representationLabel)
        {
            GeometryType = type;
            ShapeData = shape;
            TransformData = transform;
            IfcProductLabel = productLabel;
            IfcRepresentationLabel = representationLabel;
        }
    }
}
