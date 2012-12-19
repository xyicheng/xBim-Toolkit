using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Xbim.ModelGeometry.Scene
{
    /// <summary>
    /// Binary stream of triangles with posisiton, normals, indices.
    /// Uint32 numPointsAndNormals
    /// Uint32 numTriangles
    /// numPointsAndNormals * 6 floats (3 float posistions + 3 float normals)
    /// numTriangles * 3 uint (three indices)
    /// </summary>
    public class PositionsNormalsIndicesBinaryStreamWriter : IXbimTriangulatesToSimplePositionsNormalsIndices
    {
        private int _size = 0;
        public MemoryStream Stream;
        private BinaryWriter _bw;

        public PositionsNormalsIndicesBinaryStreamWriter()
        {
            
        }

        public PositionsNormalsIndicesBinaryStreamWriter(byte[] ShapeData)
        {
            XbimTriangulatedModelStream SourceStream = new XbimTriangulatedModelStream(ShapeData);
            SourceStream.BuildPNI(this);
        }

        public void FromXbimTriangulatedModelStream(XbimTriangulatedModelStream SourceStream)
        {
            SourceStream.BuildPNI(this);
        }

        #region IXbimTriangulatesToSimplePositionsNormalsIndices Members

        void IXbimTriangulatesToSimplePositionsNormalsIndices.BeginBuild(uint PointsCount, uint TrianglesCount)
        {
            _size = (int) (
                2 * sizeof(uint) + // number of points and number of triangles
                sizeof(float) * 6 * PointsCount +  // space for points (3 * position + 3 * normal) for each point
                sizeof(uint) * 3 * TrianglesCount // space for triangle indices 
                );
            Stream = new MemoryStream(_size);
            _bw = new BinaryWriter(Stream);
            _bw.Write(PointsCount);
            _bw.Write(TrianglesCount);
        }

        void IXbimTriangulatesToSimplePositionsNormalsIndices.BeginPoints(uint PointsCount)
        {
            // purposely empty 
        }

        void IXbimTriangulatesToSimplePositionsNormalsIndices.AddPoint(float px, float py, float pz, float nx, float ny, float nz)
        {
            _bw.Write(px);
            _bw.Write(py);
            _bw.Write(pz);

            _bw.Write(nx);
            _bw.Write(ny);
            _bw.Write(nz);
        }

        void IXbimTriangulatesToSimplePositionsNormalsIndices.BeginTriangles(uint totalNumberTriangles)
        {
            // purposely empty (todo: checks could be added to verify position matches with expected)
        }

        void IXbimTriangulatesToSimplePositionsNormalsIndices.AddTriangleIndex(uint index)
        {
            _bw.Write(index);
        }

        void IXbimTriangulatesToSimplePositionsNormalsIndices.EndBuild()
        {
            if (Stream.Position != _size)
            {
                throw new Exception("PositionsNormalsIndicesBinaryStreamWriter size not correct.");
            }
        }

        #endregion
    }
}
