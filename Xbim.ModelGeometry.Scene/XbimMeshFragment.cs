using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.ModelGeometry.Scene
{
    public struct XbimMeshFragment
    {
        public int StartPosition;
        public int EndPosition;
        public int EntityLabel;
        public Type EntityType;
        public int StartTriangleIndex;
        public int EndTriangleIndex;
        
        public XbimMeshFragment(int pStart, int tStart)
        {
            StartPosition = EndPosition = pStart;
            StartTriangleIndex = EndTriangleIndex = tStart;
            EntityLabel = 0;
            EntityType = null;
        }

        public bool IsEmpty 
        {
            get
            {
                return StartPosition == EndPosition;
            }
        }

        public bool Contains(int vertexIndex)
        {
            return StartPosition <= vertexIndex && EndPosition >= vertexIndex;
        }

        /// <summary>
        /// Returns a mesh of the fragment
        /// </summary>
        /// <param name="mainMesh"></param>
        /// <returns></returns>
        public XbimMeshGeometry3D GetMeshGeometry3D(IXbimMeshGeometry3D mainMesh)
        {
            XbimMeshGeometry3D mesh = new XbimMeshGeometry3D(PositionCount);
            for (int i = StartPosition; i <= EndPosition; i++)
            {
                mesh.Positions.Add(mainMesh.Positions[i]);
                mesh.Normals.Add(mainMesh.Normals[i]);
            }
            for (int i = StartTriangleIndex; i <= EndTriangleIndex; i++)
            {
                mesh.TriangleIndices.Add(mainMesh.TriangleIndices[i] - StartPosition);
            }
            return mesh;
        }

        public int PositionCount 
        {
            get
            {
                return EndPosition - StartPosition;
            }
        }
    }
}
