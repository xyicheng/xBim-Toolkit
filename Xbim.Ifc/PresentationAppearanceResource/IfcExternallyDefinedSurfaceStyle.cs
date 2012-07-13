#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcExternallyDefinedSurfaceStyle.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.Ifc.ExternalReferenceResource;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;

#endregion

namespace Xbim.Ifc.PresentationAppearanceResource
{
    [IfcPersistedEntityAttribute, Serializable]
    public class IfcExternallyDefinedSurfaceStyle : IfcExternalReference, IfcSurfaceStyleElementSelect
    {
    }
}