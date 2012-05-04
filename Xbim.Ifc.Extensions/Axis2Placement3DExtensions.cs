﻿#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc.Extensions
// Filename:    Axis2Placement3DExtensions.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Windows.Media;
using System.Windows.Media.Media3D;
using Xbim.Ifc.GeometryResource;
using Xbim.XbimExtensions;

#endregion

namespace Xbim.Ifc.Extensions
{
    public static class Axis2Placement3DExtensions
    {
        public static Vector3D ZAxisDirection(this IfcAxis2Placement3D ax3)
        {
            if (ax3.RefDirection != null && ax3.Axis != null)
            {
                Vector3D za = ax3.Axis.WVector3D();
                za.Normalize();
                return za;
            }
            else
                return new Vector3D(0, 0, 1);
        }

        public static Vector3D XAxisDirection(this IfcAxis2Placement3D ax3)
        {
            if (ax3.RefDirection != null && ax3.Axis != null)
            {
                Vector3D xa = ax3.RefDirection.WVector3D();
                xa.Normalize();
                return xa;
            }
            else
                return new Vector3D(1, 0, 0);
        }

        /// <summary>
        ///   Converts an Axis2Placement3D to a windows Matrix3D
        /// </summary>
        /// <param name = "axis3"></param>
        /// <returns></returns>
        public static Matrix3D ToMatrix3D(this IfcAxis2Placement3D axis3)
        {
            if (axis3.RefDirection != null && axis3.Axis != null)
            {
                Vector3D za = axis3.Axis.WVector3D();
                za.Normalize();
                Vector3D xa = axis3.RefDirection.WVector3D();
                xa.Normalize();
                Vector3D ya = Vector3D.CrossProduct(za, xa);
                ya.Normalize();
                return new Matrix3D(xa.X, xa.Y, xa.Z, 0, ya.X, ya.Y, ya.Z, 0, za.X, za.Y, za.Z, 0, axis3.Location.X,
                                    axis3.Location.Y, axis3.Location.Z, 1);
            }
            else
                return new Matrix3D(1, 0, 0, 0, 0, 1, 0, 0, 0, 0, 1, 0, axis3.Location.X, axis3.Location.Y,
                                    axis3.Location.Z, 1);
        }

        /// <summary>
        ///   Converts an Axis2Placement3D to a windows Matrix suitable  to apply to two dimensional coordinates, where the Z coord is 0
        /// </summary>
        /// <param name = "axis3"></param>
        /// <returns></returns>
        public static Matrix ToMatrix(this IfcAxis2Placement3D axis3)
        {
            if (axis3.RefDirection != null && axis3.Axis != null)
            {
                Vector3D za = axis3.Axis.WVector3D();
                za.Normalize();
                Vector3D xa = axis3.RefDirection.WVector3D();
                xa.Normalize();
                Vector3D ya = Vector3D.CrossProduct(za, xa);
                return new Matrix(xa.X, xa.Y, ya.X, ya.Y, axis3.Location.X, axis3.Location.Y);
            }
            else
                return new Matrix(1, 0, 0, 1, axis3.Location.X, axis3.Location.Y);
        }

        public static void SetNewLocation(this IfcAxis2Placement3D axis3, double x, double y, double z)
        {
            IModel model = ModelManager.ModelOf(axis3);
            IfcCartesianPoint location = model.New<IfcCartesianPoint>();
            location.X = x;
            location.Y = y;
            location.Z = z;
            axis3.Location = location;
        }


        /// <summary>
        ///   Sets new directions of the axes. Direction vectors are automaticaly normalized.
        /// </summary>
        /// <param name = "axis3"></param>
        /// <param name = "xAxisDirectionX"></param>
        /// <param name = "xAxisDirectionY"></param>
        /// <param name = "xAxisDirectionZ"></param>
        /// <param name = "zAxisDirectionX"></param>
        /// <param name = "zAxisDirectionY"></param>
        /// <param name = "zAxisDirectionZ"></param>
        public static void SetNewDirectionOf_XZ(this IfcAxis2Placement3D axis3, double xAxisDirectionX,
                                                double xAxisDirectionY, double xAxisDirectionZ, double zAxisDirectionX,
                                                double zAxisDirectionY, double zAxisDirectionZ)
        {
            IModel model = ModelManager.ModelOf(axis3);
            IfcDirection zDirection = model.New<IfcDirection>();
            zDirection.DirectionRatios[0] = zAxisDirectionX;
            zDirection.DirectionRatios[1] = zAxisDirectionY;
            zDirection.DirectionRatios[2] = zAxisDirectionZ;
            zDirection.Normalise();
            axis3.Axis = zDirection;

            IfcDirection xDirection = model.New<IfcDirection>();
            xDirection.DirectionRatios[0] = xAxisDirectionX;
            xDirection.DirectionRatios[1] = xAxisDirectionY;
            xDirection.DirectionRatios[2] = xAxisDirectionZ;
            xDirection.Normalise();
            axis3.RefDirection = xDirection;
        }
    }
}