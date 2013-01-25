using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;

namespace Xbim.ModelGeometry.Scene
{
    /// <summary>
    /// This class provide support for geoemtry triangulated neshes
    /// </summary>
    public class XbimMeshGeometry3D : IXbimTriangulatesToPositionsIndices, IXbimTriangulatesToPositionsNormalsIndices
    {

        const int defaultSize = 0x4000;
        public List<Point3D> Positions;
        public List<Vector3D> Normals;
        public List<Int32> TriangleIndices;
        List<Point3D> _points;
        TriangleType _meshType;
        uint _previousToLastIndex;
        uint _lastIndex;
        uint _pointTally;
        uint _fanStartIndex;
        uint indexOffset;
        private int p;

        public XbimMeshGeometry3D(int size)
        {
            Positions = new List<Point3D>(size);
            Normals = new List<Vector3D>(size);
            TriangleIndices = new List<Int32>(size * 3);
        }

        public XbimMeshGeometry3D() :this(defaultSize)
        {

        }

        static public XbimMeshGeometry3D MakeBoundingBox(Rect3D r3D, Matrix3D transform)
        {
            XbimMeshGeometry3D mesh = new XbimMeshGeometry3D(8);
            Point3D p0 = transform.Transform(r3D.Location);
            Point3D p1 = p0;
            p1.X += r3D.SizeX;
            Point3D p2 = p1;
            p2.Z += r3D.SizeZ;
            Point3D p3 = p2;
            p3.X -= r3D.SizeX;
            Point3D p4 = p3;
            p4.Y += r3D.SizeY;
            Point3D p5 = p4;
            p5.Z -= r3D.SizeZ;
            Point3D p6 = p5;
            p6.X += r3D.SizeX;
            Point3D p7 = p6;
            p7.Z += r3D.SizeZ;


            mesh.Positions.Add(p0);
            mesh.Positions.Add(p1);
            mesh.Positions.Add(p2);
            mesh.Positions.Add(p3);
            mesh.Positions.Add(p4);
            mesh.Positions.Add(p5);
            mesh.Positions.Add(p6);
            mesh.Positions.Add(p7);

            mesh.TriangleIndices.Add(3);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(2);

            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(2);

            mesh.TriangleIndices.Add(4);
            mesh.TriangleIndices.Add(5);
            mesh.TriangleIndices.Add(3);

            mesh.TriangleIndices.Add(5);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(3);

            mesh.TriangleIndices.Add(7);
            mesh.TriangleIndices.Add(6);
            mesh.TriangleIndices.Add(4);

            mesh.TriangleIndices.Add(6);
            mesh.TriangleIndices.Add(5);
            mesh.TriangleIndices.Add(4);

            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(7);

            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(6);
            mesh.TriangleIndices.Add(7);

            mesh.TriangleIndices.Add(4);
            mesh.TriangleIndices.Add(3);
            mesh.TriangleIndices.Add(7);

            mesh.TriangleIndices.Add(3);
            mesh.TriangleIndices.Add(2);
            mesh.TriangleIndices.Add(7);

            mesh.TriangleIndices.Add(6);
            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(5);

            mesh.TriangleIndices.Add(1);
            mesh.TriangleIndices.Add(0);
            mesh.TriangleIndices.Add(5);

            return mesh;
        }

      

        #region standard calls

        private void Init()
        {
            indexOffset = (uint)Positions.Count;
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
            _points = new List<Point3D>((int)numPoints);
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
            //_meshGeometry.Positions = new Point3DCollection((int)(totalNumberTriangles * 3));
            //_meshGeometry.TriangleIndices = new System.Windows.Media.Int32Collection((int)(totalNumberTriangles * 3));
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
                TriangleIndices.Add(Positions.Count);
                Positions.Add(_points[(int)index]);
            }
            else
            {
                switch (_meshType)
                {
                    case TriangleType.GL_TRIANGLES://      0x0004
                        TriangleIndices.Add(Positions.Count);
                        Positions.Add(_points[(int)index]);
                        break;
                    case TriangleType.GL_TRIANGLE_STRIP:// 0x0005
                        if (_pointTally % 2 == 0)
                        {
                            TriangleIndices.Add(Positions.Count);
                            Positions.Add(_points[(int)_previousToLastIndex]);
                            TriangleIndices.Add(Positions.Count);
                            Positions.Add(_points[(int)_lastIndex]);
                        }
                        else
                        {
                            TriangleIndices.Add(Positions.Count);
                            Positions.Add(_points[(int)_lastIndex]);
                            TriangleIndices.Add(Positions.Count);
                            Positions.Add(_points[(int)_previousToLastIndex]);
                        }
                        TriangleIndices.Add(Positions.Count);
                        Positions.Add(_points[(int)index]);
                        break;
                    case TriangleType.GL_TRIANGLE_FAN://   0x0006
                        TriangleIndices.Add(Positions.Count);
                        Positions.Add(_points[(int)_fanStartIndex]);
                        TriangleIndices.Add(Positions.Count);
                        Positions.Add(_points[(int)_lastIndex]);
                        TriangleIndices.Add(Positions.Count);
                        Positions.Add(_points[(int)index]);
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
           
        }

        void IXbimTriangulatesToPositionsNormalsIndices.AddPosition(Point3D point3D)
        {
            Positions.Add(point3D);
        }

        void IXbimTriangulatesToPositionsNormalsIndices.AddNormal(Vector3D normal)
        {
            Normals.Add(normal);
        }

        void IXbimTriangulatesToPositionsNormalsIndices.EndPoints()
        {
            // purposely empty
        }

        void IXbimTriangulatesToPositionsNormalsIndices.BeginPolygons(uint totalNumberTriangles, uint numPolygons)
        {
            
        }

        void IXbimTriangulatesToPositionsNormalsIndices.BeginPolygon(TriangleType meshType, uint indicesCount)
        {
            StandardBeginPolygon(meshType);
        }

        private int  Offset(uint index)
        {
            return (int)(index + indexOffset);
        }

        void IXbimTriangulatesToPositionsNormalsIndices.AddTriangleIndex(uint index)
        {
            
            if (_pointTally == 0)
                _fanStartIndex = index;
            if (_pointTally < 3) //first time
            {
                TriangleIndices.Add(Offset(index));
                // _meshGeometry.Positions.Add(_points[(int)index]);
            }
            else
            {
                switch (_meshType)
                {
                    case TriangleType.GL_TRIANGLES://      0x0004
                        TriangleIndices.Add(Offset(index));
                        break;
                    case TriangleType.GL_TRIANGLE_STRIP:// 0x0005
                        if (_pointTally % 2 == 0)
                        {
                            TriangleIndices.Add(Offset(_previousToLastIndex));
                            TriangleIndices.Add(Offset(_lastIndex));
                        }
                        else
                        {
                            TriangleIndices.Add(Offset(_lastIndex));
                            TriangleIndices.Add(Offset(_previousToLastIndex));
                        }
                        TriangleIndices.Add(Offset(index));
                        break;
                    case TriangleType.GL_TRIANGLE_FAN://   0x0006
                        TriangleIndices.Add(Offset(_fanStartIndex));
                        TriangleIndices.Add(Offset(_lastIndex));
                        TriangleIndices.Add(Offset(index));
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

        //adds the content of the toAdd to this
        public void Add(XbimMeshGeometry3D toAdd)
        {
            Positions.AddRange(toAdd.Positions);
            Normals.AddRange(toAdd.Normals);
            TriangleIndices.AddRange(toAdd.TriangleIndices);      
        }
    }
}
