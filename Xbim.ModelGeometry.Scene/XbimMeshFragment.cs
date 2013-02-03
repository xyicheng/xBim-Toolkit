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



        public int PositionCount 
        {
            get
            {
                return EndPosition - StartPosition;
            }
        }
    }
}
