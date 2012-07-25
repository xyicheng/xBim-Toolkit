using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.GeometricModelResource;
using Xbim.Ifc2x3.TopologyResource;
using Xbim.Ifc2x3.GeometryResource;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.Ifc2x3.MeasureResource;
using System.Diagnostics;
using Xbim.Ifc2x3.ProfileResource;

namespace Xbim.DOM
{
    public class XbimFacetedBrepTopo<VertexRef, EdgeRef, FaceRef> : IXbimGeometry
    {
        private IfcFacetedBrep _ifcFacetedBrep;
        private XbimDocument _document;
        private int tempInt;
        public XbimDocument Document { get { return _document; } }
        private Dictionary<VertexRef, IfcVertex> _vertices;
        private Dictionary<EdgeRef, IfcEdge> _edges;
        private Dictionary<FaceRef, IfcFace> _faces;

        internal IfcFacetedBrep IfcFacetedBrep { get { return _ifcFacetedBrep; } }

        internal XbimFacetedBrepTopo(XbimDocument document) 
        {
            _document = document;
            _ifcFacetedBrep = _document.Model.New<IfcFacetedBrep>();

            if (_ifcFacetedBrep.Outer == null) _ifcFacetedBrep.Outer = _document.Model.New<IfcClosedShell>();
            //if (_ifcFacetedBrep.Outer.CfsFaces == null) _ifcFacetedBrep.Outer.CfsFaces = new XbimExtensions.XbimSet<IfcFace>();

            _vertices = new Dictionary<VertexRef, IfcVertex>();
            _edges = new Dictionary<EdgeRef, IfcEdge>();
            _faces = new Dictionary<FaceRef, IfcFace>();
            tempInt = 0;
        }

        public Ifc2x3.GeometryResource.IfcGeometricRepresentationItem GetIfcGeometricRepresentation()
        {
            return _ifcFacetedBrep;
        }

        public void AddVertex(VertexRef externalRef, double X, double Y, double Z)
        {
            if (_vertices.ContainsKey(externalRef)) return;  //if the reference already exists, no vertex is added.

            IfcCartesianPoint point = _document.Model.New<IfcCartesianPoint>();
            point.SetXYZ(X, Y, Z);
            IfcVertex vertex = _document.Model.New<IfcVertexPoint>();
            (vertex as IfcVertexPoint).VertexGeometry = point;

            _vertices.Add(externalRef, vertex);
        }

        public void AddVertex(VertexRef externalRef, XbimXYZ point)
        {
            AddVertex(externalRef, point.X, point.Y, point.Z);
        }

        public void AddEdge(EdgeRef externalRefEdge, VertexRef externalRefStartVertex, VertexRef externalRefEndVertex, bool withCurveGeometry)
        {
            if (_edges.ContainsKey(externalRefEdge)) return;  //if the reference already exists, no edge is added.

            IfcVertex start = null;
            _vertices.TryGetValue(externalRefStartVertex, out start);
            if (start == null) throw new Exception("Start vertex must be defined befor the edge is constructed.");

            IfcVertex end = null;
            _vertices.TryGetValue(externalRefEndVertex, out end);
            if (end == null) throw new Exception("End vertex must be defined befor the edge is constructed.");

            IfcEdge edge = null;
            if (withCurveGeometry)
            {
                edge = _document.Model.New<IfcEdgeCurve>();
            }
            else
            {
                edge = _document.Model.New<IfcEdge>();
            }
            edge.EdgeStart = start;
            edge.EdgeEnd = end;

            _edges.Add(externalRefEdge, edge);
        }

        public void SetEdgeGeometry_toLine(EdgeRef externalRefEdge, XbimXYZ position, XbimXYZ direction, bool sameSenseOfLineAndEdge)
        {
            IfcEdgeCurve edgeCurve = GetEdgeCurve(externalRefEdge);

            IfcCartesianPoint pos = position.CreateIfcCartesianPoint(_document);
            IfcDirection vertorDirection = direction.CreateIfcDirection(_document);
            IfcVector vector = _document.Model.New<IfcVector>(vct => { vct.Magnitude = 1; vct.Orientation = vertorDirection; });

            IfcLine line = _document.Model.New<IfcLine>(ln => { ln.Dir = vector; ln.Pnt = pos; });
            edgeCurve.EdgeGeometry = line;
            edgeCurve.SameSense = sameSenseOfLineAndEdge;
        }

