using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.Script;
using Xbim.IO;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.MaterialResource;
using Xbim.Ifc2x3.QuantityResource;

namespace XbimQueryTest
{
    [TestClass]
    public class XbimScriptTests
    {
        [TestMethod]
        public void ValueTest()
        {
            Dictionary<string, string> testCases = new Dictionary<string, string>() { 
            {"12", "INTEGER"},
            {"E12", "STRING"},
            {"12.2", "DOUBLE"},
            {"2.", "DOUBLE"},
            {".5", "DOUBLE"},
            {".3e-5", "DOUBLE"},
            {"5.E8", "DOUBLE"},
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
                {"Select every wall;",true},
                {"Select every wall 'Some wall';",true},
                {"Select every wall where name is 'Some name';",true},
                {"Select every wall where name is 'Some name' and description contains 'My description';",true},
                {"Select every wall where description doesn't contain 'bad description';",true},
                {"Select every wall where type is not slab_type;",true},
                {"Select every wall where thickness is less than 25.32;",true},
                {"Select every wall where type != slab_type;",true},
                {"Select every wall where type is IfcSlabType;",true},
                {"Select every wall where type is 'wall type No.1';",true},
                {"Select every wall where type = IfcSlabType;",true},
                {"Select every wall where type contains 'part name';",true},
                {"Select every wall where 'fire protection' is true;",true},
                {"Select every wall where 'fire protection' is .F.;",true},
                {"Select every wall where 'Heat Performance' is 12.25;",true},
                {"Select every wall where 'Heat Performance' is greater than 12.25;",true},
                {"Select every wall where 'Heat Performance' is greater than or equal to 12.25;",true},
                {"Select every wall where 'Heat Performance' equals 12.25;",true},
                {"Select every wall where 'Heat Performance' = 12.25;",true},
                {"Select every wall where 'Heat Performance' != 12.25;",true},
                {"Select every wall where 'Heat Performance' is less than 12.25;",true},
                {"Select every wall where 'Heat Performance' < 12.25;",true},
                {"Select every wall where 'Heat Performance' <= 12.25;",true},
                {"Select every wall where 'Heat Performance' ~ 12.25;",false},
                {"Select every wall where 'Heat Performance' contains 12.25;",false},
                {"Select every wall where 'Heat Performance' ~ 'substring';",true},
                {"Select every wall where 'Heat Performance' doesn't contain 'substring';",true},
                {"Select every wall where 'Heat Performance' isn't like 'substring';",true},
                {"Select every wall where 'Heat Performance' is not like 'substring';",true},
                {"Select every xyz",false},
                {"$MyWalls is every wall 'My wall';",true},
                {"$MyWalls is every wall where 'External' = true;",true},
                
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

                 //Add or remove elements from spatial element (site, building, storey, space)
                {"$allWalls = every wall;",true},
                {"$building is new building 'Default building';",true},
                {"Add $allWalls to $building;",true},

                //variable manipulation
                {"Dump $wallType;",true},
                {"Export $wallType;",true},
                {"Dump 'name', 'description', 'fire rating' from $wallType;",true},
                {"Dump 'name', 'description', 'fire rating' from $wallType to file 'report.txt';",true},
                {"Count $wallType;",true},
                {"Clear $wallType;",true},

                //model manipulation syntax
                {"Save model to file 'output.ifc';", true},
                {"Close model;", true},
                {"Open model from file 'output.ifc';", true},
                {"Validate model;", true},

                //setting of attributes and properties
                {"$wall is new wall with name 'New wall is here' and description 'New description for the wall';",true},
                {"Set name to 'New name' for $wall;", true},
                {"Set name to NULL for $wall;", true},
                {"Set name to 'New name', description to 'New description' for $wall;", true},
                {"Set name to 'New name', description to 'New description', 'fire protection' to 12.3 for $wall;", true},
                {"Set name to 123, description to 'New description', 'fire protection' to 12.3 for $wall;", false},
                {"Set name to 'Some name', Description to 'Property description', fire_rating to 123.5 for $wall in property set 'PSet_Wall_Common';", true},

                //element type attributes and properties
                {"Select every wall where type name is 'Some name';",true},
                {"Select every wall where type description is 'Some description';",true},
                {"Select every slab where type predefined type is 'ROOF';",true},
                {"Select every slab where type 'Fire rating' is 'Great';",true},

                //contained in group attributes and properties
                {"Select every wall where group name is 'Some name';",true},
                {"Select every wall where group description is 'Some description';",true},
                {"Select every slab where group predefined type is 'ROOF';",true},
                {"Select every slab where group 'Fire rating' is 'Great';",true},
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
                Assert.IsTrue(parser.Results.IsDefined("$MyWall"));
                Assert.AreEqual(parser.Results["$MyWall"].FirstOrDefault(), wall2);

                parser.Parse("$Space is new space with name 'New space' and description 'Description of the space';");
                var space = model.Instances.Where<Xbim.Ifc2x3.ProductExtension.IfcSpace>(w => w.Name == "New space").FirstOrDefault();
                Assert.IsNotNull(wallType, "There should be one space now with the name 'New space'");
                Assert.IsTrue(parser.Results.IsDefined("$Space"));
                Assert.AreEqual(parser.Results["$Space"].FirstOrDefault(), space);

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

                parser.Parse("Select every wall;");
                var count = parser.Results["$$"].Count();
                Assert.AreEqual(count, 3, "There should be one wall now selected in '$$'");

                parser.Parse("Select every wall 'Wall No. 1';");
                var wall = parser.Results["$$"].FirstOrDefault();
                Assert.IsNotNull(wall, "There should be one wall selected now in '$$'");

                parser.Parse("Select every wall where name is'Wall No. 1';");
                wall = parser.Results["$$"].FirstOrDefault();
                Assert.IsNotNull(wall, "There should be one wall now selected in '$$'");

                parser.Parse("Select every wall where name contains'wall';");
                count = parser.Results["$$"].Count();
                Assert.AreEqual(count, 3 , "There should be three wall now selected in '$$'");

                parser.Parse("Select every wall where name contains'WaLL';");
                count = parser.Results["$$"].Count();
                Assert.AreEqual(count, 3, "There should be three wall now selected in '$$'");

                parser.Parse("$slabs is every slab where predefined type is 'ROOF';");
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
            {"Select every wall where 'string Value' contains 'some string';",3},
            {"Select every wall where 'string Value' contains '1';",1},
            {"Select every wall where 'string value' = 'some string for wall 3';",1},
            {"Select every wall where 'double value' < 5;",0},
            {"Select every wall where 'double value' > 5 and 'double value' < 200;",2},
            {"Select every wall where 'integer value' is 291;",1},
            {"Select every wall where 'integer value' is 291.25;",1},
            {"Select every wall where 'integer value' is 330.25;",0},
            {"Select every wall where 'bool value' = true;",2},
            {"Select every wall where 'null value' is not defined;",3},
            {"Select every wall where 'null value' is undefined;",3},
            {"Select every wall where 'null value' = null;",3},
            {"Select every wall where 'null value' doesn't equal null;",0},
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
            parser.Parse(@"
            Create new wall with name 'My wall No. 1' and description 'First description contains dog.';
            Create new wall with name 'My wall No. 2' and description 'First description contains cat.';
            Create new wall with name 'My wall No. 3' and description 'First description contains dog and cat.';
            Create new wall with name 'My wall No. 4' and description 'First description contains dog and cow.';
            Create new wall_type with name 'Wall type No. 1';
            Create new system with name 'System No. 1';
            Create new system with name 'System No. 2';");

            //check data are created
            parser.Parse("Select every wall;");
            Assert.AreEqual(parser.Results["$$"].Count(), 4);
            Assert.AreEqual(parser.Errors.Count(), 0);

            parser.Parse("Select every group;");
            Assert.AreEqual(parser.Results["$$"].Count(), 2);
            Assert.AreEqual(parser.Errors.Count(), 0);

            //variable tests
            parser.Parse("$a is every wall where description contains 'cat';");
            Assert.AreEqual(parser.Results["$a"].Count(), 2);
            parser.Parse("$a is every wall where description contains 'cow';");
            Assert.AreEqual(parser.Results["$a"].Count(), 3);
            Assert.AreEqual(parser.Errors.Count(), 0);

            parser.Parse("$a is not every wall where description contains 'dog';");
            Assert.AreEqual(parser.Results["$a"].Count(), 1);
            Assert.AreEqual(parser.Errors.Count(), 0);

            parser.Parse("$g is every group 'System No. 1';");
            Assert.AreEqual(parser.Errors.Count(), 0);
            parser.Parse("$t is every IfcWallType;");
            parser.Parse("Add $a to $g;");
            parser.Parse("Add $a to $t;");
            Assert.AreEqual(parser.Errors.Count(), 0);

            parser.Parse("Add $t to $a;");
            Assert.AreNotEqual(parser.Errors.Count(), 0);

            parser.Parse("Clear $a;");
            Assert.AreEqual(parser.Results["$a"].Count(), 0);
            Assert.AreEqual(parser.Errors.Count(), 0);

            parser.Parse(@"
            $space is new space 'Original space';
            $walls is every wall;
            Add $walls to $space;
            ");
            IfcSpace space = parser.Results["$space"].FirstOrDefault() as IfcSpace;
            Assert.AreEqual(parser.Errors.Count(), 0);
            Assert.IsNotNull(space);
            Assert.AreEqual(4, space.GetContainedElements().Count());
        }

        [TestMethod]
        public void AttributeAndPropertySettingTest()
        { 
            //create test model
            XbimModel model = XbimModel.CreateTemporaryModel();
            using (var txn = model.BeginTransaction())
            {
                var w1 = model.Instances.New<IfcWall>(w => { w.Name = "Wall No.1"; w.Description = "Some doscription No.1"; });
                w1.SetPropertySingleValue("Testing property set", "Testing label", new IfcLabel("Testing value"));
                w1.SetPropertySingleValue("Testing property set", "Testing length", new IfcLengthMeasure(12.5));
                w1.SetPropertySingleValue("Testing property set", "Testing integer", new IfcInteger(56));
                w1.SetPropertySingleValue("Testing property set", "Testing bool", new IfcBoolean(true));
                w1.SetPropertySingleValue("Testing property set", "Testing logical", new IfcLogical(null));
                Xbim.XbimExtensions.SelectTypes.IfcValue nullVal = null;
                w1.SetPropertySingleValue("Testing property set", "Testing not defined", nullVal);

                txn.Commit();
            }

            //test cases
            XbimQueryParser parser = new XbimQueryParser(model);
            parser.Parse(@"
            $wall = every wall 'Wall No.1';
            Set name to 'Changed name' for $wall;
            Set description to null for $wall;
            Set 'Testing label' to 'New label' for $wall;
            Set 'Testing length' to 123.5 for $wall;
            Set 'Testing integer' to 78 for $wall;
            Set 'Testing bool' to false for $wall;
            Set 'Testing logical' to true for $wall;
            Set 'Testing not defined' to 'New label value' for $wall;
            $slab is new slab 'Roof slab';
            Set predefined type to 'roof' for $slab;
            Set name to 'Some name', Description to 'Property description', fire_rating to 123.5 for $slab in property set 'PSet_Slab_Common';
            ");

            //testing object
            var wall = model.Instances.OfType<IfcWall>().FirstOrDefault();
            var slab = model.Instances.OfType<IfcSlab>().FirstOrDefault();

            Assert.AreEqual(0, parser.Errors.Count());
            Assert.AreEqual("Changed name", wall.Name.ToString());
            Assert.IsNull(wall.Description);
            Assert.AreEqual("New label", wall.GetPropertySingleNominalValue("Testing property set", "Testing label").ToString());
            Assert.AreEqual(123.5, wall.GetPropertySingleNominalValue("Testing property set", "Testing length").Value);
            Assert.AreEqual((Int64)78, wall.GetPropertySingleNominalValue("Testing property set", "Testing integer").Value);
            Assert.AreEqual(false, wall.GetPropertySingleNominalValue("Testing property set", "Testing bool").Value);
            Assert.AreEqual(true, wall.GetPropertySingleNominalValue("Testing property set", "Testing logical").Value);
            Assert.AreEqual("New label value", wall.GetPropertySingleNominalValue("Testing property set", "Testing not defined").Value);
            Assert.AreEqual(IfcSlabTypeEnum.ROOF, parser.Results["$slab"].Cast<IfcSlab>().FirstOrDefault().PredefinedType);
            Assert.AreEqual("Property description", slab.GetPropertySingleNominalValue("PSet_Slab_Common", "Description").Value);
        }

        [TestMethod]
        public void QuantitySettingTest()
        {
            var model = CreateQuantityModel();
            var parser = new XbimQueryParser(model);
            
            //this should change all values of simple element quantities
            parser.Parse(@"
                $o is every wall;
                $o is every walltype;
                
                Set 'Area' to 2.3, 'Count' to 2, 'Length' to 2.3, 'Time' to 2.3, 'Volume' to 2.3, 'Weight' to 2.3 for $o;
                Dump 'Area', 'Count', 'Length', 'Time', 'Volume', 'Weight'from $o to file 'Values.txt';
                Save model to file 'Quantities.ifc';
            ");

            //check parser errors
            Assert.AreEqual(0, parser.Errors.Count());

            IfcWall wall = model.Instances.OfType<IfcWall>().FirstOrDefault();;
            IfcWallType wallType = model.Instances.OfType<IfcWallType>().FirstOrDefault();
            
            var wArea = wall.GetElementPhysicalSimpleQuantity("BasicSet", "Area") as IfcQuantityArea;
            var wCount = wall.GetElementPhysicalSimpleQuantity("BasicSet", "Count") as IfcQuantityCount;
            var wLength = wall.GetElementPhysicalSimpleQuantity("BasicSet", "Length") as IfcQuantityLength;
            var wTime = wall.GetElementPhysicalSimpleQuantity("BasicSet", "Time") as IfcQuantityTime;
            var wVolume = wall.GetElementPhysicalSimpleQuantity("BasicSet", "Volume") as IfcQuantityVolume;
            var wWeight = wall.GetElementPhysicalSimpleQuantity("BasicSet", "Weight") as IfcQuantityWeight;

            Assert.IsTrue(AlmostEqual(2.3, wArea.AreaValue));
            Assert.IsTrue(AlmostEqual(2, wCount.CountValue));
            Assert.IsTrue(AlmostEqual(2.3, wLength.LengthValue));
            Assert.IsTrue(AlmostEqual(2.3, wTime.TimeValue));
            Assert.IsTrue(AlmostEqual(2.3, wVolume.VolumeValue));
            Assert.IsTrue(AlmostEqual(2.3, wWeight.WeightValue));

            wArea = wallType.GetElementPhysicalSimpleQuantity("BasicSet", "Area") as IfcQuantityArea;
            wCount = wallType.GetElementPhysicalSimpleQuantity("BasicSet", "Count") as IfcQuantityCount;
            wLength = wallType.GetElementPhysicalSimpleQuantity("BasicSet", "Length") as IfcQuantityLength;
            wTime = wallType.GetElementPhysicalSimpleQuantity("BasicSet", "Time") as IfcQuantityTime;
            wVolume = wallType.GetElementPhysicalSimpleQuantity("BasicSet", "Volume") as IfcQuantityVolume;
            wWeight = wallType.GetElementPhysicalSimpleQuantity("BasicSet", "Weight") as IfcQuantityWeight;

            Assert.IsTrue(AlmostEqual(2.3, wArea.AreaValue));
            Assert.IsTrue(AlmostEqual(2, wCount.CountValue));
            Assert.IsTrue(AlmostEqual(2.3, wLength.LengthValue));
            Assert.IsTrue(AlmostEqual(2.3, wTime.TimeValue));
            Assert.IsTrue(AlmostEqual(2.3, wVolume.VolumeValue));
            Assert.IsTrue(AlmostEqual(2.3, wWeight.WeightValue));
        }

        private bool AlmostEqual(double a, double b)
        {
            return Math.Abs(Math.Abs(a) - Math.Abs(b)) < 0.0001;
        }

        private XbimModel CreateQuantityModel()
        {
            var model = XbimModel.CreateTemporaryModel();
            using (var txn = model.BeginTransaction())
            {
                var wall = model.Instances.New<IfcWall>(w => { w.Name = "Wall No.1"; });
                var wallType = model.Instances.New<IfcWallType>(wt => { wt.Name = "Wall Type No.1"; });

                wall.SetElementPhysicalSimpleQuantity("BasicSet", "Area", 123.5, XbimQuantityTypeEnum.AREA, null);
                wall.SetElementPhysicalSimpleQuantity("BasicSet", "Count", 123, XbimQuantityTypeEnum.COUNT, null);
                wall.SetElementPhysicalSimpleQuantity("BasicSet", "Length", 123.5, XbimQuantityTypeEnum.LENGTH, null);
                wall.SetElementPhysicalSimpleQuantity("BasicSet", "Time", 123.5, XbimQuantityTypeEnum.TIME, null);
                wall.SetElementPhysicalSimpleQuantity("BasicSet", "Volume", 123.5, XbimQuantityTypeEnum.VOLUME, null);
                wall.SetElementPhysicalSimpleQuantity("BasicSet", "Weight", 123.5, XbimQuantityTypeEnum.WEIGHT, null);

                wallType.SetElementPhysicalSimpleQuantity("BasicSet", "Area", 123.5, XbimQuantityTypeEnum.AREA, null);
                wallType.SetElementPhysicalSimpleQuantity("BasicSet", "Count", 123, XbimQuantityTypeEnum.COUNT, null);
                wallType.SetElementPhysicalSimpleQuantity("BasicSet", "Length", 123.5, XbimQuantityTypeEnum.LENGTH, null);
                wallType.SetElementPhysicalSimpleQuantity("BasicSet", "Time", 123.5, XbimQuantityTypeEnum.TIME, null);
                wallType.SetElementPhysicalSimpleQuantity("BasicSet", "Volume", 123.5, XbimQuantityTypeEnum.VOLUME, null);
                wallType.SetElementPhysicalSimpleQuantity("BasicSet", "Weight", 123.5, XbimQuantityTypeEnum.WEIGHT, null);

                txn.Commit();
            }

            return model;
        }

        [TestMethod]
        public void QuanityConditionTest()
        {
            var model = CreateQuantityModel();
            var parser = new XbimQueryParser(model);

            parser.Parse(@"
                $test1 = every wall where 'length' is 123.5 and 'area' is 123.5;
                $test2 = every walltype where 'length' is 123.5 and 'area' is 123.5;
            ");

            Assert.AreEqual(0, parser.Errors.Count());
            Assert.AreEqual(1, parser.Results["$test1"].Count());
            Assert.AreEqual(1, parser.Results["$test2"].Count());
        }

        [TestMethod]
        public void TypeConditionTest()
        {
            XbimModel model = XbimModel.CreateTemporaryModel();
            XbimQueryParser parser = new XbimQueryParser(model);
            parser.Parse(@"
            Create new wall 'First wall';
            Create new wall 'Second wall';
            Create new wall 'Third wall';
            Create new wall_type 'My wall type';
            $wall = every wall;
            $wallType = every wall_type;
            Add $wall to $wallType;
            $test1 is every wall where type is 'My wall type';
            $test2 is every wall where type is IfcWallType;
            Create new slab 'New slab';
            $test3 is every slab where type is not defined;
            $test4 is every wall where type is defined;
            $test5 is every wall where type contains 'my wall';
            ");

            Assert.AreEqual(0, parser.Errors.Count());
            Assert.AreEqual(3, parser.Results["$test1"].Count());
            Assert.AreEqual(3, parser.Results["$test2"].Count());
            Assert.AreEqual(1, parser.Results["$test3"].Count());
            Assert.AreEqual(3, parser.Results["$test4"].Count());
            Assert.AreEqual(3, parser.Results["$test5"].Count());
        }

        [TestMethod]
        public void MaterialSelectionTest()
        {
            XbimModel model = XbimModel.CreateTemporaryModel();
            using (var txn = model.BeginTransaction())
            {
                var wall = model.Instances.New<IfcWall>(w => w.Name = "New wall");
                var material = model.Instances.New<IfcMaterial>(m => m.Name = "Plain material");
                wall.SetMaterial(material);

                var wall2 = model.Instances.New<IfcWall>(w => w.Name = "Second new wall");
                var materialUsage = model.Instances.New<IfcMaterialLayerSetUsage>(mlsu =>
                {
                    mlsu.DirectionSense = IfcDirectionSenseEnum.POSITIVE;
                    mlsu.LayerSetDirection = IfcLayerSetDirectionEnum.AXIS1;
                    mlsu.OffsetFromReferenceLine = 0;
                    mlsu.ForLayerSet = model.Instances.New<IfcMaterialLayerSet>(mls => {
                        mls.MaterialLayers.Add_Reversible(model.Instances.New<IfcMaterialLayer>(ml => {
                            ml.IsVentilated = false;
                            ml.LayerThickness = 10.0;
                            ml.Material = model.Instances.New<IfcMaterial>(m => m.Name = "Plaster");
                        }));
                        mls.MaterialLayers.Add_Reversible(model.Instances.New<IfcMaterialLayer>(ml =>
                        {
                            ml.IsVentilated = false;
                            ml.LayerThickness = 120.5;
                            ml.Material = model.Instances.New<IfcMaterial>(m => m.Name = "Bricks");
                        }));
                        mls.MaterialLayers.Add_Reversible(model.Instances.New<IfcMaterialLayer>(ml =>
                        {
                            ml.IsVentilated = false;
                            ml.LayerThickness = 120.5;
                            ml.Material = model.Instances.New<IfcMaterial>(m => m.Name = "Concreate");
                        }));
                        mls.MaterialLayers.Add_Reversible(model.Instances.New<IfcMaterialLayer>(ml =>
                        {
                            ml.IsVentilated = false;
                            ml.LayerThickness = 5.0;
                            ml.Material = model.Instances.New<IfcMaterial>(m => m.Name = "Outer finish");
                        }));
                    });
                });
                wall2.SetMaterial(materialUsage);


                txn.Commit();
            }
 
            //tests
            XbimQueryParser parser = new XbimQueryParser(model);
            parser.Parse(@"
            $test1 = every wall where material contains plaster;
            $test2 = every wall where material is plaster;
            $test3 = every wall where material contains pl;
            $test4 = every wall where thickness > 300;
            $test5 = every wall where thickness = 256;
            ");

            Assert.AreEqual(0, parser.Errors.Count());
            Assert.AreEqual(1, parser.Results["$test1"].Count());
            Assert.AreEqual(1, parser.Results["$test2"].Count());
            Assert.AreEqual(2, parser.Results["$test3"].Count());
            Assert.AreEqual(0, parser.Results["$test4"].Count());
            Assert.AreEqual(1, parser.Results["$test5"].Count());
        }

        [TestMethod]
        public void ElementTypeAttributes()
        {
            XbimModel model = XbimModel.CreateTemporaryModel();
            XbimQueryParser parser = new XbimQueryParser(model);

            parser.Parse(@"
                Create new wall 'Wall No.1';
                Create new wall 'Wall No.1';
                Create new wall 'Wall No.1';
                Create new wall 'Wall No.1';
                Create new wall 'Wall No.1';
                Create new wall 'Wall No.2';
                Create new wall 'Wall No.2';
                Create new wall 'Wall No.2';

                $walls1 is every wall 'Wall No.1';
                $walls2 is every wall 'Wall No.2';
                
                $type1 is new IfcWallType 'Wall type 1';
                $type2 is new IfcWallType 'Wall type 2';
                Set description to 'Description of type 1', 'Fire rating' to 'Great', IsExternal to true for $type1;
                Set description to 'Description of type 2', 'Fire rating' to 'Poor', IsExternal to false for $type2;
                
                Add $walls1 to $type1;
                Add $walls2 to $type2;
                
                $test1 is every wall where type name is 'Wall type 1';
                $test2 is every wall where type IsExternal is false;

                $slab is new slab 'My slab slab';
                $slabType is new slab_type 'My slab type';
                Set predefined type to 'BASESLAB' for $slabType;
                Add $slab to $slabType;

                $test3 is every slab where type predefined type is 'BASESLAB';
            ");

            Assert.AreEqual(0, parser.Errors.Count());
            Assert.AreEqual(5, parser.Results["$test1"].Count());
            Assert.AreEqual(3, parser.Results["$test2"].Count());
            Assert.AreEqual(1, parser.Results["$test3"].Count());
        }

        [TestMethod]
        public void ElementGroupsAttributes()
        {
            XbimModel model = XbimModel.CreateTemporaryModel();
            XbimQueryParser parser = new XbimQueryParser(model);

            parser.Parse(@"
                Create classification NRM;

                Create new wall 'Wall No.X';
                Create new wall 'Wall No.X';
                Create new wall 'Wall No.X';
                Create new wall 'Wall No.X';
                Create new wall 'Wall No.X';
                Create new wall 'Wall No.X';
                Create new wall 'Wall No.X';
                Create new wall 'Wall No.X';

                $walls is every wall;
                $group = every group where name is '02.05.01';
                Add $walls to $group;
                
                $test1 is every wall where group description is 'External Walls'; //this is a group in ancestor hierarchy
                $test2 is every wall where group description contains 'external'; 
            ");

            Assert.AreEqual(0, parser.Errors.Count());
            Assert.AreEqual(8, parser.Results["$test1"].Count());
        }

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

            parser.Parse("Select every wall;");
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

                $walls1 is every wall 'Wall No.1';
                $walls2 is every wall 'Wall No.2'       //next error
                
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
            $walls is every wall;
            $extWall is every group '02.05.01';
            Add $walls to $extWall;
            ");

            IfcGroup gr = parser.Results["$extWall"].FirstOrDefault() as IfcGroup;
            Assert.AreEqual(0, parser.Errors.Count());
            Assert.IsNotNull(gr);
            Assert.AreEqual("External walls above ground floor", gr.Description.ToString());
            Assert.AreEqual(4, gr.GetGroupedObjects().Count());
        }

        [TestMethod]
        public void ConditionsChainingTest()
        {
            //new parser with default empty model
            XbimQueryParser parser = new XbimQueryParser();
            
            //create data using queries
            parser.Parse(@"
            Create new wall with name 'My wall No. 1' and description 'First description contains dog.';
            Create new wall with name 'My wall No. 2' and description 'First description contains cat.';
            Create new wall with name 'My wall No. 3' and description 'First description contains dog and cat.';
            Create new wall with name 'My wall No. 4' and description 'First description contains dog and cow.';
            $walls is every wall;
            $group is new group with name '02.05.01' and description 'External walls';
            $building = new building with name 'Default building';
            Add $walls to $group;
            Add $walls to $building;
            Set 'Fire protection' to 'Great', 'Warranty date' to '12/05/2016' for $walls in property set 'Testing property set';
            
            $test1 is every wall where (description contains cat OR description contains cow) AND description contains dog;
            ");

            Assert.AreEqual(0, parser.Errors.Count());
            Assert.AreEqual(2, parser.Results["$test1"].Count());
        }

        [TestMethod]
        public void MaterialLayerCreationTest()
        {
            XbimQueryParser parser = new XbimQueryParser();

            parser.Parse(@"
                Create new material layer set 'My new material layer set' : Plaster 20.2, 'Brick block' 350, Insulation 300.0, 'Plaster board' 20;
            ");

            Assert.AreEqual(0, parser.Errors.Count);
            Assert.IsInstanceOfType(parser.Results["$$"].FirstOrDefault(), typeof(IfcMaterialLayerSet));
        }

        [TestMethod]
        public void MaterialSettingTest()
        {
            XbimQueryParser parser = new XbimQueryParser();
            parser.Parse(@"
                $materialLayerSet is new material layer set 'My new material layer set' : Plaster 20.2, 'Brick block' 350, Insulation 300.0, 'Plaster board' 20;
                $material is new material 'Gold';
                $wall is new wall 'My wall';
                $wallType is new walltype 'My wall type';
                $proxy is new buildingelementproxy 'Golden egg';
                Set material to $materialLayerSet for $wall;
                Set material to $materialLayerSet for $wallType;
                Set material to $material for $proxy;
            ");

            Assert.AreEqual(0, parser.Errors.Count);
            var wall = parser.Results["$wall"].FirstOrDefault() as IfcWall;
            var wallType = parser.Results["$wallType"].FirstOrDefault() as IfcWallType;
            var proxy = parser.Results["$proxy"].FirstOrDefault() as IfcBuildingElementProxy;
            var test1 = wall.GetMaterial();
            var test2 = wallType.GetMaterial();
            var test3 = proxy.GetMaterial();

            Assert.IsInstanceOfType(test1, typeof(IfcMaterialLayerSetUsage));
            Assert.IsInstanceOfType(test2, typeof(IfcMaterialLayerSet));
            Assert.IsInstanceOfType(test3, typeof(IfcMaterial));
        }

        [TestMethod]
        public void AddingAndRemovingToTypeTest()
        {
            XbimQueryParser parser = new XbimQueryParser();
            parser.Parse(@"
                Create new wall '1';
                Create new wall '1';
                Create new wall '1';
                Create new wall '2';
                Create new wall '2';
                $walls1 is every wall where name is '1';
                $walls2 is every wall where name is '2';
                
                $wallType is new walltype 'W type';
                $mSet = new material layer set 'Set of the dreams': Plaster 10, Bricks 300, Insulation 300, Plaster 10;
                Set material to $mSet for $wallType;
                
                Add $walls1 to $wallType;
                Add $walls2 to $wallType;
                
                Remove $walls1 from $wallType;
            ");

            Assert.AreEqual(0, parser.Errors.Count);

            var wall1 = parser.Results["$walls1"].FirstOrDefault() as IfcWall;
            var wall2 = parser.Results["$walls2"].FirstOrDefault() as IfcWall;
            var material1 = wall1.GetMaterial();
            var material2 = wall2.GetMaterial();
            var type1 = wall1.GetDefiningType();
            var type2 = wall2.GetDefiningType();

            Assert.IsNull(material1);
            Assert.IsNull(type1);
            Assert.IsNotNull(material2);
            Assert.IsNotNull(type2);
        }

        [TestMethod]
        public void ReferencedModelsTest()
        {
            XbimQueryParser parser = new XbimQueryParser();
            parser.Parse(@"
                Create new wall 'Wall 1, model 1';
                Create new wall 'Wall 2, model 1';
                Create new wall 'Wall 3, model 1';
                Create new wall 'Wall 4, model 1';
                Create new wall 'Wall 5, model 1';
                
                Save model to file 'TestModel1.xBIM';
                Close model;
                
                Create new wall 'Wall 1, model 2';
                Create new wall 'Wall 2, model 2';
                Create new wall 'Wall 3, model 2';
                Create new wall 'Wall 4, model 2';
                Create new wall 'Wall 5, model 2';
                
                Save model to file 'TestModel2.xBIM';
                Close model;
                
                Create new wall 'Wall 1, model 3';
                Create new wall 'Wall 2, model 3';
                Create new wall 'Wall 3, model 3';
                Create new wall 'Wall 4, model 3';
                Create new wall 'Wall 5, model 3';
                
                Save model to file 'TestModel3.xBIM';
                Close model;
                
                Add reference model 'TestModel1.xBIM' where organization is 'Organization A' and owner is 'Antoineta';
                Add reference model 'TestModel2.xBIM' where organization is 'Organization B' and owner is 'Bugatti';
                Add reference model 'TestModel3.xBIM' where organization is 'Organization C' and owner is 'Cecilia';

                $walls is every wall;

                $test1 is every wall where model is 'TestModel1';
                $test2 is every wall where model is 'TestModel1.xBIM';
                $test3 is every wall where model is 'testmodel1';
                $test4 is every wall where model owner is 'Cecilia';
                $test5 is every wall where model organization is 'Organization B';
            ");

            //test if everything was all right
            Assert.AreEqual(0, parser.Errors.Count);

            //test model conditions for referenced models
            Assert.AreEqual(0, parser.Errors.Count);
            Assert.AreEqual(5, parser.Results["$test1"].Count());
            Assert.AreEqual(5, parser.Results["$test2"].Count());
            Assert.AreEqual(5, parser.Results["$test3"].Count());
            Assert.AreEqual(5, parser.Results["$test4"].Count());
            Assert.AreEqual(5, parser.Results["$test5"].Count());

            //test existing referenced models
            Assert.AreEqual(15, parser.Results["$walls"].Count());

            //test number of federated models
            Assert.AreEqual(3, parser.Model.RefencedModels.Count);

            //test opening from IFC file
            parser.Parse(@"
                Save model to file 'Federation.ifc';
                Close model;
                Open model from file 'Federation.ifc';
                $walls is every wall;
            ");
            Assert.AreEqual(15, parser.Results["$walls"].Count());

            //test number of federated models
            Assert.AreEqual(3, parser.Model.RefencedModels.Count);

            //test opening from xBIM file
//            parser.Parse(@"
//                 Save model to file 'Federation.xBIM';
//                 Close model;
//                 Open model from file 'Federation.xBIM';
//                 $walls is every wall;
//            ");
//            Assert.AreEqual(15, parser.Results["$walls"].Count());
        }

        [TestMethod]
        public void EmbededScriptTest()
        {
            var parser = new XbimQueryParser();
            parser.Parse(@"
                $g1 is new group 'Group A';
                $g2 is new group 'Group B';
                $g3 is new group 'Group C';
                
                Add $g2 to $g1;
                Add $g3 to $g1;
                
                Set script to '$group is every wall;' for $g2;
                Set script to '$group is every slab;' for $g3;
                
                Create new wall 'My wall No.1';
                Create new wall 'My wall No.2';
                Create new wall 'My wall No.3';
                
                Create new slab 'My slab No.1';
                Create new slab 'My slab No.2';
                
                Save model to file 'EmbededScript.ifc';
            ");
            Assert.AreEqual(0, parser.Errors.Count());
            var g2 = parser.Model.Instances.Where<IfcGroup>(g => g.Name == "Group B").FirstOrDefault();
            var g3 = parser.Model.Instances.Where<IfcGroup>(g => g.Name == "Group C").FirstOrDefault();

            Assert.IsNotNull(g2);
            Assert.IsNotNull(g3);

            var walls = g2.ExecuteScript();
            var slabs = g3.ExecuteScript();

            Assert.AreEqual(3, walls.Count());
            Assert.AreEqual(2, slabs.Count());
        }
    }
}
