#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    XbimSchemaValidationException.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Text;

#endregion

namespace Xbim.XbimExtensions
{
    public class XbimSchemaValidationException : Exception
    {
        private readonly ValidationErrorCollection _errors;

        public ValidationErrorCollection ValidationErrors
        {
            get { return _errors; }
        }

        public XbimSchemaValidationException(ValidationErrorCollection errorList)
        {
            _errors = errorList;
        }

        public XbimSchemaValidationException(ValidationErrorCollection errorList, string message)
            : base(message)
        {
            _errors = errorList;
        }

        public XbimSchemaValidationException(ValidationErrorCollection errorList, string message, Exception inner)
            : base(message, inner)
        {
            _errors = errorList;
        }

        public override string StackTrace
        {
            get
            {
                StringBuilder stackStr = new StringBuilder();
                foreach (string err in _errors)
                {
                    stackStr.AppendLine(err);
                }
                stackStr.Append(base.StackTrace);
                return stackStr.ToString();
            }
        }
    }
}