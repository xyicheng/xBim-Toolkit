#include "StdAfx.h"
#include <fstream>
#include <stdlib.h>
#include <stdio.h>
#include <iomanip>
#include <istream>
#include <algorithm>
#include "XbimPolyhedron.h"
#include "XbimCsg.h"

#include <carve/mesh.hpp>
#include <carve/csg.hpp>
#include <carve/input.hpp>
#include <carve/triangulator.hpp>
#include <carve/geom.hpp>
#include <carve/mesh_simplify.hpp>
#include "CartesianTransform.h"
#include "XbimGeometryModelCollection.h"

using namespace  System::Threading;

using namespace  System::Text;
using namespace Xbim::IO;
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

				return polyData.createMesh(carve::input::Options(),0.0);
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

			size_t XbimPolyhedron::Improve(
								double min_colinearity,
								double min_delta_v,
								double min_normal_angle,
								double EPSILON)
			{			
				carve::mesh::MeshSimplifier simplifier;
				size_t mods = simplifier.improveMesh(_meshSet, min_colinearity,min_delta_v,min_normal_angle,EPSILON);
				GC::KeepAlive(this);
				return mods;
			}
			size_t XbimPolyhedron::Improve(double EPSILON)
			{			
				carve::mesh::MeshSimplifier simplifier;
				size_t mods = simplifier.improveMesh_conservative(_meshSet, EPSILON);	
				GC::KeepAlive(this);
				return mods;
			}

			size_t XbimPolyhedron::MergeCoPlanarFaces(double normalAngle)
			{			
				carve::mesh::MeshSimplifier simplifier;
				size_t mods = simplifier.mergeCoplanarFaces(_meshSet, normalAngle);	
				GC::KeepAlive(this);
				return mods;
			}

			size_t XbimPolyhedron::EliminateShortEdges(double minLength, double EPSILON)
			{
				carve::mesh::MeshSimplifier simplifier;
				return simplifier.eliminateShortEdges(_meshSet,minLength,EPSILON);
			}

			size_t XbimPolyhedron::RemoveFins()
			{
				carve::mesh::MeshSimplifier simplifier;
				return simplifier.removeFins(_meshSet);
			}  


			size_t XbimPolyhedron::Simplify(double min_colinearity,
				double min_delta_v,
				double min_normal_angle,
				double min_length,
				double EPSILON)
			{
				carve::mesh::MeshSimplifier simplifier;
				size_t mods = simplifier.simplify(_meshSet,min_colinearity, min_delta_v,min_normal_angle,min_length, EPSILON);	
				GC::KeepAlive(this);
				return mods;
			}





			//Constructs geometry based on  ascii data
			XbimPolyhedron::XbimPolyhedron(String^ strData)
			{	
				StringReader^ sr = gcnew StringReader(strData);
				String^ l = sr->ReadLine(); 
				array<Char>^ space = gcnew array<Char>{' '};
				array<Char>^ comma = gcnew array<Char>{','};
				array<Char>^ slash = gcnew array<Char>{'/'};

				std::vector<vertex_t> vertices;
				XbimNormalMap* normalMap = new XbimNormalMap();
				std::vector<face_t*> meshFaces;
				std::vector<Double3D> normals;
				int curveSurfaceCount = 0;
				while( l!=nullptr)
				{
					array<String^>^ toks = l->Split(space,StringSplitOptions::RemoveEmptyEntries);
					if(toks->Length<2) //skip if invalid line
						continue;
					String^ cmd = toks[0]->ToUpperInvariant();
					if( cmd == "T")
					{

						bool isCurvedSurface = false;
						if(toks->Length>1) //we have at least one triangle
						{
							//first look to see if we have a curved surface, if the second value has a normal they all have
							array<String^>^ indices = toks[1]->Split(comma,StringSplitOptions::RemoveEmptyEntries);
							array<String^>^ b = indices[1]->Split(slash,StringSplitOptions::RemoveEmptyEntries);
							//if the second indices has a normal all the rest do, so it is a curved surface
							if(b->Length==2) 
							{
								isCurvedSurface=true; //we will need to keep all triangles
								curveSurfaceCount++;
							}
							for (int i = 1; i < toks->Length; i++)
							{
								array<String^>^ indices = toks[i]->Split(comma,StringSplitOptions::RemoveEmptyEntries);
								array<String^>^ a = indices[0]->Split(slash,StringSplitOptions::RemoveEmptyEntries);
								array<String^>^ b = indices[1]->Split(slash,StringSplitOptions::RemoveEmptyEntries);
								array<String^>^ c = indices[2]->Split(slash,StringSplitOptions::RemoveEmptyEntries);
								meshFaces.reserve(indices->Length);
								size_t av = UInt32::Parse(a[0]);
								size_t bv = UInt32::Parse(b[0]);
								size_t cv = UInt32::Parse(c[0]);
								meshFaces.push_back(new face_t(&vertices[av],&vertices[bv],&vertices[cv]));
								if(isCurvedSurface) 
								{
									size_t an = UInt32::Parse(a[1]);
									size_t bn = UInt32::Parse(b[1]);
									size_t cn = UInt32::Parse(c[1]);
									normalMap->AddFaceToSurface(meshFaces.back(),curveSurfaceCount);
									normalMap->SetNormalToVertexOnSurface(curveSurfaceCount,av,normals[an]);
									normalMap->SetNormalToVertexOnSurface(curveSurfaceCount,bv,normals[bn]);
									normalMap->SetNormalToVertexOnSurface(curveSurfaceCount,cv,normals[cn]);
								}
							}
						}
					}
					else if(cmd=="V")
					{
						for (int i = 1; i < toks->Length; i++)
						{
							array<String^>^ coords = toks[i]->Split(comma,StringSplitOptions::RemoveEmptyEntries);
							vertices.push_back(carve::geom::VECTOR(Double::Parse(coords[0]),Double::Parse(coords[1]),Double::Parse(coords[2])));
						}
					}	
					else if(cmd == "N")
					{
						for (int i = 1; i < toks->Length; i++)
						{
							array<String^>^ norm = toks[i]->Split(comma,StringSplitOptions::RemoveEmptyEntries);
							normals.push_back(Double3D(Double::Parse(norm[0]),Double::Parse(norm[1]),Double::Parse(norm[2])));
						}
					}
					else if(cmd == "P") //initialise the polyData
					{
						String^ version = toks[1];
						int vCount = Int32::Parse(toks[2]);
						int fCount = Int32::Parse(toks[3]);
						int tCount = Int32::Parse(toks[4]);
						int nCount = Int32::Parse(toks[5]);
						vertices.reserve(vCount);
						normals.reserve(nCount);
						meshFaces.reserve(tCount);
					}
					else if(cmd == "F") //initialise the face data
					{
						//do nothing
					}
					else
						Logger->WarnFormat("Illegal Polygon command format '{0}' has been ignored", cmd);
					l = sr->ReadLine(); //get the next line
				}
				std::vector<mesh_t*> newMeshes;
				mesh_t::create(meshFaces.begin(), meshFaces.end(), newMeshes, carve::mesh::MeshOptions());
				_meshSet =  new meshset_t(vertices,newMeshes);
				if(normalMap->UniqueNormals().size() > 0)
					_normalMap = normalMap;
				else
					delete normalMap;

			}

			XbimPolyhedron::XbimPolyhedron(carve::csg::CSG::meshset_t* mesh, XbimNormalMap* normalMap, int representationLabel, int styleLabel)
			{
				_bounds=XbimRect3D::Empty;
				_meshSet = mesh;
				_representationLabel=representationLabel;
				_surfaceStyleLabel=styleLabel;
				_normalMap = normalMap;

			}

			XbimPolyhedron::XbimPolyhedron(carve::csg::CSG::meshset_t* mesh, int representationLabel, int styleLabel)
			{
				_bounds=XbimRect3D::Empty;
				_meshSet = mesh;
				_representationLabel=representationLabel;
				_surfaceStyleLabel=styleLabel;

			}

			XbimPolyhedron::XbimPolyhedron(int representationLabel, int styleLabel)
			{
				_bounds=XbimRect3D::Empty;
				_meshSet=nullptr;
				_representationLabel=representationLabel;
				_surfaceStyleLabel=styleLabel;

			}
			  
			XbimPolyhedron::XbimPolyhedron(XbimModelFactors^ modelFactors, IEnumerable<IfcFace^>^ faces, int representationLabel, int styleLabel, bool makeSolid)	
			{
				_representationLabel=representationLabel;
				_surfaceStyleLabel=styleLabel;
				std::vector<vertex_t> vertices;
				std::vector<mesh_t*> meshes;
				AddFaces(vertices,meshes, modelFactors->Precision,modelFactors->Rounding, faces, makeSolid);	
				 	///invert any manifolds that are negative
				for (size_t i = 0; i < meshes.size(); i++)
				{
					if(meshes[i]->isNegative())
							meshes[i]->invert();
				}
				_meshSet = new meshset_t(vertices, meshes);
			//	System::Diagnostics::Debug::Assert(_meshSet->isClosed());	
				
			
			}

			XbimPolyhedron::XbimPolyhedron(IfcShell^ shell, double precision, unsigned int rounding,  int representationLabel, int styleLabel, bool orientate)
			{
				_representationLabel=representationLabel;
				_surfaceStyleLabel=styleLabel;
				AddFaces(shell,precision,rounding,orientate);
			}

			XbimPolyhedron::XbimPolyhedron(IfcOpenShell^ shell, double precision, unsigned int rounding,  int representationLabel, int styleLabel, bool orientate)
			{
				_representationLabel=representationLabel;
				_surfaceStyleLabel=styleLabel;
				AddFaces(shell,precision,rounding,orientate);
			}

			XbimPolyhedron::XbimPolyhedron(IfcFacetedBrep^ brep, double precision, unsigned int rounding,  int representationLabel, int styleLabel, bool orientate)
			{
				_representationLabel=representationLabel;
				_surfaceStyleLabel=styleLabel;
				//A facetted BRep is intended to be a closed manifold
				AddFaces(brep->Outer,precision,rounding,orientate);

			}


			XbimPolyhedron::XbimPolyhedron(IfcClosedShell^ shell, double precision, unsigned int rounding,  int representationLabel, int styleLabel, bool orientate)
			{
				_representationLabel=representationLabel;
				_surfaceStyleLabel=styleLabel;
				AddFaces(shell,precision,rounding,orientate);
				
			}

			XbimPolyhedron::XbimPolyhedron(IfcFaceBasedSurfaceModel^ fbsm, double precision, unsigned int rounding,  int representationLabel, int styleLabel, bool orientate)
			{
				_representationLabel=representationLabel;
				_surfaceStyleLabel=styleLabel;
				AddFaces(fbsm,precision,rounding,orientate);
				
			}

			XbimPolyhedron::XbimPolyhedron(IfcShellBasedSurfaceModel^ sbsm, double precision, unsigned int rounding,  int representationLabel, int styleLabel, bool orientate)
			{
				_representationLabel=representationLabel;
				_surfaceStyleLabel=styleLabel;
				AddFaces(sbsm,precision,rounding,orientate);
			}

			XbimPolyhedron::XbimPolyhedron(IfcConnectedFaceSet^ faces, double precision, unsigned int rounding,  int representationLabel, int styleLabel, bool orientate)
			{
				_representationLabel=representationLabel;
				_surfaceStyleLabel=styleLabel;
				AddFaces(faces,precision,rounding,orientate);
			}

			bool IsClockwiseFace(std::vector<carve::geom2d::P2> points)
			{
				const size_t SZ = points.size();
				double n = 0;
				for (size_t i = 0; i <SZ; i++)
				{
					carve::geom2d::P2 a = points[i];
					carve::geom2d::P2 b = points[(i+1) % SZ];
					n += (b.x-a.x) * (b.y+a.y);
				}
				return n > 0;
			}

			void XbimPolyhedron::AddFaces(IfcShellBasedSurfaceModel^ sbsm, double precision, unsigned int rounding, bool orientate)
			{
				int allPointsTally = IfcShellBasedSurfaceModelGeometricExtensions::NumberOfPointsMax(sbsm);
				if(_meshSet==nullptr)
				{
					std::vector<vertex_t> vertices;
					vertices.reserve(allPointsTally);
					std::vector<mesh_t*> meshes;
					for each (IfcShell^ shell in sbsm->SbsmBoundary)
					{
						if(dynamic_cast<IfcClosedShell^>(shell))
						{
							IfcClosedShell^ closedShell = (IfcClosedShell^)shell;
							if(closedShell->ModelOf->ModelFactors->MaxBRepSewFaceCount<allPointsTally) orientate = false;
							AddFaces(vertices,meshes,precision,rounding,  closedShell->CfsFaces, orientate);
						}
						else if(dynamic_cast<IfcOpenShell^>(shell))
						{
							IfcOpenShell^ openShell = (IfcOpenShell^)shell;
							if(openShell->ModelOf->ModelFactors->MaxBRepSewFaceCount<allPointsTally) orientate = false;
							AddFaces(vertices,meshes,precision,rounding,  openShell->CfsFaces, orientate);
						}
						else
							throw gcnew Exception(String::Format("Undefined shell type, neither open nor closed in #{0}",shell->EntityLabel));
					}
					_meshSet = new meshset_t(vertices, meshes);
				}
				else
				{
					ResizeVertexStore(allPointsTally +_meshSet->vertex_storage.size());
					for each (IfcShell^ shell in sbsm->SbsmBoundary)
					{
						if(dynamic_cast<IfcClosedShell^>(shell))
						{
							IfcOpenShell^ openShell = (IfcOpenShell^)shell;
							if(openShell->ModelOf->ModelFactors->MaxBRepSewFaceCount < allPointsTally) orientate = false;
							AddFaces(_meshSet->vertex_storage,_meshSet->meshes,precision,rounding,  openShell->CfsFaces, orientate);
						}
						else if(dynamic_cast<IfcOpenShell^>(shell))
						{
							IfcOpenShell^ openShell = (IfcOpenShell^)shell;
							if(openShell->ModelOf->ModelFactors->MaxBRepSewFaceCount < allPointsTally) orientate = false;
							AddFaces(_meshSet->vertex_storage,_meshSet->meshes,precision,rounding,  openShell->CfsFaces, orientate);
						}
						else
							throw gcnew Exception(String::Format("Undefined shell type, neither open nor closed in #{0}",shell->EntityLabel));
					}
				}

			}

			void XbimPolyhedron::ResizeVertexStore(size_t newSize)
			{
				if(_meshSet!=nullptr && _meshSet->vertex_storage.size()<newSize)
				{
					//need to grow the vertex store
					std::unordered_map<vertex_t *, size_t> vert_idx;
					for (size_t m = 0; m < _meshSet->meshes.size(); ++m) {
						mesh_t *mesh = _meshSet->meshes[m];

						for (size_t f = 0; f < mesh->faces.size(); ++f) {
							face_t *face = mesh->faces[f];
							edge_t *edge = face->edge;
							do {
								vert_idx[edge->vert] = 0;
								edge = edge->next;
							} while (edge != face->edge);
						}
					}

					std::vector<vertex_t> new_vertex_storage;
					new_vertex_storage.reserve(newSize);
					for (std::unordered_map<vertex_t *, size_t>::iterator
						i = vert_idx.begin(); i != vert_idx.end(); ++i) {
							(*i).second = new_vertex_storage.size();
							new_vertex_storage.push_back(*(*i).first);
					}

					for (size_t m = 0; m < _meshSet->meshes.size(); ++m) {
						mesh_t *mesh = _meshSet->meshes[m];
						for (size_t f = 0; f < mesh->faces.size(); ++f) {
							face_t *face = mesh->faces[f];
							edge_t *edge = face->edge;
							do {
								size_t i = vert_idx[edge->vert];
								edge->vert = &new_vertex_storage[i];
								edge = edge->next;
							} while (edge != face->edge);
						}
					}
					std::swap(_meshSet->vertex_storage, new_vertex_storage);
				}
			}

			void XbimPolyhedron::AddFaces( IfcFaceBasedSurfaceModel^ fbsm,double precision, unsigned int rounding, bool orientate)
			{
				int allPointsTally = IfcFaceBasedSurfaceModelGeometricExtensions::NumberOfPointsMax(fbsm);
				if(_meshSet==nullptr)
				{
					std::vector<vertex_t> vertices;
					vertices.reserve(allPointsTally);
					std::vector<mesh_t*> meshes;
					for each (IfcConnectedFaceSet^ faces in fbsm->FbsmFaces)
						AddFaces(vertices,meshes,precision,rounding, faces->CfsFaces, orientate);
					_meshSet = new meshset_t(vertices, meshes);
				}
				else
				{
					ResizeVertexStore(allPointsTally +_meshSet->vertex_storage.size());
					for each (IfcConnectedFaceSet^ faces in fbsm->FbsmFaces)
					{
						orientate = faces->ModelOf->ModelFactors->MaxBRepSewFaceCount>=allPointsTally ;
						AddFaces(_meshSet->vertex_storage,_meshSet->meshes,precision,rounding, faces->CfsFaces, orientate);
					}
				}
				
			}

			void XbimPolyhedron::AddFaces(IfcOpenShell^ shell, double precision, unsigned int rounding,  bool orientate)
			{
				AddFaces((IfcConnectedFaceSet^)shell,precision, rounding,  orientate);
			}

			void XbimPolyhedron::AddFaces(IfcClosedShell^ shell, double precision, unsigned int rounding,  bool orientate)
			{
				AddFaces((IfcConnectedFaceSet^)shell,precision, rounding,  orientate);
				
			}


			void XbimPolyhedron::AddFaces(IfcShell^ shell, double precision, unsigned int rounding,  bool orientate)
			{
				if(dynamic_cast<IfcClosedShell^>(shell))
				{
					AddFaces((IfcClosedShell^)shell,precision, rounding,  orientate);
				}
				else if(dynamic_cast<IfcOpenShell^>(shell))
				{
					AddFaces((IfcOpenShell^)shell,precision, rounding,  orientate);
				}
				else
					throw gcnew Exception(String::Format("Undefined shell type, neither open nor closed in #{0}",shell->EntityLabel));
			}

			void XbimPolyhedron::AddFaces(IfcFacetedBrep^ brep, double precision, unsigned int rounding,  bool orientate)
			{
			    AddFaces(brep->Outer, precision,rounding, orientate);				
			}

			void XbimPolyhedron::AddFaces(IfcConnectedFaceSet^ faces, double precision, unsigned int rounding,  bool orientate)
			{
				int allPointsTally = IfcConnectedFaceSetGeometricExtensions::NumberOfPointsMax(faces);
				if(_meshSet==nullptr)
				{
					std::vector<vertex_t> vertices;
					vertices.reserve(allPointsTally);
					std::vector<mesh_t*> meshes;
					if(faces->ModelOf->ModelFactors->MaxBRepSewFaceCount<allPointsTally) orientate = false;
					AddFaces(vertices,meshes,precision,rounding,  faces->CfsFaces, orientate);
					_meshSet = new meshset_t(vertices, meshes);
				}
				else
				{
					ResizeVertexStore(allPointsTally+_meshSet->vertex_storage.size());
					if(faces->ModelOf->ModelFactors->MaxBRepSewFaceCount<allPointsTally) orientate = false;
					AddFaces(_meshSet->vertex_storage,_meshSet->meshes,precision,rounding,  faces->CfsFaces, orientate);
				}
			}

			void XbimPolyhedron::AddFaces(std::vector<vertex_t>& vertices, std::vector<mesh_t*>& meshes, double precision, unsigned int rounding, IEnumerable<IfcFace^>^ faces, bool orientate)
			{

				//first of all get all the points						
				_bounds=XbimRect3D::Empty;
				std::unordered_map<Double3D, size_t> vertexMap;		
				std::vector<std::vector<size_t>> csgFaces; //faces on the polyhedron
				csgFaces.reserve(Enumerable::Count(faces));
				for each (IfcFace^ fc in  faces)
				{
					bool outerBoundDefined;
					IfcFaceBound^ outerBound = Enumerable::FirstOrDefault(Enumerable::OfType<IfcFaceOuterBound^>(fc->Bounds)); //get the outer bound
					if(outerBound == nullptr)
					{
						outerBound = Enumerable::FirstOrDefault(fc->Bounds); //if one not defined explicitly use first found
						outerBoundDefined=false;
					}
					else
						outerBoundDefined=true;
					if(outerBound == nullptr || !dynamic_cast<IfcPolyLoop^>(outerBound->Bound)|| ((IfcPolyLoop^)(outerBound->Bound))->Polygon->Count<3) 
						continue; //invalid polygonal face
					XbimVector3D n = PolyLoopExtensions::NewellsNormal((IfcPolyLoop^)(outerBound->Bound));
					//srl if an invalid normal is returned the face is not valid (sometimes a line or a point is defined) skip the face
					if(n.IsInvalid()) 
						continue;
					std::vector<size_t> outerLoopPoints;

					std::vector<std::vector<size_t>> holes;
					
					for each (IfcFaceBound^ bound in fc->Bounds)
					{
						IfcPolyLoop^ polyLoop=(IfcPolyLoop^)bound->Bound;
						int loopPointCount = polyLoop->Polygon->Count;
						if(polyLoop->Polygon->Count < 3) 
						{
							Logger->WarnFormat("Invalid bound #{0}, less than 3 points",bound->EntityLabel);
							continue;
						}
						IEnumerable<IfcCartesianPoint^>^ pts = polyLoop->Polygon;
						if(!bound->Orientation)
							pts = Enumerable::Reverse(pts);
						//add all the points into shell point map
						if(bound==outerBound)
						{
							outerLoopPoints.reserve(loopPointCount);
							for each(IfcCartesianPoint^ p in pts)
							{
								size_t index;
								Double3D p3D(p->X,p->Y,p->Z,precision,rounding); 
								
								std::unordered_map<Double3D, size_t>::const_iterator hit = vertexMap.find(p3D);
								if(hit==vertexMap.end()) //not found add it in
								{	
									index = vertices.size();
									vertexMap.insert(std::make_pair(p3D,index));
									vertices.push_back(carve::geom::VECTOR(p->X, p->Y, p->Z));
								}
								else
									index = hit->second;
								if(outerLoopPoints.size()==0 || index!= outerLoopPoints.back()) 
									outerLoopPoints.push_back(index);  //don't add the same point twice
								else
									Logger->InfoFormat("Duplicate vertex found in IfcFaceBound #{0}, it has been ignored",bound->EntityLabel);
								
							}
							if(outerLoopPoints.size()>1 && outerLoopPoints.front() == outerLoopPoints.back()) // too many points specified, some tools duplicate the last point to close
							{
								outerLoopPoints.pop_back();
								Logger->InfoFormat("Duplicate vertex found in IfcFaceBound #{0} (start point duplicated, it has been ignored",bound->EntityLabel);
							}
							if(outerLoopPoints.size()<3)
							{
								Logger->InfoFormat("Small face #{0}, less than 3 points, it has been ignored",bound->EntityLabel);
								break; //quite and go to next face
							}
						}
						else
						{
							//get a hole loop
							holes.push_back(std::vector<size_t>());
							std::vector<size_t> & holeLoop = holes.back();	
							holeLoop.reserve(loopPointCount);
							for each(IfcCartesianPoint^ p in pts)
							{
								size_t index;
								Double3D p3D(p->X,p->Y,p->Z,precision,rounding); 
								std::unordered_map<Double3D, size_t>::const_iterator hit = vertexMap.find(p3D);
								if(hit==vertexMap.end()) //not found add it in
								{	
									index = vertices.size();
									vertexMap.insert(std::make_pair(p3D,index));
									vertices.push_back(carve::geom::VECTOR(p->X, p->Y, p->Z));

								}
								else
									index = hit->second;
								if(holeLoop.size() ==0 || index!= holeLoop.back())
									holeLoop.push_back(index); //don't add the same point twice
								else
									Logger->InfoFormat("Duplicate vertex found in IfcPolyloop #{0}, it has been ignored",bound->EntityLabel);
							}
							if(holeLoop.size()>1 && holeLoop.front() == holeLoop.back()) // too many points specified, some tools duplicate the last point to close
							{
								holeLoop.pop_back();
								Logger->InfoFormat("Duplicate vertex found in IfcPolyloop #{0} (start point duplicated, it has been ignored",bound->EntityLabel);
							}
							if(holeLoop.size()<3)
							{
									Logger->WarnFormat("Small inner bound #{0}, with less than 3 points, it has been ignored",bound->EntityLabel);
									holes.pop_back();
							}
						}
						
					}
					//if we have holes then incorporate them in the face
					if(holes.size())
					{
						bool warnFixApplied=false;
						//reserve slots for each bound, including the outer bound
IncorporateHoles:	
						std::vector<std::vector<carve::geom2d::P2> > projected_poly;
						projected_poly.resize(holes.size() + 1);
						projected_poly[0].reserve(outerLoopPoints.size());
						
						try
						{
							if(!outerBoundDefined) //check and correct as necessary
							{
								//find the outer loop
								
								//get a bounding box for each wire and check containment
								std::vector<aabb_t> bboxes;
								bboxes.reserve(holes.size()+1);
								std::vector<vector_t> outerVectors; outerVectors.reserve(outerLoopPoints.size());
								for (size_t j = 0; j < outerLoopPoints.size(); ++j) 
								{
									outerVectors.push_back(vertices[outerLoopPoints[j]].v);
								}
								aabb_t outerLoopbb(outerVectors.begin(),outerVectors.end());
								
								//project the holes
								for (size_t j = 0; j < holes.size(); ++j)
								{
									std::vector<vector_t> vectors; vectors.reserve( holes[j].size());
									for (size_t k = 0; k < holes[j].size(); ++k)
									{
										vectors.push_back(vertices[holes[j][k]].v);
									}
									aabb_t innerLoopbb(vectors.begin(),vectors.end());
									if(innerLoopbb.contains(outerLoopbb,precision)) ////check if the outerbound is the correct one
									{
										std::swap(holes[j],outerLoopPoints);
										outerLoopbb=innerLoopbb;	
									}
								}
								//create a face that fits the outer bound to project points on to
								std::vector<vertex_t *> v; 
								size_t sizeOuter = outerLoopPoints.size();
								v.reserve(sizeOuter);
								for (size_t i = 0; i < sizeOuter; ++i) 
									v.push_back(&vertices[outerLoopPoints[i]]);
								face_t projectionFace(v.begin(), v.end());
								//project all the points of the outerbound onto the 2D face
								for (size_t j = 0; j < outerLoopPoints.size(); ++j) 
								{
									projected_poly[0].push_back(projectionFace.project(vertices[outerLoopPoints[j]].v));
								}
								//project the holes
								for (size_t j = 0; j < holes.size(); ++j)
								{
									projected_poly[j+1].reserve(holes[j].size());									
									for (size_t k = 0; k < holes[j].size(); ++k)
									{
										projected_poly[j+1].push_back(projectionFace.project(vertices[holes[j][k]].v));									
									}
								}
								

							}
							else
							{	
								//create a face that fits the outer bound to project points on to
								std::vector<vertex_t *> v; 
								size_t sizeOuter = outerLoopPoints.size();
								v.reserve(sizeOuter);
								for (size_t i = 0; i < sizeOuter; ++i) 
									v.push_back(&vertices[outerLoopPoints[i]]);
								face_t projectionFace(v.begin(), v.end());
								//project all the points of the outerbound onto the 2D face

								for (size_t j = 0; j < outerLoopPoints.size(); ++j) 
								{
									projected_poly[0].push_back(projectionFace.project(vertices[outerLoopPoints[j]].v));
								}
								//project the holes
								for (size_t j = 0; j < holes.size(); ++j)
								{
									projected_poly[j+1].reserve(holes[j].size());
									for (size_t k = 0; k < holes[j].size(); ++k)
									{
										projected_poly[j+1].push_back(projectionFace.project(vertices[holes[j][k]].v));
									}
								}
							}
							//fix the orientation
							bool outerClockwise = IsClockwiseFace(projected_poly[0]);
							if(outerClockwise) 
								std::reverse(projected_poly[0].begin(),projected_poly[0].end());
							for (size_t i = 1; i < projected_poly.size(); i++)
							{
								bool innerClockwise = IsClockwiseFace(projected_poly[i]);
								if(!innerClockwise) 
									std::reverse(projected_poly[i].begin(),projected_poly[i].end());
							}
							
							std::vector<std::pair<size_t, size_t> > result = carve::triangulate::incorporateHolesIntoPolygon(projected_poly);
							csgFaces.push_back(std::vector<size_t>());
							std::vector<size_t> &out = csgFaces.back();
							out.reserve(result.size());
							for (size_t j = 0; j < result.size(); ++j) 
							{
								if (result[j].first == 0) 
									out.push_back(outerLoopPoints[result[j].second]);
								else 
									out.push_back(holes[result[j].first-1][result[j].second]);
							}		
						}
						catch(...)
						{
							if(outerBoundDefined)
							{
								outerBoundDefined=false; //try again
								warnFixApplied = true;
								goto IncorporateHoles;
							}
							if(outerLoopPoints.size()>2)
							{
								csgFaces.push_back(std::vector<size_t>()); //add the outer loop in
								std::vector<size_t> &out = csgFaces.back();
								std::swap(out,outerLoopPoints);		
							}
						}
						if(warnFixApplied)
							Logger->InfoFormat("Face error. Inner face loop is not contained in face #{0}. The error has been corrected" ,fc->EntityLabel);					

					}
					else //add the face, there are no holes
					{
						if(outerLoopPoints.size()>2)
						{
							csgFaces.push_back(std::vector<size_t>());
							std::vector<size_t> &out = csgFaces.back();
							std::swap(out,outerLoopPoints);	
						}
					}
				}

				//convert the faces and vertices to a mesh set
				std::vector<face_t *> faceList;
				faceList.reserve(csgFaces.size());
				std::vector<vertex_t *> vf; 
				for (size_t i = 0; i < csgFaces.size(); ++i) 
				{
					size_t sizeInner = csgFaces[i].size();
					vf.clear();
					vf.reserve(sizeInner);
					for (size_t j = 0; j < sizeInner; ++j)
					{
						vf.push_back(&vertices[csgFaces[i][j]]);
					}					 
					faceList.push_back(new face_t(vf.begin(),vf.end()));
				}
				std::vector<mesh_t*> newMeshes;
			    mesh_t::create(faceList.begin(), faceList.end(), newMeshes, carve::mesh::MeshOptions().avoid_cavities(true),precision*precision,orientate);
				for (size_t i = 0; i < newMeshes.size(); i++)
				{
					if(newMeshes[i]->isNegative() || newMeshes[i]->volume()<0)
						newMeshes[i]->invert();
					meshes.push_back(newMeshes[i]);
					newMeshes[i]->meshset=_meshSet;
				}
				newMeshes.clear();
				
			}

			double XbimPolyhedron::Volume::get()  
			{
				if (_meshSet==nullptr) return 0.0;
				double vol = 0.0;
				for (size_t i = 0; i < _meshSet->meshes.size(); ++i) 
				{
					vol+=_meshSet->meshes[i]->volume();
				}
				return vol;				
			}

			int XbimPolyhedron::FaceCount::get()  
			{
				if (_meshSet==nullptr) return 0;
				else
				{
					int tally = 0;
					for (size_t i = 0; i < _meshSet->meshes.size(); i++)
					{
						tally+=_meshSet->meshes[i]->faces.size();
					}
					return tally;
				}
			}

			int XbimPolyhedron::VertexCount::get()  
			{
				if (_meshSet==nullptr) return 0;
				else
					return _meshSet->vertex_storage.size();
			}

			void XbimPolyhedron::InstanceCleanup()
			{  
				IntPtr temp = System::Threading::Interlocked::Exchange(IntPtr(_meshSet), IntPtr(0));
				if(temp!=IntPtr(0))
				{
					delete _meshSet;
					_meshSet=nullptr;
					temp = System::Threading::Interlocked::Exchange(IntPtr(_normalMap), IntPtr(0));
					if(temp!=IntPtr(0))
					{
						delete _normalMap;
						_normalMap=nullptr;
					}
					System::GC::SuppressFinalize(this);
				}

				XbimGeometryModel::InstanceCleanup();
			}

			
			void XbimPolyhedron::DeletePolyhedron(void)
			{
				if(_meshSet!=nullptr)
					delete _meshSet;
				_meshSet=nullptr;
			}

			//Transforms the polyhedron by the specified matrix and returns a copy
			IXbimGeometryModel^ XbimPolyhedron::TransformBy(XbimMatrix3D t)
			{
				if(_meshSet==nullptr) return this;
				carve::math::Matrix m(t.M11,t.M21,t.M31,t.OffsetX,
					t.M12,t.M22,t.M32,t.OffsetY,
					t.M13,t.M23,t.M33,t.OffsetZ,
					t.M14,t.M24,t.M34,t.M44);
				carve::math::matrix_transformation mt(m);
				meshset_t* newMesh = _meshSet->clone();
				newMesh->transform(mt);
				XbimNormalMap* newNormaMap;
				if(_normalMap!=nullptr)
				{
					std::unordered_map<face_t*,face_t*> facelookup;
					meshset_t::face_iter clone = newMesh->faceBegin();
					meshset_t::face_iter orig =_meshSet->faceBegin();
					for (; clone!=newMesh->faceEnd();)
					{
						facelookup[*orig]=*clone;
						clone++;
						orig++;
					}
					newNormaMap = new XbimNormalMap(_normalMap,facelookup);
				}
				return gcnew XbimPolyhedron(newMesh,newNormaMap,_representationLabel,_surfaceStyleLabel);
				
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

			bool XbimPolyhedron::WritePlyInternal(String^ fileName, bool ascii)
			{
				const char* chars = (const char*)(Marshal::StringToHGlobalAnsi(fileName)).ToPointer();
				try
				{
					std::string stdString = chars;
					carve::common::writePLY(stdString,_meshSet,ascii);
					return true;
				}
				catch(...)
				{
					return false;
				}
				finally
				{
					Marshal::FreeHGlobal(IntPtr((void*)chars));
				}
			}

			void XbimPolyhedron::WriteObj(String^ fileName)
			{
				const char* chars = (const char*)(Marshal::StringToHGlobalAnsi(fileName)).ToPointer();
				std::string stdString = chars;
				carve::common::writeOBJ(stdString,_meshSet);
				Marshal::FreeHGlobal(IntPtr((void*)chars));

			}

			void XbimPolyhedron::WriteVtk(String^ fileName)
			{
				const char* chars = (const char*)(Marshal::StringToHGlobalAnsi(fileName)).ToPointer();
				std::string stdString = chars;
				carve::common::writeVTK(stdString,_meshSet);
				Marshal::FreeHGlobal(IntPtr((void*)chars));

			}

		

			bool XbimPolyhedron::Write(String^ fileName,XbimModelFactors^ modelFactors)
			{
				try
				{
					StreamWriter^ sw = gcnew StreamWriter(fileName);
					sw->Write(WriteAsString(modelFactors));
					sw->Close();
					return true;
				}
				catch(Exception^)
				{
					return false;
				}
			}
			//If model factors is a nullptr the default precision of 1e-5 is used
			String^ XbimPolyhedron::WriteAsString(XbimModelFactors^ modelFactors)
			{
				
				double precision = modelFactors==nullptr?1e-5:modelFactors->Precision;
				size_t rounding =  (size_t)(modelFactors==nullptr?4:modelFactors->Rounding);
				StringBuilder^ sw = gcnew StringBuilder();
				size_t normalsOffset=-1;
				int vCount=0, fCount=0, tCount=0, nCount=0;
				if(_meshSet==nullptr || _meshSet->vertex_storage.size() ==0) return "";
				sw->Append("V");
				for (std::vector<meshset_t::vertex_t>::const_iterator 
					i = _meshSet->vertex_storage.begin(); i!=_meshSet->vertex_storage.end(); ++i)
				{
					vCount++;
					vertex_t vt = *i;
					sw->Append(String::Format(" {0},{1},{2}",vt.v.x,vt.v.y,vt.v.z));
				}
				sw->AppendLine();
				//write out any compound faces (curved surfaces typically)

				std::vector<std::pair<size_t, face_t*>> faceGroups; //pairs to assoc surfaceID with face
				
				std::unordered_map<face_t*,size_t> faceNormals;	
				if(_normalMap==nullptr) _normalMap = new XbimNormalMap();
				
				for (meshset_t::face_iter i = _meshSet->faceBegin(), e = _meshSet->faceEnd(); i != e; ++i) 
				{
					fCount++;
					face_t *face = *i;
					size_t surfaceID = _normalMap->GetSurfaceIdOfFace(face);
					faceGroups.push_back(std::make_pair(surfaceID,face));
					if(surfaceID==0) //it is a planar face we need to add its normal
					{
						vector_t n =  face->plane.N;	
						Double3D n3D(n.x,n.y,n.z,precision,rounding);
						faceNormals[face]=_normalMap->AddNormal(n3D);
					}
				}
				
				std::sort(faceGroups.begin(), faceGroups.end(), sortFaceGroup());
				//write the normals
				const std::vector<Double3D>& uniqueNormals = _normalMap->UniqueNormals();
				nCount =uniqueNormals.size();
				if(uniqueNormals.size()>0)
				{
					sw->Append("N");
					for (std::vector<Double3D>::const_iterator i = uniqueNormals.begin(); i !=uniqueNormals.end(); ++i)
					{
						Double3D norm = *i;
						sw->Append(String::Format(" {0},{1},{2}",Math::Round(norm.Dim1,4),Math::Round(norm.Dim2,4),Math::Round(norm.Dim3,4)));
						
					}
					sw->AppendLine();
				}
			

				size_t currentGroup =  std::numeric_limits<std::size_t>::max(); //the current group is undefined
				bool firstTime=true;
				bool planarSurface = true;
				for (std::vector<std::pair<size_t, face_t*>>::iterator i = faceGroups.begin(), e = faceGroups.end(); i != e; ++i) 
				{
					size_t surfaceGroup = i->first;
					face_t *face = i->second;
					int numVertices = face->nVertices();
				
					if(numVertices>0)
					{

						//if the face is a triangle just do it
						std::vector<carve::triangulate::tri_idx> result;
						std::vector<carve::geom::vector<2> > projectedVerts;
						face->getProjectedVertices(projectedVerts);
						std::vector<carve::mesh::MeshSet<3>::vertex_t *> verts;
						face->getVertices(verts);						
						carve::triangulate::triangulate(projectedVerts,result,precision);

						if(currentGroup!=surfaceGroup || surfaceGroup==0)
						{
							firstTime=true;
							if(currentGroup!=std::numeric_limits<std::size_t>::max())
								sw->AppendLine();
							sw->Append("T"); 
						    planarSurface = _normalMap->IsPlanarSurface(surfaceGroup);
						}
						
						for (size_t i = 0; i < result.size(); i++)
						{
							tCount++;
							ptrdiff_t a = carve::poly::ptrToIndex_fast(_meshSet->vertex_storage,verts[result[i].a]);
							ptrdiff_t b = carve::poly::ptrToIndex_fast(_meshSet->vertex_storage,verts[result[i].b]);
							ptrdiff_t c = carve::poly::ptrToIndex_fast(_meshSet->vertex_storage,verts[result[i].c]);

							if(planarSurface) // normals are the same
							{
								if(firstTime)
								{
									size_t normalIndex;
									if(surfaceGroup==0)
										normalIndex= faceNormals[face]; //just get the first normal
									else
										normalIndex= _normalMap->GetNormalIndexToAnyVertexOnSurface(surfaceGroup);
									sw->AppendFormat(" {0}/{3},{1},{2}", a,b,c, normalIndex);
									firstTime=false;
								}
								else
									sw->AppendFormat(" {0},{1},{2}", a,b,c);
							}
							else //each point has its own normal
							{
								size_t an = _normalMap->GetNormalIndexToVertexOnSurface(surfaceGroup,a);
								size_t bn = _normalMap->GetNormalIndexToVertexOnSurface(surfaceGroup,b);
								size_t cn = _normalMap->GetNormalIndexToVertexOnSurface(surfaceGroup,c);
								sw->AppendFormat(" {0}/{3},{1}/{4},{2}/{5}", a,b,c, an,bn,cn);
							}
						}	
						//sw->AppendLine();
						//sw->Append("F"); fCount++; //write out the face boundaries
						//for (size_t i = 0; i < verts.size(); i++)
						//{
						//	ptrdiff_t a = carve::poly::ptrToIndex_fast(_meshSet->vertex_storage,verts[i]);
						//	sw->Append(" ");
						//	sw->Append(a);
						//}	
						//sw->AppendLine();
						currentGroup=surfaceGroup;
					}
				}
				//Polyhedron header =  Version | Vertices Count | Face Count | Trianngle Count
				String^ def = String::Format("P {0} {1} {2} {3} {4}\n", 1, vCount, fCount, tCount, nCount );
				return def + sw->ToString();
			}

			XbimPolyhedron^ XbimPolyhedron::ToPolyHedron(double deflection, double precision,double precisionMax, unsigned int rounding)
			{
				return this;
			}

			IXbimGeometryModelGroup^ XbimPolyhedron::ToPolyHedronCollection(double deflection, double precision,double precisionMax,unsigned int rounding)
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
				return _meshSet->getAABB().intersects(poly->MeshSet->getAABB(),0);
			}

			XbimMeshFragment XbimPolyhedron::MeshTo(IXbimMeshGeometry3D^ mesh3D, IfcProduct^ product, XbimMatrix3D transform, double deflection, short modelId)
			{
				XbimTriangulatedModelCollection^ triangles = Mesh(deflection);
				XbimMeshFragment fragment(mesh3D->PositionCount,mesh3D->TriangleIndexCount, modelId);
                fragment.EntityLabel = product->EntityLabel;
                fragment.EntityTypeId = IfcMetaData::IfcTypeId(product->GetType());
				
				for each (XbimTriangulatedModel^ tm in triangles) //add each mesh to the collective mesh
				{
					XbimTriangulatedModelStream^ streamer = gcnew XbimTriangulatedModelStream(tm->Triangles);
					XbimMeshFragment f = streamer->BuildWithNormals<IXbimTriangulatesToPositionsNormalsIndices^>((IXbimTriangulatesToPositionsNormalsIndices^)mesh3D,transform,modelId);
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

			XbimGeometryModel^ XbimPolyhedron::Cut(XbimGeometryModel^ shape, double deflection, double precision, double maxPrecision, unsigned int rounding)
			{
				if(this->IsEmpty) return this; //nothing to do
				XbimPolyhedron^ toCut = dynamic_cast<XbimPolyhedron^>(shape);
				if(toCut==nullptr) //we can cut one polyhedron from another
					toCut = shape->ToPolyHedron(deflection,precision,maxPrecision, rounding);
				if(toCut->IsEmpty) return this; //nothing to do
				XbimCsg^ csg = gcnew XbimCsg(precision);						
				try
				{				
					XbimPolyhedron^ result = csg->Subtract(this,toCut);
					if(result->IsValid)
						return result;
					else
						throw gcnew Exception("Boolean result invalid");
				}
				catch (XbimGeometryException^ ex) 
				{
					Logger->ErrorFormat("Failed to cut opening, exception: {0}.\n Body is #{1}, Cut is #{2}.\nCut ignored. This should not happen.", ex->Message, RepresentationLabel, shape->RepresentationLabel);
				}	
				return this;
			}

			XbimGeometryModel^ XbimPolyhedron::Combine(XbimGeometryModel^ shape, double deflection, double precision, double maxPrecision, unsigned int rounding)
			{
				if(this->IsEmpty) return this; //nothing to do
				XbimPolyhedron^ toCombine = dynamic_cast<XbimPolyhedron^>(shape);
				if(toCombine==nullptr) //we can cut one polyhedron from another
					toCombine = shape->ToPolyHedron(deflection,precision,maxPrecision, rounding);
				if(toCombine->IsEmpty) return this; //nothing to do			
				XbimCsg^ csg = gcnew XbimCsg(precision);					
				try
				{				
					XbimPolyhedron^ result = csg->Combine(this,toCombine);
					
					if(result->IsValid)
						return result;
					else
						throw gcnew Exception("Boolean result invalid");
				}
				catch (XbimGeometryException^ ex) 
				{
					
					Logger->ErrorFormat("Failed to combine two shapes, exception: {0}.\n Body is #{1}, Addition is #{2}.\nCombination ignored. This should not happen.", ex->Message, RepresentationLabel, shape->RepresentationLabel);
				}	
				return this;
			}

			XbimGeometryModel^ XbimPolyhedron::Union(XbimGeometryModel^ shape, double deflection, double precision, double maxPrecision, unsigned int rounding)
			{	
				if(this->IsEmpty) return shape; //nothing to do
				XbimPolyhedron^ polyToUnion = dynamic_cast<XbimPolyhedron^>(shape);
				if(polyToUnion==nullptr) //we can cut one polyhedron from another
					polyToUnion = shape->ToPolyHedron(deflection,precision,maxPrecision, rounding);
				if(polyToUnion->IsEmpty) return this; //nothing to do
				XbimCsg^ csg = gcnew XbimCsg(precision);					
				try
				{				
					XbimPolyhedron^ result = csg->Union(this,polyToUnion);
					if(result->IsValid)
						return result;
					else
						throw gcnew Exception("Boolean result invalid");
				}
				catch (XbimGeometryException^ ex) 
				{
					
					Logger->ErrorFormat("Failed to union shapes, exception: {0}.\n Body is #{1}, Union is #{2}.\nUnion ignored. This should not happen.", ex->Message, RepresentationLabel, shape->RepresentationLabel);
				}	
				return this;
			}

			XbimGeometryModel^ XbimPolyhedron::Intersection(XbimGeometryModel^ shape, double deflection, double precision, double maxPrecision, unsigned int rounding)
			{		

				if(this->IsEmpty) return shape; //nothing to do
				XbimPolyhedron^ polyToIntersect = dynamic_cast<XbimPolyhedron^>(shape);			
				if(polyToIntersect==nullptr) //we can cut one polyhedron from another
					polyToIntersect = shape->ToPolyHedron(deflection,precision,maxPrecision, rounding);
				if(polyToIntersect->IsEmpty) return this; //nothing to do
				XbimCsg^ csg = gcnew XbimCsg(precision);					
				try
				{				
					XbimPolyhedron^ result = csg->Intersection(this,polyToIntersect);
					if(result->IsValid)
						return result;
					else
						throw gcnew Exception("Boolean result invalid");
				}
				catch (XbimGeometryException^ ex) 
				{
					
					Logger->ErrorFormat("Failed to intersect shapes, exception: {0}.\n Body is #{1}, Intersect is #{2}.\nIntersection ignored. This should not happen.", ex->Message, RepresentationLabel, shape->RepresentationLabel);
				}	
				return this;
			}

			///true is a manifold closed solid
			bool XbimPolyhedron::IsClosed()
			{		
				return _meshSet!=nullptr && _meshSet->isClosed();
			}

			///inverts all meshes that form this shape
			void XbimPolyhedron::Invert()
			{
				if(_meshSet==nullptr) return;
				for (size_t i = 0; i < _meshSet->meshes.size(); i++)
					_meshSet->meshes[i]->invert();		
			}

			void  XbimPolyhedron::GetConnected(HashSet<XbimPolyhedron^>^ connected, Dictionary<XbimPolyhedron^,HashSet<XbimPolyhedron^>^>^ clusters, XbimPolyhedron^ clusterAround)
			{	
				if(connected->Add(clusterAround))
				{
					for each (KeyValuePair<XbimPolyhedron^,HashSet<XbimPolyhedron^>^>^ polysets in clusters)
					{
						if(!connected->Contains(polysets->Key) && !(polysets->Key==clusterAround) && polysets->Value->Contains(clusterAround))  //don't do the same one twice
						{	
							GetConnected(connected, clusters, polysets->Key);
							for each (XbimPolyhedron^ poly in polysets->Value)
							{
								GetConnected(connected, clusters, poly);
							}
						}
					}
				}
			}

			XbimPolyhedron^  XbimPolyhedron::Merge(List<XbimPolyhedron^>^ toMerge, XbimModelFactors^ mf)
			{
				if(!Enumerable::Any(toMerge)) return nullptr;
				
				//first remove any that intersect as simple merging leads to illegal geometries.
		
				Dictionary<XbimPolyhedron^,HashSet<XbimPolyhedron^>^>^ clusters = gcnew Dictionary<XbimPolyhedron^,HashSet<XbimPolyhedron^>^>();
				for each (XbimPolyhedron^ polyToCheck in toMerge) //init all the clusters
					clusters[polyToCheck]= gcnew HashSet<XbimPolyhedron^>();
				for each (XbimPolyhedron^ polyToCheck in toMerge)
				{
					for each (KeyValuePair<XbimPolyhedron^,HashSet<XbimPolyhedron^>^>^ cluster in clusters)
					{
						if(polyToCheck!=cluster->Key && polyToCheck->GetBoundingBox().Intersects(cluster->Key->GetBoundingBox()))
							cluster->Value->Add(polyToCheck);
					}
				}
	            List<XbimPolyhedron^>^ toMergeReduced = gcnew List<XbimPolyhedron^>();	
				Dictionary<XbimPolyhedron^,HashSet<XbimPolyhedron^>^>^ clustersSparse = gcnew Dictionary<XbimPolyhedron^,HashSet<XbimPolyhedron^>^>();
				for each (KeyValuePair<XbimPolyhedron^,HashSet<XbimPolyhedron^>^>^ cluster in clusters)
				{
					if(cluster->Value->Count>0) 
						clustersSparse->Add(cluster->Key,cluster->Value);
					else
						toMergeReduced->Add(cluster->Key); //record the ones to simply merge
				}
				clusters=nullptr;

				
				XbimPolyhedron^ clusterAround = Enumerable::FirstOrDefault(clustersSparse->Keys);
				
				while(clusterAround!=nullptr)
				{
					HashSet<XbimPolyhedron^>^ connected = gcnew HashSet<XbimPolyhedron^>();
					XbimPolyhedron::GetConnected(connected, clustersSparse,clusterAround);
					XbimPolyhedron^ poly = nullptr;
					for each (XbimPolyhedron^ toConnect in connected) //join up the connected
					{
						if(poly==nullptr) poly = toConnect;
						else poly = (XbimPolyhedron^)poly->Union(toConnect,mf);
					}
					if(poly!=nullptr) toMergeReduced->Add(poly);
					for each (XbimPolyhedron^ poly in connected) //remove what we have conected
						clustersSparse->Remove(poly);
					clusterAround = Enumerable::FirstOrDefault(clustersSparse->Keys);
				}

				//create a map between old and new vertices
 				std::unordered_map<vertex_t *, size_t> vert_idx;
				
				size_t meshCount = 0;
				for each (XbimPolyhedron^ poly in toMergeReduced)
				{	
					meshCount+=poly->MeshSet->meshes.size();
					for(carve::mesh::MeshSet<3U>::face_iter f = poly->MeshSet->faceBegin();f!=poly->MeshSet->faceEnd();++f)
					{
						face_t* face = *f;
						edge_t* e = face->edge;
						do
						{		
							vert_idx[e->vert] = 0;
							e = e->next;
						}
						while(e!=face->edge);
					}
				}
				//determine max number of new vertices
				std::vector<vertex_t> newVertexStorage;
				newVertexStorage.reserve(vert_idx.size());
				//Add indexes and unique vertices in to the new vertex storage
				for (std::unordered_map<vertex_t *, size_t>::iterator
						i = vert_idx.begin(); i != vert_idx.end(); ++i) 
				{
							(*i).second = newVertexStorage.size();
							newVertexStorage.push_back(*(*i).first);
				}
				
				//create meshes and faces
				std::vector<mesh_t*> newMeshes;
				newMeshes.reserve(meshCount);			
			    std::vector<carve::mesh::MeshSet<3>::vertex_t *> faceVerts;
				std::vector<face_t *> faceList;
				for each (XbimPolyhedron^ poly in toMergeReduced)
				{
					for (size_t i = 0; i < poly->MeshSet->meshes.size(); i++)
					{
						mesh_t* mesh = poly->MeshSet->meshes[i];
						faceList.clear();
						faceList.reserve(mesh->faces.size());						
						for(std::vector<face_t*>::iterator f = mesh->faces.begin();f!=mesh->faces.end();++f)
						{
							face_t* face = *f;
							edge_t* e = face->edge;
							std::vector<carve::mesh::MeshSet<3>::vertex_t *> faceVerts;
							faceVerts.clear();
							do
							{
								faceVerts.push_back(&(newVertexStorage[vert_idx[e->vert]]));
								e = e->next;
							}
							while(e!=face->edge);
							faceList.push_back(new face_t(faceVerts.begin(), faceVerts.end()));
						}
						std::vector<mesh_t*> nextMeshes;
						mesh_t::create(faceList.begin(), faceList.end(), nextMeshes, carve::mesh::MeshOptions(), false);
						for (size_t i = 0; i < nextMeshes.size(); i++)
							newMeshes.push_back(nextMeshes[i]);
					}
				}
				meshset_t* meshSet = new meshset_t(newVertexStorage, newMeshes);
				//create a new Poly using representation and style of first one by default
				XbimPolyhedron^ result = gcnew XbimPolyhedron(meshSet,toMerge[0]->RepresentationLabel,toMerge[0]->SurfaceStyleLabel);
				
				return result;
			}


			//Implementation of the IXbimPolyhedron interface
			bool XbimPolyhedron::WritePly(String^ fileName, bool ascii)
			{
				return IsValid &&  WritePlyInternal(fileName,ascii);
			}

			XbimPoint3D XbimPolyhedron::Vertex(int i)
			{
				if(_meshSet!=nullptr)
				{
					vertex_t v = _meshSet->vertex_storage.at(i);
					return XbimPoint3D(v.v.x,v.v.y,v.v.z);
				}
				else
					return XbimPoint3D();
			}

			IList<Int32>^ XbimPolyhedron::Triangulation(double precision)
			{
				if(!IsValid) return gcnew List<Int32>(0);
				List<Int32>^ indices = gcnew List<Int32>(this->FaceCount*5);
				for (meshset_t::face_iter f = _meshSet->faceBegin(), e = _meshSet->faceEnd(); f != e; ++f) 
				{
					face_t *face = *f;
					int numVertices = face->nVertices();
					if(numVertices>=3)
					{
						std::vector<carve::mesh::MeshSet<3>::vertex_t *> verts;
						face->getVertices(verts);
						if(verts.size()==3) //it is a triangular face
						{
							indices->Add((Int32) carve::poly::ptrToIndex_fast(_meshSet->vertex_storage,verts[0]));
							indices->Add((Int32) carve::poly::ptrToIndex_fast(_meshSet->vertex_storage,verts[1]));
							indices->Add((Int32) carve::poly::ptrToIndex_fast(_meshSet->vertex_storage,verts[2]));	
						}
						else //need to triangulate
						{
							std::vector<carve::geom::vector<2> > projectedVerts;
							face->getProjectedVertices(projectedVerts);
							std::vector<carve::triangulate::tri_idx> result;
							carve::triangulate::triangulate(projectedVerts,result,precision);	
							for (size_t i = 0; i < result.size(); i++)
							{	
								indices->Add((Int32) carve::poly::ptrToIndex_fast(_meshSet->vertex_storage,verts[result[i].a]));
								indices->Add((Int32) carve::poly::ptrToIndex_fast(_meshSet->vertex_storage,verts[result[i].b]));
								indices->Add((Int32) carve::poly::ptrToIndex_fast(_meshSet->vertex_storage,verts[result[i].c]));	
							}	
						}
					}
				}
				return indices;
			}
		}

		
	}
}