#include "StdAfx.h"
#include "XbimGeometryModel.h"
#include "XbimPolyhedron.h"
#include "XbimTriangularMeshStreamer.h"
#include "XbimLocation.h"
#include "XbimSolid.h"
#include "XbimGeomPrim.h"
#include "XbimFeaturedShape.h"
#include "XbimShell.h"
#include "XbimGeometryModelCollection.h"
#include "XbimFacetedShell.h"
#include "XbimMap.h"

#include <BRepTools.hxx>
#include <TopoDS_Shell.hxx>
#include <TopoDS_Solid.hxx>
#include <BRepGProp_Face.hxx>
#include <BRepAlgoAPI_Cut.hxx>
#include <BRepAlgoAPI_Fuse.hxx>
#include <BRepAlgoAPI_Common.hxx>
#include <TopoDS.hxx>
#include <ShapeFix_Shape.hxx> 
#include <ShapeFix_Wireframe.hxx> 
#include <BRepBuilderAPI_Sewing.hxx> 
#include <ShapeUpgrade_ShellSewing.hxx> 
#include <BRepMesh_IncrementalMesh.hxx>
#include <Poly_Array1OfTriangle.hxx>
#include <TColgp_Array1OfPnt.hxx>
#include <TShort_Array1OfShortReal.hxx>
#include <Poly_Triangulation.hxx>
#include <BRepBndLib.hxx>
#include <BRepBuilderAPI_Transform.hxx>
#include <GeomLProp_SLProps.hxx>
#include <BRepLib.hxx>
#include <Poly.hxx>
#include <Geom_Plane.hxx>
#include <BRepBuilderAPI.hxx>
#include <GeomLib_IsPlanarSurface.hxx>
#include <ShapeFix_ShapeTolerance.hxx>
#include <BRepCheck_Analyzer.hxx>
#include <carve/triangulator.hpp>
#include <GeomAPI_ProjectPointOnSurf.hxx>
#include <carve/mesh_simplify.hpp>
using namespace Xbim::IO;
using namespace Xbim::Ifc2x3::ProductExtension;
using namespace Xbim::Ifc2x3::SharedComponentElements;
using namespace System::Linq;
using namespace Xbim::Ifc2x3::PresentationAppearanceResource;
using namespace Xbim::Common::Exceptions;
using namespace Xbim::ModelGeometry::Scene;
using namespace  System;
using namespace  System::Threading;
using namespace  System::Text;


class Message_ProgressIndicator {};

namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
#pragma unmanaged
			void __stdcall XMS_AddVertexIndex(void *pVertexData, void *pPolygonData)
			{
				((XbimTriangularMeshStreamer*)pPolygonData)->WriteTriangleIndex((size_t)pVertexData);
			}
			;
			void __stdcall XMS_BeginTessellate(GLenum type, void *pPolygonData)
			{
				((XbimTriangularMeshStreamer*)pPolygonData)->BeginPolygon(type);
			};
			void __stdcall XMS_EndTessellate(void *pVertexData)
			{
				((XbimTriangularMeshStreamer*)pVertexData)->EndPolygon();
			};
			void __stdcall XMS_TessellateError(GLenum err)
			{
				// swallow the error.
			};

			
#pragma managed
			gp_Dir GetNormal(const TopoDS_Face& face)
			{
				// get bounds of face
				Standard_Real umin, umax, vmin, vmax;

				BRepTools::UVBounds(face, umin, umax, vmin, vmax);          // create surface
				Handle(Geom_Surface) surf=BRep_Tool::Surface(face);          // get surface properties
				GeomLProp_SLProps props(surf, umin, vmin, 1, 0.01);          // get surface normal
				gp_Dir norm = props.Normal();                         // check orientation
				if(face.Orientation()==TopAbs_REVERSED) 
					norm.Reverse();
				return norm;
			}

#pragma unmanaged

			long OpenCascadeShapeStreamerFeed(const TopoDS_Shape & shape, XbimTriangularMeshStreamer* tms)
			{
				// vertexData receives the calls from the following code that put the information in the binary stream.
				//
				// XbimTriangularMeshStreamer tms;

				// triangle indices are 1 based; this converts them to 0 based them and deals with multiple triangles to be added for multiple calls to faces.
				//
				int tally = -1;	

				for (TopExp_Explorer faceEx(shape,TopAbs_FACE) ; faceEx.More(); faceEx.Next()) 
				{

					const TopoDS_Face& face = TopoDS::Face(faceEx.Current());
					TopAbs_Orientation orient = face.Orientation();
					TopLoc_Location loc;
					Handle (Poly_Triangulation) facing = BRep_Tool::Triangulation(face,loc);
					if(facing.IsNull())
					{
						continue;
					}	

					// computation of normals
					// the returing array is 3 times longer than point array and it's to be read in groups of 3.
					//
					Poly::ComputeNormals(facing);

					const TShort_Array1OfShortReal& normals =  facing->Normals();

					Standard_Integer nbNodes = facing->NbNodes();
					// tms.info('p', (int)nbNodes);
					Standard_Integer nbTriangles = facing->NbTriangles();
					Standard_Integer nbNormals = normals.Length();
					if(nbNormals != nbNodes * 3) //there is a geometry error in OCC
						continue;
					const TColgp_Array1OfPnt& points = facing->Nodes();
					int nTally = 0;

					tms->BeginFace(nbNodes);

					for(Standard_Integer nd = 1 ; nd <= nbNodes ; nd++)
					{
						gp_XYZ p = points(nd).Coord();
						loc.Transformation().Transforms(p); 

						tms->WritePoint((float)p.X(), (float)p.Y(), (float)p.Z());
						nTally+=3;
					}

					const Poly_Array1OfTriangle& triangles = facing->Triangles();

					Standard_Integer n1, n2, n3;
					float nrmx, nrmy, nrmz;

					tms->BeginPolygon(GL_TRIANGLES);
					for(Standard_Integer tr = 1 ; tr <= nbTriangles ; tr++)
					{
						triangles(tr).Get(n1, n2, n3); // triangle indices are 1 based
						int iPointIndex;
						if(orient == TopAbs_REVERSED) //srl code below fixed to get normals in the correct order of triangulation
						{
							// note the negative values of the normals for reversed faces.
							// tms->info('R');

							// setnormal and point
							iPointIndex = 3 * n3 - 2; // n3 srl fix
							nrmx = -(float)normals(iPointIndex++);
							nrmy = -(float)normals(iPointIndex++);
							nrmz = -(float)normals(iPointIndex++);
							tms->SetNormal(nrmx, nrmy, nrmz);
							tms->WriteTriangleIndex(n3);


							// setnormal and point
							iPointIndex = 3 * n2 - 2;
							nrmx = -(float)normals(iPointIndex++);
							nrmy = -(float)normals(iPointIndex++);
							nrmz = -(float)normals(iPointIndex++);
							tms->SetNormal(nrmx, nrmy, nrmz);
							tms->WriteTriangleIndex(n2);


							// setnormal and point
							iPointIndex = 3 * n1 - 2; // n1 srl fix
							nrmx = -(float)normals(iPointIndex++);
							nrmy = -(float)normals(iPointIndex++);
							nrmz = -(float)normals(iPointIndex++);
							tms->SetNormal(nrmx, nrmy, nrmz);
							tms->WriteTriangleIndex(n1);

						}
						else
						{
							// tms->info('N');
							// setnormal and point
							iPointIndex = 3 * n1 - 2;
							nrmx = (float)normals(iPointIndex++);
							nrmy = (float)normals(iPointIndex++);
							nrmz = (float)normals(iPointIndex++);
							tms->SetNormal(nrmx, nrmy, nrmz);
							tms->WriteTriangleIndex(n1);


							// setnormal and point
							iPointIndex = 3 * n2 - 2;
							nrmx = (float)normals(iPointIndex++);
							nrmy = (float)normals(iPointIndex++);
							nrmz = (float)normals(iPointIndex++);
							tms->SetNormal(nrmx, nrmy, nrmz);
							tms->WriteTriangleIndex(n2);


							// setnormal and point
							iPointIndex = 3 * n3 - 2;
							nrmx = (float)normals(iPointIndex++);
							nrmy = (float)normals(iPointIndex++);
							nrmz = (float)normals(iPointIndex++);
							tms->SetNormal(nrmx, nrmy, nrmz);
							tms->WriteTriangleIndex(n3);
						}
					}
					tally+=nbNodes; // bonghi: question: point coordinates might be duplicated with this method for different faces. Size optimisation could be possible at the cost of performance speed.

					tms->EndPolygon();
					tms->EndFace();
				}
				size_t iSize = tms->StreamSize();
				return 0;
			}

			void OpenGLShapeStreamerFeed(const TopoDS_Shape & shape, XbimTriangularMeshStreamer* tms)
			{
				GLUtesselator *ActiveTss = gluNewTess();

				gluTessCallback(ActiveTss, GLU_TESS_BEGIN_DATA,  (GLUTessCallback) XMS_BeginTessellate);
				gluTessCallback(ActiveTss, GLU_TESS_END_DATA,  (GLUTessCallback) XMS_EndTessellate);
				gluTessCallback(ActiveTss, GLU_TESS_ERROR,    (GLUTessCallback) XMS_TessellateError);
				gluTessCallback(ActiveTss, GLU_TESS_VERTEX_DATA,  (GLUTessCallback) XMS_AddVertexIndex);

				GLdouble glPt3D[3];
				// TesselateStream vertexData(pStream, points, faceCount, streamSize);
				for (TopExp_Explorer faceEx(shape,TopAbs_FACE) ; faceEx.More(); faceEx.Next()) 
				{
					tms->BeginFace(-1);
					const TopoDS_Face& face = TopoDS::Face(faceEx.Current());
					gp_Dir normal = GetNormal(face);
					tms->SetNormal(
						(float)normal.X(), 
						(float)normal.Y(), 
						(float)normal.Z()
						);
					// vertexData.BeginFace(normal);
					// gluTessBeginPolygon(tess, &vertexData);
					gluTessBeginPolygon(ActiveTss, tms);

					// go over each wire
					for (TopExp_Explorer wireEx(face,TopAbs_WIRE) ; wireEx.More(); wireEx.Next()) 
					{
						gluTessBeginContour(ActiveTss);
						const TopoDS_Wire& wire = TopoDS::Wire(wireEx.Current());

						BRepTools_WireExplorer wEx(wire);

						for(;wEx.More();wEx.Next())
						{
							const TopoDS_Edge& edge = wEx.Current();
							const TopoDS_Vertex& vertex=  wEx.CurrentVertex();
							gp_Pnt p = BRep_Tool::Pnt(vertex);
							glPt3D[0] = p.X();
							glPt3D[1] = p.Y();
							glPt3D[2] = p.Z();
							size_t pIndex = tms->WritePoint((float)p.X(), (float)p.Y(), (float)p.Z());
							gluTessVertex(ActiveTss, glPt3D, (void*)pIndex); 
						}
						gluTessEndContour(ActiveTss);
					}
					gluTessEndPolygon(ActiveTss);
					tms->EndFace();
				}
				gluDeleteTess(ActiveTss);
			}

