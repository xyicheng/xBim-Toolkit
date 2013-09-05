using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Irony.Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.RepresentationResource;
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
        private const string Root = @"";
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
            using (XbimModel model = new XbimModel())
            {
                model.Open(SourceModelFileName, XbimDBAccess.Read);
                var cnt = model.Instances.OfType("IfcWall", false).Count();
                var chkCnt = model.Query("select @ifcWall");
                var c2 = chkCnt as IEnumerable<IPersistIfcEntity>;
                int cnt2 = c2.Count();
                Assert.AreEqual(cnt, cnt2);

                model.Close();
            }
        }

        [TestMethod]
        public void QueryingSelectFunction()
        {
            // DirectoryInfo d = new DirectoryInfo("."); // to find folder location
            using (XbimModel model = new XbimModel())
            {
                model.Open(SourceModelFileName, XbimDBAccess.Read);
                var cnt = model.Instances.OfType("IfcWall", false).Count();
                var chkCnt = model.Query("select @ifcWall.Count()");
                Assert.AreEqual(cnt.ToString(), chkCnt.ToString());
                model.Close();
            }
        }

        [TestMethod]
        public void QueryingSelectProp()
        {
            // DirectoryInfo d = new DirectoryInfo("."); // to find folder location
            using (XbimModel model = new XbimModel())
            {
                model.Open(SourceModelFileName, XbimDBAccess.Read);
                var chkCnt = model.Query("select @12275.Representation");
                Assert.IsInstanceOfType(chkCnt, typeof(IfcProductDefinitionShape));
                
                model.Close();
            }
        }

        [TestMethod]
        public void QueryingSelectNestedProp()
        {
            // DirectoryInfo d = new DirectoryInfo("."); // to find folder location
            using (XbimModel model = new XbimModel())
            {
                model.Open(SourceModelFileName, XbimDBAccess.Read);
                var chkCnt = model.Query("select @12275.Representation.Representations");
                Assert.AreEqual(3, ((IEnumerable<object>)chkCnt).Count());

                var cnt = model.Query("select @12275.Representation.Representations.Count()");
                Assert.AreEqual(3, cnt);

                model.Close();
            }
        }

        [TestMethod]
        public void QueryingSelectNestedPropRange()
        {
            // DirectoryInfo d = new DirectoryInfo("."); // to find folder location
            using (XbimModel model = new XbimModel())
            {
                model.Open(SourceModelFileName, XbimDBAccess.Read);
                var cnt = model.Query("select @12275.Representation.Representations.Range(1,1)") as List<Object>;
                IPersistIfcEntity ent = cnt.FirstOrDefault() as IPersistIfcEntity;
                Assert.AreEqual(12266, Math.Abs(ent.EntityLabel));
                model.Close();
            }
        }

        [TestMethod]
        public void QueryingSelectWhere()
        {
            // DirectoryInfo d = new DirectoryInfo("."); // to find folder location
            using (XbimModel model = new XbimModel())
            {
                model.Open(SourceModelFileName, XbimDBAccess.Read);
                var cnt = model.Query("select @12275.Representation.Representations.Where(true)") as List<Object>;
                Assert.IsNotNull(cnt);
                Assert.AreEqual(3, cnt.Count());

                cnt = model.Query("select @12275.Representation.Representations.Where(false)") as List<Object>;
                Assert.IsNotNull(cnt);
                Assert.AreEqual(0, cnt.Count());

                cnt = model.Query("select @12275.Representation.Representations.Where(1<2)") as List<Object>;
                Assert.IsNotNull(cnt);
                Assert.AreEqual(3, cnt.Count());

                model.Close();
            }
        }

        [TestMethod]
        public void QueryingSelectContextualWhere()
        {
            // DirectoryInfo d = new DirectoryInfo("."); // to find folder location
            using (XbimModel model = new XbimModel())
            {
                model.Open(SourceModelFileName, XbimDBAccess.Read);
                var cnt = model.Query(@"select @12275.Representation.Representations.Where(@RepresentationIdentifier==""Body"")") as List<Object>;
                Assert.IsNotNull(cnt);
                Assert.AreEqual(1, cnt.Count());
                model.Close();
            }
        }
    }
}
