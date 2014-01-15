using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common.Geometry;

namespace Xbim.XbimExtensions
{
    /// <summary>
    /// Conpares the shape data of two geometry objects to see if they are the same
    /// </summary>
    public class XbimShapeEqualityComparer : IEqualityComparer<XbimGeometryData>
    {
        public bool Equals(XbimGeometryData x, XbimGeometryData y)
        {
            return x.ShapeData.SequenceEqual(y.ShapeData);
        }

        public int GetHashCode(XbimGeometryData obj)
        {
            return obj.ShapeData.Length;
        }
    }

    public class XbimGeometryData
    {
        readonly public int GeometryLabel;
        readonly public int IfcProductLabel;
        readonly public XbimGeometryType GeometryType;
        readonly public byte[] ShapeData;
        
        readonly public int GeometryHash;
        readonly public short IfcTypeId;
        readonly public int StyleLabel;
        private XbimMatrix3D transform;

        public XbimGeometryData(int geometrylabel, int productLabel, XbimGeometryType geomType, short ifcTypeId, byte[] shape, byte[] transform, int geometryHash, int styleLabel)
        {
            GeometryLabel = geometrylabel;
            GeometryType = geomType;
            IfcTypeId = ifcTypeId;
            ShapeData = shape;
           // TransformData = transform;
            IfcProductLabel = productLabel;
            GeometryHash = geometryHash;
            StyleLabel = styleLabel;
            this.transform = XbimMatrix3D.FromArray(transform);
        }

        public XbimMatrix3D Transform
        {
            get
            {
                return transform;
            }
        }

        /// <summary>
        /// Transforms the shape data of the geometry by the matrix
        /// </summary>
        /// <param name="matrix"></param>
        public void TransformBy(XbimMatrix3D matrix)
        {
            transform = XbimMatrix3D.Multiply(transform, matrix);
           
        }

        public byte[] TransformData()
        {
            return transform.ToArray();
        }
        /// <summary>
        /// The constructs an XbimGeoemtryData object, the geometry hash is calculated from the array of shape data
        /// </summary>
        /// <param name="geometrylabel"></param>
        /// <param name="productLabel"></param>
        /// <param name="geomType"></param>
        /// <param name="ifcTypeId"></param>
        /// <param name="shape"></param>
        /// <param name="transform"></param>
        /// <param name="styleLabel"></param>
        public XbimGeometryData(int geometrylabel, int productLabel, XbimGeometryType geomType, short ifcTypeId, byte[] shape, byte[] transform, int styleLabel)
        {
            GeometryLabel = geometrylabel;
            GeometryType = geomType;
            IfcTypeId = ifcTypeId;
            ShapeData = shape;
            this.transform = XbimMatrix3D.FromArray(transform);
            IfcProductLabel = productLabel;
            GeometryHash = GenerateGeometryHash(ShapeData);
            StyleLabel = styleLabel;
        }

        
        
        /// <summary>
        /// Returns true if the two geometries have identical shape data
        /// </summary>
        /// <param name="to"></param>
        /// <returns></returns>
        public bool IsGeometryEqual(XbimGeometryData to)
        {

            return ShapeData.SequenceEqual(to.ShapeData);
        }


        /// <summary>
        /// Generates a FNV hash for any array of bytes
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        static public int GenerateGeometryHash(byte[] array)
        {
            unchecked
            {
                const int p = 16777619;
                int hash = (int)2166136261;

                for (int i = 0; i < array.Length; i++)
                    hash = (hash ^ array[i]) * p;

                hash += hash << 13;
                hash ^= hash >> 7;
                hash += hash << 3;
                hash ^= hash >> 17;
                hash += hash << 5;
                return hash;
            }
        }
    }
}