#pragma managed
			
			void XbimGeometryModel::Init(const TopoDS_Shape&  shape , bool hasCurves,int representationLabel, int surfaceStyleLabel)
			{
				if(shape.ShapeType() == TopAbs_SHELL)
					nativeHandle = new TopoDS_Shell();
				else if(shape.ShapeType() == TopAbs_SOLID)
					nativeHandle = new TopoDS_Solid();
				else if(shape.ShapeType() == TopAbs_COMPOUND)
					nativeHandle = new TopoDS_Compound();
				else
					throw gcnew XbimGeometryException("Attempt to build a solid from an unexpected shape type");
				*nativeHandle=shape;
				_hasCurvedEdges = hasCurves;
				_representationLabel=representationLabel;
				_surfaceStyleLabel=surfaceStyleLabel;
			};
			void XbimGeometryModel::Init(IfcRepresentationItem^ entity)
			{
				_bounds=XbimRect3D::Empty;
				RepresentationLabel = Math::Abs(entity->EntityLabel);
				IfcSurfaceStyle^ surfaceStyle = IfcRepresentationItemExtensions::SurfaceStyle(entity);
				if(surfaceStyle!=nullptr) SurfaceStyleLabel=Math::Abs(surfaceStyle->EntityLabel);
			}

			//boolean operations
			IXbimGeometryModel^ XbimGeometryModel::Cut(IXbimGeometryModel^ shape, XbimModelFactors^ factors)
			{
				if(dynamic_cast<XbimGeometryModel^>(shape))
				{
					return this->Cut((XbimGeometryModel^) shape,factors->DeflectionTolerance,factors->PrecisionBoolean,factors->PrecisionBooleanMax,factors->Rounding);
				}
				else if(dynamic_cast<XbimPolyhedron^>(shape))
				{
					return this->Cut((XbimPolyhedron^) shape,factors->DeflectionTolerance,factors->PrecisionBoolean,factors->PrecisionBooleanMax,factors->Rounding);
				}
				else if(dynamic_cast<XbimFeaturedShape^>(shape))
				{
					return this->Cut((XbimFeaturedShape^) shape,factors->DeflectionTolerance,factors->PrecisionBoolean,factors->PrecisionBooleanMax,factors->Rounding);
				}
				Logger->ErrorFormat("Unsupported geometry type ({0}) used in boolean operation", shape->GetType()->Name);
				return this;
			}

			//boolean operations
			IXbimGeometryModel^ XbimGeometryModel::Union(IXbimGeometryModel^ shape, XbimModelFactors^ factors)
			{
				if(dynamic_cast<XbimGeometryModel^>(shape))
				{
					return this->Union((XbimGeometryModel^) shape,factors->DeflectionTolerance,factors->PrecisionBoolean,factors->PrecisionBooleanMax,factors->Rounding);
				}
				else if(dynamic_cast<XbimPolyhedron^>(shape))
				{
					return this->Union((XbimPolyhedron^) shape,factors->DeflectionTolerance,factors->PrecisionBoolean,factors->PrecisionBooleanMax,factors->Rounding);
				}
				else if(dynamic_cast<XbimFeaturedShape^>(shape))
				{
					return this->Union((XbimFeaturedShape^) shape,factors->DeflectionTolerance,factors->PrecisionBoolean,factors->PrecisionBooleanMax,factors->Rounding);
				}
				Logger->ErrorFormat("Unsupported geometry type ({0}) used in boolean operation", shape->GetType()->Name);
				return this;
			}

			//boolean operations
			IXbimGeometryModel^ XbimGeometryModel::Intersection(IXbimGeometryModel^ shape, XbimModelFactors^ factors)
			{
				if(dynamic_cast<XbimGeometryModel^>(shape))
				{
					return this->Intersection((XbimGeometryModel^) shape,factors->DeflectionTolerance,factors->PrecisionBoolean,factors->PrecisionBooleanMax,factors->Rounding);
				}
				else if(dynamic_cast<XbimPolyhedron^>(shape))
				{
					return this->Intersection((XbimPolyhedron^) shape,factors->DeflectionTolerance,factors->PrecisionBoolean,factors->PrecisionBooleanMax,factors->Rounding);
				}
				else if(dynamic_cast<XbimFeaturedShape^>(shape))
				{
					return this->Intersection((XbimFeaturedShape^) shape,factors->DeflectionTolerance,factors->PrecisionBoolean,factors->PrecisionBooleanMax,factors->Rounding);
				}
				Logger->ErrorFormat("Unsupported geometry type ({0}) used in boolean operation", shape->GetType()->Name);
				return this;
			}

			IXbimGeometryModel^ XbimGeometryModel::Combine(IXbimGeometryModel^ shape, XbimModelFactors^ factors)
			{
				if(dynamic_cast<XbimGeometryModel^>(shape))
				{
					return this->Combine((XbimGeometryModel^) shape,factors->DeflectionTolerance,factors->PrecisionBoolean,factors->PrecisionBooleanMax,factors->Rounding);
				}
				else if(dynamic_cast<XbimPolyhedron^>(shape))
				{
					return this->Combine((XbimPolyhedron^) shape,factors->DeflectionTolerance,factors->PrecisionBoolean,factors->PrecisionBooleanMax,factors->Rounding);
				}
				else if(dynamic_cast<XbimFeaturedShape^>(shape))
				{
					return this->Combine((XbimFeaturedShape^) shape,factors->DeflectionTolerance,factors->PrecisionBoolean,factors->PrecisionBooleanMax,factors->Rounding);
				}
				Logger->ErrorFormat("Unsupported geometry type ({0}) used in boolean operation", shape->GetType()->Name);
				return this;
			}

			XbimGeometryModel^ XbimGeometryModel::Cut(XbimGeometryModel^ shape,  double deflection, double precision, double maxPrecision, unsigned int rounding)
			{
				bool hasCurves =  _hasCurvedEdges || shape->HasCurvedEdges; //one has a curve the result will have one
#if defined USE_CARVE
				
				if(hasCurves && !dynamic_cast<XbimPolyhedron^>(shape)) //try opencascade first for curvce shapes, this is faster and more accurate than meshing and triangulation
				{
					ShapeFix_ShapeTolerance fTol;
					fTol.SetTolerance(*Handle, precision);
					fTol.SetTolerance(*(shape->Handle),precision);
					BRepAlgoAPI_Cut boolOp(*Handle,*(shape->Handle));
					GC::KeepAlive(shape);
					if(boolOp.ErrorStatus() == 0)
					{
						return  gcnew XbimSolid(boolOp.Shape(), hasCurves,_representationLabel,_surfaceStyleLabel);
					}
				}
				//if planar shape or opencascade failed then use polygonal geometry
				
				XbimPolyhedron^ shapeA = this->ToPolyHedron(deflection, precision, maxPrecision, rounding);
				XbimPolyhedron^ shapeB = shape->ToPolyHedron(deflection, precision, maxPrecision, rounding);
				
				XbimPolyhedron^ result = (XbimPolyhedron^)shapeA->Cut(shapeB,deflection, precision, maxPrecision, rounding);
				if(!result->IsClosed()) //if we have not got a closed manifold, see if opencascade can do better (sometimes does if curves are involved
				{		
					ShapeFix_ShapeTolerance fTol;
					double currentTolerance = precision;
					fTol.SetTolerance(*Handle, currentTolerance);
					fTol.SetTolerance(*(shape->Handle),currentTolerance);
					BRepAlgoAPI_Cut boolOp(*Handle,*(shape->Handle));
					GC::KeepAlive(shape);
					if(boolOp.ErrorStatus() == 0)
					{
						return  gcnew XbimSolid(boolOp.Shape(), hasCurves,_representationLabel,_surfaceStyleLabel);
						
					}
					Logger->WarnFormat("A closed manifold solid shape could not be created the subtraction of object #{0} from #{1}", shape->RepresentationLabel,this->RepresentationLabel );
				}
				return result; //else take what we have
#else

				ShapeFix_ShapeTolerance fTol;
				double currentTolerance = precision;
				fTol.SetTolerance(*Handle, currentTolerance);
				fTol.SetTolerance(*(shape->Handle),currentTolerance);
				bool warnPrecision=false;
TryCutSolid:		
				try
				{
					
					BRepAlgoAPI_Cut boolOp(*Handle,*(shape->Handle));
					GC::KeepAlive(shape);
					if(boolOp.ErrorStatus() == 0)
					{
						//make sure it is a valid geometry
						if( BRepCheck_Analyzer(boolOp.Shape(), Standard_True).IsValid() == Standard_True) 
						{ 
							if(warnPrecision)
								Logger->WarnFormat("Precision adjusted to {0}, declared {1}", currentTolerance,precision);
							return gcnew XbimSolid(boolOp.Shape(), hasCurves,_representationLabel,_surfaceStyleLabel);
						}
						else //if not try and fix it
						{
							return this;
						
							currentTolerance*=10; //try courser;
							warnPrecision=true;
							if(currentTolerance<=maxPrecision)
							{
								fTol.SetTolerance(*Handle, currentTolerance);
								fTol.SetTolerance(*(shape->Handle),currentTolerance);
								goto TryCutSolid;
							}
							
							BRep_Builder builder;
							TopoDS_Compound solids;
							builder.MakeCompound(solids);
							
							ShapeFix_Solid solidFixer;
							solidFixer.SetMinTolerance(precision);
							solidFixer.SetMaxTolerance(maxPrecision);
							solidFixer.SetPrecision(precision);
							solidFixer.CreateOpenSolidMode()= Standard_True;
							for (TopExp_Explorer shellEx(boolOp.Shape(),TopAbs_SHELL);shellEx.More();shellEx.Next()) //get each shell and solidify if possible
							{
								TopoDS_Solid solid = solidFixer.SolidFromShell(TopoDS::Shell(shellEx.Current()));
								if( BRepCheck_Analyzer(solid, Standard_True).IsValid() == Standard_False) 
								{
									ShapeFix_Shape sfs(solid);
									sfs.SetPrecision(precision);
									sfs.SetMinTolerance(precision);
									sfs.SetMaxTolerance(maxPrecision);
									sfs.Perform();
									if(sfs.Shape().ShapeType()==TopAbs_SHELL)
										solid = solidFixer.SolidFromShell(TopoDS::Shell(sfs.Shape()));
									else if(sfs.Shape().ShapeType()==TopAbs_SOLID)
										solid = TopoDS::Solid(sfs.Shape()); 
									else //anything else is useless, ignore
										solid.Nullify(); 
								}
								if(!solid.IsNull()) builder.Add(solids,TopoDS::Solid(solid));
							}
							if(warnPrecision)
							{
								

								Logger->WarnFormat("Precision adjusted to {0}, declared {1}", currentTolerance,precision);
							}
							return gcnew XbimSolid(solids, hasCurves,_representationLabel,_surfaceStyleLabel);
						}
					}
					else
					{/*
						BRepTools::Write(*Handle, "a");
								BRepTools::Write(*(shape->Handle), "b");*/
						if(boolOp.ErrorStatus()>8) //errors below this are just errors in the input model, precision will not help
						{
							currentTolerance*=10; //try courser;
							warnPrecision=true;
							if(currentTolerance<=maxPrecision)
							{
								fTol.SetTolerance(*Handle, currentTolerance);
								fTol.SetTolerance(*(shape->Handle),currentTolerance);
								goto TryCutSolid;
							}
						}
						//it isn't working

						Logger->ErrorFormat("Unable to perform boolean cut operation on shape #{0} with shape #{1}. Discarded", RepresentationLabel,shape->RepresentationLabel );
						return this;
					}
				}
				catch(Standard_Failure e)
				{
					String^ err = gcnew String(e.GetMessageString());
					Logger->ErrorFormat("Boolean error {0} on shape #{1} with shape #{2}. Discarded", err, RepresentationLabel,shape->RepresentationLabel );
					
				}		
				
				return this;//stick with what we started with
#endif
			}

			XbimGeometryModel^ XbimGeometryModel::Union(XbimGeometryModel^ shape,double deflection, double precision, double maxPrecision, unsigned int rounding)
			{
				bool hasCurves =  _hasCurvedEdges || shape->HasCurvedEdges; //one has a curve the result will have one
#if defined USE_CARVE
								if(hasCurves && !dynamic_cast<XbimPolyhedron^>(shape)) //try opencascade first for curvce shapes, this is faster and more accurate than meshing and triangulation
				{
					ShapeFix_ShapeTolerance fTol;
					fTol.SetTolerance(*Handle, precision);
					fTol.SetTolerance(*(shape->Handle),precision);
					BRepAlgoAPI_Fuse boolOp(*Handle,*(shape->Handle));
					GC::KeepAlive(shape);
					if(boolOp.ErrorStatus() == 0)
					{
						return  gcnew XbimSolid(boolOp.Shape(), hasCurves,_representationLabel,_surfaceStyleLabel);
					}
				}
				//if planar shape or opencascade failed then use polygonal geometry
				
				XbimPolyhedron^ shapeA = this->ToPolyHedron(deflection, precision, maxPrecision, rounding);
				XbimPolyhedron^ shapeB = shape->ToPolyHedron(deflection, precision, maxPrecision, rounding);
				
				XbimPolyhedron^ result = (XbimPolyhedron^)shapeA->Union(shapeB,deflection, precision, maxPrecision, rounding);
				if(!result->IsClosed()) //if we have not got a closed manifold, see if opencascade can do better (sometimes does if curves are involved
				{		
					ShapeFix_ShapeTolerance fTol;
					double currentTolerance = precision;
					fTol.SetTolerance(*Handle, currentTolerance);
					fTol.SetTolerance(*(shape->Handle),currentTolerance);
					BRepAlgoAPI_Fuse boolOp(*Handle,*(shape->Handle));
					GC::KeepAlive(shape);
					if(boolOp.ErrorStatus() == 0)
					{
						return  gcnew XbimSolid(boolOp.Shape(), hasCurves,_representationLabel,_surfaceStyleLabel);
					}
					Logger->WarnFormat("A closed manifold solid shape could not be created from the union of object #{0} and #{1}", shape->RepresentationLabel,this->RepresentationLabel );
				}
				return result; //else take what we have
#else
				ShapeFix_ShapeTolerance fTol;
				double currentTolerance = precision;
				fTol.SetTolerance(*Handle, currentTolerance);
				fTol.SetTolerance(*(shape->Handle),currentTolerance);
TryUnionSolid:		
				try
				{
					
					BRepAlgoAPI_Fuse boolOp(*Handle,*(shape->Handle));
					if(boolOp.ErrorStatus() == 0)
					{
						//make sure it is a valid geometry
						if( BRepCheck_Analyzer(boolOp.Shape(), Standard_True).IsValid() == Standard_True) 
							return gcnew XbimSolid(boolOp.Shape(), hasCurves,_representationLabel,_surfaceStyleLabel);
						else //if not try and fix it
						{

							currentTolerance*=10; //try courser;
							if(currentTolerance<=maxPrecision)
							{
								fTol.SetTolerance(*Handle, currentTolerance);
								fTol.SetTolerance(*(shape->Handle),currentTolerance);
								goto TryUnionSolid;
							}
							ShapeFix_Shape sfs(boolOp.Shape());
							sfs.SetPrecision(precision);
							sfs.SetMinTolerance(precision);
							sfs.SetMaxTolerance(maxPrecision);
							sfs.Perform();
#ifdef _DEBUG
							if( BRepCheck_Analyzer(sfs.Shape(), Standard_True).IsValid() == Standard_False) //in release builds except the geometry is not compliant
								Logger->ErrorFormat("Unable to create valid shape when performing boolean union operation on shape #{0} with shape #{1}. Discarded", RepresentationLabel,shape->RepresentationLabel );
					
#endif // _DEBUG
								return gcnew XbimSolid(sfs.Shape(), hasCurves,_representationLabel,_surfaceStyleLabel);
						}
					}
					else
					{
						currentTolerance*=10; //try courser;
						if(currentTolerance<=maxPrecision)
						{
							fTol.SetTolerance(*Handle, currentTolerance);
							fTol.SetTolerance(*(shape->Handle),currentTolerance);
							goto TryUnionSolid;
						}
						//it isn't working
						Logger->ErrorFormat("Unable to perform boolean union operation on shape #{0} with shape #{1}. Discarded", RepresentationLabel,shape->RepresentationLabel );
						return this;
					}
				}
				catch(Standard_Failure e)
				{
					String^ err = gcnew String(e.GetMessageString());
					Logger->ErrorFormat("Boolean  error {0} on shape #{1} with shape #{2}. Discarded", err, RepresentationLabel,shape->RepresentationLabel );
					
				}	
				return this;//stick with what we started with
#endif
			}


			XbimGeometryModel^ XbimGeometryModel::Intersection(XbimGeometryModel^ shape,double deflection, double precision, double maxPrecision, unsigned int rounding)
			{
				bool hasCurves =  _hasCurvedEdges || shape->HasCurvedEdges; //one has a curve the result will have one
#if defined USE_CARVE
				
				if(hasCurves && !dynamic_cast<XbimPolyhedron^>(shape)) //try opencascade first for curvce shapes, this is faster and more accurate than meshing and triangulation
				{
					ShapeFix_ShapeTolerance fTol;
					fTol.SetTolerance(*Handle, precision);
					fTol.SetTolerance(*(shape->Handle),precision);
					BRepAlgoAPI_Common boolOp(*Handle,*(shape->Handle));
					GC::KeepAlive(shape);
					if(boolOp.ErrorStatus() == 0)
					{
						return  gcnew XbimSolid(boolOp.Shape(), hasCurves,_representationLabel,_surfaceStyleLabel);
					}
				}
				//if planar shape or opencascade failed then use polygonal geometry
				
				XbimPolyhedron^ shapeA = this->ToPolyHedron(deflection, precision, maxPrecision, rounding);
				XbimPolyhedron^ shapeB = shape->ToPolyHedron(deflection, precision, maxPrecision, rounding);
				
				XbimPolyhedron^ result = (XbimPolyhedron^)shapeA->Intersection(shapeB,deflection, precision, maxPrecision, rounding);
				if(!result->IsClosed()) //if we have not got a closed manifold, see if opencascade can do better (sometimes does if curves are involved
				{		
					ShapeFix_ShapeTolerance fTol;
					double currentTolerance = precision;
					fTol.SetTolerance(*Handle, currentTolerance);
					fTol.SetTolerance(*(shape->Handle),currentTolerance);
					BRepAlgoAPI_Common boolOp(*Handle,*(shape->Handle));
					GC::KeepAlive(shape);
					if(boolOp.ErrorStatus() == 0)
					{
						return  gcnew XbimSolid(boolOp.Shape(), hasCurves,_representationLabel,_surfaceStyleLabel);
						
					}
					Logger->WarnFormat("A closed manifold solid shape could not be created the intersection of object #{0} and #{1}", shape->RepresentationLabel,this->RepresentationLabel );
				}
				return result; //else take what we have
#else
				ShapeFix_ShapeTolerance fTol;
				double currentTolerance = precision;
				fTol.SetTolerance(*Handle, currentTolerance);
				fTol.SetTolerance(*(shape->Handle),currentTolerance);
TryIntersectSolid:		

				try
				{
					BRepAlgoAPI_Common boolOp(*Handle,*(shape->Handle));	
					if(boolOp.ErrorStatus() == 0)
					{
						//make sure it is a valid geometry
						if( BRepCheck_Analyzer(boolOp.Shape(), Standard_True).IsValid() == Standard_True) 
							return gcnew XbimSolid(boolOp.Shape(), hasCurves,_representationLabel,_surfaceStyleLabel);
						else //if not try and fix it
						{
							currentTolerance*=10; //try courser;
							if(currentTolerance<=maxPrecision)
							{
								fTol.SetTolerance(*Handle, currentTolerance);
								fTol.SetTolerance(*(shape->Handle),currentTolerance);
								goto TryIntersectSolid;
							}
							ShapeFix_Shape sfs(boolOp.Shape());
							sfs.SetPrecision(precision);
							sfs.SetMinTolerance(precision);
							sfs.SetMaxTolerance(maxPrecision);
							sfs.Perform();
#ifdef _DEBUG
							if( BRepCheck_Analyzer(sfs.Shape(), Standard_True).IsValid() == Standard_True) //in release builds except the geometry is not compliant
								Logger->ErrorFormat("Unable to create valid shape when performing boolean union operation on shape #{0} with shape #{1}. Discarded", RepresentationLabel,shape->RepresentationLabel );
#endif // _DEBUG
								return gcnew XbimSolid(sfs.Shape(), hasCurves,_representationLabel,_surfaceStyleLabel);
						}
					}
					else
					{
						currentTolerance*=10; //try courser;
						if(currentTolerance<=maxPrecision)
						{
							fTol.SetTolerance(*Handle, currentTolerance);
							fTol.SetTolerance(*(shape->Handle),currentTolerance);
							goto TryIntersectSolid;
						}
						
						//it isn't working
						Logger->ErrorFormat("Unable to perform intersect operation on shape #{0} with shape #{1}. Discarded", RepresentationLabel,shape->RepresentationLabel );
						return this;
					}
				}
				catch(Standard_Failure e)
				{
					String^ err = gcnew String(e.GetMessageString());
					Logger->ErrorFormat("Boolean error {0} on shape #{1} with shape #{2}. Discarded", err, RepresentationLabel,shape->RepresentationLabel );
					
				}
				return this;//stick with what we started with
#endif
			}

			XbimMeshFragment XbimGeometryModel::MeshTo(IXbimMeshGeometry3D^ mesh3D, IfcProduct^ product, XbimMatrix3D transform, double deflection, short modelId)
			{
				
				
				bool doTranform = !transform.IsIdentity;
				IXbimTriangulatesToPositionsIndices^ theMesh = dynamic_cast<IXbimTriangulatesToPositionsIndices^>(mesh3D);
				theMesh->BeginBuild();
                XbimMeshFragment fragment(mesh3D->PositionCount,mesh3D->TriangleIndexCount, modelId);
                fragment.EntityLabel = product->EntityLabel;
                fragment.EntityTypeId = IfcMetaData::IfcTypeId(product->GetType());

				TopoDS_Shape shape = *(this->Handle);
				Monitor::Enter(this);
				try
				{
					BRepMesh_IncrementalMesh incrementalMesh(shape, deflection); //triangulate the first time
					
				}
				finally
				{
					Monitor::Exit(this);
				}
				std::unordered_map<Double3D, size_t> vertexMap;
				int offset=-1; //opencascade indexes are 1 based, so we need to move back 1 index
				for (TopExp_Explorer shellEx(shape,TopAbs_SHELL) ; shellEx.More(); shellEx.Next()) 	
				{		
					for (TopExp_Explorer faceEx(shellEx.Current(),TopAbs_FACE) ; faceEx.More(); faceEx.Next()) 	
					{
						
						const TopoDS_Face & face = TopoDS::Face(faceEx.Current());
						TopAbs_Orientation orient = face.Orientation();
						TopLoc_Location loc;
						Handle (Poly_Triangulation) facing = BRep_Tool::Triangulation(face,loc);
						if(facing.IsNull()) continue;

						Standard_Integer nbNodes = facing->NbNodes();
						Standard_Integer nbTriangles = facing->NbTriangles();
						
						const TColgp_Array1OfPnt& points = facing->Nodes();
						//std::unordered_map<Standard_Integer,size_t> posMap;
						for(Standard_Integer nd = 1 ; nd <= nbNodes ; nd++)
						{
							gp_XYZ p = points(nd).XYZ();
							loc.Transformation().Transforms(p);			 

							if(doTranform)
								theMesh->AddPosition(transform.Transform(XbimPoint3D(p.X(),p.Y(),p.Z())));
							else
								theMesh->AddPosition(XbimPoint3D(p.X(),p.Y(),p.Z()));
						}
		
						const Poly_Array1OfTriangle& triangles = facing->Triangles();
						Standard_Integer n1, n2, n3;	
						
						for(Standard_Integer tr = 1 ; tr <= nbTriangles ; tr++)
						{
							
						    theMesh->BeginPolygon(TriangleType::GL_Triangles, 0);
							triangles(tr).Get(n1, n2, n3); // triangle indices are 1 based
							if(orient == TopAbs_REVERSED) //srl code below fixed to get normals in the correct order of triangulation
							{
								theMesh->AddTriangleIndex(offset+n3);
								theMesh->AddTriangleIndex(offset+n2);
								theMesh->AddTriangleIndex(offset+n1);
							}
							else
							{
								theMesh->AddTriangleIndex(offset+n1);
								theMesh->AddTriangleIndex(offset+n2);
								theMesh->AddTriangleIndex(offset+n3);
							}
						}	
						offset+=nbNodes;
					}
				}
                fragment.EndPosition = mesh3D->PositionCount-1;
                fragment.EndTriangleIndex = mesh3D->TriangleIndexCount-1;
                theMesh->EndBuild();
				mesh3D->Meshes->Add(fragment);
				Monitor::Enter(this);
				try
				{
					BRepTools::Clean(shape); //remove all triangulatulation
					
				}
				finally
				{
					Monitor::Exit(this);
				}
				return fragment;
			}

			XbimTriangulatedModelCollection^ XbimGeometryModel::Mesh(double deflection )
			{	
				//Build the Mesh
				XbimTriangularMeshStreamer value(RepresentationLabel, SurfaceStyleLabel);
				XbimTriangularMeshStreamer* m = &value;
				//decide which meshing algorithm to use, Opencascade is slow but necessary to resolve curved edges
				TopoDS_Shape shape = *(Handle);
				if (HasCurvedEdges) 
				{
					Monitor::Enter(this);
					try
					{
						try
						{				
							BRepMesh_IncrementalMesh incrementalMesh(shape, deflection);
							OpenCascadeShapeStreamerFeed(shape, m);								
						}
						catch(Standard_Failure e)
						{
							String^ err = gcnew String(e.GetMessageString());
							Logger->ErrorFormat("Mesh triangulation error, {0}. Geometry {1} has been discarded", err,RepresentationLabel);
							return gcnew XbimTriangulatedModelCollection();
						}
					}
					finally
					{
						Monitor::Exit(this);
					}
					
					GC::KeepAlive(this); //stop the native object being deleted by the garbage collector
				}
				else
					OpenGLShapeStreamerFeed(shape, m);
				size_t uiCalcSize = m->StreamSize();
				IntPtr BonghiUnManMem = Marshal::AllocHGlobal((int)uiCalcSize);
				unsigned char* BonghiUnManMemBuf = (unsigned char*)BonghiUnManMem.ToPointer();
				size_t controlSize = m->StreamTo(BonghiUnManMemBuf);
				array<unsigned char>^ BmanagedArray = gcnew array<unsigned char>((int)uiCalcSize);
				Marshal::Copy(BonghiUnManMem, BmanagedArray, 0, (int)uiCalcSize);
				Marshal::FreeHGlobal(BonghiUnManMem);
				XbimTriangulatedModelCollection^ list = gcnew XbimTriangulatedModelCollection();
				list->Add(gcnew XbimTriangulatedModel(BmanagedArray, GetBoundingBox() ,RepresentationLabel, SurfaceStyleLabel) );

				return list;
			};

			XbimGeometryModel^ XbimGeometryModel::Combine(XbimGeometryModel^ shape,double deflection, double precision, double maxPrecision, unsigned int rounding)
			{
				bool hasCurves =  _hasCurvedEdges || shape->HasCurvedEdges; //one has a curve the result will have one
#if defined USE_CARVE
				
				//if planar shape or opencascade failed then use polygonal geometry

				XbimPolyhedron^ shapeA = this->ToPolyHedron(deflection, precision, maxPrecision, rounding);
				XbimPolyhedron^ shapeB = shape->ToPolyHedron(deflection, precision, maxPrecision, rounding);
				XbimPolyhedron^ result = (XbimPolyhedron^)shapeA->Combine(shapeB,deflection, precision, maxPrecision, rounding);
				return result; //else take what we have
#else
				ShapeFix_ShapeTolerance fTol;
				double currentTolerance = precision;
				fTol.SetTolerance(*Handle, currentTolerance);
				fTol.SetTolerance(*(shape->Handle),currentTolerance);
TryCombineSolid:		
				try
				{
					
					BRepAlgoAPI_Fuse boolOp(*Handle,*(shape->Handle));
					if(boolOp.ErrorStatus() == 0)
					{
						//make sure it is a valid geometry
						if( BRepCheck_Analyzer(boolOp.Shape(), Standard_True).IsValid() == Standard_True) 
							return gcnew XbimSolid(boolOp.Shape(), hasCurves,_representationLabel,_surfaceStyleLabel);
						else //if not try and fix it
						{

							currentTolerance*=10; //try courser;
							if(currentTolerance<=maxPrecision)
							{
								fTol.SetTolerance(*Handle, currentTolerance);
								fTol.SetTolerance(*(shape->Handle),currentTolerance);
								goto TryCombineSolid;
							}
							ShapeFix_Shape sfs(boolOp.Shape());
							sfs.SetPrecision(precision);
							sfs.SetMinTolerance(precision);
							sfs.SetMaxTolerance(maxPrecision);
							sfs.Perform();
#ifdef _DEBUG
							if( BRepCheck_Analyzer(sfs.Shape(), Standard_True).IsValid() == Standard_False) //in release builds except the geometry is not compliant
								Logger->ErrorFormat("Unable to create valid shape when performing boolean union operation on shape #{0} with shape #{1}. Discarded", RepresentationLabel,shape->RepresentationLabel );
					
#endif // _DEBUG
								return gcnew XbimSolid(sfs.Shape(), hasCurves,_representationLabel,_surfaceStyleLabel);
						}
					}
					else
					{
						currentTolerance*=10; //try courser;
						if(currentTolerance<=maxPrecision)
						{
							fTol.SetTolerance(*Handle, currentTolerance);
							fTol.SetTolerance(*(shape->Handle),currentTolerance);
							goto TryCombineSolid;
						}
						//it isn't working
						Logger->ErrorFormat("Unable to perform boolean union operation on shape #{0} with shape #{1}. Discarded", RepresentationLabel,shape->RepresentationLabel );
						return this;
					}
				}
				catch(Standard_Failure e)
				{
					String^ err = gcnew String(e.GetMessageString());
					Logger->ErrorFormat("Boolean  error {0} on shape #{1} with shape #{2}. Discarded", err, RepresentationLabel,shape->RepresentationLabel );
					
				}	
				return this;//stick with what we started with
#endif
			}

			bool XbimGeometryModel::Write(String^ fileName,XbimModelFactors^ modelFactors)
			{
				try	
				{
					XbimPolyhedron^ poly = this->ToPolyHedron(
						modelFactors->DeflectionTolerance,
						modelFactors->Precision,
						modelFactors->PrecisionMax,
						modelFactors->Rounding);
					return poly->Write(fileName,modelFactors);
				}
				catch(Exception^ )
				{
					return false;
				}
			}

			void XbimGeometryModel::TransformBy(XbimMatrix3D t)
			{

				if(Handle!=nullptr)
				{
					TopoDS_Shape temp = *(Handle);
					nativeHandle = new TopoDS_Shape();
					BRepBuilderAPI_Transform gTran(temp,XbimGeomPrim::ToTransform(t));
					*nativeHandle =gTran.Shape();
				}
			}

			IXbimPolyhedron^ XbimGeometryModel::ToPolyhedron(XbimModelFactors^ modelFactors)
			{
				return this->ToPolyHedron(modelFactors->DeflectionTolerance,modelFactors->PrecisionBoolean,modelFactors->PrecisionBooleanMax,modelFactors->Rounding);
			}

			String^ XbimGeometryModel::WriteAsString(XbimModelFactors^ modelFactors)
			{
				
				double deflection = modelFactors->DeflectionTolerance;
				double precision = modelFactors->Precision;
				double precisionMax = modelFactors->PrecisionMax;
				int rounding =  modelFactors->Rounding;
#if USE_CARVE
				return this->ToPolyHedron(deflection,precision,precisionMax,rounding)->WriteAsString(modelFactors);
#else
				StringBuilder^ sb = gcnew StringBuilder();
				TopoDS_Shape shape = *(this->Handle);
				Monitor::Enter(this);
				try
				{
					BRepMesh_IncrementalMesh incrementalMesh(shape, deflection); //triangulate the first time				
				}
				finally
				{
					Monitor::Exit(this);
				}
				std::unordered_map<Double3D, size_t> vertexMap;
				
				int normalsOffset = -1; //corrects for 1 based arrays in open cascade
				for (TopExp_Explorer shellEx(shape,TopAbs_SHELL) ; shellEx.More(); shellEx.Next()) 	
				{		
					for (TopExp_Explorer faceEx(shellEx.Current(),TopAbs_FACE) ; faceEx.More(); faceEx.Next()) 	
					{
						const TopoDS_Face & face = TopoDS::Face(faceEx.Current());
						
						TopAbs_Orientation orient = face.Orientation();
						TopLoc_Location loc;
						Handle (Poly_Triangulation) facing = BRep_Tool::Triangulation(face,loc);
						if(facing.IsNull()) continue;
						Handle(Geom_Surface) surf = BRep_Tool::Surface(face); //the surface
						BRepGProp_Face gprop(face);
						Standard_Integer nbNodes = facing->NbNodes();
						Standard_Integer nbNormals=0; //set when we know if it planar or not
						Standard_Integer nbTriangles = facing->NbTriangles();
						

						const TColgp_Array1OfPnt& points = facing->Nodes();
						std::vector<size_t> posMap;
						posMap.reserve(nbNodes+1);
						posMap.push_back(-1); //Opencascade lists are 1 based, move on one to avoid decrementing all indexes
						bool vWritten = false;
						GeomLib_IsPlanarSurface ps(surf, precision);
						Standard_Boolean planar = ps.IsPlanar();
						std::vector<double> normals;
						if(!planar) //only need to reserve if we have a curved surface
							normals.reserve(nbNodes * 3);
						for(Standard_Integer nd = 1 ; nd <= nbNodes ; nd++)
						{
							gp_XYZ p = points(nd).XYZ();
							loc.Transformation().Transforms(p);	
							if(!planar) //need to calculate the exact normals
							{
								GeomAPI_ProjectPointOnSurf projpnta(p, surf,precision);
								if(projpnta.IsDone())
								{
									double au, av; //the u- and v-coordinates of the projected point
									projpnta.LowerDistanceParameters(au, av); //get the nearest projection
									gp_Pnt centre;
									gp_Vec normalDir;
									gprop.Normal(au,av,centre,normalDir);	
									normalDir.Normalize();
									normals.push_back(normalDir.X());
									normals.push_back(normalDir.Y());
									normals.push_back(normalDir.Z());
								}
								else
								{
									normals.push_back(0);
									normals.push_back(0);
									normals.push_back(1);
								}

							}
							Double3D p3D(p.X(),p.Y(),p.Z(),precision,rounding); 
							//p3D.Round((float)rounding); //round the numbers to avpid numercic issues with precision
							std::unordered_map<Double3D, size_t>::const_iterator hit = vertexMap.find(p3D);
							if(hit==vertexMap.end()) //not found add it in
							{	size_t idx = vertexMap.size();
								vertexMap.insert(std::make_pair(p3D,idx));
								posMap.push_back(idx);
								if(!vWritten)
								{
									sb->AppendFormat("V");
									vWritten=true;
								}
								sb->AppendFormat(" {0},{1},{2}",p3D.Dim1, p3D.Dim2, p3D.Dim3);	
								
							}
							else
								posMap.push_back(hit->second);
						}
						if(vWritten) sb->AppendLine();
						
						String^ f = "$"; //the name of the face normal if it is a simple (LRUPFB)
						if(planar)
						{
							gp_Dir normal = ps.Plan().Axis().Direction();
							if(orient == TopAbs_REVERSED) normal.Reverse();
							nbNormals=0; //reset we will only write one if it is not a face as below
							
							if(normal.IsEqual(gp::DX(),0.1)) f="R";
							else if(normal.IsOpposite(gp::DX(),0.1)) f="L";
							else if(normal.IsEqual(gp::DY(),0.1)) f="B";
							else if(normal.IsOpposite(gp::DY(),0.1)) f="F";
							else if(normal.IsEqual(gp::DZ(),0.1)) f="U";
							else if(normal.IsOpposite(gp::DZ(),0.1)) f="D";
							else
							{
								if(abs(normal.X())<precision) normal.SetX(0.);
								if(abs(normal.Y())<precision) normal.SetY(0.);
								if(abs(normal.Z())<precision) normal.SetZ(0.);
								sb->AppendFormat("N {0},{1},{2}",(float)normal.X(),(float)normal.Y(),(float)normal.Z());	
								sb->AppendLine();
								nbNormals=1;
							}
						}
						else
						{
							nbNormals = nbNodes;					
							sb->AppendFormat("N");
							for (Standard_Integer nm = 0 ; nm < nbNodes ; nm++)
							{
								int ofs = nm*3;
								sb->AppendFormat(" {0},{1},{2}", normals[ofs],normals[ofs+1],normals[ofs+2]);	
							}
							sb->AppendLine();
						}
						const Poly_Array1OfTriangle& triangles = facing->Triangles();
						Standard_Integer n1, n2, n3;	


						sb->Append("T");
						for(Standard_Integer tr = 1 ; tr <= nbTriangles ; tr++)
						{
							if(orient == TopAbs_REVERSED) //srl code below fixed to get normals in the correct order of triangulation
								triangles(tr).Get(n3, n2, n1); // triangle indices are 1 based
							else
								triangles(tr).Get(n1, n2, n3);

							if(nbNormals>1) //we have a normal for every point
								sb->AppendFormat(" {0}/{3},{1}/{4},{2}/{5}", posMap[n1], posMap[n2], posMap[n3], normalsOffset+n1,normalsOffset+n2,normalsOffset+n3);
							else //we only have one normal for each point it is a plane
								if(tr==1) //only do the first one
								{
									if(f=="$") //face name is undefined
										sb->AppendFormat(" {0}/{3},{1},{2}", posMap[n1], posMap[n2], posMap[n3],normalsOffset+1);
									else
										sb->AppendFormat(" {0}/{3},{1},{2}", posMap[n1], posMap[n2], posMap[n3],f);
								}
								else
									sb->AppendFormat(" {0},{1},{2}", posMap[n1], posMap[n2], posMap[n3]);


						}	
						sb->AppendLine();
						normalsOffset+=nbNormals;//we will have written out this number of normals
					}
				}
				Monitor::Enter(this);
				try
				{
					BRepTools::Clean(shape); //remove all triangulatulation
					
				}
				finally
				{
					Monitor::Exit(this);
				}
				GC::KeepAlive(this);
				return sb->ToString();
#endif
			}

			

			XbimRect3D XbimGeometryModel::GetAxisAlignedBoundingBox()
			{
				XbimRect3D bb = GetBoundingBox();
				return XbimRect3D::TransformBy(bb, Transform);
			}

			XbimRect3D XbimGeometryModel::GetBoundingBox()
			{
				if(_bounds.IsEmpty)
				{
					Bnd_Box pBox;
					BRepBndLib::Add(*(this->Handle), pBox);
					Standard_Real srXmin, srYmin, srZmin, srXmax, srYmax, srZmax;
					if(pBox.IsVoid()) return XbimRect3D::Empty;
					pBox.Get(srXmin, srYmin, srZmin, srXmax, srYmax, srZmax);
					_bounds = XbimRect3D((float)srXmin, (float)srYmin, (float)srZmin, (float)(srXmax-srXmin),  (float)(srYmax-srYmin), (float)(srZmax-srZmin));
				}
				return _bounds;
			};

			IXbimGeometryModelGroup^ XbimGeometryModel::AsPolyhedron(double deflection, double precision,double precisionMax, unsigned int rounding) 
			{
				return ToPolyHedronCollection(deflection, precision, precisionMax,rounding);
			} 
		
			XbimPolyhedron^ XbimGeometryModel::ToPolyHedron(double deflection, double precision,double precisionMax, unsigned int rounding)
			{	
				TopoDS_Shape shape = *(this->Handle);
				std::vector<vertex_t> vertexStore; //vertices in the polyhedron
				std::vector<std::vector<carve::mesh::MeshSet<3>::vertex_t *>> faces; //faces on the polyhedron
				vertexStore.reserve(2048);
				faces.reserve(1024);
				TopTools_DataMapOfShapeInteger vertexMap;
				bool warned=false;
				std::vector<mesh_t*> meshes;

				if(!HasCurvedEdges)
				{
					bool hasUnboundedFace = false; //use this to check if we have a half space solid
					gp_Pln halfSpacePlane;
					gp_Pnt tl,bl,tr,br; //the four points of the bounding plane
					gp_Pnt tlm,blm,trm,brm; //the four points of the bounding plane
					for (TopExp_Explorer vEx(shape,TopAbs_VERTEX) ; vEx.More(); vEx.Next()) //gather all the points
					{
						const TopoDS_Vertex& curVert=TopoDS::Vertex(vEx.Current());
						if(!vertexMap.IsBound(curVert))
						{
							vertexMap.Bind(curVert,(Standard_Integer)vertexStore.size());
							gp_Pnt p = BRep_Tool::Pnt(curVert);
							
							vertexStore.push_back(carve::geom::VECTOR(p.X(), p.Y(), p.Z()));
						}
					}
					for (TopExp_Explorer shellEx(shape,TopAbs_SHELL) ; shellEx.More(); shellEx.Next()) 
					{
						faces.clear();
						//go over each face and gets its loop
						for (TopExp_Explorer faceEx(shellEx.Current(),TopAbs_FACE) ; faceEx.More(); faceEx.Next()) 
						{
							const TopoDS_Face& occFace = TopoDS::Face(faceEx.Current());
							TopAbs_Orientation orientation = occFace.Orientation();
							bool reversed = orientation==TopAbs_REVERSED;
							if(occFace.IsNull()) continue; //nothing here				
							TopoDS_Wire outerWire = BRepTools::OuterWire(occFace);//get the outer loop
							if(outerWire.IsNull()) //check if this is a half space
							{
								
								Handle(Geom_Surface) surface = BRep_Tool::Surface(occFace);
								if (surface->IsKind(STANDARD_TYPE(Geom_Plane))) 
								{
									hasUnboundedFace=true; 
									Handle(Geom_Plane) pl = Handle(Geom_Plane)::DownCast(surface);
									halfSpacePlane = pl->Pln();
									Standard_Real U1,  U2,  V1,  V2;
									pl->Bounds (U1, U2, V1, V2);
									U1/=1e90;U2/=1e90;V1/=1e90;V2/=1e90;
									pl->D0(U1,V1,bl);
									pl->D0(U1,V2,tl);
									pl->D0(U2,V1,br);
									pl->D0(U2,V2,tr);
									

									carve::input::PolyhedronData data;
									gp_Vec dir = halfSpacePlane.Axis().Direction();
									tlm=tl;trm=tr;blm=bl;brm=br;
									if( occFace.Orientation() == TopAbs_FORWARD )
									{
										dir *= -1.e+10;
										tl.Translate(dir);
										tr.Translate(dir);
										bl.Translate(dir);
										br.Translate(dir);
									}
									else
									{
										dir *= 1.e+10;
										tlm.Translate(dir);
										trm.Translate(dir);
										blm.Translate(dir);
										brm.Translate(dir);
									}


									data.addVertex(carve::geom::VECTOR(tlm.X(), tlm.Y(), tlm.Z()));
									data.addVertex(carve::geom::VECTOR(blm.X(), blm.Y(), blm.Z()));
									data.addVertex(carve::geom::VECTOR(brm.X(), brm.Y(), brm.Z()));
									data.addVertex(carve::geom::VECTOR(trm.X(), trm.Y(), trm.Z()));
									data.addVertex(carve::geom::VECTOR(tl.X(), tl.Y(), tl.Z()));
									data.addVertex(carve::geom::VECTOR(bl.X(), bl.Y(), bl.Z()));
									data.addVertex(carve::geom::VECTOR(br.X(), br.Y(), br.Z()));
									data.addVertex(carve::geom::VECTOR(tr.X(), tr.Y(), tr.Z()));


									data.addFace(0, 1, 2, 3);
									data.addFace(7, 6, 5, 4);
									data.addFace(0, 4, 5, 1);
									data.addFace(1, 5, 6, 2);
									data.addFace(2, 6, 7, 3);
									data.addFace(3, 7, 4, 0);

									carve::csg::CSG::meshset_t* m = data.createMesh( carve::input::Options());
									return  gcnew XbimPolyhedron(m, RepresentationLabel, SurfaceStyleLabel);

								}
								continue;
							}; //nothing here
							std::vector<vertex_t *> initialFaceLoop;//first get the outer loop
							for(BRepTools_WireExplorer outerWireEx(outerWire);outerWireEx.More();outerWireEx.Next())
							{
								const TopoDS_Vertex& vertex=  outerWireEx.CurrentVertex();
								initialFaceLoop.push_back(&vertexStore[vertexMap.Find(vertex)]);
							}
							if(initialFaceLoop.size() < 3) //we do not have a valid face
							{
								Logger->InfoFormat("A face with {0} edges found in IfcRepresentationItem #{1}, 3 is minimum. Ignored",initialFaceLoop.size(), RepresentationLabel );
								continue;
							}
							TopExp_Explorer wireEx(occFace,TopAbs_WIRE);
							wireEx.Next();
							if(wireEx.More()) //we have more than one wire
							{
								wireEx.ReInit();
								std::vector<std::vector<vertex_t *>> holes;
								for(;wireEx.More();wireEx.Next()) //go   over holes
								{
									TopoDS_Wire holeWire = TopoDS::Wire(wireEx.Current());
									if(holeWire.IsEqual(outerWire)) 
										continue; //skip the outer wire

									BRepTools_WireExplorer wEx(holeWire, occFace);


									if(wEx.More())
									{
										holes.push_back(std::vector<vertex_t *>());
										std::vector<vertex_t *> & holeLoop = holes.back();				
										for(;wEx.More();wEx.Next())
										{
											const TopoDS_Vertex& vertex=  wEx.CurrentVertex();
											holeLoop.push_back(&vertexStore[vertexMap.Find(vertex)]);
										}
										if(holeLoop.size() < 3) //we do not have a valid hole
										{
											Logger->WarnFormat("An opening with {0} edges found in IfcRepresentation #{1}, 3 is minimum. Ignored",initialFaceLoop.size(), RepresentationLabel );
											holes.pop_back();
										}
									}

								}
								face_t face(initialFaceLoop.begin(), initialFaceLoop.end());
								std::vector<std::vector<carve::geom2d::P2> > projected_poly;
								projected_poly.resize(holes.size() + 1);
								projected_poly[0].reserve(initialFaceLoop.size());
								for (size_t j = 0; j < initialFaceLoop.size(); ++j) {
									projected_poly[0].push_back(face.project(initialFaceLoop[j]->v));
								}
								for (size_t j = 0; j < holes.size(); ++j) {
									projected_poly[j+1].reserve(holes[j].size());
									for (size_t k = 0; k < holes[j].size(); ++k) {
										projected_poly[j+1].push_back(face.project(holes[j][k]->v));
									}
								}
								try
								{
									std::vector<std::pair<size_t, size_t> > result = carve::triangulate::incorporateHolesIntoPolygon(projected_poly);
									faces.push_back(std::vector<carve::mesh::MeshSet<3>::vertex_t *>());
									std::vector<carve::mesh::MeshSet<3>::vertex_t *> &out = faces.back();
									out.reserve(result.size());
									for (size_t j = 0; j < result.size(); ++j) 
									{
										if (result[j].first == 0) 
											out.push_back(initialFaceLoop[result[j].second]);
										else 
											out.push_back(holes[result[j].first-1][result[j].second]);
									}	
								}
								catch(...) //in case hole punching fails
								{
									if(!warned)
									{
										warned=true;
										Logger->WarnFormat("Face error. Inner face loop is not contained in outer face loop. The geometry has been incorrectly defined in entity #{0}, the inner loop has been ignored" ,this->RepresentationLabel);				

									}
									faces.push_back(std::vector<carve::mesh::MeshSet<3>::vertex_t *>());
									std::vector<carve::mesh::MeshSet<3>::vertex_t *> &out = faces.back();
									out.reserve(initialFaceLoop.size());
									for (size_t i = 0; i < initialFaceLoop.size(); i++)
									{
										out.push_back(initialFaceLoop[i]);
									}	
								}
							}
							else //solid face, no holes
							{
								faces.push_back(std::vector<carve::mesh::MeshSet<3>::vertex_t *>());
								std::vector<carve::mesh::MeshSet<3>::vertex_t *> &out = faces.back();
								out.reserve(initialFaceLoop.size());
								for (size_t i = 0; i < initialFaceLoop.size(); i++)
								{
									out.push_back(initialFaceLoop[i]);
								}	
							}
						}
						std::vector<face_t *> faceList;
						faceList.reserve(faces.size());
						for (size_t i = 0; i < faces.size(); ++i) 
							faceList.push_back(new face_t(faces[i].begin(), faces[i].end()));
						std::vector<mesh_t*> theseMeshes;
						mesh_t::create(faceList.begin(), faceList.end(), theseMeshes, carve::mesh::MeshOptions(),precision*precision, false);
						for (size_t i = 0; i < theseMeshes.size(); i++)
						{
							meshes.push_back(theseMeshes[i]);
						}

					}
					
				}
				else //triangulate the faces
				{

					Monitor::Enter(this);
					try
					{
						BRepMesh_IncrementalMesh incrementalMesh(shape, deflection); //triangulate the first time
					}
					finally
					{
						Monitor::Exit(this);
					}

					std::unordered_map<Double3D, size_t> vertexMap;
					for (TopExp_Explorer vEx(shape,TopAbs_VERTEX) ; vEx.More(); vEx.Next()) //gather all the points
					{
						const TopoDS_Vertex& curVert=TopoDS::Vertex(vEx.Current());
						gp_Pnt p = BRep_Tool::Pnt(curVert);
						Double3D p3D(p.X(),p.Y(),p.Z(),precision,rounding);
						std::unordered_map<Double3D, size_t>::const_iterator hit = vertexMap.find(p3D);
						if(hit==vertexMap.end())
						{
							vertexMap.insert(std::make_pair(p3D,vertexStore.size()));	
							vertexStore.push_back(carve::geom::VECTOR(p.X(), p.Y(), p.Z()));
						}
					}
					for (TopExp_Explorer shellEx(shape,TopAbs_SHELL) ; shellEx.More(); shellEx.Next()) 	
					{
						faces.clear();
						for (TopExp_Explorer faceEx(shellEx.Current(),TopAbs_FACE) ; faceEx.More(); faceEx.Next()) 	
						{
							const TopoDS_Face & face = TopoDS::Face(faceEx.Current());
							TopAbs_Orientation orient = face.Orientation();
							TopLoc_Location loc;
							Handle (Poly_Triangulation) facing = BRep_Tool::Triangulation(face,loc);
							if(facing.IsNull()) continue;

							Standard_Integer nbNodes = facing->NbNodes();
							Standard_Integer nbTriangles = facing->NbTriangles();

							const TColgp_Array1OfPnt& points = facing->Nodes();
							std::unordered_map<Standard_Integer,size_t> posMap;
							for(Standard_Integer nd = 1 ; nd <= nbNodes ; nd++)
							{
								gp_XYZ p = points(nd).XYZ();
								loc.Transformation().Transforms(p);			 
								Double3D p3D(p.X(),p.Y(),p.Z(),precision,rounding);
								std::unordered_map<Double3D, size_t>::const_iterator hit = vertexMap.find(p3D);
								if(hit==vertexMap.end()) //not found add it in
								{	
									posMap.insert(std::make_pair(nd,vertexStore.size()));
									vertexMap.insert(std::make_pair(p3D,vertexStore.size()));									
									vertexStore.push_back(carve::geom::VECTOR(p.X(),p.Y(),p.Z()));
								}
								else
									posMap.insert(std::make_pair(nd,hit->second));
							}
							const Poly_Array1OfTriangle& triangles = facing->Triangles();
							Standard_Integer n1, n2, n3;			
							for(Standard_Integer tr = 1 ; tr <= nbTriangles ; tr++)
							{
								triangles(tr).Get(n1, n2, n3); // triangle indices are 1 based
								faces.push_back(std::vector<carve::mesh::MeshSet<3>::vertex_t *>());
								std::vector<carve::mesh::MeshSet<3>::vertex_t *> &m = faces.back();
								m.reserve(3);
								if(orient == TopAbs_REVERSED) //srl code below fixed to get normals in the correct order of triangulation
								{
									m.push_back(&vertexStore[posMap[n3]]);
									m.push_back(&vertexStore[posMap[n2]]);
									m.push_back(&vertexStore[posMap[n1]]);
								}
								else
								{
									m.push_back(&vertexStore[posMap[n1]]);
									m.push_back(&vertexStore[posMap[n2]]);
									m.push_back(&vertexStore[posMap[n3]]);
								}
							}
						}
						std::vector<face_t *> faceList;
						faceList.reserve(faces.size());
						for (size_t i = 0; i < faces.size(); ++i) 
						{
							faceList.push_back(new face_t(faces[i].begin(), faces[i].end()));
						}
						std::vector<mesh_t*> theseMeshes;
						mesh_t::create(faceList.begin(), faceList.end(), theseMeshes, carve::mesh::MeshOptions(), false);
						for (size_t i = 0; i < theseMeshes.size(); i++)
						{
							if(theseMeshes[i]!=nullptr && theseMeshes[i]->isClosed()) //if we have not got a closed manifold, remove any open manifolds
					    {
							meshes.push_back(theseMeshes[i]);
						}
							else
							{
								meshes.push_back(theseMeshes[i]);
							}
							
						}
						
					}

				}
				//Make the Polyhedron 
				meshset_t *mesh = new meshset_t(vertexStore, meshes);
				XbimPolyhedron^ p =  gcnew XbimPolyhedron(mesh, RepresentationLabel, SurfaceStyleLabel);
			//	p->WritePly("s",true);
				return p;

			}

			bool XbimGeometryModel::Intersects(XbimGeometryModel^ other)
			{
				Bnd_Box aBox;
				BRepBndLib::Add(*(this->Handle), aBox);
				Bnd_Box bBox;
				BRepBndLib::Add(*(other->Handle), bBox);
				return aBox.IsOut(bBox)==Standard_False;
			}

			bool XbimGeometryModel::IsMap::get() 
			{
				return dynamic_cast<XbimMap^>(this)!=nullptr;
			};


		}
	}
}
