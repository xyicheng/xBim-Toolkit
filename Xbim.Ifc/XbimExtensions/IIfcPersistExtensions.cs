#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IIfcPersistExtensions.cs
// (See accompanying copyright.rtf)

#endregion

#region Directives
using System;
using Xbim.Common.Exceptions;

using Xbim.XbimExtensions.Interfaces;
#endregion

namespace Xbim.XbimExtensions
{
    /// <summary>
    /// Extension methods for the <see cref="IPersistIfc"/> interface.
    /// </summary>
    public static class IIfcPersistExtensions
    {

        /// <summary>
        /// Handles the case where a property was not expected for this entity.
        /// </summary>
        /// <param name="persistIfc">The item being parsed.</param>
        /// <param name="propIndex">Index of the property.</param>
        /// <param name="value">The value of the property.</param>
        public static void HandleUnexpectedAttribute(this IPersistIfc persistIfc, int propIndex, IPropertyValue value)
        {
            // TODO: Review this workaround for older IFC files with extraneous properties
            if (value.Type == IfcParserType.Enum && String.Compare(value.EnumVal, "NOTDEFINED") == 0)
                return;

            throw new XbimParserException(string.Format("Attribute index {0} is out of range for {1}", propIndex + 1,
                                                      persistIfc.GetType().Name.ToUpper()));
        }
    }
}
