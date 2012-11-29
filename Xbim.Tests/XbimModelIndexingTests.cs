using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.Ifc2x3.ActorResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.IO;
using Xbim.XbimExtensions;

namespace Xbim.Tests
{
    [DeploymentItem(SourceFile, Root)]
    [TestClass]
    public class XbimModelIndexingTests
    {

        private const string Root = "TestSourceFiles";
        private const string SourceModelLeaf = "2012-03-23-Duplex-Handover.xbim";
        private const string SourceFile = Root + @"\" + SourceModelLeaf;

        [ClassInitialize]
        public static void TestInit(TestContext context)
        {
            System.Diagnostics.Debug.WriteLine("Test Init");
        }
        [ClassCleanup]
        public static void TestCleanUp()
        {
            System.Diagnostics.Debug.WriteLine("Test Cleanup");
        }
        [TestMethod]
        public void GlobalCountTest()
        {
            XbimModel model = new XbimModel();
            model.Open(SourceFile);
           //get the cached count
            long c = model.Instances.Count;
            //read every record
            foreach (var entity in model.Instances)
            {
                c--;
            }
            model.Close();
            Assert.AreEqual(c , 0);
        }

        /// <summary>
        /// Test the count feature in combination with roll back transaction
        /// Tests the general count functions and their interaction with transactions
        /// </summary>
        [TestMethod]
        public void AddRecordsCountRollbackTest()
        {
            using (XbimModel model = new XbimModel())
            {
                Assert.IsTrue(model.Open(SourceFile, XbimDBAccess.ReadWrite));
                int inserts = 100;
                long initTotalCount = model.Instances.Count;
                long initPointCount = model.Instances.CountOf<IfcCartesianPoint>();
                using (var txn = model.BeginTransaction())
                {
                    for (int i = 0; i < inserts; i++)
                    {
                        model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(1, 2, 3));
                    }
                    long newTotalCount = model.Instances.Count;
                    long newPointCount = model.Instances.CountOf<IfcCartesianPoint>();
                    Assert.AreEqual(inserts + initTotalCount, newTotalCount);
                    Assert.AreEqual(inserts + initPointCount, newPointCount);

                }
                long finalCount = model.Instances.Count;
                long finalPointCount = model.Instances.CountOf<IfcCartesianPoint>();
                model.Close();
                Assert.AreEqual(finalCount, initTotalCount); //check everything undid
                Assert.AreEqual(initPointCount, finalPointCount);
            }
        }

        /// <summary>
        /// Creates a new database, adds records, commits them and confirms counts
        /// Tests the generalcount functions and their interaction with transactions
        /// </summary>
        [TestMethod]
        public void AddRecordsCountCommitTest()
        {
            string tmpFile = "AddRecordsCountCommitTest.xBIM";
            if(File.Exists(tmpFile)) File.Delete(tmpFile);
            Assert.IsFalse(File.Exists(tmpFile));
            XbimModel model = XbimModel.CreateModel(tmpFile);
            model.Open(tmpFile, XbimDBAccess.ReadWrite);
            int inserts = 100;
            long reqCount = (2 * inserts) + 7; //100 walls, 100 points and 7 ownerhistory etc object 
            long initTotalCount = model.Instances.Count;
            long initPointCount = model.Instances.CountOf<IfcCartesianPoint>();
            long initWallCount = model.Instances.CountOf<IfcWall>();
            using (var txn = model.BeginTransaction())
            {
                for (int i = 0; i < inserts; i++)
                {
                    model.Instances.New<IfcCartesianPoint>(p => p.SetXYZ(1, 2, 3));
                    model.Instances.New<IfcWall>();
                }
                long newTotalCount = model.Instances.Count;
                long newPointCount = model.Instances.CountOf<IfcCartesianPoint>();
                long newWallCount = model.Instances.CountOf<IfcWall>();
                Assert.AreEqual(reqCount, newTotalCount);
                Assert.AreEqual(inserts + initPointCount, newPointCount);
                Assert.AreEqual(inserts + initWallCount, newWallCount);
                txn.Commit();
            }
            long finalCount = model.Instances.Count;
            long finalPointCount = model.Instances.CountOf<IfcCartesianPoint>();
            long finalWallCount = model.Instances.CountOf<IfcWall>();
            model.Close();
            Assert.AreEqual(finalCount, reqCount); //check everything undid
            Assert.AreEqual(initPointCount + inserts, finalPointCount);
            Assert.AreEqual(initWallCount + inserts, finalWallCount);
        }

