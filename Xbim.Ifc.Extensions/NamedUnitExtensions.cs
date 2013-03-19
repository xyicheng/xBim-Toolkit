using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.MeasureResource;

namespace Xbim.Ifc.Extensions
{
    public static class NamedUnitExtensions
    {
        public static string Name(this IfcNamedUnit namedUnit)
        {
            if (namedUnit is IfcSIUnit)
                return ((IfcSIUnit)namedUnit).ToString();
            else if(namedUnit is IfcConversionBasedUnit)
                return ((IfcConversionBasedUnit)namedUnit).Name;
            else if(namedUnit is IfcContextDependentUnit)
                return ((IfcContextDependentUnit)namedUnit).Name;
            else
                return "";
        }
    }
}
