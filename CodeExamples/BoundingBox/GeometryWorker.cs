using System;
using System.Collections.Generic;
using System.IO;
using Xbim.Ifc.Kernel;
using Xbim.ModelGeometry;
using Xbim.ModelGeometry.Scene;
using Xbim.XbimExtensions;

namespace CodeExamples.BoundingBox
{
    /// <summary>
    /// Geometry Worker Class 
    /// </summary>
    public class GeometryWorker
    {
        //the model
        public IModel Model { get; set; }
        //file name the model was created from
        string ModelFileName { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="model"></param>
        /// <param name="fileName"></param>
        public GeometryWorker(IModel model, string fileName)
        {
            Model = model;
            ModelFileName = fileName;
        }

        /// <summary>
        /// Get the Transform Graph for this model
        /// </summary>
        /// <returns>TransformGraph object</returns>
        public TransformGraph GetTransformGraph()
        {
            TransformGraph graph = null;

            if (!string.IsNullOrEmpty(ModelFileName))
            {
                //get the file name to store the geometry 
                string cacheFile = Path.ChangeExtension(ModelFileName, ".xbimGC");
                //if no Geometry file than create it
                if (!File.Exists(cacheFile)) GenerateGeometry(cacheFile);
                //now we have a file read it into the XbimSceneStream
                if (File.Exists(cacheFile))
                {
                    XbimSceneStream scene = new XbimSceneStream(Model, cacheFile);
                    graph = scene.Graph; //the graph holds product boundary box's so we will return it
                    scene.Close();
                }
            }
            return graph;
        }

        /// <summary>
        /// Create the geometry file (xbimGC file)
        /// </summary>
        /// <param name="cacheFile">file path to write file too</param>
        private void GenerateGeometry(string cacheFile)
        {
            //get all products for this model to place into the return graph, can filter here if required
            IEnumerable<IfcProduct> toDraw = Model.IfcProducts.Items;
            //Create the XBimScene which helps builds the file
            XbimScene scene = new XbimScene(Model, toDraw);
            //create the geometry file
            using (FileStream sceneStream = new FileStream(cacheFile, FileMode.Create, FileAccess.ReadWrite))
            {
                BinaryWriter bw = new BinaryWriter(sceneStream);
                //show current status to user via ReportProgressDelegate
                Console.Clear();
                scene.Graph.Write(bw, delegate(int percentProgress, object userState)
                {
                    Console.Write("\rCreating GC File {0}", percentProgress);
                });
                bw.Flush();
            }

        }
    }
}
