#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcRelFlowControlElementss.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Collections.Generic;
using Xbim.Ifc.SharedBldgServiceElements;

#endregion

namespace Xbim.XbimExtensions.DataProviders
{
    public class IfcRelFlowControlElementss
    {
        private readonly IModel _model;

        public IfcRelFlowControlElementss(IModel model)
        {
            this._model = model;
        }

        public IEnumerable<IfcRelFlowControlElements> Items
        {
            get { return this._model.InstancesOfType<IfcRelFlowControlElements>(); }
        }
    }
}