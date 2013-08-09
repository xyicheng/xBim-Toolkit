using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Xbim.IO;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.XbimExtensions.Interfaces;
using Xbim.Ifc2x3.Extensions;

namespace Xbim.ModelEnhancement
{
    /// <summary>
    /// This class will parse CSV file containing definition of the classification
    /// and will create hierarchy of IfcSystem in the model.
    /// </summary>
    public class SystemsFromCSV
    {
        private XbimModel _model;

        public SystemsFromCSV(XbimModel model)
        {
            _model = model;
        }

        public void ParseCSVFile(string fileName)
        {
            if (!File.Exists(fileName))
                throw new ArgumentException("File " + fileName + "doesn't exist");

            var csv = File.OpenRead(fileName);
            TextReader csvReader = new StreamReader(csv);

            //header line
            string line = csvReader.ReadLine();
            CsvLineParser lineParser = new CsvLineParser(line);
            //get first line of data
            line = csvReader.ReadLine();

            while (line != null)
            {
                //parse line
                Line parsedLine = lineParser.ParseLine(line);

                //create IFC object
                IfcSystem system = GetOrCreateSystem(parsedLine.Code);
                if (system == null) continue;

                //set up hierarchy
                IfcSystem parentSystem = GetOrCreateSystem(parsedLine.ParentCode);
                if (parentSystem != null) parentSystem.AddObjectToGroup(system);

                //read new line to be processed in the next step
                line = csvReader.ReadLine();
            }
        }


        private IfcSystem GetOrCreateSystem(string name)
        {
            if (name == null) return null;

            IfcSystem system = _model.Instances.Where<IfcSystem>(s => s.Name == name).FirstOrDefault();
            if (system == null)
                system = _model.Instances.New<IfcSystem>(s => s.Name = name);
            return system;
        }
        

        private class CsvLineParser
        {
            //settings
            public char separator = ',';
            public string CodeFieldName = "Code";
            public string DescriptionFieldName = "Description";
            public string ParentCodeFieldName = "Parent";

            //header used to parse the file
            private string[] header;
            //get index
            private int codeIndex;
            private int descriptionIndex;
            private int parentCodeIndex;

            public CsvLineParser(string headerLine)
            {
                //create header of the file
                string line = headerLine.ToLower();
                line = line.Replace("\"", "");
                header = headerLine.Split(separator);

                //get indexes of the fields
                codeIndex = header.ToList().IndexOf(CodeFieldName);
                descriptionIndex = header.ToList().IndexOf(DescriptionFieldName);
                parentCodeIndex = header.ToList().IndexOf(ParentCodeFieldName);

                if (codeIndex < 0) throw new Exception("File is either not CSV file or it doesn't comply to the predefined structure.");
            }

            public Line ParseLine(string line)
            {
                Line result = new Line();
                line = line.Replace("\"", "");
                string[] fields = line.Split(separator);

                //get data
                if (codeIndex >= 0) result.Code = fields[codeIndex];
                if (descriptionIndex >= 0) result.Description = fields[descriptionIndex];
                if (parentCodeIndex >= 0) result.ParentCode = fields[parentCodeIndex];

                return result;
            }
        }

        private struct Line {
            public string Code;
            public string Description;
            public string ParentCode;
        }
    }
}
