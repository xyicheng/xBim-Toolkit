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
        
        public static implicit operator GeometryModel3D(WpfMeshGeometry3D mesh)
        {
             if(mesh.WpfMesh==null)
                mesh.WpfMesh=new GeometryModel3D();
             return mesh.WpfMesh;
        }

        private MeshGeometry3D Mesh
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
        /// Do not use this rather create a XbimMeshGeometry3D first and construc this from it, appending WPF collections is slow
        /// </summary>
        /// <param name="geometryMeshData"></param>
        public void Append(XbimGeometryData geometryMeshData)
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
                
                toMesh.Positions = this.Positions; 
                toMesh.Normals = this.Normals; 
                toMesh.TriangleIndices = this.TriangleIndices; 
                toMesh.Meshes = this.Meshes; this.meshes.Clear();
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
    }
}
