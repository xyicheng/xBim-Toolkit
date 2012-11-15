using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;
using System.Threading;
using Xbim.IO;
using Xbim.Ifc.GeometryResource;
using Xbim.XbimExtensions;
using System.IO;

namespace Xbim.Tests
{
    /// <summary>
    /// Summary description for InternationalisationTests
    /// </summary>
    [TestClass]
    [DeploymentItem(TestSourceFileIfc, Root)]
    public class InternationalisationTests
    {
        const string Root = "TestSourceFiles";
        const string Temp = "Temp";

        const string TestSourceFileIfc = Root + "/Test.ifc";

        public InternationalisationTests()
        {
        }

        #region Context
        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }
        
        #endregion

        CultureInfo _originalCulture;

        XbimFileModelServer _systemUnderTest;
        List<string> _toDelete;

        [TestInitialize()]
        public void BeforeEachTest() 
        {
            _originalCulture = Thread.CurrentThread.CurrentCulture;
            _systemUnderTest = new XbimFileModelServer();
            _toDelete = new List<string>();
        }
          
        [TestCleanup()]
        public void AfterEachTest() 
        {
            Thread.CurrentThread.CurrentCulture = _originalCulture;
            _systemUnderTest.Dispose();

            foreach (string file in _toDelete)
            {
                if (File.Exists(file))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch { }
                }
            }
        }

        public XbimFileModelServer SuT
        {
            get { return _systemUnderTest; }
        }

        private void EnsureDeleted(string file)
        {
            _toDelete.Add(file);
        }

        [TestMethod]
        public void Parsing_Decimals_is_Culture_Invariant()
        {

            // Use Dutch culture: where commas = decimals, & vice versa
            // 12.345 in Dutch = 12,345 in UK/US & Invariant cultures
            SetCulture("nl");

            SuT.ImportIfc(TestSourceFileIfc);

            // Entity #3 is a cartesian point entity with the value of:
            // #3=IFCCARTESIANPOINT((0.,123.456789,-1.E-006));
            var entity = SuT.GetInstance(3);

            Assert.IsInstanceOfType(entity, typeof(IfcCartesianPoint));
            IfcCartesianPoint point = entity as IfcCartesianPoint;

            Assert.AreEqual(0, point.X, "X incorrect");
            Assert.AreEqual(123.456789, point.Y, "Y incorrect");
            Assert.AreEqual(-1E-006, point.Z, "Z incorrect");   // or -0.000001
            
        }

        [TestMethod]
        public void Writing_Decimals_is_Culture_Invariant()
        {
            // Arrange

            SetCulture("nl");
            
            LoadModelForWriting();
            // Add a point
            IfcCartesianPoint point = SuT.New<IfcCartesianPoint>(cp => { cp.X = 345.6789012; cp.Y = -0.000001; cp.Z = 0.0; });

            string tempFile = GetTempIfcFile();
            // Act
            SuT.ExportIfc(tempFile);

            // Assert

            StreamReader reader = new StreamReader (tempFile);
            string ifcData = reader.ReadToEnd();

            Assert.IsTrue(ifcData.Contains("=IFCCARTESIANPOINT((345.6789012,-1.E-06,0.))"), "Didn't contain expected output");
            
        }

        private string GetTempIfcFile()
        {
            string tempFile = Path.ChangeExtension(Path.GetTempFileName(), ".ifc");
            EnsureDeleted(tempFile);
            return tempFile;
        }

        private string LoadModelForWriting()
        {
            // Start with a baseline IFC, convert to Xbim and open for exit.
            String xbimFile = SuT.ImportIfc(TestSourceFileIfc);
            SuT.Close();
            SuT.Open(xbimFile, System.IO.FileAccess.ReadWrite);
            SuT.BeginTransaction("CultureTest");
            EnsureDeleted(xbimFile);
            return xbimFile;
        }

        private static void SetCulture(string culture)
        {
            Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo(culture);
        }
    }
}