        public void SetEdgeGeometry_toLine(EdgeRef externalRefEdge, VertexRef pointRef, XbimXYZ direction, bool sameSenseOfLineAndEdge)
        {
            IfcEdgeCurve edgeCurve = GetEdgeCurve(externalRefEdge);

            IfcVertex start = null;
            _vertices.TryGetValue(pointRef, out start);
            if (start == null) throw new Exception("Start vertex must be defined befor the edge is constructed.");

            IfcCartesianPoint pos = (start as IfcVertexPoint).VertexGeometry as IfcCartesianPoint;
            IfcDirection vertorDirection = direction.CreateIfcDirection(_document);
            IfcVector vector = _document.Model.New<IfcVector>(vct => { vct.Magnitude = 1; vct.Orientation = vertorDirection; });

            IfcLine line = _document.Model.New<IfcLine>(ln => { ln.Dir = vector; ln.Pnt = pos; });
            edgeCurve.EdgeGeometry = line;
            edgeCurve.SameSense = sameSenseOfLineAndEdge;
        }

        public void SetEdgeGeometry_toCircle(EdgeRef externalRefEdge, XbimAxis2Placement3D placement, double radius, bool sameSenseOfCircleAndEdge)
        {
            IfcEdgeCurve edgeCurve = GetEdgeCurve(externalRefEdge);
            if (radius < 0) throw new Exception("Negative radius is not allowed.");
            IfcAxis2Placement axis2placement = placement._ifcAxis2Placement;

            IfcCircle circle = _document.Model.New<IfcCircle>(cr => { cr.Position = axis2placement; cr.Radius = radius; });
            edgeCurve.EdgeGeometry = circle;
            edgeCurve.SameSense = sameSenseOfCircleAndEdge;
        }

        public void SetEdgeGeometry_toCircleSegment(EdgeRef externalRefEdge, XbimAxis2Placement3D placement, double radius, bool sameSenseOfArcAndEdge, XbimXYZ startPoint, XbimXYZ endPoint, bool senseOfTrimming)
        {
            IfcEdgeCurve edgeCurve = GetEdgeCurve(externalRefEdge);
            if (radius < 0) throw new Exception("Negative radius is not allowed.");
            IfcAxis2Placement axis2placement = placement._ifcAxis2Placement;

            IfcCircle circle = _document.Model.New<IfcCircle>(cr => { cr.Position = axis2placement; cr.Radius = radius; });
            IfcCartesianPoint point1 = startPoint.CreateIfcCartesianPoint(_document);
            IfcCartesianPoint point2 = endPoint.CreateIfcCartesianPoint(_document);

            IfcTrimmedCurve trimmedCurve = _document.Model.New<IfcTrimmedCurve>(crv => { crv.BasisCurve = circle; crv.Trim1.Add_Reversible(point1); crv.Trim2.Add_Reversible(point2); crv.SenseAgreement = senseOfTrimming; crv.MasterRepresentation = IfcTrimmingPreference.CARTESIAN; });
            edgeCurve.EdgeGeometry = trimmedCurve;
            edgeCurve.SameSense = sameSenseOfArcAndEdge;
        }

        public void SetEdgeGeometry_toEllipse(EdgeRef externalRefEdge, XbimAxis2Placement3D position, double semiAxis1, double semiAxis2, bool sameSenseOfEllipseAndEdge)
        {
            IfcEdgeCurve edgeCurve = GetEdgeCurve(externalRefEdge);
            if (semiAxis1 <= 0 || semiAxis2 <= 0) throw new Exception("Semi axes must be greater than 0.");
            IfcAxis2Placement placement = position._ifcAxis2Placement;

            IfcEllipse ellipse = _document.Model.New<IfcEllipse>(el => { el.Position = placement; el.SemiAxis1 = semiAxis1; el.SemiAxis2 = semiAxis2;});
            edgeCurve.EdgeGeometry = ellipse;
            edgeCurve.SameSense = sameSenseOfEllipseAndEdge;
        }

        public void SetEdgeGeometry_toEllipseSegment(EdgeRef externalRefEdge, //identification of the edge
            XbimAxis2Placement3D position, double semiAxis1, double semiAxis2, bool sameSenseOfEllipseAndEdge, //ellipse parameters
            XbimXYZ startPoint, XbimXYZ endPoint, bool senseOfTrimming) //trimming parameters
        {
            IfcEdgeCurve edgeCurve = GetEdgeCurve(externalRefEdge);
            if (semiAxis1 <= 0 || semiAxis2 <= 0) throw new Exception("Semi axes must be greater than 0.");
            IfcAxis2Placement placement = position._ifcAxis2Placement;

            IfcEllipse ellipse = _document.Model.New<IfcEllipse>(el => { el.Position = placement; el.SemiAxis1 = semiAxis1; el.SemiAxis2 = semiAxis2; });
            IfcCartesianPoint point1 = startPoint.CreateIfcCartesianPoint(_document);
            IfcCartesianPoint point2 = endPoint.CreateIfcCartesianPoint(_document);

            IfcTrimmedCurve trimmedCurve = _document.Model.New<IfcTrimmedCurve>(crv => { crv.BasisCurve = ellipse; crv.Trim1.Add_Reversible(point1); crv.Trim2.Add_Reversible(point2); crv.SenseAgreement = senseOfTrimming; crv.MasterRepresentation = IfcTrimmingPreference.CARTESIAN; });
            edgeCurve.EdgeGeometry = trimmedCurve;
            edgeCurve.SameSense = sameSenseOfEllipseAndEdge;
        }

