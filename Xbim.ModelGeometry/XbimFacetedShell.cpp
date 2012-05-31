#include "StdAfx.h"
#include "XbimFacetedShell.h"

using namespace System::Collections::Generic;
using namespace Xbim::XbimExtensions;
using namespace System::Windows::Media::Media3D;
using namespace System::Linq;
class GLUtesselator {};

namespace Xbim
{
	namespace ModelGeometry
	{
		XbimFacetedShell::XbimFacetedShell(IfcConnectedFaceSet^ shell)
		{
			_faceSet=shell;
			_boundingBox = gcnew XbimBoundingBox();
		}
		XbimFacetedShell::XbimFacetedShell(IfcOpenShell^ shell)
		{
			_faceSet=shell;
			_boundingBox = gcnew XbimBoundingBox();
		}
		XbimFacetedShell::XbimFacetedShell(IfcClosedShell^ shell)
		{
			_faceSet=shell;
			_boundingBox = gcnew XbimBoundingBox();
		}

		XbimFacetedShell::XbimFacetedShell(IfcShell^ shell)
		{
			_boundingBox = gcnew XbimBoundingBox();
			if(dynamic_cast<IfcOpenShell^>(shell))
				_faceSet=(IfcOpenShell^)shell;
			else if(dynamic_cast<IfcClosedShell^>(shell))
				_faceSet=(IfcClosedShell^)shell;
			else
			{
				Type^ type = shell->GetType();
				throw gcnew Exception("Error buiding shell from type " + type->Name);
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

		XbimTriangulatedModelStream^ XbimFacetedShell::Mesh()
		{
			return Mesh(true, XbimGeometryModel::DefaultDeflection, Matrix3D::Identity);
		}

		XbimTriangulatedModelStream^ XbimFacetedShell::Mesh( bool withNormals )
		{
			return Mesh(withNormals, XbimGeometryModel::DefaultDeflection, Matrix3D::Identity);
		}
		
		XbimTriangulatedModelStream^ XbimFacetedShell::Mesh(bool withNormals, double deflection )
		{
			return Mesh(withNormals, deflection, Matrix3D::Identity);
		}


		// Uses opengl to mesh the profiles
		XbimTriangulatedModelStream^ XbimFacetedShell::Mesh(bool withNormals, double deflection, Matrix3D transform )
		{
			XbimTriangularMeshStreamer tms;
			// XbimTriangularMeshStreamer* m = &tms;

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
				IfcDirection^ normal = ((IFace^)fc)->Normal;
				if(normal==nullptr) break; //abandon face if the normal is invalid
				tms.BeginFace((int)-1);
				
				tms.SetNormal(
					(float)normal->X, 
					(float)normal->Y, 
					(float)normal->Z
					);
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
						glPt3D[0] = p3D.X;
						glPt3D[1] = p3D.Y;
						glPt3D[2] = p3D.Z;
						void * pIndex = (void *)tms.WritePoint((float)p3D.X, (float)p3D.Y, (float)p3D.Z);
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
			return gcnew XbimTriangulatedModelStream(BmanagedArray);			
		}
	}
}
