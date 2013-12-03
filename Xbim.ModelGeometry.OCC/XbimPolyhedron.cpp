#include "StdAfx.h"
#include <fstream>
#include <stdlib.h>
#include <stdio.h>
#include <iomanip>
#include <istream>
#include "XbimPolyhedron.h"
#include "XbimTriangularMeshStreamer.h"
#include <carve/mesh.hpp>
#include <carve/csg.hpp>
#include <carve/input.hpp>
#include <carve/triangulator.hpp>
#include <carve/geom.hpp>
#include "CartesianTransform.h"

using namespace  System::Threading;
using System::Runtime::InteropServices::Marshal;

namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{


			XbimPolyhedronMeshStreamer::XbimPolyhedronMeshStreamer(double precision)
			{
				_precision=precision;
			}

			void XbimPolyhedronMeshStreamer::BeginPolygon(GLenum type)
			{
				_meshType = type;
				_pointTally=0;
				_previousToLastIndex=0;
				_lastIndex = 0;
				_fanStartIndex=0;
				_indices.clear();
			}

			void XbimPolyhedronMeshStreamer::EndPolygon()
			{
				size_t t = _indices.size();
				//build the mesh of triangles
				polyData.reserveFaces(t/3,3); //this many faces
				//std::cout<<"Index Count = "<<t<<"  Face Count = "<< t/3<<std::endl; 
				for (size_t i = 0; i < t; i+=3)  
				{
					const carve::geom3d::Vector & v0 =  polyData.getVertex(_indices[i]);

					const carve::geom3d::Vector & v1 =  polyData.getVertex(_indices[i+1]);
					const carve::geom3d::Vector & v2 =  polyData.getVertex(_indices[i+2]);
					/*std::cout<<v0.x<<"\t"<<v0.y<<"\t"<<v0.z<<std::endl;
					std::cout<<v1.x<<"\t"<<v1.y<<"\t"<<v1.z<<std::endl;
					std::cout<<v2.x<<"\t"<<v2.y<<"\t"<<v2.z<<std::endl;*/
					const carve::geom3d::Vector vq = v1-v0;
					const carve::geom3d::Vector vr = v2-v0;
					//double area= carve::geom::cross(vq , vr).length()/2;
					//calculate if the three points are colinear
					//std::cout<< "Area = " << area<<std::endl;
					double diffABx = v1.x-v0.x;
					double diffABy = v1.y-v0.y;
					double diffABz = v1.z-v0.z;

					double diffACx = v2.x-v0.x;
					double diffACy = v2.y-v0.y;
					double diffACz = v2.z-v0.z;

					double ratio =	diffABx==0?0:diffACx/diffABx;
					if(ratio==0) ratio=diffABy==0?0:diffACy/diffABy;
					if(ratio==0) ratio=diffABz==0?0:diffACz/diffABz;
					//throw away colinear triangles
					if(!(Math::Abs((diffABx*ratio) - diffACx) <=_precision && Math::Abs((diffABy*ratio) - diffACy) <=_precision && Math::Abs((diffABz*ratio)-diffACz)<=_precision))
					{
						polyData.addFace(_indices[i],_indices[i+1],_indices[i+2]);
						//std::cout<<"Accepted"<<std::endl;
					}
					/*else
					std::cout<<"Rejected"<<std::endl;*/
				}

			}
			carve::csg::CSG::meshset_t* XbimPolyhedronMeshStreamer::GetPolyhedron()
			{
				/*size_t fC = polyData.getFaceCount();
				size_t vC = polyData.getVertexCount();*/

				return polyData.createMesh(carve::input::Options());
			}

			void XbimPolyhedronMeshStreamer::WriteTriangleIndex(size_t idxPtr)
			{
				size_t index = idxPtr;
				if(_pointTally==0)
					_fanStartIndex=index;
				if(_pointTally  < 3) //first time
				{
					_indices.push_back(index);
				}
				else 
				{

					switch(_meshType)
					{

					case GL_TRIANGLES://      0x0004
						_indices.push_back(index);
						break;
					case GL_TRIANGLE_STRIP:// 0x0005
						if(_pointTally % 2 ==0)
						{
							_indices.push_back(_previousToLastIndex);
							_indices.push_back(_lastIndex);
						}
						else
						{
							_indices.push_back(_lastIndex);
							_indices.push_back(_previousToLastIndex);
						} 
						_indices.push_back(index);
						break;
					case GL_TRIANGLE_FAN://   0x0006

						_indices.push_back(_fanStartIndex);
						_indices.push_back(_lastIndex);
						_indices.push_back(index);
						break;

					}
				}
				_previousToLastIndex = _lastIndex;
				_lastIndex = index;
				_pointTally++;
			}

			size_t XbimPolyhedronMeshStreamer::WritePoint(IfcCartesianPoint^ p)
			{
				size_t index = polyData.getVertexCount();
				polyData.addVertex(carve::geom::VECTOR(p->X, p->Y, p->Z));
				return index;
			}
			carve::input::PolyhedronData * XbimPolyhedronMeshStreamer::GetPolyData()
			{
				return &polyData;
			}

			XbimPolyhedron::XbimPolyhedron(void)
			{
				_bounds=XbimRect3D::Empty;
			}

			//Constructs geometry based on PLY ascii data
			XbimPolyhedron::XbimPolyhedron(String^ plyData)
			{	
				StringReader^ sr = gcnew StringReader(plyData);
				String^ ply = sr->ReadLine(); 
				if(	String::Compare(ply,"PLY",true) == 0) // correct format
				{	
					String^ elementVertex = sr->ReadLine();
					String^ elementFace = sr->ReadLine();
					array<String^>^ toks = elementVertex->Split(' ');
					int numVertices = Int32::Parse(elementVertex); 
					int numFaces = Int32::Parse(elementFace); 
					carve::input::PolyhedronData polyData;
					polyData.reserveVertices(numVertices);
					for (int i = 0; i < numVertices; i++)
					{
						String^ ptText = sr->ReadLine(); 
						toks = ptText->Split(' ');
						polyData.addVertex(carve::geom::VECTOR(Double::Parse(toks[0]),Double::Parse(toks[1]),Double::Parse(toks[2])));
					}
					for (int i = 0; i < numFaces; i++)
					{
						String^ ptText = sr->ReadLine(); 
						toks = ptText->Split(' ');
						int numPointsInFace = Convert::ToInt32(toks[0]); 
						std::vector<int> vidx;
						for (int p = 1; p < numPointsInFace+1; p++)
						{
							vidx.push_back(Int32::Parse(toks[p]));
						}
						polyData.addFace(vidx.begin(),vidx.end());
					}
					carve::csg::CSG::meshset_t* mesh = polyData.createMesh(carve::input::Options());
					_meshSet=mesh;
				}

			}

			XbimPolyhedron::XbimPolyhedron(carve::csg::CSG::meshset_t* mesh, int representationLabel, int styleLabel)
			{
				_bounds=XbimRect3D::Empty;
				_meshSet = mesh;
				_representationLabel=representationLabel;
				_surfaceStyleLabel=styleLabel;

			}

			double XbimPolyhedron::Volume::get()  
			{
				if (_meshSet==nullptr) return 0.0;
				double vol = 0.0;
				meshset_t::face_iter begin = _meshSet->faceBegin();
				begin++;
				meshset_t::face_iter end = _meshSet->faceBegin();
				if(begin==end) //no faces
					return 0.0;
				vertex_t::vector_t origin = (*begin)->edge->vert->v;
				for (meshset_t::face_iter i = _meshSet->faceBegin(), e = _meshSet->faceEnd(); i != e; ++i) 
				{
					face_t *face = *i;
					edge_t *e1 = face->edge;
					for (edge_t *e2 = e1->next ;e2->next != e1; e2 = e2->next) {
						vol += carve::geom3d::tetrahedronVolume(e1->vert->v, e2->vert->v, e2->next->vert->v, origin);
					}
				}
				return vol;
			}

			void XbimPolyhedron::InstanceCleanup()
			{  
				IntPtr temp = System::Threading::Interlocked::Exchange(IntPtr(_meshSet), IntPtr(0));
				if(temp!=IntPtr(0))
				{
					delete _meshSet;
					_meshSet=nullptr;
					System::GC::SuppressFinalize(this);
				}
				XbimGeometryModel::InstanceCleanup();
			}

			void XbimPolyhedron::MakeCube(double x, double y, double z)
			{
				const carve::math::Matrix &t = carve::math::Matrix::SCALE(x,y,z);
				carve::input::PolyhedronData data;

				data.addVertex(t * carve::geom::VECTOR(+1.0, +1.0, +1.0));
				data.addVertex(t * carve::geom::VECTOR(-1.0, +1.0, +1.0));
				data.addVertex(t * carve::geom::VECTOR(-1.0, -1.0, +1.0));
				data.addVertex(t * carve::geom::VECTOR(+1.0, -1.0, +1.0));
				data.addVertex(t * carve::geom::VECTOR(+1.0, +1.0, -1.0));
				data.addVertex(t * carve::geom::VECTOR(-1.0, +1.0, -1.0));
				data.addVertex(t * carve::geom::VECTOR(-1.0, -1.0, -1.0));
				data.addVertex(t * carve::geom::VECTOR(+1.0, -1.0, -1.0));
				data.addFace(0, 1, 2, 3);
				data.addFace(7, 6, 5, 4);
				data.addFace(0, 4, 5, 1);
				data.addFace(1, 5, 6, 2);
				data.addFace(2, 6, 7, 3);
				data.addFace(3, 7, 4, 0);

				carve::csg::CSG::meshset_t* mesh = data.createMesh(carve::input::Options());
				DeletePolyhedron();
				_meshSet=mesh;
			}

			void XbimPolyhedron::DeletePolyhedron(void)
			{
				if(_meshSet!=nullptr)
					delete _meshSet;
				_meshSet=nullptr;
			}

			//Transforms the polyhedron by the specified matrix
			void XbimPolyhedron::Transform(XbimMatrix3D t)
			{
				carve::math::Matrix m(t.M11,t.M12,t.M13,t.M14,
					t.M21,t.M22,t.M23,t.M24,
					t.M31,t.M32,t.M33,t.M34,
					t.OffsetX,t.OffsetY,t.OffsetZ,t.M44);
				carve::math::matrix_transformation mt(m);
				_meshSet->transform(mt);
			}

			//returns true if the Polyhedron is not Valid or if it has a manifold shape that is empty
			bool XbimPolyhedron::IsEmpty::get()
			{
				return (_meshSet==nullptr || _meshSet->getAABB().isEmpty());

			}

			//Returns true if the Polyhedron has been built and is valid
			bool XbimPolyhedron::IsValid::get()
			{
				return _meshSet!=nullptr ;

			}
			XbimRect3D XbimPolyhedron::GetBoundingBox()
			{
				if(_meshSet==nullptr) return XbimRect3D::Empty;
				if(_bounds.IsEmpty)
				{
					carve::mesh::MeshSet<3>::aabb_t aabb =  _meshSet->getAABB();
					if(!aabb.isEmpty())
					{
						carve::geom::aabb<3>::vector_t min =  aabb.min();
						carve::geom::aabb<3>::vector_t max =  aabb.max();
						_bounds = XbimRect3D((float)min.x, (float)min.y, (float)min.z, (float)(max.x-min.x),  (float)(max.y-min.y), (float)(max.z-min.z));
					}
				}
				return _bounds;
			}
			void XbimPolyhedron::ToSolid(double precision, double maxPrecision) 
			{
				poly_t* poly = carve::polyhedronFromMesh(_meshSet,-1);
				delete poly;
				delete _meshSet;
				_meshSet = carve::meshFromPolyhedron(poly,-1);
			}

		/*	String^ XbimPolyhedron::AsPLY()
			{

				int vertexCount = _meshSet->vertex_storage.size();
				int faceCount = 0;
				for (meshset_t::face_iter i = _meshSet->faceBegin(), e = _meshSet->faceEnd(); i != e; ++i) faceCount++;
				MemoryStream^ ms = gcnew MemoryStream();
				BinaryWriter^ bw = gcnew BinaryWriter(ms);
				bw->Write(vertexCount);
				bw->Write(faceCount);
				for (std::vector<meshset_t::vertex_t>::const_iterator i = _meshSet->vertex_storage.begin(); i!=_meshSet->vertex_storage.end(); ++i)
				{
					vertex_t vt = *i;
					bw->Write(vt.v.x);
					bw->Write(vt.v.y);
					bw->Write(vt.v.z);
				}
				for (meshset_t::face_iter i = _meshSet->faceBegin(), e = _meshSet->faceEnd(); i != e; ++i) 
				{
					face_t *face = *i;
					int numVertices = face->nVertices();
					bw->Write(numVertices);

					if(numVertices>0)
					{
						edge_t *e1 = face->edge;
						
						bw->Write(carve::poly::ptrToIndex_fast(_meshSet->vertex_storage,e1->v1()));
						for (edge_t *e2 = e1 ;e2->next != e1; e2 = e2->next) 
						{
							size_t idx = carve::poly::ptrToIndex_fast(_meshSet->vertex_storage,e2->v2());
							bw->Write(idx);
						}
					}     
				}
				array<unsigned char>^ arr = ms->ToArray();
				int len = arr->Length;
				Console::WriteLine(len);
				return "";
			}*/

			
			String^ XbimPolyhedron::WriteAsString()
			{
				StringWriter^ sw = gcnew StringWriter();
				sw->WriteLine("PLY");
				int faceCount = 0;
				for (meshset_t::face_iter i = _meshSet->faceBegin(), e = _meshSet->faceEnd(); i != e; ++i) faceCount++;
				sw->WriteLine(Convert::ToString(_meshSet->vertex_storage.size()));
				sw->WriteLine(Convert::ToString(faceCount));
				for (std::vector<meshset_t::vertex_t>::const_iterator i = _meshSet->vertex_storage.begin(); i!=_meshSet->vertex_storage.end(); ++i)
				{
					vertex_t vt = *i;
					sw->WriteLine(String::Format("{0} {1} {2}",vt.v.x,vt.v.y,vt.v.z));
				}
				for (meshset_t::face_iter i = _meshSet->faceBegin(), e = _meshSet->faceEnd(); i != e; ++i) 
				{
					face_t *face = *i;
					int numVertices = face->nVertices();
					sw->Write(Convert::ToString(numVertices));

					if(numVertices>0)
					{
						edge_t *e1 = face->edge;
						sw->Write(" ");
						sw->Write(Convert::ToString(carve::poly::ptrToIndex_fast(_meshSet->vertex_storage,e1->v1())));
						for (edge_t *e2 = e1 ;e2->next != e1; e2 = e2->next) 
						{
							sw->Write(" ");
							size_t idx = carve::poly::ptrToIndex_fast(_meshSet->vertex_storage,e2->v2());
							sw->Write(Convert::ToString(idx));
						}
					}
					sw->WriteLine(); 
					std::vector<carve::geom::vector<2> > projectedVerts;
					face->getProjectedVertices(projectedVerts);
					std::vector<carve::triangulate::tri_idx> result;
					carve::triangulate::triangulate(projectedVerts,result,0.00001);
					sw->Write(Convert::ToString(result.size()));
					for (size_t i = 0; i < result.size(); i++)
					{
						sw->Write(String::Format("{0} {1} {2}",result[i].a,result[i].b,result[i].c));
					}
					sw->WriteLine(); 
				}
				return sw->ToString();

			}
			
			XbimPolyhedron^ XbimPolyhedron::ToPolyHedron(double deflection, double precision,double precisionMax)
			{
				return this;
			}

			IXbimGeometryModelGroup^ XbimPolyhedron::ToPolyHedronCollection(double deflection, double precision,double precisionMax)
			{
				return this;
			}

			XbimGeometryModel^ XbimPolyhedron::CopyTo(IfcAxis2Placement^ placement)
			{
				//if(dynamic_cast<IfcLocalPlacement^>(placement))
				//{
				//	
				//	XbimMatrix3D m3d = CartesianTransform::ConvertMatrix3D();
				//	meshset_t* copy = _meshSet->clone();
				//	XbimPolyhedron^ p = gcnew XbimPolyhedron(copy,_representationLabel,_surfaceStyleLabel);
				//	p->Transform(m3d);
				//	return p;
				//}
				//else
				throw(gcnew NotSupportedException("XbimPolyhedron::CopyTo only supports IfcLocalPlacement type"));

			}
			////Subtracts toCut from this, if it fails a Null Polyhedron is returned
			//XbimPolyhedron^ XbimPolyhedron::Subtract(XbimPolyhedron^ toCut, double precision)
			//{
			//	carve::csg::CSG::meshset_t*  polyhedron;
			//	//Monitor::Enter(resourceLock);
			//	try
			//	{
			//		
			//		carve::csg::CSG csg(precision);
			//		polyhedron =  csg.compute(_meshSet,toCut->_meshSet,csg.A_MINUS_B);
			//	}
			//	catch (carve::exception e) 
			//	{
			//		String^ err = gcnew String(e.str().c_str());
			//		Logger->WarnFormat("XbimPolyhedron::Subtract failed, exception: {0}",  err);
			//	}
			//	return gcnew XbimPolyhedron(polyhedron,RepresentationLabel, SurfaceStyleLabel);
			//}

			//XbimPolyhedron^ XbimPolyhedron::Intersect(XbimPolyhedron^ toIntersect, double precision)
			//{
			//	carve::csg::CSG::meshset_t*  polyhedron;
			//	try
			//	{
			//		carve::csg::CSG csg(precision);
			//		polyhedron = csg.compute(_meshSet,toIntersect->_meshSet,csg.INTERSECTION);
			//	}
			//	catch (carve::exception e) 
			//	{
			//		String^ err = gcnew String(e.str().c_str());
			//		Logger->WarnFormat("XbimPolyhedron::Intersect failed, exception: {0}",  err);
			//	}
			//	return gcnew XbimPolyhedron(polyhedron,RepresentationLabel, SurfaceStyleLabel);
			//}

			//XbimPolyhedron^ XbimPolyhedron::Union(XbimPolyhedron^ toUnion, double precision)
			//{
			//	carve::csg::CSG::meshset_t*  polyhedron;
			//	try
			//	{
			//		carve::csg::CSG csg(precision);
			//		polyhedron = csg.compute(_meshSet,toUnion->_meshSet,csg.UNION);
			//	}
			//	catch (carve::exception e) 
			//	{
			//		String^ err = gcnew String(e.str().c_str());
			//		Logger->WarnFormat("XbimPolyhedron::Union failed, exception: {0}",  err);
			//	}
			//	return gcnew XbimPolyhedron(polyhedron,RepresentationLabel, SurfaceStyleLabel);
			//}

			bool  XbimPolyhedron::Intersects(XbimPolyhedron^ poly)
			{
				if(_meshSet==nullptr || poly->MeshSet==nullptr) return false;
				return _meshSet->getAABB().intersects(poly->MeshSet->getAABB());
			}

			XbimMeshFragment XbimPolyhedron::MeshTo(IXbimMeshGeometry3D^ mesh3D, IfcProduct^ product, XbimMatrix3D transform, double deflection)
			{
				XbimTriangulatedModelCollection^ triangles = Mesh(deflection);
				XbimMeshFragment fragment(mesh3D->PositionCount,mesh3D->TriangleIndexCount);
                fragment.EntityLabel = product->EntityLabel;
                fragment.EntityType = product->GetType();
				
				for each (XbimTriangulatedModel^ tm in triangles) //add each mesh to the collective mesh
				{
					XbimTriangulatedModelStream^ streamer = gcnew XbimTriangulatedModelStream(tm->Triangles);
					XbimMeshFragment f = streamer->BuildWithNormals<IXbimTriangulatesToPositionsNormalsIndices^>((IXbimTriangulatesToPositionsNormalsIndices^)mesh3D,transform);
				}

				fragment.EndPosition = mesh3D->PositionCount-1;
                fragment.EndTriangleIndex = mesh3D->TriangleIndexCount-1;
				mesh3D->Meshes->Add(fragment);
				return fragment;
			}


			XbimTriangulatedModelCollection^ XbimPolyhedron::Mesh(double deflection)
			{
				XbimTriangularMeshStreamer tms (RepresentationLabel, SurfaceStyleLabel);
				for (size_t ims = 0; ims < _meshSet->meshes.size(); ++ims) //go over each mesh in the mesh set
				{
					carve::mesh::Mesh<3>* mesh = _meshSet->meshes[ims]; //get a mesh

					for (size_t im = 0, lm = mesh->faces.size(); im != lm; ++im) //go over each face in the mesh
					{
						carve::mesh::Face<3> *f = mesh->faces[im];

						if (f->nVertices() == 3) //it's a triangle
						{

							std::vector<carve::mesh::Vertex<3> *> v1;
							f->getVertices(v1); //get the vertices
							tms.BeginFace(-1);//begin a face
							tms.SetNormal((float) f->plane.N.x,(float) f->plane.N.y,(float) f->plane.N.z);
							std::vector<size_t> indices;
							for (int ip = 0; ip < 3; ip++)
							{
								size_t pt = tms.WritePoint((float)v1[ip]->v.x, (float)v1[ip]->v.y, (float)v1[ip]->v.z);
								indices.push_back(pt);
							}	

							tms.BeginPolygon(GL_TRIANGLES);
							for (size_t j = 0; j < 3; ++j) 
							{
								tms.WriteTriangleIndex(indices[j]);		
							}						
							tms.EndPolygon();
							tms.EndFace();//end face
						} 
						else //otherwise we need to triangulate
						{
							if(f->plane.N.isZero(1e-5)) continue; //if the normal is invalid skip face
							// OPENGL TESSELLATION
							GLUtesselator *ActiveTss = gluNewTess();
							gluTessCallback(ActiveTss, GLU_TESS_BEGIN_DATA,  (GLUTessCallback) XMS_BeginTessellate);
							gluTessCallback(ActiveTss, GLU_TESS_END_DATA,  (GLUTessCallback) XMS_EndTessellate);
							gluTessCallback(ActiveTss, GLU_TESS_ERROR,    (GLUTessCallback)XMS_TessellateError);
							gluTessCallback(ActiveTss, GLU_TESS_VERTEX_DATA,  (GLUTessCallback) XMS_AddVertexIndex);

							tms.BeginFace(-1);//begin a face
							tms.SetNormal((float)f->plane.N.x,(float)f->plane.N.y,(float)f->plane.N.z);

							gluTessBeginPolygon(ActiveTss, &tms);
							gluTessBeginContour(ActiveTss);
							carve::mesh::Edge<3> *e = f->edge;

							for (size_t ie = 0, le = f->nVertices(); ie != le; ++ie)
							{
								carve::geom::vector<3U> v =  e->v1()->v;
								size_t uniquePos = tms.WritePoint((float)v.x, (float)v.y,(float)v.z);	
								GLdouble glPt3D[3];
								glPt3D[0] = e->v1()->v.x;glPt3D[1] = e->v1()->v.y;glPt3D[2] = e->v1()->v.z;
								gluTessVertex(ActiveTss, glPt3D, (void*)uniquePos); 
								e = e->next;
							}
							gluTessEndContour(ActiveTss);
							gluTessEndPolygon(ActiveTss);
							gluDeleteTess(ActiveTss);
							tms.EndFace();//end face
						}
					}
				}
				size_t uiCalcSize = tms.StreamSize();
				IntPtr BonghiUnManMem = Marshal::AllocHGlobal((int)uiCalcSize);
				unsigned char* BonghiUnManMemBuf = (unsigned char*)BonghiUnManMem.ToPointer();
				size_t controlSize = tms.StreamTo(BonghiUnManMemBuf);
				array<unsigned char>^ BmanagedArray = gcnew array<unsigned char>((int)uiCalcSize);
				Marshal::Copy(BonghiUnManMem, BmanagedArray, 0, (int)uiCalcSize);
				Marshal::FreeHGlobal(BonghiUnManMem);

				XbimTriangulatedModelCollection^ list = gcnew XbimTriangulatedModelCollection();
				list->Add(gcnew XbimTriangulatedModel(BmanagedArray,GetBoundingBox(),RepresentationLabel, SurfaceStyleLabel));
				return list;
			}

			XbimGeometryModel^ XbimPolyhedron::Cut(XbimGeometryModel^ shape, double precision, double maxPrecision)
			{
				Logger->Error("Mixed polyhedron and occ not supported. Failed to form difference between two shapes");
				return nullptr;
			}
			XbimGeometryModel^ XbimPolyhedron::Union(XbimGeometryModel^ shape, double precision, double maxPrecision)
			{				
				Logger->Error("Mixed polyhedron and occ not supported. Failed to form union between two shapes");
				return nullptr;
			}

			XbimGeometryModel^ XbimPolyhedron::Intersection(XbimGeometryModel^ shape, double precision, double maxPrecision)
			{		
				Logger->Error("Mixed polyhedron and occ not supported. Failed to form Intersection between two shapes");
				return nullptr;
			}
		}

	}
}