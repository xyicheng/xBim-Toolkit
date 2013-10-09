#include "StdAfx.h"
#include "XbimTriangularMeshStreamer.h"
#include "XbimSolid.h"
#include "XbimFacetedShell.h"

#include "XbimGeometryModel.h"
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
				gluTessCallback(ActiveTss, GLU_TESS_BEGIN_DATA,  (void (CALLBACK *)()) XMS_BeginTessellate);
				gluTessCallback(ActiveTss, GLU_TESS_END_DATA,  (void (CALLBACK *)()) XMS_EndTessellate);
				gluTessCallback(ActiveTss, GLU_TESS_ERROR,    (void (CALLBACK *)()) XMS_TessellateError);
				gluTessCallback(ActiveTss, GLU_TESS_VERTEX_DATA,  (void (CALLBACK *)()) XMS_AddVertexIndex);
				// TesselateStream vertexData(pStream, points, faceCount, streamSize);
				for each (IfcFace^ fc in  faces)
				{
					{
						// IfcDirection^ normal = ((IFace^)fc)->Normal;
						IVector3D^ normal = ((IFace^)fc)->Normal;

						//srl if an invalid normal is returned the face is not valid (sometimes a line or a point is defined) skip the face
						if(normal->IsInvalid()) 
							continue;
						tms.BeginFace((int)-1);
						tms.SetNormal(
							(float)normal->X, 
							(float)normal->Y, 
							(float)normal->Z
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
		}
	}
}
