using System;

using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.ModelGeometry.Scene
{
    public class XbimMeshComparer : Comparer<XbimMeshFragment>
    {
        public override int Compare(XbimMeshFragment x, XbimMeshFragment y)
        {
            if (x.EndPosition < y.StartPosition) //x is less than y
                return -1;
            else if (x.StartPosition > y.EndPosition) //x is greater than y
                return 1;
            else //x and y overlap
                return 0;
        }
    }

    public struct XbimMeshFragment
    {
        public int StartPosition;
        public int EndPosition;
        public int EntityLabel;
        public Type EntityType;
        public int StartTriangleIndex;
        public int EndTriangleIndex;
        public int GeometryId;

        public XbimMeshFragment(int pStart, int tStart)
        {
            StartPosition = EndPosition = pStart;
            StartTriangleIndex = EndTriangleIndex = tStart;
            EntityLabel = 0;
            EntityType = null;
            GeometryId = 0;
        }

        public XbimMeshFragment(int pStart, int tStart, Type productType, int productLabel, int geometryLabel)
        {

            this.StartPosition = EndPosition = pStart;
            this.StartTriangleIndex = EndTriangleIndex = tStart;
            this.EntityType = productType;
            this.EntityLabel = productLabel;
            this.GeometryId = geometryLabel;
        }

        public bool IsEmpty
        {
            get
            {
                return StartPosition >= EndPosition;
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
                return EndPosition - StartPosition + 1;
            }
        }

        /// <summary>
        /// Offsets the start of the fragment positions and triangle indices 
        /// </summary>
        /// <param name="startPos"></param>
        internal void Offset(int startPos, int startIndex)
        {
            StartPosition += startPos;
            EndPosition += startPos;
            StartTriangleIndex += startIndex;
            EndTriangleIndex += startIndex;

        }
    }
}