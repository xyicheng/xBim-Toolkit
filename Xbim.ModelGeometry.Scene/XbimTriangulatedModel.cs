using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.ModelGeometry.Scene
{
    public class XbimTriangulatedModel
    {
        public XbimTriangulatedModel(byte[] triangles, int representationLabel, int surfaceStylelabel)
        {
            Triangles = triangles;
            RepresentationLabel = representationLabel;
            SurfaceStyleLabel = surfaceStylelabel;
        }

        public byte[] Triangles { get; set; }

        public int RepresentationLabel { get; set; }

        public int SurfaceStyleLabel { get; set; }
    }
}
