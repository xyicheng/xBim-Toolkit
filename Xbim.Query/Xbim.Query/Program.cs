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

namespace Xbim.Query
{
    class Program
    {
        static void Main(string[] args)
        {
            string source = @"
            Select wall where 'integer value' is 291.25;
            ";//where name is 'New wall'
            //test scanner
            Scanner scanner = new Scanner();
            scanner.SetSource(source, 0);
            int val = scanner.yylex();
            while(val != (int)Tokens.EOF)
            {
                string name = val >= 60 ? Enum.GetName(typeof(Tokens), val) : ((char)val).ToString();
                Console.Write(name + " ");
                if (name == ";") Console.WriteLine();

                val = scanner.yylex();
            }


            //try parser with the same source
            scanner = new Scanner();
            scanner.SetSource(source, 0);

            XbimModel model = XbimModel.CreateTemporaryModel();
            IfcWall wall = null;

            using (XbimReadWriteTransaction txn = model.BeginTransaction("Model processing"))
            {
                wall = model.Instances.New<IfcWall>(w => w.Name = "Wall No. 1");
                model.Instances.New<IfcWall>(w => w.Name = "New wall");
                model.Instances.New<IfcWall>(w => w.Name = "New wall");
                model.Instances.New<IfcWall>(w => w.Name = "New wall");
                model.Instances.New<IfcWall>(w => w.Name = "New wall");

                Parser parser = new Parser(scanner, model);
                var success = parser.Parse();
                var groups = model.Instances.OfType<Xbim.Ifc2x3.Kernel.IfcGroup>();
                txn.Commit();
                
                Console.WriteLine();
                Console.WriteLine("Valid syntax: " + (scanner.Errors.Count == 0));

                foreach (var item in scanner.Errors)
                {
                    Console.WriteLine(item);
                }

                var ent = parser.Variables["$$"].Count();
            }

            Console.ReadKey();
            
        }


    }
}
