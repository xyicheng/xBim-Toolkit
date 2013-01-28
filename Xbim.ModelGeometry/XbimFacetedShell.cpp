#include "StdAfx.h"
#include "XbimFacetedShell.h"
#include "XbimTriangularMeshStreamer.h"
using namespace System::Collections::Generic;
using namespace Xbim::XbimExtensions;
using namespace System::Windows::Media::Media3D;
using namespace System::Linq;
using namespace Xbim::Common::Exceptions;
class GLUtesselator {};

namespace Xbim
{
	namespace ModelGeometry
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

		


		IXbimGeometryModel^ XbimFacetedShell::Cut(IXbimGeometryModel^ shape)
		{
			throw gcnew NotImplementedException("Cut needs to be implemented");
		}

		IXbimGeometryModel^ XbimFacetedShell::Union(IXbimGeometryModel^ shape)
		{
			throw gcnew NotImplementedException("Union needs to be implemented");
		}

		IXbimGeometryModel^ XbimFacetedShell::Intersection(IXbimGeometryModel^ shape)
		{
			throw gcnew NotImplementedException("Intersection needs to be implemented");
		}

		IXbimGeometryModel^ XbimFacetedShell::CopyTo(IfcObjectPlacement^ placement)
		{
			throw gcnew NotImplementedException("CopyTo needs to be implemented");
		}
			
		void XbimFacetedShell::Move(TopLoc_Location location)
		{
			throw gcnew NotImplementedException("Move needs to be implemented");
		}

		List<XbimTriangulatedModel^>^XbimFacetedShell::Mesh()
		{
			return Mesh(true, XbimGeometryModel::DefaultDeflection, Matrix3D::Identity);
		}

		List<XbimTriangulatedModel^>^XbimFacetedShell::Mesh( bool withNormals )
		{
			return Mesh(withNormals, XbimGeometryModel::DefaultDeflection, Matrix3D::Identity);
		}
		
		List<XbimTriangulatedModel^>^XbimFacetedShell::Mesh(bool withNormals, double deflection )
		{
			return Mesh(withNormals, deflection, Matrix3D::Identity);
		}

		List<XbimTriangulatedModel^>^XbimFacetedShell::Mesh(bool withNormals, double deflection, Matrix3D transform )
		{
			
			bool doTransform = (transform!=Matrix3D::Identity);
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

			GLdouble glPt3D[3];
			// TesselateStream vertexData(pStream, points, faceCount, streamSize);
			for each (IfcFace^ fc in  _faceSet->CfsFaces)
			{
				{
					// IfcDirection^ normal = ((IFace^)fc)->Normal;
					IVector3D^ normal = ((IFace^)fc)->Normal;

					//srl if an invalid normal is returned the face is not valid (sometimes a line or a point is defined) skip the face
					if(normal->IsInvalid()) 
						break;
					tms.BeginFace((int)-1);

					if(doTransform ) 
					{
						Vector3D v(normal->X, normal->Y, normal->Z);
						v = transform.Transform(v);
						tms.SetNormal(
							(float)v.X, 
							(float)v.Y, 
							(float)v.Z
							);
					}
					else
					{
						tms.SetNormal(
							(float)normal->X, 
							(float)normal->Y, 
							(float)normal->Z
							);
					}
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
						Point3D p3D = p->WPoint3D();
						if(doTransform ) p3D = transform.Transform(p3D);
						glPt3D[0] = p3D.X;
						glPt3D[1] = p3D.Y;
						glPt3D[2] = p3D.Z;
						void * pIndex = (void *)tms.WritePoint((float)p3D.X, (float)p3D.Y, (float)p3D.Z);
						
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
						gluTessVertex(ActiveTss, glPt3D, pIndex); 
					}
					gluTessEndContour(ActiveTss);
				}
				gluTessEndPolygon(ActiveTss);
				tms.EndFace();
			}
			gluDeleteTess(ActiveTss);

			// END OPENGL TESSELLATION

			unsigned int uiCalcSize = tms.StreamSize();
			IntPtr BonghiUnManMem = Marshal::AllocHGlobal(uiCalcSize);
			unsigned char* BonghiUnManMemBuf = (unsigned char*)BonghiUnManMem.ToPointer();
			unsigned int controlSize = tms.StreamTo(BonghiUnManMemBuf);

			if (uiCalcSize != controlSize)
			{
				int iError = 0;
				iError++;
			}

			array<unsigned char>^ BmanagedArray = gcnew array<unsigned char>(uiCalcSize);
			Marshal::Copy(BonghiUnManMem, BmanagedArray, 0, uiCalcSize);
			Marshal::FreeHGlobal(BonghiUnManMem);
			List<XbimTriangulatedModel^>^list = gcnew List<XbimTriangulatedModel^>();
			list->Add(gcnew XbimTriangulatedModel(BmanagedArray,this->RepresentationLabel, this->SurfaceStyleLabel));
			//set bounding box
			_boundingBox = gcnew XbimBoundingBox(xmin,ymin,zmin,xmax,ymax,zmax);
			return list;
			
		}

	}

}
