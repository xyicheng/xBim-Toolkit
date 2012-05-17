using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.ModelGeometry.Scene;

namespace Xbim.Web.Viewer3D.ServerSide
{
    public class MeshGeometry : IXbimTriangulatesFromBinaryStream
    {

        private Int32Collection _indices = new Int32Collection();

        public Int32Collection TriangleIndices
        {
            get { return _indices; }
            set { _indices = value; }
        }

        public List<PositionNormalPair> UniquePoints = new List<PositionNormalPair>();

        public struct PositionNormalPair
        {
            public int PositionIndex;
            public int NormalIndex;
            public PositionNormalPair(int v1, int v2)
            {
                PositionIndex = v1;
                NormalIndex = v2;
            }
        }

        private Vector3DCollection _normals = new Vector3DCollection();

        public Vector3DCollection Normals
        {
            get { return _normals; }
            set { _normals = value; }
        }

        private Point3DCollection _positions = new Point3DCollection();

        public Point3DCollection Positions
        {
            get { return _positions; }
            set { _positions = value; }
        }

        public void BeginBuild()
        {
            Init();
        }

        private void Init()
        {
            _indices = new Int32Collection();
            _normals = new Vector3DCollection();
            _positions = new Point3DCollection();
            UniquePoints = new List<PositionNormalPair>();
        }

        public void BeginVertices(uint numPoints)
        {
            _positions = new Point3DCollection((int)numPoints);
        }

        public void AddVertex(Point3D point3D)
        {
            _positions.Add(point3D);
        }

        public void EndVertices()
        {
        }

        public void BeginFaces(ushort numFaces)
        {

        }

        public void BeginFace()
        {
        }

        public void BeginNormals(ushort numNormals)
        {
        }

        int currentNormal = 0;
        public void AddNormal(Vector3D normal)
        {
            if (Normals.Contains(normal))
            {
                currentNormal = Normals.IndexOf(normal);
            }
            else
            {
                Normals.Add(normal);
                currentNormal = Normals.Count - 1;
            }
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

        TriangleType _meshType;
        int _previousToLastIndex;
        int _lastIndex;
        int _pointTally;
        int _fanStartIndex;

        public void BeginTriangulation(TriangleType meshType, uint indicesCount)
        {
            _meshType = meshType;
            _pointTally = 0;
            _previousToLastIndex = 0;
            _lastIndex = 0;
            _fanStartIndex = 0;
        }

        public int UniquePointIndex(uint index)
        {
            var v = new PositionNormalPair((int)index, currentNormal);
            if (UniquePoints.Contains(v))
            {
                return UniquePoints.IndexOf(v);
            }
            UniquePoints.Add(v);
            return UniquePoints.Count - 1;
        }

        public void AddTriangleIndex(uint uindex)
        {
            int index = UniquePointIndex(uindex);


            if (_pointTally == 0)
                _fanStartIndex = index;
            if (_pointTally < 3) // no one triangle available yet.
            {
                _indices.Add(index);
            }
            else
            {

                switch (_meshType)
                {

                    case TriangleType.GL_TRIANGLES://      0x0004
                        _indices.Add(index);
                        break;
                    case TriangleType.GL_TRIANGLE_STRIP:// 0x0005
                        if (_pointTally % 2 == 0)
                        {
                            _indices.Add(_previousToLastIndex);
                            _indices.Add(_lastIndex);   
                        }
                        else
                        {
                            _indices.Add(_lastIndex);
                            _indices.Add(_previousToLastIndex);
                        }
                        _indices.Add(index);
                        break;
                    case TriangleType.GL_TRIANGLE_FAN://   0x0006
                        _indices.Add(_fanStartIndex);
                        _indices.Add(_lastIndex);
                        _indices.Add(index);
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

        public void EndChild()
        {

        }

        public void BeginChild()
        {
            //_meshGeometry = new MeshGeometry3D();
            //_geometryModel = new GeometryModel3D();
            //_geometryModel.Geometry = _meshGeometry;
            //_modelGroup.Children.Add(_geometryModel);
        }
    }

}