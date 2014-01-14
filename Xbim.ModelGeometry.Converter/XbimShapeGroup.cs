using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common.Exceptions;
using Xbim.Common.Geometry;
using Xbim.IO;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions;

namespace Xbim.ModelGeometry.Converter
{
    /// <summary>
    /// A collection of one or more shapes that define a product shape
    /// </summary>
    public class XbimShapeGroup 
    {
        private XbimModel _model;
        private int[] _shapeGeomLabels;
        private List<XbimShape> _shapes;
        private List<int> _geometryHashCodes;


        public IEnumerable<XbimShape> Shapes()
        {
            LoadShapes();
            return _shapes;
        }



        public XbimShapeGroup(XbimModel model, int[] shapeGeomLabels)
        {
            _shapeGeomLabels = shapeGeomLabels;
            _model = model;
        }

        /// <summary>
        /// Returns a list of the geometry hashes for each shape that makes up this group
        /// </summary>
        public IEnumerable<int> ShapeHashCodes()
        {
            LoadGeometryHashCodes();
            return _geometryHashCodes;
        }

        private void LoadGeometryHashCodes()
        {
            if (_geometryHashCodes == null)
            {
               _geometryHashCodes = new List<int>();
                XbimGeometryCursor geomTable = _model.GetGeometryTable();
                IXbimGeometryEngine engine = _model.GeometryEngine();
                try
                {
                    using (var transaction = geomTable.BeginReadOnlyTransaction())
                    {

                        foreach (var geomId in _shapeGeomLabels)
                        {
                            XbimGeometryData data = geomTable.GetGeometryData(geomId);
                            if (data.GeometryType == XbimGeometryType.Polyhedron)
                            {
                                _geometryHashCodes.Add(data.GeometryHash);

                            }
                            else if (data.GeometryType == XbimGeometryType.PolyhedronMap)
                            {
                                //ADD EACH SHAPE IN THE MAP
                                
                            }
                            else
                                throw new XbimGeometryException("Unexpected geometry type " + Enum.GetName(typeof(XbimGeometryType), data.GeometryType));

                        }
                    }
                }
                finally
                {
                    _model.FreeTable(geomTable);
                }
            }
            

        }

        private void LoadShapes()
        {
            if (_shapes == null)//load cache if we do not have it
            {
                _shapes = new List<XbimShape>();
               _geometryHashCodes = new List<int>(); //reset the hash codes as we go
                XbimGeometryCursor geomTable = _model.GetGeometryTable();
                IXbimGeometryEngine engine = _model.GeometryEngine();
                try
                {
                    using (var transaction = geomTable.BeginReadOnlyTransaction())
                    {

                        foreach (var geomId in _shapeGeomLabels)
                        {
                            XbimGeometryData data = geomTable.GetGeometryData(geomId);
                            if (data.GeometryType == XbimGeometryType.Polyhedron)
                            {
                                int l = data.IfcProductLabel;
                                Type type = IfcMetaData.GetType(data.IfcTypeId);
                                string boundsString = System.Text.Encoding.ASCII.GetString(data.DataArray2);
                                int hash = data.GeometryHash;
                                _geometryHashCodes.Add(hash);
                                XbimRect3D boundingBox = XbimRect3D.Parse(boundsString);
                                string geometryString = System.Text.Encoding.ASCII.GetString(data.ShapeData);
                                IXbimGeometryModel geometry = engine.GetGeometry3D(geometryString, data.GeometryType);
                                int stylelabel = data.StyleLabel;
                                _shapes.Add(new XbimShape(l, type, boundingBox, geometry, stylelabel, hash, data.Counter));

                            }
                            else if (data.GeometryType == XbimGeometryType.PolyhedronMap)
                            {
                                //ADD EACH SHAPE IN THE MAP
                            }
                            else
                                throw new XbimGeometryException("Unexpected geometry type " + Enum.GetName(typeof(XbimGeometryType), data.GeometryType));

                        }
                    }
                }
                finally
                {
                    _model.FreeTable(geomTable);
                }
            }
        }
    }
}