        /// <summary>
        /// Opens a model twice in readonly mode
        /// </summary>
        [TestMethod]
        public void OpenModelTwice()
        {
            XbimModel model1 = new XbimModel();
            model1.Open(SourceFile, XbimDBAccess.Read);
            XbimModel model2 = new XbimModel();
            model2.Open(SourceFile, XbimDBAccess.Read);
            long c1 = model1.Instances.Count;
            long c2 = model2.Instances.Count;
            model1.Close();
            model2.Close();
            Assert.AreEqual(c1,c2);
        }

        [TestMethod]
        public void CheckModelDispose()
        {
            long c1;
            using (XbimModel model1 = new XbimModel())
            {
                 model1.Open(SourceFile, XbimDBAccess.ReadWrite);
                 c1 = model1.Instances.Count;
            } //dispose should close the model, allowing us to open it for reading and writing
            XbimModel model2 = new XbimModel();
            model2.Open(SourceFile, XbimDBAccess.ReadWrite);
            long c2 = model2.Instances.Count;
            model2.Close();
            Assert.AreEqual(c1, c2);
        }

        [TestMethod]
        public void CheckModelReadWrite()
        {
            using (XbimModel model1 = new XbimModel())
            {
                Assert.IsTrue(model1.Open(SourceFile, XbimDBAccess.ReadWrite));
                using (var txn1 = model1.BeginTransaction())
                {
                     model1.Instances.New<IfcWall>(); //DO SOMETHING
                }
               
                using (XbimModel model2 = new XbimModel())
                {
                    Assert.IsTrue(model2.Open(SourceFile, XbimDBAccess.ReadWrite));
                    using (var txn2 = model2.BeginTransaction())
                    {
                        model2.Instances.New<IfcWall>(); //DO SOMETHING
                    }
                }
            } //dispose should close the models, allowing us to open it for reading and writing

            try
            {
                FileStream f = File.OpenWrite(SourceFile); //check if file is unlocked
                f.Close();
            }
            catch (Exception)
            {
                Assert.Fail();
            }
            using (XbimModel model = new XbimModel())
            {
                model.Open(SourceFile, XbimDBAccess.ReadWrite);
                try
                {
                    FileStream f = File.OpenWrite(SourceFile); //check if file is unlocked
                    f.Close();
                    Assert.Fail(); //it should not be able to be opened
                }
                catch (System.IO.IOException) //this should fail
                {

                }
                finally
                {
                    model.Close();
                }
            }
        }

        [TestMethod]
        public void CheckReferenceModels()
        {
            using (XbimModel model = XbimModel.CreateModel("ReferenceTestModel.xBIM"))
            {
                using (var txn = model.BeginTransaction())
                {
                    //create an author of the referenced model
                    IfcOrganization org = model.Instances.New<IfcOrganization>();
                    IfcActorRole role = model.Instances.New<IfcActorRole>();
                    role.Role=IfcRole.Architect;
                    org.Name="Grand Designers";
                    org.AddRole(role);

                    //reference in the other model
                    model.AddModelReference(SourceFile, org);
                    txn.Commit();
                }
                //ensure everything is closed
                model.Close();
                //open the referenced model to get count
                long originalCount;
                using ( XbimModel original = new XbimModel())
                {
                    original.Open(SourceFile);
                    originalCount = original.Instances.Count;
                    original.Close();
                }
                //open the referencing model to get count
                long referencingCount;
                using (XbimModel referencing = new XbimModel())
                {
                    referencing.Open("ReferenceTestModel.xBIM");
                    referencingCount = referencing.Instances.Count;
                    foreach (var refModel in referencing.RefencedModels)
                    {
                        referencingCount += refModel.Model.Instances.Count;
                    }
                    referencing.Close();
                }
                //there should be 3 more instances than in the original model
                Assert.IsTrue(3 == (referencingCount - originalCount));
            }
        }

    }
}
