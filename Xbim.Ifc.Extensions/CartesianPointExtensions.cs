#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc.Extensions
// Filename:    CartesianPointExtensions.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using Xbim.Ifc.GeometryResource;

#endregion

namespace Xbim.Ifc.Extensions
{
    public static class CartesianPointExtensions
    {
        public static IfcCartesianPoint CrossProduct(this IfcCartesianPoint a, IfcCartesianPoint b)
        {
            return new IfcCartesianPoint(a.Y*b.Z - a.Z*b.Y, a.Z*b.X - a.X*b.Z, a.X*b.Y - a.Y*b.X);
        }

        public static void Add(this IfcCartesianPoint a, IfcCartesianPoint b)
        {
            a.SetXYZ(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }
    }
}