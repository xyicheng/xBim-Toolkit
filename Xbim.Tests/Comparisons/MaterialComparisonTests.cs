using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Analysis.Comparing;

namespace Xbim.Tests.Comparisons
{
    [TestClass]
    public class MaterialComparisonTests
    {
        [TestMethod]
        public void MaterialSimpleComparisonTest()
        {
            //create two models
            var baseline = SampleModelsCreator.TwoDisjointColumns;
            var revision = SampleModelsCreator.TwoDisjointColumns;

            //this object should be identical
            var baseCol1 = baseline.Instances.Where<IfcColumn>(c => c.Name == "000").FirstOrDefault();
            var baseCol2 = baseline.Instances.Where<IfcColumn>(c => c.Name == "010").FirstOrDefault();
            var revCol1 = revision.Instances.Where<IfcColumn>(c => c.Name == "000").FirstOrDefault();
            var revCol2 = revision.Instances.Where<IfcColumn>(c => c.Name == "010").FirstOrDefault();

            if (baseCol1 == null || baseCol2 == null || revCol1 == null || revCol2 == null)
                throw new Exception("Wrong test data.");

            //create materials
            using (var txn = baseline.BeginTransaction())
            {
                var concrete = baseline.Instances.New<IfcMaterial>(m => m.Name = "Concrete");
                var wood = baseline.Instances.New<IfcMaterial>(m => m.Name = "Wood");
                baseCol1.SetMaterial(concrete);
                baseCol2.SetMaterial(wood);
                txn.Commit();
            }
            using (var txn = revision.BeginTransaction())
            {
                var concrete = revision.Instances.New<IfcMaterial>(m => m.Name = "Concrete");
                var wood = revision.Instances.New<IfcMaterial>(m => m.Name = "Steel");
                revCol1.SetMaterial(concrete);
                revCol2.SetMaterial(wood);
                txn.Commit();
            }

            //create comparer
            var comparer = new MaterialComparer(revision);
            
            //test if the match is found
            var test1 = comparer.Compare<IfcColumn>(baseCol1, revision);
            Assert.IsTrue(test1.Candidates.Count == 1);

            var test2 = comparer.Compare<IfcColumn>(baseCol2, revision);
            Assert.IsTrue(test2.Candidates.Count == 0);

            var test3 = comparer.GetResidualsFromRevision<IfcColumn>(revision);
            Assert.IsTrue(test3.Candidates.Count == 1);
            Assert.IsNull(test3.Baseline);
        }

