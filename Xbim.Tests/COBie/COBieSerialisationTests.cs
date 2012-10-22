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
using Xbim.COBie.Contracts;

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

        [TestMethod]
        public void Should_Have_HashValue_Serialise()
        {
            // Arrange

            COBieContext context = new COBieContext(null);
            IModel model = new XbimFileModelServer();
            model.Open(SourceFile);
            context.Model = model;
            context.TemplateFileName = ExcelTemplateFile;

            COBieBuilder builder = new COBieBuilder(context);

            COBieWorkbook book = builder.Workbook;
            string hashCode1 = book[Constants.WORKSHEET_FACILITY][0].RowHashValue;
            Debug.WriteLine(string.Format("Serialised Row hash Value = {0}", hashCode1));
            string hashCodeSerial1 = book[Constants.WORKSHEET_FACILITY][0].InitialRowHashValue;
            Debug.WriteLine(string.Format("Created Row Hash Value Was = {0}", hashCodeSerial1));
            // Act
            string output = Path.GetTempFileName();
            COBieBinarySerialiser serialiser = new COBieBinarySerialiser(output);
            serialiser.Serialise(book);

            // Deserialiser into a new workbook.
            COBieBinaryDeserialiser deserialiser = new COBieBinaryDeserialiser(output);
            COBieWorkbook newBook = deserialiser.Deserialise();
           // newBook[Constants.WORKSHEET_FACILITY][0][0] = new COBieCell("TTTTT");
            string hashCode2 = newBook[Constants.WORKSHEET_FACILITY][0].RowHashValue;
            Debug.WriteLine(string.Format("Deserialised Row hash Value = {0}", hashCode2));
            string hashCodeSerial2 = book[Constants.WORKSHEET_FACILITY][0].InitialRowHashValue;
            Debug.WriteLine(string.Format("Created Row Hash Value Was = {0}", hashCodeSerial2));
            // Assert
            Assert.AreEqual(hashCode1, hashCode2);

            
        }

        [TestMethod]
        public void Should_Roundtrip_XLSSerialise()
        {
            // Arrange

            COBieContext context = new COBieContext(null);
            IModel model = new XbimFileModelServer();
            model.Open(SourceFile);
            context.Model = model;
            context.TemplateFileName = ExcelTemplateFile;

            COBieBuilder builder = new COBieBuilder(context);

            COBieWorkbook book = builder.Workbook;
            // Act
            string output = Path.ChangeExtension(SourceFile, ".xls");

            ICOBieSerialiser serialiser = new COBieXLSSerialiser(output, ExcelTemplateFile);
            ICOBieSheet<COBieRow> PickList = book.Where(wb => wb.SheetName == "PickLists").FirstOrDefault();
            if (PickList != null)
                book.Remove(PickList);
            serialiser.Serialise(book);
            
            
            
            Stopwatch timer = new Stopwatch();
            timer.Start();
            //TEST on COBieXLSDeserialiser
            COBieXLSDeserialiser deSerialiser = new COBieXLSDeserialiser(output);
            COBieWorkbook newbook = deSerialiser.Deserialise();
            timer.Stop();
            Debug.WriteLine(string.Format("COBieXLSDeserialiser Time = {0}", timer.Elapsed.TotalSeconds.ToString()));

            string newOutputFile = Root + @"\" + "RoundTrip" + Path.ChangeExtension(SourceModelLeaf, ".xls"); 
            ICOBieSerialiser serialiserTest = new COBieXLSSerialiser(newOutputFile, ExcelTemplateFile);
            //remove the pick list sheet
            PickList = newbook.Where(wb => wb.SheetName == "PickLists").FirstOrDefault();
            if (PickList != null)
                newbook.Remove(PickList);
            serialiserTest.Serialise(newbook);

            Process.Start(output);
            Process.Start(newOutputFile);

            // Assert
            Assert.AreEqual(18, newbook.Count); //no picklist

            // 19 workbooks. # rows in a selection.
        }
    }
}