        public void SetEdgeGeometry_toRationalBezierCurve(EdgeRef externalRefEdge)
        {
            IfcEdgeCurve edgeCurve = GetEdgeCurve(externalRefEdge);
            throw new NotImplementedException();
        }

        private IfcEdgeCurve GetEdgeCurve(EdgeRef reference)
        {
            IfcEdge edge = null;
            _edges.TryGetValue(reference, out edge);
            if (edge == null) throw new Exception("This if not valid reference for the edge in the geometry.");

            IfcEdgeCurve edgeCurve = edge as IfcEdgeCurve;
            if (edgeCurve == null)
            {
                throw new Exception("This edge was created not to contain any geometry curve.");
            }

            return edgeCurve;
        }

        public void AddFaceOuterBound(FaceRef externalRefFace, List<EdgeRef> externalRefEdges, List<bool> orientationOfEdges, bool orientationOfFaceBound)
        {
            IfcFaceBound faceBound = _document.Model.New<IfcFaceOuterBound>(fb => fb.Orientation = orientationOfFaceBound);
            SetFaceBound(externalRefFace, externalRefEdges, orientationOfEdges, ref faceBound);
        }

        public void AddFaceInnerBound(FaceRef externalRefFace, List<EdgeRef> externalRefEdges, List<bool> orientationOfEdges, bool orientationOfFaceBound)
        {
            IfcFaceBound faceBound = _document.Model.New<IfcFaceBound>(fb => fb.Orientation = orientationOfFaceBound);
            SetFaceBound(externalRefFace, externalRefEdges, orientationOfEdges, ref faceBound);
        }

        private void SetFaceBound(FaceRef externalRefFace, List<EdgeRef> externalRefEdges, List<bool> orientationOfEdges, ref IfcFaceBound faceBound)
        {
            //get face from the dictionary. It must exist there.
            IfcFace face = null;
            _faces.TryGetValue(externalRefFace, out face);
            if (face == null) throw new Exception("Face with this external reference does not exist.");

            int countEdges = externalRefEdges.Count;
            int countOrientation = orientationOfEdges.Count;
            if (countEdges != countOrientation) throw new Exception("Number of edges and their orientation differs. Every edge must be oriented.");

            IfcEdgeLoop loop = _document.Model.New<IfcEdgeLoop>();
            //if (loop.EdgeList == null) loop.EdgeList = new XbimExtensions.XbimList<IfcOrientedEdge>();

            for (int i = 0; i < countEdges; i++)
            {
                IfcEdge edge = null;
                _edges.TryGetValue(externalRefEdges[i], out edge);
                if (tempInt == 39)
                {
                    foreach (EdgeRef re in _edges.Keys)
                    {
                        Debug.WriteLine(re.ToString());
                    }
                    Debug.WriteLine("\n" + externalRefEdges[i].ToString());
                }
                if (edge == null) throw new Exception("Edge is not defined in the geometry model of faceted Brep.");
                tempInt++;
                bool orientation = orientationOfEdges[i];

                IfcOrientedEdge orientEdge = _document.Model.New<IfcOrientedEdge>(ed => { ed.Orientation = orientation; ed.EdgeElement = edge; });
                (orientEdge as IfcEdge).EdgeStart = edge.EdgeStart;
                (orientEdge as IfcEdge).EdgeEnd = edge.EdgeEnd;
                orientEdge.EdgeElement = edge;
                orientEdge.Orientation = orientation;
                loop.EdgeList.Add_Reversible(orientEdge);
            }

            //set loop
            faceBound.Bound = loop;

            //add face bound to the face
            face.Bounds.Add_Reversible(faceBound);
        }

