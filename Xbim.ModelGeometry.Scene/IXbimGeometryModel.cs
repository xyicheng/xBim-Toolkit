using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common;
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
        XbimRect3D GetAxisAlignedBoundingBox();
        bool IsMap { get;}
        /// <summary>
        /// Creates a mesh with the specified deflection on curve interpolation, use model.ModelFactors to get the default deflection for the model
        /// </summary>
        /// <param name="deflection"></param>
        /// <returns></returns>
        XbimTriangulatedModelCollection Mesh(double deflection);
        /// <summary>
        /// Write the geometry as a triangulated mesh onto the mesh geometry and returns the details about the fragment on the mesh
        /// </summary>
        /// <param name="mesh3D">The mesh to write the geometry on to</param>
        /// <param name="product">The product that the geometry represents</param>
        /// <param name="transform">Tranforms all point before writing to the meshs</param>
        /// <param name="deflection">The tangental deflection to use for curved surfaces</param>
        /// <returns>The fragment of the mesh that has been added</returns>
        XbimMeshFragment MeshTo(IXbimMeshGeometry3D mesh3D, IfcProduct product, XbimMatrix3D transform, double deflection);
        /// <summary>
        /// Returns a string containing the geometry in PLY format
        /// </summary>
        /// <returns></returns>
        string WriteAsString(XbimModelFactors modelFactors);
        double Volume { get; }

        XbimPoint3D CentreOfMass { get; }
    }
}
