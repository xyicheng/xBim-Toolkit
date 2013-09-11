using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common.Geometry;
using Xbim.Ifc2x3.Kernel;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.ModelGeometry.Scene
{
    public interface IXbimGeometryModel
    {
      
        int RepresentationLabel { get; set; }

        int SurfaceStyleLabel { get; set; }

        XbimMatrix3D Transform { get; }

        XbimRect3D GetBoundingBox();

        List<XbimTriangulatedModel> Mesh();

        bool IsMap { get;}

        List<XbimTriangulatedModel> Mesh(bool p, double deflection);

        double Volume { get; }
    }
}
