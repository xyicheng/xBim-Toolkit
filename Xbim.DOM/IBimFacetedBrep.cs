using System;
namespace Xbim.DOM
{
    public interface IBimFacetedBrep : IBimGeometry
    {
        void AddFaceBoundPoint(double X, double Y, double Z);
        void AddInnerLoop();
        void AddInnerLoop(bool orientation);
        void AddPlaneFace(bool orientation, double position_X, double position_Y, double position_Z, double normal_X, double normal_Y, double normal_Z, double planeVector_X, double planeVector_Y, double planeVector_Z);
        void AddPlaneFace(double position_X, double position_Y, double position_Z, double normal_X, double normal_Y, double normal_Z, double planeVector_X, double planeVector_Y, double planeVector_Z);
        void AddPolyLoopBoundedFace();
        void AddPolyLoopBoundedFace(bool orientation);
        void AddTriangleFace(XbimXYZ point1, XbimXYZ point2, XbimXYZ point3);
        Xbim.Ifc.GeometryResource.IfcGeometricRepresentationItem GetIfcGeometricRepresentation();
    }
}
