#include "StdAfx.h"
#include "XbimGeometryModel.h"
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
#include <BRepBuilderAPI.hxx>
using namespace Xbim::IO;
using namespace Xbim::Ifc2x3::ProductExtension;
using namespace Xbim::Ifc2x3::SharedComponentElements;
using namespace System::Linq;
using namespace Xbim::Ifc2x3::PresentationAppearanceResource;
using namespace Xbim::Common::Exceptions;
using namespace  System::Threading;
class Message_ProgressIndicator {};

namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
void CALLBACK XMS_BeginTessellate(GLenum type, void *pPolygonData)
{
	((XbimTriangularMeshStreamer*)pPolygonData)->BeginPolygon(type);
};
void CALLBACK XMS_EndTessellate(void *pVertexData)
{
	((XbimTriangularMeshStreamer*)pVertexData)->EndPolygon();
};
void CALLBACK XMS_TessellateError(GLenum err)
{
	// swallow the error.
};
void CALLBACK XMS_AddVertexIndex(void *pVertexData, void *pPolygonData)
{
				((XbimTriangularMeshStreamer*)pPolygonData)->WriteTriangleIndex((size_t)pVertexData);
};
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



		XbimGeometryModel^ XbimGeometryModel::Fix(XbimGeometryModel^ shape)
		{


			ShapeUpgrade_ShellSewing ss;
			TopoDS_Shape res = ss.ApplySewing(*(shape->Handle), BRepBuilderAPI::Precision()*10);
			if(res.IsNull())
			{
				Logger->Warn("Failed to fix shape, an empty solid has been found");
				return nullptr;
			}
			if(res.ShapeType() == TopAbs_COMPOUND)
			{
				BRep_Builder b;
				TopoDS_Shell shell;
				b.MakeShell(shell);
				for(TopExp_Explorer fExp(res, TopAbs_FACE); fExp.More(); fExp.Next())
				{
					b.Add(shell, TopoDS::Face(fExp.Current()));
				}

				ShapeFix_Shell shellFix(shell);
				shellFix.Perform();
				ShapeFix_Solid sfs;
				return  gcnew XbimSolid(sfs.SolidFromShell(shellFix.Shell()));				
			}
			else if(res.ShapeType() == TopAbs_SHELL) //make shells into solids
			{
				ShapeFix_Shell shellFix(TopoDS::Shell(res));
				shellFix.Perform();
				ShapeFix_Solid sfs;
				return gcnew XbimSolid(sfs.SolidFromShell(shellFix.Shell()));				
			}
			else if(res.ShapeType() == TopAbs_SOLID)
				return gcnew XbimSolid(TopoDS::Solid(res));
			else if(res.ShapeType() == TopAbs_COMPSOLID)
				Logger->Warn("Failed to fix shape, Compound Solids not supported");
			return nullptr;
		}

#pragma unmanaged

		
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

			gluTessCallback(ActiveTss, GLU_TESS_BEGIN_DATA,  (void (CALLBACK *)()) XMS_BeginTessellate);
			gluTessCallback(ActiveTss, GLU_TESS_END_DATA,  (void (CALLBACK *)()) XMS_EndTessellate);
			gluTessCallback(ActiveTss, GLU_TESS_ERROR,    (void (CALLBACK *)()) XMS_TessellateError);
			gluTessCallback(ActiveTss, GLU_TESS_VERTEX_DATA,  (void (CALLBACK *)()) XMS_AddVertexIndex);

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
		List<XbimTriangulatedModel^>^XbimGeometryModel::Mesh()
		{
			return Mesh(true, XbimGeometryModel::DefaultDeflection);
		}

		List<XbimTriangulatedModel^>^XbimGeometryModel::Mesh( bool withNormals )
		{
			return Mesh(withNormals, XbimGeometryModel::DefaultDeflection);
		}
		List<XbimTriangulatedModel^>^XbimGeometryModel::Mesh(bool withNormals, double deflection )
		{	
//Build the Mesh
			try
			{
				
				
				//BRepMesh_IncrementalMesh::SetParallelDefault(Standard_True);
				
				bool hasCurvedEdges = HasCurvedEdges;
				
				// transformed shape is the shape placed according to the transform matrix
				
				try
				{
					XbimTriangularMeshStreamer value(RepresentationLabel, SurfaceStyleLabel);
					XbimTriangularMeshStreamer* m = &value;
					//decide which meshing algorithm to use, Opencascade is slow but necessary to resolve curved edges
					TopoDS_Shape shape = *(Handle);
					if (hasCurvedEdges) 
					{	

						try
						{
							Monitor::Enter(resourceLock);
							/*bool done =  BRepTools::Triangulation(shape,4);
							if(!done)
							{*/
								BRepMesh_IncrementalMesh incrementalMesh(shape, deflection);
							//}
							OpenCascadeShapeStreamerFeed(shape, m);
						}
						finally
						{
							Monitor::Exit(resourceLock);
						}

					}
					else
						OpenGLShapeStreamerFeed(shape, m);

					size_t uiCalcSize = m->StreamSize();

					IntPtr BonghiUnManMem = Marshal::AllocHGlobal((int)uiCalcSize);
					unsigned char* BonghiUnManMemBuf = (unsigned char*)BonghiUnManMem.ToPointer();
					size_t controlSize = m->StreamTo(BonghiUnManMemBuf);

					if (uiCalcSize != controlSize)
					{
						int iError = 0;
						iError++;
					}

					array<unsigned char>^ BmanagedArray = gcnew array<unsigned char>((int)uiCalcSize);
					Marshal::Copy(BonghiUnManMem, BmanagedArray, 0, (int)uiCalcSize);
					Marshal::FreeHGlobal(BonghiUnManMem);
					List<XbimTriangulatedModel^>^list = gcnew List<XbimTriangulatedModel^>();
					list->Add(gcnew XbimTriangulatedModel(BmanagedArray, RepresentationLabel, SurfaceStyleLabel) );
					return list;
				}
				catch(...)
				{
					System::Diagnostics::Debug::WriteLine("Error processing geometry in XbimGeometryModel::Mesh");
				}
				finally
				{
					// Marshal::FreeHGlobal(vertexPtr);

				}
				
			}
			catch(...)
			{
				System::Diagnostics::Debug::WriteLine("Failed to Triangulate shape");
				
			}
			return gcnew List<XbimTriangulatedModel^>();
		};

		XbimRect3D XbimGeometryModel::GetBoundingBox()
		{
			Bnd_Box pBox;
			BRepBndLib::Add(*(this->Handle), pBox);
			Standard_Real srXmin, srYmin, srZmin, srXmax, srYmax, srZmax;
			if(pBox.IsVoid()) return XbimRect3D::Empty;
			pBox.Get(srXmin, srYmin, srZmin, srXmax, srYmax, srZmax);
			return XbimRect3D((float)srXmin, (float)srYmin, (float)srZmin, (float)(srXmax-srXmin),  (float)(srYmax-srYmin), (float)(srZmax-srZmin));
		};

		bool XbimGeometryModel::Intersects(XbimGeometryModel^ other)
		{
			Bnd_Box aBox;
			BRepBndLib::Add(*(this->Handle), aBox);
			Bnd_Box bBox;
			BRepBndLib::Add(*(other->Handle), bBox);
			return aBox.IsOut(bBox)==Standard_True;
		}

		bool XbimGeometryModel::IsMap::get() 
		{
			return dynamic_cast<XbimMap^>(this)!=nullptr;
		};
		
		
	}
}
}
