#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcArbitraryProfileDefWithVoidss.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Collections.Generic;
using Xbim.Ifc.ProfileResource;

#endregion

namespace Xbim.XbimExtensions.DataProviders
{
    public class IfcArbitraryProfileDefWithVoidss
    {
        private readonly IModel _model;

        public IfcArbitraryProfileDefWithVoidss(IModel model)
        {
            this._model = model;
        }

        public IEnumerable<IfcArbitraryProfileDefWithVoids> Items
        {
            get { return this._model.InstancesOfType<IfcArbitraryProfileDefWithVoids>(); }
        }
    }
}