#include "StdAfx.h"
#include "XbimFaceBound.h"
#include "XbimVertexPoint.h"
#include "XbimGeomPrim.h"

#include <TopAbs.hxx> 
#include "XbimEdgeLoop.h"
#include <BRepBuilderAPI_MakeWire.hxx>
#include <GC_MakeArcOfCircle.hxx>
#include <gp_Ax2.hxx>
#include <gp_Circ.hxx>
#include <Geom_Circle.hxx>
#include <GC_MakeCircle.hxx>
#include <BRepBuilderAPI_MakeEdge.hxx>
#include <BRepBuilderAPI_MakePolygon.hxx>
#include <gp_Elips.hxx> 
#include <GC_MakeArcOfEllipse.hxx>
#include <BRep_Builder.hxx>
#include <TopoDS_Vertex.hxx>
#include <BRepBuilderAPI_MakeEdge.hxx>
#include <GC_MakeSegment.hxx>
//#include <ShapeFix_ShapeTolerance.hxx> 
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
				throw(gcnew Exception(String::Format("XbimFaceBound. Could not BuildShape of type {0}. It is not implemented",type->Name)));
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
				wireSeg.Orientation(seg->SameSense ? TopAbs_FORWARD : TopAbs_REVERSED);
				wire.Add(wireSeg);
				
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
				throw(gcnew Exception(String::Format("XbimFaceBound::Build. BoundedCurve of type {0} is not implemented",type->Name)));	
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
				throw(gcnew Exception(String::Format("XbimFaceBound::Build. Curve of type {0} is not implemented",type->Name)));	
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
			if(nbPoints>2 && first.IsEqual(last, Precision::Confusion()))
				poly.Close();
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
					Type ^ type = c->Position->GetType();
					throw(gcnew Exception(String::Format("XbimFaceBound. Circle with Placement of type {0} is not implemented",type->Name)));	
				}
				else
				{
					Type ^ type = c->Position->GetType();
					throw(gcnew Exception(String::Format("XbimFaceBound. Circle with Placement of type {0} is not implemented",type->Name)));	
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
					throw(gcnew Exception(String::Format("XbimFaceBound. Ellipse with Placement of type {0} is not implemented",type->Name)));	
				}
				else
				{
					Type ^ type = c->Position->GetType();
					throw(gcnew Exception(String::Format("XbimFaceBound. Ellipse with Placement of type {0} is not implemented",type->Name)));	
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
				throw(gcnew Exception(String::Format("XbimFaceBound. CompositeCurveSegments with BasisCurve of type {0} is not implemented",type->Name)));	
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