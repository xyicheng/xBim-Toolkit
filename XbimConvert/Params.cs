using System;
using System.IO;
using System.Linq;

namespace XbimConvert
{
    public class Params
    {

        public static Params ParseParams(string[] args)
        {
            Params result = new Params();

            if (args.Length < 1)
            {
                Console.WriteLine("Invalid number of Parameters, filename required");
                Console.WriteLine("Syntax: ConvertToXbim source [-quiet]");
                return result;
            }

            result.IfcFileName = args[0];
            if (!File.Exists(result.IfcFileName))
            {
                Console.WriteLine("Invalid ifc filename {0}", result.IfcFileName);
                Console.Error.WriteLine("Invalid ifc filename {0}", result.IfcFileName);
                return result;
            }

            foreach(string arg in args.Skip(1))
            {
                switch(arg.ToLowerInvariant())
                {
                    case "-quiet":
                        result.IsQuiet = true;
                        break;

                    default:
                        Console.WriteLine("Skipping un-expected argument '{0}'", arg);
                        break;
                }
            }

            result.IsValid = true;

            return result;
        }

        public String IfcFileName { get; set; }
        public bool IsQuiet { get; set; }
        public bool IsValid { get; set; }
    }
}
