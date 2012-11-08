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
using Xbim.Ifc.UtilityResource;

namespace Xbim.Tests.COBie
{
    [DeploymentItem(SourceFile, Root)]
    [DeploymentItem(PickListFile, Root)]
    [DeploymentItem(ExcelTemplateFile, Root)]
    [DeploymentItem(BinaryFile, Root)]
    
    [TestClass]
    public class COBieXbimSerialisationTests
    {
        private const string Root = "TestSourceFiles";
        private const string SourceModelLeaf = "Clinic-Handover.xbim"; //"Clinic_A.xbim";//"2012-03-23-Duplex-Handover.xbim";
        private const string PickListLeaf = "PickLists.xml";
        private const string SourceBinaryFile = "COBieToXbim.xCOBie";
        private const string ExcelTemplateLeaf = "COBie-US-2_4-template.xls";

        private const string SourceFile = Root + @"\" + SourceModelLeaf;
        private const string PickListFile = Root + @"\" + PickListLeaf;
        private const string ExcelTemplateFile = Root + @"\" + ExcelTemplateLeaf;
        private const string BinaryFile = Root + @"\" + SourceBinaryFile;

        [TestMethod]
        public void Contacts_XBimSerialise()
        {
            COBieWorkbook workBook;
            COBieContext context;
            IModel model;
            COBieBuilder builder;
            //if (!File.Exists(BinaryFile))
            //{
            //context = new COBieContext(null);
            //model = new XbimFileModelServer();
            //model.Open(SourceFile, delegate(int percentProgress, object userState)
            //{
            //    Console.Write("\rReading File {1} {0}", percentProgress, SourceFile);
            //});
            //context.Model = model;
            //context.TemplateFileName = ExcelTemplateFile;
            //builder = new COBieBuilder(context);
            //workBook = builder.Workbook;
            //COBieBinarySerialiser serialiser = new COBieBinarySerialiser(BinaryFile);
            //serialiser.Serialise(workBook);
            //}
            //else
            //{
                COBieBinaryDeserialiser deserialiser = new COBieBinaryDeserialiser(BinaryFile);
                workBook = deserialiser.Deserialise();
            //}

            COBieXBimSerialiser xBimSerialiser = new COBieXBimSerialiser();
            xBimSerialiser.Serialise(workBook);

            
            context = new COBieContext(null);
            model = xBimSerialiser.Model;
            IEnumerable<IfcOwnerHistory> xxx = model.InstancesOfType<IfcOwnerHistory>();

            context.Model = model;
            context.TemplateFileName = ExcelTemplateFile;

            builder = new COBieBuilder(context);

            COBieWorkbook book = builder.Workbook;
            
            
            //create excel file
            string excelFile = Path.ChangeExtension(SourceBinaryFile, ".xls");
            ICOBieSerialiser formatter = new COBieXLSSerialiser(excelFile, ExcelTemplateFile);
            builder.Export(formatter);
            Process.Start(excelFile);

            // Assert
            //Assert.AreEqual(19, newBook.Count);

            // 19 workbooks. # rows in a selection.
        }
    }
}
