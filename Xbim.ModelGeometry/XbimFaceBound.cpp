#include "StdAfx.h"
#include "XbimFaceBound.h"
#include "XbimVertexPoint.h"
#include "XbimGeomPrim.h"

#include <TopAbs.hxx> 
#include "XbimEdgeLoop.h"
#include <BRepBuilderAPI_MakeWire.hxx>
#include <GC_MakeArcOfCircle.hxx>
#include <gp_Ax2.hxx>
#include <gp_Ax3.hxx>
#include <gp_Circ.hxx>
#include <Geom_Circle.hxx>
#include <GC_MakeCircle.hxx>
#include <GC_MakeEllipse.hxx>
#include <BRepBuilderAPI_MakeEdge.hxx>
#include <BRepBuilderAPI_MakePolygon.hxx>
#include <gp_Elips.hxx> 
#include <GC_MakeArcOfEllipse.hxx>
#include <BRep_Builder.hxx>
#include <TopoDS_Vertex.hxx>
#include <BRepBuilderAPI_MakeEdge.hxx>
#include <GC_MakeSegment.hxx>
#include <BRepLib.hxx>
#include <ShapeFix_ShapeTolerance.hxx> 
#include <BRepTools.hxx> 
#include <TopExp_Explorer.hxx> 
#include <BRepLib_MakePolygon.hxx> 
using namespace System;

namespace Xbim
{
	namespace ModelGeometry
	{
		XbimFaceBound::XbimFaceBound(const TopoDS_Wire & wire, const TopoDS_Face & face)
		{
			pWire = new TopoDS_Wire();
			*pWire = wire;
			pFace = new TopoDS_Face();
			*pFace = face;
		}

		/*Interface*/

		XbimEdgeLoop^ XbimFaceBound::Bound::get()
		{

			return gcnew XbimEdgeLoop(*pWire, *pFace);
		}

		bool XbimFaceBound::Orientation::get()
		{
			return pWire->Orientation() == TopAbs_FORWARD; 
		}


