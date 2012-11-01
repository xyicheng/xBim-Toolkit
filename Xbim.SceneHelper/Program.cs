using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Xbim.ModelGeometry;

namespace Xbim.SceneHelper
{
    public class Program
    {
        static void Main(string[] args)
        {
            CreateXbimFiles(args[0]);
        }

        private static void CreateXbimFiles(string fileName)
        {
            string xbimFileName = Path.ChangeExtension(fileName, ".xbim");
            string xbimGeometryFileName = Path.ChangeExtension(fileName, ".xbimGC");
            //ClosePreviousModel();
            XbimScene scene = new XbimScene(fileName, xbimFileName, xbimGeometryFileName, false);
            scene.Close();
            scene.Dispose();
        }
    }
}
