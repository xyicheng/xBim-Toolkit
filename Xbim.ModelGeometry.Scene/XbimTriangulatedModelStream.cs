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

    public class XbimTriangulatedModelStream
    {
        public static readonly XbimTriangulatedModelStream Empty;
        UInt16 _numChildren=0;
        public UInt16 NumChildren
        {
            get
            {
                return _numChildren;
            }
        }

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

        public XbimTriangulatedModelStream(byte[] data, UInt16 numChildren, Byte hasData)
        {
            _dataStream = new MemoryStream(data);
            _numChildren = numChildren;
            _hasData = hasData;
        }
        public Rect3D BoundingBox
        {
            get { throw new NotImplementedException(); }
        }

        //writes the data to the stream and returns the start offset position of the data in the stream
        public void Write(BinaryWriter bw)
        {
            
            if (_dataStream != null)
            {
                bw.Write((int)(_dataStream.Length));
                bw.Write(_numChildren);
                bw.Write(_hasData);
                bw.Write(_dataStream.GetBuffer(), 0, (int)_dataStream.Length);
            }
            else
                bw.Write((int)0);
           
        }

      

        public void AddChild(XbimTriangulatedModelStream child)
        {
            if (child.DataStream.Length > 0)
            {
                _numChildren++;
                _dataStream.Write(child.DataStream.GetBuffer(), 0, (int)child.DataStream.Length);
            }
        }

        public Model3D AsModel3D()
        {
            XbimMeshGeometry3D m3D = new XbimMeshGeometry3D();
            Build(m3D);
            return m3D;
		}

        

        public void Build<TGeomType>(TGeomType builder) where TGeomType : IXbimTriangulatedModelBuilder, new() 
        {
            _dataStream.Seek(0, SeekOrigin.Begin);
            BinaryReader br = new BinaryReader(_dataStream);
            
            builder.BeginBuild();
            if (_hasData > 0) //has data 
                 Build(builder, br);
            for (int i = 0; i < _numChildren; i++)
            {
                builder.BeginChild();
                Build(builder, br);
                builder.EndChild();
            }
            builder.EndBuild();
        }

        private void Build<TGeomType>(TGeomType builder, BinaryReader br)  where TGeomType : IXbimTriangulatedModelBuilder, new() 
        {
            uint numPoints = br.ReadUInt32();
            int indexSize;
            if (numPoints <= 0xFF) //we will use byte for indices
                indexSize = sizeof(byte);
            else if (numPoints <= 0xFFFF)
                indexSize = sizeof(ushort); //use  unsigned short int for indices
            else
                indexSize = sizeof(uint); //use unsigned int for indices
            builder.BeginVertices(numPoints);
            
            for (uint i = 0; i < numPoints; i++)
            {
                double x = br.ReadSingle();
                double y = br.ReadSingle();
                double z = br.ReadSingle();
                builder.AddVertex(new Point3D(x, y, z));
            }
            builder.EndVertices();
            ushort numFaces = br.ReadUInt16();
            builder.BeginFaces(numFaces);
            for (ushort f = 0; f < numFaces; f++)
            {
                builder.BeginFace();
                //get the number of polygons
                ushort numPolygons = br.ReadUInt16();
               
                //get the normals
                ushort numNormals = br.ReadUInt16();
                builder.BeginNormals(numNormals);
                for (ushort n = 0; n < numNormals; n++)
                {
                    //get the face normal
                    double x = br.ReadDouble();
                    double y = br.ReadDouble();
                    double z = br.ReadDouble();
                    Vector3D normal = new Vector3D(x, y, z);
                    builder.AddNormal(normal);
                }
                builder.EndNormals();
                builder.BeginPolygons(numPolygons);
                for (uint p = 0; p < numPolygons; p++)
                {
                    builder.BeginPolygon();
                    //set the state
                    TriangleType meshType = (TriangleType)br.ReadByte();
                    uint indicesCount = br.ReadUInt16();
                    builder.BeginTriangulation(meshType, indicesCount);
                    //get the triangles
                    for (uint i = 0; i < indicesCount; i++)
                    {
                        uint index;
                        switch (indexSize)
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
                        builder.AddTriangleIndex(index);
                    }
                    builder.EndTriangulation();
                    builder.EndPolygon();
                }
                builder.EndPolygons();
                builder.EndFace();
            }
            builder.EndFaces();
        }
    }
}