        [TestMethod]
        public void MaterialUsageComparisonTest()
        {
            //create two models
            var baseline = SampleModelsCreator.TwoDisjointColumns;
            var revision = SampleModelsCreator.TwoDisjointColumns;

            //this object should be identical
            var baseCol1 = baseline.Instances.Where<IfcColumn>(c => c.Name == "000").FirstOrDefault();
            var baseCol2 = baseline.Instances.Where<IfcColumn>(c => c.Name == "010").FirstOrDefault();
            var revCol1 = revision.Instances.Where<IfcColumn>(c => c.Name == "000").FirstOrDefault();
            var revCol2 = revision.Instances.Where<IfcColumn>(c => c.Name == "010").FirstOrDefault();

            if (baseCol1 == null || baseCol2 == null || revCol1 == null || revCol2 == null)
                throw new Exception("Wrong test data.");

            //create materials
            using (var txn = baseline.BeginTransaction())
            {
                var concrete = baseline.Instances.New<IfcMaterial>(m => m.Name = "Concrete");
                var wood = baseline.Instances.New<IfcMaterial>(m => m.Name = "Wood");
                var steel = baseline.Instances.New<IfcMaterial>(m => m.Name = "Steel");
                var insulation = baseline.Instances.New<IfcMaterial>(m => m.Name = "Insulation");

                var lSetA = baseline.Instances.New<IfcMaterialLayerSet>(mls => {
                    mls.MaterialLayers.Add_Reversible(baseline.Instances.New<IfcMaterialLayer>(ml => { 
                        ml.Material = concrete;
                        ml.LayerThickness = 200.0;
                    }));
                    mls.MaterialLayers.Add_Reversible(baseline.Instances.New<IfcMaterialLayer>(ml =>
                    {
                        ml.Material = insulation;
                        ml.LayerThickness = 100.0;
                    }));
                    mls.MaterialLayers.Add_Reversible(baseline.Instances.New<IfcMaterialLayer>(ml =>
                    {
                        ml.Material = wood;
                        ml.LayerThickness = 50.0;
                    }));

                });

                //both have the same thiskness and structure
                baseCol1.SetMaterialLayerSetUsage(lSetA, IfcLayerSetDirectionEnum.AXIS1, IfcDirectionSenseEnum.POSITIVE, 0);
                baseCol2.SetMaterialLayerSetUsage(lSetA, IfcLayerSetDirectionEnum.AXIS1, IfcDirectionSenseEnum.POSITIVE, 0);
                txn.Commit();
            }
            using (var txn = revision.BeginTransaction())
            {
                var concrete = revision.Instances.New<IfcMaterial>(m => m.Name = "Concrete");
                var wood = revision.Instances.New<IfcMaterial>(m => m.Name = "Wood");
                var steel = revision.Instances.New<IfcMaterial>(m => m.Name = "Steel");
                var insulation = revision.Instances.New<IfcMaterial>(m => m.Name = "Insulation");

                var lSetA = revision.Instances.New<IfcMaterialLayerSet>(mls =>
                {
                    mls.MaterialLayers.Add_Reversible(revision.Instances.New<IfcMaterialLayer>(ml =>
                    {
                        ml.Material = concrete;
                        ml.LayerThickness = 200.0;
                    }));
                    mls.MaterialLayers.Add_Reversible(revision.Instances.New<IfcMaterialLayer>(ml =>
                    {
                        ml.Material = insulation;
                        ml.LayerThickness = 100.0;
                    }));
                    mls.MaterialLayers.Add_Reversible(revision.Instances.New<IfcMaterialLayer>(ml =>
                    {
                        ml.Material = wood;
                        ml.LayerThickness = 50.0;
                    }));

                });

                //different thickness
                var lSetB = revision.Instances.New<IfcMaterialLayerSet>(mls =>
                {
                    mls.MaterialLayers.Add_Reversible(revision.Instances.New<IfcMaterialLayer>(ml =>
                    {
                        ml.Material = concrete;
                        ml.LayerThickness = 200.0;
                    }));
                    mls.MaterialLayers.Add_Reversible(revision.Instances.New<IfcMaterialLayer>(ml =>
                    {
                        ml.Material = insulation;
                        ml.LayerThickness = 300.0;
                    }));
                    mls.MaterialLayers.Add_Reversible(revision.Instances.New<IfcMaterialLayer>(ml =>
                    {
                        ml.Material = wood;
                        ml.LayerThickness = 50.0;
                    }));

                });

                revCol1.SetMaterialLayerSetUsage(lSetA, IfcLayerSetDirectionEnum.AXIS1, IfcDirectionSenseEnum.POSITIVE, 0);
                revCol2.SetMaterialLayerSetUsage(lSetB, IfcLayerSetDirectionEnum.AXIS1, IfcDirectionSenseEnum.POSITIVE, 0);
                txn.Commit();
            }

            //create comparer
            var comparer = new MaterialComparer(revision);

            //test if the match is found
            var test1 = comparer.Compare<IfcColumn>(baseCol1, revision);
            Assert.IsTrue(test1.Candidates.Count == 1);

            //this sould find the same column as for the first
            var test2 = comparer.Compare<IfcColumn>(baseCol2, revision);
            Assert.IsTrue(test2.Candidates.Count == 1);

            var test3 = comparer.GetResidualsFromRevision<IfcColumn>(revision);
            Assert.IsTrue(test3.Candidates.Count == 1);
            Assert.IsNull(test3.Baseline);
        }

