using System;
using System.IO;
using System.Linq;
using Xbim.XbimExtensions;
using Xbim.IO;

namespace XbimConvert
{
    public class Params
    {

        public static Params ParseParams(string[] args)
        {
            Params result = new Params(args);

            return result;
        }

        private Params(string[] args)
        {
            

            if (args.Length < 1)
            {
                Console.WriteLine("Invalid number of Parameters, filename required");
                Console.WriteLine("Syntax: XbimConvert source [-quiet|-q] [-keepextension|-ke] [-filter|-f <elementid|elementtype>]");
                return;
            }

            IfcFileName = args[0];
            if (!File.Exists(IfcFileName))
            {
                Console.WriteLine("Invalid ifc filename {0}", IfcFileName);
                Console.Error.WriteLine("Invalid ifc filename {0}", IfcFileName);
                return;
            }

            CompoundParameter paramType = CompoundParameter.None;

            foreach(string arg in args.Skip(1))
            {
                switch (paramType)
                {
                    case CompoundParameter.None:

                        switch (arg.ToLowerInvariant())
                        {
                            case "-quiet":
                            case "-q":
                                IsQuiet = true;
                                break;

                            case "-keepextension":
                            case "-ke":
                                KeepFileExtension = true;
                                break;

                            case "-filter":
                            case "-f":
                                paramType = CompoundParameter.Filter;
                                // need to read next param
                                break;

                            default:
                                Console.WriteLine("Skipping un-expected argument '{0}'", arg);
                                break;
                        }
                        break;

                    case CompoundParameter.Filter:
                        // next arg will be either ID or Type

                        IfcType t;
                        if (IfcInstances.IfcTypeLookup.TryGetValue(arg.ToUpperInvariant(), out t) == true)
                        {
                            ElementTypeFilter = t;
                            FilterType = FilterType.ElementType;
                        }
                        else
                        {
                            // try looking for an instance by ID.
                            long elementId;
                            if (long.TryParse(arg, out elementId) == true)
                            {
                                ElementIdFilter = elementId;
                                FilterType = FilterType.ElementID;
                            }
                            else
                            {
                                // not numeric either
                                Console.WriteLine("Error: Invalid Filter parameter '{0}'", arg);
                                return;
                            }
                        }
                        break;
                }

            }

            // Parameters are valid
            IsValid = true;

        }

        public String IfcFileName { get; set; }
        public bool IsQuiet { get; set; }
        public bool KeepFileExtension { get; set; }
        public bool IsValid { get; set; }
        public FilterType FilterType { get; set; }
        public long ElementIdFilter {get;set;}
        public IfcType ElementTypeFilter { get; set; }

        private enum CompoundParameter
        {
            None,
            Filter
        };

        
    }

    public enum FilterType
    {
        None,
        ElementID,
        ElementType
    };
}
