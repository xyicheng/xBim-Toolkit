using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Analysis.Comparing;

namespace Xbim.Tests.Comparisons
{
    [TestClass]
    public class NameComparisonTests
    {
        [TestMethod]
        public void NameComparisonTest()
        {
            //create two models
            var baseline = SampleModelsCreator.TwoDisjointColumns;
            var revision = SampleModelsCreator.TwoDisjointColumns;

            //this object should be identical
            var baseCol1 = baseline.Instances.Where<IfcColumn>(c => c.Name == "000").FirstOrDefault();
            var baseCol2 = baseline.Instances.Where<IfcColumn>(c => c.Name == "010").FirstOrDefault();

            if (baseCol1 == null || baseCol2 == null)
                throw new Exception("Wrong test data.");
            var comparer = new NameComparer();
            
            var test1 = comparer.Compare<IfcColumn>(baseCol1, revision);
            Assert.IsTrue(test1.Candidates.Count == 1);

            baseCol1.Name = "changed name";
            var test2 = comparer.Compare<IfcColumn>(baseCol1, revision);
            Assert.IsTrue(test2.Candidates.Count == 0);

            baseCol1.Name = null;
            var test3 = comparer.Compare<IfcColumn>(baseCol1, revision);
            Assert.IsTrue(test3 == null);
        }
    }
}
