#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Presentation
// Filename:    IGroupable.cs
// Published:   01, 2012
// Last Edited: 9:05 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;

#endregion

namespace Xbim.Presentation
{
    public interface IGroupable
    {
        Guid ID { get; }
        Guid ParentID { get; set; }
        bool IsGroup { get; set; }
    }
}