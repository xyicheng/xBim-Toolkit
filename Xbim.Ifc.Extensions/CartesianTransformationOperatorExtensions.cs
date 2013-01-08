#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc.Extensions
// Filename:    CartesianTransformationOperatorExtensions.cs
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
    public static class CartesianTransformationOperatorExtensions
    {
        public static Matrix3D ToMatrix3D(this IfcCartesianTransformationOperator ct, Dictionary<int, Object> maps = null)
        {
            if (ct is IfcCartesianTransformationOperator3DnonUniform)
               return ((IfcCartesianTransformationOperator3DnonUniform) ct).ToMatrix3D(maps);
            else if (ct is IfcCartesianTransformationOperator3D)
                return ((IfcCartesianTransformationOperator3D) ct).ToMatrix3D(maps);
            else throw new ArgumentException("ToMatrix3D", "Unsupported CartesianTransformationOperator3D");
            
        }

        /// <summary>
        ///   Converts a CartesianTransformationOperator2D to a System.Windows.Media.Matrix
        /// </summary>
        /// <param name = "ct"></param>
        /// <returns></returns>
        public static Matrix ToMatrix(this IfcCartesianTransformationOperator2D ct)
        {
            Matrix m;
            IfcDirection axis1 = ct.Axis1, axis2 = ct.Axis2;
            double scale = ct.Scale.HasValue ? ct.Scale.Value : 1.0;
            IfcCartesianPoint o = ct.LocalOrigin;
            if (o == null)
                throw new ArgumentNullException("LocationOrigin", "A locationOrigin cannot be null");
            if (axis1 != null)
            {
                WVector d1 = axis1.WVector();
                d1.Normalize();
                m = new Matrix(d1.X, d1.Y, -d1.Y, d1.X, 0, 0);
                if (axis2 != null)
                {
                    double factor = WVector.Multiply(axis2.WVector(), new WVector(-d1.Y, d1.X));
                    if (factor < 0)
                    {
                        m.M21 = -m.M21;
                        m.M22 = -m.M22;
                    }
                }
            }
            else
            {
                if (axis2 != null)
                {
                    WVector d1 = axis2.WVector();
                    d1.Normalize();
                    m = new Matrix(-d1.Y, d1.X, d1.X, d1.Y, 0, 0);
                    m.M11 = -m.M11;
                    m.M12 = -m.M12;
                }
                else
                    m = new Matrix();
            }
            m.Scale(scale, scale);
            m.Translate(o.X, o.Y);
            return m;
        }


        /// <summary>
        ///   Builds a windows Matrix3D from a CartesianTransformationOperator3D
        /// </summary>
        /// <param name = "ct3D"></param>
        /// <returns></returns>
        public static Matrix3D ToMatrix3D(this IfcCartesianTransformationOperator3D ct3D, Dictionary<int, Object> maps = null)
        {
            object transform;
            if (maps!=null && maps.TryGetValue(Math.Abs(ct3D.EntityLabel), out transform)) //already converted it just return cached
                return (Matrix3D)transform;
            Vector3D u3; //Z Axis Direction
            Vector3D u2; //X Axis Direction
            Vector3D u1; //Y axis direction
            if (ct3D.Axis3 != null)
            {
                IfcDirection dir = ct3D.Axis3;
                u3 = new Vector3D(dir.DirectionRatios[0], dir.DirectionRatios[1], dir.DirectionRatios[2]);
                u3.Normalize();
            }
            else
                u3 = new Vector3D(0, 0, 1);
            if (ct3D.Axis1 != null)
            {
                IfcDirection dir = ct3D.Axis1;
                u1 = new Vector3D(dir.DirectionRatios[0], dir.DirectionRatios[1], dir.DirectionRatios[2]);
                u1.Normalize();
            }
            else
            {
                Vector3D defXDir = new Vector3D(1, 0, 0);
                u1 = u3 != defXDir ? defXDir : new Vector3D(0, 1, 0);
            }
            Vector3D xVec = Vector3D.Multiply(Vector3D.DotProduct(u1, u3), u3);
            Vector3D xAxis = Vector3D.Subtract(u1, xVec);
            xAxis.Normalize();

            if (ct3D.Axis2 != null)
            {
                IfcDirection dir = ct3D.Axis2;
                u2 = new Vector3D(dir.DirectionRatios[0], dir.DirectionRatios[1], dir.DirectionRatios[2]);
                u2.Normalize();
            }
            else
                u2 = new Vector3D(0, 1, 0);

            Vector3D tmp = Vector3D.Multiply(Vector3D.DotProduct(u2, u3), u3);
            Vector3D yAxis = Vector3D.Subtract(u2, tmp);
            tmp = Vector3D.Multiply(Vector3D.DotProduct(u2, xAxis), xAxis);
            yAxis = Vector3D.Subtract(yAxis, tmp);
            yAxis.Normalize();
            u2 = yAxis;
            u1 = xAxis;

            Point3D lo = ct3D.LocalOrigin.WPoint3D(); //local origin
            double s = 1;
            if (ct3D.Scale.HasValue)
                s = ct3D.Scale.Value;

            Matrix3D matrix = new Matrix3D(u1.X, u1.Y, u1.Z, 0,
                               u2.X, u2.Y, u2.Z, 0,
                               u3.X, u3.Y, u3.Z, 0,
                               lo.X, lo.Y, lo.Z, 1);
            matrix.Scale(new Vector3D(ct3D.Scl, ct3D.Scl, ct3D.Scl));
            if (maps != null) maps.Add(Math.Abs(ct3D.EntityLabel), matrix);
            return matrix;
        }

        /// <summary>
        ///   Builds a windows Matrix3D from a CartesianTransformationOperator3DnonUniform
        /// </summary>
        /// <param name = "ct3D"></param>
        /// <returns></returns>
        public static Matrix3D ToMatrix3D(this IfcCartesianTransformationOperator3DnonUniform ct3D, Dictionary<int, Object> maps = null)
        {
            object transform;
            if (maps != null && maps.TryGetValue(Math.Abs(ct3D.EntityLabel), out transform)) //already converted it just return cached
                return (Matrix3D)transform;
            Vector3D u3; //Z Axis Direction
            Vector3D u2; //X Axis Direction
            Vector3D u1; //Y axis direction
            if (ct3D.Axis3 != null)
            {
                IfcDirection dir = ct3D.Axis3;
                u3 = new Vector3D(dir.DirectionRatios[0], dir.DirectionRatios[1], dir.DirectionRatios[2]);
                u3.Normalize();
            }
            else
                u3 = new Vector3D(0, 0, 1);
            if (ct3D.Axis1 != null)
            {
                IfcDirection dir = ct3D.Axis1;
                u1 = new Vector3D(dir.DirectionRatios[0], dir.DirectionRatios[1], dir.DirectionRatios[2]);
                u1.Normalize();
            }
            else
            {
                Vector3D defXDir = new Vector3D(1, 0, 0);
                u1 = u3 != defXDir ? defXDir : new Vector3D(0, 1, 0);
            }
            Vector3D xVec = Vector3D.Multiply(Vector3D.DotProduct(u1, u3), u3);
            Vector3D xAxis = Vector3D.Subtract(u1, xVec);
            xAxis.Normalize();

            if (ct3D.Axis2 != null)
            {
                IfcDirection dir = ct3D.Axis2;
                u2 = new Vector3D(dir.DirectionRatios[0], dir.DirectionRatios[1], dir.DirectionRatios[2]);
                u2.Normalize();
            }
            else
                u2 = new Vector3D(0, 1, 0);

            Vector3D tmp = Vector3D.Multiply(Vector3D.DotProduct(u2, u3), u3);
            Vector3D yAxis = Vector3D.Subtract(u2, tmp);
            tmp = Vector3D.Multiply(Vector3D.DotProduct(u2, xAxis), xAxis);
            yAxis = Vector3D.Subtract(yAxis, tmp);
            yAxis.Normalize();
            u2 = yAxis;
            u1 = xAxis;

            Point3D lo = ct3D.LocalOrigin.WPoint3D(); //local origin

            Matrix3D matrix = new Matrix3D(u1.X, u1.Y, u1.Z, 0,
                                           u2.X, u2.Y, u2.Z, 0,
                                           u3.X, u3.Y, u3.Z, 0,
                                           lo.X, lo.Y, lo.Z, 1);
            matrix.Scale(new Vector3D(ct3D.Scl, ct3D.Scl2, ct3D.Scl3));
            if(maps!=null)  maps.Add(Math.Abs(ct3D.EntityLabel), matrix);
            return matrix;
        }
    }
}