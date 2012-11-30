using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Kernel;
using System.IO;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.ModelGeometry.Scene
{
    public class XbimSceneStream : IXbimScene
    {
        private Stream _sceneStream;
        private string _sceneStreamFileName;
        private TransformGraph _graph;
        private XbimLOD _lod;

        public XbimLOD LOD
        {
            get { return _lod; }
            set { _lod = value; }
        }      

        public XbimSceneStream(IModel model, string cacheFile)
        {
     
            _sceneStreamFileName = cacheFile;
            _sceneStream = new FileStream(cacheFile,FileMode.Open, FileAccess.Read);
            lock (_sceneStream)
            {
                _graph = new TransformGraph(model, this);
                _graph.Read(new BinaryReader(_sceneStream));
            }
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

       
    }
}
