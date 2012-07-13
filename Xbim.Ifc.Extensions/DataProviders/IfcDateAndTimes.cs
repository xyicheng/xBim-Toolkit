#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcDateAndTimes.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Collections.Generic; using Xbim.XbimExtensions.Interfaces;
using Xbim.Ifc.DateTimeResource;

#endregion

namespace Xbim.XbimExtensions.DataProviders
{
    public class IfcDateAndTimes
    {
        private readonly IModel _model;

        public IfcDateAndTimes(IModel model)
        {
            this._model = model;
        }

        public IEnumerable<IfcDateAndTime> Items
        {
            get { return this._model.InstancesOfType<IfcDateAndTime>(); }
        }
    }
}