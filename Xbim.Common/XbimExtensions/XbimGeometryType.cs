using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.XbimExtensions
{
    public enum XbimGeometryType : byte
    {
        Undefined = 0x0,
        /// <summary>
        /// This type can be transformed to XbimRect3D via XbimRect3D.FromArray(geomdata.ShapeData)
        /// </summary>
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
        TransformOnly = 0x5,
        /// <summary>
        /// 128 bit hash of a geometry
        /// </summary>
        TriangulatedMeshHash = 0x6,
        /// <summary>
        /// The xBIM variant of the PLY format, a set of nominally planar polygons
        /// </summary>
        Polyhedron = 0x7,
        /// <summary>
        /// A map, or reference to one or more Polyhedron shapes
        /// </summary> = 
        PolyhedronMap = 0x8,
        /// <summary>
        /// The map of polyhedron that represent this product
        /// </summary>
        ProductPolyhedronMap = 0x9,
        /// <summary>
        /// The Polyhedron that represents this product after openings and additions have been applied
        /// </summary>
        NetProductPolyhedronMap = 0xA,
        /// <summary>
        /// A boundary representation using the OpenCascade model
        /// </summary>
        BRep = 0xE,
        
    }
}
