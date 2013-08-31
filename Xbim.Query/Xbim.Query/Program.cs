using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.IO;

namespace Xbim.Query
{
    class Program
    {
        static void Main(string[] args)
        {
            string source = @"
            select wall where name is 'my wall'; 
            create new group 'My group number one';
            $myGroup is new wall 'My wall no.1';
            ";
            //test scanner
            Scanner scanner = new Scanner();
            scanner.SetSource(source, 0);
            int val = scanner.yylex();
            while(val != (int)Tokens.EOF)
            {
                string name = val >= 60 ? Enum.GetName(typeof(Tokens), val) : ((char)val).ToString();
                Console.Write(name + " ");
                val = scanner.yylex();
            }


            //try parser with the same source
            scanner = new Scanner();
            scanner.SetSource(source, 0);

            XbimModel model = XbimModel.CreateTemporaryModel();
            
            using (XbimReadWriteTransaction txn = model.BeginTransaction("Model processing"))
            {
                XbimQueryParser parser = new XbimQueryParser(scanner, model);
                var success = parser.Parse();
                var groups = model.Instances.OfType<Xbim.Ifc2x3.Kernel.IfcGroup>();
                txn.Commit();
                
                Console.WriteLine();
                Console.WriteLine("Valid syntax: " + (scanner.Errors.Count == 0));

                foreach (var item in scanner.Errors)
                {
                    Console.WriteLine(item);
                }
            }
            
            
            
            
            

            Console.ReadKey();
            
        }
    }
}
