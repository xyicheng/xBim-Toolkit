#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.ModelGeometry.Scene
// Filename:    Matrix3DExtensions.cs
// Published:   01, 2012
// Last Edited: 9:29 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.IO;
using System.Windows.Media.Media3D;

#endregion

namespace Xbim.ModelGeometry.Scene
{
    public static class Matrix3DExtensions
    {
        public static void Write(this Matrix3D m3D, BinaryWriter strm)
        {
            if (m3D.IsIdentity)
            {
                strm.Write('I');
            }
            else
            {
                strm.Write('M');
                strm.Write(m3D.M11);
                strm.Write(m3D.M12);
                strm.Write(m3D.M13);
                strm.Write(m3D.M14);
                strm.Write(m3D.M21);
                strm.Write(m3D.M22);
                strm.Write(m3D.M23);
                strm.Write(m3D.M24);
                strm.Write(m3D.M31);
                strm.Write(m3D.M32);
                strm.Write(m3D.M33);
                strm.Write(m3D.M34);
                strm.Write(m3D.OffsetX);
                strm.Write(m3D.OffsetY);
                strm.Write(m3D.OffsetZ);
                strm.Write(m3D.M44);
            }
        }

        public static Matrix3D Read(this Matrix3D m3D, BinaryReader strm)
        {
            char test = strm.ReadChar();
            if (test == 'I')
            {
                m3D.SetIdentity();
            }
            else
            {
                m3D.M11 = strm.ReadDouble();
                m3D.M12 = strm.ReadDouble();
                m3D.M13 = strm.ReadDouble();
                m3D.M14 = strm.ReadDouble();
                m3D.M21 = strm.ReadDouble();
                m3D.M22 = strm.ReadDouble();
                m3D.M23 = strm.ReadDouble();
                m3D.M24 = strm.ReadDouble();
                m3D.M31 = strm.ReadDouble();
                m3D.M32 = strm.ReadDouble();
                m3D.M33 = strm.ReadDouble();
                m3D.M34 = strm.ReadDouble();
                m3D.OffsetX = strm.ReadDouble();
                m3D.OffsetY = strm.ReadDouble();
                m3D.OffsetZ = strm.ReadDouble();
                m3D.M44 = strm.ReadDouble();
            }
            return m3D;
        }
    }
}