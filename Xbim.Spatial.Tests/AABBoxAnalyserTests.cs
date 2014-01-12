using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.IO;
using Xbim.Analysis.Spatial;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.ProductExtension;

namespace Xbim.Spatial.Tests
{
    [TestClass]
    public class AABBoxAnalyserTests
    {
        XbimModel DisjointModel;
        XbimModel TouchesModel;
        XbimModel IntersectsModel;
        XbimModel IdenticalModel;

        XbimAABBoxAnalyser DisjointAnalyser;
        XbimAABBoxAnalyser TouchesAnalyser;
        XbimAABBoxAnalyser IntersectsAnalyser;
        XbimAABBoxAnalyser IdenticalAnalyser;

        IEnumerable<XbimModel> Models
        {
            get 
            {
                yield return DisjointModel;
                yield return TouchesModel;
                yield return IntersectsModel;
                yield return IdenticalModel;
            }
        }

        IEnumerable<XbimAABBoxAnalyser> Analysers
        {
            get 
            {
                yield return DisjointAnalyser;
                yield return TouchesAnalyser;
                yield return IntersectsAnalyser;
                yield return IdenticalAnalyser;
            }
        }

        public AABBoxAnalyserTests()
        {
            DisjointModel = SampleModelsCreator.TwoDisjointColumns;
            DisjointModel.IfcProject.Name = "Disjoint";
            
            TouchesModel = SampleModelsCreator.TwoTouchingColumns;
            DisjointModel.IfcProject.Name = "Touches";
            
            IntersectsModel = SampleModelsCreator.TwoIntersectingColumns;
            DisjointModel.IfcProject.Name = "Intersects";

            IdenticalModel = SampleModelsCreator.TwoIdenticalColumns;
            DisjointModel.IfcProject.Name = "Identical";

            DisjointAnalyser = new XbimAABBoxAnalyser(DisjointModel); 
            TouchesAnalyser = new XbimAABBoxAnalyser(TouchesModel);
            IntersectsAnalyser = new XbimAABBoxAnalyser(IntersectsModel);
            IdenticalAnalyser = new XbimAABBoxAnalyser(IdenticalModel);
        }

        [TestMethod]
        public void BBTouchesTest()
        {
            foreach (var analyser in Analysers)
            {
                var model = analyser.Model;
                var prods = model.Instances.OfType<IfcColumn>().ToList();
                var first = prods[0];
                var second = prods[1];

                var result = analyser.Touches(first, second);
                if (model.IfcProject.Name == "Touches")
                    Assert.IsTrue(result);
                else
                    Assert.IsFalse(result);
            }

        }

        [TestMethod]
        public void BBDisjointTest()
        {
            foreach (var analyser in Analysers)
            {
                var model = analyser.Model;
                var prods = model.Instances.OfType<IfcColumn>().ToList();
                var first = prods[0];
                var second = prods[1];

                var result = analyser.Disjoint(first, second);
                if (model.IfcProject.Name == "Disjoint")
                    Assert.IsTrue(result);
                else
                    Assert.IsFalse(result);
            }

        }

        [TestMethod]
        public void BBIntersectsTest()
        {
            foreach (var analyser in Analysers)
            {
                var model = analyser.Model;
                var prods = model.Instances.OfType<IfcColumn>().ToList();
                var first = prods[0];
                var second = prods[1];

                var result = analyser.Intersects(first, second);
                if (model.IfcProject.Name == "Intersects")
                    Assert.IsTrue(result);
                else
                    Assert.IsFalse(result);
            }
        }

        [TestMethod]
        public void BBEqualsTest()
        {
            foreach (var analyser in Analysers)
            {
                var model = analyser.Model;
                var prods = model.Instances.OfType<IfcColumn>().ToList();
                var first = prods[0];
                var second = prods[1];

                var result = analyser.Equals(first, second);
                if (model.IfcProject.Name == "Identical")
                    Assert.IsTrue(result);
                else
                    Assert.IsFalse(result);
            }
        }

        [TestMethod]
        public void BBContainsTest()
        {
            var model = SampleModelsCreator.Disjoint4x4x4cubes;
            var analyser = new XbimAABBoxAnalyser(model);
            
            var space = model.Instances.OfType<IfcSpace>().First();
            var boundingCol = model.Instances.Where<IfcColumn>(c => c.Name == "000").First();
            var insideCol = model.Instances.Where<IfcColumn>(c => c.Name == "222").First();

            Assert.IsTrue(analyser.Contains(space, insideCol));
            Assert.IsTrue(analyser.Contains(space, boundingCol));

            foreach (var a in Analysers)
            {
                model = a.Model;
                var prods = model.Instances.OfType<IfcColumn>().ToList();
                var first = prods[0];
                var second = prods[1];

                var result = a.Contains(first, second);
                Assert.IsFalse(result);
            }
        }

        //[TestMethod]
        //public void SaveAllModels()
        //{
        //    foreach (var model in Models)
        //    {
        //        model.SaveAs(model.IfcProject.Name, Xbim.XbimExtensions.Interfaces.XbimStorageType.IFC);
        //    }
        //}
    }
}
