using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.XbimExtensions
{
    public enum XbimGeometryType : byte
    {
        Undefined = 0x0,
        BoundingBox = 0x01,
        MultipleBoundingBox = 0x02,
        TriangulatedMesh = 0x03
    }
}
