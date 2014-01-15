using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Irony.Parsing;
using Xbim.IO;
using Xbim.Querying;

namespace Xbim.IO.Querying
{
    public static class QLModelExtensions
    {
        public static object Query(this XbimModel model, string query)
        {
            var grammar = new xBimQueryLanguage(model);
            var _language = new LanguageData(grammar);
            var parser = new Irony.Parsing.Parser(_language);
            ParseTree _parseTree = parser.Parse(query);

            if (_parseTree.ParserMessages.Count > 0) 
                return null;

            var iRunner = _language.Grammar as ICanRunSample;
            var args = new RunSampleArgs(_language, query, _parseTree);

            // string output = iRunner.RunSample(args);
            return grammar.Run(args);
        }
    }
}
