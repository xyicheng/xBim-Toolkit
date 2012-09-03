#include "StdAfx.h"
#include "XbimFace.h"

#include "XbimGeomPrim.h"
#include <gp_Ax3.hxx>
#include <BRepBuilderAPI_MakeFace.hxx>
#include <BRep_Builder.hxx>
#include <BRepLib_FindSurface.hxx>
#include <BRepBuilderAPI_FindPlane.hxx>
#include <ShapeFix_Wireframe.hxx>
#include <BRepGProp_Face.hxx>
#include <ShapeFix_ShapeTolerance.hxx>
namespace Xbim
{
	namespace ModelGeometry
	{
		XbimFace::XbimFace(const TopoDS_Face & face)
		{
			nativeHandle = new TopoDS_Face();
			*nativeHandle = face;
		}

		/*Interface*/

		System::Collections::Generic::IEnumerable<XbimFaceBound^>^ XbimFace::Bounds::get()
		{

			return this;
		}

		XbimFaceOuterBound^ XbimFace::OuterBound::get()
		{
			return gcnew XbimFaceOuterBound(BRepTools::OuterWire(*nativeHandle), *nativeHandle);
		}

		gp_Vec XbimFace::TopoNormal(const TopoDS_Face & face)
		{
			BRepGProp_Face prop(face);
			gp_Pnt centre;
			gp_Vec normalDir;
			double u1,u2,v1,v2;
			prop.Bounds(u1,u2,v1,v2);
			prop.Normal((u1+u2)/2.0,(v1+v2)/2.0,centre,normalDir);						
			return normalDir;
		}

		//static builders
		//Builds a face from a ProfileDef
		TopoDS_Face XbimFace::Build(IfcProfileDef ^ profile, bool% hasCurves)
		{
			if(dynamic_cast<IfcArbitraryClosedProfileDef^>(profile))
				return Build((IfcArbitraryClosedProfileDef^)profile, hasCurves);	 
			else if(dynamic_cast<IfcParameterizedProfileDef^>(profile))
				return  XbimFace::Build((IfcParameterizedProfileDef^)profile,hasCurves);
			else if(dynamic_cast<IfcDerivedProfileDef^>(profile))
				return XbimFace::Build((IfcDerivedProfileDef^)profile,hasCurves);
			return TopoDS_Face();
		}
		// SRL: Builds a face from a IfcParameterizedProfileDef
		TopoDS_Face XbimFace::Build(IfcParameterizedProfileDef ^ profile, bool% hasCurves)
		{
			TopoDS_Face face;
			if(dynamic_cast<IfcRectangleProfileDef^>(profile))
				face = XbimFace::Build((IfcRectangleProfileDef^)profile,hasCurves);	
			else if(dynamic_cast<IfcCircleProfileDef^>(profile))
				face = XbimFace::Build((IfcCircleProfileDef^)profile,hasCurves);	
			else if(dynamic_cast<IfcLShapeProfileDef^>(profile))
				face = XbimFace::Build((IfcLShapeProfileDef^)profile,hasCurves);	
			else if(dynamic_cast<IfcUShapeProfileDef^>(profile))
				face = XbimFace::Build((IfcUShapeProfileDef^)profile,hasCurves);	
			else if(dynamic_cast<IfcIShapeProfileDef^>(profile))
				face = XbimFace::Build((IfcIShapeProfileDef^)profile,hasCurves);
			else if(dynamic_cast<IfcCShapeProfileDef^>(profile))
				face = XbimFace::Build((IfcCShapeProfileDef^)profile,hasCurves);
			else if(dynamic_cast<IfcTShapeProfileDef^>(profile))
				face = XbimFace::Build((IfcTShapeProfileDef^)profile,hasCurves);
			else if(dynamic_cast<IfcCraneRailFShapeProfileDef^>(profile))
				face = XbimFace::Build((IfcCraneRailFShapeProfileDef^)profile,hasCurves);
			else if(dynamic_cast<IfcCraneRailAShapeProfileDef^>(profile))
				face = XbimFace::Build((IfcCraneRailAShapeProfileDef^)profile,hasCurves);
			else if(dynamic_cast<IfcEllipseProfileDef^>(profile))
				face = XbimFace::Build((IfcEllipseProfileDef^)profile,hasCurves);
			return face;
		}
		// SRL: Builds a face from a IfcCraneRailFShapeProfileDef
		TopoDS_Face XbimFace::Build(IfcCraneRailFShapeProfileDef ^ profile, bool% hasCurves)
		{
			BRepBuilderAPI_MakeFace faceBlder(XbimFaceBound::Build(profile, hasCurves));
			return faceBlder.Face();
		}

