using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.Kernel;
using System.IO;
using Xbim.XbimExtensions;

namespace Xbim.ModelGeometry.Scene
{
    public class XbimSceneStream : IXbimScene
    {
        private Stream _sceneStream;
        private string _sceneStreamFileName;
        private TransformGraph _graph;

        public XbimSceneStream(IModel model, string cacheFile)
        {
     
            _sceneStreamFileName = cacheFile;
            _sceneStream = new FileStream(cacheFile,FileMode.Open, FileAccess.Read);
            _graph = new TransformGraph(model, this);
            _graph.Read(new BinaryReader(_sceneStream));
        }

        public XbimTriangulatedModelStream Triangulate(TransformNode node)
        {
            _sceneStream.Seek(node.FilePosition, SeekOrigin.Begin);
            BinaryReader br = new BinaryReader(_sceneStream);
            int len = br.ReadInt32();
            if (len > 0)
            {
                byte [] data = br.ReadBytes(len);
                return new XbimTriangulatedModelStream(data);
            }
            else
                return XbimTriangulatedModelStream.Empty;
        }


        public TransformGraph Graph
        {
            get { return _graph; }
        }

        public void Close()
        {
            if (_sceneStream != null)
            {
                _sceneStream.Close();
                _sceneStream = null;
            }
        }

        public bool ReOpen()
        {
            try
            {
                _sceneStream = new FileStream(_sceneStreamFileName, FileMode.Open, FileAccess.Read);
                return true;
            }
            catch (Exception)
            {

                return false;
            }
        }
    }
}
