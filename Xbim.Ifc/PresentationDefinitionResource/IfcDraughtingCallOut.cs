#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcDraughtingCallOut.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.Ifc.GeometryResource;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

#endregion

namespace Xbim.Ifc.PresentationDefinitionResource
{
    [IfcPersistedEntity, Serializable]
    public class IfcDraughtingCallOut : IfcGeometricRepresentationItem
    {
        public IfcDraughtingCallOut()
        {
            _contents = new XbimSet<IfcDraughtingCalloutElement>(this);
        }

        #region Fields

        private readonly XbimSet<IfcDraughtingCalloutElement> _contents;

        #endregion

        #region Properties

        #endregion

        #region Part 21 Step file Parse routines

        public override void IfcParse(int propIndex, IPropertyValue value)
        {
            if (propIndex == 0)
                _contents.Add((IfcDraughtingCalloutElement) value.EntityVal);
            else
                this.HandleUnexpectedAttribute(propIndex, value);
        }

        #endregion

        public override string WhereRule()
        {
            return "";
        }
    }
}