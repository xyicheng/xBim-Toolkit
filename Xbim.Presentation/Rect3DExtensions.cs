using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Media3D;
using Xbim.Common.Geometry;

namespace Xbim.Presentation
{
    public static  class Rect3DExtensions
    {
        public static XbimRect3D ToXbimRect3D(this Rect3D r3D)
        {
            return new XbimRect3D((float)r3D.X, (float)r3D.Y, (float)r3D.Z, (float)r3D.SizeX, (float)r3D.SizeY, (float)r3D.SizeZ);
        }
    }
}