		// AK: Builds a wire from a composite IfcLShapeProfileDef
		//TODO: SRL: Support for fillet radii needs to be added, nb set the hascurves=true when added
		// and note too that this will decrease performance due to use of OCC for triangulation
		TopoDS_Wire XbimFaceBound::Build(IfcLShapeProfileDef ^ profile, bool% hasCurves)
		{
			
			double dY = profile->Depth/2;
			double dX;
			if (profile->Width.HasValue)
				dX = profile->Width.Value/2;
			else
				dX=dY;
			double tF = profile->Thickness;
			gp_Pnt p1(-dX,dY,0);
			gp_Pnt p2(-dX + tF,dY,0);
			gp_Pnt p3(-dX + tF,-dY+tF,0);
			gp_Pnt p4(dX,-dY+tF,0);
			gp_Pnt p5(dX,-dY,0);
			gp_Pnt p6(-dX,-dY,0);
			
			BRepBuilderAPI_MakeWire wireMaker;
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p1,p2));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p2,p3));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p3,p4));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p4,p5));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p5,p6));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p6,p1));
			TopoDS_Wire wire = wireMaker.Wire();
			wire.Move(XbimGeomPrim::ToLocation(profile->Position));
			return wire;
		}

		// AK: Builds a wire from a composite IfcUShapeProfileDef
		//TODO: SRL: Support for fillet radii needs to be added, nb set the hascurves=true when added
		// and note too that this will decrease performance due to use of OCC for triangulation
		TopoDS_Wire XbimFaceBound::Build(IfcUShapeProfileDef ^ profile, bool% hasCurves)
		{
			double dX = profile->FlangeWidth/2;
			double dY = profile->Depth/2;
			double tF = profile->FlangeThickness;
			double tW = profile->WebThickness;

			gp_Pnt p1(-dX,dY,0);
			gp_Pnt p2(dX,dY,0);
			gp_Pnt p3(dX,dY-tF,0);
			gp_Pnt p4(-dX + tW,dY-tF,0);
			gp_Pnt p5(-dX + tW,-dY+tF,0);
			gp_Pnt p6(dX,-dY+tF,0);
			gp_Pnt p7(dX,-dY,0);
			gp_Pnt p8(-dX,-dY,0);
			
			BRepBuilderAPI_MakeWire wireMaker;
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p1,p2));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p2,p3));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p3,p4));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p4,p5));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p5,p6));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p6,p7));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p7,p8));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p8,p1));
			TopoDS_Wire wire = wireMaker.Wire();
			wire.Move(XbimGeomPrim::ToLocation(profile->Position));
			return wire;
		}

		// SRL: Builds a wire from a composite IfcCraneRailFShapeProfileDef
		//TODO: SRL: Support for fillet radii needs to be added, nb set the hascurves=true when added
		// and note too that this will decrease performance due to use of OCC for triangulation
		//NB. This is untested as we haven't enountered one yet
		TopoDS_Wire XbimFaceBound::Build(IfcCraneRailFShapeProfileDef ^ profile, bool% hasCurves)
		{
			double dX  = profile->HeadWidth/2;
			double dY  = profile->OverallHeight/2;
			double hd2 = profile->HeadDepth2;	
			double hd3 = profile->HeadDepth3;
			double tW  = profile->WebThickness;
			double bd1 = profile->BaseDepth1;
			double bd2 = profile->BaseDepth2;

			gp_Pnt p1(-dX,dY,0);
			gp_Pnt p2(dX,dY,0);
			gp_Pnt p3(dX,dY-hd3,0);
			gp_Pnt p4(tW/2,dY-hd2,0);
			gp_Pnt p5(tW/2,-dY+bd2,0);
			gp_Pnt p6(dX,-dY+bd1,0);
			gp_Pnt p7(dX,-dY,0);
			gp_Pnt p8(-dX,-dY,0);
			gp_Pnt p9(-dX,-dY+bd1,0);
			gp_Pnt p10(-tW/2,-dY+bd2,0);
			gp_Pnt p11(tW/2, dY-hd2,0);
			gp_Pnt p12(-dX, dY-hd3,0);
			
			BRepBuilderAPI_MakeWire wireMaker;

			wireMaker.Add(BRepBuilderAPI_MakeEdge(p1,p2));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p2,p3));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p3,p4));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p4,p5));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p5,p6));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p6,p7));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p7,p8));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p8,p9));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p9,p10));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p10,p11));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p11,p12));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p12,p1));
			TopoDS_Wire wire = wireMaker.Wire();
			wire.Move(XbimGeomPrim::ToLocation(profile->Position));
			return wire;
		}
		// SRL: Builds a wire from a composite IfcCraneRailAShapeProfileDef
		//TODO: SRL: Support for fillet radii needs to be added, nb set the hascurves=true when added
		// and note too that this will decrease performance due to use of OCC for triangulation
		//NB. This is untested as we haven't enountered one yet
		TopoDS_Wire XbimFaceBound::Build(IfcCraneRailAShapeProfileDef ^ profile, bool% hasCurves)
		{
			double bW  = profile->HeadWidth/2;
			double dY  = profile->OverallHeight/2;
			double hd2 = profile->HeadDepth2;	
			double hd3 = profile->HeadDepth3;
			double tW  = profile->WebThickness;
			double bd1 = profile->BaseDepth1;
			double bd2 = profile->BaseDepth2;
			double bd3 = profile->BaseDepth3;
			double bw2 = profile->BaseWidth2/2;
			double bw4 = profile->BaseWidth4/2;

			gp_Pnt p1(-bw4,dY,0);
			gp_Pnt p2(bw4,dY,0);
			gp_Pnt p3(bw4,dY-hd3,0);
			gp_Pnt p4(tW/2,dY-hd2,0);
			gp_Pnt p5(tW/2,-dY+bd2,0);
			gp_Pnt p6(bw4,-dY+bd3,0);
			gp_Pnt p7(bw2,-dY+bd1,0);
			gp_Pnt p8(bw2,-dY,0);
			gp_Pnt p9(-bw2,-dY,0);
			gp_Pnt p10(-bw2,-dY+bd1,0);
			gp_Pnt p11(-bw4,-dY+bd3,0);
			gp_Pnt p12(tW/2, -dY+bd2,0);
			gp_Pnt p13(tW/2, dY-hd2,0);
			gp_Pnt p14(-bw4, dY-hd3,0);
			
			BRepBuilderAPI_MakeWire wireMaker;

			wireMaker.Add(BRepBuilderAPI_MakeEdge(p1,p2));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p2,p3));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p3,p4));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p4,p5));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p5,p6));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p6,p7));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p7,p8));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p8,p9));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p9,p10));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p10,p11));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p11,p12));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p12,p13));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p13,p14));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p14,p1));
			TopoDS_Wire wire = wireMaker.Wire();
			wire.Move(XbimGeomPrim::ToLocation(profile->Position));
			return wire;
		}
		// SRL: Builds a wire from a composite IfcEllipseProfileDef
		//TODO: SRL: Support for fillet radii needs to be added, nb set the hascurves=true when added
		// and note too that this will decrease performance due to use of OCC for triangulation
		//NB. This is untested as we haven't enountered one yet
		TopoDS_Wire XbimFaceBound::Build(IfcEllipseProfileDef ^ profile, bool% hasCurves)
		{

			IfcAxis2Placement2D^ ax2 = (IfcAxis2Placement2D^)profile->Position;
			gp_Ax2 gpax2(gp_Pnt(ax2->Location->X, ax2->Location->Y,0), gp_Dir(0,0,1),gp_Dir(ax2->P[0]->X, ax2->P[0]->Y,0.));			
			gp_Elips gc(gpax2,profile->SemiAxis1, profile->SemiAxis2);
			if(profile->SemiAxis1<=0 || profile->SemiAxis2 <=0) throw gcnew XbimGeometryException("Illegal Ellipse Semi Axix, for IfcEllipseProfileDef, must be greater than 0, in entity #" + profile->EntityLabel);
			Handle(Geom_Ellipse) hellipse = GC_MakeEllipse(gc);
			TopoDS_Edge edge = BRepBuilderAPI_MakeEdge(hellipse);
			BRepBuilderAPI_MakeWire wire;
			wire.Add(edge);
			return  wire.Wire();
		}

		// AK: Builds a wire from a composite IfcIShapeProfileDef
		//TODO: SRL: Support for fillet radii needs to be added, nb set the hascurves=true when added
		// and note too that this will decrease performance due to use of OCC for triangulation
		TopoDS_Wire XbimFaceBound::Build(IfcIShapeProfileDef ^ profile, bool% hasCurves)
		{
			double dX = profile->OverallWidth/2;
			double dY = profile->OverallDepth/2;
			double tF = profile->FlangeThickness;
			double tW = profile->WebThickness;

		
			gp_Pnt p1(-dX,dY,0);
			gp_Pnt p2(dX,dY,0);
			gp_Pnt p3(dX,dY-tF,0);
			gp_Pnt p4(tW/2,dY-tF,0);
			gp_Pnt p5(tW/2,-dY+tF,0);
			gp_Pnt p6(dX,-dY+tF,0);
			gp_Pnt p7(dX,-dY,0);
			gp_Pnt p8(-dX,-dY,0);
			gp_Pnt p9(-dX,-dY+tF,0);
			gp_Pnt p10(-tW/2,-dY+tF,0);
			gp_Pnt p11(-tW/2,dY-tF,0);
			gp_Pnt p12(-dX,dY-tF,0);
			
			BRepBuilderAPI_MakeWire wireMaker;

			wireMaker.Add(BRepBuilderAPI_MakeEdge(p1,p2));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p2,p3));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p3,p4));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p4,p5));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p5,p6));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p6,p7));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p7,p8));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p8,p9));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p9,p10));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p10,p11));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p11,p12));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p12,p1));
			TopoDS_Wire wire = wireMaker.Wire();
			wire.Move(XbimGeomPrim::ToLocation(profile->Position));
			return wire;
		}

		//SRL: Builds a wire from a composite IfcZShapeProfileDef
		//TODO: SRL: Support for fillet radii needs to be added, nb set the hascurves=true when added
		// and note too that this will decrease performance due to use of OCC for triangulation
		TopoDS_Wire XbimFaceBound::Build(IfcZShapeProfileDef ^ profile, bool% hasCurves)
		{
			double dX = profile->FlangeWidth;
			double dY = profile->Depth/2;
			double tF = profile->FlangeThickness;
			double tW = profile->WebThickness;

		
			gp_Pnt p1(-dX+(tW/2),dY,0);
			gp_Pnt p2(tW/2,dY,0);
			gp_Pnt p3(tW/2,-dY + tF,0);
			gp_Pnt p4(dX -tW/2,-dY + tF,0);
			gp_Pnt p5(dX -tW/2,-dY,0);
			gp_Pnt p6(-tW/2,-dY,0);
			gp_Pnt p7(-tW/2,dY-tF,0);
			gp_Pnt p8(-dX+(tW/2),dY-tF,0);

			
			BRepBuilderAPI_MakeWire wireMaker;

			wireMaker.Add(BRepBuilderAPI_MakeEdge(p1,p2));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p2,p3));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p3,p4));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p4,p5));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p5,p6));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p6,p7));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p7,p8));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p8,p1));

			TopoDS_Wire wire = wireMaker.Wire();
			wire.Move(XbimGeomPrim::ToLocation(profile->Position));
			return wire;
		}

		// SRL: Builds a wire from a composite IfcCShapeProfileDef
		//TODO: SRL: Support for fillet radii needs to be added, nb set the hascurves=true when added
		// and note too that this will decrease performance due to use of OCC for triangulation
		TopoDS_Wire XbimFaceBound::Build(IfcCShapeProfileDef ^ profile, bool% hasCurves)
		{
			double dX = profile->Width/2;
			double dY = profile->Depth/2;
			double dG = profile->Girth;
			double tW = profile->WallThickness;
			if(dG<=0) throw gcnew XbimGeometryException("Illegal girth for IfcCShapeProfileDef, must be greater than 0, in entity #"+profile->EntityLabel);
			if(tW<=0) throw gcnew XbimGeometryException("Illegal wall thickness for IfcCShapeProfileDef, must be greater than 0, in entity #"+profile->EntityLabel);
			gp_Pnt p1(-dX,dY,0);
			gp_Pnt p2(dX,dY,0);
			gp_Pnt p3(dX,dY-dG,0);
			gp_Pnt p4(dX-tW,dY-dG,0);
			gp_Pnt p5(dX-tW,dY-tW,0);
			gp_Pnt p6(-dX+tW,dY-tW,0);
			gp_Pnt p7(-dX+tW,-dY+tW,0);
			gp_Pnt p8(dX-tW,-dY+tW,0);
			gp_Pnt p9(dX-tW,-dY+dG,0);
			gp_Pnt p10(dX,-dY+dG,0);
			gp_Pnt p11(dX,-dY,0);
			gp_Pnt p12(-dX,-dY,0);
			
			BRepBuilderAPI_MakeWire wireMaker;

			wireMaker.Add(BRepBuilderAPI_MakeEdge(p1,p2));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p2,p3));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p3,p4));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p4,p5));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p5,p6));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p6,p7));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p7,p8));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p8,p9));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p9,p10));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p10,p11));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p11,p12));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p12,p1));
			TopoDS_Wire wire = wireMaker.Wire();
			wire.Move(XbimGeomPrim::ToLocation(profile->Position));
			return wire;
		}
		// SRL: Builds a wire from a composite IfcTShapeProfileDef
		//TODO: SRL: Support for fillet radii needs to be added, nb set the hascurves=true when added
		// and note too that this will decrease performance due to use of OCC for triangulation
		TopoDS_Wire XbimFaceBound::Build(IfcTShapeProfileDef ^ profile, bool% hasCurves)
		{
			double dX = profile->FlangeWidth/2;
			double dY = profile->Depth/2;
			double tF = profile->FlangeThickness;
			double tW = profile->WebThickness;

			gp_Pnt p1(-dX,dY,0);
			gp_Pnt p2(dX,dY,0);
			gp_Pnt p3(dX,dY-tF,0);
			gp_Pnt p4(tW/2,dY-tF,0);
			gp_Pnt p5(tW/2,-dY,0);
			gp_Pnt p6(-tW/2,-dY,0);
			gp_Pnt p7(-tW/2,dY-tF,0);
			gp_Pnt p8(-dX,dY-tF,0);
			
			BRepBuilderAPI_MakeWire wireMaker;

			wireMaker.Add(BRepBuilderAPI_MakeEdge(p1,p2));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p2,p3));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p3,p4));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p4,p5));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p5,p6));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p6,p7));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p7,p8));
			wireMaker.Add(BRepBuilderAPI_MakeEdge(p8,p1));
			TopoDS_Wire wire = wireMaker.Wire();
			wire.Move(XbimGeomPrim::ToLocation(profile->Position));
			return wire;
		}

		//Builds a wire from a composite ArbitraryClosedProfileDef
		TopoDS_Wire XbimFaceBound::Build(IfcArbitraryClosedProfileDef ^ profile, bool% hasCurves)
		{
			if(dynamic_cast<IfcCompositeCurve^>(profile->OuterCurve))
				return Build((IfcCompositeCurve^)profile->OuterCurve, hasCurves);
			if(dynamic_cast<IfcPolyline^>(profile->OuterCurve))
				return Build((IfcPolyline^)profile->OuterCurve, hasCurves);
			else
			{
				Type ^ type = profile->OuterCurve->GetType();
				throw(gcnew NotImplementedException(String::Format("XbimFaceBound. Could not BuildShape of type {0}. It is not implemented",type->Name)));
			}
		}

		//Builds a wire from a composite curve
		TopoDS_Wire XbimFaceBound::Build(IfcCompositeCurve ^ cCurve, bool% hasCurves)
		{
			BRepBuilderAPI_MakeWire wire;
			for each(IfcCompositeCurveSegment^ seg in cCurve->Segments)
			{


				///TODO: Need to add support for curve segment continuity a moment only continuos supported
				TopoDS_Wire wireSeg = Build(seg->ParentCurve, hasCurves);
				if(!wireSeg.IsNull())
				{
					if(!seg->SameSense) wireSeg.Reverse();
					ShapeFix_ShapeTolerance FTol;
					FTol.SetTolerance(wireSeg, BRepLib::Precision()*10, TopAbs_WIRE);
					wire.Add(wireSeg);
				}

			}
			
			return wire.Wire();

		}

		//Builds a wire from a CircleProfileDef
		TopoDS_Wire XbimFaceBound::Build(IfcCircleProfileDef ^ circProfile, bool% hasCurves)
		{
			hasCurves=true;
			IfcAxis2Placement2D^ ax2 = (IfcAxis2Placement2D^)circProfile->Position;
			gp_Ax2 gpax2(gp_Pnt(ax2->Location->X, ax2->Location->Y,0), gp_Dir(0,0,1),gp_Dir(ax2->P[0]->X, ax2->P[0]->Y,0.));			
			gp_Circ gc(gpax2,circProfile->Radius);
			Handle(Geom_Circle) hCirc = GC_MakeCircle(gc);

			TopoDS_Edge edge = BRepBuilderAPI_MakeEdge(hCirc);
			BRepBuilderAPI_MakeWire wire;
			wire.Add(edge);
			return  wire.Wire();
		}

		TopoDS_Wire XbimFaceBound::Build(IfcPolyLoop ^ loop, bool% hasCurves)
		{	
			BRepBuilderAPI_MakePolygon poly;	
			int nbPoints = loop->Polygon->Count;
			int finalPoints = nbPoints;
			bool is3D=false;
			if(nbPoints>0) is3D = (loop->Polygon[0]->Dim == 3);
			for(int i=0; i<=nbPoints-1; i++) 
			{
				
				gp_Pnt pt(loop->Polygon[i]->X,loop->Polygon[i]->Y,is3D ? loop->Polygon[i]->Z : 0);
				poly.Add(pt);
				Standard_Boolean ok = poly.Added();
				if(i >0 && ok != Standard_True) 
					finalPoints--;

			}		
			if(!poly.IsDone() || finalPoints<3) //invalid wire
			{
				return TopoDS_Wire();
			}
			else
			{
				poly.Close();
				return poly.Wire();
			}
			
		}


		//Builds a wire from a RectangleProfileDef
		TopoDS_Wire XbimFaceBound::Build(IfcRectangleProfileDef ^ rectProfile, bool% hasCurves)
		{
			double xOff = rectProfile->XDim/2;
			double yOff= rectProfile->YDim/2;


			gp_XYZ bl(-xOff,-yOff,0);
			gp_XYZ br(xOff,-yOff,0);
			gp_XYZ tr(xOff,yOff,0);
			gp_XYZ tl(-xOff,yOff,0);

			BRepBuilderAPI_MakePolygon rect(bl,tl,tr,br,true);

			TopoDS_Wire wire = rect.Wire();
			//apply the position transformation
			wire.Move(XbimGeomPrim::ToLocation(rectProfile->Position));
			return wire;


		}

		TopoDS_Wire XbimFaceBound::Build(IfcBoundedCurve ^ bCurve, bool% hasCurves)
		{
			BRepBuilderAPI_MakeWire wire;
			if (dynamic_cast<IfcPolyline^>(bCurve))
				return Build((IfcPolyline^)bCurve, hasCurves);
			else if(dynamic_cast<IfcTrimmedCurve^>(bCurve))
				return Build((IfcTrimmedCurve^)bCurve, hasCurves);
			if(dynamic_cast<IfcCompositeCurve^>(bCurve))
				return Build((IfcCompositeCurve^)bCurve, hasCurves);
			else
			{
				Type ^ type = bCurve->GetType();
				throw(gcnew NotImplementedException(String::Format("XbimFaceBound::Build. BoundedCurve of type {0} is not implemented",type->Name)));	
			}
			if(!wire.IsDone())
			{
				System::Diagnostics::Debug::WriteLine(String::Format("Error processing entity #{0}",bCurve->EntityLabel));
			}
			return wire.Wire();

		}

		TopoDS_Wire XbimFaceBound::Build(IfcCurve ^ curve, bool% hasCurves)
		{
			if (dynamic_cast<IfcBoundedCurve^>(curve))
				return Build((IfcBoundedCurve^) curve, hasCurves);
			else
			{
				Type ^ type = curve->GetType();
				throw(gcnew NotImplementedException(String::Format("XbimFaceBound::Build. Curve of type {0} is not implemented",type->Name)));	
			}

		}


		TopoDS_Wire XbimFaceBound::Build(IfcPolyline ^ pLine, bool% hasCurves)
		{

			BRepLib_MakePolygon poly;	

			int nbPoints = pLine->Points->Count;
			
			gp_Pnt first, last;
			bool is3D = (pLine->Dim == 3);
			for(int i=0; i<nbPoints; i++) //ignore the last repeated point
			{
				gp_Pnt pt(pLine->Points[i]->X,pLine->Points[i]->Y,is3D ? pLine->Points[i]->Z : 0);
				poly.Add(pt);
				if(i==0) 
					first = pt;
				else if (i==nbPoints-1)
					last = pt;

			}
			if(nbPoints==2 && first.IsEqual(last, BRepLib::Precision())) //we have no edge just two convergent points
				return TopoDS_Wire(); //return an empty wire
			/*if(nbPoints>2 && first.IsEqual(last, Precision::Confusion()))
				poly.Close();*/
			
			return poly.Wire();	

		}

		TopoDS_Wire XbimFaceBound::Build(IfcTrimmedCurve ^ tCurve, bool% hasCurves)
		{
			BRepBuilderAPI_MakeWire wire;
			IfcCartesianPoint^ start;
			IfcCartesianPoint^ end; 
			if(tCurve->SenseAgreement)
			{
				start = tCurve->Trim1Point(true);
				end = tCurve->Trim2Point(true);

			}
			else
			{
				end = tCurve->Trim1Point(true);
				start = tCurve->Trim2Point(true);
			}

			//it could be based on a circle, ellipse or line
			if(dynamic_cast<IfcCircle^>(tCurve->BasisCurve))
			{
				hasCurves=true;
				IfcCircle^ c = (IfcCircle^) tCurve->BasisCurve;

				if(dynamic_cast<IfcAxis2Placement2D^>(c->Position))
				{
					IfcAxis2Placement2D^ ax2 = (IfcAxis2Placement2D^)c->Position;
					gp_Ax2 gpax2(gp_Pnt(ax2->Location->X, ax2->Location->Y,0), gp_Dir(0,0,1),gp_Dir(ax2->P[0]->X, ax2->P[0]->Y,0.));			
					gp_Circ gc(gpax2,c->Radius);

					Handle(Geom_TrimmedCurve) aArcOfCircle = GC_MakeArcOfCircle(gc,gp_Pnt(start->X, start->Y,0), gp_Pnt(end->X, end->Y,0),tCurve->SenseAgreement);
					TopoDS_Edge edge = BRepBuilderAPI_MakeEdge(aArcOfCircle);
					wire.Add(edge);
				}
				else if(dynamic_cast<IfcAxis2Placement3D^>(c->Position))
				{
					IfcAxis2Placement3D^ ax2 = (IfcAxis2Placement3D^)c->Position;
					gp_Ax3 	gpax3 = XbimGeomPrim::ToAx3(ax2);		
					gp_Circ gc(gpax3.Ax2(),c->Radius);

					Handle(Geom_TrimmedCurve) aArcOfCircle = GC_MakeArcOfCircle(gc,gp_Pnt(start->X, start->Y,0), gp_Pnt(end->X, end->Y,0),tCurve->SenseAgreement);
					TopoDS_Edge edge = BRepBuilderAPI_MakeEdge(aArcOfCircle);
					wire.Add(edge);}
				else
				{
					Type ^ type = c->Position->GetType();
					throw(gcnew NotImplementedException(String::Format("XbimFaceBound. Circle with Placement of type {0} is not implemented",type->Name)));	
				}
			}
			else if (dynamic_cast<IfcEllipse^>(tCurve->BasisCurve))
			{
				hasCurves=true;
				IfcEllipse^ c = (IfcEllipse^) tCurve->BasisCurve;

				if(dynamic_cast<IfcAxis2Placement2D^>(c->Position))
				{
					IfcAxis2Placement2D^ ax2 = (IfcAxis2Placement2D^)c->Position;
					c->SemiAxis1;
					gp_Ax2 gpax2(gp_Pnt(ax2->Location->X, ax2->Location->Y,0), gp_Dir(0,0,1),gp_Dir(ax2->P[0]->X, ax2->P[0]->Y,0.));			
					gp_Elips gc(gpax2,c->SemiAxis1, c->SemiAxis2);
					Handle(Geom_TrimmedCurve) aArcOfEllipse = GC_MakeArcOfEllipse(gc,gp_Pnt(start->X, start->Y,0), gp_Pnt(end->X, end->Y,0),tCurve->SenseAgreement);
					TopoDS_Edge edge = BRepBuilderAPI_MakeEdge(aArcOfEllipse);
					wire.Add(edge);
				}
				else if(dynamic_cast<IfcAxis2Placement3D^>(c->Position))
				{
					Type ^ type = c->Position->GetType();
					throw(gcnew NotImplementedException(String::Format("XbimFaceBound. Ellipse with Placement of type {0} is not implemented",type->Name)));	
				}
				else
				{
					Type ^ type = c->Position->GetType();
					throw(gcnew NotImplementedException(String::Format("XbimFaceBound. Ellipse with Placement of type {0} is not implemented",type->Name)));	
				}
			}
			else if (dynamic_cast<IfcLine^>(tCurve->BasisCurve))
			{
				IfcLine^ line = (IfcLine^)(tCurve->BasisCurve);
				BRep_Builder b;
				TopoDS_Vertex v1, v2;
				b.MakeVertex(v1, gp_Pnt(start->X, start->Y,0), 1.E-005);
				b.MakeVertex(v2, gp_Pnt(end->X, end->Y,0),  1.E-005);
				BRepBuilderAPI_MakeEdge edgeMaker(v1, v2);
				wire.Add(edgeMaker.Edge());
			}
			else
			{
				Type ^ type = tCurve->BasisCurve->GetType();
				throw(gcnew NotImplementedException(String::Format("XbimFaceBound. CompositeCurveSegments with BasisCurve of type {0} is not implemented",type->Name)));	
			}
			if(!wire.IsDone())
			{
				System::Diagnostics::Debug::WriteLine(String::Format("Error processing entity #{0}",tCurve->EntityLabel));
			}
			return wire.Wire();
		}
		
		void XbimFaceBound::Print()
		{
			
			Bound->Print();
			
		}
	}
}