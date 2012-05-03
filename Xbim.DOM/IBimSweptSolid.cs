using System;
namespace Xbim.DOM
{
    public interface IBimSweptSolid: IBimGeometry
    {
        void AddInnerCurves();
        void AddProfileCurveCircle(IBimAxis2Placement3D placement, double radius);
        void AddProfileCurveCircleSegment(IBimAxis2Placement3D placement, XbimXYZ startPoint, XbimXYZ endPoint, double radius);
        void AddProfileCurveEllipse(IBimAxis2Placement3D position, double semiAxis1, double semiAxis2);
        void AddProfileCurveEllipseSegment(IBimAxis2Placement3D position, double semiAxis1, double semiAxis2, XbimXYZ startPoint, XbimXYZ endPoint);
        void AddProfileCurveLine(XbimXYZ start, XbimXYZ end);
        XbimDocument Document { get; }
        Xbim.Ifc.GeometryResource.IfcGeometricRepresentationItem GetIfcGeometricRepresentation();
    }
}
