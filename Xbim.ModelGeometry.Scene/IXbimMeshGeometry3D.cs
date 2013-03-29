using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common.Geometry;
using Xbim.XbimExtensions;

namespace Xbim.ModelGeometry.Scene
{
    public interface IXbimMeshGeometry3D
    {
        IEnumerable<XbimPoint3D> Positions { get; set; }
        IEnumerable<XbimVector3D> Normals { get; set; }
        IList<Int32> TriangleIndices { get; set; }
        XbimMeshFragmentCollection Meshes { get; set; }
      

        bool Add(XbimGeometryData geometryMeshData);


        void MoveTo(IXbimMeshGeometry3D toMesh);

        void BeginUpdate();

        void EndUpdate();
        /// <summary>
        /// Returns the part of the mesh described in the fragment
        /// </summary>
        /// <param name="frag"></param>
        /// <returns></returns>
        IXbimMeshGeometry3D GetMeshGeometry3D(XbimMeshFragment frag);
    }
}
