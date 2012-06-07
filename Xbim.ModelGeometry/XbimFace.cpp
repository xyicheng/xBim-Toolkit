#include "StdAfx.h"
#include "XbimFace.h"

#include "XbimGeomPrim.h"
#include <gp_Ax3.hxx>
#include <BRepBuilderAPI_MakeFace.hxx>
#include <BRep_Builder.hxx>
#include <BRepLib_FindSurface.hxx>
#include <BRepBuilderAPI_FindPlane.hxx>
#include <ShapeFix_Wireframe.hxx>
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

		//static builders
		//Builds a face from a ProfileDef
		TopoDS_Face XbimFace::Build(IfcProfileDef ^ profile, bool% hasCurves)
		{
			if(dynamic_cast<IfcArbitraryClosedProfileDef^>(profile))
				return Build((IfcArbitraryClosedProfileDef^)profile, hasCurves);
			else if(dynamic_cast<IfcRectangleProfileDef^>(profile))
				return Build((IfcRectangleProfileDef^)profile, hasCurves);
			else if(dynamic_cast<IfcCircleProfileDef^>(profile))
				return Build((IfcCircleProfileDef^)profile, hasCurves);
			else
			{
				Type ^ type = profile->GetType();
				throw(gcnew Exception(String::Format("XbimFace. BuildFace of type {0} is not implemented",type->Name)));
			}
		}

		//Builds a face from a ArbitraryClosedProfileDef
		TopoDS_Face XbimFace::Build(IfcArbitraryClosedProfileDef ^ profile, bool% hasCurves)
		{
			if(dynamic_cast<IfcArbitraryProfileDefWithVoids^>(profile))
				return Build((IfcArbitraryProfileDefWithVoids^)profile, hasCurves);
			else
			{
				BRepBuilderAPI_MakeFace faceBlder(XbimFaceBound::Build(profile, hasCurves));
				BRepBuilderAPI_FaceError err = faceBlder.Error();
				return faceBlder.Face();
			}

		}

		//Builds a face from a ArbitraryProfileDefWithVoids
		TopoDS_Face XbimFace::Build(IfcArbitraryProfileDefWithVoids ^ profile, bool% hasCurves)
		{
			BRepBuilderAPI_MakeFace faceBlder(XbimFaceBound::Build(profile->OuterCurve, hasCurves));
			for each( IfcCurve^ curve in profile->InnerCurves)
			{
				faceBlder.Add(XbimFaceBound::Build(curve, hasCurves));
			}
			return faceBlder.Face();
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
				throw(gcnew Exception("XbimFace. Support for SurfaceOfRevolution is not implemented"));
			else if(dynamic_cast<IfcSurfaceOfLinearExtrusion^>(surface))
				throw(gcnew Exception("XbimFace. Support for SurfaceOfLinearExtrusion is not implemented"));
			else if(dynamic_cast<IfcCurveBoundedPlane^>(surface))
				throw(gcnew Exception("XbimFace. Support for CurveBoundedPlane is not implemented"));
			else if(dynamic_cast<IfcRectangularTrimmedSurface^>(surface))
				throw(gcnew Exception("XbimFace. Support for RectangularTrimmedSurface is not implemented"));
			else if(dynamic_cast<IfcBoundedSurface^>(surface))
				throw(gcnew Exception("XbimFace. Support for BoundedSurface is not implemented"));
			else
			{
				Type ^ type = surface->GetType();
				throw(gcnew Exception(String::Format("XbimFace. BuildFace of type {0} is not implemented",type->Name)));
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