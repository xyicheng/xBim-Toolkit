#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcRelAssociatesDocuments.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Collections.Generic;
using Xbim.Ifc.Kernel;

#endregion

namespace Xbim.XbimExtensions.DataProviders
{
    public class IfcRelAssociatesDocuments
    {
        private readonly IModel _model;

        public IfcRelAssociatesDocuments(IModel model)
        {
            this._model = model;
        }

        public IEnumerable<IfcRelAssociatesDocument> Items
        {
            get { return this._model.InstancesOfType<IfcRelAssociatesDocument>(); }
        }
    }
}