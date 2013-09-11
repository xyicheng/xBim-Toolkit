using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.IO;
using Xbim.Query;
using System.IO;
using Xbim.Ifc2x3.MeasureResource;
using System.Reflection;

namespace XbimQueryTest
{
    [TestClass]
    public class ModelTests
    {
        [TestMethod]
        public void ModelManipulation()
        {
            XbimQueryParser parser = new XbimQueryParser();
            var dataCreation = @"
            Create new wall 'My wall 1';
            Create new wall 'My wall 2';
            Create new wall 'My wall 3';
            ";
            parser.Parse(dataCreation);
            Assert.AreEqual(0, parser.Errors.Count());

            parser.Parse("Save model to file 'output2.ifc';");
            Assert.AreEqual(0, parser.Errors.Count());

            parser.Parse("Close model;");
            Assert.AreEqual(0, parser.Errors.Count());
            
            parser.Parse("Open model from file 'output2.ifc';");
            Assert.AreEqual(0, parser.Errors.Count());
            
            parser.Parse("Select wall;");
            Assert.AreEqual(0, parser.Errors.Count());
            Assert.AreEqual(3, parser.Results["$$"].Count());
        }

    }

    
}
