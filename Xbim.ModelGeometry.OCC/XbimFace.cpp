#include "StdAfx.h"
#include "XbimFace.h"

#include "XbimGeomPrim.h"
#include <gp_Ax3.hxx>
#include <gp_Circ.hxx>
#include <GC_MakeCircle.hxx>
#include <BRepBuilderAPI_MakeFace.hxx>
#include <BRep_Builder.hxx>
#include <BRepLib_FindSurface.hxx>
#include <BRepBuilderAPI_MakeEdge.hxx>
#include <BRepBuilderAPI_MakeWire.hxx>
#include <BRepBuilderAPI_FindPlane.hxx>
#include <ShapeFix_Wireframe.hxx>
#include <BRepGProp_Face.hxx>
#include <ShapeFix_ShapeTolerance.hxx>
#include <Geom_Plane.hxx>
#include <Handle_Geom_Plane.hxx>
#include <GeomAPI_ProjectPointOnSurf.hxx>
namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
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

			gp_Vec XbimFace::GetNormalAt( gp_Pnt pnt)
			{
				Handle(Geom_Surface) surf = BRep_Tool::Surface(*nativeHandle); //the surface
				GeomAPI_ProjectPointOnSurf projpnta(pnt, surf);
				double au, av; //the u- and v-coordinates of the projected point
				projpnta.LowerDistanceParameters(au, av); //get the nearest projection
				BRepGProp_Face prop(*nativeHandle);
				gp_Pnt centre;
				gp_Vec normalDir;
				prop.Normal(au,av,centre,normalDir);	
				return normalDir;
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
				else if (dynamic_cast<IfcCircleHollowProfileDef^>(profile))
					face = XbimFace::Build((IfcCircleHollowProfileDef^)profile,hasCurves);	
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
					double tolerance = profile->ModelOf->ModelFactors->Precision;
					double toleranceMax = profile->ModelOf->ModelFactors->PrecisionMax;
					ShapeFix_ShapeTolerance FTol;
					TopoDS_Wire wire = XbimFaceBound::Build(profile, hasCurves);
					if(wire.IsNull()) 
					{
						Logger->WarnFormat("IfcArbitraryClosedProfileDef #{0} has an invalid outer bound. Discarded",profile->EntityLabel);
						return TopoDS_Face();
					}
					double currentFaceTolerance = tolerance;
					TryBuildFace:
					BRepBuilderAPI_MakeFace faceMaker(wire, true);
					BRepBuilderAPI_FaceError err = faceMaker.Error();
					if ( err == BRepBuilderAPI_NotPlanar ) 
					{
						currentFaceTolerance*=10;
						if(currentFaceTolerance<=toleranceMax)
						{
							FTol.SetTolerance(wire,currentFaceTolerance, TopAbs_WIRE);
							goto TryBuildFace;
						}
						String^ errMsg = XbimFace::GetBuildFaceErrorMessage(err);
						Logger->WarnFormat("Invalid bound, {0}. Found in IfcArbitraryClosedProfileDef = #{1}, face discarded",errMsg, profile->EntityLabel);
						return TopoDS_Face();
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

			// Raises warnings if there are errors  and returns true if no errors
			bool XbimFace::HasErrors(BRepBuilderAPI_FaceError er, int entityLabel, bool warn)
			{
				if(!warn) return  er != BRepBuilderAPI_FaceDone;
				switch (er)
				{
				case BRepBuilderAPI_FaceDone:
					return false;
				case BRepBuilderAPI_NoFace:
					Logger->WarnFormat("Could not build a face for Entity = #{0}", Math::Abs(entityLabel));
					break;
				case BRepBuilderAPI_NotPlanar:
					Logger->WarnFormat("Could not build a planar face for Entity = #{0}", Math::Abs(entityLabel));
					break;
				case BRepBuilderAPI_CurveProjectionFailed:
					Logger->WarnFormat("Could not project face boundary for Entity = #{0}", Math::Abs(entityLabel));
					break;
				case BRepBuilderAPI_ParametersOutOfRange:
					Logger->WarnFormat("Face parameters out of range for Entity = #{0}", Math::Abs(entityLabel));
					break;
				default:
					Logger->WarnFormat("Unknown error building a face for Entity = #{0}", Math::Abs(entityLabel));
					break;
				}
				return true;
			}


			//Builds a face from a ArbitraryProfileDefWithVoids
			TopoDS_Face XbimFace::Build(IfcArbitraryProfileDefWithVoids ^ profile, bool% hasCurves)
			{
				
				double tolerance = profile->ModelOf->ModelFactors->Precision;
				double toleranceMax = profile->ModelOf->ModelFactors->PrecisionMax;
				ShapeFix_ShapeTolerance FTol;
				TopoDS_Face face;
				TopoDS_Wire wire = XbimFaceBound::Build(profile->OuterCurve, hasCurves);
				double currentFaceTolerance = tolerance;
TryBuildFace:
				BRepBuilderAPI_MakeFace faceMaker(wire, false);
				BRepBuilderAPI_FaceError err = faceMaker.Error();				
				if ( err == BRepBuilderAPI_NotPlanar )
				{				
					currentFaceTolerance*=10;
					if(currentFaceTolerance<=toleranceMax)
					{
						FTol.SetTolerance(wire,currentFaceTolerance, TopAbs_WIRE);
						goto TryBuildFace;
					}
					String^ errMsg = XbimFace::GetBuildFaceErrorMessage(err);
					Logger->ErrorFormat("Invalid bound, {0}. Found in IfcArbitraryClosedProfileDefWithVoids = #{1}, face discarded",errMsg, profile->EntityLabel);
					return TopoDS_Face();
				}
				
				face= faceMaker.Face();

				gp_Vec tn = XbimFace::TopoNormal(face);
				
				
				for each( IfcCurve^ curve in profile->InnerCurves)
				{
					TopoDS_Wire innerWire = XbimFaceBound::Build(curve, hasCurves);
					if(!innerWire.IsNull() && innerWire.Closed()==Standard_True) //if the loop is not closed it is not a bound
					{
						gp_Vec n = XbimFaceBound::NewellsNormal(innerWire);
						if ( n.Dot(tn) > 0 ) //inner wire should be reverse of outer wire
							innerWire.Reverse();
						double currentloopTolerance=tolerance;
TryBuildLoop:
						faceMaker.Add(innerWire);
						BRepBuilderAPI_FaceError loopErr=faceMaker.Error();
						if(loopErr!=BRepBuilderAPI_FaceDone)
						{
							currentloopTolerance*=10; //try courser tolerance
							if(currentloopTolerance<=toleranceMax)
							{
								FTol.SetTolerance(innerWire, currentloopTolerance , TopAbs_WIRE);
								goto TryBuildLoop;
							}
							
							String^ errMsg = XbimFace::GetBuildFaceErrorMessage(loopErr);
							Logger->WarnFormat("Invalid void, {0}. IfcCurve #(1) could not be added to IfcArbitraryClosedProfileDefWithVoids = #{2}. Inner Bound ignored",errMsg, curve->EntityLabel, profile->EntityLabel);
						}
						face = faceMaker.Face();
					}
					else
					{
						Logger->InfoFormat("Invalid void in IfcArbitraryClosedProfileDefWithVoids #{0}. It is not a hole. Void discarded",curve->EntityLabel);
					}
				}
				return face;
			}

			//Builds a face from a CircleProfileDef
			TopoDS_Face XbimFace::Build(IfcCircleProfileDef ^ profile, bool% hasCurves)
			{
				BRepBuilderAPI_MakeFace faceBlder(XbimFaceBound::Build(profile, hasCurves));
				return faceBlder.Face();
			}

			//Builds a face from a CircleProfileDef
			TopoDS_Face XbimFace::Build(IfcCircleHollowProfileDef ^ circProfile, bool% hasCurves)
			{
				hasCurves=true;
				IfcAxis2Placement2D^ ax2 = (IfcAxis2Placement2D^)circProfile->Position;
				gp_Ax2 gpax2(gp_Pnt(ax2->Location->X, ax2->Location->Y,0), gp_Dir(0,0,1),gp_Dir(ax2->P[0].X, ax2->P[0].Y,0.));			
				
				//make the outer wire
				gp_Circ outer(gpax2,circProfile->Radius);
				Handle(Geom_Circle) hOuter = GC_MakeCircle(outer);
				TopoDS_Edge outerEdge = BRepBuilderAPI_MakeEdge(hOuter);
				BRepBuilderAPI_MakeWire outerWire;
				outerWire.Add(outerEdge);
				double innerRadius = circProfile->Radius - circProfile->WallThickness;
				BRepBuilderAPI_MakeFace faceBlder(outerWire);
				//now add inner wire
				if(innerRadius>0)
				{
					gp_Circ inner(gpax2,circProfile->Radius - circProfile->WallThickness);
					Handle(Geom_Circle) hInner = GC_MakeCircle(inner);
					TopoDS_Edge innerEdge = BRepBuilderAPI_MakeEdge(hInner);
					BRepBuilderAPI_MakeWire innerWire;
					innerWire.Add(innerEdge);
					faceBlder.Add(innerWire);
				}
				//make the face
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
				BRepLib_FindSurface FS(wire, pline->ModelOf->ModelFactors->PrecisionMax, Standard_False); //need to lower tolerance as many faces in facetations are not coplanar
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
				double tolerance = loop->ModelOf->ModelFactors->PrecisionMax;
				TopoDS_Wire wire = XbimFaceBound::Build(loop, hasCurves);
				BRepBuilderAPI_FindPlane  FS(wire,tolerance); //need to lower tolerance as many faces in facetations are not coplanar
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

			String^ XbimFace::GetBuildFaceErrorMessage(BRepBuilderAPI_FaceError err)
			{
				switch (err)
				{
				case BRepBuilderAPI_NoFace:
					return "No Face";
				case BRepBuilderAPI_NotPlanar:
					return "Not Planar";
				case BRepBuilderAPI_CurveProjectionFailed:
					return "Curve Projection Failed";
				case BRepBuilderAPI_ParametersOutOfRange:
					return "Parameters Out Of Range";
				case BRepBuilderAPI_FaceDone:
					return "";
				default:
					return "Unknown Error";
				}
			}
		}
	}
}