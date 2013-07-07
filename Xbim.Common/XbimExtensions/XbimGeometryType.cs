using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.XbimExtensions
{
    public enum XbimGeometryType : byte
    {
        Undefined = 0x0,
        BoundingBox = 0x01,
        MultipleBoundingBox = 0x02,
        TriangulatedMesh = 0x03,
        /// <summary>
        /// Regions (clusters of elements in a model) are stored for the project in one database row.
        /// Use XbimRegionCollection.FromArray(ShapeData) for deserialising.
        /// </summary>
        Region = 0x4,
        /// <summary>
        /// For products with no geometry use TransformOnly to store the transform matrix associated with the placement.
        /// </summary>
        TransformOnly = 0x5
    }
}
