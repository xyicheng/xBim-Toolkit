using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.RepresentationResource;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions;

namespace Xbim.ModelGeometry.Converter
{
    /// <summary>
    /// A basic shape geoemetry, note this is independent of placement and not specific to any product
    /// </summary>
    public struct XbimShapeGeometry : IXbimShapeGeometryData
    {
        /// <summary>
        /// The unique label of this shape instance
        /// </summary>
        uint _shapeLabel;
        /// <summary>
        /// The label of the IFC object that defines this shape
        /// </summary>
        uint _ifcShapeLabel;
        /// <summary>
        ///  Hash of the shape Geometry, based on the IFC representation, this is not unique
        /// </summary>
        int _geometryHash;
        /// <summary>
        /// The number of references to this shape
        /// </summary>
        uint _referenceCount;
        /// <summary>
        /// The level of detail or development that the shape is suited for
        /// </summary>
        XbimLOD _lod;
        /// <summary>
        /// The format in which the shape data is represented, i.e. triangular mesh, polygon, opencascade
        /// </summary>
        XbimGeometryType _format;
        /// <summary>
        /// The bounding box of this instance, requires tranformation to place in world coordinates
        /// </summary>
        XbimRect3D _boundingBox;
        /// <summary>
        /// The geometry data defining the shape
        /// </summary>
        string _shapeData;

       
        /// <summary>
        /// The unique label of this shape geometry
        /// </summary>
        public uint ShapeLabel
        {
            get
            {
                return _shapeLabel;
            }
            set
            {
                _shapeLabel = value;
            }
        }
        /// <summary>
        /// The label of the IFC object that defines this shape
        /// </summary>
        public uint IfcShapeLabel
        {
            get
            {
                return _ifcShapeLabel;
            }
            set
            {
                _ifcShapeLabel = value;
            }
        }
        /// <summary>
        ///  Hash of the shape Geometry, based on the IFC representation, this is not unique
        /// </summary>
        public int GeometryHash
        {
            get
            {
                return _geometryHash;
            }
            set
            {
                _geometryHash = value;
            }
        }
        
        /// <summary>
        /// The cost in bytes of this shape
        /// </summary>
        public uint Cost
        {
            get
            {
                if(_referenceCount==0)
                    return (uint)_shapeData.Length;
                else 
                   return (uint)(_referenceCount * _shapeData.Length);
            }

        }
        /// <summary>
        /// The number of references to this shape
        /// </summary>
        public uint ReferenceCount
        {
            get
            {
                return _referenceCount;
            }
            set
            {
                _referenceCount = value;
            }
        }
        /// <summary>
        /// The level of detail or development that the shape is suited for
        /// </summary>
        public XbimLOD LOD
        {
            get
            {
                return _lod;
            }
            set
            {
                _lod = value;
            }
        }

        byte IXbimShapeGeometryData.LOD
        {
            get
            {
                return (byte)_lod;
            }
            set
            {
                _lod = (XbimLOD)value;
            }
        }
        /// <summary>
        /// The format in which the shape data is represented, i.e. triangular mesh, polygon, opencascade
        /// </summary>
        public XbimGeometryType Format
        {
            get
            {
                return _format;
            }
            set
            {
                _format = value;
            }
        }
        /// <summary>
        /// The format in which the shape data is represented, i.e. triangular mesh, polygon, opencascade as a byte
        /// </summary>
        byte IXbimShapeGeometryData.Format
        {
            get
            {
                return (byte)_format;
            }
            set
            {
                _format = (XbimGeometryType)value;
            }
        }
        /// <summary>
        /// The bounding box of this instance, requires tranformation to place in world coordinates
        /// </summary>
        public XbimRect3D BoundingBox
        {
            get
            {
                return _boundingBox;
            }
            set
            {
                _boundingBox = value;
            }
        }
        /// <summary>
        /// The bounding box of this instance, requires tranformation to place in world coordinates
        /// </summary>
        byte[] IXbimShapeGeometryData.BoundingBox
        {
            get
            {
                return _boundingBox.ToFloatArray();
            }
            set
            {
                _boundingBox = XbimRect3D.FromArray(value);
            }
        }
        /// <summary>
        /// The geometry data defining the shape
        /// </summary>
        public string ShapeData
        {
            get
            {
                return _shapeData;
            }
            set
            {
                _shapeData = value;
            }
        }
        /// <summary>
        /// The geometry data defining the shape, this is a compressed representation of the data
        /// </summary>
        byte[] IXbimShapeGeometryData.ShapeData
        {
            get
            {
                var bytes = Encoding.UTF8.GetBytes(_shapeData);

                using (var msi = new MemoryStream(bytes))
                using (var mso = new MemoryStream())
                {
                    using (var gs = new GZipStream(mso, CompressionMode.Compress))
                    {
                        msi.CopyTo(gs);
                    }

                    return mso.ToArray();
                }
            }
            set
            {
                using (var msi = new MemoryStream(value))
                using (var mso = new MemoryStream())
                {
                    using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                    {
                        gs.CopyTo(mso);
                    }
                    _shapeData = Encoding.UTF8.GetString(mso.ToArray());
                }
            }
        }

        /// <summary>
        /// Returns true if the geometry is valid
        /// </summary>
        public bool IsValid
        {
            get
            {
                return _shapeLabel > 0;
            }
        }

        public override string ToString()
        {

            return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}", _shapeLabel, _ifcShapeLabel, _geometryHash, _shapeLabel, _referenceCount, _lod, _format, _boundingBox.ToString(), _shapeData);
        }


     
    }
}
