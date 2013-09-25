using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.IO;
using Xbim.XbimExtensions.Interfaces;
using System.Linq.Expressions;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.Extensions;
using System.IO;

namespace Xbim.Script
{
    class Program
    {
        static void Main(string[] args)
        {
            var testModelPath = @"D:\CODE\xBIM\XbimFramework\Dev\Martin\XbimQueryTest\TestModels\Standard Classroom.ifc";
            var isTestModel = File.Exists(testModelPath);

            XbimModel model = XbimModel.CreateTemporaryModel();
            XbimQueryParser parser = new XbimQueryParser(model);

            if (args.Count() > 0)
            {
                Stream reader = File.Open(args[0], FileMode.Open, FileAccess.Read);
                parser.Parse(reader);

                if (parser.Errors.Count() == 0)
                    Console.WriteLine("Input file processed");
                else
                {
                    Console.WriteLine("Input file processed with errors");
                    FileStream errFile = File.Open("Errors.txt", FileMode.OpenOrCreate, FileAccess.Write);
                    var errLog = new StreamWriter(errFile);
                    foreach (var err in parser.Errors)
                    {
                        Console.WriteLine(err);
                        errLog.WriteLine(err);
                    }
                    errLog.Close();
                }

            }
            

            Console.WriteLine("Enter your commands sir (use 'exit' command to quit):");
            Console.WriteLine();

            string command = Console.ReadLine();
            while (command.ToLower() != "exit")
            {
                if (command == "X" && isTestModel)
                {
                    Console.WriteLine("Opening model...");
                    model.CreateFrom(testModelPath, null, null, true);
                    Console.WriteLine("Model ready...");
                }
                parser.Parse(command);
                if (parser.Errors.Count() != 0)
                    foreach (var error in parser.Errors)
                    {
                        Console.WriteLine(error);
                    }
                command = Console.ReadLine();
            }

            model.Close();
        }

        //public static void PropertySelectionTest()
        //{
        //    //create model and sample data
        //    XbimModel model = XbimModel.CreateTemporaryModel();
        //    using (var txn = model.BeginTransaction())
        //    {
        //        var w1 = model.Instances.New<IfcWall>(w => w.Name = "Wall No.1");
        //        var w2 = model.Instances.New<IfcWall>(w => w.Name = "Wall No.2");
        //        var w3 = model.Instances.New<IfcWall>(w => w.Name = "Wall No.3");

        //        w1.SetPropertySingleValue("Test set 1", "String value", new IfcLabel("some string for wall 1"));
        //        w1.SetPropertySingleValue("Test set 1", "Double value", new IfcLengthMeasure(156.32));
        //        w1.SetPropertySingleValue("Test set 1", "Identifier value", new IfcIdentifier("identifier value 123sdfds8sfads58sdf"));
        //        w1.SetPropertySingleValue("Test set 1", "Integer value", new IfcInteger(235));
        //        w1.SetPropertySingleValue("Test set 1", "Bool value", new IfcBoolean(true));
        //        //null property value
        //        w1.SetPropertySingleValue("Test set 1", "Null value", typeof(IfcLabel));
        //        var nulProp = w1.GetPropertySingleValue("Test set 1", "Null value");
        //        nulProp.NominalValue = null;

        //        w2.SetPropertySingleValue("Test set 1", "String value", new IfcLabel("some string for wall 2"));
        //        w2.SetPropertySingleValue("Test set 1", "Double value", new IfcLengthMeasure(7856.32));
        //        w2.SetPropertySingleValue("Test set 1", "Identifier value", new IfcIdentifier("identifier value 123sdfds8sfads58sdf"));
        //        w2.SetPropertySingleValue("Test set 1", "Integer value", new IfcInteger(735));
        //        w2.SetPropertySingleValue("Test set 1", "Bool value", new IfcBoolean(true));
        //        //null property value
        //        w2.SetPropertySingleValue("Test set 1", "Null value", typeof(IfcLabel));
        //        var nulProp2 = w2.GetPropertySingleValue("Test set 1", "Null value");
        //        nulProp2.NominalValue = null;

        //        w3.SetPropertySingleValue("Test set 1", "String value", new IfcLabel("some string for wall 3"));
        //        w3.SetPropertySingleValue("Test set 1", "Double value", new IfcLengthMeasure(6.32));
        //        w3.SetPropertySingleValue("Test set 1", "Identifier value", new IfcIdentifier("identifier value 123sdfds8sfads58sdf"));
        //        w3.SetPropertySingleValue("Test set 1", "Integer value", new IfcInteger(291));
        //        w3.SetPropertySingleValue("Test set 1", "Bool value", new IfcBoolean(false));
        //        //null property value
        //        w3.SetPropertySingleValue("Test set 1", "Null value", typeof(IfcLabel));
        //        var nulProp3 = w3.GetPropertySingleValue("Test set 1", "Null value");
        //        nulProp3.NominalValue = null;

        //        txn.Commit();
        //    }

        //    //create parser and perform the test
        //    XbimQueryParser parser = new XbimQueryParser(model);
        //    parser.Parse("Select wall where 'integer value' is 330.25;");
        //    var count = parser.Results["$$"].Count();
        //}

//        string source = @"
//            Select wall where 'Heat Performance' ~ 'substring';
//            ";//where name is 'New wall'
//            //test scanner
//            Scanner scanner = new Scanner();
//            scanner.SetSource(source, 0);
//            int val = scanner.yylex();
//            while(val != (int)Tokens.EOF)
//            {
//                string name = val >= 60 ? Enum.GetName(typeof(Tokens), val) : ((char)val).ToString();
//                Console.Write(name + " ");
//                if (name == ";") Console.WriteLine();

//                val = scanner.yylex();
//            }


//            //try parser with the same source
//            scanner = new Scanner();
//            scanner.SetSource(source, 0);

//            XbimModel model = XbimModel.CreateTemporaryModel();
//            IfcWall wall = null;

//            using (XbimReadWriteTransaction txn = model.BeginTransaction("Model processing"))
//            {
//                wall = model.Instances.New<IfcWall>(w => w.Name = "Wall No. 1");
//                model.Instances.New<IfcWall>(w => w.Name = "New wall");
//                model.Instances.New<IfcWall>(w => w.Name = "New wall");
//                model.Instances.New<IfcWall>(w => w.Name = "New wall");
//                model.Instances.New<IfcWall>(w => w.Name = "New wall");

//                Parser parser = new Parser(scanner, model);
//                var success = parser.Parse();
//                var groups = model.Instances.OfType<Xbim.Ifc2x3.Kernel.IfcGroup>();
//                txn.Commit();
                
//                Console.WriteLine();
//                Console.WriteLine("Valid syntax: " + (scanner.Errors.Count == 0));

//                foreach (var item in scanner.Errors)
//                {
//                    Console.WriteLine(item);
//                }

//                var ent = parser.Variables["$$"].Count();
//            }

//            //PropertySelectionTest();

//            Console.ReadKey();

    }
}