        [TestMethod]
        public void MaterialLayerSetComparisonTest()
        {
            //create two models
            var baseline = SampleModelsCreator.TwoDisjointColumns;
            var revision = SampleModelsCreator.TwoDisjointColumns;

            //this object should be identical
            var baseCol1 = baseline.Instances.Where<IfcColumn>(c => c.Name == "000").FirstOrDefault();
            var baseCol2 = baseline.Instances.Where<IfcColumn>(c => c.Name == "010").FirstOrDefault();
            var revCol1 = revision.Instances.Where<IfcColumn>(c => c.Name == "000").FirstOrDefault();
            var revCol2 = revision.Instances.Where<IfcColumn>(c => c.Name == "010").FirstOrDefault();

            if (baseCol1 == null || baseCol2 == null || revCol1 == null || revCol2 == null)
                throw new Exception("Wrong test data.");

            //create materials
            using (var txn = baseline.BeginTransaction())
            {
                var concrete = baseline.Instances.New<IfcMaterial>(m => m.Name = "Concrete");
                var wood = baseline.Instances.New<IfcMaterial>(m => m.Name = "Wood");
                var steel = baseline.Instances.New<IfcMaterial>(m => m.Name = "Steel");
                var insulation = baseline.Instances.New<IfcMaterial>(m => m.Name = "Insulation");

                var lSetA = baseline.Instances.New<IfcMaterialLayerSet>(mls =>
                {
                    mls.MaterialLayers.Add_Reversible(baseline.Instances.New<IfcMaterialLayer>(ml =>
                    {
                        ml.Material = concrete;
                        ml.LayerThickness = 200.0;
                    }));
                    mls.MaterialLayers.Add_Reversible(baseline.Instances.New<IfcMaterialLayer>(ml =>
                    {
                        ml.Material = insulation;
                        ml.LayerThickness = 100.0;
                    }));
                    mls.MaterialLayers.Add_Reversible(baseline.Instances.New<IfcMaterialLayer>(ml =>
                    {
                        ml.Material = wood;
                        ml.LayerThickness = 50.0;
                    }));

                });

                //both have the same thiskness and structure
                baseCol1.SetMaterial(lSetA);
                baseCol2.SetMaterial(lSetA);
                txn.Commit();
            }
            using (var txn = revision.BeginTransaction())
            {
                var concrete = revision.Instances.New<IfcMaterial>(m => m.Name = "Concrete");
                var wood = revision.Instances.New<IfcMaterial>(m => m.Name = "Wood");
                var steel = revision.Instances.New<IfcMaterial>(m => m.Name = "Steel");
                var insulation = revision.Instances.New<IfcMaterial>(m => m.Name = "Insulation");

                var lSetA = revision.Instances.New<IfcMaterialLayerSet>(mls =>
                {
                    mls.MaterialLayers.Add_Reversible(revision.Instances.New<IfcMaterialLayer>(ml =>
                    {
                        ml.Material = concrete;
                        ml.LayerThickness = 200.0;
                    }));
                    mls.MaterialLayers.Add_Reversible(revision.Instances.New<IfcMaterialLayer>(ml =>
                    {
                        ml.Material = insulation;
                        ml.LayerThickness = 100.0;
                    }));
                    mls.MaterialLayers.Add_Reversible(revision.Instances.New<IfcMaterialLayer>(ml =>
                    {
                        ml.Material = wood;
                        ml.LayerThickness = 50.0;
                    }));

                });

                //different thickness
                var lSetB = revision.Instances.New<IfcMaterialLayerSet>(mls =>
                {
                    mls.MaterialLayers.Add_Reversible(revision.Instances.New<IfcMaterialLayer>(ml =>
                    {
                        ml.Material = concrete;
                        ml.LayerThickness = 200.0;
                    }));
                    mls.MaterialLayers.Add_Reversible(revision.Instances.New<IfcMaterialLayer>(ml =>
                    {
                        ml.Material = insulation;
                        ml.LayerThickness = 300.0;
                    }));
                    mls.MaterialLayers.Add_Reversible(revision.Instances.New<IfcMaterialLayer>(ml =>
                    {
                        ml.Material = wood;
                        ml.LayerThickness = 50.0;
                    }));

                });

                revCol1.SetMaterial(lSetA);
                revCol2.SetMaterial(lSetB);
                txn.Commit();
            }

            //create comparer
            var comparer = new MaterialComparer(revision);

            //test if the match is found
            var test1 = comparer.Compare<IfcColumn>(baseCol1, revision);
            Assert.IsTrue(test1.Candidates.Count == 1);

            //this sould find the same column as for the first
            var test2 = comparer.Compare<IfcColumn>(baseCol2, revision);
            Assert.IsTrue(test2.Candidates.Count == 1);

            var test3 = comparer.GetResidualsFromRevision<IfcColumn>(revision);
            Assert.IsTrue(test3.Candidates.Count == 1);
            Assert.IsNull(test3.Baseline);
        }
    }
}
