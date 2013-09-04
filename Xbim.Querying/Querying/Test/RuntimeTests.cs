using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Irony.Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.IO;
using Xbim.IO.Querying;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.Querying.xBimQL.Test
{
    [DeploymentItem(SourceFile)]
    [TestClass]
    public class xBimQueryRuntimeTests
    {
        private const string Root = @"Querying\Test";
        private const string SourceModelFileName = "Duplex-Handover.xBIM";
        private const string SourceFile = Root + @"\" + SourceModelFileName;
        private LanguageData _language;

        private ParseTree GetParseTree(string Query)
        {
            var grammar = new xBimQueryLanguage();
            _language = new LanguageData(grammar);
            var parser = new Parser(_language);
            var pt = parser.Parse(Query);
            return pt;
        }

        [TestMethod]
        public void BasicSetup()
        {
            string command = 
                "a = 1+1\r\n" + 
                "a + 1";
            ParseTree _parseTree = GetParseTree(command);
            if (_parseTree.ParserMessages.Count > 0) return;
            var iRunner = _language.Grammar as ICanRunSample;
            var args = new RunSampleArgs(_language, command, _parseTree);
            string output = iRunner.RunSample(args);
            Assert.AreEqual("3\r\n", output);
        }

        [TestMethod]
        public void ModelRootBasicQuery()
        {
            // DirectoryInfo d = new DirectoryInfo("."); // to find folder location
            XbimModel model = new XbimModel();
            model.Open(SourceModelFileName, XbimDBAccess.Read);
            var cnt = model.Instances.OfType("IfcWall", false).Count();
            var chkCnt = model.Query("select @ifcWall");
            var c2 = chkCnt as IEnumerable<IPersistIfcEntity>;
            int cnt2 = c2.Count();
            Assert.AreEqual(cnt, cnt2);

            model.Close();
        }

        [TestMethod]
        public void QueryingSelectFunction()
        {
            // DirectoryInfo d = new DirectoryInfo("."); // to find folder location
            XbimModel model = new XbimModel();
            model.Open(SourceModelFileName, XbimDBAccess.Read);
            var cnt = model.Instances.OfType("IfcWall", false).Count();
            var chkCnt = model.Query("select @ifcWall.Count()");
            Assert.AreEqual(cnt.ToString(), chkCnt.ToString());
            model.Close();
        }

        [TestMethod]
        public void QueryingPropFunction()
        {
            // DirectoryInfo d = new DirectoryInfo("."); // to find folder location
            XbimModel model = new XbimModel();
            model.Open(SourceModelFileName, XbimDBAccess.Read);
            var chkCnt = model.Query("select @12275.Representation");
            Assert.AreEqual(1, ((IEnumerable<object>)chkCnt).Count());

            model.Close();
        }

        [TestMethod]
        public void QueryingNestedPropFunction()
        {
            // DirectoryInfo d = new DirectoryInfo("."); // to find folder location
            XbimModel model = new XbimModel();
            model.Open(SourceModelFileName, XbimDBAccess.Read);
            var chkCnt = model.Query("select @12275.Representation.Representations");
            Assert.AreEqual(3, ((IEnumerable<object>)chkCnt).Count());
            model.Close();
        }
    }
}
