using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common;
using Xbim.Common.Geometry;
using Xbim.ModelGeometry.Scene;

namespace Xbim.ModelGeometry.Converter
{
    public class XbimShapeMap : IXbimGeometryModel
    {
        public XbimMatrix3D Placement;
        private int _shapeMapLabel;
        private int _shapeLabel;
    
        public XbimShapeMap(int map, int shape)
        {
            this._shapeMapLabel = map;
            this._shapeLabel = shape;
        }

        public int ShapeMapLabel
        {
            get
            {
                return _shapeMapLabel;
            }
        }

        int IXbimGeometryModel.RepresentationLabel
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        int IXbimGeometryModel.SurfaceStyleLabel
        {
            get
            {
                throw new NotImplementedException();
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        XbimMatrix3D IXbimGeometryModel.Transform
        {
            get { throw new NotImplementedException(); }
        }

        XbimRect3D IXbimGeometryModel.GetBoundingBox()
        {
            throw new NotImplementedException();
        }

        XbimRect3D IXbimGeometryModel.GetAxisAlignedBoundingBox()
        {
            throw new NotImplementedException();
        }

        bool IXbimGeometryModel.IsMap
        {
            get { throw new NotImplementedException(); }
        }

        XbimTriangulatedModelCollection IXbimGeometryModel.Mesh(double deflection)
        {
            throw new NotImplementedException();
        }

        XbimMeshFragment IXbimGeometryModel.MeshTo(IXbimMeshGeometry3D mesh3D, Ifc2x3.Kernel.IfcProduct product, XbimMatrix3D transform, double deflection)
        {
            throw new NotImplementedException();
        }

        string IXbimGeometryModel.WriteAsString(XbimModelFactors modelFactors)
        {
            throw new NotImplementedException();
        }

        double IXbimGeometryModel.Volume
        {
            get { throw new NotImplementedException(); }
        }


        public XbimPoint3D CentreOfMass
        {
            get { throw new NotImplementedException(); }
        }
    }
}
