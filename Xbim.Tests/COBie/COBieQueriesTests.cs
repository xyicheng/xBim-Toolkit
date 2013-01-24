using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.COBie;
using Xbim.IO;
using Xbim.XbimExtensions;
using Xbim.COBie.Rows;
using Xbim.XbimExtensions.Interfaces;
using System;

namespace Xbim.Tests.COBie
{
    [DeploymentItem(SourceFile, Root)]
    [DeploymentItem(PickListFile, Root)]
    [TestClass]
    public class COBieQueriesTests
    {
        private const string Root = "TestSourceFiles";
        private const string SourceModelLeaf = "2012-03-23-Duplex-Handover.xbim";
        private const string PickListLeaf = "PickLists.xml";

        private const string SourceFile =  Root + @"\" + SourceModelLeaf;
        private const string PickListFile = Root + @"\" + PickListLeaf;

        [TestInitialize]
        public  void LoadModel()
        {


        }

        [TestCleanup]
        public void CloseModel()
        {
           
        }

        [TestMethod]
        public void Should_Return_Floors()
        {
            using (XbimModel model = new XbimModel())
            {
                model.Open(SourceFile, XbimDBAccess.ReadWrite);
                COBieContext cobieContext = new COBieContext();
                cobieContext.Model = model;
                
                COBieQueries cobieEngine = new COBieQueries(cobieContext);

                var floors = cobieEngine.GetCOBieFloorSheet();

                Assert.AreEqual(4, floors.Rows.Count);

                FormatRows(floors);
            }
        }        

        [TestMethod]
          // "Need to resolve interdependency between sheets. Errors since Facilities needs calling first"
        public void Should_Return_Spaces()
        {
            using (XbimModel model = new XbimModel())
            {
                model.Open(SourceFile, XbimDBAccess.ReadWrite);
                COBieContext cobieContext = new COBieContext();
                cobieContext.Model = model;

                COBieQueries cobieEngine = new COBieQueries(cobieContext);

                var spaces = cobieEngine.GetCOBieSpaceSheet();

                Assert.AreEqual(22, spaces.Rows.Count);

                FormatRows(spaces);
            }
        }

        private static void FormatRows(ICOBieSheet<COBieRow> cOBieSheet)
        {
            int columns = 0;
            foreach (var column in cOBieSheet.Columns.OrderBy(c => c.Key))
            {
                Console.Write(column.Value.ColumnName);
                Console.Write(", ");
                columns++;
            }
            for (int i = 0; i < cOBieSheet.RowCount; i++)
            {
                COBieRow row = cOBieSheet[i];
                Console.WriteLine("");
                for (int col = 0; col < columns; col++)
                {
                    Console.Write(row[col].CellValue);
                    Console.Write(",");
                }
            }
           
        }
   
    }
}
