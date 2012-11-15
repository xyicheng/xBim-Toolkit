using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.IO;
using Xbim.Ifc2x3.Extensions;
using System.IO;
using System.Reflection;
using Xbim.XbimExtensions;
using System.Collections;
using System.Globalization;
using System.Threading;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.XbimExtensions.Interfaces;
using Xbim.IO.Parser;
using Xbim.Ifc2x3.Kernel;
using Xbim.ModelGeometry;

namespace Xbim.Tests
{
    // Deploy our sample files to the correct TestResults folder, so we can access under the Test Runner
    [DeploymentItem(TestSourceFileIfc, Root)]
    [DeploymentItem(TestSourceFileIfcXml, Root)]
    [DeploymentItem(TestSourceFileXbim, Root)]
    [TestClass]
    public class XbimModelTest
    {
        const string Root = "TestSourceFiles";
        const string Temp = "Temp";

        // 3 reference files, under version control
        const string TestSourceFileIfc = Root + "/Ref.ifc";
        const string TestSourceFileIfcXml = Root + "/Ref_Xbim_IfcXml.ifcxml";
        const string TestSourceFileXbim = Root + "/Ref_Xbim.xbim";

        #region "Init and Cleanup"

        [ClassInitialize]
        public static void LoadModel(TestContext context)
        {
            Directory.CreateDirectory(Path.Combine(Root, Temp));
        }

        [TestInitialize]
        public void TestInit()
        {
            // this method runs before start of every test
            // any init code
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // this method runs after finishes every test

        }

        #endregion

        #region "Tests: Converts ifc reference file to various formats successfully"

        [TestMethod]
        public void Test_01_CreateTestSuite()
        {
            // This method expects a Ref.ifc file to be converted to various formats in "TestSourceFiles" folder
            // Original Ref.ifc -> Ref_Xbim.xbim -> Ref_Xbim_IfcXml.ifcxml -> Ref_Xbim_IfcXml_Xbim.xbim
            // -> Ref_Xbim_IfcXml_Xbim_Ifc.ifc -> Ref_Xbim_IfcXml_Xbim_ifc_Xbim.xbim

            // By the end of all conversion we will have 3 xbim files. Compare each file with other 2 and they all
            // should have same binary data for each entity.

            string Ref = Root + "/Ref.ifc";
            string Ref_Xbim = Root + "/Ref_Xbim.xbim";
            string Ref_Xbim_IfcXml = Root + "/Ref_Xbim_IfcXml.ifcxml";
            string Ref_Xbim_IfcXml_Xbim = Root + "/Ref_Xbim_IfcXml_Xbim.xbim";
            string Ref_Xbim_IfcXml_Xbim_Ifc = Root + "/Ref_Xbim_IfcXml_Xbim_Ifc.ifc";
            string Ref_Xbim_IfcXml_Xbim_ifc_Xbim = Root + "/Ref_Xbim_IfcXml_Xbim_ifc_Xbim.xbim";

            // create reference xbim file from original ifc
            CreateXbimFile(Ref, Ref_Xbim);

            // create ref ifcxml file from previous xbim
            CreateIfcXmlFile(Ref_Xbim, Ref_Xbim_IfcXml);

            // previous ifcxml to new xbim
            CreateXbimFile(Ref_Xbim_IfcXml, Ref_Xbim_IfcXml_Xbim);

            CreateIfcFile(Ref_Xbim_IfcXml_Xbim, Ref_Xbim_IfcXml_Xbim_Ifc);

            CreateXbimFile(Ref_Xbim_IfcXml_Xbim_Ifc, Ref_Xbim_IfcXml_Xbim_ifc_Xbim);

            // compare xbim files
            CompareXbimFiles(Ref_Xbim, Ref_Xbim_IfcXml_Xbim);
            CompareXbimFiles(Ref_Xbim_IfcXml_Xbim, Ref_Xbim_IfcXml_Xbim_ifc_Xbim);
            CompareXbimFiles(Ref_Xbim_IfcXml_Xbim_ifc_Xbim, Ref_Xbim);
            Assert.IsTrue(true);
        }

        #endregion

        #region "Tests: Convert files from one format to another"

