using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions.Interfaces;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.Extensions;
using System.IO;
using Xbim.IO;

namespace Xbim.Query
{
    internal class SystemsCreator
    {
        private XbimModel _model;

        public void CreateSystem(XbimModel model, SYSTEM system)
        {
            //set model in which the systems are to be created
            _model = model;
            
            //get data
            string data = null;
            CsvLineParser parser = null;
            switch (system)
            {
                case SYSTEM.NRM:
                    if (model.Instances.Where<IfcSystem>(s => s.Name == "NRM").FirstOrDefault() != null) return;
                    data = Properties.Resources.NRM;
                    parser = new CsvLineParser(';');
                    break;
                case SYSTEM.UNICLASS:
                    if (model.Instances.Where<IfcSystem>(s => s.Name == "uniclass").FirstOrDefault() != null) return;
                    data = Properties.Resources.Uniclass;
                    parser = new CsvLineParser();
                    break;
                default:
                    break;
            }

            //process file
            if (_model.IsTransacting)
                ParseCSV(data, parser);
            else
                using (var txn = model.BeginTransaction("Systems creation"))
                {
                    ParseCSV(data, parser);
                    txn.Commit();
                }
        }

        private void ParseCSV(string data, CsvLineParser parser)
        {
            TextReader csvReader = new StringReader(data);

            //header line
            string line = csvReader.ReadLine();
            CsvLineParser lineParser = parser;
            lineParser.ParseHeader(line);

            //get first line of data
            line = csvReader.ReadLine();

            while (line != null)
            {
                //parse line
                Line parsedLine = lineParser.ParseLine(line);

                //create IFC object
                IfcSystem system = GetOrCreateSystem(parsedLine.Code, parsedLine.Description);
                if (system == null) continue;

                //set up hierarchy
                IfcSystem parentSystem = GetOrCreateSystem(parsedLine.ParentCode);
                if (parentSystem != null) parentSystem.AddObjectToGroup(system);

                //read new line to be processed in the next step
                line = csvReader.ReadLine();
            }
        }


        private IfcSystem GetOrCreateSystem(string name, string description = null)
        {
            if (name == null) return null;

            IfcSystem system = _model.Instances.Where<IfcSystem>(s => s.Name == name).FirstOrDefault();
            if (system == null)
                system = _model.Instances.New<IfcSystem>(s =>
                {
                    s.Name = name;
                });
            if (description != null) system.Description = description;
            return system;
        }


        private class CsvLineParser
        {
            //settings
            private char   _separator = ',';
            private string _CodeFieldName = "Code";
            private string _DescriptionFieldName = "Description";
            private string _ParentCodeFieldName = "Parent";

            //header used to parse the file
            private string[] header;
            //get index
            private int codeIndex;
            private int descriptionIndex;
            private int parentCodeIndex;

            public CsvLineParser(char separator = ',', string codeField = "Code", string descriptionField = "Description", string parentField = "Parent")
            {
                _separator = separator;
                _CodeFieldName = codeField;
                _DescriptionFieldName = descriptionField;
                _ParentCodeFieldName = parentField;
            }

            public void ParseHeader(string headerLine)
            {
                //create header of the file
                string line = headerLine.ToLower();
                line = line.Replace("\"", "");
                header = headerLine.Split(_separator);

                //get indexes of the fields
                codeIndex = header.ToList().IndexOf(_CodeFieldName);
                descriptionIndex = header.ToList().IndexOf(_DescriptionFieldName);
                parentCodeIndex = header.ToList().IndexOf(_ParentCodeFieldName);

                if (codeIndex < 0) throw new Exception("File is either not CSV file or it doesn't comply to the predefined structure.");
            }

            public Line ParseLine(string line)
            {
                Line result = new Line();
                line = line.Replace("\"", "");
                string[] fields = line.Split(_separator);

                //get data
                if (codeIndex >= 0) result.Code = fields[codeIndex];
                if (descriptionIndex >= 0) result.Description = fields[descriptionIndex];
                if (parentCodeIndex >= 0) result.ParentCode = fields[parentCodeIndex];

                return result;
            }
        }

        private struct Line
        {
            public string Code;
            public string Description;
            public string ParentCode;
        }
    }

    internal enum SYSTEM
    {
        NRM,
        UNICLASS
    }
}
