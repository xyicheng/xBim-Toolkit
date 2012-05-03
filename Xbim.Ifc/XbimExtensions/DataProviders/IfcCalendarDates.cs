#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcCalendarDates.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Collections.Generic;
using Xbim.Ifc.DateTimeResource;

#endregion

namespace Xbim.XbimExtensions.DataProviders
{
    public class IfcCalendarDates
    {
        private readonly IModel _model;

        public IfcCalendarDates(IModel model)
        {
            this._model = model;
        }

        public IEnumerable<IfcCalendarDate> Items
        {
            get { return this._model.InstancesOfType<IfcCalendarDate>(); }
        }
    }
}