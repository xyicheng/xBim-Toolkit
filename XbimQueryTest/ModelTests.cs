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

        [TestMethod]
        public void ErrorLocation()
        {
            XbimQueryParser parser = new XbimQueryParser();
            parser.Parse(@"
                Create new wall 'Wall No.1';
                Create new wall 'Wall No.1';
                Create new wall 'Wall No.1';
                Create new wall 'Wall No.1';
                Create new wall 'Wall No.1';
                Create wall 'Wall No.2';           //first error
                Create new wall 'Wall No.2';
                Create new wall 'Wall No.2';

                $walls1 is wall 'Wall No.1';
                $walls2 is wall 'Wall No.2'       //next error
                
                $type1 is new IfcWallType 'Wall type 1';
                $type2 is new IfcWallType 'Wall type 2';
                Set description to 'Description of type 1', 'Fire rating' to 'Great', IsExternal to true for $type1;
                Set description to 'Description of type 2', 'Fire rating' to 'Poor', IsExternal to false for $type2;
                
                Add $walls1 to $type1;
                Add $walls2 to $type2;
                
                $slab is new slab 'My slab slab';
                $slabType is new slab_type 'My slab type';
                Set predefined type to 'BASESLAB' for $slabType;
                Add $slab to $slabType;
           ");
        }
    }

    
}
