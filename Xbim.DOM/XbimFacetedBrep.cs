using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.GeometricModelResource;
using Xbim.XbimExtensions;
using Xbim.Ifc.TopologyResource;
using Xbim.Ifc.GeometryResource;
using Xbim.Ifc.Extensions;

namespace Xbim.DOM
{
    
    public class XbimFacetedBrep: IXbimGeometry, Xbim.DOM.IBimFacetedBrep
    {
        private IfcFacetedBrep _ifcFacetedBrep;
        private XbimDocument _document;
        
        private XbimSet<IfcFace> _faces;
        private XbimListUnique<IfcCartesianPoint> _newPoints;
        private XbimFaceTypeEnum _actualFaceType;
        private IfcFace _face;

        public XbimDocument Document { get { return _document; } }
        internal IfcFacetedBrep IfcFacetedBrep { get { return _ifcFacetedBrep; } }

        internal XbimFacetedBrep(XbimDocument document) 
        {
            _document = document;
            _ifcFacetedBrep = _document.Model.New<IfcFacetedBrep>();

            IfcClosedShell outer = _document.Model.New<IfcClosedShell>();
            _ifcFacetedBrep.Outer = outer;

            //if (outer.CfsFaces == null) outer.CfsFaces = new XbimSet<IfcFace>();
            _faces = outer.CfsFaces;
        }

        public void AddPolyLoopBoundedFace(bool orientation)
        {
            AddFace(orientation, XbimFaceTypeEnum.POLYLOOP_BOUNDED);
        }

        public void AddPolyLoopBoundedFace()
        {
            AddFace(true, XbimFaceTypeEnum.POLYLOOP_BOUNDED);
        }

        private IfcFace AddFace(bool orientation, XbimFaceTypeEnum type)
        {
            _actualFaceType = type;

            _face = GetFace(type);
            _faces.Add_Reversible(_face);

            //if (_face.Bounds == null) _face.Bounds = new XbimSet<IfcFaceBound>();

            IfcFaceBound faceBound = _document.Model.New<IfcFaceBound>();
            _face.Bounds.Add_Reversible(faceBound);

            faceBound.Orientation = orientation;
            IfcPolyLoop polyLoop = _document.Model.New<IfcPolyLoop>();
            faceBound.Bound = polyLoop;

            //if (polyLoop.Polygon == null) polyLoop.Polygon = new XbimListUnique<IfcCartesianPoint>();
            _newPoints = polyLoop.Polygon;

            return _face;
        }

        public void AddTriangleFace(XbimXYZ point1, XbimXYZ point2, XbimXYZ point3)
        {
            AddFace(true, XbimFaceTypeEnum.TRIANGLE);
            AddFaceBoundPoint(point1.X, point1.Y, point1.Z);
            AddFaceBoundPoint(point2.X, point2.Y, point2.Z);
            AddFaceBoundPoint(point3.X, point3.Y, point3.Z);
        }

        public void AddFaceBoundPoint(double X, double Y, double Z)
        {
            //ensure that adding of this point is appropriate for the type of face
            //if (_actualFaceType != FaceType.POLYLOOP_BOUNDED)
            //    throw new Exception("AddPolyLoopPoint can be used only for PolyloopBoundedFace.");

            IfcCartesianPoint point = _document.Model.New<IfcCartesianPoint>();
            point.Add(X);
            point.Add(Y);
            point.Add(Z);

            _newPoints.Add_Reversible(point);
        }

        public void AddPlaneFace(double position_X, double position_Y, double position_Z, double normal_X, double normal_Y, double normal_Z, double planeVector_X, double planeVector_Y, double planeVector_Z)
        {
            IfcFaceSurface face = AddFace(true, XbimFaceTypeEnum.PLANE_SURFACE) as IfcFaceSurface;
            if (face == null) throw new Exception("Wrong initialization of the object.");

            face.Surface = _document.Model.New<IfcPlane>();
            IfcPlane plane = face.Surface as IfcPlane;

            plane.Position = _document.Model.New<IfcAxis2Placement3D>();
            plane.Position.SetNewLocation(position_X, position_Y, position_Z);
            plane.Position.SetNewDirectionOf_XZ(planeVector_X, planeVector_Y, planeVector_Z, normal_X, normal_Y, normal_Z);

        }

        public void AddPlaneFace(bool orientation, double position_X, double position_Y, double position_Z, double normal_X, double normal_Y, double normal_Z, double planeVector_X, double planeVector_Y, double planeVector_Z)
        {
            IfcFaceSurface face = AddFace(orientation, XbimFaceTypeEnum.PLANE_SURFACE) as IfcFaceSurface;
            if (face == null) throw new Exception("Wrong initialization of the object.");

            face.Surface = _document.Model.New<IfcPlane>();
            IfcPlane plane = face.Surface as IfcPlane;

            plane.Position = _document.Model.New<IfcAxis2Placement3D>();
            plane.Position.SetNewLocation(position_X, position_Y, position_Z);
            plane.Position.SetNewDirectionOf_XZ(planeVector_X, planeVector_Y, planeVector_Z, normal_X, normal_Y, normal_Z);
        }

        public void AddInnerLoop(bool orientation)
        {
            IfcFaceBound faceBound = _document.Model.New<IfcFaceBound>();
            _face.Bounds.Add_Reversible(faceBound);

            faceBound.Orientation = orientation;
            IfcPolyLoop polyLoop = _document.Model.New<IfcPolyLoop>();
            faceBound.Bound = polyLoop;

            //if (polyLoop.Polygon == null) polyLoop.Polygon = new XbimListUnique<IfcCartesianPoint>();
            _newPoints = polyLoop.Polygon;
        }

        public void AddInnerLoop()
        {
            AddInnerLoop(true);
        }


        private IfcFace GetFace(XbimFaceTypeEnum type)
        {
            switch (type)
            {
                case XbimFaceTypeEnum.POLYLOOP_BOUNDED: return _document.Model.New<IfcFace>();
                case XbimFaceTypeEnum.PLANE_SURFACE: return _document.Model.New<IfcFaceSurface>();
                case XbimFaceTypeEnum.TRIANGLE: return _document.Model.New<IfcFace>();
                default:
                    return _document.Model.New<IfcFace>();
            }
        }

        public IfcGeometricRepresentationItem GetIfcGeometricRepresentation()
        {
            return _ifcFacetedBrep;
        }

    }

    internal enum XbimFaceTypeEnum
    {
        POLYLOOP_BOUNDED,
        PLANE_SURFACE,
        TRIANGLE
    }
     
}
