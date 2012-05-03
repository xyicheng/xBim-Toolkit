using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace Xbim.ModelGeometry.Scene
{
    /// <summary>
    /// This class extends the WPF MeshGeometry3D class to support the XbimTriangulationModel
    /// </summary>
    public class XbimMeshGeometry3D : IXbimTriangulatedModelBuilder
    {
        Model3DGroup _modelGroup;
        MeshGeometry3D _meshGeometry;
        GeometryModel3D _geometryModel;
        Point3DCollection _points;
        //HashSet<uint> _facePointIndexes;
        TriangleType _meshType;
        uint _previousToLastIndex;
        uint _lastIndex;
        uint _pointTally;
        uint _fanStartIndex;

        public static implicit operator Model3D(XbimMeshGeometry3D modelGeom)
        {

            return modelGeom._modelGroup;
        }

        public void BeginBuild()
        {
            _modelGroup = new Model3DGroup();
            _meshGeometry = new MeshGeometry3D();
            _geometryModel = new GeometryModel3D();
            _geometryModel.Geometry = _meshGeometry;
            _modelGroup.Children.Add(_geometryModel);
        }

        public void BeginVertices(uint numPoints)
        {
            _points = new Point3DCollection((int)numPoints);
        }

        public void AddVertex(Point3D point3D)
        {
            _points.Add(point3D);
        }

        public void EndVertices()
        {
        }

        public void BeginFaces(ushort numFaces)
        {
            
        }

        public void BeginFace()
        {
            //_facePointIndexes = new HashSet<uint>();
        }

        public void BeginNormals(ushort numNormals)
        {
            
        }

        public void AddNormal(Vector3D normal)
        {
            
        }

        public void EndNormals()
        {
           
        }

        public void BeginPolygons(ushort numPolygons)
        {
            
        }

        public void BeginPolygon()
        {
            
        }

        public void BeginTriangulation(TriangleType meshType, uint indicesCount)
        {
           
            _meshType = meshType;
            _pointTally = 0;
            _previousToLastIndex = 0;
            _lastIndex = 0;
            _fanStartIndex = 0;
        }

        public void AddTriangleIndex(uint index)
        {
            //int actualIndex = (int)index;
            //if(!_facePointIndexes.Contains(index);
            //{
            //    _facePointIndexes.Add(index);
            //    actualIndex = 
            //}
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

        public void EndTriangulation()
        {
            
        }

        public void EndPolygon()
        {
           
        }

        public void EndPolygons()
        {
           
        }

        public void EndFace()
        {
           
        }

        public void EndFaces()
        {
            
        }

        public void EndBuild()
        {
           
        }


        public void BeginChild()
        {
            //_geometryModel.Freeze();
            _meshGeometry = new MeshGeometry3D();
            _geometryModel = new GeometryModel3D();
            _geometryModel.Geometry = _meshGeometry;
            _modelGroup.Children.Add(_geometryModel);
        }

        public void EndChild()
        {
           
        }
    }
}
