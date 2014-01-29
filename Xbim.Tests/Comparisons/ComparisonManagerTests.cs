using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.IO;
using Xbim.Ifc2x3.Kernel;
using Xbim.Analysis.Comparing;

namespace Xbim.Tests.Comparisons
{
    [TestClass]
    public class ComparisonManagerTests
    {
        [TestMethod]
        public void ComparisonManagerTest()
        {
            var baseline = new XbimModel();
            baseline.CreateFrom("OneRoom.ifc", "ComparisonManagerTestBaseline", null, true, false);
            var revision = new XbimModel();
            revision.CreateFrom("OneRoom_added_window.ifc", "ComparisonManagerTestRevision", null, true, false);


            var comparerAttribute = new AttributeComparer("ObjectType", revision);
            var comparerGeom = new GeometryComparerII(baseline, revision);
            var comparerGuid = new GuidComparer();
            var comparerMaterial = new MaterialComparer(revision);
            var comparerName = new NameComparer();
            var comparerProperty = new PropertyComparer(baseline, revision);

            var manager = new ComparisonManager(baseline, revision);
            manager.AddComparer(comparerAttribute);
            manager.AddComparer(comparerGeom);
            manager.AddComparer(comparerGuid);
            manager.AddComparer(comparerMaterial);
            manager.AddComparer(comparerName);
            manager.AddComparer(comparerProperty);


            manager.Compare<IfcProduct>();
            manager.SaveResultToCSV("comparison_report");
            manager.SaveResultToXLS("comparison_report");
        }
        
[TestMethod]
        public void ComparisonManagerRealTest()
        {
            var baseline = new XbimModel();
            baseline.CreateFrom(@"d:\CODE\Sample_IFC\01 E21 Ellison Place Original.ifc", "ComparisonManagerTestBaseline", null, true, false);
            var revision = new XbimModel();
            revision.CreateFrom(@"d:\CODE\Sample_IFC\01 E21 Ellison Place Modified.ifc", "ComparisonManagerTestRevision", null, true, false);


            var comparerAttribute = new AttributeComparer("ObjectType", revision);
            var comparerGeom = new GeometryComparerII(baseline, revision);
            var comparerGuid = new GuidComparer();
            var comparerMaterial = new MaterialComparer(revision);
            var comparerName = new NameComparer();
            var comparerProperty = new PropertyComparer(baseline, revision);

            var manager = new ComparisonManager(baseline, revision);
            manager.AddComparer(comparerAttribute);
            manager.AddComparer(comparerGeom);
            manager.AddComparer(comparerGuid);
            manager.AddComparer(comparerMaterial);
            manager.AddComparer(comparerName);
            manager.AddComparer(comparerProperty);


            manager.Compare<IfcProduct>();
            manager.SaveResultToXLS("comparison_report_real");

        }

    }
}
