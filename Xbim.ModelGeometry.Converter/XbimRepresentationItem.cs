using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Common;
using Xbim.ModelGeometry.Scene;

namespace Xbim.ModelGeometry.Converter
{
    public class XbimRepresentationItem : IXbimGeometryModel
    {
        private int representationItemLabel;
      
        public XbimRepresentationItem(int p)
        {

            this.representationItemLabel = p;
        }

        public int RepresentationItemLabel
        {
            get
            {
                return representationItemLabel;
            }
        }

        int IXbimGeometryModel.RepresentationLabel
        {
            get
            {
                return representationItemLabel;
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

        Common.Geometry.XbimMatrix3D IXbimGeometryModel.Transform
        {
            get { throw new NotImplementedException(); }
        }

        Common.Geometry.XbimRect3D IXbimGeometryModel.GetBoundingBox()
        {
            throw new NotImplementedException();
        }

        Common.Geometry.XbimRect3D IXbimGeometryModel.GetAxisAlignedBoundingBox()
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

        XbimMeshFragment IXbimGeometryModel.MeshTo(IXbimMeshGeometry3D mesh3D, Ifc2x3.Kernel.IfcProduct product, Common.Geometry.XbimMatrix3D transform, double deflection, short modelid)
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


        public Common.Geometry.XbimPoint3D CentreOfMass
        {
            get { throw new NotImplementedException(); }
        }
    }
}
