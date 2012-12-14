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
        /// <summary>
        /// returns a hash code for the shape data only
        /// </summary>
        /// <returns></returns>
        public int GetGeometryHash()
        {
            int len = ShapeData.Length;
            int midBytePos = len / 2;
            byte midByte = ShapeData[midBytePos];
            int byteHighBit = midByte << 24;
            return byteHighBit ^ len;
        }
        
        /// <summary>
        /// Returns true if the two geometries have identical shape data
        /// </summary>
        /// <param name="to"></param>
        /// <returns></returns>
        public bool IsGeometryEqual(XbimGeometryData to)
        {
            if(GetGeometryHash() == to.GetGeometryHash())
            {
                return ShapeData.SequenceEqual(to.ShapeData);
            }
            else
                return false;

        }

        /// <summary>
        /// returns true if this geometry is a Map of mapOf geometry
        /// </summary>
        /// <param name="of"></param>
        /// <returns></returns>
        public bool IsMapOf(XbimGeometryData mapOf)
        {
            return IsGeometryEqual(mapOf);
        }
 
    }
}
