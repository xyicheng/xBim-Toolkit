using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.Ifc2x3.Kernel;
using Xbim.ModelGeometry.Converter;
using Xbim.IO;
using Xbim.Ifc2x3.SharedBldgElements;
using System.IO;

namespace Xbim.Spatial.Tests
{
    [TestClass]
    public class GeometryTests
    {
        private Xbim3DModelContext _baselineContext;
        private Xbim3DModelContext _revisedContext;


        [TestMethod]
        public void GeometryHashTest()
        {
            //create models
            var baseline = new XbimModel();
            baseline.CreateFrom("OneRoom.ifc", "GeomHashTest", null, true, false);
            var revision = new XbimModel();
            revision.CreateFrom("OneRoom_added_window.ifc", "GeomHashTest_AddedWindow", null, true, false);

            //load geometry engine if it is not loaded yet
            string basePath = Path.GetDirectoryName(GetType().Assembly.Location);
            AssemblyResolver.GetModelGeometryAssembly(basePath);

            _baselineContext = new Xbim3DModelContext(baseline);
            _baselineContext.CreateContext();
            _revisedContext = new Xbim3DModelContext(revision);
            _revisedContext.CreateContext();

            //these two objects should have the same geometry
            var baselineProduct = baseline.Instances.Where<IfcWallStandardCase>(w => w.EntityLabel == 350 || w.EntityLabel == -350).FirstOrDefault();
            var revisedProduct = revision.Instances.Where<IfcWallStandardCase>(w => w.EntityLabel == 362 || w.EntityLabel == -362).FirstOrDefault();

            Assert.IsTrue(CompareHashes(baselineProduct, revisedProduct));
        }

        private bool CompareHashes(IfcProduct baseline, IfcProduct revision)
        {
            //Martin**** need to resolve geometry
            //var shape = _baselineContext.ProductShapes.Where(ps => ps.Product == baseline).FirstOrDefault();

            //XbimShapeGroup baseShapeGroup = shape.Shapes; //get the basic geometries that make up this one
            //IEnumerable<int> baseShapeHashes = baseShapeGroup.ShapeHashCodes();
            //int baseCount = baseShapeHashes.Count();

            //var rs = _revisedContext.ProductShapes.Where(ps => ps.Product == revision).FirstOrDefault();
            //if (rs != null)
            //{
            //    XbimShapeGroup shapeGroup = rs.Shapes;
            //    IEnumerable<int> revShapeHashes = rs.Shapes.ShapeHashCodes();
            //    if (baseCount == revShapeHashes.Count() && baseShapeHashes.Union(revShapeHashes).Count() == baseCount) //we have a match
            //    {
            //        return true;
            //    }
            //    else
            //        return false;
            //}
            return false;
        }
    }
}
