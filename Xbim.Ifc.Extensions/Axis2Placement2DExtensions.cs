#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc.Extensions
// Filename:    Axis2Placement2DExtensions.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.Ifc2x3.GeometryResource;
using WVector = System.Windows.Vector;

#endregion

namespace Xbim.Ifc2x3.Extensions
{
    public static class Axis2Placement2DExtensions
    {

        static public IfcAxis2Placement2D Create(this IfcAxis2Placement2D ax, IfcCartesianPoint centre, IfcDirection xAxisDirection)
        {
            IfcAxis2Placement2D ax2 = new IfcAxis2Placement2D()
            {
                RefDirection = xAxisDirection,
                Location = centre
            };
            return ax2;
        }

        public static Matrix ToMatrix(this IfcAxis2Placement2D axis2)
        {
            if (axis2.RefDirection != null)
            {
                WVector v = axis2.RefDirection.WVector();
                v.Normalize();
                return new Matrix(v.X, v.Y, v.Y, v.X, axis2.Location.X, axis2.Location.Y);
            }
            else
                return new Matrix(1, 0, 0, 1, axis2.Location.X, axis2.Location.Y);
        }

        public static Matrix3D ToMatrix3D(this IfcAxis2Placement2D axis2, Dictionary<int, Object> maps = null)
        {
            object transform;
            if (maps != null && maps.TryGetValue(Math.Abs(axis2.EntityLabel), out transform)) //already converted it just return cached
                return (Matrix3D)transform;
            if (axis2.RefDirection != null)
            {
                WVector v = axis2.RefDirection.WVector();
                v.Normalize();
                transform = new Matrix3D(v.X, v.Y, 0, 0, v.Y, v.X, 0, 0, 0, 0, 1, 0, axis2.Location.X, axis2.Location.Y, 0, 1);
            }
            else
                transform = new Matrix3D(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, axis2.Location.X, axis2.Location.Y,
                                    axis2.Location.Z, 1);
            if (maps != null) maps.Add(Math.Abs(axis2.EntityLabel), transform);
            return (Matrix3D)transform;
        }
    }
}
