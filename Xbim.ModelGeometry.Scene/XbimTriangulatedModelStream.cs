using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Media.Media3D;

namespace Xbim.ModelGeometry.Scene
{
    public enum TriangleType : byte
    {
        GL_TRIANGLES = 0x0004,
        GL_TRIANGLE_STRIP = 0x0005, 
        GL_TRIANGLE_FAN = 0x0006
    }

    /// <summary>
    /// Binary stream encoded triangulated mesh; capable of builing other 
    /// </summary>
    public class XbimTriangulatedModelStream
    {
        public static readonly XbimTriangulatedModelStream Empty;
        // children have been dropped; models are currently merged
        // UInt16 _numChildren=0;
        //public UInt16 NumChildren
        //{
        //    get
        //    {
        //        return _numChildren;
        //    }
        //}

        Byte _hasData = 0;
        public Byte HasData
        {
            get { return _hasData; }
        }

        static XbimTriangulatedModelStream()
        {
            Empty = new XbimTriangulatedModelStream(true);    
        }

        internal XbimTriangulatedModelStream(bool empty)
        {

        }
        public XbimTriangulatedModelStream()
        {
            _dataStream = new MemoryStream(0x4000);
        }
        public bool IsEmpty
        {
            get
            {
                return this == Empty || ((_dataStream == null || _dataStream.Length == 0));
            }
        }

        MemoryStream _dataStream;

        public MemoryStream DataStream
        {
            get { return _dataStream; }
            set { _dataStream = value; }
        }

        public XbimTriangulatedModelStream(byte []  data)
        {
            _dataStream = new MemoryStream(0x4000);
            _dataStream.Write(data, 0, data.Length);
            _hasData = 1;
        }

        public Rect3D BoundingBox
        {
            get { throw new NotImplementedException(); }
        }

        //writes the data to the stream 
        public void Write(BinaryWriter bw)
        {
            
            if (_dataStream != null)
            {
                bw.Write((int)(_dataStream.Length));
                // bw.Write(_numChildren);
                // bw.Write(_hasData);
                bw.Write(_dataStream.GetBuffer(), 0, (int)_dataStream.Length);
            }
            else
                bw.Write((int)0);  
        }

        // this function seems only to be called in meshing XbimGeometryModelCollection
        // 
        public void MergeStream(XbimTriangulatedModelStream other)
        {
            throw new NotImplementedException("Merge to be added.");
            if (other.DataStream.Length > 0)
            {
                // _numChildren++;
                _dataStream.Write(other.DataStream.GetBuffer(), 0, (int)other.DataStream.Length);
            }
        }

        public Model3D AsModel3D()
        {
            XbimMeshGeometry3D m3D = new XbimMeshGeometry3D();
            BuildWithNormals(m3D);
            return m3D;
        }

        private class IndexReader
        {
            private byte _IndexByteSize;
            public IndexReader(uint MaxSize)
            {
                if (MaxSize <= 0xFF) //we will use byte for indices
                    _IndexByteSize = sizeof(byte);
                else if (MaxSize <= 0xFFFF)
                    _IndexByteSize = sizeof(ushort); //use  unsigned short int for indices
                else
                    _IndexByteSize = sizeof(uint); //use unsigned int for indices   
            }

            public uint ReadIndex(BinaryReader br)
            {
                uint index;
                switch (_IndexByteSize)
                {
                    case sizeof(byte):
                        index = br.ReadByte();
                        break;
                    case sizeof(ushort):
                        index = br.ReadUInt16();
                        break;
                    default:
                        index = br.ReadUInt32();
                        break;
                }
                return index;
            }
        }

        // conversion to IXbimTriangulatesToPositionsIndices
        //
        public void Build<TGeomType>(TGeomType builder) where TGeomType : IXbimTriangulatesToPositionsIndices, new() 
        {
            _dataStream.Seek(0, SeekOrigin.Begin);
            BinaryReader br = new BinaryReader(_dataStream);
            
            builder.BeginBuild();
            if (_hasData > 0) //has data 
                 Build(builder, br);

            // children have been removed
            //
            //for (int i = 0; i < _numChildren; i++)
            //{
            //    builder.BeginChild();
            //    Build(builder, br);
            //    builder.EndChild();
            //}
            builder.EndBuild();
        }

