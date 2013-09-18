using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.IO;
using Xbim.IO.Tree;
using System.IO;
using Xbim.Presentation;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.Extensions;


namespace Xbim.Tests
{
    /// <summary>
    /// Summary description for XbimTree
    /// </summary>
    [DeploymentItem(SourceFile, Root)]
    [TestClass]
    public class XbimTree
    {

        private const string Root = "TestSourceFiles";
        private const string SourceModelLeaf = "Clinic-CutDown.xbim";
        private const string SourceFile = Root + @"\" + SourceModelLeaf;
        
        private static XbimModel _model = null;

        [ClassInitialize]
        public static void LoadModel(TestContext context)
        {
            
            if (!File.Exists(SourceFile)) throw new Exception("Cannot find file");
            _model = new XbimModel();
            _model.Open(SourceFile);
            
           
        }

        [ClassCleanup]
        public static void CloseModel()
        {
            if (_model != null)
                _model.Close();
            _model = null;
        }

        

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        // [ClassInitialize()]
        // public static void MyClassInitialize(TestContext testContext) { }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        //
        // Use TestInitialize to run code before running each test 
        // [TestInitialize()]
        // public void MyTestInitialize() { }
        //
        // Use TestCleanup to run code after each test has run
        // [TestCleanup()]
        // public void MyTestCleanup() { }
        //
        #endregion

        [TestMethod]
        public void TestTreeBuilding()
        {
            TreeContainmentStrategy contTreeQuery = new TreeContainmentStrategy(_model);
            TreeNodes containerTreeStructure = TreeBuilder.BuildTreeStructure(contTreeQuery);
            //CreateTreeStructure(Trees[0], containerTreeStructure);

            //TreeComponentStrategy compTreeQuery = new TreeComponentStrategy(_model);
            //TreeNodes componentTreeStructure = TreeBuilder.BuildTreeStructure(compTreeQuery);
            //CreateTreeStructure(Trees[1], componentTreeStructure);


            //New way========================================================================
            List<SpatialViewModel> svList = new List<SpatialViewModel>();

            IfcProject project = _model.IfcProject as IfcProject;
            if (project != null)
            {
                foreach (var item in project.GetSpatialStructuralElements())
                {
                    var sv = new SpatialViewModel(item, null);
                    svList.Add(sv);
                }

                foreach (var child in svList)
                {
                    LazyLoadAll(child);
                }

            }
        }

        

        private void LazyLoadAll(IXbimViewModel parent)
        {

            foreach (var child in parent.Children)
            {
                LazyLoadAll(child);
            }

        }
    }
}
