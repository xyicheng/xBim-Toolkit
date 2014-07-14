using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Xbim.WeXplorer.MVC4.Models
{
    public class XbimError : IXbimJsonResult
    {
        public string Type
        {
            get { return "Error"; }
        }

        public uint? Label
        {
            get { return null; }
        }

        private string _message;
        public string Message { get { return _message; } }

        public XbimError(string message)
        {
            _message = message;
        }
    }
}