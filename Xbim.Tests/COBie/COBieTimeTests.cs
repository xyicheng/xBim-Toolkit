using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.COBie;
using Xbim.XbimExtensions;
using Xbim.IO;
using System.Diagnostics;

namespace Xbim.Tests.COBie
{
    [DeploymentItem(SourceFile, Root)]
    [TestClass]
    public class COBieTimeTests
    {
        private const string Root = "TestSourceFiles";
        private const string SourceModelLeaf = "Clinic-Handover.xbim";
        private const string SourceFile = Root + @"\" + SourceModelLeaf;
        
        static COBieContext _cobieContext = new COBieContext();
        COBieQueries cobieEngine = new COBieQueries(_cobieContext);
        static IModel _model;

        [ClassInitialize]
        public static void LoadModel(TestContext context)
        {

            _model = new XbimFileModelServer();
            _model.Open(SourceFile);
            _cobieContext = new COBieContext();
            _cobieContext.COBieGlobalValues.Add("FILENAME", SourceFile);
            _cobieContext.COBieGlobalValues.Add("DEFAULTDATE", DateTime.Now.ToString(Constants.DATE_FORMAT));
            _cobieContext.Model = _model;
            COBieQueries cobieEngine = new COBieQueries(_cobieContext);
        }

        [ClassCleanup]
        public static void CloseModel()
        {
            if (_model != null)
                _model.Dispose();
            _model = null;
            _cobieContext = null;
        }
        
        [TestMethod]
        public void Time_On_Contacts()
        {
            //COBieQueries cobieEngine = new COBieQueries(_cobieContext);
            Stopwatch timer = new Stopwatch();
            timer.Start();
            cobieEngine.GetCOBieContactSheet();
            timer.Stop();
            Debug.WriteLine(string.Format("Contact Sheet Time = {0}", timer.Elapsed.ToString()));
            //check time against max time it should take.
            Assert.IsTrue(timer.Elapsed < new TimeSpan(0,0,2));
        }

        [TestMethod]
        public void Time_On_Facility()
        {

            //COBieQueries cobieEngine = new COBieQueries(_cobieContext);
            Stopwatch timer = new Stopwatch();
            timer.Start();
            cobieEngine.GetCOBieFacilitySheet();
            timer.Stop();
            Debug.WriteLine(string.Format("Facility Sheet Time = {0}", timer.Elapsed.ToString()));
            //check time against max time it should take.
            Assert.IsTrue(timer.Elapsed < new TimeSpan(0, 0, 2));
        }

        [TestMethod]
        public void Time_On_Floor()
        {

            //COBieQueries cobieEngine = new COBieQueries(_cobieContext);
            Stopwatch timer = new Stopwatch();
            timer.Start();
            cobieEngine.GetCOBieFloorSheet();
            timer.Stop();
            Debug.WriteLine(string.Format("Floor Sheet Time = {0}", timer.Elapsed.ToString()));
            //check time against max time it should take.
            Assert.IsTrue(timer.Elapsed < new TimeSpan(0, 0, 2));
        }

        [TestMethod]
        public void Time_On_Space()
        {

            //COBieQueries cobieEngine = new COBieQueries(_cobieContext);
            Stopwatch timer = new Stopwatch();
            timer.Start();
            cobieEngine.GetCOBieSpaceSheet();
            timer.Stop();
            Debug.WriteLine(string.Format("Space Sheet Time = {0}", timer.Elapsed.ToString()));
            //check time against max time it should take.
            Assert.IsTrue(timer.Elapsed < new TimeSpan(0, 0, 4));
        }

        [TestMethod]
        public void Time_On_Zone()
        {

            //COBieQueries cobieEngine = new COBieQueries(_cobieContext);
            Stopwatch timer = new Stopwatch();
            timer.Start();
            cobieEngine.GetCOBieZoneSheet();
            timer.Stop();
            Debug.WriteLine(string.Format("Zone Sheet Time = {0}", timer.Elapsed.ToString()));
            //check time against max time it should take.
            Assert.IsTrue(timer.Elapsed < new TimeSpan(0, 0, 2));
        }

        [TestMethod]
        public void Time_On_Type()
        {

            //COBieQueries cobieEngine = new COBieQueries(_cobieContext);
            Stopwatch timer = new Stopwatch();
            timer.Start();
            cobieEngine.GetCOBieTypeSheet();
            timer.Stop();
            Debug.WriteLine(string.Format("Type Sheet Time = {0}", timer.Elapsed.ToString()));
            //check time against max time it should take.
            Assert.IsTrue(timer.Elapsed < new TimeSpan(0, 0, 2));
        }

        [TestMethod]
        public void Time_On_Component()
        {

            //COBieQueries cobieEngine = new COBieQueries(_cobieContext);
            Stopwatch timer = new Stopwatch();
            timer.Start();
            cobieEngine.GetCOBieComponentSheet();
            timer.Stop();
            Debug.WriteLine(string.Format("Component Sheet Time = {0}", timer.Elapsed.ToString()));
            //check time against max time it should take.
            Assert.IsTrue(timer.Elapsed < new TimeSpan(0, 0, 30));
        }

        [TestMethod]
        public void Time_On_System()
        {

           // COBieQueries cobieEngine = new COBieQueries(_cobieContext);
            Stopwatch timer = new Stopwatch();
            timer.Start();
            cobieEngine.GetCOBieSystemSheet();
            timer.Stop();
            Debug.WriteLine(string.Format("System Sheet Time = {0}", timer.Elapsed.ToString()));
            //check time against max time it should take.
            Assert.IsTrue(timer.Elapsed < new TimeSpan(0, 0, 2));
        }

