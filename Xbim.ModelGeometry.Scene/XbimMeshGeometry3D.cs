using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace Xbim.ModelGeometry.Scene
{
    /// <summary>
    /// This class extends the WPF MeshGeometry3D class to support the XbimTriangulationModel.
    /// The geometry is generated from the points only which are replicated every time they are invoked.
    /// Not memory efficient but prevents normal clashing.
    /// </summary>
    public class XbimMeshGeometry3D : IXbimTriangulatesToPositionsIndices, IXbimTriangulatesToPositionsNormalsIndices
    {
        Model3DGroup _modelGroup;
        MeshGeometry3D _meshGeometry;
        GeometryModel3D _geometryModel;
        Point3DCollection _points;
        TriangleType _meshType;
        uint _previousToLastIndex;
        uint _lastIndex;
        uint _pointTally;
        uint _fanStartIndex;

        public static implicit operator Model3D(XbimMeshGeometry3D modelGeom)
        {
            return modelGeom._modelGroup;
        }

        #region standard calls
        
        private void Init()
        {
            _modelGroup = new Model3DGroup();
            _meshGeometry = new MeshGeometry3D();
            _geometryModel = new GeometryModel3D();
            _geometryModel.Geometry = _meshGeometry;
            _modelGroup.Children.Add(_geometryModel);
        }

        private void StandardBeginPolygon(TriangleType meshType)
        {
            _meshType = meshType;
            _pointTally = 0;
            _previousToLastIndex = 0;
            _lastIndex = 0;
            _fanStartIndex = 0;
        }
        #endregion

        #region IXbimTriangulatesToPointsIndices

        void IXbimTriangulatesToPositionsIndices.BeginBuild()
        {
            Init();
        }

        void IXbimTriangulatesToPositionsIndices.BeginPositions(uint numPoints)
        {
            _points = new Point3DCollection((int)numPoints);
        }

        void IXbimTriangulatesToPositionsIndices.AddPosition(Point3D point3D)
        {
            _points.Add(point3D);
        }

        void IXbimTriangulatesToPositionsIndices.EndPositions()
        {
        }

        void IXbimTriangulatesToPositionsIndices.BeginPolygons(uint totalNumberTriangles, uint numPolygons)
        {
            // three position for each triangle
            _meshGeometry.Positions = new Point3DCollection((int)(totalNumberTriangles * 3));
            _meshGeometry.TriangleIndices = new System.Windows.Media.Int32Collection((int)(totalNumberTriangles * 3));
        }

        void IXbimTriangulatesToPositionsIndices.BeginPolygon(TriangleType meshType, uint indicesCount)
        {
            StandardBeginPolygon(meshType);
        }

        void IXbimTriangulatesToPositionsIndices.AddTriangleIndex(uint index)
        {
            if (_pointTally == 0)
                _fanStartIndex = index;
            if (_pointTally < 3) //first time
            {
                _meshGeometry.TriangleIndices.Add(_meshGeometry.Positions.Count);
                _meshGeometry.Positions.Add(_points[(int)index]);
            }
            else
            {
                switch (_meshType)
                {
                    case TriangleType.GL_TRIANGLES://      0x0004
                        _meshGeometry.TriangleIndices.Add(_meshGeometry.Positions.Count);
                        _meshGeometry.Positions.Add(_points[(int)index]);
                        break;
                    case TriangleType.GL_TRIANGLE_STRIP:// 0x0005
                        if (_pointTally % 2 == 0)
                        {
                            _meshGeometry.TriangleIndices.Add(_meshGeometry.Positions.Count);
                            _meshGeometry.Positions.Add(_points[(int)_previousToLastIndex]);
                            _meshGeometry.TriangleIndices.Add(_meshGeometry.Positions.Count);
                            _meshGeometry.Positions.Add(_points[(int)_lastIndex]);
                        }
                        else
                        {
                            _meshGeometry.TriangleIndices.Add(_meshGeometry.Positions.Count);
                            _meshGeometry.Positions.Add(_points[(int)_lastIndex]);
                            _meshGeometry.TriangleIndices.Add(_meshGeometry.Positions.Count);
                            _meshGeometry.Positions.Add(_points[(int)_previousToLastIndex]);
                        }
                        _meshGeometry.TriangleIndices.Add(_meshGeometry.Positions.Count);
                        _meshGeometry.Positions.Add(_points[(int)index]);
                        break;
                    case TriangleType.GL_TRIANGLE_FAN://   0x0006
                        _meshGeometry.TriangleIndices.Add(_meshGeometry.Positions.Count);
                        _meshGeometry.Positions.Add(_points[(int)_fanStartIndex]);
                        _meshGeometry.TriangleIndices.Add(_meshGeometry.Positions.Count);
                        _meshGeometry.Positions.Add(_points[(int)_lastIndex]);
                        _meshGeometry.TriangleIndices.Add(_meshGeometry.Positions.Count);
                        _meshGeometry.Positions.Add(_points[(int)index]);
                        break;
                    default:
                        break;
                }
            }
            _previousToLastIndex = _lastIndex;
            _lastIndex = index;
            _pointTally++;
        }

        void IXbimTriangulatesToPositionsIndices.EndPolygon()
        {
            
        }

        void IXbimTriangulatesToPositionsIndices.EndPolygons()
        {
           
        }

        void IXbimTriangulatesToPositionsIndices.EndBuild()
        {

        }
        #endregion

        
        #region IXbimTriangulatesToPositionsNormalsIndices
        
        void IXbimTriangulatesToPositionsNormalsIndices.BeginBuild()
        {
            Init();
        }

        void IXbimTriangulatesToPositionsNormalsIndices.BeginPoints(uint numPoints)
        {
            _meshGeometry.Positions = new Point3DCollection((int)numPoints);
            _meshGeometry.Normals = new Vector3DCollection((int)numPoints);
        }

        void IXbimTriangulatesToPositionsNormalsIndices.AddPoint(Point3D point3D, Vector3D normal)
        {
            _meshGeometry.Positions.Add(point3D);
            _meshGeometry.Normals.Add(normal);
        }

        void IXbimTriangulatesToPositionsNormalsIndices.EndPoints()
        {
            // purposely empty
        }

        void IXbimTriangulatesToPositionsNormalsIndices.BeginPolygons(uint totalNumberTriangles, uint numPolygons)
        {
            _meshGeometry.TriangleIndices = new System.Windows.Media.Int32Collection((int)(totalNumberTriangles * 3));
        }

        void IXbimTriangulatesToPositionsNormalsIndices.BeginPolygon(TriangleType meshType, uint indicesCount)
        {
            StandardBeginPolygon(meshType);
        }

        void IXbimTriangulatesToPositionsNormalsIndices.AddTriangleIndex(uint index)
        {
            if (_pointTally == 0)
                _fanStartIndex = index;
            if (_pointTally < 3) //first time
            {
                _meshGeometry.TriangleIndices.Add((int)index);
                // _meshGeometry.Positions.Add(_points[(int)index]);
            }
            else
            {
                switch (_meshType)
                {
                    case TriangleType.GL_TRIANGLES://      0x0004
                        _meshGeometry.TriangleIndices.Add((int)index);
                        // _meshGeometry.Positions.Add(_points[(int)index]);
                        break;
                    case TriangleType.GL_TRIANGLE_STRIP:// 0x0005
                        if (_pointTally % 2 == 0)
                        {
                            _meshGeometry.TriangleIndices.Add((int)_previousToLastIndex);
                            // _meshGeometry.Positions.Add(_points[(int)_previousToLastIndex]);
                            _meshGeometry.TriangleIndices.Add((int)_lastIndex);
                            // _meshGeometry.Positions.Add(_points[(int)_lastIndex]);
                        }
                        else
                        {
                            _meshGeometry.TriangleIndices.Add((int)_lastIndex);
                            // _meshGeometry.Positions.Add(_points[(int)_lastIndex]);
                            _meshGeometry.TriangleIndices.Add((int)_previousToLastIndex);
                            // _meshGeometry.Positions.Add(_points[(int)_previousToLastIndex]);
                        }
                        _meshGeometry.TriangleIndices.Add((int)index);
                        // _meshGeometry.Positions.Add(_points[(int)index]);
                        break;
                    case TriangleType.GL_TRIANGLE_FAN://   0x0006
                        _meshGeometry.TriangleIndices.Add((int)_fanStartIndex);
                        // _meshGeometry.Positions.Add(_points[(int)_fanStartIndex]);
                        _meshGeometry.TriangleIndices.Add((int)_lastIndex);
                        // _meshGeometry.Positions.Add(_points[(int)_lastIndex]);
                        _meshGeometry.TriangleIndices.Add((int)index);
                        // _meshGeometry.Positions.Add(_points[(int)index]);
                        break;
                    default:
                        break;
                }
            }
            _previousToLastIndex = _lastIndex;
            _lastIndex = index;
            _pointTally++;
        }

        void IXbimTriangulatesToPositionsNormalsIndices.EndPolygon()
        {
            // purposely empty
        }

        void IXbimTriangulatesToPositionsNormalsIndices.EndPolygons()
        {
            // purposely empty
        }

        void IXbimTriangulatesToPositionsNormalsIndices.EndBuild()
        {
            // purposely empty
        }
        #endregion
        
    }
}