        public void AddPlaneFace(FaceRef externalRefFace, bool sameSenseAsBound, XbimXYZ position, XbimXYZ normal, XbimXYZ planeVector)
        {
            //check if it does not already exist in the dictionary. Reference (and face) must be unique.
            IfcFace existFace = null;
            _faces.TryGetValue(externalRefFace, out existFace);
            if (existFace != null) throw new Exception("Face with this external reference already exists.");

            IfcFaceSurface face = _document.Model.New<IfcFaceSurface>(fc => fc.SameSense = sameSenseAsBound);
            //if (face.Bounds == null) face.Bounds = new XbimExtensions.XbimSet<IfcFaceBound>();
            face.Surface = _document.Model.New<IfcPlane>();
            IfcPlane plane = face.Surface as IfcPlane;

            plane.Position = _document.Model.New<IfcAxis2Placement3D>();
            plane.Position.SetNewLocation(position.X, position.Y, position.Z);
            plane.Position.SetNewDirectionOf_XZ(planeVector.X, planeVector.Y, planeVector.Z, normal.X, normal.Y, normal.Z);

            _faces.Add(externalRefFace, face);
            _ifcFacetedBrep.Outer.CfsFaces.Add_Reversible(face);
        }

        public void AddCylindricalExtrudedFace(FaceRef externalRefFace, bool sameSenseAsBound, EdgeRef edgeWithProfile, double depthExtrusion, XbimAxis2Placement3D extrusionPosition, XbimXYZ extrusionDirection)
        {
            //check if it does not already exist in the dictionary. Reference (and face) must be unique.
            IfcFace existFace = null;
            _faces.TryGetValue(externalRefFace, out existFace);
            if (existFace != null) throw new Exception("Face with this external reference already exists.");

            IfcFaceSurface face = _document.Model.New<IfcFaceSurface>(fc => fc.SameSense = sameSenseAsBound);
            //if (face.Bounds == null) face.Bounds = new XbimExtensions.XbimSet<IfcFaceBound>();
            face.Surface = _document.Model.New<IfcSurfaceOfLinearExtrusion>();
            IfcSurfaceOfLinearExtrusion surface = face.Surface as IfcSurfaceOfLinearExtrusion;

            //get curve for extrusion from the specified edge
            IfcEdge edge = null;
            _edges.TryGetValue(edgeWithProfile, out edge);
            if (edge == null) throw new Exception("This if not valid reference for the edge in the geometry.");
            IfcEdgeCurve edgeCurve = edge as IfcEdgeCurve;
            IfcTrimmedCurve curve = edgeCurve.EdgeGeometry as IfcTrimmedCurve;

            //set parameters of the extrusion
            surface.Position = extrusionPosition._ifcAxis2Placement;
            surface.Depth = depthExtrusion;
            surface.SweptCurve = _document.Model.New<IfcArbitraryOpenProfileDef>();
            surface.ExtrudedDirection = extrusionDirection.CreateIfcDirection(_document);
            IfcArbitraryOpenProfileDef profile = surface.SweptCurve as IfcArbitraryOpenProfileDef;
            profile.Curve = curve;
            profile.ProfileType = IfcProfileTypeEnum.CURVE;

            _faces.Add(externalRefFace, face);
            _ifcFacetedBrep.Outer.CfsFaces.Add_Reversible(face);
        }

        public bool ContainsVertex(VertexRef vertexRef)
        {
            return _vertices.ContainsKey(vertexRef);
        }

        public bool ContainsEdge(EdgeRef edgeRef)
        {
            return _edges.ContainsKey(edgeRef);
        }

        public bool ContainsFace(FaceRef faceRef)
        {
            return _faces.ContainsKey(faceRef);
        }
    }

    

    public class XbimXYZ
    {
        private double _X;
        private double _Y;
        private double _Z;

        public XbimXYZ(double X, double Y, double Z)
        {
            _X = X;
            _Y = Y;
            _Z = Z;
        }

        public XbimXYZ(double X, double Y)
        {
            _X = X;
            _Y = Y;
            _Z = double.NaN;
        }

        public void ChangeTo2D()
        {
            _Z = double.NaN;
        }

        //properties to access coordinates
        public double X { get { return _X; } set { _X = value; } }
        public double Y { get { return _Y; } set { _Y = value; } }
        public double Z { get { return _Z; } set { _Z = value; } }

        internal IfcCartesianPoint CreateIfcCartesianPoint(XbimDocument document)
        {
            return document.Model.New<IfcCartesianPoint>(pt => pt.SetXYZ(X, Y, Z));
        }

        internal IfcCartesianPoint CreateIfcCartesianPoint2D(XbimDocument document)
        {
            return document.Model.New<IfcCartesianPoint>(pt => pt.SetXY(X, Y));
        }

        internal IfcDirection CreateIfcDirection(XbimDocument document)
        {
            return document.Model.New<IfcDirection>(dir => dir.SetXYZ(X, Y, Z));
        }
    }
}
