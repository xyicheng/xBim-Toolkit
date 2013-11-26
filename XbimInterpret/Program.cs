using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Linq.Expressions;
using System.IO;
using Xbim.Script;
using Xbim.IO;
using Xbim.XbimExtensions.Interfaces;

namespace XbimInterpret
{
    class Program
    {
        static void Main(string[] args)
        {
            var testModelPath = @"G:\CODE\xBIM\Martin\XbimQueryTest\TestModels\Standard Classroom.ifc";
            var isTestModel = File.Exists(testModelPath);

            XbimQueryParser parser = new XbimQueryParser();

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
                    Console.ReadKey();
                }
                return;
            }


            Console.WriteLine("Enter your commands (use 'exit' command to quit):");
            Console.Write(">>> ");
            string command = Console.ReadLine();
            IEnumerable<IPersistIfcEntity> lastResults = null;

            while (command.ToLower() != "exit")
            {
                if (command == "X" && isTestModel)
                {
                    Console.WriteLine("Opening model...");
                    parser.Model.CreateFrom(testModelPath, null, null, true);
                    Console.WriteLine("Model ready...");
                }
                parser.Parse(command);
                if (parser.Errors.Count() != 0)
                    foreach (var error in parser.Errors)
                        Console.WriteLine(error);

                if (lastResults != parser.Results.LastEntities)
                {
                    lastResults = parser.Results.LastEntities;
                    if (lastResults != null)
                        parser.Parse("Dump " + parser.Results.LastVariable + ";");
                }


                Console.Write(">>> ");
                command = Console.ReadLine();
            }

            if (parser.Model != null) parser.Model.Close();
        }
    }
}
