using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.IO;
using Xbim.ModelGeometry.Converter;
using System.IO;

namespace Xbim.Tests.Geometry
{
    [TestClass]
    [DeploymentItem(@"Geometry\TestFiles\")]
    public class GeometryStreamingTests
    {
        [TestMethod]
        public void StreamProductShapeMetaData()
        {
            //Martin**** need to resolve
            //var model = new XbimModel();
            //model.CreateFrom("OneRoom.ifc", "StreamProductShapeMetaData", null, true, false);
            //Xbim3DModelContext ctxt = new Xbim3DModelContext(model);
            //ctxt.CreateContext();
            //StringWriter sw = new StringWriter();
            //foreach (var productShape in ctxt.ProductShapes)
            //{
            //    productShape.WriteMetaData(sw);
            //}
            //string result = sw.ToString();
        }
    }
}
