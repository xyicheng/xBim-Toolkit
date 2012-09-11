using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.COBie;
using Xbim.IO;
using Xbim.XbimExtensions;
using Xbim.COBie.Rows;

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

        private const string SourceFile = Root + @"\" + SourceModelLeaf;
        private const string PickListFile = Root + @"\" + PickListLeaf;

        static IModel _model;

        [ClassInitialize]
        public static void LoadModel(TestContext context)
        {
            _model = new XbimFileModelServer();
            _model.Open(SourceFile);
            COBieQueries cobieEngine = new COBieQueries(_model);

        }

        [ClassCleanup]
        public static void CloseModel()
        {
            if(_model != null)
                _model.Dispose();
            _model = null;
        }

        [TestMethod]
        public void Should_Return_Floors()
        {
            COBieQueries cobieEngine = new COBieQueries(Model);

            var floors = cobieEngine.GetCOBieFloorSheet();

            Assert.AreEqual(4, floors.Rows.Count);

            FormatFloors(floors);           
        }        

        [TestMethod]
        public void Should_Return_Spaces()
        {
            COBieQueries cobieEngine = new COBieQueries(Model);

            var spaces = cobieEngine.GetCOBieSpaceSheet();

            Assert.AreEqual(22, spaces.Rows.Count);

            FormatSpaces(spaces);
        }

        // TODO: refactor to generic
        private static void FormatFloors(COBieSheet<COBieFloorRow> floors)
        {
            int columns = 0;
            foreach (var column in floors.Columns.OrderBy(c => c.Key))
            {

                Debug.Write(column.Value.ColumnName);
                Debug.Write(", ");
                columns++;
            }
            foreach (var row in floors.Rows)
            {
                Debug.WriteLine("");
                for (int i = 0; i < columns; i++)
                {
                    Debug.Write(row[i].CellValue);
                    Debug.Write(",");
                }
            }
        }

        // TODO: refactor to generic
        private static void FormatSpaces(COBieSheet<COBieSpaceRow> spaces)
        {
            int columns = 0;
            foreach (var column in spaces.Columns.OrderBy(c => c.Key))
            {
                Debug.Write(column.Value.ColumnName);
                Debug.Write(", ");
                columns++;
            }
            foreach (var row in spaces.Rows)
            {
                Debug.WriteLine("");
                for (int i = 0; i < columns; i++)
                {
                    Debug.Write(row[i].CellValue);
                    Debug.Write(",");
                }
            }
        }

        private IModel Model
        {
            get
            {
                return _model;
            }
        }

   
    }
}
