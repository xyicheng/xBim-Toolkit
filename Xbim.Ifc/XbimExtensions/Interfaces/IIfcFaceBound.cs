#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IIfcFaceBound.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using Xbim.Ifc.MeasureResource;

#endregion

namespace Xbim.XbimExtensions.Interfaces
{
    public interface IIfcFaceBound
    {
        IIfcEdgeLoop Bound { get; }
        IfcBoolean Orientation { get; }
    }
}