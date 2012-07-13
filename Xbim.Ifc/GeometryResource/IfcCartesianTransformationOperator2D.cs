#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    IfcCartesianTransformationOperator2D.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System;
using System.Collections.Generic;
using Xbim.XbimExtensions;
using WPoint = System.Windows.Point;
using WVector = System.Windows.Vector;
using WMatrix = System.Windows.Media.Matrix;

#endregion

namespace Xbim.Ifc.GeometryResource
{
    /// <summary>
    ///   A Cartesian transformation operator 2d defines a geometric transformation in two-dimensional space composed of translation, rotation, mirroring and uniform scaling.
    /// </summary>
    /// <remarks>
    ///   Definition from ISO/CD 10303-42:1992: 
    ///   A Cartesian transformation operator 2d defines a geometric transformation in two-dimensional space composed of translation, rotation, mirroring and uniform scaling. 
    ///   The list of normalised vectors u defines the columns of an orthogonal matrix T. 
    ///   These vectors are computed from the direction attributes axis1 and axis2 by the base axis function. If |T|= -1, the transformation includes mirroring. 
    ///   NOTE: Corresponding STEP entity : cartesian_transformation_operator_2d, please refer to ISO/IS 10303-42:1994, p. 36 for the final definition of the formal standard. 
    ///   HISTORY: New entity in IFC Release 2x.
    /// </remarks>
    [IfcPersistedEntityAttribute, Serializable]
    public class IfcCartesianTransformationOperator2D : IfcCartesianTransformationOperator
    {
        #region Part 21 Step file Parse routines

        /// <summary>
        ///   The list of mutually orthogonal, normalised vectors defining the transformation matrix T. 
        ///   They are derived from the explicit attributes Axis1 and Axis2 in that order.
        /// </summary>
        public List<IfcDirection> U
        {
            get { return BaseAxis(); }
        }

        #endregion

        public List<IfcDirection> BaseAxis()
        {
            List<IfcDirection> u = new List<IfcDirection>(2);
            if (Axis1 != null)
            {
                WVector d1 = Axis1.WVector();
                d1.Normalize();
                u.Add(new IfcDirection(d1.X, d1.Y));
                u.Add(new IfcDirection(-d1.Y, d1.X)); //OrthogonalComplement of D1
                if (Axis2 != null)
                {
                    double factor = WVector.Multiply(Axis2.WVector(), u[1].WVector());
                    if (factor < 0)
                    {
                        u[1][0] = -u[1][0];
                        u[1][1] = -u[1][1];
                    }
                }
            }
            else
            {
                if (Axis2 != null)
                {
                    WVector d1 = Axis2.WVector();
                    d1.Normalize();
                    u.Add(new IfcDirection(-d1.Y, d1.X)); //OrthogonalComplement of D1
                    u.Add(new IfcDirection(d1.X, d1.Y));
                    u[0][0] = -u[0][0];
                    u[0][1] = -u[0][1];
                }
                else
                {
                    u.Add(new IfcDirection(1, 0));
                    u.Add(new IfcDirection(0, 1));
                }
            }
            return u;
        }

        public override string WhereRule()
        {
            string baseErr = base.WhereRule();
            if (Dim != 2)
                baseErr +=
                    "WR1 CartesianTransformationOperator2D : The coordinate space dimensionality of this entity shall be 2\n";
            if (Axis1 != null && Axis1.Dim != 2)
                baseErr +=
                    "WR2 CartesianTransformationOperator2D : The inherited Axis1 should have (if given) the dimensionality of 2\n";
            if (Axis2 != null && Axis2.Dim != 2)
                baseErr +=
                    "WR2 CartesianTransformationOperator2D : The inherited Axis2 should have (if given) the dimensionality of 2\n";
            return baseErr;
        }
    }
}