using System;
using System.IO;
using System.Linq;
using Xbim.XbimExtensions;
using Xbim.IO;
using System.Globalization;
using Xbim.ModelGeometry.Converter;

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
                Console.WriteLine("Syntax: XbimConvert source [-quiet|-q] [-generatescene|-gs[:options]] [-nogeometry|-ng] [-keepextension|-ke] [-filter|-f <elementid|elementtype>] [-sanitiselog] [-occ]");
                Console.Write("-generatescene options are: ");
                foreach (var i in Enum.GetValues(typeof(GenerateSceneOption)))
                    Console.Write(" " + i.ToString());
                return;
            }
            specdir = Path.GetDirectoryName(args[0]);
            if (specdir == "")
                specdir = Directory.GetCurrentDirectory();
            specpart = Path.GetFileName(args[0]);

            GenerateSceneOptions = 
                        GenerateSceneOption.IncludeRegions |
                        GenerateSceneOption.IncludeStoreys |
                        GenerateSceneOption.IncludeSpaces;

            CompoundParameter paramType = CompoundParameter.None;
            foreach (string arg in args.Skip(1))
            {

                switch (paramType)
                {
                    case CompoundParameter.None:
                        string[] argNames = arg.ToLowerInvariant().Split(new string[] { ":" }, StringSplitOptions.RemoveEmptyEntries);
                        switch (argNames[0])
                        {
                            case "-quiet":
                            case "-q":
                                IsQuiet = true;
                                break;

                            case "-keepextension":
                            case "-ke":
                                KeepFileExtension = true;
                                break;
                            case "-generatescene":
                            case "-gs":
                                GenerateScene = true;

                                if (argNames.Length > 1)
                                {
                                    foreach (var i in Enum.GetValues(typeof(GenerateSceneOption)))
                                    {
                                        if (CultureInfo.CurrentCulture.CompareInfo.IndexOf((string)argNames[1], i.ToString(), CompareOptions.IgnoreCase) >= 0)
                                        {
                                            GenerateSceneOptions = GenerateSceneOptions | (GenerateSceneOption)i;
                                        }
                                    }
                                }
                                break;
                            case "-nogeometry":
                            case "-ng":
                                NoGeometry = true;
                                break;
                            case "-filter":
                            case "-f":
                                paramType = CompoundParameter.Filter;
                                // need to read next param
                                break;

                            case "-sanitiselog":
                                SanitiseLogs = true;
                                break;
                            case "-occ":
                                OCC = true;
                                break;
                            default:
                                Console.WriteLine("Skipping un-expected argument '{0}'", arg);
                                break;
                        }
                        break;

                    case CompoundParameter.Filter:
                        // next arg will be either ID or Type

                        IfcType t;
                        if (IfcMetaData.TryGetIfcType(arg.ToUpperInvariant(), out t) == true)
                        {
                            ElementTypeFilter = t;
                            FilterType = FilterType.ElementType;
                        }
                        else
                        {
                            // try looking for an instance by ID.
                            int elementId;
                            if (int.TryParse(arg, out elementId) == true)
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

        public string specdir { get; set; }
        public string specpart { get; set; }
        public bool IsQuiet { get; set; }
        public bool KeepFileExtension { get; set; }
        public bool GenerateScene { get; set; }
        public GenerateSceneOption GenerateSceneOptions { get; set; }
        
        public bool NoGeometry { get; set; }
        public bool IsValid { get; set; }
        public FilterType FilterType { get; set; }
        public int ElementIdFilter {get;set;}
        public IfcType ElementTypeFilter { get; set; }
        public bool OCC { get; set; }
        /// <summary>
        /// Indicates that logs should not contain sensitive path information.
        /// </summary>
        public bool SanitiseLogs { get; set; }

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
