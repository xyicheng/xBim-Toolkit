using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.Common.Geometry;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions;

namespace Xbim.Presentation
{
    public class WpfMeshGeometry3D : IXbimMeshGeometry3D
    {
        public GeometryModel3D WpfMesh;
        XbimMeshFragmentCollection meshes = new XbimMeshFragmentCollection();

        public void ReportGeometryTo(StringBuilder sb)
        {
            int i = 0;
            var pEn = Positions.GetEnumerator();
            var nEn = Normals.GetEnumerator();
            while (pEn.MoveNext() && nEn.MoveNext())
            {
                var p = pEn.Current;
                var n = nEn.Current;
                sb.AppendFormat("{0} pos: {1} nrm:{2}\r\n", i++, p, n);
            }

            i = 0;
            sb.AppendLine("Triangles:");
            foreach (var item in TriangleIndices)
            {
                sb.AppendFormat("{0}, ", item);
                i++;
                if (i % 3 == 0)
                {
                    sb.AppendLine();
                }
            }
        }

        public WpfMeshGeometry3D()
        {

        }
        public WpfMeshGeometry3D(IXbimMeshGeometry3D mesh)
        {
            WpfMesh = new GeometryModel3D();
            WpfMesh.Geometry = new MeshGeometry3D();
            Mesh.Positions = new WpfPoint3DCollection(mesh.Positions);
            Mesh.Normals = new WpfVector3DCollection(mesh.Normals);
            Mesh.TriangleIndices = new Int32Collection (mesh.TriangleIndices);
            meshes = new XbimMeshFragmentCollection(mesh.Meshes);
        }
        
        public static implicit operator GeometryModel3D(WpfMeshGeometry3D mesh)
        {
             if(mesh.WpfMesh==null)
                mesh.WpfMesh=new GeometryModel3D();
             return mesh.WpfMesh;
        }

        public MeshGeometry3D Mesh
        {
            get
            { 
                return WpfMesh.Geometry as MeshGeometry3D;
            }
        }
        //public IEnumerable<XbimPoint3D> Positions
        //{
        //    get { return Mesh.Positions; }
        //}

        //public IList<XbimVector3D> Normals
        //{
        //    get { return Mesh.Normals; }
        //}

        //public IList<int> TriangleIndices
        //{
        //    get { return Mesh.TriangleIndices; }
        //}



        public XbimMeshFragmentCollection Meshes
        {
            get { return meshes; }
            set
            {
                meshes = new XbimMeshFragmentCollection(value);
            }
        }

        /// <summary>
        /// Do not use this rather create a XbimMeshGeometry3D first and construct this from it, appending WPF collections is slow
        /// </summary>
        /// <param name="geometryMeshData"></param>
        public bool Add(XbimGeometryData geometryMeshData)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<XbimPoint3D> Positions
        {
            get { return new WpfPoint3DCollection(Mesh.Positions); }
            set
            {
                Mesh.Positions = new WpfPoint3DCollection(value);
            }
        }

        public IEnumerable<XbimVector3D> Normals
        {
            get { return new WpfVector3DCollection(Mesh.Normals); }
            set
            {
                Mesh.Normals = new WpfVector3DCollection(value);
            }
        }

        public IList<int> TriangleIndices
        {
            get { return Mesh.TriangleIndices; }
            set
            {
                Mesh.TriangleIndices = new Int32Collection(value);
            }
        }

        public void MoveTo(IXbimMeshGeometry3D toMesh)
        {
            if (meshes.Any()) //if no meshes nothing to move
            {
                toMesh.BeginUpdate();
                
                toMesh.Positions = new List<XbimPoint3D>(this.Positions); 
                toMesh.Normals = new List<XbimVector3D>(this.Normals); 
                toMesh.TriangleIndices = new List<int>(this.TriangleIndices);

                toMesh.Meshes = new XbimMeshFragmentCollection(this.Meshes); this.meshes.Clear();
                WpfMesh.Geometry = new MeshGeometry3D();
                toMesh.EndUpdate();
            }
        }

        public void BeginUpdate()
        {
            if (WpfMesh == null)
                WpfMesh = new GeometryModel3D();
            WpfMesh.Geometry = new MeshGeometry3D();
        }

        public void EndUpdate()
        {
            WpfMesh.Geometry.Freeze();
        }

        public GeometryModel3D ToGeometryModel3D()
        {
            return WpfMesh;
        }

        public MeshGeometry3D GetWpfMeshGeometry3D(XbimMeshFragment frag)
        {
            MeshGeometry3D m3d = new MeshGeometry3D();
            var m = Mesh;
            if (m != null)
            {
                for (int i = frag.StartPosition; i <= frag.EndPosition; i++)
                {   
                    Point3D p = m.Positions[i];
                    m3d.Positions.Add(p);
                    if (m.Normals != null)
                    {
                        Vector3D v = m.Normals[i];
                        m3d.Normals.Add(v);
                    }
                }
                for (int i = frag.StartTriangleIndex; i <= frag.EndTriangleIndex; i++)
                {
                    m3d.TriangleIndices.Add(m.TriangleIndices[i] - frag.StartPosition);
                }
            }
            return m3d;
        }

        public IXbimMeshGeometry3D GetMeshGeometry3D(XbimMeshFragment frag)
        { 
            XbimMeshGeometry3D m3d = new XbimMeshGeometry3D();
            var m = Mesh;
            if (m != null)
            {
                for (int i = frag.StartPosition; i <= frag.EndPosition; i++)
                {
                    Point3D p = m.Positions[i];
                    Vector3D v = m.Normals[i];
                    m3d.Positions.Add(new XbimPoint3D(p.X, p.Y, p.Z));
                    m3d.Normals.Add(new XbimVector3D(v.X, v.Y, v.Z));
                }
                for (int i = frag.StartTriangleIndex; i <= frag.EndTriangleIndex; i++)
                {
                    m3d.TriangleIndices.Add(m.TriangleIndices[i] - frag.StartPosition);
                }
                m3d.Meshes.Add(new XbimMeshFragment(0, 0)
                {
                    EndPosition = m3d.PositionCount - 1,
                    StartTriangleIndex = frag.StartTriangleIndex - m3d.PositionCount - 1,
                    EndTriangleIndex = frag.EndTriangleIndex - m3d.PositionCount - 1
                });
            }
            return m3d;
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


        public void Add(IXbimGeometryModel geometryModel)
        {
            Add((IXbimMeshGeometry3D)geometryModel);
        }

        /// <summary>
        /// Adds a geometry mesh to this, includes all mesh fragments
        /// </summary>
        /// <param name="geom"></param>
        public void Add(IXbimMeshGeometry3D geom)
        {
            //if (geom.Positions.Any()) //if no positions nothing to add
            //{
            //    this.BeginUpdate();
            //    int startPos = Mesh.Positions.Count;
            //    int startIndices = Mesh.TriangleIndices.Count;
            //    foreach (var pos in geom.Positions)
            //    {
            //        Mesh.Positions.Add(pos);
            //    }
            //     Mesh.Positions.AddRange(geom.Positions);
            //    Mesh.Normals.AddRange(geom.Normals);
            //    foreach (var indices in geom.TriangleIndices)
            //        TriangleIndices.Add(indices + startPos);
            //    foreach (var fragment in geom.Meshes)
            //    {
            //        fragment.Offset(startPos, startIndices);
            //        Meshes.Add(fragment);
            //    }

            //    this.EndUpdate();
            //}
        }
    }
}
