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

		XbimTriangulatedModelStream^ XbimFacetedShell::Mesh(bool withNormals, double deflection, Matrix3D transform )
		{

			GLUtesselator *tess = gluNewTess();

			gluTessCallback(tess, GLU_TESS_BEGIN_DATA,  (void (CALLBACK *)()) BeginTessellate);
			gluTessCallback(tess, GLU_TESS_END_DATA,  (void (CALLBACK *)()) EndTessellate);
			gluTessCallback(tess, GLU_TESS_ERROR,    (void (CALLBACK *)()) TessellateError);

			//determine unique points
			Dictionary<Point3D, int>^ uniquePoints = gcnew Dictionary<Point3D, int>();
			Dictionary<Vector3D, int>^ uniqueNormals = gcnew Dictionary<Vector3D, int>();
			//	int pointCount = 0;
			unsigned short faceCount = _faceSet->CfsFaces->Count;
			int totalIndices = 0;
			for each ( IfcFace^ fc in  _faceSet->CfsFaces)
			{

				for each (IfcFaceBound^ faceBound in fc->Bounds)
				{
					//boundCount++;
					if(dynamic_cast<IfcPolyLoop^>(faceBound->Bound))
					{
						IfcPolyLoop^ polyLoop=(IfcPolyLoop^)faceBound->Bound;
						for each(IfcCartesianPoint^ pt in polyLoop->Polygon)
						{
							Point3D% p3 = pt->WPoint3D();
							int pPos;
							if(!uniquePoints->TryGetValue(p3, pPos))
							{
								pPos = (int)uniquePoints->Count;
								uniquePoints->Add(p3,pPos);
								_boundingBox->Add(p3);
							}
							totalIndices++;
						}
					}
					else
						System::Diagnostics::Debug::WriteLine(String::Format("XbimFacetedShell loops of type {0} are not implemented, Loop id = #{1}", faceBound->Bound->GetType()->ToString(), faceBound->Bound->EntityLabel));
				}
			}
			unsigned int vertexCount=uniquePoints->Count;
			int indexSize;
			if(vertexCount<=0xFF) //we will use byte for indices
			{
				gluTessCallback(tess, GLU_TESS_VERTEX_DATA,  (void (CALLBACK *)()) AddVertexByte);
				indexSize =sizeof(unsigned char) ;
			}
			else if(vertexCount<=0xFFFF) //use  unsigned short int for indices
			{
				gluTessCallback(tess, GLU_TESS_VERTEX_DATA,  (void (CALLBACK *)()) AddVertexShort);
				indexSize = sizeof(unsigned short); //use  unsigned short int for indices
			}
			else //use unsigned int for indices
			{
				gluTessCallback(tess, GLU_TESS_VERTEX_DATA,  (void (CALLBACK *)()) AddVertexInt);
				indexSize = sizeof(unsigned int); //use unsigned int for indices
			}

			GLdouble glPt3D[3];

			if(vertexCount==0) return XbimTriangulatedModelStream::Empty;
			int memSize =  sizeof(int) + (vertexCount * 3 *sizeof(double)); //number of points plus x,y,z of each point
			memSize += sizeof(unsigned int); //allow unsigned int for total number of faces
			memSize += faceCount * (sizeof(unsigned char)+(2*sizeof(unsigned short)) +sizeof(unsigned short)+ 3 * sizeof(double)); //allow space for the type of triangulation (1 byte plus number of indices - 2 bytes plus polygon count-2 bytes) + normal count + the normal
			memSize += (totalIndices*indexSize*2) ; //assume worst case each face is made only of triangles, Max number of indices + Triangle Mode=1byte per triangle
			
			IntPtr vertexPtr = Marshal::AllocHGlobal(memSize);
			try
			{	
				unsigned char* pointBuffer = (unsigned char*)vertexPtr.ToPointer();
				int position = 0;
				//write out number of points
				unsigned int * pPointCount = (unsigned int *)pointBuffer;
				*pPointCount = vertexCount;
				//move position on 
				position+=sizeof(unsigned int);
				if(TesselateStream::UseDouble)
				{
					for each(Point3D p in uniquePoints->Keys )
					{
						double* pCord = (double *)(pointBuffer + position);
						*pCord = p.X; position += sizeof(double);
						pCord = (double *)(pointBuffer + position);
						*pCord = p.Y; position += sizeof(double);
						pCord = (double *)(pointBuffer + position);
						*pCord = p.Z; position += sizeof(double);
					}
				}
				else
				{
					for each(Point3D p in uniquePoints->Keys )
					{
						float* pCord = (float *)(pointBuffer + position);
						*pCord = (float)p.X; position += sizeof(float);
						pCord = (float *)(pointBuffer + position);
						*pCord = (float)p.Y; position += sizeof(float);
						pCord = (float *)(pointBuffer + position);
						*pCord = (float)p.Z; position += sizeof(float);
					}
				}
				unsigned short * pFaceCount = (unsigned short *)(pointBuffer + position);
				*pFaceCount=faceCount;
				position+=sizeof(faceCount);
				TesselateStream vertexData(pointBuffer, memSize, position);
				for each ( IfcFace^ fc in  _faceSet->CfsFaces)
				{

					IfcDirection^ normal = ((IFace^)fc)->Normal;
					vertexData.BeginFace(gp_Dir(normal->X, normal->Y, normal->Z));
					gluTessBeginPolygon(tess, &vertexData);
					// go over each bound
					for each (IfcFaceBound^ bound in fc->Bounds)
					{
						gluTessBeginContour(tess);
						IfcPolyLoop^ polyLoop=(IfcPolyLoop^)bound->Bound;
						if(polyLoop->Polygon->Count < 3) 
						{
							System::Diagnostics::Debug::WriteLine(String::Format("XbimFacetedShell, illegal number of points in Bound {0}",bound->EntityLabel));
							continue;
						}
						//write a face

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
							void * pIndex = (void *) (uniquePoints[p3D]);
							gluTessVertex(tess, glPt3D, pIndex); 
						}
						gluTessEndContour(tess);
					}
					gluTessEndPolygon(tess);
					vertexData.EndFace();
				}
				gluDeleteTess(tess);
				long len = vertexData.Length();
				
				//System::Diagnostics::Debug::WriteLine(String::Format("MemSize={0}, Actual={1}, Diff={2}", memSize/2, len, (memSize/2) - len));
				array<unsigned char>^ managedArray = gcnew array<unsigned char>(len);
				Marshal::Copy(vertexPtr, managedArray, 0, len);
				return gcnew XbimTriangulatedModelStream(managedArray);
			}
			catch(Exception^ e)
			{
				System::Diagnostics::Debug::WriteLine(String::Format("XbimFacetedShell, General failure in Shell #{0}",_faceSet->EntityLabel));
				System::Diagnostics::Debug::WriteLine(e->Message);
				return XbimTriangulatedModelStream::Empty;		
			}
			finally
			{

				Marshal::FreeHGlobal(vertexPtr);
			}
		}
	}

}