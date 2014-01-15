using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.IO;
using Xbim.Ifc2x3.Kernel;
using Xbim.ModelGeometry.Scene;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.RepresentationResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.ProfileResource;
using Xbim.XbimExtensions.Interfaces;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.GeometricConstraintResource;
using Xbim.Ifc2x3.PresentationAppearanceResource;
using Xbim.Ifc2x3.PresentationResource;
using Xbim.Common.Geometry;
using Xbim.Analysis.Spatial;

namespace Xbim.Spatial.Tests
{
    /// <summary>
    /// Summary description for XbimOctreeTests
    /// </summary>
    [TestClass]
    public class XbimOctreeTests
    {
        public XbimOctreeTests()
        {
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void OctreeCreation()
        {
            var model = SampleModelsCreator.Disjoint4x4x4cubes;
            //var model = new XbimModel();
            //model.CreateFrom(@"c:\Users\cerny.m\Desktop\sample_project.ifc", null, null, true, false);
            var analyser = new XbimSpatialAnalyser(model);
        }
    }
}
