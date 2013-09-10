using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.Query;
using Xbim.IO;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.MeasureResource;

namespace XbimQueryTest
{
    [TestClass]
    public class ParserTests
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
            {"undefined", "NONDEF"},
            {"Unknown", "NONDEF"},
            {"defined", "DEFINED"}
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
                {"Select wall where 'Heat Performance' is 12.25;",true},
                {"Select wall where 'Heat Performance' is greater than 12.25;",true},
                {"Select wall where 'Heat Performance' is greater than or equal to 12.25;",true},
                {"Select wall where 'Heat Performance' equals 12.25;",true},
                {"Select wall where 'Heat Performance' = 12.25;",true},
                {"Select wall where 'Heat Performance' != 12.25;",true},
                {"Select wall where 'Heat Performance' is less than 12.25;",true},
                {"Select wall where 'Heat Performance' < 12.25;",true},
                {"Select wall where 'Heat Performance' <= 12.25;",true},
                {"Select wall where 'Heat Performance' ~ 12.25;",false},
                {"Select wall where 'Heat Performance' contains 12.25;",false},
                {"Select wall where 'Heat Performance' ~ 'substring';",true},
                {"Select wall where 'Heat Performance' doesn't contain 'substring';",true},
                {"Select xyz",false},
                {"$MyWalls is wall 'My wall';",true},
                {"$MyWalls is wall where 'External' = true;",true},
                
                //Creation of the new elements
                {"Create new wall 'Totally new wall';",true},
                {"Create new wall called 'Totally new wall number 2' and described as 'Other description';",true},
                {"$NewGroup is new group with name 'New group No.1' and description 'This is brand new description of the group';",true},
                
                //Add or remove elements from the group
                {"Add $MyWalls to $NewGroup;",true},
                {"Add $MyWalls from $NewGroup;",false},
                {"Remove $MyWalls from $NewGroup;",true},
                
                //Add or remove elements from the type
                {"$wallType = new wall_type 'New wall type No.1';",true},
                {"$wall is new wall 'New wall is here.';",true},
                {"Add $wall to $wallType;",true},

                //variable manipulation
                {"Dump $wallType;",true},
                {"Export $wallType;",true},
                {"Dump 'name', 'description', 'fire rating' from $wallType;",true},
                {"Dump 'name', 'description', 'fire rating' from $wallType to file 'report.txt';",true},
                {"Clear $wallType;",true},

