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
    [DeploymentItem(ExcelTemplateFile, Root)]
    [DeploymentItem(BinaryFile, Root)]
    [TestClass]
    public class COBieSerialisationTests
    {

        private const string Root = "TestSourceFiles";
        private const string ExcelTemplateLeaf = "COBie-US-2_4-template.xls";
        private const string SourceBinaryFile = "COBieToXbim.xCOBie";

        private const string ExcelTemplateFile = Root + @"\" + ExcelTemplateLeaf;
        private const string BinaryFile = Root + @"\" + SourceBinaryFile;

        static COBieWorkbook _book;
        
        [ClassInitialize]
        public static void LoadModel(TestContext context)
        {
            COBieBinaryDeserialiser deserialiser = new COBieBinaryDeserialiser(BinaryFile);
            _book = deserialiser.Deserialise();
        }

        [TestMethod]
        public void Should_Roundtrip_Serialise()
        {
            string output = Path.GetTempFileName();
            COBieBinarySerialiser serialiser = new COBieBinarySerialiser(output);
            serialiser.Serialise(_book);

            // Deserialiser into a new workbook.
            Stopwatch timer = new Stopwatch();
            timer.Start();
            COBieBinaryDeserialiser deserialiser = new COBieBinaryDeserialiser(output);
            COBieWorkbook newBook = deserialiser.Deserialise();
            timer.Stop();
            Debug.WriteLine(string.Format("COBieBinaryDeserialiser Time = {0}", timer.Elapsed.TotalSeconds.ToString()));
                                
            
            Assert.AreEqual(19, _book.Count);

        }

        [TestMethod]
        public void Should_Have_HashValue_Serialise()
        {
            string hashCode1, hashCode2;
            string output = Path.GetTempFileName();
            hashCode1 = _book[Constants.WORKSHEET_FACILITY][0].RowHashValue;
            Debug.WriteLine(string.Format("Serialised Row hash Value = {0}", hashCode1));
            string hashCodeSerial1 = _book[Constants.WORKSHEET_FACILITY][0].InitialRowHashValue;
            Debug.WriteLine(string.Format("Created Row Hash Value Was = {0}", hashCodeSerial1));
            // Act
            COBieBinarySerialiser serialiser = new COBieBinarySerialiser(output);
            serialiser.Serialise(_book);
            
            // Deserialiser into a new workbook.
            COBieBinaryDeserialiser deserialiser = new COBieBinaryDeserialiser(output);
            
            COBieWorkbook newBook = deserialiser.Deserialise();
            hashCode2 = newBook[Constants.WORKSHEET_FACILITY][0].RowHashValue;
            Debug.WriteLine(string.Format("Deserialised Row hash Value = {0}", hashCode2));
            string hashCodeSerial2 = newBook[Constants.WORKSHEET_FACILITY][0].InitialRowHashValue;
            Debug.WriteLine(string.Format("Created Row Hash Value Was = {0}", hashCodeSerial2));
            
            // Assert
            Assert.AreEqual(hashCode1, hashCode2);

            
        }

        [TestMethod]
        public void Should_Roundtrip_XLSSerialise()
        {
            
            string output = Path.ChangeExtension(SourceBinaryFile, ".xls");
            ICOBieSerialiser serialiser = new COBieXLSSerialiser(output, ExcelTemplateFile);
            serialiser.Serialise(_book, null);
            
            
            Stopwatch timer = new Stopwatch();
            timer.Start();
            //TEST on COBieXLSDeserialiser
            COBieXLSDeserialiser deSerialiser = new COBieXLSDeserialiser(output);
            COBieWorkbook newbook = deSerialiser.Deserialise();

            timer.Stop();
            Debug.WriteLine(string.Format("COBieXLSDeserialiser Time = {0}", timer.Elapsed.TotalSeconds.ToString()));

            string newOutputFile = Root + @"\" + "RoundTrip" + Path.ChangeExtension(SourceBinaryFile, ".xls"); 
            ICOBieSerialiser serialiserTest = new COBieXLSSerialiser(newOutputFile, ExcelTemplateFile);

            // Assert
            Assert.AreEqual(19, newbook.Count); //with picklist

            // 19 workbooks. # rows in a selection.
        }
    }
}
