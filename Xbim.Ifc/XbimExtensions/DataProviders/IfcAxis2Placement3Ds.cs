#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcAxis2Placement3Ds.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Collections.Generic;
using Xbim.Ifc.GeometryResource;

#endregion

namespace Xbim.XbimExtensions.DataProviders
{
    public class IfcAxis2Placement3Ds
    {
        private readonly IModel _model;

        public IfcAxis2Placement3Ds(IModel model)
        {
            this._model = model;
        }

        public IEnumerable<IfcAxis2Placement3D> Items
        {
            get { return this._model.InstancesOfType<IfcAxis2Placement3D>(); }
        }
    }
}