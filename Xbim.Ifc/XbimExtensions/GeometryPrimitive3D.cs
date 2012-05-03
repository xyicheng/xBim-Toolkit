#region XbimHeader

// The eXtensible Building Information Modelling (xBIM) Toolkit
// Solution:    XbimComplete
// Project:     Xbim.Ifc
// Filename:    GeometryPrimitive3D.cs
// Published:   01, 2012
// Last Edited: 9:04 AM on 20 12 2011
// (See accompanying copyright.rtf)

#endregion

#region Directives

using System.Windows.Media.Media3D;

#endregion

namespace Xbim.XbimExtensions
{
    public class GeometryPrimitive3D : PrimitiveModel3D
    {
        private MeshGeometry3D _geometry;
        private Material _material;
        private Material _backgroundMaterial;

        public Material BackgroundMaterial
        {
            get { return _backgroundMaterial; }
            set { _backgroundMaterial = value; }
        }

        public Material Material
        {
            get { return _material; }
            set { _material = value; }
        }

        public MeshGeometry3D Geometry
        {
            get { return _geometry; }
            set { _geometry = value; }
        }

        /// <summary>
        ///   Creates a visual GeometryModel3D and populates with the NonVisualGeometryModel3D geometry and Material
        /// </summary>
        public override Model3D ToVisualModel3D()
        {
            GeometryModel3D gm = new GeometryModel3D();
            gm.Geometry = _geometry;
            gm.BackMaterial = _backgroundMaterial;
            gm.Material = _material;
            return gm;
        }

        /// <summary>
        ///   Freezes the underlying Geometry and Material. nb this is a non visual class and cannot itself be frozen
        /// </summary>
        public override void Freeze()
        {
            if (_geometry != null && !_geometry.IsFrozen) _geometry.Freeze();
            // if (_material != null && !_material.IsFrozen) _material.Freeze();
        }
    }
}