using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.GeometryResource;

namespace Xbim.DOM
{
    public class XbimSurfaceCurveSweptAreaSolid : XbimSweptAreaSolid
    {
        private IfcSurfaceCurveSweptAreaSolid IfcSurfaceCurveSweptAreaSolid { get { return IfcSweptAreaSolid as IfcSurfaceCurveSweptAreaSolid; } }

        internal XbimSurfaceCurveSweptAreaSolid(XbimDocument document)
            : base(document)
        {
            BaseInit<IfcSurfaceCurveSweptAreaSolid>();
            throw new NotImplementedException("IfcSurfaceCurveSweptAreaSolid is not implemented");
        }

        public void DefineReferenceSurfacePlane(XbimAxis2Placement3D position)
        {
            IfcPlane plane = Document.Model.New<IfcPlane>(pla => pla.Position = position._ifcAxis2Placement);
            // IfcSurfaceCurveSweptAreaSolid.ReferenceSurface = plane;
        }
    }
}
