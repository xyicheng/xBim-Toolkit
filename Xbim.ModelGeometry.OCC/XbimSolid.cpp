#include "StdAfx.h"
#include "XbimSolid.h"
#include "XbimShell.h"
#include "XbimGeometryModelCollection.h"
#include "XbimPolyhedron.h"
#include "XbimGeomPrim.h"
#include <TopoDS_Compound.hxx>
#include <TopoDS_Shell.hxx>
#include <TopExp_Explorer.hxx>
#include <BRep_Builder.hxx> 
#include <BRepBuilderAPI_MakeSolid.hxx>
#include <BRepPrimAPI_MakeHalfSpace.hxx>
#include <BRepBuilderAPI_MakeFace.hxx>
#include <BRepPrimAPI_MakePrism.hxx>
#include <BRepBuilderAPI.hxx>
#include <BRepBuilderAPI_MakeWire.hxx>
#include <BRepPrimAPI_MakeRevol.hxx>
#include <BRepOffsetAPI_MakePipeShell.hxx>
#include <BRepAlgoAPI_Common.hxx>
#include <BRepAlgoAPI_Cut.hxx>
#include <BRepAlgoAPI_Fuse.hxx>
#include <Standard_PrimitiveTypes.hxx>
#include <BRepBuilderAPI_Transform.hxx>
#include <BRepBuilderAPI_GTransform.hxx>
#include <BRepBuilderAPI_FindPlane.hxx>
#include <BRepAlgo_Section.hxx>
#include <Geom_Plane.hxx>
#include <ShapeFix_Solid.hxx> 
#include <ShapeFix_Shell.hxx> 
#include <ShapeFix_Shape.hxx> 
#include <ShapeAnalysis_FreeBounds.hxx> 
#include <TopTools_HSequenceOfShape.hxx> 
#include <BRepBuilderAPI_Sewing.hxx> 
#include <ShapeUpgrade_ShellSewing.hxx> 
#include <BRepOffsetAPI_Sewing.hxx> 
#include <BRepLib.hxx>
#include <BRepAlgo_Cut.hxx>
#include <BRepCheck_Analyzer.hxx>
#include <ShapeFix_ShapeTolerance.hxx>
#include <BOPTools_DSFiller.hxx>
#include <Geom_Curve.hxx>
#include <ShapeAnalysis_Shell.hxx>

#include <gp_Circ.hxx>
#include <gp_Elips.hxx>
#include <GC_MakeCircle.hxx>
#include <GC_MakeEllipse.hxx>
#include <GC_MakeLine.hxx>
#include <BRepBuilderAPI_MakeFace.hxx>
#include <BRepBuilderAPI_MakeEdge.hxx>

