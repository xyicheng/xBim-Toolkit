using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Xbim.ModelGeometry;
using Xbim.IO;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.ProductExtension;

namespace Xbim.SceneHelper
{
    public class Program
    {
        static void Main(string[] args)
        {
            CreateXbimFile(args[0]);
        }

        private static void CreateXbimFile(string fileName)
        {

            string xbimFileName = Path.ChangeExtension(fileName, ".xbim");
            XbimModel model = new XbimModel();
            model.CreateFrom(fileName, xbimFileName, null);
            model.Open(xbimFileName, XbimDBAccess.ReadWrite);
            XbimScene.ConvertGeometry(model.Instances.OfType<IfcProduct>().Where(t => !(t is IfcFeatureElement)),null, false);
            model.Close();
            
        }
    }
}
