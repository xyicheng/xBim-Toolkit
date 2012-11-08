using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie;
using Xbim.IO;
using Xbim.XbimExtensions;
using Xbim.COBie.Serialisers;
using System.IO;
using System.Diagnostics;

namespace COBie.Xbim.Tester
{
    class Program
    {
        private static string _sourceFile = "Clinic-Handover.xbim";
        private static string _testFile = "COBieToXbim.xCOBie";
        private static string _templateFile = "COBie-UK-2012-template.xls";

        static void Main(string[] args)
        {
            COBieWorkbook workBook;
            if (!File.Exists(_testFile))
            {
                COBieContext context = new COBieContext(null);
                IModel model = new XbimFileModelServer();
                model.Open(_sourceFile, delegate(int percentProgress, object userState)
                {
                    Console.Write("\rReading File {1} {0}", percentProgress, _sourceFile);
                });
                context.Model = model;
                context.TemplateFileName = _templateFile;
                COBieBuilder builder = new COBieBuilder(context);
                workBook = builder.Workbook;
                COBieBinarySerialiser serialiser = new COBieBinarySerialiser(_testFile);
                serialiser.Serialise(workBook);
            }
            else
            {
                COBieBinaryDeserialiser deserialiser = new COBieBinaryDeserialiser(_testFile);
                workBook = deserialiser.Deserialise();
            }
            Stopwatch sWatch = new Stopwatch();
            sWatch.Start();
            
            COBieXBimSerialiser xBimSerialiser = new COBieXBimSerialiser();
            xBimSerialiser.Serialise(workBook);

            sWatch.Stop();
            Console.WriteLine("Time = {0}", sWatch.Elapsed.Seconds);

            Console.WriteLine("Press any key...");
            Console.ReadKey();
        }

        
    }
}
