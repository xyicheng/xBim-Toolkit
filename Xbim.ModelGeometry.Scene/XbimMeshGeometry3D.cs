using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xbim.Common.Exceptions;
using Xbim.Common.Geometry;
using Xbim.IO;
using Xbim.XbimExtensions;

namespace Xbim.ModelGeometry.Scene
{
    /// <summary>
    /// This class provide support for geoemtry triangulated neshes
    /// </summary>
    public class XbimMeshGeometry3D : IXbimMeshGeometry3D, IXbimTriangulatesToPositionsIndices, IXbimTriangulatesToPositionsNormalsIndices
    {

        const int defaultSize = 0x4000;
        public List<XbimPoint3D> Positions;
        public List<XbimVector3D> Normals;
        public List<Int32> TriangleIndices;

        XbimMeshFragmentCollection meshes = new XbimMeshFragmentCollection();
        List<XbimPoint3D> _points;
        TriangleType _meshType;
        uint _previousToLastIndex;
        uint _lastIndex;
        uint _pointTally;
        uint _fanStartIndex;
        uint indexOffset;
       

        public XbimMeshGeometry3D(int size)
        {
            Positions = new List<XbimPoint3D>(size);
            Normals = new List<XbimVector3D>(size);
            TriangleIndices = new List<Int32>(size * 3);
        }

        public XbimMeshGeometry3D() :this(defaultSize)
        {

        }

        static public XbimMeshGeometry3D MakeBoundingBox(XbimRect3D r3D, XbimMatrix3D transform)
        {
            XbimMeshGeometry3D mesh = new XbimMeshGeometry3D(8);
            XbimPoint3D p0 = transform.Transform(r3D.Location);
            XbimPoint3D p1 = p0;
            p1.X += r3D.SizeX;
            XbimPoint3D p2 = p1;
            p2.Z += r3D.SizeZ;
            XbimPoint3D p3 = p2;
            p3.X -= r3D.SizeX;
            XbimPoint3D p4 = p3;
            p4.Y += r3D.SizeY;
            XbimPoint3D p5 = p4;
            p5.Z -= r3D.SizeZ;
            XbimPoint3D p6 = p5;
            p6.X += r3D.SizeX;
            XbimPoint3D p7 = p6;
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
            _points = new List<XbimPoint3D>((int)numPoints);
        }

        void IXbimTriangulatesToPositionsIndices.AddPosition(XbimPoint3D XbimPoint3D)
        {
            _points.Add(XbimPoint3D);
        }

        void IXbimTriangulatesToPositionsIndices.EndPositions()
        {
        }

