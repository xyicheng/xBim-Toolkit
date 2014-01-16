using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.IO;
using Xbim.Analysis.Spatial;
using Xbim.Analysis.Comparing;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.MeasureResource;

namespace Xbim.Spatial.Tests
{
    [TestClass]
    [DeploymentItem(@"TestFiles\")]
    public class SpatialComparerTests
    {
        [TestMethod]
        public void ComparerSimpleTest()
        {
            var original = new XbimModel();
            original.CreateFrom("OneRoom.ifc", null, null, true, false);

            var chAdded = new XbimModel();
            chAdded.CreateFrom("OneRoom_added_window.ifc", null, null, true, false);
            var chDeleted = new XbimModel();
            chDeleted.CreateFrom("OneRoom_deleted_window.ifc", null, null, true, false);
            var chMoved = new XbimModel();
            chMoved.CreateFrom("OneRoom_moved_window.ifc", null, null, true, false);
            var chReplaced = new XbimModel();
            chReplaced.CreateFrom("OneRoom_replaced_with_the_same_window.ifc", null, null, true, false);

            var cmpAdded = new XbimSpatialModelComparer(original, chAdded);
            //One more window and one more opening
            Assert.AreEqual(2, cmpAdded.Comparison.OnlyInNew.Count());
            //all old products should match one to one
            Assert.AreEqual(cmpAdded.CountProductsFromA, cmpAdded.Comparison.MatchOneToOne.Count());
            //there should be nothing only in old
            Assert.AreEqual(0, cmpAdded.Comparison.OnlyInOld.Count());
            //there should be no conflicts
            Assert.AreEqual(0, cmpAdded.Comparison.HasMoreNewVersions.Count());

            var cmpDeleted = new XbimSpatialModelComparer(original, chDeleted);
            //there should be nothing new
            Assert.AreEqual(0, cmpDeleted.Comparison.OnlyInNew.Count());
            //there is opening and window missing
            Assert.AreEqual(2, cmpDeleted.Comparison.OnlyInOld.Count());
            //there is opening and window missing
            Assert.AreEqual(cmpDeleted.CountProductsFromA - 2, cmpDeleted.Comparison.MatchOneToOne.Count());
            //there should be no conflicts
            Assert.AreEqual(0, cmpDeleted.Comparison.HasMoreNewVersions.Count());


            var cmpMoved = new XbimSpatialModelComparer(original, chMoved);
            //window and opening is moved away so no match will be found
            Assert.AreEqual(2, cmpMoved.Comparison.OnlyInNew.Count());
            Assert.AreEqual(2, cmpMoved.Comparison.OnlyInOld.Count());
            Assert.AreEqual(cmpMoved.CountProductsFromA - 2, cmpMoved.Comparison.MatchOneToOne.Count());
            //there should be no conflicts
            Assert.AreEqual(0, cmpMoved.Comparison.HasMoreNewVersions.Count());

            var cmpReplaced = new XbimSpatialModelComparer(original, chReplaced);
            //window has been replaced with the new window with the same position and geometry
            Assert.AreEqual(0, cmpReplaced.Comparison.OnlyInNew.Count());
            Assert.AreEqual(0, cmpReplaced.Comparison.OnlyInOld.Count());
            Assert.AreEqual(cmpReplaced.CountProductsFromA, cmpReplaced.Comparison.MatchOneToOne.Count());
            //there should be no conflicts
            Assert.AreEqual(0, cmpReplaced.Comparison.HasMoreNewVersions.Count());
        }

        [TestMethod]
        public void ComparerDisjointColumnsTest()
        {
            var model8cubes = SampleModelsCreator.ColumnsArrayModel("2x2x2", 500, 500, 500, 2000, 2000, 2000, 2, 2, 2);
            var model4cubes = SampleModelsCreator.ColumnsArrayModel("2x2x1", 500, 500, 500, 2000, 2000, 0, 2, 2, 1);

            var cmp = new XbimSpatialModelComparer(model4cubes, model8cubes);

            //four new columnt in the second layer and space which is bigger so it doesn't match the old one
            Assert.AreEqual(5, cmp.Comparison.OnlyInNew.Count());
            //four original columns are on the same space with the same dimensions
            Assert.AreEqual(4, cmp.Comparison.MatchOneToOne.Count());
            //one space which is changed so there is no match
            Assert.AreEqual(1, cmp.Comparison.OnlyInOld.Count());
            //there sould be no conflict
            Assert.AreEqual(0, cmp.Comparison.HasMoreNewVersions.Count());
        }

        [TestMethod]
        public void ComparerManagerTest()
        {
            //create models
            var original = new XbimModel();
            original.CreateFrom("OneRoom.ifc", "CmpMngTest", null, true, false);
            var chAdded = new XbimModel();
            chAdded.CreateFrom("OneRoom_added_window.ifc", "CmpMngTest_AddedWindow", null, true, false);

            //create comparers
            var cmpGeometry = new GeometryComparerII(original, chAdded);
            var cmpName = new NameComparer();
            var cmpGuid = new GuidComparer();
            var cmpProperties = new PropertyComparer();

            var manager = new ComparisonManager(original, chAdded);
            manager.AddComparer(cmpGeometry);
            manager.AddComparer(cmpName);
            manager.AddComparer(cmpGuid);
            manager.AddComparer(cmpProperties);

            manager.Compare<IfcProduct>();

            var results = manager.Results;
            manager.SaveResultToCSV(@"..\..\output.csv");
        }

        [TestMethod]
        public void LabelTest()
        {
            var a = new IfcLabel("<NONE>");
            var b = new IfcLabel("<NONE>");

            Assert.IsTrue(a == b);
        }

        //[TestMethod]
        //public void CompareSampleRacProject()
        //{
        //    var oldVersion = @"c:\Users\Martin\Desktop\sample_project.ifc";
        //    var newVersion = @"c:\Users\Martin\Desktop\sample_project_doors_moved.ifc";

        //    var oldModel = new XbimModel();
        //    oldModel.CreateFrom(oldVersion, null, null, true, false);

        //    var newModel = new XbimModel();
        //    newModel.CreateFrom(newVersion, null, null, true, false);

        //    var cmp = new XbimSpatialModelComparer(oldModel, newModel);

        //    var file = System.IO.File.CreateText("OnlyInNew.txt");
        //    foreach (var item in cmp.Comparison.OnlyInNew)
        //    {
        //        file.WriteLine("{0} {1}",item.GetType().Name, item.Name);
        //    }
        //    file.Close();

        //    //door and opening is moved away so no match will be found
        //    Assert.AreEqual(2, cmp.Comparison.OnlyInNew.Count());
        //    Assert.AreEqual(2, cmp.Comparison.OnlyInOld.Count());
        //    Assert.AreEqual(cmp.CountProductsFromA - 2, cmp.Comparison.MatchOneToOne.Count());
        //    //there should be no conflicts
        //    Assert.AreEqual(0, cmp.Comparison.HasMoreNewVersions.Count());
        //}
    }
}