#include <TColgp_Array1OfPnt.hxx>
//#include <BRepPrimAPI_MakeSphere.hxx>
//#include <BRepPrimAPI_MakeCylinder.hxx>
using namespace Xbim::XbimExtensions;
using namespace Xbim::Ifc2x3::Extensions;
using namespace System::Diagnostics;
using namespace Xbim::Common::Exceptions;
using namespace Xbim::Common;
namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
			//constructors

			XbimSolid::XbimSolid(const TopoDS_Shape&  shape , bool hasCurves,int representationLabel, int surfaceStyleLabel)
			{
				Init(shape,hasCurves,representationLabel,surfaceStyleLabel);
			};



			XbimSolid::XbimSolid(XbimSolid^ solid, IfcAxis2Placement^ origin, IfcCartesianTransformationOperator^ transform, bool hasCurves)
			{
				_representationLabel = solid->RepresentationLabel;
				_surfaceStyleLabel = solid->SurfaceStyleLabel;
				TopoDS_Solid temp = *(((TopoDS_Solid*)solid->Handle));
				nativeHandle = new TopoDS_Solid();
				_hasCurvedEdges = solid->HasCurvedEdges;
				if(origin!=nullptr)
					temp.Move(XbimGeomPrim::ToLocation(origin));
				if(transform!=nullptr)
				{
					if(dynamic_cast<IfcCartesianTransformationOperator3DnonUniform^>( transform))
					{
						BRepBuilderAPI_GTransform gTran(temp,XbimGeomPrim::ToTransform((IfcCartesianTransformationOperator3DnonUniform^)transform));
						*nativeHandle = gTran.Shape();

					}
					else
					{
						BRepBuilderAPI_Transform gTran(temp,XbimGeomPrim::ToTransform(transform));
						*nativeHandle = gTran.Shape();
					}
				}
				else
					*nativeHandle = temp;
			};

			XbimSolid::XbimSolid(XbimGeometryModel^ solid, XbimMatrix3D transform)
			{
				_representationLabel = solid->RepresentationLabel;
				_surfaceStyleLabel = solid->SurfaceStyleLabel;
				TopoDS_Solid temp = *(((TopoDS_Solid*)solid->Handle));
				nativeHandle = new TopoDS_Solid();
				_hasCurvedEdges = solid->HasCurvedEdges;
				if(!transform.IsIdentity)	
				{
					//assume worst case a non-uniform transformation and use BRepBuilderAPI_GTransform
					BRepBuilderAPI_GTransform gTran(temp,XbimGeomPrim::ToTransform(transform));
					*nativeHandle = gTran.Shape();	
				}
			};

			XbimSolid::XbimSolid(XbimGeometryModel^ solid, bool hasCurves)
			{
				_representationLabel = solid->RepresentationLabel;
				_surfaceStyleLabel = solid->SurfaceStyleLabel;	
				nativeHandle = new TopoDS_Solid();
				*nativeHandle = *(((TopoDS_Solid*)solid->Handle));
				_hasCurvedEdges = hasCurves;	
			};

			XbimSolid::XbimSolid(IfcExtrudedAreaSolid^ repItem)
			{
				Init(repItem);
				nativeHandle = new TopoDS_Solid();
				*nativeHandle = Build(repItem, _hasCurvedEdges);
			};


			XbimSolid::XbimSolid(IfcVertexPoint^ pt)

			{
				Init(pt);
				/*nativeHandle = new TopoDS_Solid();
				_hasCurvedEdges = true;
				double diameter = pt->ModelOf->ModelFactors->VertxPointDiameter;
				BRepPrimAPI_MakeSphere sphere(diameter/2);
				*nativeHandle = sphere.Solid();*/
			};

			XbimSolid::XbimSolid(IfcEdge^ edge)
			{
				Init(edge);
				/*nativeHandle = new TopoDS_Solid();
				_hasCurvedEdges = true;
				double diameter = edge->ModelOf->ModelFactors->VertxPointDiameter/2;
				BRepPrimAPI_MakeCylinder edge(diameter/2,edge->End, edge->EdgeStart);
				*nativeHandle = sphere.Solid();*/
			};



			XbimSolid::XbimSolid(IfcRevolvedAreaSolid^ repItem)
			{	
				Init(repItem);
				nativeHandle = new TopoDS_Solid();
				*nativeHandle = Build(repItem,_hasCurvedEdges);
			};

			

			XbimSolid::XbimSolid(IfcHalfSpaceSolid^ repItem)
			{	
				Init(repItem);
				nativeHandle = new TopoDS_Solid();
				*nativeHandle = Build(repItem, _hasCurvedEdges);
			};

			

			XbimSolid::XbimSolid(IfcSolidModel^ repItem)
			{	
				Init(repItem);
				nativeHandle = new TopoDS_Solid();
				if(dynamic_cast<IfcManifoldSolidBrep^>(repItem))
					throw gcnew Exception("Not implemented, use XbimFacetedShell");
				else if(dynamic_cast<IfcSweptAreaSolid^>(repItem))
					*nativeHandle = Build((IfcSweptAreaSolid^)repItem,_hasCurvedEdges);
				/*else if(dynamic_cast<IfcCsgSolid^>(repItem))
				*nativeHandle = Build((IfcCsgSolid^)repItem,_hasCurvedEdges);*/
				else if(dynamic_cast<IfcSweptDiskSolid^>(repItem))
					*nativeHandle = Build((IfcSweptDiskSolid^)repItem,_hasCurvedEdges);
				else
				{
					Type^ type = repItem->GetType();
					throw gcnew XbimGeometryException("Error buiding solid from type " + type->Name);
				}

			};
			XbimSolid::XbimSolid(IfcCsgPrimitive3D^ repItem)
			{	
				throw gcnew NotImplementedException("Solid of type IfcCsgPrimitive3D is not implemented yet");
				Init(repItem);
			};
			/*Interfaces*/



			System::Collections::Generic::IEnumerable<XbimFace^>^ XbimSolid::Faces::get()
			{

				return this;
			}



			
			XbimGeometryModel^ XbimSolid::CopyTo(IfcAxis2Placement^ placement)
			{
				TopoDS_Shape movedShape = *nativeHandle;
				movedShape.Move(XbimGeomPrim::ToLocation(placement));
				XbimSolid^ solid = gcnew XbimSolid(movedShape, _hasCurvedEdges,_representationLabel,_surfaceStyleLabel);
				return solid;
			}


			//Static Builders

			TopoDS_Solid XbimSolid::Build(IfcSweptDiskSolid^ swdSolid, bool% hasCurves)
			{

				//Build the directrix
				TopoDS_Wire sweep;
				bool isConic = (dynamic_cast<IfcConic^>(swdSolid->Directrix)!=nullptr);
				gp_Ax2 ax2(gp_Pnt(0.,0.,0.),gp_Dir(0.,0.,1.));
				XbimModelFactors^ mf = ((IPersistIfcEntity^)swdSolid)->ModelOf->ModelFactors;

				double parameterFactor =  mf->LengthToMetresConversionFactor;
				Handle(Geom_Curve) curve;
				if(isConic)
				{
					//it could be based on a circle, ellipse or line
					if(dynamic_cast<IfcCircle^>(swdSolid->Directrix))
					{
						hasCurves=true;
						IfcCircle^ c = (IfcCircle^) swdSolid->Directrix;
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
					else if (dynamic_cast<IfcEllipse^>(swdSolid->Directrix))
					{
						hasCurves=true;
						IfcEllipse^ c = (IfcEllipse^) swdSolid->Directrix;

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
							throw(gcnew NotImplementedException(String::Format("XbimSolid. Ellipse with Placement of type {0} is not implemented",type->Name)));	
						}
						else
						{
							Type ^ type = c->Position->GetType();
							throw(gcnew NotImplementedException(String::Format("XbimSolid. Ellipse with Placement of type {0} is not implemented",type->Name)));	
						}
					}
					BRepBuilderAPI_MakeWire w;
					double flt1 = (double)(swdSolid->StartParam.Value) * parameterFactor;
					double flt2 = (double)(swdSolid->EndParam.Value) * parameterFactor;
					if ( isConic && Math::Abs(Math::IEEERemainder(flt2-flt1,(double)(Math::PI*2.0))-0.0f) < BRepBuilderAPI::Precision()) 
					{
						w.Add(BRepBuilderAPI_MakeEdge(curve));
					} 
					else 
					{
						BRepBuilderAPI_MakeEdge e (curve, flt1, flt2);
						w.Add(e.Edge());
					}
					sweep = w.Wire();
				}
				else if (dynamic_cast<IfcLine^>(swdSolid->Directrix))
				{
					IfcLine^ line = (IfcLine^)(swdSolid->Directrix);
					IfcCartesianPoint^ cp = line->Pnt;

					IfcVector^ dir = line->Dir;
					gp_Pnt pnt(cp->X,cp->Y,cp->Z);
					XbimVector3D v3d = dir->XbimVector3D();
					gp_Vec vec(v3d.X,v3d.Y,v3d.Z);
					curve = GC_MakeLine(pnt,vec);
					sweep = BRepBuilderAPI_MakeWire(BRepBuilderAPI_MakeEdge(GC_MakeLine(pnt,vec),0,dir->Magnitude));
				}
				else if(dynamic_cast<IfcCompositeCurve^>(swdSolid->Directrix))
				{
					sweep = XbimFaceBound::Build((IfcCompositeCurve^)(swdSolid->Directrix),hasCurves);
				}
				else
				{
					Type ^ type = swdSolid->Directrix->GetType();
					throw(gcnew NotImplementedException(String::Format("XbimSolid. CompositeCurveSegments with BasisCurve of type {0} is not implemented",type->Name)));	
				}

				//build the surface to sweep
				//make the outer wire
				gp_Circ outer(ax2,swdSolid->Radius);
				Handle(Geom_Circle) hOuter = GC_MakeCircle(outer);
				TopoDS_Edge outerEdge = BRepBuilderAPI_MakeEdge(hOuter);
				BRepBuilderAPI_MakeWire outerWire;
				outerWire.Add(outerEdge);

				
				BRepOffsetAPI_MakePipeShell pipeMaker(sweep);
				pipeMaker.Add(outerWire.Wire(),Standard_True, Standard_True);
				pipeMaker.Build();
				if(pipeMaker.IsDone() && pipeMaker.MakeSolid())
				{ 
					TopoDS_Shape result = pipeMaker.Shape();

					//now add inner wire if it is defined
					/*if(swdSolid->InnerRadius.HasValue)
					{
					gp_Circ inner(ax2,swdSolid->InnerRadius.Value);
					Handle(Geom_Circle) hInner = GC_MakeCircle(inner);
					TopoDS_Edge innerEdge = BRepBuilderAPI_MakeEdge(hInner);
					BRepBuilderAPI_MakeWire innerWire;
					innerWire.Add(innerEdge);
					faceBlder.Add(innerWire);
					}*/
					return TopoDS::Solid(result);
				}
				else
				{
					Logger->WarnFormat( "Entity #" + swdSolid->EntityLabel.ToString() + ", IfcSweptDiskSolid could not be constructed ");
					return TopoDS_Solid();
				}
			}

			TopoDS_Solid XbimSolid::Build(IfcSweptAreaSolid^ sweptAreaSolid, bool% hasCurves)
			{
				if(dynamic_cast<IfcExtrudedAreaSolid^>(sweptAreaSolid))
					return Build((IfcExtrudedAreaSolid^)sweptAreaSolid, hasCurves);
				else if(dynamic_cast<IfcRevolvedAreaSolid^>(sweptAreaSolid))
					return Build((IfcRevolvedAreaSolid^)sweptAreaSolid, hasCurves);
				else if(dynamic_cast<IfcSurfaceCurveSweptAreaSolid^>(sweptAreaSolid))
					return Build((IfcSurfaceCurveSweptAreaSolid^)sweptAreaSolid, hasCurves);
				else
				{
					Type ^ type = sweptAreaSolid->GetType();
					throw(gcnew NotImplementedException(String::Format("XbimSolid. SweptAreaSolid of type {0} is not implemented",type->Name)));
				}
			}


			TopoDS_Solid XbimSolid::Build(IfcExtrudedAreaSolid^ repItem, bool% hasCurves)
			{
				TopoDS_Face face = XbimFace::Build(repItem->SweptArea,hasCurves);

				if(!face.IsNull() &&repItem->Depth<=0)
				{
					XbimModelFactors^ mf = ((IPersistIfcEntity^)repItem)->ModelOf->ModelFactors;
					/*Logger->WarnFormat(String::Format("Invalid Solid Extrusion, Extrusion Depth must be >0, found in Entity #{0}=IfcExtrudedAreaSolid\nIt has been ignored",
					repItem->EntityLabel));*/
					//use a very thin 1mm extrusion
					TopoDS_Solid solid = Build(face,repItem->ExtrudedDirection , mf->OneMilliMetre/10, hasCurves);
					solid.Move(XbimGeomPrim::ToLocation(repItem->Position));
					return  solid;
				}
				if(!face.IsNull())
				{

					TopoDS_Solid solid = Build(face,repItem->ExtrudedDirection , repItem->Depth, hasCurves);
					solid.Move(XbimGeomPrim::ToLocation(repItem->Position));
					return  solid;
				}
				else
				{
					//a null face indicates an invalid solid, 
					return TopoDS_Solid();
				}
			}


			TopoDS_Solid XbimSolid::Build(IfcRevolvedAreaSolid^ repItem, bool% hasCurves)
			{
				// gettin the face right is necessary before the revolution can be performed.
				//
				TopoDS_Face face;
				if(dynamic_cast<IfcArbitraryClosedProfileDef^>(repItem->SweptArea)) 
					face =  XbimFace::Build((IfcArbitraryClosedProfileDef^)repItem->SweptArea, hasCurves);
				else if(dynamic_cast<IfcRectangleProfileDef^>(repItem->SweptArea))
					face = XbimFace::Build((IfcRectangleProfileDef^)repItem->SweptArea, hasCurves);	
				else if(dynamic_cast<IfcCircleProfileDef^>(repItem->SweptArea))
					face = XbimFace::Build((IfcCircleProfileDef^)repItem->SweptArea, hasCurves);	
				else
				{
					Type ^ type = repItem->SweptArea->GetType();
					throw(gcnew NotImplementedException(String::Format("XbimSolid. Could not BuildShape of type {0}. It is not implemented",type->Name)));
				}

				// Here we need to prepare the revolution.
				//

				TopoDS_Solid solid = Build(face,repItem->Axis, repItem->Angle, hasCurves);
				solid.Move(XbimGeomPrim::ToLocation(repItem->Position));
				return  solid;
			}

			TopoDS_Solid XbimSolid::Build(IfcSurfaceCurveSweptAreaSolid^ repItem, bool% hasCurves)
			{
				TopoDS_Wire profile;
				if(dynamic_cast<IfcArbitraryProfileDefWithVoids^>(repItem->SweptArea)) 
					profile =  XbimFaceBound::Build((IfcArbitraryProfileDefWithVoids^)repItem->SweptArea, hasCurves);
				else if(dynamic_cast<IfcArbitraryClosedProfileDef^>(repItem->SweptArea)) 
					profile =  XbimFaceBound::Build((IfcArbitraryClosedProfileDef^)repItem->SweptArea, hasCurves);
				else if(dynamic_cast<IfcRectangleProfileDef^>(repItem->SweptArea))
					profile = XbimFaceBound::Build((IfcRectangleProfileDef^)repItem->SweptArea, hasCurves);	
				else if(dynamic_cast<IfcCircleProfileDef^>(repItem->SweptArea))
					profile = XbimFaceBound::Build((IfcCircleProfileDef^)repItem->SweptArea, hasCurves);	
				else
				{
					Type ^ type = repItem->SweptArea->GetType();
					Logger->WarnFormat(String::Format("XbimSolid. Could not BuildShape of type {0}. It is not implemented",type->Name));
					return TopoDS_Solid();
				}
				//profile.Move(XbimGeomPrim::ToLocation(repItem->Position));
				TopoDS_Wire sweep = XbimFaceBound::Build(repItem->Directrix, hasCurves);
				BRepOffsetAPI_MakePipeShell pipeMaker(sweep);

				if(dynamic_cast<IfcPlane^>(repItem->ReferenceSurface))
				{
					IfcPlane^ ifcPlane = (IfcPlane^)repItem->ReferenceSurface;
					gp_Ax3 ax3 = XbimGeomPrim::ToAx3(ifcPlane->Position);
					pipeMaker.SetMode(ax3.Direction());
					//find the start position of the sweep
					BRepTools_WireExplorer wExp(sweep);
					Standard_Real start = 0;
					Standard_Real end = 1;
					Handle_Geom_Curve curve = BRep_Tool::Curve(wExp.Current(),start, end);
					gp_Pnt p1;
					gp_Vec tangent;
					curve->D1(0, p1, tangent);
					const TopoDS_Vertex firstPoint = wExp.CurrentVertex();
					gp_Ax3 toAx3(BRep_Tool::Pnt(firstPoint),tangent, ax3.Direction());	//rotate so normal of profile is tangental and X axis 
					gp_Trsf trsf;
					trsf.SetTransformation(toAx3,gp_Ax3());
					TopLoc_Location topLoc(trsf);			
					profile.Location(topLoc);
					pipeMaker.Add(profile,Standard_False, Standard_False);
				}
				else
				{
					Logger->WarnFormat( "Entity #" + repItem->EntityLabel.ToString() + ", IfcSurfaceCurveSweptAreaSolid has a Non-Planar surface");
					pipeMaker.SetMode(Standard_False); //use auto calculation of tangent and binormal
					pipeMaker.Add(profile,Standard_False, Standard_True);
				}

				pipeMaker.SetTransitionMode(BRepBuilderAPI_RightCorner);
				pipeMaker.Build();
				if(pipeMaker.IsDone() && pipeMaker.MakeSolid())
				{ 
					TopoDS_Shape result = pipeMaker.Shape();
					result.Move(XbimGeomPrim::ToLocation(repItem->Position));
					ShapeFix_ShapeTolerance FTol;	
					FTol.SetTolerance(result,repItem->ModelOf->ModelFactors->Precision ,TopAbs_WIRE);
					return TopoDS::Solid(result);
				}
				else
				{
					Logger->WarnFormat( "Entity #" + repItem->EntityLabel.ToString() + ", IfcSurfaceCurveSweptAreaSolid could not be constructed ");
					return TopoDS_Solid();
				}
			}

			TopoDS_Solid XbimSolid::MakeHalfSpace(IfcHalfSpaceSolid^ hs, bool% hasCurves, bool shift)
			{
				IfcSurface^ surface = (IfcSurface^)hs->BaseSurface;
				if(!dynamic_cast<IfcPlane^>(surface)) throw gcnew Exception("Non-Planar half spaces are not supported");
				IfcPlane^ ifcPlane = (IfcPlane^)surface;
				gp_Ax3 ax3 = XbimGeomPrim::ToAx3(ifcPlane->Position);
				gp_Pln pln(ax3);
				gp_Vec direction = hs->AgreementFlag ? -pln.Axis().Direction() : pln.Axis().Direction();
				const gp_Pnt pnt = pln.Location().Translated(direction );
				if(shift)
					pln.SetLocation(pln.Location().Translated(direction * 10 * -BRepBuilderAPI::Precision())); //shift a little to avoid covergent faces
				return BRepPrimAPI_MakeHalfSpace(BRepBuilderAPI_MakeFace(pln),pnt).Solid();

			}


			TopoDS_Solid XbimSolid::Build(IfcHalfSpaceSolid^ hs, bool% hasCurves)
			{
				if(dynamic_cast<IfcPolygonalBoundedHalfSpace^>(hs))
					return Build((IfcPolygonalBoundedHalfSpace^)hs, hasCurves);
				else if (dynamic_cast<IfcBoxedHalfSpace^>(hs))
					return Build((IfcBoxedHalfSpace^)hs, hasCurves);
				else //it is a simple Half space
				{
					return MakeHalfSpace(hs, hasCurves, false);
				}
			}

			TopoDS_Solid XbimSolid::Build(IfcPolygonalBoundedHalfSpace^ pbhs, bool% hasCurves)
			{

				//creates polygon and its plane normal direction
				gp_Ax3 ax3Polygon = XbimGeomPrim::ToAx3(pbhs->Position);
				gp_Dir normPolygon = ax3Polygon.Direction();	
				TopoDS_Wire wire =  XbimFaceBound::Build(pbhs->PolygonalBoundary, hasCurves); //get the polygon
				BRepBuilderAPI_MakeFace makeFace(wire);
				TopoDS_Face face = makeFace.Face();
				if(face.IsNull()) 
				{
					Logger->WarnFormat("The IfcPolygonalBoundedHalfSpace #{0} has an icorrectly defined PolygonalBoundary #{1}, it has been ignored",pbhs->EntityLabel,pbhs->PolygonalBoundary->EntityLabel);
					return TopoDS_Solid(); //the face is illegal
				}

				gp_Trsf toPos = XbimGeomPrim::ToTransform(pbhs->Position);
				face.Move(toPos);	

				TopoDS_Shape pris = BRepPrimAPI_MakePrism(face, gp_Vec(normPolygon)*2e6); //create infinite extrusion,  this is a work around as infinite half space don't work properly in open cascade
				//Move the prism so that it approximates to infinit in both directions
				gp_Trsf away; 
				away.SetTranslation(gp_Vec(normPolygon)*-1e6);
				pris.Move(away);

				TopoDS_Solid hs = MakeHalfSpace((IfcHalfSpaceSolid^)pbhs,hasCurves,false );//cast to build the half space

				BRepAlgoAPI_Common joiner(pris, hs);

				if(joiner.ErrorStatus() == 0) //find the solid and return it, else throw an exception
				{
					TopoDS_Shape result = joiner.Shape();
					if( BRepCheck_Analyzer(result).IsValid() == 0) //try and move half space in case it is co-planar with a face. This cause OpenCascade to delete the face and make an illegal solid
					{
						TopoDS_Solid hsMoved = MakeHalfSpace((IfcHalfSpaceSolid^)pbhs,hasCurves, true );//cast to build the half space

						BRepAlgoAPI_Common joiner2(pris, hsMoved);
						if(BRepCheck_Analyzer(joiner2.Shape()).IsValid() != 0)
							result = joiner2.Shape();
						else //these shapes have nothing in common, so just return an empty solid
							return TopoDS_Solid();
					}

					if(result.ShapeType() == TopAbs_SOLID) //if we have a solid just send it
					{
						return TopoDS::Solid(result);
					}

					for (TopExp_Explorer solidEx(result,TopAbs_SOLID) ; solidEx.More(); solidEx.Next())  
					{
						return TopoDS::Solid(solidEx.Current());
					}
				}

				throw gcnew XbimGeometryException("Failed to create polygonally bounded half space");

			}


			TopoDS_Solid XbimSolid::Build(IfcBoxedHalfSpace^ bhs, bool% hasCurves)
			{
				IfcSurface^ surface = (IfcSurface^)bhs->BaseSurface;
				if(dynamic_cast<IfcPlane^>(surface))
				{
					IfcPlane^ ifcPlane = (IfcPlane^) surface;
					gp_Ax3 ax3BaseSurface = XbimGeomPrim::ToAx3(ifcPlane->Position);
					gp_Pln plnBaseSurface(ax3BaseSurface);
					gp_Dir normBaseSurface = plnBaseSurface.Axis().Direction();   
					gp_Vec zVec(normBaseSurface);
					gp_Pnt pnt(ax3BaseSurface.Location());
					if(bhs->AgreementFlag) zVec.Reverse();
					pnt.Translate(zVec);
					TopoDS_Face faceBase = BRepBuilderAPI_MakeFace(plnBaseSurface);
					BRepPrimAPI_MakeHalfSpace halfSpaceBulder(faceBase, pnt);
					TopoDS_Solid hs =  halfSpaceBulder.Solid();
					ShapeFix_ShapeTolerance FTol;	
					FTol.SetTolerance(hs,bhs->ModelOf->ModelFactors->Precision ,TopAbs_WIRE);
					return hs;
				}
				throw gcnew XbimGeometryException("Only planar boxed half spaces are valid for building IfcBoxedHalfSpace");
			}


			TopoDS_Solid XbimSolid::Build(const TopoDS_Wire & wire, gp_Dir dir, bool% hasCurves)
			{
				BRepBuilderAPI_MakeFace faceBlder(wire);
				BRepPrimAPI_MakePrism prism(faceBlder.Face() , dir);
				TopoDS_Solid solid = TopoDS::Solid(prism.Shape());
				return solid;
			}


			TopoDS_Shell XbimSolid::Build(const TopoDS_Wire & wire, IfcDirection^ dir, double depth, bool% hasCurves)
			{
				gp_Vec vec(dir->X,dir->Y,dir->Z );
				vec*= depth;
				BRepPrimAPI_MakePrism prism(wire , vec);
				TopoDS_Shell shell = TopoDS::Shell(prism.Shape());
				return shell;
			}

			TopoDS_Solid XbimSolid::Build(const TopoDS_Face & face, IfcDirection^ dir, double depth, bool% hasCurves)
			{
				// TODO: when depth is 0 this throws an exception
				//
				gp_Vec vec(dir->X,dir->Y,dir->Z );
				vec*= depth;
				BRepPrimAPI_MakePrism prism(face , vec);
				return TopoDS::Solid(prism.Shape());
			}

			TopoDS_Solid XbimSolid::Build(const TopoDS_Face & face, IfcAxis1Placement^ revolaxis, double angle, bool% hasCurves)
			{
				hasCurves=true;
				gp_Pnt Orig(
					revolaxis->Location->X,
					revolaxis->Location->Y,
					revolaxis->Location->Z
					);

				gp_Dir Vx(
					revolaxis->Axis->X,
					revolaxis->Axis->Y,
					revolaxis->Axis->Z
					);

				gp_Ax1 ax1(Orig,Vx);

				BRepPrimAPI_MakeRevol revol(face , ax1, angle);

				TopoDS_Solid solid =TopoDS::Solid(revol.Shape());
				
				return solid;
			}

			void XbimSolid::Print()
			{
				int c = 1;
				System::Diagnostics::Debug::WriteLine("Solid");
				for each(XbimFace^ face in this->Faces)
				{
					System::Diagnostics::Debug::WriteLine("Face " + c);
					face->Print();
				}
				System::Diagnostics::Debug::WriteLine("End Solid");
			}
		}
	}
}




