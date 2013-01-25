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
using Xbim.Ifc2x3.UtilityResource;
using Xbim.ModelGeometry.Scene;
using Xbim.Ifc2x3.Kernel;
using Xbim.ModelGeometry;
using Xbim.COBie.Serialisers.XbimSerialiser;
//using Xbim.IO.Tree;

namespace Xbim.Tests.COBie
{
    [DeploymentItem(ExcelTemplateFile, Root)]
    [DeploymentItem(DuplexFile, Root)]
    [DeploymentItem(DuplexBinaryFile, Root)]
    [DeploymentItem(DLLFiles)]
    [TestClass]
    public class COBieXbimSerialisationTests
    {
        private const string Root = "TestSourceFiles";

        private const string ExcelTemplateLeaf = "COBie-US-2_4-template.xls";
        private const string DuplexModelLeaf = "Duplex_A_Co-ord.xbim"; 
        private const string DuplexBinaryLeaf = "DuplexCOBieToXbim.xCOBie";
        
        private const string ExcelTemplateFile = Root + @"\" + ExcelTemplateLeaf;
        private const string DuplexFile = Root + @"\" + DuplexModelLeaf;
        private const string DuplexBinaryFile = Root + @"\" + DuplexBinaryLeaf;
        
        private const string DLLFiles = @"C:\Xbim\XbimFramework\Dev\COBie\Xbim.ModelGeometry\OpenCascade\Win32\Bin";

        /// <summary>
        /// Test to Create Binary file from workbook 
        /// </summary>
        [TestMethod]
        public void Should_BinarySerialiser()
        {
            COBieWorkbook workBook;
            COBieContext context;
            COBieBuilder builder;


            context = new COBieContext(null);
            context.TemplateFileName = ExcelTemplateFile;

            using (XbimModel model = new XbimModel())
            {
                model.Open(DuplexFile, XbimDBAccess.ReadWrite, delegate(int percentProgress, object userState)
                {
                    Console.Write("\rReading File {1} {0}", percentProgress, DuplexFile);
                });
                context.Model = model;


                builder = new COBieBuilder(context);
                workBook = builder.Workbook;
                COBieBinarySerialiser serialiser = new COBieBinarySerialiser(DuplexBinaryFile);
                serialiser.Serialise(workBook);

            }
            double bytes = 0;
            if (File.Exists(DuplexBinaryFile))
            {
                FileInfo fileInfo = new FileInfo(DuplexBinaryFile);
                bytes = fileInfo.Length;
            }

            Assert.IsTrue(bytes == 1944935);

        }

        /// <summary>
        /// Test to create Ifc File from Workbook
        /// </summary>
        [TestMethod]
        public void Should_XBimSerialise()
        {
            COBieWorkbook workBook;
            COBieContext context;
            COBieBuilder builder;
            COBieWorkbook book;

            COBieBinaryDeserialiser deserialiser = new COBieBinaryDeserialiser(DuplexBinaryFile);
            workBook = deserialiser.Deserialise();

            using (COBieXBimSerialiser xBimSerialiser = new COBieXBimSerialiser(Path.ChangeExtension(DuplexBinaryFile, ".Ifc")))//Path.ChangeExtension(Path.GetFullPath(BinaryFile), ".Ifc")
            {
                xBimSerialiser.Serialise(workBook);

                context = new COBieContext(null);
                context.TemplateFileName = ExcelTemplateFile;
                context.Model = xBimSerialiser.Model;

                builder = new COBieBuilder(context);
                book = builder.Workbook;
            }

            // Assert
            Assert.AreEqual(19, book.Count);
            
        }

        /// <summary>
        /// Test on Delimited Strings
        /// </summary>
        [TestMethod]
        public void Delimited_Strings()
        {
            string test1 = "This is split : here and , here and : again";
            string test2 = "This is not split";
            string test3 = "This is also not split";
            string test4 = "This is split , here and , here and , again";
            List<string> strList = new List<string>() { test1, test2, test3, test4 };

            string delimited = COBieXBim.JoinStrings(':', strList);
            List<string> delimitesStrings = COBieXBim.SplitString(delimited, ':');

            Debug.WriteLine(string.Format("\"{0}\"", delimited));
            Debug.WriteLine(string.Format("\"{0}\" \"{1}\"", test1, delimitesStrings[0]));
            Debug.WriteLine(string.Format("\"{0}\" \"{1}\"", test2, delimitesStrings[1]));
            Debug.WriteLine(string.Format("\"{0}\" \"{1}\"", test3, delimitesStrings[2]));
            Debug.WriteLine(string.Format("\"{0}\" \"{1}\"", test4, delimitesStrings[3]));

            Assert.AreEqual(test1, delimitesStrings[0]);
            Assert.AreEqual(test2, delimitesStrings[1]);
            Assert.AreEqual(test3, delimitesStrings[2]);
            Assert.AreEqual(test4, delimitesStrings[3]);
            
        }
        
        
    }
}
