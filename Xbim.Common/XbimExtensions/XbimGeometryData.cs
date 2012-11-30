using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.XbimExtensions
{
    public class XbimGeometryData
    {
        readonly public int GeometryLabel;
        readonly public int IfcProductLabel;
        readonly public XbimGeometryType GeometryType;
        readonly public byte[] ShapeData;
        readonly public byte[] TransformData;
        readonly public int IfcRepresentationLabel;
        readonly public short IfcTypeId;
        readonly public int StyleLabel;

        public XbimGeometryData(int geometrylabel, int productLabel, XbimGeometryType geomType, short ifcTypeId, byte[] shape, byte[] transform, int representationLabel, int styleLabel)
        {
            GeometryLabel = geometrylabel;
            GeometryType = geomType;
            IfcTypeId = ifcTypeId;
            ShapeData = shape;
            TransformData = transform;
            IfcProductLabel = productLabel;
            IfcRepresentationLabel = representationLabel;
            StyleLabel = styleLabel;
        }
    }
}
