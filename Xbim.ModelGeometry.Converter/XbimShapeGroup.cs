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
    public class XbimShapeGroup : IEnumerable<XbimShape>
    {
        private Xbim3DModelContext _context;

        public Xbim3DModelContext Context
        {
            get { return _context; } 
        }

        private int[] _shapeGeomLabels;
        private List<XbimShape> _shapes;
        private List<int> _geometryHashCodes;


        public IEnumerable<XbimShape> Shapes()
        {
            LoadShapes();
            return _shapes;
        }



        public XbimShapeGroup(Xbim3DModelContext context, int[] shapeGeomLabels)
        {
            _shapeGeomLabels = shapeGeomLabels;
            _context = context;
        }

        public XbimModel Model
        {
            get
            {
                return _context.Model;
            }
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
                XbimGeometryCursor geomTable = Model.GetGeometryTable();
                IXbimGeometryEngine engine = Model.GeometryEngine();
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
                                //srl this whole class should go
                            //else if (data.GeometryType == XbimGeometryType.PolyhedronMap)
                            //{
                            //    //ADD EACH SHAPE IN THE MAP
                            //    string shapeString = System.Text.Encoding.ASCII.GetString(data.ShapeData);
                            //    string[] itms = shapeString.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                            //    XbimMatrix3D transform = XbimMatrix3D.FromString(itms[0]);
                            //    _geometryHashCodes.Add(data.GeometryHash);
                            //}
                            else
                                throw new XbimGeometryException("Unexpected geometry type " + Enum.GetName(typeof(XbimGeometryType), data.GeometryType));

                        }
                    }
                }
                finally
                {
                    Model.FreeTable(geomTable);
                }
            }
            

        }

        private void LoadShapes()
        {
            if (_shapes == null)//load cache if we do not have it
            {
                _shapes = new List<XbimShape>();
               _geometryHashCodes = new List<int>(); //reset the hash codes as we go
               foreach (var shape in _context.Shapes(_shapeGeomLabels))
               {
                   _geometryHashCodes.Add(shape.GeometryHash);
                   _shapes.Add(shape);
               }                
            }
        }

        public IEnumerator<XbimShape> GetEnumerator()
        {
            LoadShapes();
            return _shapes.GetEnumerator();    
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            LoadShapes();
            return _shapes.GetEnumerator();    
        }
    }
}
