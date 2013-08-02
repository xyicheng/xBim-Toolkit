#include "StdAfx.h"
#include "XbimFacetedShell.h"
#include "XbimTriangularMeshStreamer.h"
using namespace System::Collections::Generic;
using namespace Xbim::XbimExtensions;
using namespace System::Linq;
using namespace Xbim::Common::Exceptions;
using namespace Xbim::Common::Geometry;
class GLUtesselator {};

namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
		XbimFacetedShell::XbimFacetedShell(IfcConnectedFaceSet^ shell)
		{
			_faceSet=shell;
			
		}
		XbimFacetedShell::XbimFacetedShell(IfcOpenShell^ shell)
		{
			_faceSet=shell;
			
		}
		XbimFacetedShell::XbimFacetedShell(IfcClosedShell^ shell)
		{
			_faceSet=shell;
			
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

		


		XbimGeometryModel^ XbimFacetedShell::Cut(XbimGeometryModel^ shape)
		{
			throw gcnew NotImplementedException("Cut needs to be implemented");
		}

		XbimGeometryModel^ XbimFacetedShell::Union(XbimGeometryModel^ shape)
		{
			throw gcnew NotImplementedException("Union needs to be implemented");
		}

		XbimGeometryModel^ XbimFacetedShell::Intersection(XbimGeometryModel^ shape)
		{
			throw gcnew NotImplementedException("Intersection needs to be implemented");
		}

		XbimGeometryModel^ XbimFacetedShell::CopyTo(IfcObjectPlacement^ placement)
		{
			throw gcnew NotImplementedException("CopyTo needs to be implemented");
		}
			
		void XbimFacetedShell::Move(TopLoc_Location location)
		{
			throw gcnew NotImplementedException("Move needs to be implemented");
		}

	

		List<XbimTriangulatedModel^>^XbimFacetedShell::Mesh(bool withNormals, double deflection)
		{
			
			XbimTriangularMeshStreamer tms (this->RepresentationLabel, this->SurfaceStyleLabel);
			// XbimTriangularMeshStreamer* m = &tms;
			double xmin = 0; double ymin = 0; double zmin = 0; double xmax = 0; double ymax = 0; double zmax = 0;
			bool first = true;
			// OPENGL TESSELLATION
			//
			GLUtesselator *ActiveTss = gluNewTess();
			gluTessCallback(ActiveTss, GLU_TESS_BEGIN_DATA,  (void (CALLBACK *)()) XMS_BeginTessellate);
			gluTessCallback(ActiveTss, GLU_TESS_END_DATA,  (void (CALLBACK *)()) XMS_EndTessellate);
			gluTessCallback(ActiveTss, GLU_TESS_ERROR,    (void (CALLBACK *)()) XMS_TessellateError);
			gluTessCallback(ActiveTss, GLU_TESS_VERTEX_DATA,  (void (CALLBACK *)()) XMS_AddVertexIndex);
				//we take a copy of the faceset to avoid loading and retaining large meshes in memory
				//if we do not do this the model geometry object will retain all geometry data of the mesh until it is releases
			IfcConnectedFaceSet^ faceset = (IfcConnectedFaceSet^)_faceSet->ModelOf->Instances[_faceSet->EntityLabel];
			GLdouble glPt3D[3];
			// TesselateStream vertexData(pStream, points, faceCount, streamSize);
				for each (IfcFace^ fc in  faceset->CfsFaces)
			{
				{
					// IfcDirection^ normal = ((IFace^)fc)->Normal;
					IVector3D^ normal = ((IFace^)fc)->Normal;

					//srl if an invalid normal is returned the face is not valid (sometimes a line or a point is defined) skip the face
					if(normal->IsInvalid()) 
						break;
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
						System::Diagnostics::Debug::WriteLine(String::Format("XbimFacetedShell, illegal number of points in Bound {0}",bound->EntityLabel));
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
			List<XbimTriangulatedModel^>^list = gcnew List<XbimTriangulatedModel^>();
			_boundingBox = XbimRect3D((float)xmin, (float)ymin, (float)zmin, (float)(xmax-xmin),  (float)(ymax-ymin), (float)(zmax-zmin));
			list->Add(gcnew XbimTriangulatedModel(BmanagedArray,this->RepresentationLabel, this->SurfaceStyleLabel));
			//set bounding box
			

			return list;
			}
		}
	}
}
