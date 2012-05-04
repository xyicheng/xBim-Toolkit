﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcCurve.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.Ifc.SelectTypes;
using Xbim.XbimExtensions;

#endregion

namespace Xbim.Ifc.GeometryResource
{
    [IfcPersistedEntity, Serializable]
    public class CurveSet : XbimSet<IfcCurve>
    {
        internal CurveSet(IPersistIfcEntity owner)
            : base(owner)
        {
        }
    }

    [IfcPersistedEntity, Serializable]
    public abstract class IfcCurve : IfcGeometricRepresentationItem, IfcGeometricSetSelect
    {
        /// <summary>
        ///   Derived. The space dimensionality of this abstract class, defined differently for all subtypes, i.e. for IfcLine, IfcConic and IfcBoundedCurve.
        /// </summary>
        public abstract IfcDimensionCount Dim { get; }
    }
}