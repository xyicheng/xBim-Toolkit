using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.IO;
using Xbim.Query;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.Extensions;

namespace XbimQueryTest
{
    [TestClass]
    public class ClassificationTest
    {
        [TestMethod]
        public void CreateClassificationGroups()
        {
            XbimModel model = XbimModel.CreateTemporaryModel();
            XbimQueryParser parser = new XbimQueryParser(model);

            //create data using queries
            parser.Parse(@"
            Create classification NRM;
            //Create classification uniclass;            
            Create new wall with name 'My wall No. 1' and description 'First description contains dog.';
            Create new wall with name 'My wall No. 2' and description 'First description contains cat.';
            Create new wall with name 'My wall No. 3' and description 'First description contains dog and cat.';
            Create new wall with name 'My wall No. 4' and description 'First description contains dog and cow.';
            $walls is wall;
            $extWall is group '02.05.01';
            Add $walls to $extWall;
            ");

            IfcGroup gr = parser.Results["$extWall"].FirstOrDefault() as IfcGroup;
            Assert.AreEqual(0, parser.Errors.Count());
            Assert.IsNotNull(gr);
            Assert.AreEqual("External walls above ground floor", gr.Description.ToString());
            Assert.AreEqual(4, gr.GetGroupedObjects().Count());
        }
    }
}
