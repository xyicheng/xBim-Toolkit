using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.IO;

namespace Xbim.Script
{
    public class XbimQueryParser
    {
        private Parser _parser;
        private Scanner _scanner;

        private string _strSource;
        private IList<string> _strListSource;
        private System.IO.Stream _streamSource;

        /// <summary>
        /// Errors encountered during parsing.
        /// </summary>
        public List<string> Errors { get { return _scanner.Errors; } }

        /// <summary>
        /// Locations of the errors. Use index of the error to get according location.
        /// It is already part of the error message but information contained in this property
        /// can be used to show the position interactively for example.
        /// </summary>
        public List<Location> ErrorLocations { get { return _scanner.ErrorLocations; } }

        /// <summary>
        /// Messages go to the Console command line normally but 
        /// you can use this property to define optional output 
        /// where messages should go. 
        /// </summary>
        public System.IO.TextWriter Output { get { return _parser.Output; } set { _parser.Output = value; } }

        /// <summary>
        /// Model on which the parser operates
        /// </summary>
        public XbimModel Model { get { return _parser.Model; } }

        /// <summary>
        /// Variables which are result of the parsing process. 
        /// It can be either list of selected objects or new objects assigned to the variables.
        /// </summary>
        public XbimVariables Results { get { return _parser.Variables; } }

        /// <summary>
        /// Constructor which takes a existing model as an argument. 
        /// You can also close and open any model from the script.
        /// </summary>
        /// <param name="model">Model which shuldbe used for the script execution</param>
        public XbimQueryParser(XbimModel model)
        {
            _scanner = new Scanner();
            _parser = new Parser(_scanner, model);
        }

        /// <summary>
        /// Parameterless constructor of the class. 
        /// Default empty model is created which can be used 
        /// or you can open other model from the script.
        /// </summary>
        public XbimQueryParser()
        {
            //create new empty model
            var model = XbimModel.CreateTemporaryModel();
            _scanner = new Scanner();
            _parser = new Parser(_scanner, model);
        }

        /// <summary>
        /// Set source for scanning and parsing
        /// </summary>
        /// <param name="source">source to be used</param>
        public void SetSource(string source)
        {
            ClearSources();
            _strSource = source;
        }

        /// <summary>
        /// Set source for scanning and parsing
        /// </summary>
        /// <param name="source">source to be used</param>
        public void SetSource(System.IO.Stream source)
        {
            ClearSources();
            _streamSource = source;
        }

        /// <summary>
        /// Set source for scanning and parsing
        /// </summary>
        /// <param name="source">source to be used</param>
        public void SetSource(IList<string> source)
        {
            ClearSources();
            _strListSource = source;
        }

        /// <summary>
        /// Performs only scan of the source and returns list of string 
        /// representation of Tokens. This is mainly for debugging.
        /// </summary>
        /// <returns>List of string representation of tokens</returns>
        public IEnumerable<string> ScanOnly()
        {
            ResetSource();
            List<string> result = new List<string>();
            int val = _scanner.yylex();
            while (val != (int)Tokens.EOF)
            {
                string name = val >= 60 ? Enum.GetName(typeof(Tokens), val) : ((char)val).ToString();
                result.Add(name);
                val = _scanner.yylex();
            }
            return result;
        }

        /// <summary>
        /// The main function used to perform parsing of the query. 
        /// Rerurns false only if something serious happen during
        /// parsing process. However it is quite possible that some errors occured. 
        /// So, make sure to check Errors if there are any.
        /// </summary>
        /// <returns>False if parsing failed, true otherwise.</returns>
        public bool Parse()
        {
            ResetSource();
            //_parser = new Parser(_scanner, _model);
            return _parser.Parse();
        }

        /// <summary>
        /// The main function used to perform parsing of the query. 
        /// Rerurns false only if something serious happen during
        /// parsing process. However it is quite possible that some errors occured. 
        /// So, make sure to check Errors if there are any.
        /// </summary>
        /// <returns>False if parsing failed, true otherwise.</returns>
        public bool Parse(string source)
        {
            SetSource(source);
            return Parse();
        }

        /// <summary>
        /// The main function used to perform parsing of the query. 
        /// Rerurns false only if something serious happen during
        /// parsing process. However it is quite possible that some errors occured. 
        /// So, make sure to check Errors if there are any.
        /// </summary>
        /// <returns>False if parsing failed, true otherwise.</returns>
        public bool Parse(System.IO.Stream source)
        {
            SetSource(source);
            return Parse();
        }

        /// <summary>
        /// The main function used to perform parsing of the query. 
        /// Rerurns false only if something serious happen during
        /// parsing process. However it is quite possible that some errors occured. 
        /// So, make sure to check Errors if there are any.
        /// </summary>
        /// <returns>False if parsing failed, true otherwise.</returns>
        public bool Parse(IList<string> source)
        {
            SetSource(source);
            return Parse();
        }

        /// <summary>
        /// Source is available untill new source is defined. 
        /// So it is possible to perform scanning or parsing with the 
        /// source many times. Be carefull as side effects like data 
        /// creation will be persisted over the repeated execution.
        /// </summary>
        private void ResetSource()
        {
            if (_streamSource != null && _strListSource != null && _strSource != null) throw new Exception("Only one source can be valid.");
            if (_streamSource == null && _strListSource == null && _strSource == null) throw new Exception("One source must be valid.");
            if (_streamSource != null) _scanner.SetSource(_streamSource);
            if (_strListSource != null) _scanner.SetSource(_strListSource);
            if (_strSource != null) _scanner.SetSource(_strSource, 0);

            //reset error log with new source
            _scanner.Errors.Clear();
        }

        /// <summary>
        /// Helper function to clear sources available before new one is set.
        /// </summary>
        private void ClearSources() 
        {
            _strSource = null;
            _strListSource = null;
            _streamSource = null;
            //_scanner = new Scanner();
        }



    }
}
