#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcAnnotationTextOccurrences.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Collections.Generic;
using Xbim.Ifc.PresentationDefinitionResource;

#endregion

namespace Xbim.XbimExtensions.DataProviders
{
    public class IfcAnnotationTextOccurrences
    {
        private readonly IModel _model;

        public IfcAnnotationTextOccurrences(IModel model)
        {
            this._model = model;
        }

        public IEnumerable<IfcAnnotationTextOccurrence> Items
        {
            get { return this._model.InstancesOfType<IfcAnnotationTextOccurrence>(); }
        }
    }
}