#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.IO
// Filename:    IfcOutputStream.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Diagnostics;
using System.IO;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.IO
{
    public class IfcOutputStream
    {
        #region Fields

        private TextWriter _output;

        #endregion

        public IfcOutputStream(TextWriter output)
        {
            _output = output;
        }

        public void Store(IModel model, ReportProgressDelegate progressDelegate)
        {
            using (Part21FileWriter fw = new Part21FileWriter())
            {
                try
                {
                    if (model != null)
                    {
                        fw.Write(model, _output);
                        fw.Close();
                    }
                }
                catch (Exception e)
                {
                    Exception ex = e;
                    int indent = Debug.IndentLevel;
                    while (ex != null)
                    {
                        Debug.WriteLine(ex.Message);
                        ex = ex.InnerException;
                        Debug.Indent();
                    }
                    Debug.IndentLevel = indent;
                    throw new Exception("Failed to save Ifc formatted file", e);
                }
            }
        }

        public void Store(IModel model)
        {
            Store(model, null);
        }
    }
}