#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    XbimVertex.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Runtime.InteropServices;
using Xbim.XbimExtensions.Interfaces;

#endregion

namespace Xbim.XbimExtensions
{
    [StructLayout(LayoutKind.Sequential)]
    public struct XbimVertex : IVertex3D
    {
        public double X;
        public double Y;
        public double Z;

        public XbimVertex(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        #region IVertex3D Members

        double IVertex3D.X
        {
            get { return X; }
        }

        double IVertex3D.Y
        {
            get { return Y; }
        }

        double IVertex3D.Z
        {
            get { return Z; }
        }

        #endregion
    }
}