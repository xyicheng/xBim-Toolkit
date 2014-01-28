#include "StdAfx.h"
#include "XbimGeometryModel.h"
#include "XbimTriangularMeshStreamer.h"
#include "XbimSolid.h"
#include "XbimFacetedShell.h"


#include "XbimGeometryModelCollection.h"
#include "XbimGeomPrim.h"
using namespace System::Collections::Generic;
using namespace Xbim::XbimExtensions;
using namespace Xbim::Ifc2x3::Extensions;
using namespace System::Linq;
using namespace Xbim::Common::Exceptions;
using namespace Xbim::Common::Geometry;
using namespace Xbim::Ifc2x3::PresentationAppearanceResource;
using namespace Xbim::IO;
using namespace  System::Text;

#include <carve/csg.hpp>
#include <carve/mesh.hpp>
#include <carve/polyline.hpp>
#include <carve/pointset.hpp>
#include <carve/rtree.hpp>
#include <carve/mesh_ops.hpp>
#include <carve/mesh_simplify.hpp>
#include <carve/geom2d.hpp>
#include <carve/heap.hpp>
#include <carve/triangulator.hpp>
#include <BRepBuilderAPI_Sewing.hxx>
#include <BRepCheck_Analyzer.hxx>
#include <ShapeFix_Shape.hxx>


class GLUtesselator {};

namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{

			XbimFacetedShell::XbimFacetedShell( bool solid,IfcRepresentationItem^ faceSet , bool hasCurves,int representationLabel, int surfaceStyleLabel)
			{
				_hasCurvedEdges=hasCurves;
				RepresentationLabel=representationLabel;
				SurfaceStyleLabel=surfaceStyleLabel;
				_faceSet=faceSet;
				isSolid=solid;
			};

			
			XbimFacetedShell::XbimFacetedShell(IfcFacetedBrep^ brep)
			{
				Init(brep);
				_faceSet=brep;

			}
			XbimFacetedShell::XbimFacetedShell(IfcShellBasedSurfaceModel^ sbms)
			{
				Init(sbms);
				_faceSet=sbms;

			}
			XbimFacetedShell::XbimFacetedShell(IfcFaceBasedSurfaceModel^ fbms)
			{
				Init(fbms);
				_faceSet=fbms;

			}
			XbimFacetedShell::XbimFacetedShell(IfcConnectedFaceSet^ shell)
			{
				Init(shell);
				_faceSet=shell;

			}
			XbimFacetedShell::XbimFacetedShell(IfcOpenShell^ shell)
			{
				Init(shell);
				_faceSet=shell;

			}
			XbimFacetedShell::XbimFacetedShell(IfcClosedShell^ shell)
			{
				Init(shell);
				_faceSet=shell;
			}

			void XbimFacetedShell::Build(IfcClosedShell^ cShell)
			{
				System::Diagnostics::Debug::Assert(nativeHandle==nullptr);
				TopoDS_Shape shape = XbimShell::Build((IfcConnectedFaceSet^)cShell, _hasCurvedEdges); //get a sewn shell
				
				double precision = cShell->ModelOf->ModelFactors->Precision;
				double maxPrecision = cShell->ModelOf->ModelFactors->PrecisionMax;
				
				ShapeFix_Solid solidFixer;
				solidFixer.SetMinTolerance(precision);
				solidFixer.SetMaxTolerance(maxPrecision);
				solidFixer.SetPrecision(precision);
				solidFixer.CreateOpenSolidMode()= Standard_True;
				TopoDS_Compound solids;
				BRep_Builder builder;
				builder.MakeCompound(solids);
				for (TopExp_Explorer shellEx(shape,TopAbs_SHELL);shellEx.More();shellEx.Next()) //get each shell and make it a solid
				{
					TopoDS_Shell shell = TopoDS::Shell(shellEx.Current());
					if(BRepCheck_Analyzer(shell,Standard_True).IsValid() == Standard_False)
					{
						ShapeFix_Shell sfs(shell);
						sfs.SetPrecision(precision);
						sfs.SetMinTolerance(precision);
						sfs.SetMaxTolerance(maxPrecision);
						sfs.Perform();
						shell = sfs.Shell();
					}
					TopoDS_Solid solid = solidFixer.SolidFromShell(shell);
					if(!solid.IsNull())
					{
						GProp_GProps System;
						BRepGProp::VolumeProperties(solid, System, Standard_False);
						double vol =  System.Mass();
						if(vol<0) 
							solid.Reverse();
						else if(vol==0)
						{
							Logger->WarnFormat("IfcClosedShell #{0} contains a solid with zero volume. Ignored", cShell->EntityLabel);
							continue;
						}
						builder.Add(solids,solid);
					}
				}
				nativeHandle = new TopoDS_Shape();
				/*if(BRepCheck_Analyzer(solids,Standard_True).IsValid() == Standard_False)
				{
					ShapeFix_Shape sfs(solids);
					sfs.SetPrecision(precision);
					sfs.SetMinTolerance(precision);
					sfs.SetMaxTolerance(maxPrecision);
					sfs.Perform();
					*nativeHandle = sfs.Shape();
				}
				else */
				*nativeHandle=solids;
				
				isSolid=true;
			}

			void XbimFacetedShell::Build(IfcConnectedFaceSet^ repItem)
			{

				if(dynamic_cast<IfcClosedShell^>(repItem))
					Build((IfcClosedShell^)repItem);
				else 
				{
					System::Diagnostics::Debug::Assert(nativeHandle==nullptr);
					nativeHandle = new TopoDS_Shape();
					*nativeHandle = XbimShell::Build(repItem, _hasCurvedEdges); //get a sewn shell
					isSolid=false;
				}


			}

			void XbimFacetedShell::Build(IfcRepresentationItem^ repItem)
			{
				if(dynamic_cast<IfcClosedShell^>(repItem))
					Build((IfcClosedShell^)repItem);
				else if(dynamic_cast<IfcOpenShell^>(repItem))
					Build((IfcOpenShell^)repItem);
				else if(dynamic_cast<IfcConnectedFaceSet^>(repItem))
					Build((IfcConnectedFaceSet^)repItem);		 
				else if(dynamic_cast<IfcFacetedBrep^>(repItem))
				{
					XbimFacetedShell^ fs = gcnew XbimFacetedShell(((IfcFacetedBrep^)repItem)->Outer);
					fs->Build();	
					shapes->Clear(); //just in case
					for (TopExp_Explorer compEx(*(fs->Handle),TopAbs_COMPOUND);compEx.More();compEx.Next()) //get first compound and use it
					{
						System::Diagnostics::Debug::Assert(nativeHandle==nullptr);
						nativeHandle = new TopoDS_Compound();
						*nativeHandle=TopoDS::Compound(compEx.Current());
						break;
					}
					isSolid=fs->isSolid;
				}
				else if (dynamic_cast<IfcFaceBasedSurfaceModel^>(repItem))
					Build((IfcFaceBasedSurfaceModel^)repItem);
				else if (dynamic_cast<IfcShellBasedSurfaceModel^>(repItem))
					Build((IfcShellBasedSurfaceModel^)repItem);
				else
					throw gcnew XbimGeometryException("Unsupported Facetted model "+ repItem->GetType()->Name+ " in #" + repItem->EntityLabel);

			}


			XbimFacetedShell::XbimFacetedShell(IfcShell^ shell)
			{

				if(dynamic_cast<IfcOpenShell^>(shell))
					_faceSet=(IfcOpenShell^)shell;
				else if(dynamic_cast<IfcClosedShell^>(shell))
					_faceSet=(IfcClosedShell^)shell;
				else
				{
					Type^ type = shell->GetType();
					throw gcnew XbimGeometryException("Error buiding shell from type " + type->Name);
				}
			}

			IList<IfcFace^>^ XbimFacetedShell::Faces()
			{
				//get a copy to reduce caching
				IfcRepresentationItem^ faceset = (IfcRepresentationItem^)_faceSet->ModelOf->Instances[_faceSet->EntityLabel];
				if(dynamic_cast<IfcClosedShell^>(faceset))
					return (IList<IfcFace^>^) ((IfcClosedShell^)faceset)->CfsFaces;
				else if(dynamic_cast<IfcOpenShell^>(faceset))
					return (IList<IfcFace^>^) ((IfcOpenShell^)faceset)->CfsFaces;
				else if(dynamic_cast<IfcConnectedFaceSet^>(faceset))
					return (IList<IfcFace^>^) ((IfcConnectedFaceSet^)_faceSet)->CfsFaces;	 
				else if(dynamic_cast<IfcFacetedBrep^>(faceset))
					return (IList<IfcFace^>^)(((IfcFacetedBrep^)faceset)->Outer);
				else if (dynamic_cast<IfcFaceBasedSurfaceModel^>(faceset))
				{
					List<IfcFace^>^ faceList = gcnew List<IfcFace^>();
					for each (IfcConnectedFaceSet^ fs in ((IfcFaceBasedSurfaceModel^)faceset)->FbsmFaces)
					{
						faceList->AddRange(fs->CfsFaces);
					}
					return faceList;
				}
				else if (dynamic_cast<IfcShellBasedSurfaceModel^>(faceset))
				{
					List<IfcFace^>^ faceList = gcnew List<IfcFace^>();
					for each (IfcConnectedFaceSet^ fs in ((IfcShellBasedSurfaceModel^)faceset)->SbsmBoundary)
					{
						faceList->AddRange(fs->CfsFaces);
					}
					return faceList;
				}
				else
					return gcnew List<IfcFace^>();
			}


			TopoDS_Shape* XbimFacetedShell::Handle::get()
			{
				Monitor::Enter(_faceSet); //stop two threads simultaineously building a mode
				try
				{
					if(nativeHandle==nullptr)
						//make a copy of the face set to avoid caching large objects
					{
						Build((IfcRepresentationItem^)_faceSet->ModelOf->Instances[Math::Abs(_faceSet->EntityLabel)]);
					    System::Diagnostics::Debug::Assert((shapes->Count > 0 && nativeHandle==nullptr) || (nativeHandle!=nullptr &&shapes->Count == 0 ) );
						nativeHandle =  XbimGeometryModelCollection::Handle;
					}
				}
				finally
				{
					Monitor::Exit(_faceSet);
				}
				return nativeHandle;	
			};		

			void XbimFacetedShell::Move(TopLoc_Location location)
			{	
				Build();	//force a build if required
				if(shapes->Count>0)
				{
					for each(XbimGeometryModel^ shape in shapes)
						shape->Move(location);
					delete nativeHandle;
					nativeHandle=nullptr;
					
				}
				else if(nativeHandle!=nullptr)
				{
					nativeHandle->Move(location);
				}
			}

			XbimGeometryModel^ XbimFacetedShell::CopyTo(IfcAxis2Placement^ placement)
			{
				//TopoDS_Shape copyShape = *Handle;	//force a build if required
				Build();
				XbimFacetedShell^ fs = gcnew XbimFacetedShell(isSolid, _faceSet,_hasCurvedEdges,_representationLabel,_surfaceStyleLabel);
				
				if(shapes->Count>0)
				{
					for each(XbimGeometryModel^ shape in shapes)
						fs->Add(shape->CopyTo(placement));
				}
				else if(nativeHandle !=nullptr)
				{
					*(fs->Handle) = *nativeHandle;
					fs->Handle->Move(XbimGeomPrim::ToLocation(placement));
				}
				return fs;
			}

			void XbimFacetedShell::Build( IfcFaceBasedSurfaceModel^ repItem)
			{
				if(repItem->FbsmFaces->Count == 0)
					Logger->WarnFormat("XbimFacetedShell: An IfcFaceBasedSurfaceModel #{0} with no face sets has been found, this is no compliant",repItem->EntityLabel); 
				else
				{
					shapes->Clear();
					if(nativeHandle!=nullptr) {delete nativeHandle;nativeHandle=nullptr;}
					for each(IfcConnectedFaceSet^ fbsm in repItem->FbsmFaces)
					{
						XbimFacetedShell^ fs = gcnew XbimFacetedShell(fbsm);
						fs->Build();		
						Add(fs);
					}	
					
				}
			}

			void XbimFacetedShell::Build( IfcShellBasedSurfaceModel^ repItem)
			{

				if(repItem->SbsmBoundary->Count == 0)
					Logger->WarnFormat("XbimFacetedShell: An IfcShellBasedSurfaceModel #{0} with no face sets has been found, this is no compliant",repItem->EntityLabel); 
				else
				{
					shapes->Clear();
					if(nativeHandle!=nullptr) {delete nativeHandle;nativeHandle=nullptr;}
					for each(IfcConnectedFaceSet^ sbsm in repItem->SbsmBoundary)
					{
						XbimFacetedShell^ fs = gcnew XbimFacetedShell(sbsm);
						fs->Build();		
						Add(fs);
					}
					
				}
			}

			void XbimFacetedShell::Build() 
			{
				Monitor::Enter(this);
				try
				{
					if(nativeHandle==nullptr && shapes->Count == 0) //if not built
					{
						Build(_faceSet);
					}
				}
				finally
				{
					Monitor::Exit(this);
				}
			};

			XbimPolyhedron^ XbimFacetedShell::ToPolyHedron(double deflection, double precision,double precisionMax)
			{
				ToSolid(precision,precisionMax);
				return XbimGeometryModelCollection::ToPolyHedron(deflection,  precision, precisionMax);
			}

			IXbimGeometryModelGroup^ XbimFacetedShell::ToPolyHedronCollection(double deflection, double precision,double precisionMax)
			{
				return XbimGeometryModelCollection::ToPolyHedronCollection(deflection,  precision, precisionMax);
			}

			void  XbimFacetedShell::ToSolid(double precision, double maxPrecision)
			{

				if(isSolid) return; //already done
				Build();//make sure we have built the object
				TopoDS_Compound solids;
				BRep_Builder builder;
				builder.MakeCompound(solids);
				ShapeFix_Solid solidFixer;
				solidFixer.SetMinTolerance(precision);
				solidFixer.SetMaxTolerance(maxPrecision);
				solidFixer.SetPrecision(precision);
				solidFixer.CreateOpenSolidMode()= Standard_True;
				if(shapes->Count>0) 
				{
					for each (XbimGeometryModel^ shape in shapes)
						builder.Add(solids,*(shape->Handle));
					BRepBuilderAPI_Sewing  sfs(precision);
					sfs.SetMinTolerance(precision);
					sfs.SetMaxTolerance(maxPrecision);
					sfs.SetFloatingEdgesMode(Standard_True);
					sfs.Add(solids);
					sfs.Perform();
					for (TopExp_Explorer shellEx(sfs.SewedShape(),TopAbs_SHELL);shellEx.More();shellEx.Next()) //get each shell and make it a solid
					{
						TopoDS_Solid solid = solidFixer.SolidFromShell(TopoDS::Shell(shellEx.Current()));
						
						if(!solid.IsNull())
						{
							GProp_GProps System;
							BRepGProp::VolumeProperties(solid, System, Standard_False);
							double vol =  System.Mass();
							if(vol<0) 
								solid.Reverse();
							else if(vol==0)
							{
								Logger->WarnFormat("Closed Shell #{0} contains a solid with zero volume. Ignored", _faceSet->EntityLabel);
								continue;
							}
							builder.Add(solids,solid);
						}
					}
				}
				else if(nativeHandle!=nullptr)
				{
					const TopLoc_Location& loc = nativeHandle->Location();
					for (TopExp_Explorer shellEx(*nativeHandle,TopAbs_SHELL);shellEx.More();shellEx.Next()) //get each shell and make it a solid
					{
						TopoDS_Solid solid = solidFixer.SolidFromShell(TopoDS::Shell(shellEx.Current()));
						if(!solid.IsNull())
						{
							GProp_GProps System;
							BRepGProp::VolumeProperties(solid, System, Standard_False);
							double vol =  System.Mass();
							if(vol<0) 
								solid.Reverse();
							else if(vol==0)
							{
								Logger->WarnFormat("Closed Shell #{0} contains a solid with zero volume. Ignored", _faceSet->EntityLabel);
								continue;
							}
							solid.Move(loc);
							builder.Add(solids,solid);
						}
					}
				}
				else
					Logger->Error("Error processing facetted shell, pointer should not be null or shapes must present.");
				if(nativeHandle==nullptr) nativeHandle = new TopoDS_Compound();
				if(BRepCheck_Analyzer(solids,Standard_True).IsValid() == Standard_False)
				{
					ShapeFix_Shape sfs(solids);
					sfs.SetPrecision(precision);
					sfs.SetMinTolerance(precision);
					sfs.SetMaxTolerance(maxPrecision);
					sfs.Perform();
					*nativeHandle = sfs.Shape();
				}
				else 
					*nativeHandle=solids;
				isSolid=true;
				
			}


			XbimMeshFragment XbimFacetedShell::MeshTo(IXbimMeshGeometry3D^ mesh3D, IfcProduct^ product, XbimMatrix3D transform, double deflection)
			{	
				IfcRepresentationItem^ faceSet = (IfcRepresentationItem^)_faceSet->ModelOf->Instances[_faceSet->EntityLabel];
				XbimTriangulatedModelCollection^ list = gcnew XbimTriangulatedModelCollection();
				if(dynamic_cast<IfcClosedShell^>(faceSet))
					list->Add(TriangulateFaceSet(((IfcClosedShell^)faceSet)->CfsFaces));
				else if(dynamic_cast<IfcOpenShell^>(faceSet))
					list->Add(TriangulateFaceSet(((IfcOpenShell^)faceSet)->CfsFaces));
				else if(dynamic_cast<IfcConnectedFaceSet^>(faceSet))
					list->Add(TriangulateFaceSet(((IfcConnectedFaceSet^)faceSet)->CfsFaces));
				else if(dynamic_cast<IfcFacetedBrep^>(faceSet))
					list->Add(TriangulateFaceSet(((IfcFacetedBrep^)faceSet)->Outer->CfsFaces));
				else if (dynamic_cast<IfcFaceBasedSurfaceModel^>(faceSet))
				{
					for each (IfcConnectedFaceSet^ fbsmFaces in ((IfcFaceBasedSurfaceModel^)faceSet)->FbsmFaces)
						list->Add(TriangulateFaceSet(fbsmFaces->CfsFaces));
				}
				else if (dynamic_cast<IfcShellBasedSurfaceModel^>(faceSet))
				{
					for each (IfcConnectedFaceSet^ sbsmFaces in ((IfcShellBasedSurfaceModel^)faceSet)->SbsmBoundary)
					{
						if(dynamic_cast<IfcClosedShell^>(sbsmFaces))
							list->Add(TriangulateFaceSet(((IfcClosedShell^)sbsmFaces)->CfsFaces));
						if(dynamic_cast<IfcOpenShell^>(sbsmFaces))
							list->Add(TriangulateFaceSet(((IfcOpenShell^)sbsmFaces)->CfsFaces));
					}
				}
				XbimMeshFragment fragment(mesh3D->PositionCount,mesh3D->TriangleIndexCount);
                fragment.EntityLabel = product->EntityLabel;
                fragment.EntityType = product->GetType();
				
				for each (XbimTriangulatedModel^ tm in list) //add each mesh to the collective mesh
				{
					XbimTriangulatedModelStream^ streamer = gcnew XbimTriangulatedModelStream(tm->Triangles);
					XbimMeshFragment f = streamer->BuildWithNormals<IXbimTriangulatesToPositionsNormalsIndices^>((IXbimTriangulatesToPositionsNormalsIndices^)mesh3D,transform);
				}

				fragment.EndPosition = mesh3D->PositionCount-1;
                fragment.EndTriangleIndex = mesh3D->TriangleIndexCount-1;
				mesh3D->Meshes->Add(fragment);
				return fragment;
				
			}


			XbimTriangulatedModelCollection^ XbimFacetedShell::Mesh(double deflection)	
			{
				
				//if we have a collection mesh them
				if(shapes->Count>0)
					return XbimGeometryModelCollection::Mesh(deflection);
				if(nativeHandle!=nullptr)
				{
					return XbimGeometryModel::Mesh(deflection);
				}
				else if(_faceSet!=nullptr)
				{

					//we take a copy of the faceset to avoid loading and retaining large meshes in memory
					//if we do not do this the model geometry object will retain all geometry data of the mesh until it is releases
					XbimTriangulatedModelCollection^ list = gcnew XbimTriangulatedModelCollection();
					IfcRepresentationItem^ faceSet = (IfcRepresentationItem^)_faceSet->ModelOf->Instances[_faceSet->EntityLabel];
					if(dynamic_cast<IfcClosedShell^>(faceSet))
						list->Add(TriangulateFaceSet(((IfcClosedShell^)faceSet)->CfsFaces));
					else if(dynamic_cast<IfcOpenShell^>(faceSet))
						list->Add(TriangulateFaceSet(((IfcOpenShell^)faceSet)->CfsFaces));
					else if(dynamic_cast<IfcConnectedFaceSet^>(faceSet))
						list->Add(TriangulateFaceSet(((IfcConnectedFaceSet^)faceSet)->CfsFaces));
					else if(dynamic_cast<IfcFacetedBrep^>(faceSet))
						list->Add(TriangulateFaceSet(((IfcFacetedBrep^)faceSet)->Outer->CfsFaces));
					else if (dynamic_cast<IfcFaceBasedSurfaceModel^>(faceSet))
					{
						for each (IfcConnectedFaceSet^ fbsmFaces in ((IfcFaceBasedSurfaceModel^)faceSet)->FbsmFaces)
							list->Add(TriangulateFaceSet(fbsmFaces->CfsFaces));
					}
					else if (dynamic_cast<IfcShellBasedSurfaceModel^>(faceSet))
					{
						for each (IfcConnectedFaceSet^ sbsmFaces in ((IfcShellBasedSurfaceModel^)faceSet)->SbsmBoundary)
						{
							if(dynamic_cast<IfcClosedShell^>(sbsmFaces))
								list->Add(TriangulateFaceSet(((IfcClosedShell^)sbsmFaces)->CfsFaces));
							if(dynamic_cast<IfcOpenShell^>(sbsmFaces))
								list->Add(TriangulateFaceSet(((IfcOpenShell^)sbsmFaces)->CfsFaces));
						}
					}
					bool first = true;
					//calc bounding box for the shape
					for each (XbimTriangulatedModel^ tm in list) 
					{
						if(first) {_bounds = tm->Bounds; first=false;}
						else _bounds.Union(tm->Bounds);
					}
					return list;
				}
				throw gcnew XbimGeometryException("Unhandled Faceted Shell geometry" );
			}

			XbimTriangulatedModel^ XbimFacetedShell::TriangulateFaceSet(IEnumerable<IfcFace^>^ faces)
			{
				XbimTriangularMeshStreamer tms (RepresentationLabel, SurfaceStyleLabel);
				double xmin = 0; double ymin = 0; double zmin = 0; double xmax = 0; double ymax = 0; double zmax = 0;
				bool first = true;
				GLdouble glPt3D[3];
				// OPENGL TESSELLATION
				//
				GLUtesselator *ActiveTss = gluNewTess();
				gluTessCallback(ActiveTss, GLU_TESS_VERTEX_DATA,  (GLUTessCallback) XMS_AddVertexIndex);
				gluTessCallback(ActiveTss, GLU_TESS_BEGIN_DATA,  (GLUTessCallback) XMS_BeginTessellate);
				gluTessCallback(ActiveTss, GLU_TESS_END_DATA,  (GLUTessCallback) XMS_EndTessellate);
				gluTessCallback(ActiveTss, GLU_TESS_ERROR,    (GLUTessCallback) XMS_TessellateError);
				
				// TesselateStream vertexData(pStream, points, faceCount, streamSize);
				for each (IfcFace^ fc in  faces)
				{
					{
						// IfcDirection^ normal = ((IFace^)fc)->Normal;
						IfcFaceBound^ outerBound = Enumerable::FirstOrDefault(Enumerable::OfType<IfcFaceOuterBound^>(fc->Bounds)); //get the outer bound
						if(outerBound == nullptr) outerBound = Enumerable::FirstOrDefault(fc->Bounds); //if one not defined explicitly use first found
						if(outerBound == nullptr || !dynamic_cast<IfcPolyLoop^>(outerBound->Bound)|| ((IfcPolyLoop^)(outerBound->Bound))->Polygon->Count<3) 
							continue; //invalid polygonal face
						XbimVector3D normal = PolyLoopExtensions::NewellsNormal((IfcPolyLoop^)(outerBound->Bound));
						//srl if an invalid normal is returned the face is not valid (sometimes a line or a point is defined) skip the face
						if(normal.IsInvalid()) 
							continue;
						tms.BeginFace((int)-1);
						tms.SetNormal(
							(float)normal.X, 
							(float)normal.Y, 
							(float)normal.Z
							);
					}
					gluTessBeginPolygon(ActiveTss, &tms);
					// go over each boundary
					for each (IfcFaceBound^ bound in fc->Bounds)
					{
						gluTessBeginContour(ActiveTss);
						IfcPolyLoop^ polyLoop=(IfcPolyLoop^)bound->Bound;
						if(polyLoop->Polygon->Count < 3) 
						{
							Logger->WarnFormat("Invalid bound #{0}, less than 3 points",bound->EntityLabel);
							continue;
						}

						IEnumerable<IfcCartesianPoint^>^ pts = polyLoop->Polygon;
						if(!bound->Orientation)
							pts = Enumerable::Reverse(pts);
						//add all the points into shell point map

						for each(IfcCartesianPoint^ p in pts)
						{
							XbimPoint3D p3D = p->XbimPoint3D();
							glPt3D[0] = p3D.X;
							glPt3D[1] = p3D.Y;
							glPt3D[2] = p3D.Z;
							size_t pIndex = tms.WritePoint((float)p3D.X, (float)p3D.Y, (float)p3D.Z);

							//get the bounding box as we go
							if (first)
							{
								xmin = glPt3D[0];
								ymin = glPt3D[1];
								zmin = glPt3D[2];
								xmax = glPt3D[0];
								ymax = glPt3D[1];
								zmax = glPt3D[2];
								first = false;
							}
							else
							{
								xmin = Math::Min(xmin,glPt3D[0]);
								ymin = Math::Min(ymin,glPt3D[1]);
								zmin = Math::Min(zmin, glPt3D[2]);
								xmax = Math::Max(xmax,glPt3D[0]);
								ymax = Math::Max(ymax,glPt3D[1]);
								zmax = Math::Max(zmax, glPt3D[2]);
							}
							gluTessVertex(ActiveTss, glPt3D, (void*)pIndex); 
						}
						gluTessEndContour(ActiveTss);
					}
					gluTessEndPolygon(ActiveTss);
					tms.EndFace();
				}
				gluDeleteTess(ActiveTss);

				// END OPENGL TESSELLATION

				size_t uiCalcSize = tms.StreamSize();
				IntPtr BonghiUnManMem = Marshal::AllocHGlobal((int)uiCalcSize);
				unsigned char* BonghiUnManMemBuf = (unsigned char*)BonghiUnManMem.ToPointer();
				size_t controlSize = tms.StreamTo(BonghiUnManMemBuf);
				array<unsigned char>^ BmanagedArray = gcnew array<unsigned char>((int)uiCalcSize);
				Marshal::Copy(BonghiUnManMem, BmanagedArray, 0, (int)uiCalcSize);
				Marshal::FreeHGlobal(BonghiUnManMem);

				XbimRect3D boundingBox((float)xmin, (float)ymin, (float)zmin, (float)(xmax-xmin),  (float)(ymax-ymin), (float)(zmax-zmin));
				return gcnew XbimTriangulatedModel(BmanagedArray, boundingBox, RepresentationLabel, SurfaceStyleLabel);
			}

#pragma unmanaged
			public class TriangleIndicesStream
			{
			private:
				size_t _previousToLastIndex;
				size_t _lastIndex;
				size_t _pointTally;
				size_t _fanStartIndex;
				size_t _indexOffset;
				GLenum _meshType;
			public:
				std::vector<size_t> Indices;
				static void __stdcall AddVertexIndex(void *pVertexData, void *pStreamer)
				{
					TriangleIndicesStream* This = ((TriangleIndicesStream*)pStreamer);
					size_t index = (size_t)pVertexData;
					if (This->_pointTally == 0)
						This->_fanStartIndex = index;
					if (This->_pointTally < 3) //first time
						This->Indices.push_back(index);
					else
					{
						switch (This->_meshType)
						{
						case GL_TRIANGLES://      0x0004
							This->Indices.push_back(index);
							break;
						case GL_TRIANGLE_STRIP:// 0x0005
							if (This->_pointTally % 2 == 0)
							{				
								This->Indices.push_back(This->_previousToLastIndex);
								This->Indices.push_back(This->_lastIndex);
								This->Indices.push_back(index);
							}
							else
							{
								This->Indices.push_back(This->_lastIndex);
								This->Indices.push_back(This->_previousToLastIndex);
								This->Indices.push_back(index);
							}
							break;
						case GL_TRIANGLE_FAN://   0x0006
							This->Indices.push_back(This->_fanStartIndex);
							This->Indices.push_back(This->_lastIndex);
							This->Indices.push_back(index);
							break;
						default:
							break;
						}
					}
					This->_previousToLastIndex = This->_lastIndex;
					This->_lastIndex = index;
					This->_pointTally++;
				};
				static void __stdcall BeginTessellate(GLenum type, void *pStreamer)
				{
					TriangleIndicesStream* This = ((TriangleIndicesStream*)pStreamer);
					This->_meshType = type;
					This->_pointTally = 0;
					This->_previousToLastIndex = 0;
					This->_lastIndex = 0;
					This->_fanStartIndex = 0;
					

				};
				static void __stdcall EndTessellate(void *pStreamer)
				{
					TriangleIndicesStream* This = ((TriangleIndicesStream*)pStreamer);
				};
				static void __stdcall TessellateError(GLenum err)
				{
					// swallow the error.
				};
			};

			
			
			


#pragma managed
			String^ XbimFacetedShell::WriteAsString(XbimModelFactors^ modelFactors)
			{
				IfcRepresentationItem^ faceSet = (IfcRepresentationItem^)_faceSet->ModelOf->Instances[_faceSet->EntityLabel];
				String^ result;
				if(dynamic_cast<IfcClosedShell^>(faceSet))
					result = WriteAsString(modelFactors, ((IfcClosedShell^)faceSet)->CfsFaces);
				else if(dynamic_cast<IfcOpenShell^>(faceSet))
					result = WriteAsString(modelFactors, ((IfcOpenShell^)faceSet)->CfsFaces);
				else if(dynamic_cast<IfcConnectedFaceSet^>(faceSet))
					result = WriteAsString(modelFactors, ((IfcConnectedFaceSet^)faceSet)->CfsFaces);
				else if(dynamic_cast<IfcFacetedBrep^>(faceSet))
					result = WriteAsString(modelFactors, ((IfcFacetedBrep^)faceSet)->Outer->CfsFaces);
				else if (dynamic_cast<IfcFaceBasedSurfaceModel^>(faceSet))
				{
					List<IfcFace^>^ allFaces = gcnew List<IfcFace^>();
					for each (IfcConnectedFaceSet^ fbsmFaces in ((IfcFaceBasedSurfaceModel^)faceSet)->FbsmFaces)
						allFaces->AddRange(fbsmFaces->CfsFaces);
					result = WriteAsString(modelFactors,allFaces);
				}
				else if (dynamic_cast<IfcShellBasedSurfaceModel^>(faceSet))
				{
					List<IfcFace^>^ allFaces = gcnew List<IfcFace^>();
					for each (IfcConnectedFaceSet^ sbsmFaces in ((IfcShellBasedSurfaceModel^)faceSet)->SbsmBoundary)
						allFaces->AddRange(sbsmFaces->CfsFaces);
					result = WriteAsString(modelFactors,allFaces);
				}
				else
					result = "";
				return result;
			}

			String^ XbimFacetedShell::WriteAsString(XbimModelFactors^ modelFactors, IEnumerable<IfcFace^>^ faces)
			{
				double deflection = modelFactors->DeflectionTolerance;
				double precision = modelFactors->Precision;
				int rounding =  modelFactors->Rounding;
				
				size_t normalsOffset=-1;
				StringBuilder^ sb = gcnew StringBuilder();
				double xmin = 0; double ymin = 0; double zmin = 0; double xmax = 0; double ymax = 0; double zmax = 0;
				bool first = true;
				GLdouble glPt3D[3];
				// OPENGL TESSELLATION
				GLUtesselator *ActiveTss = gluNewTess();
				gluTessCallback(ActiveTss, GLU_TESS_VERTEX_DATA,  (GLUTessCallback) TriangleIndicesStream::AddVertexIndex);
				gluTessCallback(ActiveTss, GLU_TESS_BEGIN_DATA,  (GLUTessCallback) TriangleIndicesStream::BeginTessellate);
				gluTessCallback(ActiveTss, GLU_TESS_END_DATA,  (GLUTessCallback) TriangleIndicesStream::EndTessellate);
				gluTessCallback(ActiveTss, GLU_TESS_ERROR,    (GLUTessCallback) TriangleIndicesStream::TessellateError);

				std::unordered_map<Float3D, size_t> vertexMap;

				for each (IfcFace^ fc in  faces)
				{
					bool vWritten = false;
					IfcFaceBound^ outerBound = Enumerable::FirstOrDefault(Enumerable::OfType<IfcFaceOuterBound^>(fc->Bounds)); //get the outer bound
					if(outerBound == nullptr) outerBound = Enumerable::FirstOrDefault(fc->Bounds); //if one not defined explicitly use first found
					if(outerBound == nullptr || !dynamic_cast<IfcPolyLoop^>(outerBound->Bound)|| ((IfcPolyLoop^)(outerBound->Bound))->Polygon->Count<3) 
						continue; //invalid polygonal face
					XbimVector3D n = PolyLoopExtensions::NewellsNormal((IfcPolyLoop^)(outerBound->Bound));
					//srl if an invalid normal is returned the face is not valid (sometimes a line or a point is defined) skip the face
					if(n.IsInvalid()) 
						continue;
					TriangleIndicesStream tms;
					gluTessBeginPolygon(ActiveTss, &tms);
					// go over each boundary
					for each (IfcFaceBound^ bound in fc->Bounds)
					{
						gluTessBeginContour(ActiveTss);
						IfcPolyLoop^ polyLoop=(IfcPolyLoop^)bound->Bound;
						if(polyLoop->Polygon->Count < 3) 
						{
							Logger->WarnFormat("Invalid bound #{0}, less than 3 points",bound->EntityLabel);
							continue;
						}

						IEnumerable<IfcCartesianPoint^>^ pts = polyLoop->Polygon;
						if(!bound->Orientation)
							pts = Enumerable::Reverse(pts);
						//add all the points into shell point map

						for each(IfcCartesianPoint^ p in pts)
						{
							size_t index;
							Float3D p3D((float)p->X,(float)p->Y,(float)p->Z,precision); 
							p3D.Round((float)rounding); //round the numbers to avpid numercic issues with precision
							std::unordered_map<Float3D, size_t>::const_iterator hit = vertexMap.find(p3D);
							if(hit==vertexMap.end()) //not found add it in
							{	
								index = vertexMap.size();
								vertexMap.insert(std::make_pair(p3D,index));
								//posMap.push_back(index);
								if(!vWritten)
								{
									sb->AppendFormat("V");
									vWritten=true;
								}
								sb->AppendFormat(" {0},{1},{2}",p3D.Dim1, p3D.Dim2, p3D.Dim3);	

							}
							else
								index = hit->second;
							//posMap.push_back(hit->second);
							glPt3D[0] = p3D.Dim1;
							glPt3D[1] = p3D.Dim2;
							glPt3D[2] = p3D.Dim3;

							//get the bounding box as we go
							if (first)
							{
								xmin = glPt3D[0];
								ymin = glPt3D[1];
								zmin = glPt3D[2];
								xmax = glPt3D[0];
								ymax = glPt3D[1];
								zmax = glPt3D[2];
								first = false;
							}
							else
							{
								xmin = Math::Min(xmin,glPt3D[0]);
								ymin = Math::Min(ymin,glPt3D[1]);
								zmin = Math::Min(zmin, glPt3D[2]);
								xmax = Math::Max(xmax,glPt3D[0]);
								ymax = Math::Max(ymax,glPt3D[1]);
								zmax = Math::Max(zmax, glPt3D[2]);
							}
							gluTessVertex(ActiveTss, glPt3D, (void*)index); 
						}
						gluTessEndContour(ActiveTss);
					}
					gluTessEndPolygon(ActiveTss);
					if(vWritten) sb->AppendLine();
					gp_Dir normal(n.X,n.Y,n.Z);
					String^ f = "$"; //the name of the face normal if it is a simple (LRUPFB)
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
						normalsOffset++;//we will have written out this number of normals
					}
					//Now write the face
					bool firstTime=true;
					sb->Append("T");
					for (std::vector<size_t>::iterator it = tms.Indices.begin(); it != tms.Indices.end(); it++) 
					{

						size_t x = *it; it++;
						size_t y = *it; it++;
						size_t z = *it;
						if(firstTime)
						{
							if(f=="$") //face name is undefined
								sb->AppendFormat(" {0}/{3},{1},{2}", x, y, z, normalsOffset);
							else
								sb->AppendFormat(" {0}/{3},{1},{2}", x, y, z, f);
							firstTime=false;
						}
						else
							sb->AppendFormat(" {0},{1},{2}", x, y, z);
					}	
					sb->AppendLine();
				}
				gluDeleteTess(ActiveTss);
				_bounds = XbimRect3D((float)xmin, (float)ymin, (float)zmin, (float)(xmax-xmin),  (float)(ymax-ymin), (float)(zmax-zmin));
				return sb->ToString();
			}
		}
	}
}
