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
      
        bool IsMap { get;}
        /// <summary>
        /// Creates a mesh with the specified deflection on curve interpolation, use model.ModelFactors to get the default deflection for the model
        /// </summary>
        /// <param name="deflection"></param>
        /// <returns></returns>
        XbimTriangulatedModelCollection Mesh(double deflection);

        double Volume { get; }
    }
}