        [TestMethod]
        public void Test_ConvertIfcToXbim()
        {
            string ifcFileName = TestSourceFileIfc;
            string xbimFileName = CreateXbimFile(ifcFileName);

            // check if file is created 
            bool fileExist = File.Exists(xbimFileName);
            Assert.IsTrue(fileExist);

            // check file created is not blank
            byte[] data = File.ReadAllBytes(xbimFileName);
            bool isValidFile = (data.Length > 0);
            Assert.IsTrue(isValidFile);

            // check the created xbim file is same as reference xbim file
            // get any labels in xbimFile1 that are not in xbimFile2
            List<int> badLabels1 = GetFileBadLabels(xbimFileName, TestSourceFileXbim);

            // get any labels in xbimFile2 that are not in xbimFile1
            List<int> badLabels2 = GetFileBadLabels(TestSourceFileXbim, xbimFileName);

            // both files are created using different procedures from same ifc, so should be same and have 0 badlabels
            Assert.AreEqual(badLabels1.Count, 0);
            Assert.AreEqual(badLabels2.Count, 0);

            
        }

        [TestMethod]
        public void Test_ConvertIfcXmlToXbim()
        {
            string ifcXmlFileName = TestSourceFileIfcXml;
            string xbimFileName = CreateXbimFile(ifcXmlFileName);

            // check if file is created 
            bool fileExist = File.Exists(xbimFileName);
            Assert.IsTrue(fileExist);

            // check file created is not blank
            byte[] data = File.ReadAllBytes(xbimFileName);
            bool isValidFile = (data.Length > 0);
            Assert.IsTrue(isValidFile);

            // check the created xbim file is same as reference xbim file
            // get any labels in xbimFile1 that are not in xbimFile2
            List<int> badLabels1 = GetFileBadLabels(xbimFileName, TestSourceFileXbim);

            // get any labels in xbimFile2 that are not in xbimFile1
            List<int> badLabels2 = GetFileBadLabels(TestSourceFileXbim, xbimFileName);

            // both files are created using different procedures from same ifc, so should be same and have 0 badlabels
            Assert.AreEqual(badLabels1.Count, 0);
            Assert.AreEqual(badLabels2.Count, 0);

            
        }

        [TestMethod]
        public void Test_ConvertXbimToIfcXml()
        {
            string xbimFileName = TestSourceFileXbim;
            string ifcXmlFileName = CreateIfcXmlFile(xbimFileName);

            // check if file is created 
            bool fileExist = File.Exists(ifcXmlFileName);
            Assert.IsTrue(fileExist);

            // check file created is not blank
            byte[] data = File.ReadAllBytes(ifcXmlFileName);
            bool isValidFile = (data.Length > 0);
            Assert.IsTrue(isValidFile);

            
        }

        [TestMethod]
        public void Test_ConvertXbimToIfc()
        {
            string xbimFileName = TestSourceFileXbim;
            string ifcFileName = CreateIfcFile(xbimFileName);

            // check if file is created 
            bool fileExist = File.Exists(ifcFileName);
            Assert.IsTrue(fileExist);

            // check file created is not blank
            byte[] data = File.ReadAllBytes(ifcFileName);
            bool isValidFile = (data.Length > 0);
            Assert.IsTrue(isValidFile);

            
        }

        [TestMethod]
        public void Test_ConvertIfcxmlToIfc()
        {
            string ifcXmlFileName = TestSourceFileIfcXml;
            string ifcFileName = CreateIfcFile(ifcXmlFileName);

            // check if file is created 
            bool fileExist = File.Exists(ifcXmlFileName);
            Assert.IsTrue(fileExist);

            // check file created is not blank
            byte[] data = File.ReadAllBytes(ifcXmlFileName);
            bool isValidFile = (data.Length > 0);
            Assert.IsTrue(isValidFile);


        }

        [TestMethod]
        public void Test_ConvertXbimToGeometryCache()
        {
            string geomFileName = CreateGeometry(TestSourceFileXbim);

            // check if file is created 
            bool fileExist = File.Exists(geomFileName);
            Assert.IsTrue(fileExist);

            // check file created is not blank
            byte[] data = File.ReadAllBytes(geomFileName);
            bool isValidFile = (data.Length > 0);
            Assert.IsTrue(isValidFile);


        }


        [TestMethod]
        [ExpectedException(typeof(FileNotFoundException))]
        public void Importing_Missing_File_Throws_FileNotFoundException()
        {
            using (XbimModel modelServer = new XbimModel())
            {
                modelServer.CreateFrom("DoesNotExist.ifc");
            }
        }

        [TestMethod]
        [ExpectedException(typeof(DirectoryNotFoundException))]
        public void Importing_From_Missing_Folder_Throws_DirectoryNotFoundException()
        {
            using (XbimModel modelServer = new XbimModel())
            {
                modelServer.CreateFrom("/BadPath/DoesNotExist.ifc");
            }
        }

        #endregion

        #region "Tests: Open and close files of different formats using FileModelServer and MemoryModel"

        [TestMethod]
        public void Test_OpenIfcXml_FileModel()
        {
            XbimModel model = new XbimModel();
            model.Open(TestSourceFileIfcXml);
            model.Close();
        }

