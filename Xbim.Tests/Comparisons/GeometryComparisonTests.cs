using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.IO;
using Xbim.Analysis.Comparing;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.Kernel;

namespace Xbim.Tests.Comparisons
{
    [TestClass]
    [DeploymentItem(@"Comparisons\TestFiles\")]
    public class GeometryComparisonTests
    {
        [TestMethod]
        public void GeometryComparisonWindowAddedTest()
        {
            var baseline = new XbimModel();
            baseline.CreateFrom("OneRoom.ifc", "GeometryComparisonTestsBaseline1", null, true, false);
            var revision = new XbimModel();
            revision.CreateFrom("OneRoom_added_window.ifc", "GeometryComparisonWindowAddedTest", null, true, false);
            
            var manager = new ComparisonManager(baseline, revision);
            var comparer = new GeometryComparerII(baseline, revision);
            manager.AddComparer(comparer);
            manager.Compare<IfcProduct>();
            var results = manager.Results;

            //One more window and one more opening
            Assert.AreEqual(2, results.Added.Count());
            //all old products should match one to one
            Assert.AreEqual(baseline.Instances.Where<IfcProduct>(p => p.Representation != null).Count()-1, results.MatchOneToOne.Count);
            //there should be nothing only in old but wall has changed
            Assert.AreEqual(1, results.Deleted.Count());
            //there should be no conflicts
            Assert.AreEqual(0, results.Ambiquity.Count);
        }

        [TestMethod]
        public void GeometryComparisonWindowDeletedTest()
        {
            var baseline = new XbimModel();
            baseline.CreateFrom("OneRoom.ifc", "GeometryComparisonTestsBaseline2", null, true, false);
            var revision = new XbimModel();
            revision.CreateFrom("OneRoom_deleted_window.ifc", "GeometryComparisonWindowDeletedTest", null, true, false);

            var manager = new ComparisonManager(baseline, revision);
            var comparer = new GeometryComparerII(baseline, revision);
            manager.AddComparer(comparer);
            manager.Compare<IfcProduct>();
            var results = manager.Results;

            //there should be nothing new
            Assert.AreEqual(0, results.Added.Count());
            //there is opening and window missing
            Assert.AreEqual(3, results.Deleted.Count());
            //there should be no conflicts
            Assert.AreEqual(0, results.Ambiquity.Count());
        }

        [TestMethod]
        public void GeometryComparisonWindowMovedTest()
        {
            var baseline = new XbimModel();
            baseline.CreateFrom("OneRoom.ifc", "GeometryComparisonTestsBaseline3", null, true, false);
            var revision = new XbimModel();
            revision.CreateFrom("OneRoom_moved_window.ifc", "GeometryComparisonWindowMovedTest", null, true, false);

            var manager = new ComparisonManager(baseline, revision);
            var comparer = new GeometryComparerII(baseline, revision);
            manager.AddComparer(comparer);
            manager.Compare<IfcProduct>();
            var results = manager.Results;

            //window and opening is moved away so no match will be found
            Assert.AreEqual(2, results.Added.Count());
            Assert.AreEqual(2, results.Deleted.Count());
            //there should be no conflicts
            Assert.AreEqual(0, results.Ambiquity.Count());
        }

        [TestMethod]
        public void GeometryComparisonWindowReplacedTest()
        {
            var baseline = new XbimModel();
            baseline.CreateFrom("OneRoom.ifc", "GeometryComparisonTestsBaseline4", null, true, false);
            var revision = new XbimModel();
            revision.CreateFrom("OneRoom_replaced_with_the_same_window.ifc", "GeometryComparisonWindowReplacedTest", null, true, false);

            var manager = new ComparisonManager(baseline, revision);
            var comparer = new GeometryComparerII(baseline, revision);
            manager.AddComparer(comparer);
            manager.Compare<IfcProduct>();
            var results = manager.Results;


            //window has been replaced with the new window with the same position and geometry
            Assert.AreEqual(0, results.Added.Count());
            Assert.AreEqual(0, results.Deleted.Count());
            //there should be no conflicts
            Assert.AreEqual(0, results.Ambiquity.Count());
        }
    }
}
