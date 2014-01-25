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
    public class AttributeComparisonTests
    {
        [TestMethod]
        public void AttributeComparisonTest()
        {
            //create two models
            var baseline = SampleModelsCreator.TwoDisjointColumns;
            var revision = SampleModelsCreator.TwoDisjointColumns;

            //this object should be identical
            var baseCol1 = baseline.Instances.Where<IfcColumn>(c => c.Name == "000").FirstOrDefault();
            var baseCol2 = baseline.Instances.Where<IfcColumn>(c => c.Name == "010").FirstOrDefault();
            var revCol1 = revision.Instances.Where<IfcColumn>(c => c.Name == "000").FirstOrDefault();
            var revCol2 = revision.Instances.Where<IfcColumn>(c => c.Name == "010").FirstOrDefault();

            using (var txn = baseline.BeginTransaction())
            {
                baseCol1.ObjectType = "Type 1";
                txn.Commit();
            }
            using (var txn = revision.BeginTransaction())
            {
                revCol1.ObjectType = "Type 1";
                txn.Commit();
            }
            var t = revision.Instances.Where<IfcColumn>(c => c.ObjectType == "Type 1").FirstOrDefault();

            if (baseCol1 == null || baseCol2 == null || revCol1 == null || revCol2 == null)
                throw new Exception("Wrong test data.");
            var comparer = new AttributeComparer("ObjectType", revision);

            var test1 = comparer.Compare<IfcColumn>(baseCol1, revision);
            Assert.IsTrue(test1.Candidates.Count == 1);


            using (var txn = baseline.BeginTransaction())
            {
                baseCol1.ObjectType = "changed type";
                txn.Commit();
            }
            var test2 = comparer.Compare<IfcColumn>(baseCol1, revision);
            Assert.IsTrue(test2.Candidates.Count == 0);

            using (var txn = baseline.BeginTransaction())
            {
                baseCol1.ObjectType = null;
                txn.Commit();
            }
            var test3 = comparer.Compare<IfcColumn>(baseCol1, revision);
            Assert.IsTrue(test3 == null);
        }
    }
}
