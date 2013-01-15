using glMatrix;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xbim.Xplorer
{
    class viewer_mesh_converter
    {
        public List<vec3> points = new List<vec3>();
        public List<vec3> normals = new List<vec3>();
        public List<Int32[]> uniquePoints = new List<Int32[]>();
        public List<Int32> indices = new List<Int32>();
        public Byte meshType = 0x00;
        public Int32 currentNormalIndex = -1;

        private Int32 _pointTally = 0;
        private Int32 _fanStartIndex = 0;
        private Int32 _lastIndex = 0;
        private Int32 _previousToLastIndex = 0;

        public Int32 addPoint(vec3 point)
        {
            this.points.Add(point);
            return this.points.Count - 1;
        }
        public Int32 setNormal(vec3 point)
        {

            var fnd = -1;
            for (var i = 0; i < this.normals.Count; i++)
            {
                if (this.normals[i].X == point.X
                    && this.normals[i].Y == point.Y
                     && this.normals[i].Z == point.Z
                     )
                {
                    fnd = i;
                    break;
                }
            }
            if (fnd > -1)
            {
                this.currentNormalIndex = fnd;
            }
            else
            {
                this.normals.Add(point);
                this.currentNormalIndex = this.normals.Count - 1;
            }
            return this.currentNormalIndex;

        }
        public void SetType(byte setMeshType)
        {
            this.meshType = setMeshType;
            this._pointTally = 0;
            this._fanStartIndex = 0;
            this._lastIndex = 0;
        }

        public void AddOneMesh(View view)
        {

            var numPoints = view.getInt32();
            if (numPoints == 0)
            {
                return; //nothing to do
            }

            Int32 indexSize;
            if (numPoints <= 0xFF) //we will use byte for indices
                indexSize = 1;
            else if (numPoints <= 0xFFFF)
                indexSize = 2; //use  unsigned short int for indices
            else
                indexSize = 4; //use unsigned int for indices

            // when adding another mesh indices need to be mapped to the new position in the list
            // 
            var basePointCount = this.points.Count;
            for (Int32 i = 0; i < numPoints; i++)
            {
                var p = vec3.create();
                p.X = view.getFloat32();
                p.Y = view.getFloat32();
                p.Z = view.getFloat32();
                this.addPoint(p);
            }
            vec3 currentNormal;

            var numFaces = view.getUint16();
            for (var f = 0; f < numFaces; f++)
            {
                var numPolygons = view.getUint16();
                //get the normals
                var numNormals = view.getUint16();
                for (var n = 0; n < numNormals; n++)
                {
                    //get the face normal
                    var norm = vec3.create();
                    norm.X = (Single)view.getFloat64();
                    norm.Y = (Single)view.getFloat64();
                    norm.Z = (Single)view.getFloat64();
                    currentNormalIndex = this.setNormal(norm);
                }
                for (var p = 0; p < numPolygons; p++)
                {
                    var meshType = view.getUint8();
                    this.SetType(meshType);
                    var indicesCount = view.getUint16();
                    for (var i = 0; i < indicesCount; i++)
                    {
                        UInt32 index;
                        switch (indexSize)
                        {
                            case 1:
                                index = view.getUint8();
                                break;
                            case 2:
                                index = view.getUint16();
                                break;
                            default:
                                index = view.getUint32();
                                break;
                        }
                        this.AddTriangleIndex((Int32)(index + basePointCount));
                    }
                }
            }
        }

        public void AddTriangleIndex(Int32 iIndex)
        {
            var uniqueindex = 0; // this.UniqueIndex(iIndex);
            for (var i = 0; i < this.uniquePoints.Count; i++)
            {
                if (this.uniquePoints[i][0] == iIndex && this.uniquePoints[i][1] == this.currentNormalIndex)
                {
                    uniqueindex = i;
                    break;
                }
            }
            if (uniqueindex == 0)
            {
                this.uniquePoints.Add(new Int32[] { iIndex, this.currentNormalIndex });
                uniqueindex = this.uniquePoints.Count - 1;
            }


            if (this._pointTally == 0)
                this._fanStartIndex = uniqueindex;
            if (this._pointTally < 3) // no one triangle available yet.
            {
                this.indices.Add(uniqueindex);
            }
            else
            {

                switch (this.meshType)
                {

                    case 4: // 0x0004 - TriangleType.GL_TRIANGLES
                        this.indices.Add(uniqueindex);
                        break;
                    case 5: // 0x0005 - TriangleType.GL_TRIANGLE_STRIP
                        if (this._pointTally % 2 == 0)
                        {
                            this.indices.Add(this._previousToLastIndex);
                            this.indices.Add(this._lastIndex);
                        }
                        else
                        {
                            this.indices.Add(this._lastIndex);
                            this.indices.Add(this._previousToLastIndex);
                        }
                        this.indices.Add(uniqueindex);
                        break;
                    case 6: //   0x0006 - TriangleType.GL_TRIANGLE_FAN
                        this.indices.Add(this._fanStartIndex);
                        this.indices.Add(this._lastIndex);
                        this.indices.Add(uniqueindex);
                        break;
                    default:
                        break;
                }
            }
            this._previousToLastIndex = this._lastIndex;
            this._lastIndex = uniqueindex;
            this._pointTally++;
        }
    }
}