		// SRL: Builds a face from a IfcCraneRailAShapeProfileDef
		TopoDS_Face XbimFace::Build(IfcCraneRailAShapeProfileDef ^ profile, bool% hasCurves)
		{
			BRepBuilderAPI_MakeFace faceBlder(XbimFaceBound::Build(profile, hasCurves));
			return faceBlder.Face();
		}
		// SRL: Builds a face from a IfcEllipseProfileDef
		TopoDS_Face XbimFace::Build(IfcEllipseProfileDef ^ profile, bool% hasCurves)
		{
			BRepBuilderAPI_MakeFace faceBlder(XbimFaceBound::Build(profile, hasCurves));
			return faceBlder.Face();
		}
		// SRL: Builds a face from a IfcIShapeProfileDef
		TopoDS_Face XbimFace::Build(IfcIShapeProfileDef ^ profile, bool% hasCurves)
		{
			BRepBuilderAPI_MakeFace faceBlder(XbimFaceBound::Build(profile, hasCurves));
			return faceBlder.Face();
		}

			// SRL: Builds a face from a IfcZShapeProfileDef
		TopoDS_Face XbimFace::Build(IfcZShapeProfileDef ^ profile, bool% hasCurves)
		{
			BRepBuilderAPI_MakeFace faceBlder(XbimFaceBound::Build(profile, hasCurves));
			return faceBlder.Face();
		}

		// AK: Builds a face from a IfcLShapeProfileDef
		TopoDS_Face XbimFace::Build(IfcLShapeProfileDef ^ profile, bool% hasCurves)
		{
			BRepBuilderAPI_MakeFace faceBlder(XbimFaceBound::Build(profile, hasCurves));
			
			return faceBlder.Face();

		}

		// AK: Builds a face from a IfcUShapeProfileDef
		TopoDS_Face XbimFace::Build(IfcUShapeProfileDef ^ profile, bool% hasCurves)
		{
			BRepBuilderAPI_MakeFace faceBlder(XbimFaceBound::Build(profile, hasCurves));
			
			return faceBlder.Face();

		}
		
		// SRL: Builds a face from a IfcTShapeProfileDef
		TopoDS_Face XbimFace::Build(IfcTShapeProfileDef ^ profile, bool% hasCurves)
		{
			BRepBuilderAPI_MakeFace faceBlder(XbimFaceBound::Build(profile, hasCurves));
			return faceBlder.Face();
		}

		// SRL: Builds a face from a IfcCShapeProfileDef
		TopoDS_Face XbimFace::Build(IfcCShapeProfileDef ^ profile, bool% hasCurves)
		{
			BRepBuilderAPI_MakeFace faceBlder(XbimFaceBound::Build(profile, hasCurves));
			return faceBlder.Face();

		}

		//Builds a face from a ArbitraryClosedProfileDef
		TopoDS_Face XbimFace::Build(IfcArbitraryClosedProfileDef ^ profile, bool% hasCurves)
		{
			if(dynamic_cast<IfcArbitraryProfileDefWithVoids^>(profile))
				return Build((IfcArbitraryProfileDefWithVoids^)profile, hasCurves);
			else
			{
				TopoDS_Wire wire = XbimFaceBound::Build(profile, hasCurves);
				BRepBuilderAPI_MakeFace faceMaker(wire, false);
				BRepBuilderAPI_FaceError er = faceMaker.Error();
				if ( er == BRepBuilderAPI_NotPlanar ) {
					ShapeFix_ShapeTolerance FTol;
					FTol.SetTolerance(wire, 0.001, TopAbs_WIRE);
					BRepBuilderAPI_MakeFace faceMaker2(wire, false);
					er = faceMaker2.Error();
					if ( er != BRepBuilderAPI_FaceDone )
						return TopoDS_Face();
					else
						return faceMaker2.Face();
				}
				else
					return faceMaker.Face();
				
			}

		}
		//Builds a face from a IfcDerivedProfileDef
		TopoDS_Face XbimFace::Build(IfcDerivedProfileDef ^ profile, bool% hasCurves)
		{
			TopoDS_Face face = XbimFace::Build(profile->ParentProfile,hasCurves);
			gp_Trsf trsf = XbimGeomPrim::ToTransform(profile->Operator);
			face.Move(TopLoc_Location(trsf));
			return face;
		}

		//Builds a face from a ArbitraryProfileDefWithVoids
		TopoDS_Face XbimFace::Build(IfcArbitraryProfileDefWithVoids ^ profile, bool% hasCurves)
		{
			TopoDS_Wire wire = XbimFaceBound::Build(profile->OuterCurve, hasCurves);
			TopoDS_Face face;
				BRepBuilderAPI_MakeFace faceMaker(wire, false);
				BRepBuilderAPI_FaceError er = faceMaker.Error();
				if ( er == BRepBuilderAPI_NotPlanar ) {
					ShapeFix_ShapeTolerance FTol;
					FTol.SetTolerance(wire, 0.001, TopAbs_WIRE);
					BRepBuilderAPI_MakeFace faceMaker2(wire, false);
					er = faceMaker2.Error();
					if ( er != BRepBuilderAPI_FaceDone )
						return TopoDS_Face();
					else
						face= faceMaker2.Face();
				}
				else
					face= faceMaker.Face();
			gp_Vec nn =   XbimFaceBound::NewellsNormal(wire);
			BRepBuilderAPI_MakeFace faceMaker3(face);
			for each( IfcCurve^ curve in profile->InnerCurves)
			{
				TopoDS_Wire innerWire = XbimFaceBound::Build(curve, hasCurves);

				gp_Vec inorm =  XbimFaceBound::NewellsNormal(innerWire);
				if ( inorm.Dot(nn) >= 0 ) //inner wire should be reverse of outer wire
				{
					TopAbs_Orientation o = innerWire.Orientation();
					innerWire.Orientation(o == TopAbs_FORWARD ? TopAbs_REVERSED : TopAbs_FORWARD);
				}
				faceMaker3.Add(innerWire);
			}
			return faceMaker3.Face();;
		}

