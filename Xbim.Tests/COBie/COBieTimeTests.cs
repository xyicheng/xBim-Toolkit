using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.COBie;
using Xbim.XbimExtensions;
using Xbim.IO;
using System.Diagnostics;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.Tests.COBie
{
    [DeploymentItem(SourceFile, Root)]
    [TestClass]
    public class COBieTimeTests
    {
        private const string Root = "TestSourceFiles";
        private const string SourceModelLeaf = "Clinic-CutDown.xbim";
        private const string SourceFile = Root + @"\" + SourceModelLeaf;
        
        private static COBieContext _cobieContext = null;
        private static COBieQueries _cobieEngine = null;
        private static XbimModel _model = null;

        [ClassInitialize]
        public static void LoadModel(TestContext context)
        {
            
            if (!File.Exists(SourceFile)) throw new Exception("Cannot find file");
            _model = new XbimModel();
            _model.Open(SourceFile);
            _cobieContext = new COBieContext();
            _cobieContext.Model = _model;
           
        }

        [ClassCleanup]
        public static void CloseModel()
        {
            if (_model != null)
                _model.Close();
            _model = null;
            _cobieContext = null;
            
        }
        
        //[TestMethod]
        public void Time_On_All()
        {
            
                _cobieEngine = new COBieQueries(_cobieContext);

                ContactTime();
                FacilityTime();
                FloorTime();
                SpaceTime();
                ZoneTime();
                TypeTime();
                ComponentTime();
                SystemTime();
                AssemblyTime();
                ConnectionTime();
                SpareTime();
                ResourceTime();
                JobTime();
                ImpactTime();
                DocumentTime();
                AttributeTime();
                //CoordinateTime();
                IssueTime();
            
        }

        [TestMethod]
        public void Time_On_Contacts()
        {
            _cobieEngine = new COBieQueries(_cobieContext);
            Assert.IsTrue(ContactTime() < new TimeSpan(0, 0, 1));

        }

        [TestMethod]
        public void Time_On_Facility()
        {
            _cobieEngine = new COBieQueries(_cobieContext);
            Assert.IsTrue(FacilityTime() < new TimeSpan(0, 0, 1));
        }


        [TestMethod]
        public void Time_On_Floor()
        {
            _cobieEngine = new COBieQueries(_cobieContext);
            Assert.IsTrue(FloorTime() < new TimeSpan(0, 0, 1));
        }

        [TestMethod]
        public void Time_On_Space()
        {
            _cobieEngine = new COBieQueries(_cobieContext);
            Assert.IsTrue(SpaceTime() < new TimeSpan(0, 0, 3));
        }

        [TestMethod]
        public void Time_On_Zone()
        {
            _cobieEngine = new COBieQueries(_cobieContext);
            Assert.IsTrue(ZoneTime() < new TimeSpan(0, 0, 2));
        }

        [TestMethod]
        public void Time_On_Type()
        {
            _cobieEngine = new COBieQueries(_cobieContext);
            Assert.IsTrue(TypeTime() < new TimeSpan(0, 0, 3));
        }

        [TestMethod]
        public void Time_On_Component()
        {

            _cobieEngine = new COBieQueries(_cobieContext);
            Assert.IsTrue(ComponentTime() < new TimeSpan(0, 0, 5));
        }

        [TestMethod]
        public void Time_On_System()
        {
            _cobieEngine = new COBieQueries(_cobieContext);
            Assert.IsTrue(SystemTime() < new TimeSpan(0, 0, 1));
        }

        [TestMethod]
        public void Time_On_Assembly()
        {
            _cobieEngine = new COBieQueries(_cobieContext);
            Assert.IsTrue(AssemblyTime() < new TimeSpan(0, 0, 1));
        }

        [TestMethod]
        public void Time_On_Connection()
        {
            _cobieEngine = new COBieQueries(_cobieContext);
            Assert.IsTrue(ConnectionTime() < new TimeSpan(0, 0, 1));
        }

        [TestMethod]
        public void Time_On_Spare()
        {
            _cobieEngine = new COBieQueries(_cobieContext);
            Assert.IsTrue(SpareTime() < new TimeSpan(0, 0, 1));
        }

        [TestMethod]
        public void Time_On_Resource()
        {
            _cobieEngine = new COBieQueries(_cobieContext);
            Assert.IsTrue(ResourceTime() < new TimeSpan(0, 0, 1));
        }

        [TestMethod]
        public void Time_On_Job()
        {
            _cobieEngine = new COBieQueries(_cobieContext);
            Assert.IsTrue(JobTime() < new TimeSpan(0, 0, 1));
        }

        [TestMethod]
        public void Time_On_Impact()
        {
            _cobieEngine = new COBieQueries(_cobieContext);
            Assert.IsTrue(ImpactTime() < new TimeSpan(0, 0, 1));
        }

        [TestMethod]
        public void Time_On_Document()
        {
            _cobieEngine = new COBieQueries(_cobieContext);
            Assert.IsTrue(DocumentTime() < new TimeSpan(0, 0, 1));
        }

        [TestMethod]
        public void Time_On_Attribute()
        {
            _cobieEngine = new COBieQueries(_cobieContext);
            Assert.IsTrue(AttributeTime() < new TimeSpan(0, 0, 7));
        }

        [TestMethod]
        [Ignore]
        public void Time_On_Coordinate()
        {
            _cobieEngine = new COBieQueries(_cobieContext);
            Assert.IsTrue(CoordinateTime() < new TimeSpan(0, 0, 2));
        }

        [TestMethod]
        public void Time_On_Issue()
        {
            _cobieEngine = new COBieQueries(_cobieContext);
            Assert.IsTrue(IssueTime() < new TimeSpan(0, 0, 1));
        }

        //---------------------------------------------------------------------------
        private TimeSpan ContactTime()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            _cobieEngine.GetCOBieContactSheet();
            timer.Stop();
            Console.WriteLine(string.Format("Contact Sheet Time = {0}", timer.Elapsed.TotalSeconds.ToString()));
            return timer.Elapsed;
        }

        private TimeSpan FacilityTime()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            _cobieEngine.GetCOBieFacilitySheet();
            timer.Stop();
            Console.WriteLine(string.Format("Facility Sheet Time = {0}", timer.Elapsed.TotalSeconds.ToString()));
            return timer.Elapsed;
        }

        private TimeSpan FloorTime()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            _cobieEngine.GetCOBieFloorSheet();
            timer.Stop();
            Console.WriteLine(string.Format("Floor Sheet Time = {0}", timer.Elapsed.TotalSeconds.ToString()));
            return timer.Elapsed;
        }

        private TimeSpan SpaceTime()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            _cobieEngine.GetCOBieSpaceSheet();
            timer.Stop();
            Console.WriteLine(string.Format("Space Sheet Time = {0}", timer.Elapsed.TotalSeconds.ToString()));
            return timer.Elapsed;
        }

        private TimeSpan ZoneTime()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            _cobieEngine.GetCOBieZoneSheet();
            timer.Stop();
            Console.WriteLine(string.Format("Zone Sheet Time = {0}", timer.Elapsed.TotalSeconds.ToString()));
            return timer.Elapsed;
        }

        private TimeSpan TypeTime()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            _cobieEngine.GetCOBieTypeSheet();
            timer.Stop();
            Console.WriteLine(string.Format("Type Sheet Time = {0}", timer.Elapsed.TotalSeconds.ToString()));
            return timer.Elapsed;
        }

        private TimeSpan ComponentTime()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            _cobieEngine.GetCOBieComponentSheet();
            timer.Stop();
            Console.WriteLine(string.Format("Component Sheet Time = {0}", timer.Elapsed.TotalSeconds.ToString()));
            return timer.Elapsed;
        }

        private TimeSpan SystemTime()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            _cobieEngine.GetCOBieSystemSheet(new Dictionary<string, HashSet<string>>());
            timer.Stop();
            Console.WriteLine(string.Format("System Sheet Time = {0}", timer.Elapsed.TotalSeconds.ToString()));
            return timer.Elapsed;
        }

        private TimeSpan AssemblyTime()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            _cobieEngine.GetCOBieAssemblySheet();
            timer.Stop();
            Console.WriteLine(string.Format("Assembly Sheet Time = {0}", timer.Elapsed.TotalSeconds.ToString()));
            return timer.Elapsed;
        }

        private TimeSpan ConnectionTime()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            _cobieEngine.GetCOBieConnectionSheet();
            timer.Stop();
            Console.WriteLine(string.Format("Connection Sheet Time = {0}", timer.Elapsed.TotalSeconds.ToString()));
            return timer.Elapsed;
        }

        private TimeSpan SpareTime()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            _cobieEngine.GetCOBieSpareSheet();
            timer.Stop();
            Console.WriteLine(string.Format("Spare Sheet Time = {0}", timer.Elapsed.TotalSeconds.ToString()));
            return timer.Elapsed;
        }

        private TimeSpan ResourceTime()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            _cobieEngine.GetCOBieResourceSheet();
            timer.Stop();
            Console.WriteLine(string.Format("Resource Sheet Time = {0}", timer.Elapsed.TotalSeconds.ToString()));
            return timer.Elapsed;
        }

        private TimeSpan JobTime()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            _cobieEngine.GetCOBieJobSheet();
            timer.Stop();
            Console.WriteLine(string.Format("Job Sheet Time = {0}", timer.Elapsed.TotalSeconds.ToString()));
            return timer.Elapsed;
        }

        private TimeSpan ImpactTime()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            _cobieEngine.GetCOBieImpactSheet();
            timer.Stop();
            Console.WriteLine(string.Format("Impact Sheet Time = {0}", timer.Elapsed.TotalSeconds.ToString()));
            return timer.Elapsed;
        }

        private TimeSpan DocumentTime()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            _cobieEngine.GetCOBieDocumentSheet();
            timer.Stop();
            Console.WriteLine(string.Format("Document Sheet Time = {0}", timer.Elapsed.TotalSeconds.ToString()));
            return timer.Elapsed;
        }

        private TimeSpan AttributeTime()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            _cobieEngine.GetCOBieAttributeSheet();
            timer.Stop();
            Console.WriteLine(string.Format("Attribute Sheet Time = {0}", timer.Elapsed.TotalSeconds.ToString()));
            return timer.Elapsed;
        }

        private TimeSpan CoordinateTime()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            _cobieEngine.GetCOBieCoordinateSheet();
            timer.Stop();
            Console.WriteLine(string.Format("Coordinate Sheet Time = {0}", timer.Elapsed.TotalSeconds.ToString()));
            return timer.Elapsed;
        }

        private TimeSpan IssueTime()
        {
            Stopwatch timer = new Stopwatch();
            timer.Start();
            _cobieEngine.GetCOBieIssueSheet();
            timer.Stop();
            Console.WriteLine(string.Format("Issue Sheet Time = {0}", timer.Elapsed.TotalSeconds.ToString()));
            return timer.Elapsed;
        }


        
    }
}
