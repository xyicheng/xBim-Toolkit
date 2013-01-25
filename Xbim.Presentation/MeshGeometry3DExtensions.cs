using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace Xbim.Presentation
{
    public static class MeshGeometry3DExtensions
    {
        /// <summary>
        /// Adds the mesh to the current mesh and returns a new mesh containing both
        /// </summary>
        /// <param name="sourceMesh">The mesh to add the new content to</param>
        /// <param name="toAdd">The mesh to add</param>
        /// <returns></returns>
        public static Geometry3D Append(this Geometry3D sourceMesh, MeshGeometry3D toAdd)
        {
            
            MeshGeometry3D addTo = sourceMesh as MeshGeometry3D;
            Debug.Assert(addTo!=null);

            MeshGeometry3D m3d = new MeshGeometry3D();

            
            m3d.Positions = new Point3DCollection(addTo.Positions.Count + toAdd.Positions.Count);
            foreach (var pt in addTo.Positions) m3d.Positions.Add(pt);
            foreach (var pt in toAdd.Positions) m3d.Positions.Add(pt);


            m3d.Normals = new Vector3DCollection(addTo.Normals.Count + toAdd.Normals.Count);
            foreach (var v in addTo.Normals) m3d.Normals.Add(v);
            foreach (var v in toAdd.Normals) m3d.Normals.Add(v);

            int maxIndices = addTo.Positions.Count; //we need to increment all indices by this amount
            m3d.TriangleIndices = new Int32Collection(addTo.TriangleIndices.Count + toAdd.TriangleIndices.Count);
            foreach (var i in addTo.TriangleIndices) m3d.TriangleIndices.Add(i);
            foreach (var i in toAdd.TriangleIndices) m3d.TriangleIndices.Add(i + maxIndices);
           // m3d.Freeze();
            return m3d;
        }   

       
    }
}