		//Builds a face from a CircleProfileDef
		TopoDS_Face XbimFace::Build(IfcCircleProfileDef ^ profile, bool% hasCurves)
		{
			BRepBuilderAPI_MakeFace faceBlder(XbimFaceBound::Build(profile, hasCurves));
			return faceBlder.Face();
		}

		//Builds a face from a composite curve
		TopoDS_Face XbimFace::Build(IfcCompositeCurve ^ cCurve, bool% hasCurves)
		{
			BRepBuilderAPI_MakeFace faceBlder(XbimFaceBound::Build(cCurve, hasCurves));

			return faceBlder.Face();
		}

		//Builds a face from a Polyline
		TopoDS_Face XbimFace::Build(IfcPolyline ^ pline, bool% hasCurves)
		{
			TopoDS_Wire wire = XbimFaceBound::Build(pline, hasCurves);
			BRepLib_FindSurface FS(wire, 1e-3, Standard_False); //need to lower tolerance as many faces in facetations are not coplanar
			BRepBuilderAPI_MakeFace faceBlder(FS.Surface(), wire);	
			return faceBlder.Face();	
		}

		//Builds a face from a PolyLoop
		TopoDS_Face XbimFace::Build(IfcPolyLoop ^ loop, bool% hasCurves)
		{			
			return Build(loop, true, hasCurves);
		}

		//Builds a face from a PolyLoop
		TopoDS_Face XbimFace::Build(IfcPolyLoop ^ loop, bool sense, bool% hasCurves)
		{			
			TopoDS_Wire wire = XbimFaceBound::Build(loop, hasCurves);
			BRepBuilderAPI_FindPlane  FS(wire); //need to lower tolerance as many faces in facetations are not coplanar
			wire.Orientation(sense?TopAbs_FORWARD :TopAbs_REVERSED);
			BRepBuilderAPI_MakeFace faceBlder(FS.Plane(), wire);
			return faceBlder.Face();	

		}

		//Builds a face from a RectangleProfileDef
		TopoDS_Face XbimFace::Build(IfcRectangleProfileDef ^ profile, bool% hasCurves)
		{
			TopoDS_Wire wire = XbimFaceBound::Build(profile, hasCurves);
			BRepBuilderAPI_MakeFace faceBlder(wire);
			return faceBlder.Face();
		}

		//Builds a face from a Surface
		TopoDS_Face XbimFace::Build(IfcSurface ^ surface, bool% hasCurves)
		{
			if(dynamic_cast<IfcPlane^>(surface))
				return Build((IfcPlane^)surface, hasCurves);
			else if(dynamic_cast<IfcSurfaceOfRevolution^>(surface))
				throw(gcnew NotImplementedException("XbimFace. Support for SurfaceOfRevolution is not implemented"));
			else if(dynamic_cast<IfcSurfaceOfLinearExtrusion^>(surface))
				throw(gcnew NotImplementedException("XbimFace. Support for SurfaceOfLinearExtrusion is not implemented"));
			else if(dynamic_cast<IfcCurveBoundedPlane^>(surface))
				throw(gcnew NotImplementedException("XbimFace. Support for CurveBoundedPlane is not implemented"));
			else if(dynamic_cast<IfcRectangularTrimmedSurface^>(surface))
				throw(gcnew NotImplementedException("XbimFace. Support for RectangularTrimmedSurface is not implemented"));
			else if(dynamic_cast<IfcBoundedSurface^>(surface))
				throw(gcnew NotImplementedException("XbimFace. Support for BoundedSurface is not implemented"));
			else
			{
				Type ^ type = surface->GetType();
				throw(gcnew NotImplementedException(String::Format("XbimFace. BuildFace of type {0} is not implemented",type->Name)));
			}

		}

		//Builds a face from a Plane
		TopoDS_Face XbimFace::Build(IfcPlane ^ plane, bool% hasCurves)
		{
			gp_Ax3 ax3 = XbimGeomPrim::ToAx3(plane->Position);
			gp_Pln pln(ax3);
			BRepBuilderAPI_MakeFace  builder(pln);
			return builder.Face();
		}

		void XbimFace::Print()
		{
			System::Diagnostics::Debug::WriteLine("Outer Bound");
			OuterBound->Print();
			System::Diagnostics::Debug::WriteLine("End Outer Bound");
		}
	}
}