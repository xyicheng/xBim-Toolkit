using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.Ifc2x3.ActorResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.Ifc2x3.PresentationAppearanceResource;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.IO;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.Tests
{
    [DeploymentItem(SourceFile, Root)]
    [DeploymentItem(ModelA, Root)]
    [DeploymentItem(ModelB, Root)]
    [TestClass]
    public class XbimModelDatabaseTests
    {

        private const string Root = "TestSourceFiles";
        private const string SourceModelLeaf = "Clinic_Handover.xbim";
        private const string SourceFile = Root + @"\" + SourceModelLeaf;
        private const string ModelA = Root + @"\BIM Logo-Complete, fully schema compliant.xBIM";
        private const string ModelB = Root + @"\BIM Logo - Coordination View 2 - No M.xBIM";
        [ClassInitialize]
        public static void TestInit(TestContext context)
        {
           // System.Diagnostics.Debug.WriteLine("Test Init");
        }
        [ClassCleanup]
        public static void TestCleanUp()
        {
            //System.Diagnostics.Debug.WriteLine("Test Cleanup");
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
        public void CheckModelReadWrite()
        {
            using (XbimModel model1 = new XbimModel())
            {
                Assert.IsTrue(model1.Open(SourceFile, XbimDBAccess.ReadWrite));
                using (var txn1 = model1.BeginTransaction())
                {
                    model1.Instances.New<IfcWall>(); //DO SOMETHING
                }
            }

            using (XbimModel model2 = new XbimModel())
            {
                Assert.IsTrue(model2.Open(SourceFile, XbimDBAccess.ReadWrite));
                using (var txn2 = model2.BeginTransaction())
                {
                    model2.Instances.New<IfcWall>(); //DO SOMETHING
                }
            }
            //dispose should close the models, allowing us to open it for reading and writing

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
                catch (System.IO.IOException) //we should get this exception
                {
                  
                }
               
            }
            Assert.IsTrue(XbimModel.ModelOpenCount == 0);
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
                    referencingCount = referencing.InstancesLocal.Count;
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

        /// <summary>
        /// Checks that the geometry hash algorithm returns the same results as true equality
        /// </summary>
        [TestMethod]
        public void CheckGeometryHash()
        {
            //check how may geometry data objects have the same byte array
            HashSet<XbimGeometryData> gHash = new HashSet<XbimGeometryData>(new XbimShapeEqualityComparer());

            HashSet<int> hashHash = new HashSet<int>();
            
            using (XbimModel model1 = new XbimModel())
            {
                Assert.IsTrue(model1.Open(SourceFile, XbimDBAccess.Read));
                foreach (var mesh in model1.GetGeometryData(XbimGeometryType.TriangulatedMesh))
                {
                    int sCount = gHash.Count;
                    gHash.Add(mesh);
                    bool dupGeom = sCount == gHash.Count - 1;
                    sCount = hashHash.Count;
                    hashHash.Add(mesh.GeometryHash);
                    bool dupHash = sCount == hashHash.Count - 1;
                    Assert.AreEqual(dupGeom, dupHash);
                    
                }
                Assert.IsTrue(gHash.Count == hashHash.Count);
            }
        }

        /// <summary>
        /// Check if the unique surface style function is correct
        /// </summary>
        [TestMethod]
        public void CheckSurfaceStyles()
        {

            HashSet<int> surfaceSyleHash = new HashSet<int>();
            using (XbimModel model1 = new XbimModel())
            {

                Assert.IsTrue(model1.Open(SourceFile, XbimDBAccess.Read));
                foreach (var geom in model1.GetGeometryData(XbimGeometryType.TriangulatedMesh))
                    surfaceSyleHash.Add(geom.StyleLabel);
                XbimGeometryHandleCollection geomHandles = model1.GetGeometryHandles(XbimGeometryType.TriangulatedMesh, XbimGeometrySort.OrderByIfcSurfaceStyleThenIfcType);
                IEnumerable<XbimSurfaceStyle> uniqueStyles = geomHandles.GetSurfaceStyles();

                Assert.IsTrue(uniqueStyles.Count() == surfaceSyleHash.Count);
                foreach (var item in uniqueStyles)
                    surfaceSyleHash.Remove(item.IfcSurfaceStyleLabel);
                Assert.IsTrue(surfaceSyleHash.Count == 0);
                XbimSurfaceStyleMap map = geomHandles.ToSurfaceStyleMap();
                Assert.IsTrue(geomHandles.Count == map.GeometryHandles.Count());
                Assert.IsTrue(map.Styles.Count() == uniqueStyles.Count());
            }
        }

        /// <summary>
        /// Check if the unique surface style function is correct
        /// </summary>
        [TestMethod]
        public void CountSameObjects()
        {
            using (XbimModel model1 = new XbimModel())
            {
                Assert.IsTrue(model1.Open(ModelA, XbimDBAccess.Read));
                
                foreach (var entity in model1.Instances)
                {
                   
                    MemoryStream ms = new MemoryStream();
                    BinaryWriter bw = new BinaryWriter(ms);
                    entity.WriteEntity(bw);
                    int uniqueHash = XbimGeometryData.GenerateGeometryHash(ms.GetBuffer());
                    Debug.WriteLine(uniqueHash + ", #" + entity);
                }
                Debug.WriteLine("End of Model A");
                using (XbimModel model2 = new XbimModel())
                {
                    Assert.IsTrue(model2.Open(ModelB, XbimDBAccess.Read));

                    foreach (var entity in model2.Instances)
                    {
                        MemoryStream ms = new MemoryStream();
                        BinaryWriter bw = new BinaryWriter(ms);
                        entity.WriteEntity(bw);
                        int uniqueHash = XbimGeometryData.GenerateGeometryHash(ms.GetBuffer());
                        Debug.WriteLine(uniqueHash + ", #" + entity.EntityLabel);
                    }
                }
            }

        }
        /// <summary>
        /// Opens a model twice in readonly mode
        /// </summary>
        [TestMethod]
        public void OpenReadOnly()
        {
            Assert.IsTrue(XbimModel.ModelOpenCount == 0);
            using (XbimModel m = new XbimModel())
            {
                m.Open(ModelA, XbimDBAccess.Read);
                Assert.IsTrue(XbimModel.ModelOpenCount == 1);
                using (XbimModel m2 = new XbimModel())
                {
                    m2.Open(ModelA, XbimDBAccess.Read);
                    Assert.IsTrue(XbimModel.ModelOpenCount == 2);

                }
            }
            Assert.IsTrue(XbimModel.ModelOpenCount == 0);
        }

        /// <summary>
        /// Checks that the correct number of instances are stored in the database meta data
        /// </summary>
        [TestMethod]
        public void GlobalCountTest()
        {
            XbimModel model = new XbimModel();
            model.Open(ModelA);
            //get the cached count
            long c = model.Instances.Count;
            //read every record
            foreach (var entity in model.Instances)
            {
                c--;
            }
            model.Close();
            Assert.AreEqual(c, 0);
        }

        /// <summary>
        /// Checks whether the database is being disposed and closed correctly
        /// </summary>
        [TestMethod]
        public void CheckModelDispose()
        {
            long c1;
            using (XbimModel model1 = new XbimModel())
            {
                model1.Open(ModelA, XbimDBAccess.ReadWrite);
                c1 = model1.Instances.Count;
            } //dispose should close the model, allowing us to open it for reading and writing
            XbimModel model2 = new XbimModel();
            model2.Open(ModelA, XbimDBAccess.ReadWrite);
            long c2 = model2.Instances.Count;
            model2.Close();
            Assert.AreEqual(c1, c2);
        }

        [TestMethod]
        public void InsertCopyOfEntityIntoAnotherModel()
        {
            if (File.Exists("Test.ifc"))
                File.Delete("Test.ifc");
            using (XbimModel model1 = new XbimModel())
            {
                 model1.Open(SourceFile, XbimDBAccess.Read);
                 using (XbimModel model2 = XbimModel.CreateTemporaryModel())
                 {
                     using (var txn = model2.BeginTransaction())
                     {
                         model2.InsertCopy(model1.IfcProject);
                         txn.Commit();
                     }
                     model2.SaveAs("Test.ifc", XbimStorageType.IFC);
                     Assert.IsTrue(model2.Instances.Count == 21);
                 }
            } 
           
            Assert.IsTrue(File.Exists("Test.ifc"));
            Assert.IsTrue(XbimModel.ModelOpenCount == 0);
        }
    }
}
