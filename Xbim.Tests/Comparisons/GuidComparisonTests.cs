using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.Analysis.Comparing;
using Xbim.Ifc2x3.Kernel;
using Xbim.IO;

namespace Xbim.Tests.Comparisons
{
    [TestClass]
    [DeploymentItem(@"Comparisons\TestFiles\OneRoom.ifc")]
    public class GuidComparisonTests
    {
        [TestMethod]
        public void GuidComparisonDifferentTest()
        {
            //create two in-memory models where guids are random so 
            //models are supposed to be completely different based on the GUID
            var baseline = SampleModelsCreator.TwoDisjointColumns;
            var revision = SampleModelsCreator.TwoDisjointColumns;

            var manager = new ComparisonManager(baseline, revision);
            var comparer = new GuidComparer();
            manager.AddComparer(comparer);
            manager.Compare<IfcProduct>();
            var result = manager.Results;

            var prodNum = baseline.Instances.OfType<IfcProduct>().Count();
            Assert.AreEqual(prodNum, result.Added.Count());
            Assert.AreEqual(prodNum, result.Deleted.Count());
            Assert.AreEqual(0, result.Ambiquity.Count());
            Assert.AreEqual(0, result.MatchOneToOne.Count());
        }

        [TestMethod]
        public void GuidComparisonTheSameTest()
        {
            //open one models twice so its content is identical
            var baseline = new XbimModel();
            baseline.CreateFrom("OneRoom.ifc", "GuidComparisonTheSameTestBaseline", null, true, false);
            var revision = new XbimModel();
            revision.CreateFrom("OneRoom.ifc", "GuidComparisonTheSameTestRevision", null, true, false);

            var manager = new ComparisonManager(baseline, revision);
            var comparer = new GuidComparer();
            manager.AddComparer(comparer);
            manager.Compare<IfcProduct>();
            var result = manager.Results;

            var prodNum = baseline.Instances.OfType<IfcProduct>().Count();
            Assert.AreEqual(0, result.Added.Count());
            Assert.AreEqual(0, result.Deleted.Count());
            Assert.AreEqual(0, result.Ambiquity.Count());
            Assert.AreEqual(prodNum, result.MatchOneToOne.Count());
        }


    }
}