        void IXbimTriangulatesToPositionsIndices.BeginPolygons(uint totalNumberTriangles, uint numPolygons)
        {
            // three position for each triangle
            //_meshGeometry.Positions = new XbimPoint3DCollection((int)(totalNumberTriangles * 3));
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

        void IXbimTriangulatesToPositionsNormalsIndices.AddPosition(XbimPoint3D XbimPoint3D)
        {
            Positions.Add(XbimPoint3D);
        }

        void IXbimTriangulatesToPositionsNormalsIndices.AddNormal(XbimVector3D normal)
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

        //adds the content of the toAdd to this, it is added as a single mesh fragment, any meshes in toAdd are lost
        public void Add(IXbimMeshGeometry3D toAdd, int entityLabel, Type ifcType)
        {
            int startPosition = Positions.Count;
            XbimMeshFragment fragment = new XbimMeshFragment(startPosition, TriangleIndexCount);
            Positions.AddRange(toAdd.Positions);
            Normals.AddRange(toAdd.Normals);
            foreach (var idx in toAdd.TriangleIndices)
                 TriangleIndices.Add(idx+startPosition);
            fragment.EndPosition = PositionCount - 1;
            fragment.EndTriangleIndex = TriangleIndexCount - 1;
            fragment.EntityLabel = entityLabel;
            fragment.EntityType = ifcType;
            meshes.Add(fragment);
        }


        public int PositionCount
        {
            get { return Positions.Count; }
        }

        public int TriangleIndexCount
        {
            get { return TriangleIndices.Count; }
        }

        IEnumerable<XbimPoint3D> IXbimMeshGeometry3D.Positions
        {
            get { return Positions; }
            set { Positions = new List<XbimPoint3D>(value); }
        }

        IEnumerable<XbimVector3D> IXbimMeshGeometry3D.Normals
        {
            get { return Normals; } 
            set { Normals = new List<XbimVector3D>(value); }
        }
       

        IList<int> IXbimMeshGeometry3D.TriangleIndices
        {
            get { return TriangleIndices; }
            set { TriangleIndices = new List<int>(value); }
        }


        public XbimMeshFragmentCollection Meshes
        {
            get { return meshes; }
            set
            {
                meshes = new XbimMeshFragmentCollection(value);
            }
        }

        /// <summary>
        /// Appends a geometry data object to the Mesh, returns false if the mesh would become too big and needs splitting
        /// </summary>
        /// <param name="geometryMeshData"></param>
        public bool Add(XbimGeometryData geometryMeshData)
        {
            XbimMatrix3D transform = geometryMeshData.Transform;
            if (geometryMeshData.GeometryType == XbimGeometryType.TriangulatedMesh)
            {
                XbimTriangulatedModelStream strm = new XbimTriangulatedModelStream(geometryMeshData.ShapeData);
                XbimMeshFragment fragment = strm.BuildWithNormals(this, transform);
                if (fragment.EntityLabel==int.MinValue) //nothing was added due to size being exceeded
                    return false;
                else //added ok
                {
                    fragment.EntityLabel = geometryMeshData.IfcProductLabel;
                    fragment.EntityType = IfcMetaData.GetType(geometryMeshData.IfcTypeId);
                    meshes.Add(fragment);
                }
            }
            else if (geometryMeshData.GeometryType == XbimGeometryType.BoundingBox)
            {
                XbimRect3D r3d = XbimRect3D.FromArray(geometryMeshData.ShapeData);
                this.Add(XbimMeshGeometry3D.MakeBoundingBox(r3d, transform), geometryMeshData.IfcProductLabel, IfcMetaData.GetType(geometryMeshData.IfcTypeId));
            }
            else
                throw new XbimException("Illegal geometry type found");
            return true;
        }

        /// <summary>
        /// Moves the content of this mesh to the other
        /// </summary>
        /// <param name="toMesh"></param>
        public void MoveTo(IXbimMeshGeometry3D toMesh)
        {
            if (meshes.Any()) //if no meshes nothing to move
            {
                toMesh.BeginUpdate();
                toMesh.Positions = this.Positions; this.Positions.Clear();
                toMesh.Normals = this.Normals; this.Normals.Clear();
                toMesh.TriangleIndices = this.TriangleIndices; this.TriangleIndices.Clear();
                toMesh.Meshes = this.Meshes; this.meshes.Clear();
                toMesh.EndUpdate();
            }
        }


        public void BeginUpdate()
        {
            
        }

        public void EndUpdate()
        {
            
        }


        public IXbimMeshGeometry3D GetMeshGeometry3D(XbimMeshFragment frag)
        {
            XbimMeshGeometry3D m3d = new XbimMeshGeometry3D();
            for (int i = frag.StartPosition; i <= frag.EndPosition; i++)
            {
                m3d.Positions.Add(this.Positions[i]);
                m3d.Normals.Add(this.Normals[i]);
            }
            for (int i = frag.StartTriangleIndex; i <= frag.EndTriangleIndex; i++)
            {
                m3d.TriangleIndices.Add(this.TriangleIndices[i] - frag.StartPosition);
            }
            return m3d;
        }

        /// <summary>
        /// Adds a geometry mesh to this, includes all mesh fragments
        /// </summary>
        /// <param name="geom"></param>
        public void Add(IXbimMeshGeometry3D geom)
        {
            if (geom.Positions.Any()) //if no positions nothing to add
            {
                this.BeginUpdate();
                int startPos = Positions.Count;
                int startIndices = TriangleIndices.Count;
                Positions.AddRange(geom.Positions);
                Normals.AddRange(geom.Normals);
                foreach (var indices in geom.TriangleIndices)
                    TriangleIndices.Add(indices + startPos);
                foreach (var fragment in geom.Meshes)
                {
                    fragment.Offset(startPos, startIndices);
                    Meshes.Add(fragment);
                }

                this.EndUpdate();
            }
        }

        public byte[] ToByteArray()
        {
            //calculate the indices count
            int pointCount = 0;
            foreach (var mesh in meshes)
                pointCount += mesh.EndTriangleIndex - mesh.StartTriangleIndex + 1;
            byte[] bytes = new byte[pointCount * ((6 * sizeof(float)) + sizeof(int))]; //max size of data stream
            MemoryStream ms = new MemoryStream(bytes);
            BinaryWriter bw = new BinaryWriter(ms);
            foreach (var mesh in meshes)
            {
                int label = mesh.EntityLabel;
                label =  ((label & 0xff) << 24) + ((label & 0xff00) << 8) + ((label & 0xff0000) >> 8) + ((label >> 24) & 0xff);
                for (int i = mesh.StartTriangleIndex; i <= mesh.EndTriangleIndex; i++)
                {
                    XbimPoint3D pt = Positions[TriangleIndices[i]];
                    XbimVector3D n = Normals[TriangleIndices[i]];
                    bw.Write(pt.X); bw.Write(pt.Y); bw.Write(pt.Z);
                    bw.Write(n.X); bw.Write(n.Y); bw.Write(n.Z);
                    bw.Write(label); 
                }
            }
            return bytes;
        }


        public XbimRect3D GetBounds()
        {
            bool first = true;
            XbimRect3D boundingBox = XbimRect3D.Empty;
            foreach (var pos in Positions)
            {
                if (first)
                {
                    boundingBox = new XbimRect3D(pos);
                    first = false;
                }
                else
                    boundingBox.Union(pos);

            }
            return boundingBox;
        }
    }
}