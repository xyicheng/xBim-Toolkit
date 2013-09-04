using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Irony.Parsing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Xbim.Querying
{
    [TestClass]
    public class xBimQueryGrammarSelectTests
    {
        [TestMethod]
        public void xBimSimpleSelectRoots()
        {
            var grammar = new xBimQueryLanguage();
            var language = new LanguageData(grammar);
            var parser = new Parser(language);

            // basic root elements
            MustRunOk(parser, "select @IfcWall");  // all walls
            MustRunOk(parser, "select @3"); // ifclabel 3
            MustRunKO(parser, "select @-3"); // should fail

            
        }

        [TestMethod]
        public void xBimSimpleChildren()
        {
            var grammar = new xBimQueryLanguage();
            var language = new LanguageData(grammar);
            var parser = new Parser(language);
            // chidren and functions
            MustRunOk(parser, "select @3.HasOpenings");
            MustRunOk(parser, "select @IfcWall.Where(false)");
            MustRunOk(parser, "select @*.Where(false)"); // seelct all
            MustRunOk(parser, "select @.HasOpenings"); // start from default element 
        }

        private static void MustRunOk(Parser parser, string s)
        {
            var pt2 = parser.Parse(s);
            Assert.AreEqual(0, pt2.ParserMessages.Count, s);
        }

        private static void MustRunKO(Parser parser, string s)
        {
            var pt2 = parser.Parse(s);
            Assert.AreEqual(1, pt2.ParserMessages.Count, s);
        }
        

        [TestMethod]
        public void xBimMultipleSelectRoots()
        {
            var grammar = new xBimQueryLanguage();
            var language = new LanguageData(grammar);
            var parser = new Parser(language);
            MustRunOk(parser, "select @IfcWall,IfcWallStandardCase.Where()");
            MustRunOk(parser, "select @IfcWall,IfcWallStandardCase,3.Where()");
            MustRunOk(parser, "select @3,IfcWallStandardCase,7.Where()");
        }

        [TestMethod]
        public void xBimSelectBasicTree()
        {
            var grammar = new xBimQueryLanguage();
            var language = new LanguageData(grammar);
            var parser = new Parser(language);

            MustRunOk(parser, "select @IfcWall.HasOpenings");
            MustRunOk(parser, "select @IfcWall.HasOpenings.RelatedOpeningElement");
            MustRunKO(parser, "select @IfcWall/2"); // this one must fail.
            MustRunOk(parser, "(select @IfcWall.Count())/2"); // this sintax is ok
            

        }

        [TestMethod]
        public void xBimSelectFunctions()
        {
            var grammar = new xBimQueryLanguage();
            var language = new LanguageData(grammar);
            var parser = new Parser(language);

            MustRunOk(parser, "select @itemOne.Where(true)");
            MustRunKO(parser, "select @itemOne.InvalidFunctionName(true)"); // this one musn't match
            MustRunOk(parser, "select @itemOne.Where(true).Range(0)");
            MustRunOk(parser, "select @itemOne.Range(0).ChildProp.Range(2,2)");
            MustRunOk(parser, "select @itemOne.Where(price>35.50)");
            MustRunOk(parser, "select @itemOne.Where(price>35.50 && lenght==1)");
            MustRunOk(parser, "select @itemOne.Where(function(param)>35.50 && function2(par1, par2))");
            MustRunOk(parser, "select @itemOne.Where(function(param)>35.50 || (function2(par2) && Function3(tre)))");
            
        }


        [TestMethod]
        public void xBimSelectFunctionsWithStrings()
        {
            var grammar = new xBimQueryLanguage();
            var language = new LanguageData(grammar);
            var parser = new Parser(language);

            MustRunOk(parser, "select @12275.Representation.Representations.Where(Identifier=='Body')");
        }
    }
}
