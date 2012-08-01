#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.IO
// Filename:    IfcInputStream.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;
using Xbim.Common.Exceptions;

#endregion

namespace Xbim.IO
{
    public class IfcInputStream : IDisposable
    {
        #region Fields

        private TextWriter _errorLog;
        private Stream _input;

        #endregion

        /// <summary>
        ///   Creates a new IfcInputStream and on the specified the input stream
        /// </summary>
        public IfcInputStream(Stream inputStream)
        {
            _input = inputStream;
            _errorLog = new StringWriter(new StringBuilder(0xFFFF));
        }

        /// <summary>
        ///   Creates a new IfcInputStream and on the specified the input stream and writes any errors to the specified error log
        /// </summary>
        public IfcInputStream(Stream inputStream, TextWriter errorLog)
            : this(inputStream)
        {
            _errorLog = errorLog;
        }

        public TextWriter ErrorLog
        {
            get { return _errorLog; }
        }


        /// <summary>
        ///   Parses and loads the contents of the stream into the model
        /// </summary>
        /// <param name = "intoModel">Model to populate with contents of stream, normally this model is empty, use ModelManager.CreateModel to create a model. 
        ///   Use ModelManager.ReleaseModel to destroy the model when no longer required</param>
        /// <param name = "validate">Level to validate the model after parsing, multiple flags cab be set i.e. (ValidationLevel.Properties|ValidationLevel.Inverses)</param>
        /// <param name = "progressDelegate">delegate to call to report progress</param>
        /// <returns>The number of errors found. -1 indicates a general parsing failure, most likely a the input is not properly formatted to Ifc STEP format</returns>
        public int Load(IModel intoModel, FilterViewDefinition filter, ValidationFlags validate,
                        ReportProgressDelegate progressDelegate)
        {
            IndentedTextWriter tw = new IndentedTextWriter(_errorLog);
            try
            {
                ModelManager.TransactingModel = intoModel;
                int errors = intoModel.ParsePart21(_input, filter, _errorLog, progressDelegate);
                if (errors == 0 && validate > 0)
                    errors = intoModel.Validate(_errorLog, progressDelegate);

                return errors;
            }
            catch (Exception e)
            {
                Exception ex = e;
                int indent = tw.Indent;
                while (ex != null)
                {
                    tw.WriteLine(ex.Message);
                    ex = ex.InnerException;
                    tw.Indent++;
                }
                tw.Indent = indent;
                Console.WriteLine(_errorLog.ToString());
                return -1;
            }
        }

        /// <summary>
        ///   Parses and loads the contents of the stream into the model
        /// </summary>
        /// <param name = "intoModel">Model to populate with contents of stream, normally this model is empty, use ModelManager.CreateModel to create a model.</param>
        /// <param name = "validate">Level to validate the model after parsing, multiple flags cab be set i.e. (ValidationLevel.Properties|ValidationLevel.Inverses)</param>
        /// <returns></returns>
        public int Load(IModel intoModel, ValidationFlags validate)
        {
            return Load(intoModel, null, validate, null);
        }

        /// <summary>
        ///   Parses and loads the contents of the stream into the model
        /// </summary>
        /// <param name = "intoModel">Model to populate with contents of stream, normally this model is empty, use ModelManager.CreateModel to create a model.</param>
        /// <returns></returns>
        public int Load(IModel intoModel)
        {
            return Load(intoModel, null, ValidationFlags.None, null);
        }

        /// <summary>
        ///   Parses and loads the contents of the stream into the model, only loads items in filter
        /// </summary>
        /// <param name = "intoModel">Model to populate with contents of stream, normally this model is empty, use ModelManager.CreateModel to create a model.</param>
        /// <param name = "filter">Selected entities to load into the model</param>
        /// <returns></returns>
        public int Load(IModel intoModel, FilterViewDefinition filter)
        {
            return Load(intoModel, filter, ValidationFlags.None, null);
        }

        public int Index(Stream indexStream, /*FilterViewDefinition filter, TextWriter errorLog, */
                         ReportProgressDelegate progressHandler)
        {
            int errorCount = 0;

            using (P21toIndexParser part21Parser = new P21toIndexParser(_input, indexStream))
            {
                IndentedTextWriter tw = new IndentedTextWriter(_errorLog, "    ");
                try
                {
                    if (progressHandler != null) part21Parser.ProgressStatus += progressHandler;
                    part21Parser.Parse();

                }
                catch (XbimException ex)
                {
                    // Expected errors. We don't need the full exception stack.
                    _errorLog.WriteLine(ex.Message);
                    errorCount++;
                }
                catch (Exception ex)
                {
                    _errorLog.WriteLine("Unexpected Parser error.");
                    int indent = tw.Indent;
                    while (ex != null)
                    {
                        tw.Indent++;
                        _errorLog.WriteLine(ex.Message);
                        ex = ex.InnerException;
                    }
                    tw.Indent = indent;
                    errorCount++;
                }
                finally
                {
                    // part21Parser.EntityCreate -= creator;
                    if (progressHandler != null) part21Parser.ProgressStatus -= progressHandler;
                }
                //  errorCount = part21Parser.ErrorCount + errorCount;
            }
            return errorCount;
        }

        public void Dispose()
        {
            if (_errorLog != null) _errorLog.Close();
            if (_input != null) _input.Close();
        }

        public void Close()
        {
            Dispose();
        }

        public int Index(Stream stream)
        {
            return Index(stream, null);
        }
    }
}