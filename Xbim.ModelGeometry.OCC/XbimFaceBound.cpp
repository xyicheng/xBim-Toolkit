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
#include <TopTools_Array1OfShape.hxx> 
#include <BRepBuilderApi.hxx>
#include <TopExp.hxx>
#include <Geom_BezierCurve.hxx>
#include <Geom_Parabola.hxx>
#include <Geom_Hyperbola.hxx>
#include <Geom_BSplineCurve.hxx>
#include <Geom_Plane.hxx>
#include <BRepAdaptor_Curve.hxx>
#include <BRepCheck_Analyzer.hxx>
#include <ShapeFix_Shape.hxx>

using namespace System;
using namespace System::Linq;
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


		///Calculates the normal of the wire using Newells method. This takes into account the winding 
		///Curve and line edges are supported
		gp_Vec XbimFaceBound::NewellsNormal(const TopoDS_Wire & bound)
		{
			double x = 0, y = 0, z = 0;
			gp_Pnt currentStart, previousEnd, first;
			int count = 0;
			TopLoc_Location loc;
			Standard_Real start, end;

			for(BRepTools_WireExplorer wEx(bound);wEx.More();wEx.Next())
			{	
				const TopoDS_Vertex& v = wEx.CurrentVertex();
				currentStart = BRep_Tool::Pnt(v);	
				
				Handle(Geom_Curve) c3d =  BRep_Tool::Curve(wEx.Current(), loc, start, end);
				if (!c3d.IsNull()) 
				{
					Handle(Geom_Curve) c3dptr = Handle(Geom_Curve)::DownCast(c3d->Transformed(loc.Transformation()));
					Handle(Standard_Type) cType = c3dptr->DynamicType();
					if(cType == STANDARD_TYPE(Geom_Line))
					{
						if (count>0) 
							AddNewellPoint(previousEnd, currentStart, x,y,z);
						else
							first = currentStart;
						previousEnd = currentStart;
					}
					else if ((cType == STANDARD_TYPE(Geom_Circle)) ||
						(cType == STANDARD_TYPE(Geom_Ellipse)) ||
						(cType == STANDARD_TYPE(Geom_Parabola)) ||
						(cType == STANDARD_TYPE(Geom_Hyperbola)) ||
						(cType == STANDARD_TYPE(Geom_BezierCurve)) ||
						(cType == STANDARD_TYPE(Geom_BSplineCurve)))
					{	
						BRepAdaptor_Curve curve(wEx.Current());
						double us = curve.FirstParameter();
						double ue = curve.LastParameter();
						double umiddle = (us+ue)/2;
						gp_Pnt mid;
						gp_Vec V;
						curve.D1(umiddle, mid, V);
						if (count>0)
						{	
							AddNewellPoint(previousEnd, currentStart, x,y,z);	
							AddNewellPoint(currentStart, mid, x,y,z);
							previousEnd = mid;
						}
						else
						{
							first = currentStart;
							AddNewellPoint(first, mid, x,y,z);
							previousEnd = mid;
						}
						
					}
					else //throw AN EXCEPTION
					{
						throw gcnew XbimGeometryException("Unsupported Edge type");
					}
				}			
				count++;
			}
			//do the last one
			AddNewellPoint(previousEnd, first, x,y,z);
			gp_Vec vec(x,y,z);
			return vec.Normalized();
		}
		/*Interface*/

		void XbimFaceBound::AddNewellPoint(const gp_Pnt& previous, const gp_Pnt& current, double & x, double & y, double & z)
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
			ShapeFix_ShapeTolerance FTol;	
			FTol.SetTolerance(wire,profile->ModelOf->ModelFactors->Precision ,TopAbs_WIRE);
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
			ShapeFix_ShapeTolerance FTol;	
			FTol.SetTolerance(wire,profile->ModelOf->ModelFactors->Precision ,TopAbs_WIRE);
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
			ShapeFix_ShapeTolerance FTol;	
			FTol.SetTolerance(wire,profile->ModelOf->ModelFactors->Precision ,TopAbs_WIRE);
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
			ShapeFix_ShapeTolerance FTol;	
			FTol.SetTolerance(wire,profile->ModelOf->ModelFactors->Precision ,TopAbs_WIRE);
			return wire;
		}
		// SRL: Builds a wire from a composite IfcEllipseProfileDef
		//TODO: SRL: Support for fillet radii needs to be added, nb set the hascurves=true when added
		// and note too that this will decrease performance due to use of OCC for triangulation
		//NB. This is untested as we haven't enountered one yet
		TopoDS_Wire XbimFaceBound::Build(IfcEllipseProfileDef ^ profile, bool% hasCurves)
		{

			IfcAxis2Placement2D^ ax2 = (IfcAxis2Placement2D^)profile->Position;
			gp_Ax2 gpax2(gp_Pnt(ax2->Location->X, ax2->Location->Y,0), gp_Dir(0,0,1),gp_Dir(ax2->P[0].X, ax2->P[0].Y,0.));			
			double semiAx1 =profile->SemiAxis1;
			double semiAx2 =profile->SemiAxis2;
			if(semiAx1<=0 ) 
			{
				XbimModelFactors^ mf = ((IPersistIfcEntity^)profile)->ModelOf->ModelFactors;
				semiAx1 = mf->OneMilliMetre;
				//	throw gcnew XbimGeometryException("Illegal Ellipse Semi Axix, for IfcEllipseProfileDef, must be greater than 0, in entity #" + profile->EntityLabel);
			}
			if(semiAx2 <=0) 
			{
				XbimModelFactors^ mf = ((IPersistIfcEntity^)profile)->ModelOf->ModelFactors;
				semiAx2 =  mf->OneMilliMetre;
				//	throw gcnew XbimGeometryException("Illegal Ellipse Semi Axix, for IfcEllipseProfileDef, must be greater than 0, in entity #" + profile->EntityLabel);
			}
			gp_Elips gc(gpax2,semiAx1, semiAx2);
			Handle(Geom_Ellipse) hellipse = GC_MakeEllipse(gc);
			TopoDS_Edge edge = BRepBuilderAPI_MakeEdge(hellipse);
			BRep_Builder b;
			TopoDS_Wire wire;
			b.MakeWire(wire);
			b.Add(wire,edge);
			ShapeFix_ShapeTolerance FTol;	
			FTol.SetTolerance(wire,profile->ModelOf->ModelFactors->Precision ,TopAbs_WIRE);
			return  wire;
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
			ShapeFix_ShapeTolerance FTol;	
			FTol.SetTolerance(wire,profile->ModelOf->ModelFactors->Precision ,TopAbs_WIRE);
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
			ShapeFix_ShapeTolerance FTol;	
			FTol.SetTolerance(wire,profile->ModelOf->ModelFactors->Precision ,TopAbs_WIRE);
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
				XbimModelFactors^ mf = ((IPersistIfcEntity^)profile)->ModelOf->ModelFactors;
				tW = mf->OneMilliMetre * 3;
				Logger->WarnFormat("Illegal wall thickness for IfcCShapeProfileDef, it must be greater than 0, in entity #{0}. Adjusted to be 3mm thick",profile->EntityLabel);
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
			ShapeFix_ShapeTolerance FTol;	
			FTol.SetTolerance(wire,profile->ModelOf->ModelFactors->Precision ,TopAbs_WIRE);
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
			ShapeFix_ShapeTolerance FTol;	
			FTol.SetTolerance(wire,profile->ModelOf->ModelFactors->Precision ,TopAbs_WIRE);
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
			bool haveWarned=false;
			BRepBuilderAPI_MakeWire wire;
			XbimModelFactors^ mf = ((IPersistIfcEntity^)cCurve)->ModelOf->ModelFactors;
			ShapeFix_ShapeTolerance FTol;
			double precision = mf->PrecisionBoolean; //use a courser precision for trimmed curves
			double currentPrecision = precision;
			double maxTolerance = mf->PrecisionBooleanMax;
			for each(IfcCompositeCurveSegment^ seg in cCurve->Segments)
			{		

				///TODO: Need to add support for curve segment continuity a moment only continuous supported
				TopoDS_Wire wireSeg = Build(seg->ParentCurve, hasCurves);
				if(!wireSeg.IsNull())
				{
					if(!seg->SameSense) wireSeg.Reverse();
retryAddWire:	
					
					FTol.SetTolerance(wireSeg, currentPrecision, TopAbs_WIRE);	
					wire.Add(wireSeg);				
					if(!wire.IsDone() ) 
					{
						currentPrecision*=10;
						if(currentPrecision <= maxTolerance)
							goto retryAddWire;
						else
						{		
							haveWarned=true;
							Logger->WarnFormat("IfcCompositeCurveSegment {0} was not contiguous with any edges in IfcCompositeCurve #{1}. It has been ignored",seg->EntityLabel,cCurve->EntityLabel);
						}
					}
				}
			}

			if ( wire.IsDone()) 
			{
				TopoDS_Wire w = wire.Wire();
				if( BRepCheck_Analyzer(w, Standard_True).IsValid() == Standard_True) 
					return w;
				else
				{
					double toleranceMax = cCurve->ModelOf->ModelFactors->PrecisionMax;
					ShapeFix_Shape sfs(w);
					sfs.SetMinTolerance(mf->Precision);
					sfs.SetMaxTolerance(mf->OneMilliMetre*50);
					sfs.Perform();
					if( BRepCheck_Analyzer(sfs.Shape(), Standard_True).IsValid() == Standard_True && sfs.Shape().ShapeType()==TopAbs_WIRE) //in release builds except the geometry is not compliant
						return TopoDS::Wire(sfs.Shape());
					else
					{
						Logger->WarnFormat("Invalid IfcCompositeCurveSegment #{0} found. Discarded",cCurve->EntityLabel);
						return TopoDS_Wire();
					}
				}
			}
			else if(!haveWarned) //don't do it twice
			{
				BRepBuilderAPI_WireError err = wire.Error();
				switch (err)
				{
				case BRepBuilderAPI_EmptyWire:
					Logger->WarnFormat("Illegal bound found in IfcCompositeCurve = #{0}, it has no edges. Ignored",cCurve->EntityLabel);
					break;
				case BRepBuilderAPI_DisconnectedWire:
					Logger->WarnFormat("Illegal bound found in IfcCompositeCurve = #{0}, all edges could not be connected. Ignored",cCurve->EntityLabel);
					break;
				case BRepBuilderAPI_NonManifoldWire:
					Logger->WarnFormat("Illegal found in IfcCompositeCurve = #{0}, it is non-manifold. Ignored",cCurve->EntityLabel);
					break;
				default:
					Logger->WarnFormat("Illegal bound found in IfcCompositeCurve = #{0}, unknown error. Ignored",cCurve->EntityLabel);
					break;
				}
				return TopoDS_Wire();
			}
			else
				return TopoDS_Wire();
		}

		//Builds a wire from a CircleProfileDef
		TopoDS_Wire XbimFaceBound::Build(IfcCircleProfileDef ^ circProfile, bool% hasCurves)
		{
			hasCurves=true;
			IfcAxis2Placement2D^ ax2 = (IfcAxis2Placement2D^)circProfile->Position;
			gp_Ax2 gpax2(gp_Pnt(ax2->Location->X, ax2->Location->Y,0), gp_Dir(0,0,1),gp_Dir(ax2->P[0].X, ax2->P[0].Y,0.));			
			gp_Circ gc(gpax2,circProfile->Radius);
			Handle(Geom_Circle) hCirc = GC_MakeCircle(gc);
			TopoDS_Edge edge = BRepBuilderAPI_MakeEdge(hCirc);
			BRep_Builder b;
			TopoDS_Wire w;
			b.MakeWire(w);
			b.Add(w,edge);
			ShapeFix_ShapeTolerance FTol;	
			FTol.SetTolerance(w,circProfile->ModelOf->ModelFactors->Precision ,TopAbs_WIRE);
			return w;
		}

		TopoDS_Wire XbimFaceBound::Build(IfcPolyLoop ^ loop, bool% hasCurves)
		{
			int lastPt=loop->Polygon->Count;
			if(lastPt<3) 
				Logger->WarnFormat("Invalid loop in IfcPolyloop #{0}, it has less than three points. Loop discarded",loop->EntityLabel);
			double tolerance = loop->ModelOf->ModelFactors->Precision;
			//check we haven't got duplicate start and end points
			IfcCartesianPoint^ first = Enumerable::First(loop->Polygon);
			IfcCartesianPoint^ last = Enumerable::Last(loop->Polygon);
			if(first->IsEqual(last,tolerance))
			{
				Logger->WarnFormat("Invalid edge found in IfcPolyloop = #{0}, Start = #{7}({1}, {2}, {3}) End = #{8}({4}, {5}, {6}). Edge discarded",	loop->EntityLabel, first->X,first->Y,first->Z,last->X,last->Y,last->Z, first->EntityLabel, last->EntityLabel);
				lastPt--;
				if(lastPt<3) 
				{
					Logger->WarnFormat("Invalid loop in IfcPolyloop #{0}, it has less than three points. Loop discarded",loop->EntityLabel);
					return TopoDS_Wire();
				}
			}

			int totalEdges=0;	
			bool is3D = (loop->Polygon[0]->Dim == 3);
			BRep_Builder builder;
			TopoDS_Wire wire;
			builder.MakeWire(wire);
			for (int p=1; p<=lastPt; p++)
			{
				IfcCartesianPoint^ p1;
				IfcCartesianPoint^ p2;
				if(p==lastPt)
				{
					p2 = loop->Polygon[0];
					p1 = loop->Polygon[p-1];	
				}
				else
				{
					p1= loop->Polygon[p-1];
					p2 =loop->Polygon[p];
				}
				TopoDS_Vertex v1, v2;
				gp_Pnt pt1(p1->X, p1->Y, is3D ? p1->Z : 0);
				gp_Pnt pt2(p2->X, p2->Y, is3D ? p2->Z : 0);
				
				builder.MakeVertex(v1,pt1,tolerance);
				builder.MakeVertex(v2,pt2,tolerance);
				BRepBuilderAPI_MakeEdge edgeMaker(v1,v2);
				BRepBuilderAPI_EdgeError edgeErr = edgeMaker.Error();
				if(edgeErr!=BRepBuilderAPI_EdgeDone)
				{	
					String^ errMsg = XbimEdge::GetBuildEdgeErrorMessage(edgeErr);
					Logger->WarnFormat("Invalid edge, {9},  in IfcPolyloop = #{0}. Start = #{7}({1}, {2}, {3}) End = #{8}({4}, {5}, {6}).\nEdge discarded",
						loop->EntityLabel, pt1.X(),pt1.Y(),pt1.Z(),pt2.X(),pt2.Y(),pt2.Z(), p1, p2, errMsg);
				}
				else
				{
					TopoDS_Edge edge = edgeMaker.Edge();
					builder.Add(wire,edge);
					totalEdges++;
				}
			}
			if(totalEdges<3)
			{
				Logger->WarnFormat("Invalid loop. IfcPolyloop = #{0} only has {1} edge(s), a minimum of 3 is required. Bound discarded",loop->EntityLabel, totalEdges);
				return TopoDS_Wire();
			}
			wire.Closed(Standard_True);
			return wire;
		}


		//Builds a wire from a RectangleProfileDef
		TopoDS_Wire XbimFaceBound::Build(IfcRectangleProfileDef ^ rectProfile, bool% hasCurves)
		{
			
			if(rectProfile->XDim<=0 || rectProfile->YDim<=0)
			{
				Logger->WarnFormat("Invalid IfcRectangleProfileDef #{0}, XDim = {1}, YDim = {2}. Discarded",rectProfile->EntityLabel, rectProfile->XDim,rectProfile->YDim);
				return TopoDS_Wire();
			}

			double xOff = rectProfile->XDim/2;
			double yOff= rectProfile->YDim/2;
			double precision = rectProfile->ModelOf->ModelFactors->Precision;
			gp_Pnt bl(-xOff,-yOff,0);
			gp_Pnt br(xOff,-yOff,0);
			gp_Pnt tr(xOff,yOff,0);
			gp_Pnt tl(-xOff,yOff,0);
			BRep_Builder builder;
			TopoDS_Vertex vbl,vbr,vtr,vtl;
			builder.MakeVertex(vbl,bl,precision);
			builder.MakeVertex(vbr,br,precision);
			builder.MakeVertex(vtr,tr,precision);
			builder.MakeVertex(vtl,tl,precision);
			TopoDS_Wire wire;
			builder.MakeWire(wire);
			builder.Add(wire, BRepBuilderAPI_MakeEdge(vbl,vbr));
			builder.Add(wire, BRepBuilderAPI_MakeEdge(vbr,vtr));
			builder.Add(wire, BRepBuilderAPI_MakeEdge(vtr,vtl));
			builder.Add(wire, BRepBuilderAPI_MakeEdge(vtl,vbl));
	
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
				gp_Ax2 gpax2(gp_Pnt(ax2->Location->X, ax2->Location->Y,0), gp_Dir(0,0,1),gp_Dir(ax2->P[0].X, ax2->P[0].Y,0.));			
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
			BRep_Builder b;
			TopoDS_Wire w;
			b.MakeWire(w);
			b.Add(w, e);
			// set the tolerance for this shape.
			ShapeFix_ShapeTolerance FTol;	
			FTol.SetTolerance(w,circle->ModelOf->ModelFactors->Precision ,TopAbs_WIRE);
			return w;
		}

		TopoDS_Wire XbimFaceBound::Build(IfcLine ^ line, bool% hasCurves)
		{
			IfcCartesianPoint^ cp = line->Pnt;
			IfcVector^ dir = line->Dir;
			gp_Pnt pnt(cp->X,cp->Y,cp->Z);
			XbimVector3D v3d = dir->XbimVector3D();
			gp_Vec vec(v3d.X,v3d.Y,v3d.Z);
			BRep_Builder b;
			TopoDS_Wire w;
			b.MakeWire(w);
			b.Add(w,BRepBuilderAPI_MakeEdge(GC_MakeLine(pnt,vec),0,dir->Magnitude));	
			// set the tolerance for this shape.
			ShapeFix_ShapeTolerance FTol;	
			FTol.SetTolerance(w, line->ModelOf->ModelFactors->Precision ,TopAbs_WIRE);
			return w;	
		}

		TopoDS_Wire XbimFaceBound::Build(IfcPolyline ^ pLine, bool% hasCurves)
		{
			
			int total = pLine->Points->Count;
			if(total<2)
			{
				Logger->WarnFormat("Line with zero length found in IfcPolyline = #{0}. Ignored",pLine->EntityLabel);
				return TopoDS_Wire();
			}		
			
			double tolerance = pLine->ModelOf->ModelFactors->Precision;
			//Make all the vertices
			Standard_Boolean closed = Standard_False;
			
			if(Enumerable::First<IfcCartesianPoint^>(pLine->Points)->IsEqual(Enumerable::Last<IfcCartesianPoint^>(pLine->Points),tolerance))
			{
				total--; //skip the last point
				if(total>2) closed = Standard_True ;//closed polyline with two points is not a valid closed shape
			}
			
			TopTools_Array1OfShape vertexStore(1,pLine->Points->Count+1);
			BRep_Builder builder;	
			TopoDS_Wire wire;
			builder.MakeWire(wire);
			bool is3D = pLine->Dim == 3;

			gp_Pnt first;
			gp_Pnt previous;
			
			for (int i=0; i<total; i++) //add all the points into unique collection
			{
				IfcCartesianPoint^ p = pLine->Points[i];
				gp_Pnt current(p->X, p->Y, is3D ? p->Z : 0);
				TopoDS_Vertex v;
				builder.MakeVertex(v, current, tolerance);
				vertexStore.SetValue(i+1,v); 	
			}
			int firstIdx = 1;
			bool edgeAdded = false;
			for(int pt = 1; pt <= total; pt++)
			{
				int next = pt+1;
				if(pt==total) //we are at the last point
				{
					if(closed==Standard_True) //add the last edge in
						next=firstIdx;
					else
						break; //stop
				}
				const TopoDS_Vertex& v1=TopoDS::Vertex(vertexStore.Value(pt));
				const TopoDS_Vertex& v2=TopoDS::Vertex(vertexStore.Value(next));
				try
				{
					BRepBuilderAPI_MakeEdge edgeMaker(v1,v2);	
					BRepBuilderAPI_EdgeError edgeErr = edgeMaker.Error();
					if(edgeErr!=BRepBuilderAPI_EdgeDone)
						{
							gp_Pnt p1 =BRep_Tool::Pnt(v1);
							gp_Pnt p2 =BRep_Tool::Pnt(v2);
							String^ errMsg = XbimEdge::GetBuildEdgeErrorMessage(edgeErr);
							Logger->InfoFormat("Invalid edge found in IfcPolyline = #{0}, Start = {1}, {2}, {3} End = {4}, {5}, {6}. Ignored",
								pLine->EntityLabel, p1.X(),p1.Y(),p1.Z(),p2.X(),p2.Y(),p2.Z());
							
						}
					else
					{
						builder.Add(wire, edgeMaker.Edge());
						if(!edgeAdded) firstIdx=pt; //we need this in case the first edge is invalid and we need to close properly
						edgeAdded=true;
					}
				}
				catch( System::Runtime::InteropServices::SEHException^ )
				{
					gp_Pnt p1 =BRep_Tool::Pnt(v1);
					gp_Pnt p2 =BRep_Tool::Pnt(v2);
					Logger->InfoFormat("Invalid edge found in IfcPolyline = #{0}, Start = {1}, {2}, {3} End = {4}, {5}, {6}. Ignored",
						pLine->EntityLabel, p1.X(),p1.Y(),p1.Z(),p2.X(),p2.Y(),p2.Z());
				}
			}
			wire.Closed(closed);
			if( BRepCheck_Analyzer(wire, Standard_True).IsValid() == Standard_True) 
				return wire;
			else
			{
				
				double toleranceMax = pLine->ModelOf->ModelFactors->PrecisionMax;
				ShapeFix_Shape sfs(wire);
				sfs.SetPrecision(tolerance);
				sfs.SetMinTolerance(tolerance);
				sfs.SetMaxTolerance(toleranceMax);
				sfs.Perform();
				
				if( BRepCheck_Analyzer(sfs.Shape(), Standard_True).IsValid() == Standard_True && sfs.Shape().ShapeType()==TopAbs_WIRE) //in release builds except the geometry is not compliant
					return TopoDS::Wire(sfs.Shape());
				else
				{
					Logger->WarnFormat("Invalid IfcPolyline #{0} found. Discarded",pLine->EntityLabel);
					return TopoDS_Wire();
				}
			}
		}


		TopoDS_Wire XbimFaceBound::Build(IfcTrimmedCurve ^ tCurve, bool% hasCurves)
		{
			ShapeFix_ShapeTolerance FTol;	
			
			bool isConic = (dynamic_cast<IfcConic^>(tCurve->BasisCurve)!=nullptr);

			XbimModelFactors^ mf = ((IPersistIfcEntity^)tCurve)->ModelOf->ModelFactors;
			double tolerance = mf->Precision;
			double toleranceMax = mf->PrecisionMax;
			double parameterFactor =  isConic ? mf->AngleToRadiansConversionFactor : 1;
			Handle(Geom_Curve) curve;
			bool rotateElipse;
			IfcAxis2Placement2D^ ax2;
			//it could be based on a circle, ellipse or line
			if(dynamic_cast<IfcCircle^>(tCurve->BasisCurve))
			{
				hasCurves=true;
				IfcCircle^ c = (IfcCircle^) tCurve->BasisCurve;
				if(dynamic_cast<IfcAxis2Placement2D^>(c->Position))
				{
					IfcAxis2Placement2D^ ax2 = (IfcAxis2Placement2D^)c->Position;
					gp_Ax2 gpax2(gp_Pnt(ax2->Location->X, ax2->Location->Y,0), gp_Dir(0,0,1),gp_Dir(ax2->P[0].X, ax2->P[0].Y,0.));	
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
					ax2 = (IfcAxis2Placement2D^)c->Position;
					double s1;
					double s2;
					
					if( c->SemiAxis1 > c->SemiAxis2)
					{
						s1=c->SemiAxis1;
						s2=c->SemiAxis2;
						rotateElipse=false;
					}
					else //either same or two is larger than 1
					{
						s1=c->SemiAxis2;
						s2=c->SemiAxis1;
						rotateElipse=true;
					}

					gp_Ax2 gpax2(gp_Pnt(ax2->Location->X, ax2->Location->Y,0), gp_Dir(0,0,1),gp_Dir(ax2->P[rotateElipse?1:0].X, ax2->P[rotateElipse?1:0].Y,0.));	
					
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
				gp_Pnt pnt(cp->X,cp->Y,cp->Dim==3?cp->Z:0);
				
				gp_Vec vec(dir->Orientation->X,dir->Orientation->Y,dir->Dim==3?dir->Orientation->Z:0);
				parameterFactor=dir->Magnitude;
				vec*=dir->Magnitude;
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
			BRep_Builder b;
			TopoDS_Wire w;
			b.MakeWire(w);
			for each ( IfcTrimmingSelect^ trim in tCurve->Trim2 ) 
			{
				if (  dynamic_cast<IfcCartesianPoint^>(trim) && trim_cartesian && trimmed1 ) 
				{
					IfcCartesianPoint^ cp = (IfcCartesianPoint^)trim; 
					cp->XYZ( x, y, z);
					gp_Pnt pnt2(x,y,z);
					if(!pnt1.IsEqual(pnt2, tolerance))
					{

						if(rotateElipse) //if we have had to roate the elipse, then rotate the trims
						{
							gp_Ax1 centre(gp_Pnt(ax2->Location->X, ax2->Location->Y, 0),gp_Dir(0,0,1));
							pnt1.Rotate(centre,90.0);
							pnt2.Rotate(centre,90.0);
						}
						TopoDS_Vertex v1,v2;
						double currentTolerance = tolerance;
						b.MakeVertex(v1,pnt1,currentTolerance);
						b.MakeVertex(v2,pnt2,currentTolerance); 
						if(dynamic_cast<IfcLine^>(tCurve->BasisCurve)) //we have a line and two points, just build it
						{
							b.Add(w,BRepBuilderAPI_MakeEdge(sense_agreement ? v1 : v2,sense_agreement ? v2 : v1));
						}
						else //we need to trim
						{
							
TryMakeEdge:
							BRepBuilderAPI_MakeEdge e (curve,sense_agreement ? v1 : v2,sense_agreement ? v2 : v1);
							BRepBuilderAPI_EdgeError err = e.Error();
							if ( err!=BRepBuilderAPI_EdgeDone) 
							{
								currentTolerance*=10;
								if(currentTolerance<=toleranceMax) 
								{
									FTol.SetTolerance(v1,currentTolerance);
									FTol.SetTolerance(v2,currentTolerance);
									goto TryMakeEdge;
								}
								String^ errMsg = XbimEdge::GetBuildEdgeErrorMessage(err);
								Logger->WarnFormat("Construction of Trimmed Curve #{0}, failed, {1}. A line segment has been used",tCurve->EntityLabel, errMsg);
								b.Add(w,BRepBuilderAPI_MakeEdge(sense_agreement ? v1 : v2,sense_agreement ? v2 : v1));
							}
							else 
								b.Add(w, e.Edge());
						}
						trimmed2 = true;
					}
					break;
				} 
				else if (dynamic_cast<IfcParameterValue^>(trim) && !trim_cartesian && trimmed1 ) 
				{
					IfcParameterValue^ pv = (IfcParameterValue^)trim; 
					const double value = (double)(pv->Value);
					double flt2 = (value * parameterFactor);
					if ( isConic && Math::Abs(Math::IEEERemainder(flt2-flt1,(double)(Math::PI*2.0))-0.0f) <= BRepBuilderAPI::Precision()) 
					{
						
						b.Add(w,BRepBuilderAPI_MakeEdge(curve));
					} 
					else 
					{
						if(rotateElipse) //if we have had to roate the elipse, then rotate the trims
						{
							flt1+=(90*parameterFactor);
							flt2+=(90*parameterFactor);
						}
						BRepBuilderAPI_MakeEdge e (curve,sense_agreement ? flt1 : flt2,sense_agreement ? flt2 : flt1);
						b.Add(w,e.Edge());
					}
					trimmed2 = true;
					break;
				}
			}
			
			return w;

		}

		void XbimFaceBound::Print()
		{

			Bound->Print();

		}
	}
}
}