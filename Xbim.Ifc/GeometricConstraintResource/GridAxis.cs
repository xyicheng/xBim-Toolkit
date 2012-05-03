#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    GridAxis.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using Xbim.Ifc.GeometryResource;
using Xbim.Ifc.MeasureResource;
using Xbim.XbimExtensions;

#endregion

namespace Xbim.Ifc.GeometricConstraintResource
{
    /// <summary>
    ///   An individual axis, the IfcGridAxis, is defined in the context of a design grid.
    /// </summary>
    /// <remarks>
    ///   Definition from IAI: An individual axis, the IfcGridAxis, is defined in the context of a design grid. The axis definition is based on a curve of dimensionality 2. The grid axis is positioned within the XY plane of the position coordinate system defined by the IfcDesignGrid.
    ///   HISTORY: New entity in IFC Release 1.0
    ///   Geometry Use Definitions: 
    ///   The standard geometric representation of IfcGridAxis is defined using a 2D curve entity. Grid axes are normally defined by an offset to another axis. The IfcOffsetCurve2D supports this concept.
    ///   Each grid axis has a sense given by the parameterization of the curve. The attribute SameSense is an indicator of whether or not the sense of the grid axis agrees with, or opposes, that of the underlying curve. 
    ///   Illustration
    ///   The grid axis is defined as a 2D curve within the xy plane of the position coordinate system. Any curve can be used to define a grid axis, most common is the use of IfcLine for linear grids and IfcCircle for radial grids.
    ///   Most grids are defined by a pair of axis lists, each defined by a base grid axis and axes given by an offset to the base axis. The use of IfcOffsetCurve2D as underlying AxisCurve supports this concept.
    ///   Formal Propositions:
    ///   WR1   :   The dimensionality of the grid axis is 2.  
    ///   WR2   :   The IfcGridAxis needs to be used by exactly one of the three attributes of IfcGrid: 
    ///   UAxes 
    ///   VAxes 
    ///   WAxes 
    ///   i.e. it can only refer to a single instance of IfcGrid in one of the three list of axes.
    /// </remarks>
    [IfcPersistedEntity, Serializable]
    public class GridAxis
    {
        /// <summary>
        ///   Optional. The tag or name for this grid axis.
        /// </summary>
        public IfcLabel AxisTag()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Underlying curve which provides the geometry for this grid axis.
        /// </summary>
        public IfcCurve AxisCurve()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Defines whether the original sense of curve is used or whether it is reversed in the context of the grid axis.
        /// </summary>
        public bool SameSense()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   The reference to a set of 's, that connect other grid axes to this grid axis.
        /// </summary>
        public VirtualGridIntersection HasIntersections()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Inverse. If provided, the IfcGridAxis is part of the WAxes of IfcGrid.
        /// </summary>
        public void PartOfW()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Inverse. If provided, the IfcGridAxis is part of the VAxes of IfcGrid.
        /// </summary>
        public void PartOfV()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Inverse. If provided, the IfcGridAxis is part of the UAxes of IfcGrid.
        /// </summary>
        public void PartOfU()
        {
            throw new NotImplementedException();
        }
    }
}