        [TestMethod]
        public void Time_On_Assembly()
        {
            //COBieQueries cobieEngine = new COBieQueries(_cobieContext);
            Stopwatch timer = new Stopwatch();
            timer.Start();
            cobieEngine.GetCOBieAssemblySheet();
            timer.Stop();
            Debug.WriteLine(string.Format("Assembly Sheet Time = {0}", timer.Elapsed.ToString()));
            //check time against max time it should take.
            Assert.IsTrue(timer.Elapsed < new TimeSpan(0, 0, 2));
        }

        [TestMethod]
        public void Time_On_Connection()
        {
            //COBieQueries cobieEngine = new COBieQueries(_cobieContext);
            Stopwatch timer = new Stopwatch();
            timer.Start();
            cobieEngine.GetCOBieConnectionSheet();
            timer.Stop();
            Debug.WriteLine(string.Format("Connection Sheet Time = {0}", timer.Elapsed.ToString()));
            //check time against max time it should take.
            Assert.IsTrue(timer.Elapsed < new TimeSpan(0, 0, 2));
        }

        [TestMethod]
        public void Time_On_Spare()
        {
            //COBieQueries cobieEngine = new COBieQueries(_cobieContext);
            Stopwatch timer = new Stopwatch();
            timer.Start();
            cobieEngine.GetCOBieSpareSheet();
            timer.Stop();
            Debug.WriteLine(string.Format("Spare Sheet Time = {0}", timer.Elapsed.ToString()));
            //check time against max time it should take.
            Assert.IsTrue(timer.Elapsed < new TimeSpan(0, 0, 2));
        }

        [TestMethod]
        public void Time_On_Resource()
        {
            //COBieQueries cobieEngine = new COBieQueries(_cobieContext);
            Stopwatch timer = new Stopwatch();
            timer.Start();
            cobieEngine.GetCOBieResourceSheet();
            timer.Stop();
            Debug.WriteLine(string.Format("Resource Sheet Time = {0}", timer.Elapsed.ToString()));
            //check time against max time it should take.
            Assert.IsTrue(timer.Elapsed < new TimeSpan(0, 0, 2));
        }

        [TestMethod]
        public void Time_On_Job()
        {
            //COBieQueries cobieEngine = new COBieQueries(_cobieContext);
            Stopwatch timer = new Stopwatch();
            timer.Start();
            cobieEngine.GetCOBieJobSheet();
            timer.Stop();
            Debug.WriteLine(string.Format("Resource Sheet Time = {0}", timer.Elapsed.ToString()));
            //check time against max time it should take.
            Assert.IsTrue(timer.Elapsed < new TimeSpan(0, 0, 2));
        }

        [TestMethod]
        public void Time_On_Impact()
        {
            //COBieQueries cobieEngine = new COBieQueries(_cobieContext);
            Stopwatch timer = new Stopwatch();
            timer.Start();
            cobieEngine.GetCOBieImpactSheet();
            timer.Stop();
            Debug.WriteLine(string.Format("Impact Sheet Time = {0}", timer.Elapsed.ToString()));
            //check time against max time it should take.
            Assert.IsTrue(timer.Elapsed < new TimeSpan(0, 0, 2));
        }

        [TestMethod]
        public void Time_On_Document()
        {
            //COBieQueries cobieEngine = new COBieQueries(_cobieContext);
            Stopwatch timer = new Stopwatch();
            timer.Start();
            cobieEngine.GetCOBieDocumentSheet();
            timer.Stop();
            Debug.WriteLine(string.Format("Document Sheet Time = {0}", timer.Elapsed.ToString()));
            //check time against max time it should take.
            Assert.IsTrue(timer.Elapsed < new TimeSpan(0, 0, 2));
        }

        [TestMethod]
        public void Time_On_Attribute()
        {
            //COBieQueries cobieEngine = new COBieQueries(_cobieContext);
            Stopwatch timer = new Stopwatch();
            timer.Start();
            cobieEngine.GetCOBieAttributeSheet();
            timer.Stop();
            Debug.WriteLine(string.Format("Attribute Sheet Time = {0}", timer.Elapsed.ToString()));
            //check time against max time it should take.
            Assert.IsTrue(timer.Elapsed < new TimeSpan(0, 0, 2));
        }

        [TestMethod]
        [Ignore]
        public void Time_On_Coordinate()
        {
            //COBieQueries cobieEngine = new COBieQueries(_cobieContext);
            Stopwatch timer = new Stopwatch();
            timer.Start();
            cobieEngine.GetCOBieCoordinateSheet();
            timer.Stop();
            Debug.WriteLine(string.Format("Coordinate Sheet Time = {0}", timer.Elapsed.ToString()));
            //check time against max time it should take.
            Assert.IsTrue(timer.Elapsed < new TimeSpan(0, 0, 2));
        }

        [TestMethod]
        public void Time_On_Issue()
        {
            //COBieQueries cobieEngine = new COBieQueries(_cobieContext);
            Stopwatch timer = new Stopwatch();
            timer.Start();
            cobieEngine.GetCOBieIssueSheet();
            timer.Stop();
            Debug.WriteLine(string.Format("Issue Sheet Time = {0}", timer.Elapsed.ToString()));
            //check time against max time it should take.
            Assert.IsTrue(timer.Elapsed < new TimeSpan(0, 0, 2));
        }

        
    }
}
