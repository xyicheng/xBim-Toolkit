using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.Query;
using Xbim.IO;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.Kernel;

namespace XbimQueryTest
{
    [TestClass]
    public class ScannerTest
    {
        [TestMethod]
        public void ValueTest()
        {
            Dictionary<string, string> testCases = new Dictionary<string, string>() { 
            {"12", "INTEGER"},
            {"E12", "STRING"},
            {"12.2", "FLOAT"},
            {"2.", "FLOAT"},
            {".5", "FLOAT"},
            {".3e-5", "FLOAT"},
            {"5.E8", "FLOAT"},
            {"'5E8'", "STRING"},
            {"E5-P", "STRING"},
            {"E-8", "STRING"},
            {"true", "BOOLEAN"},
            {"FALSE", "BOOLEAN"},
            {".f.", "BOOLEAN"},
            {".T.", "BOOLEAN"},
            {"'Nějaký text s interpunkcí'", "STRING"},
            {"\"Nějaký text s interpunkcí\"", "STRING"},
            {"NuLL", "NONDEF"},
            {"Not defined", "NONDEF"},
            {"Unknown", "NONDEF"}
            };

            XbimQueryParser parser = new XbimQueryParser(Xbim.IO.XbimModel.CreateTemporaryModel());
            foreach (var test in testCases)
            {
                parser.SetSource(test.Key);
                var result = parser.ScanOnly();
                Assert.AreEqual(result.Count(), 1, "There should be only one result in this case.");
                Assert.AreEqual(test.Value, result.FirstOrDefault());
            }

        }

        [TestMethod]
        public void QuerySyntaxTest()
        {
            Dictionary<string, bool> testCases = new Dictionary<string, bool>() { 
                //Empty command
                //{"",true},
                //Selection of elements and assignment to the variable
                {"Select wall;",true},
                {"Select wall 'Some wall';",true},
                {"Select wall where name is 'Some name';",true},
                {"Select wall where name is 'Some name' and description contains 'My description';",true},
                {"Select wall where description doesn't contain 'bad description';",true},
                {"Select wall where type is not slab_type;",true},
                {"Select wall where type != slab_type;",true},
                {"Select wall where type is IfcSlabType;",true},
                {"Select wall where type is 'wall type No.1';",true},
                {"Select wall where type = IfcSlabType;",true},
                {"Select wall where 'fire protection' is true;",true},
                {"Select wall where 'fire protection' is .F.;",true},
                {"Select xyz",false},
                {"$MyWalls is wall 'My wall';",true},
                {"$MyWalls is wall where 'External' = true;",true},
                
                //Creation of the new elements
                {"Create new wall 'Totally new wall';",true},
                {"$NewGroup is new group with name 'New group No.1' and description 'This is brand new description of the group';",true},
                
                //Add or remove elements from the group
                {"Add $MyWalls to $NewGroup;",true},
                {"Add $MyWalls from $NewGroup;",false},
                {"Remove $MyWalls from $NewGroup;",true},
                
                //Add or remove elements from the type
                {"$wallType = new wall_type 'New wall type No.1';",true},
                {"$wall is new wall 'New wall is here.';",true},
                {"Add $wall to $wallType;",true}
            };

            Xbim.IO.XbimModel model = Xbim.IO.XbimModel.CreateTemporaryModel();
            XbimQueryParser parser = new XbimQueryParser(model);
            using (var txn = model.BeginTransaction("Query test"))
            {
                foreach (var test in testCases)
                {
                    var result = parser.Parse(test.Key);
                    Assert.AreEqual(test.Value, parser.Errors.FirstOrDefault() == null);
                }
                txn.Commit();
            }
        }

        [TestMethod]
        public void CreationTest()
        {
            XbimModel model = XbimModel.CreateTemporaryModel();
            XbimQueryParser parser = new XbimQueryParser(model);
            using (var txn = model.BeginTransaction("Objects creation"))
            {
                parser.Parse("Create new wall 'My wall';");
                var wall = model.Instances.OfType<IfcWall>().FirstOrDefault();
                Assert.IsNotNull(wall, "There should be one wall now");
                Assert.AreEqual(wall.Name.ToString(), "My wall", "Wall should have a name: 'My wall'");

                parser.Parse("Create new group with name 'New group' and description 'Description of the group';");
                var group = model.Instances.OfType<IfcGroup>().FirstOrDefault();
                Assert.IsNotNull(group, "There should be one group now");

                parser.Parse("Create new IfcWallType with name 'New wall type' and description 'Description of the wall type';");
                var wallType = model.Instances.OfType<IfcGroup>().FirstOrDefault();
                Assert.IsNotNull(wallType, "There should be one wall type now");

                parser.Parse("$MyWall is new IfcWall with name 'New wall assigned' and description 'Description of the wall assigned';");
                var wall2 = model.Instances.Where<IfcWall>(w => w.Name == "New wall assigned").FirstOrDefault();
                Assert.IsNotNull(wallType, "There should be one wall now with the name 'New wall assigned'");
                Assert.AreEqual(parser.Results.FirstOrDefault().Key, "$MyWall");
                Assert.AreEqual(parser.Results.FirstOrDefault().Value, wall2);

                txn.Commit();
            }
        }

        [TestMethod]
        public void AttributeSelectionTest()
        {
            XbimModel model = XbimModel.CreateTemporaryModel();
            XbimQueryParser parser = new XbimQueryParser(model);
            using (var txn = model.BeginTransaction("Objects creation"))
            {
                model.Instances.New<IfcWall>(w => {
                    w.Name = "Wall No. 1";
                    w.Description = "Some description of the wall No. 1";
                });

                model.Instances.New<IfcSlab>(s =>
                {
                    s.Name = "Slab No. 1";
                    s.Description = "Some description of the slab No. 1";
                    s.PredefinedType = IfcSlabTypeEnum.ROOF;
                });

                parser.Parse("Select wall 'Wall No. 1';");
                var wall = parser.Results["$$"].FirstOrDefault();
                Assert.IsNotNull(wall, "There should be one wall now selected in '$$'");

                parser.Parse("Select wall where name is'Wall No. 1';");
                wall = parser.Results["$$"].FirstOrDefault();
                Assert.IsNotNull(wall, "There should be one wall now selected in '$$'");

                parser.Parse("$slabs is slab where predefined type is 'ROOF';");
                var roof = parser.Results["$slabs"].FirstOrDefault();
                Assert.IsNotNull(wall, "There should be one slab now selected in '$slab'");

                txn.Commit();
            }
        }
       
    }
}
