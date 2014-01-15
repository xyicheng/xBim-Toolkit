﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IFaceBasedModelCollection.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Collections.Generic;

#endregion

namespace Xbim.XbimExtensions.Interfaces
{
    public interface IFaceBasedModelCollection
    {
        IEnumerable<IFaceBasedModel> FaceModels { get; }
    }
}