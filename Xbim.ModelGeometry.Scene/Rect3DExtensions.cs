#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.ModelGeometry.Scene
// Filename:    Rect3DExtensions.cs
// Published:   01, 2012
// Last Edited: 10:01 AM on 04 01 2012
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.IO;
using System.Windows.Media.Media3D;

#endregion

namespace Xbim.ModelGeometry.Scene
{
    public static class Rect3DExtensions
    {
        public static void Write(this Rect3D rect, BinaryWriter strm)
        {
            if (rect.IsEmpty)
                strm.Write('E');
            else
            {
                strm.Write('R');
                strm.Write(rect.X);
                strm.Write(rect.Y);
                strm.Write(rect.Z);
                strm.Write(rect.SizeX);
                strm.Write(rect.SizeY);
                strm.Write(rect.SizeZ);
            }
        }

        public static Rect3D Read(this Rect3D rect, BinaryReader strm)
        {
            char test = strm.ReadChar();
            if (test == 'E')
                return new Rect3D();
            else
            {
                rect.X = strm.ReadDouble();
                rect.Y = strm.ReadDouble();
                rect.Z = strm.ReadDouble();
                rect.SizeX = strm.ReadDouble();
                rect.SizeY = strm.ReadDouble();
                rect.SizeZ = strm.ReadDouble();
                return rect;
            }
        }
    }
}