                //model manipulation syntax
                {"Save model to file 'output.ifc';", true},
                {"Close model;", true},
                {"Open model from file 'output.ifc';", true},
            };

            Xbim.IO.XbimModel model = Xbim.IO.XbimModel.CreateTemporaryModel();
            XbimQueryParser parser = new XbimQueryParser(model);
            parser.Parse("$MyWalls is new wall 'wall';");
            parser.Parse("$NewGroup is new group 'group';");

            foreach (var test in testCases)
            {
                var result = parser.Parse(test.Key);
                var wasOK = 0 == parser.Errors.Count();

                Assert.AreEqual(test.Value, wasOK);
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
                Assert.IsTrue(parser.Results.ContainsKey("$MyWall"));
                Assert.AreEqual(parser.Results["$MyWall"].FirstOrDefault(), wall2);

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

                model.Instances.New<IfcWall>(w =>
                {
                    w.Name = "Wall No. 2";
                    w.Description = "Some description of the wall No. 2";
                });

                model.Instances.New<IfcWall>(w =>
                {
                    w.Name = "Wall No. 3";
                    w.Description = "Some description of the wall No. 3";
                });

                model.Instances.New<IfcSlab>(s =>
                {
                    s.Name = "Slab No. 1";
                    s.Description = "Some description of the slab No. 1";
                    s.PredefinedType = IfcSlabTypeEnum.ROOF;
                });

                txn.Commit();

                parser.Parse("Select wall;");
                var count = parser.Results["$$"].Count();
                Assert.AreEqual(count, 3, "There should be one wall now selected in '$$'");

                parser.Parse("Select wall 'Wall No. 1';");
                var wall = parser.Results["$$"].FirstOrDefault();
                Assert.IsNotNull(wall, "There should be one wall selected now in '$$'");

                parser.Parse("Select wall where name is'Wall No. 1';");
                wall = parser.Results["$$"].FirstOrDefault();
                Assert.IsNotNull(wall, "There should be one wall now selected in '$$'");

                parser.Parse("Select wall where name contains'wall';");
                count = parser.Results["$$"].Count();
                Assert.AreEqual(count, 3 , "There should be three wall now selected in '$$'");

                parser.Parse("Select wall where name contains'WaLL';");
                count = parser.Results["$$"].Count();
                Assert.AreEqual(count, 3, "There should be three wall now selected in '$$'");

                parser.Parse("$slabs is slab where predefined type is 'ROOF';");
                var roof = parser.Results["$slabs"].FirstOrDefault();
                Assert.IsNotNull(roof, "There should be one slab now selected in '$slab'");

            }
        }

        [TestMethod]
        public void PropertySelectionTest()
        {
            #region Model definition
            //create model and sample data
            XbimModel model = XbimModel.CreateTemporaryModel();
            using (var txn = model.BeginTransaction())
            {
                var w1 = model.Instances.New<IfcWall>(w => w.Name = "Wall No.1");
                var w2 = model.Instances.New<IfcWall>(w => w.Name = "Wall No.2");
                var w3 = model.Instances.New<IfcWall>(w => w.Name = "Wall No.3");

                w1.SetPropertySingleValue("Test set 1", "String value", new IfcLabel("some string for wall 1"));
                w1.SetPropertySingleValue("Test set 1", "Double value", new IfcLengthMeasure(156.32));
                w1.SetPropertySingleValue("Test set 1", "Identifier value", new IfcIdentifier("identifier value 123sdfds8sfads58sdf"));
                w1.SetPropertySingleValue("Test set 1", "Integer value", new IfcInteger(235));
                w1.SetPropertySingleValue("Test set 1", "Bool value", new IfcBoolean(true));
                //null property value
                w1.SetPropertySingleValue("Test set 1", "Null value", typeof(IfcLabel));
                var nulProp = w1.GetPropertySingleValue("Test set 1", "Null value");
                nulProp.NominalValue = null;

                w2.SetPropertySingleValue("Test set 1", "String value", new IfcLabel("some string for wall 2"));
                w2.SetPropertySingleValue("Test set 1", "Double value", new IfcLengthMeasure(7856.32));
                w2.SetPropertySingleValue("Test set 1", "Identifier value", new IfcIdentifier("identifier value 123sdfds8sfads58sdf"));
                w2.SetPropertySingleValue("Test set 1", "Integer value", new IfcInteger(735));
                w2.SetPropertySingleValue("Test set 1", "Bool value", new IfcBoolean(true));
                //null property value
                w2.SetPropertySingleValue("Test set 1", "Null value", typeof(IfcLabel));
                var nulProp2 = w2.GetPropertySingleValue("Test set 1", "Null value");
                nulProp2.NominalValue = null;

                w3.SetPropertySingleValue("Test set 1", "String value", new IfcLabel("some string for wall 3"));
                w3.SetPropertySingleValue("Test set 1", "Double value", new IfcLengthMeasure(6.32));
                w3.SetPropertySingleValue("Test set 1", "Identifier value", new IfcIdentifier("identifier value 123sdfds8sfads58sdf"));
                w3.SetPropertySingleValue("Test set 1", "Integer value", new IfcInteger(291));
                w3.SetPropertySingleValue("Test set 1", "Bool value", new IfcBoolean(false));
                //null property value
                w3.SetPropertySingleValue("Test set 1", "Null value", typeof(IfcLabel));
                var nulProp3 = w3.GetPropertySingleValue("Test set 1", "Null value");
                nulProp3.NominalValue = null;

                txn.Commit();
            }
            #endregion

            //Queries and expected number of results
            Dictionary<string, int> tests = new Dictionary<string, int>() { 
            {"Select wall where 'string Value' contains 'some string';",3},
            {"Select wall where 'string Value' contains '1';",1},
            {"Select wall where 'string value' = 'some string for wall 3';",1},
            {"Select wall where 'double value' < 5;",0},
            {"Select wall where 'double value' > 5 and 'double value' < 200;",2},
            {"Select wall where 'integer value' is 291;",1},
            {"Select wall where 'integer value' is 291.25;",1},
            {"Select wall where 'integer value' is 330.25;",0},
            {"Select wall where 'bool value' = true;",2},
            {"Select wall where 'null value' is not defined;",3},
            {"Select wall where 'null value' is undefined;",3},
            {"Select wall where 'null value' = null;",3},
            {"Select wall where 'null value' doesn't equal null;",0},
            };

            //create parser and perform the test
            XbimQueryParser parser = new XbimQueryParser(model);
            foreach (var test in tests)
            {
                parser.Parse(test.Key);
                Assert.AreEqual(0, parser.Errors.Count(), "There shouldn't be any parser errors.");
                Assert.AreEqual(test.Value, parser.Results["$$"].Count());
            }
        }

        [TestMethod]
        public void VariablesOperationsTest()
        {
            XbimModel model = XbimModel.CreateTemporaryModel();
            XbimQueryParser parser = new XbimQueryParser(model);

            //create data using queries
            parser.Parse("Create new wall with name 'My wall No. 1' and description 'First description contains dog.';");
            parser.Parse("Create new wall with name 'My wall No. 2' and description 'First description contains cat.';");
            parser.Parse("Create new wall with name 'My wall No. 3' and description 'First description contains dog and cat.';");
            parser.Parse("Create new wall with name 'My wall No. 4' and description 'First description contains dog and cow.';");
            parser.Parse("Create new wall_type with name 'Wall type No. 1';");
            parser.Parse("Create new system with name 'System No. 1';");
            parser.Parse("Create new system with name 'System No. 2';");
            var res = model.Instances.OfType<IfcWall>().Count();

            parser.Parse("Select wall;");
            Assert.AreEqual(parser.Results["$$"].Count(), 4);
            Assert.AreEqual(parser.Errors.Count(), 0);

            parser.Parse("Select group;");
            Assert.AreEqual(parser.Results["$$"].Count(), 2);
            Assert.AreEqual(parser.Errors.Count(), 0);

            parser.Parse("$a is wall where description contains 'cat';");
            Assert.AreEqual(parser.Results["$a"].Count(), 2);
            parser.Parse("$a is wall where description contains 'cow';");
            Assert.AreEqual(parser.Results["$a"].Count(), 3);
            Assert.AreEqual(parser.Errors.Count(), 0);

            parser.Parse("$a is not wall where description contains 'dog';");
            Assert.AreEqual(parser.Results["$a"].Count(), 1);
            Assert.AreEqual(parser.Errors.Count(), 0);

            parser.Parse("$g is group 'System No. 1';");
            Assert.AreEqual(parser.Errors.Count(), 0);
            parser.Parse("$t is IfcWallType;");
            parser.Parse("Add $a to $g;");
            parser.Parse("Add $a to $t;");
            Assert.AreEqual(parser.Errors.Count(), 0);

            parser.Parse("Add $t to $a;");
            Assert.AreNotEqual(parser.Errors.Count(), 0);

            parser.Parse("Clear $a;");
            Assert.AreEqual(parser.Results["$a"].Count(), 0);
            Assert.AreEqual(parser.Errors.Count(), 0);
        }
    }
}
