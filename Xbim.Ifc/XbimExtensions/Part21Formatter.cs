#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    Part21Formatter.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.UtilityResource;

#endregion

namespace Xbim.XbimExtensions
{
    public class Part21Formatter : IFormatProvider, ICustomFormatter
    {
        public object GetFormat(Type formatType)
        {
            if (formatType == typeof (ICustomFormatter))
                return this;
            else
                return null;
        }

        public string Format(string fmt, object arg, IFormatProvider formatProvider)
        {
            // Convert argument to a string
            string result = arg.ToString().ToUpper();
            if (!String.IsNullOrEmpty(fmt) && fmt.ToUpper() == "R")
            {
                if (!result.Contains("."))
                {
                    if (result.Contains("E"))
                        result = result.Replace("E", ".E");
                    else
                        result += ".";
                }

                return result;
            }
            else if (!String.IsNullOrEmpty(fmt) && fmt.ToUpper() == "T") //TimeStamp
            {
                DateTime? dt = arg as DateTime?;
                if (dt.HasValue == false)
                    throw new ArgumentException("Only valid DateTime objects can be converted to Part21 Timestamp");
                return IfcTimeStamp.ToTimeStamp(dt.Value).ToPart21;
            }
            else if (!String.IsNullOrEmpty(fmt) && fmt.ToUpper() == "G") //Guid
            {
                Guid guid = (Guid) arg;
                return string.Format(@"'{0}'", IfcGloballyUniqueId.AsPart21(guid));
            }
                // Return string representation of argument for any other formatting code
            else
                return string.Format(@"'{0}'", arg.ToString().Replace("\'", "\'\'"));
        }
    }
}