        [TestMethod]
        public void Test_OpenIfc_FileModel()
        {
            IModel model = new XbimModel();
            model.Open(TestSourceFileIfc);
            model.Close();
        }

        [TestMethod]
        public void Test_OpenXbim_FileModel()
        {
            IModel model = new XbimModel();
            model.Open(TestSourceFileXbim);
            model.Close();
        }

        [TestMethod]
        public void Test_OpenIfcXml_MemoryModel()
        {
            IModel model = new XbimModel();
            model.Open(TestSourceFileIfcXml);
            model.Close();
        }

        [TestMethod]
        public void Test_OpenIfc_MemoryModel()
        {
            IModel model = new XbimModel();
            model.Open(TestSourceFileIfc);
            model.Close();
        }

        [TestMethod]
        public void Test_OpenXbim_MemoryModel()
        {
            IModel model = new XbimModel();
            model.Open(TestSourceFileXbim);
            model.Close();
        }
#endregion

        #region "Private Methods"

            private void CompareXbimFiles(string source, string target)
            {
                List<int> badLabels1_3 = GetFileBadLabels(source, target);
                // get any labels in xbimFile2 that are not in xbimFile1
                List<int> badLabels2_3 = GetFileBadLabels(target, source);
                // both files are created using different procedures from same ifc, so should be same and have 0 badlabels
                Assert.AreEqual(badLabels1_3.Count, 0);
                Assert.AreEqual(badLabels2_3.Count, 0);
            }

        

            private static string CreateTempFile(string baseDir, string ext)
            {
                return Path.Combine(Path.Combine(baseDir, Temp), (Path.GetRandomFileName() + ext));

            }

            private string CreateSemanticContent(string xbimSourceFile)
            {
                string xbimFilePath = Path.GetDirectoryName(xbimSourceFile);

                string nonGeomXbimFileName = CreateTempFile(xbimFilePath, ".xbim");

                // create xbim semantic file without geometry
                XbimModel modelServer = new XbimModel();
                modelServer.Open(xbimSourceFile);
                //srl to fix throw new Exception("To Fix");
                // modelServer.ExtractSemantic(nonGeomXbimFileName, XbimStorageType.XBIM, null);
                modelServer.Close();
                

                return nonGeomXbimFileName;
            }

            private string CreateGeometry(string xbimSourceFile)
            {
                string xbimFilePath = Path.GetDirectoryName(xbimSourceFile);

                XbimModel modelServer = new XbimModel();
                modelServer.Open(xbimSourceFile,XbimDBAccess.ReadWrite);
                IEnumerable<IfcProduct> toDraw = modelServer.IfcProducts.Cast<IfcProduct>();
                XbimScene.ConvertGeometry(toDraw, null,false);
                modelServer.Close();

                return xbimSourceFile;
            }

            private static string CreateXbimFile(string sourceFilePath, string targetPath = null)
            {
                string ext = Path.GetExtension(sourceFilePath);

                if (ext.ToLower() == ".ifc")
                {
                    string ifcFilePath = Path.GetDirectoryName(sourceFilePath);
                    string xbimFileName = CreateTempFile(ifcFilePath, ".xbim");
                    if (targetPath != null)
                        xbimFileName = targetPath;
                    using (XbimModel modelServer = new XbimModel())
                    {
                        modelServer.CreateFrom(sourceFilePath, xbimFileName);
                        modelServer.Close();
                    }
                   
                   
                    return xbimFileName;
                }
                else if (ext.ToLower() == ".ifcxml" || ext.ToLower() == ".xml")
                {
                    string ifcxmlFilePath = Path.GetDirectoryName(sourceFilePath);
                    string xbimFileName = CreateTempFile(ifcxmlFilePath, ".xbim"); 
                    if (targetPath != null)
                        xbimFileName = targetPath;
                    using (XbimModel modelServer = new XbimModel())
                    {
                        modelServer.CreateFrom(sourceFilePath, xbimFileName);
                        modelServer.Close();
                    }
                    
                    return xbimFileName;
                }
                return "";
            }