        private void Build<TGeomType>(TGeomType builder, BinaryReader br) where TGeomType : IXbimTriangulatesToPositionsIndices, new()
        {
            uint numPositions = br.ReadUInt32();
            uint numNormals = br.ReadUInt32();
            uint numUniques = br.ReadUInt32();
            uint numTriangles = br.ReadUInt32();
            uint numPolygons = br.ReadUInt32();

            IndexReader PositionReader = new IndexReader(numPositions);
            IndexReader NormalsReader = new IndexReader(numNormals);
            IndexReader UniquesReader = new IndexReader(numUniques);


            // coordinates of positions
            //
            builder.BeginPositions(numPositions);
            for (uint i = 0; i < numPositions; i++)
            {
                double x = br.ReadSingle();
                double y = br.ReadSingle();
                double z = br.ReadSingle();
                builder.AddPosition(new Point3D(x, y, z));
            }
            builder.EndPositions();

            // skips normals
            br.BaseStream.Seek(numNormals * sizeof(float) * 3, SeekOrigin.Current);

            // prepares local array of point coordinates.
            uint[] UniqueToPosition = new uint[numUniques];
            for (uint i = 0; i < numUniques; i++)
            {
                uint readposition = PositionReader.ReadIndex(br);
                if (readposition > numPositions)
                {
                    System.Diagnostics.Debug.WriteLine("Error");
                }
                UniqueToPosition[i] = readposition;
                NormalsReader.ReadIndex(br); // just to skip the normal
            }

            builder.BeginPolygons(numTriangles, numPolygons);
            for (uint p = 0; p < numPolygons; p++)
            {
                // set the state
                TriangleType meshType = (TriangleType)br.ReadByte();
                uint indicesCount = br.ReadUInt32();
                builder.BeginPolygon(meshType, indicesCount);
                //get the triangles
                for (uint i = 0; i < indicesCount; i++)
                {
                    uint iUi = UniquesReader.ReadIndex(br);
                    builder.AddTriangleIndex(UniqueToPosition[iUi]);
                }
                builder.EndPolygon();
            }
            builder.EndPolygons();
        }

        
        // conversion to IXbimTriangulatesToPositionsIndices
        //
        public void BuildWithNormals<TGeomType>(TGeomType builder) where TGeomType : IXbimTriangulatesToPositionsNormalsIndices, new()
        {
            _dataStream.Seek(0, SeekOrigin.Begin);
            BinaryReader br = new BinaryReader(_dataStream);

            builder.BeginBuild();
            if (_hasData > 0) //has data 
                BuildWithNormals(builder, br);

            // children have been removed
            //
            //for (int i = 0; i < _numChildren; i++)
            //{
            //    builder.BeginChild();
            //    Build(builder, br);
            //    builder.EndChild();
            //}
            builder.EndBuild();
        }

        private void BuildWithNormals<TGeomType>(TGeomType builder, BinaryReader br) where TGeomType : IXbimTriangulatesToPositionsNormalsIndices, new()
        {
            uint numPositions = br.ReadUInt32();
            uint numNormals = br.ReadUInt32();
            uint numUniques = br.ReadUInt32();
            uint numTriangles = br.ReadUInt32();
            uint numPolygons = br.ReadUInt32();

            IndexReader PositionReader = new IndexReader(numPositions);
            IndexReader NormalsReader = new IndexReader(numNormals);
            IndexReader UniquesReader = new IndexReader(numUniques);

            float[,] pos = new float[numPositions,3];
            float[,] nrm = new float[numNormals, 3];


            // coordinates of positions
            //
            for (uint i = 0; i < numPositions; i++)
            {
                pos[i, 0] = br.ReadSingle();
                pos[i, 1] = br.ReadSingle();
                pos[i, 2] = br.ReadSingle();
            }
            // dimensions of normals
            //
            for (uint i = 0; i < numNormals; i++)
            {
                nrm[i, 0] = br.ReadSingle();
                nrm[i, 1] = br.ReadSingle();
                nrm[i, 2] = br.ReadSingle();
            }

            builder.BeginPoints(numUniques);
            for (uint i = 0; i < numUniques; i++)
            {
                uint readpositionI = PositionReader.ReadIndex(br);
                uint readnormalI  = NormalsReader.ReadIndex(br);
                System.Diagnostics.Debug.WriteLine("PosNrm: " + readpositionI + " " + readnormalI);
                builder.AddPoint(
                    new Point3D(pos[readpositionI,0],pos[readpositionI,1],pos[readpositionI,2]),
                    new Vector3D(nrm[readnormalI, 0], nrm[readnormalI, 1], nrm[readnormalI, 2])
                    );
            }
            builder.EndPoints();

            builder.BeginPolygons(numTriangles, numPolygons);
            for (uint p = 0; p < numPolygons; p++)
            {
                // set the state
                TriangleType meshType = (TriangleType)br.ReadByte();
                uint indicesCount = br.ReadUInt32();
                builder.BeginPolygon(meshType, indicesCount);
                //get the triangles
                for (uint i = 0; i < indicesCount; i++)
                {
                    builder.AddTriangleIndex(UniquesReader.ReadIndex(br));
                }
                builder.EndPolygon();
            }
            builder.EndPolygons();
        }
    }
}
