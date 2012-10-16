using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.COBie;
using Xbim.XbimExtensions;
using Xbim.IO;
using Xbim.COBie.Serialisers;
using System.IO;
using System.Diagnostics;
using XBim.COBie.Client.Formatters;

namespace Xbim.Tests.COBie
{
    [DeploymentItem(SourceFile, Root)]
    [DeploymentItem(PickListFile, Root)]
    [DeploymentItem(ExcelTemplateFile, Root)]
    [TestClass]
    public class COBieSerialisationTests
    {

        private const string Root = "TestSourceFiles";
        private const string SourceModelLeaf = "Clinic-Handover.xbim"; //"Clinic_A.xbim";//"2012-03-23-Duplex-Handover.xbim";
        private const string PickListLeaf = "PickLists.xml";
        private const string ExcelTemplateLeaf = "COBie-US-2_4-template.xls";

        private const string SourceFile = Root + @"\" + SourceModelLeaf;
        private const string PickListFile = Root + @"\" + PickListLeaf;
        private const string ExcelTemplateFile = Root + @"\" + ExcelTemplateLeaf;

        [TestMethod]
        public void Should_Roundtrip_Serialise()
        {
            // Arrange

            COBieContext context = new COBieContext(null);
            IModel model = new XbimFileModelServer();
            model.Open(SourceFile);
            context.Model = model ;
            context.TemplateFileName = ExcelTemplateFile;
            
            COBieBuilder builder = new COBieBuilder(context);

            COBieWorkbook book = builder.Workbook;
            // Act
            string output = Path.GetTempFileName();
            COBieBinarySerialiser serialiser = new COBieBinarySerialiser(output);

            serialiser.Serialise(book);

            // Deserialiser into a new workbook.
            Stopwatch timer = new Stopwatch();
            timer.Start();
            COBieBinaryDeserialiser deserialiser = new COBieBinaryDeserialiser(output);
            COBieWorkbook newBook = deserialiser.Deserialise();
            timer.Stop();
            Debug.WriteLine(string.Format("COBieBinaryDeserialiser Time = {0}", timer.Elapsed.TotalSeconds.ToString()));

            //create excel file
            //string excelFile = Path.ChangeExtension(output, ".xls");
            //ICOBieFormatter formatter = new XLSFormatter(excelFile, ExcelTemplateFile);
            //builder.Export(formatter);
            //Process.Start(excelFile);

            // Assert
            Assert.AreEqual(19, newBook.Count);

            // 19 workbooks. # rows in a selection.
        }
    }
}
