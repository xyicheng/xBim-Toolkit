#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Presentation
// Filename:    ISelectable.cs
// Published:   01, 2012
// Last Edited: 9:05 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

namespace Xbim.Presentation
{
    // Common interface for items that can be selected

    public interface ISelectable
    {
        bool IsSelected { get; set; }
    }
}