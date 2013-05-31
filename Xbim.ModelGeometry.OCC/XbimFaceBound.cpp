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
#include <GC_MakeLine.hxx>
#include <BRepLib.hxx>
#include <ShapeFix_ShapeTolerance.hxx> 
#include <BRepTools.hxx> 
#include <TopExp_Explorer.hxx> 
#include <BRepLib_MakePolygon.hxx> 
#include <BRepBuilderAPI_WireError.hxx> 
#include <BRepBuilderApi.hxx>
using namespace System;
using namespace Xbim::XbimExtensions;
using namespace Xbim::Common;
using namespace Xbim::Common::Geometry;
namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
		XbimFaceBound::XbimFaceBound(const TopoDS_Wire & wire, const TopoDS_Face & face)
		{
			pWire = new TopoDS_Wire();
			*pWire = wire;
			pFace = new TopoDS_Face();
			*pFace = face;
		}

		gp_Vec XbimFaceBound::NewellsNormal(const TopoDS_Wire & bound)
		{
			double x = 0, y = 0, z = 0;
			gp_Pnt current, previous, first;
			int count = 0;

			for ( TopExp_Explorer exp(bound,TopAbs_VERTEX);; exp.Next()) {
				unsigned more = exp.More();
				if ( more ) 
				{
					const TopoDS_Vertex& v = TopoDS::Vertex(exp.Current());
					current = BRep_Tool::Pnt(v);
				} 
				else 
					current = first;			
				if (count) 
				{
					const double& xn  = previous.X();
					const double& yn  = previous.Y();
					const double& zn  = previous.Z();
					const double& xn1 =  current.X();
					const double& yn1 =  current.Y();
					const double& zn1 =  current.Z();
					x += (yn-yn1)*(zn+zn1);
					y += (xn+xn1)*(zn-zn1);
					z += (xn-xn1)*(yn+yn1);
				} 
				else 
					first = current;
				if ( !more ) break;
				previous = current;
				count++;
			}
			return gp_Vec(x,y,z);
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
			double semiAx1 =profile->SemiAxis1;
			double semiAx2 =profile->SemiAxis2;
			if(semiAx1<=0 ) 
			{
				XbimModelFactors^ mf = ((IPersistIfcEntity^)profile)->ModelOf->GetModelFactors;
				semiAx1 = mf->OneMilliMetre;
				//	throw gcnew XbimGeometryException("Illegal Ellipse Semi Axix, for IfcEllipseProfileDef, must be greater than 0, in entity #" + profile->EntityLabel);
			}
			if(semiAx2 <=0) 
			{
				XbimModelFactors^ mf = ((IPersistIfcEntity^)profile)->ModelOf->GetModelFactors;
				semiAx2 =  mf->OneMilliMetre;
				//	throw gcnew XbimGeometryException("Illegal Ellipse Semi Axix, for IfcEllipseProfileDef, must be greater than 0, in entity #" + profile->EntityLabel);
			}
			gp_Elips gc(gpax2,semiAx1, semiAx2);
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
			
			if(tW<=0)
			{
				XbimModelFactors^ mf = ((IPersistIfcEntity^)profile)->ModelOf->GetModelFactors;
				tW = mf->OneMilliMetre * 3;
				//throw gcnew XbimGeometryException("Illegal wall thickness for IfcCShapeProfileDef, must be greater than 0, in entity #"+profile->EntityLabel);
			}
			BRepBuilderAPI_MakeWire wireMaker;
			if(dG>0) 
			{
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
			}
			else
			{
				gp_Pnt p1(-dX,dY,0);
				gp_Pnt p2(dX,dY,0);
				gp_Pnt p5(dX,dY-tW,0);
				gp_Pnt p6(-dX+tW,dY-tW,0);
				gp_Pnt p7(-dX+tW,-dY+tW,0);
				gp_Pnt p8(dX,-dY+tW,0);	
				gp_Pnt p11(dX,-dY,0);
				gp_Pnt p12(-dX,-dY,0);

				wireMaker.Add(BRepBuilderAPI_MakeEdge(p1,p2));
				wireMaker.Add(BRepBuilderAPI_MakeEdge(p2,p5));
				wireMaker.Add(BRepBuilderAPI_MakeEdge(p5,p6));
				wireMaker.Add(BRepBuilderAPI_MakeEdge(p6,p7));
				wireMaker.Add(BRepBuilderAPI_MakeEdge(p7,p8));
				wireMaker.Add(BRepBuilderAPI_MakeEdge(p8,p11));
				wireMaker.Add(BRepBuilderAPI_MakeEdge(p11,p12));
				wireMaker.Add(BRepBuilderAPI_MakeEdge(p12,p1));
			}
		
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
			if(dynamic_cast<IfcCurve^>(profile->OuterCurve))
				return Build((IfcCurve^)profile->OuterCurve, hasCurves);
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
			XbimModelFactors^ mf = ((IPersistIfcEntity^)cCurve)->ModelOf->GetModelFactors;
			ShapeFix_ShapeTolerance FTol;
			for each(IfcCompositeCurveSegment^ seg in cCurve->Segments)
			{
				///TODO: Need to add support for curve segment continuity a moment only continuous supported
				TopoDS_Wire wireSeg = Build(seg->ParentCurve, hasCurves);
				if(!wireSeg.IsNull())
				{
					
					if(!seg->SameSense) wireSeg.Reverse();
					
					FTol.SetTolerance(wireSeg, mf->WireTolerance, TopAbs_WIRE);	
					wire.Add(wireSeg);
					
					if( wire.Error() != BRepBuilderAPI_WireDone ) 
					{
						FTol.SetTolerance(wireSeg, mf->WireTolerance*1000, TopAbs_WIRE);	
						wire.Add(wireSeg);
						
					}
					
				}
			}

			if ( wire.IsDone()) 
				return wire.Wire();
			else
				throw gcnew XbimGeometryException("Invalid wire forming IfcFaceBound #" + cCurve->EntityLabel);
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
			BRepBuilderAPI_MakeWire w;
			int nbPoints = loop->Polygon->Count;
			bool is3D = (loop->Polygon[0]->Dim == 3);
			gp_Pnt p1;gp_Pnt first;
			int count = 0;

			for(int i=0; i<nbPoints; i++) 
			{
				gp_Pnt p2(loop->Polygon[i]->X,loop->Polygon[i]->Y,is3D ? loop->Polygon[i]->Z : 0);
				if ( i>0 &&  !p1.IsEqual(p2,BRepBuilderAPI::Precision()))
				{
					w.Add(BRepBuilderAPI_MakeEdge(p1,p2));
					count ++;
				} 
				else if ( i==0 ) 
					first = p2;		
				p1 = p2;
			}
			if ( !p1.IsEqual(first,BRepBuilderAPI::Precision()) ) {
				w.Add(BRepBuilderAPI_MakeEdge(p1,first));
				count ++;

			}
			if ( count < 3 ) return TopoDS_Wire(); //invalid polyloop

			TopoDS_Wire result = w.Wire();

			// set the tolerance for this shape.
			ShapeFix_ShapeTolerance FTol;	
			FTol.SetTolerance(result, BRepBuilderAPI::Precision() ,TopAbs_WIRE);
			return result;
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
			ShapeFix_ShapeTolerance FTol;
			// set the tolerance for this shape.
			FTol.SetTolerance(wire, BRepBuilderAPI::Precision() ,TopAbs_WIRE);

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
			else if(dynamic_cast<IfcCompositeCurve^>(bCurve))
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
			else if(dynamic_cast<IfcCircle^>(curve))
				return Build((IfcCircle^)curve, hasCurves);
			else if(dynamic_cast<IfcLine^>(curve))
				return Build((IfcLine^)curve, hasCurves);
			else
			{
				Type ^ type = curve->GetType();
				throw(gcnew NotImplementedException(String::Format("XbimFaceBound::Build. Curve of type {0} is not implemented",type->Name)));	
			}

		}
		TopoDS_Wire XbimFaceBound::Build(IfcCircle ^ circle, bool% hasCurves)
		{
			hasCurves=true;
			Handle(Geom_Curve) curve;
			if(dynamic_cast<IfcAxis2Placement2D^>(circle->Position))
			{
				IfcAxis2Placement2D^ ax2 = (IfcAxis2Placement2D^)circle->Position;
				gp_Ax2 gpax2(gp_Pnt(ax2->Location->X, ax2->Location->Y,0), gp_Dir(0,0,1),gp_Dir(ax2->P[0]->X, ax2->P[0]->Y,0.));			
				gp_Circ gc(gpax2,circle->Radius);
				curve = GC_MakeCircle(gc);
			}
			else if(dynamic_cast<IfcAxis2Placement3D^>(circle->Position))
			{
				IfcAxis2Placement3D^ ax2 = (IfcAxis2Placement3D^)circle->Position;
				gp_Ax3 	gpax3 = XbimGeomPrim::ToAx3(ax2);		
				gp_Circ gc(gpax3.Ax2(),circle->Radius);	
				curve = GC_MakeCircle(gc);
			}	
			else
			{
				Type ^ type = circle->Position->GetType();
				throw(gcnew NotImplementedException(String::Format("XbimFaceBound. Circle with Placement of type {0} is not implemented",type->Name)));	
			}
			BRepBuilderAPI_MakeEdge e(curve);
			BRepBuilderAPI_MakeWire w;
			w.Add(e);
			TopoDS_Wire result = w.Wire();
			// set the tolerance for this shape.
			ShapeFix_ShapeTolerance FTol;	
			FTol.SetTolerance(result, BRepBuilderAPI::Precision() ,TopAbs_WIRE);
			return result;
		}

		TopoDS_Wire XbimFaceBound::Build(IfcLine ^ line, bool% hasCurves)
		{
			IfcCartesianPoint^ cp = line->Pnt;
			IfcVector^ dir = line->Dir;
			gp_Pnt pnt(cp->X,cp->Y,cp->Z);
			XbimVector3D v3d = dir->XbimVector3D();
			gp_Vec vec(v3d.X,v3d.Y,v3d.Z);
			BRepBuilderAPI_MakeWire w;
			w.Add(BRepBuilderAPI_MakeEdge(GC_MakeLine(pnt,vec),0,dir->Magnitude));
			TopoDS_Wire result = w.Wire();
			// set the tolerance for this shape.
			ShapeFix_ShapeTolerance FTol;	
			FTol.SetTolerance(result, BRepBuilderAPI::Precision() ,TopAbs_WIRE);
			return result;	
		}

		TopoDS_Wire XbimFaceBound::Build(IfcPolyline ^ pLine, bool% hasCurves)
		{
			BRepBuilderAPI_MakeWire w;
			int nbPoints = pLine->Points->Count;		
			gp_Pnt p1;
			bool is3D = (pLine->Dim == 3);
			for(int i=0; i<nbPoints; i++) 
			{
				gp_Pnt p2(pLine->Points[i]->X,pLine->Points[i]->Y,is3D ? pLine->Points[i]->Z : 0);
				if ( i>0 &&  !p1.IsEqual(p2,BRepBuilderAPI::Precision()))
					w.Add(BRepBuilderAPI_MakeEdge(p1,p2));
				p1 = p2;
			}			
			TopoDS_Wire result = w.Wire();
			// set the tolerance for this shape.
			ShapeFix_ShapeTolerance FTol;	
			FTol.SetTolerance(result, BRepBuilderAPI::Precision() ,TopAbs_WIRE);
			return result;
		}


		TopoDS_Wire XbimFaceBound::Build(IfcTrimmedCurve ^ tCurve, bool% hasCurves)
		{
			bool isConic = (dynamic_cast<IfcConic^>(tCurve->BasisCurve)!=nullptr);

			XbimModelFactors^ mf = ((IPersistIfcEntity^)tCurve)->ModelOf->GetModelFactors;

			double parameterFactor =  isConic ? mf->AngleToRadiansConversionFactor : mf->LengthToMetresConversionFactor;
			Handle(Geom_Curve) curve;

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
					curve = GC_MakeCircle(gc);
				}
				else if(dynamic_cast<IfcAxis2Placement3D^>(c->Position))
				{
					IfcAxis2Placement3D^ ax2 = (IfcAxis2Placement3D^)c->Position;
					gp_Ax3 	gpax3 = XbimGeomPrim::ToAx3(ax2);		
					gp_Circ gc(gpax3.Ax2(),c->Radius);	
					curve = GC_MakeCircle(gc);
				}	
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
					double s1;
					double s2;
					if( c->SemiAxis1 > c->SemiAxis2)
					{
						s1=c->SemiAxis1;
						s2=c->SemiAxis2;
					}
					else //either same or two is larger than 1
					{
						s1=c->SemiAxis2;
						s2=c->SemiAxis1;
					}

					gp_Ax2 gpax2(gp_Pnt(ax2->Location->X, ax2->Location->Y,0), gp_Dir(0,0,1),gp_Dir(ax2->P[0]->X, ax2->P[0]->Y,0.));	
					
					gp_Elips gc(gpax2,s1, s2);
					curve = GC_MakeEllipse(gc);
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
				IfcCartesianPoint^ cp = line->Pnt;
				
				IfcVector^ dir = line->Dir;
				gp_Pnt pnt(cp->X,cp->Y,cp->Z);
				XbimVector3D v3d = dir->XbimVector3D();
				gp_Vec vec(v3d.X,v3d.Y,v3d.Z);
				curve = GC_MakeLine(pnt,vec);
			}
			else
			{
				Type ^ type = tCurve->BasisCurve->GetType();
				throw(gcnew NotImplementedException(String::Format("XbimFaceBound. CompositeCurveSegments with BasisCurve of type {0} is not implemented",type->Name)));	
			}

			bool trim_cartesian = (tCurve->MasterRepresentation == IfcTrimmingPreference::CARTESIAN);

			bool trimmed1 = false;
			bool trimmed2 = false;
			bool sense_agreement = tCurve->SenseAgreement;
			double flt1;
			gp_Pnt pnt1;
			double x,y,z;
			BRepBuilderAPI_MakeWire w;
			for each ( IfcTrimmingSelect^ trim in tCurve->Trim1 ) 
			{
				
				if ( dynamic_cast<IfcCartesianPoint^>(trim) && trim_cartesian ) 
				{
					IfcCartesianPoint^ cp = (IfcCartesianPoint^)trim; 
					
					cp->XYZ( x, y, z);
					pnt1.SetXYZ(gp_XYZ(x,y,z));
					trimmed1 = true;
				}
				else if ( dynamic_cast<IfcParameterValue^>(trim) && !trim_cartesian ) 
				{
					IfcParameterValue^ pv = (IfcParameterValue^)trim; 
					const double value = (double)(pv->Value);
					flt1 = value * parameterFactor;
					trimmed1 = true;
				}
			}
			for each ( IfcTrimmingSelect^ trim in tCurve->Trim2 ) 
			{
				if (  dynamic_cast<IfcCartesianPoint^>(trim) && trim_cartesian && trimmed1 ) 
				{
					IfcCartesianPoint^ cp = (IfcCartesianPoint^)trim; 
					cp->XYZ( x, y, z);
					gp_Pnt pnt2(x,y,z);
					if(!pnt1.IsEqual(pnt2, BRepBuilderAPI::Precision()))
					{
						BRepBuilderAPI_MakeEdge e (curve,sense_agreement ? pnt1 : pnt2,sense_agreement ? pnt2 : pnt1);
						if ( ! e.IsDone() ) 
						{
							BRepBuilderAPI_EdgeError err = e.Error();
							if ( err == BRepBuilderAPI_PointProjectionFailed ) 
							{
								w.Add(BRepBuilderAPI_MakeEdge(sense_agreement ? pnt1 : pnt2,sense_agreement ? pnt2 : pnt1));
								//Logger::Message(Logger::LOG_WARNING,"Point projection failed for:",l->entity);
							}
						} 
						else 
							w.Add(e.Edge());
						trimmed2 = true;
					}
					break;
				} 
				else if (dynamic_cast<IfcParameterValue^>(trim) && !trim_cartesian && trimmed1 ) 
				{
					IfcParameterValue^ pv = (IfcParameterValue^)trim; 
					const double value = (double)(pv->Value);
					double flt2 = value * parameterFactor;
					if ( isConic && Math::Abs(Math::IEEERemainder(flt2-flt1,(double)(Math::PI*2.0))-0.0f) < BRepBuilderAPI::Precision()) 
					{
						w.Add(BRepBuilderAPI_MakeEdge(curve));
					} 
					else 
					{
						BRepBuilderAPI_MakeEdge e (curve,sense_agreement ? flt1 : flt2,sense_agreement ? flt2 : flt1);
						w.Add(e.Edge());
					}
					trimmed2 = true;
					break;
				}
			}
			TopoDS_Wire wire;
			if ( trimmed2 ) 
			{
					wire = w.Wire();
					
			}
			return wire;

		}

		void XbimFaceBound::Print()
		{

			Bound->Print();

		}
	}
}
}