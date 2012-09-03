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
        readonly public ushort IfcTypeId;
        public XbimGeometryData(long productLabel, XbimGeometryType geomType, ushort ifcTypeId, byte[] shape, byte[] transform, long representationLabel)
        {
            GeometryType = geomType;
            IfcTypeId = ifcTypeId;
            ShapeData = shape;
            TransformData = transform;
            IfcProductLabel = productLabel;
            IfcRepresentationLabel = representationLabel;
        }
    }
}