            private static string CreateIfcXmlFile(string sourceFilePath, string targetPath = null)
            {
                string ext = Path.GetExtension(sourceFilePath);

                if (ext.ToLower() == ".ifc")
                {
                    //string ifcFilePath = Path.GetDirectoryName(sourceFilePath);
                    //string xbimFileName = ifcFilePath + "\\Temp\\" + Path.GetRandomFileName() + ".xbim";
                    //XbimFileModelServer modelServer = new XbimFileModelServer();
                    //modelServer.ImportIfc(sourceFilePath, xbimFileName);
                    //modelServer.Close();
                    //modelServer.Dispose();

                    //// now use the xbim file to create ifcXml file
                    //string ifcXmlFileName = ifcFilePath + "\\Temp\\" + Path.GetRandomFileName() + ".ifcxml";
                    //modelServer = new XbimFileModelServer(xbimFileName, FileAccess.ReadWrite);
                    //modelServer.Export(XbimStorageType.IFCXML, ifcXmlFileName);
                    //modelServer.Close();
                    //modelServer.Dispose();

                    //return ifcXmlFileName;
                }
                else if (ext.ToLower() == ".xbim")
                {
                    string xbimFilePath = Path.GetDirectoryName(sourceFilePath);
                    string ifcXmlFileName = CreateTempFile(xbimFilePath, ".ifcxml"); 
                    if (targetPath != null)
                        ifcXmlFileName = targetPath;
                    using (XbimModel modelServer = new XbimModel())
                    {
                        modelServer.Open(sourceFilePath);
                        modelServer.SaveAs(ifcXmlFileName, XbimStorageType.IFCXML);
                        modelServer.Close();
                    }
                    return ifcXmlFileName;
                }
                return "";
            }

            private static string CreateIfcFile(string sourceFilePath, string targetPath = null)
            {
                string ext = Path.GetExtension(sourceFilePath);

                if (ext.ToLower() == ".ifcxml" || ext.ToLower() == ".xml")
                {
                    string ifcFilePath = Path.GetDirectoryName(sourceFilePath);
                    string ifcFileName = CreateTempFile(ifcFilePath, ".ifc"); 
                    string xbimFileName = CreateTempFile(ifcFilePath, ".xbim");

                    // convert xml to xbim first
                    using (XbimModel modelServer = new XbimModel())
                    {
                        modelServer.CreateFrom(sourceFilePath, xbimFileName);
                        modelServer.Open(xbimFileName);
                        modelServer.SaveAs(ifcFileName,XbimStorageType.IFC);
                        modelServer.Close();
                    }
                    return ifcFileName;
                }
                else if (ext.ToLower() == ".xbim")
                {
                    string xbimFilePath = Path.GetDirectoryName(sourceFilePath);
                    string ifcFileName = CreateTempFile(xbimFilePath, ".ifc"); 
                    if (targetPath != null)
                        ifcFileName = targetPath;
                    using (XbimModel modelServer = new XbimModel())
                    {
                        modelServer.Open(sourceFilePath);
                        modelServer.SaveAs(ifcFileName, XbimStorageType.IFC);
                        modelServer.Close();
                    }
                    return ifcFileName;
                }
                return "";
            }

            private List<int> GetFileBadLabels(string fileName1, string fileName2)
            {
                List<int> badLabels = new List<int>();
                using (XbimModel modelServer = new XbimModel())
                {
                    modelServer.Open(fileName1);
                    using (XbimModel modelServer2 = new XbimModel())
                    {
                        modelServer2.Open(fileName2);
                        foreach (var handle in modelServer.Instances)
                        {
                            IPersistIfcEntity entity = modelServer.Instances[handle];
                            int entityLabel = entity.EntityLabel;
                            byte[] b1 = modelServer.GetEntityBinaryData(entity);

                            int posLabel = Math.Abs(entityLabel);

                            // we have entityLabel from 1st file, this should be in second file as well
                            IPersistIfcEntity entity2 = null;
                            bool isBadEntity = false;
                            entity2 = modelServer2.Instances[entityLabel]; //GetInstance(modelServer2, entityLabel); //modelServer2.GetInstance(entityLabel);

                            if (entity2 != null)
                            {
                                byte[] b2 = modelServer2.GetEntityBinaryData(entity2);


                                IStructuralEquatable eqb1 = b1;
                                bool isEqual = eqb1.Equals(b2, StructuralComparisons.StructuralEqualityComparer);
                                if (!isEqual)
                                {
                                    // they may be equal but showing unequal because of decimal precision
                                    // check if its decimal and if yes, then lower the precision and compare

                                    //if (!CompareBytes(modelServer, entity, b1, b2))
                                    isBadEntity = true;
                                    throw new Exception("Entity mismatch: EntityLabel: " + posLabel + " \n" + b1.ToString() + " \n" + b2.ToString());
                                }
                            }
                            else
                            {
                                // file1 entity dosent exist in file2
                                isBadEntity = true;

                            }
                            if (isBadEntity)
                            {
                                // add label to badLabels List
                                badLabels.Add(posLabel);
                            }
                            
                        }
                        modelServer2.Close();
                    }
                    modelServer.Close();
                }
                return badLabels;
            }

        #endregion
    }
}
