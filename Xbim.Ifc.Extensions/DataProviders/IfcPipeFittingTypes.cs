#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcPipeFittingTypes.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Collections.Generic; using Xbim.XbimExtensions.Interfaces;
using Xbim.Ifc.HVACDomain;

#endregion

namespace Xbim.XbimExtensions.DataProviders
{
    public class IfcPipeFittingTypes
    {
        private readonly IModel _model;

        public IfcPipeFittingTypes(IModel model)
        {
            this._model = model;
        }

        public IEnumerable<IfcPipeFittingType> Items
        {
            get { return this._model.InstancesOfType<IfcPipeFittingType>(); }
        }
    }
}