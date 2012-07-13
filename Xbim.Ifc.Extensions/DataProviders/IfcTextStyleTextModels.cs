#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcTextStyleTextModels.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Collections.Generic; using Xbim.XbimExtensions.Interfaces;
using Xbim.Ifc.PresentationAppearanceResource;

#endregion

namespace Xbim.XbimExtensions.DataProviders
{
    public class IfcTextStyleTextModels
    {
        private readonly IModel _model;

        public IfcTextStyleTextModels(IModel model)
        {
            this._model = model;
        }

        public IEnumerable<IfcTextStyleTextModel> Items
        {
            get { return this._model.InstancesOfType<IfcTextStyleTextModel>(); }
        }
    }
}