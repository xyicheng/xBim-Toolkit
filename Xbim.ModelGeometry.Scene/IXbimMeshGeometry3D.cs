using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using Xbim.XbimExtensions;

namespace Xbim.ModelGeometry.Scene
{
    public interface IXbimMeshGeometry3D
    {
        IList<Point3D> Positions { get; set; }
        IList<Vector3D> Normals { get; set; }
        IList<Int32> TriangleIndices { get; set; }
        XbimMeshFragmentCollection Meshes { get; set; }
      

        void Append(XbimGeometryData geometryMeshData);


        void MoveTo(IXbimMeshGeometry3D toMesh);

        void BeginUpdate();

        void EndUpdate();
    }
}
