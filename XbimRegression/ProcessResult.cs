using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XbimRegression
{
    /// <summary>
    /// Class summarising the results of a model conversion
    /// </summary>
    public class ProcessResult
    {
        public String FileName { get; set; }
        public int ExitCode { get; set; }
        public long Duration { get; set; }

        public long XbimLength { get; set; }
        public long XbimGCLength { get; set; }
        public long IfcLength { get; set; }

        public long Entities { get; set; }
        public long GeometryEntries { get; set; }

        public String IfcSchema { get; set; }
        public String IfcName { get; set; }
        public String IfcDescription { get; set; }


        public const String CsvHeader = @"IFC File, Conversion Errors, Duration (ms), IFC Size, Xbim Size, Xbim GC Size, IFC Entities, Geometry Nodes, "+
            "FILE_SCHEMA, FILE_NAME, FILE_DESCRIPTION";

        public String ToCsv()
        {
            return String.Format("\"{0}\",{1},{2},{3},{4},{5},{6},{7},\"{8}\",\"{9}\",\"{10}\"",
                FileName,           // 0
                ExitCode,           // 1
                Duration,           // 2
                IfcLength,          // 3
                XbimLength,         // 4
                XbimGCLength,       // 5
                Entities,           // 6
                GeometryEntries,    // 7
                IfcSchema,          // 8
                IfcName,            // 9
                IfcDescription      //10
                );
        }
    }
}
