﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcConstraintEnum.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

namespace Xbim.Ifc2x3.ConstraintResource
{
    /// <summary>
    ///   An IfcConstraintEnum is an enumeration used to qualify a constraint.
    /// </summary>
    public enum IfcConstraintEnum
    {
        HARD,
        SOFT,
        ADVISORY,
        USERDEFINED,
        NOTDEFINED
    }
}