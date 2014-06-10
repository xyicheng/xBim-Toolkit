#pragma once

#include "XbimGeometryModel.h"
#include <carve\polyhedron_decl.hpp>
#include <carve\input.hpp>
#include <carve\csg.hpp>
#include <carve/carve.hpp>
#include <carve/collection_types.hpp>
#include "CarveCsg\common\write_ply.hpp"
#ifndef WIN32
#  include <stdint.h>
#endif



using namespace Xbim::ModelGeometry::Scene;
using namespace System::Collections::Generic;
using namespace Xbim::Common::Geometry;
using namespace Xbim::Common::Logging;
using namespace Xbim::Ifc2x3::GeometryResource;
using namespace Xbim::Ifc2x3::GeometricConstraintResource;
using namespace Xbim::ModelGeometry::Scene;
using namespace Xbim::Ifc2x3::TopologyResource;
using namespace Xbim::XbimExtensions;
typedef std::pair<const TopoDS_Edge, const TopoDS_Vertex*> edgeVertexPair_t;
typedef std::vector<edgeVertexPair_t> edgeVertexPairList_t;
typedef std::unordered_map<const TopoDS_Vertex*, edgeVertexPairList_t> edgeMap_t;
typedef carve::poly::Polyhedron poly_t;
typedef carve::mesh::MeshSet<3> meshset_t;
typedef carve::mesh::Mesh<3> mesh_t;
typedef mesh_t::vertex_t vertex_t;
typedef mesh_t::edge_t edge_t;
typedef mesh_t::face_t face_t;
typedef face_t::aabb_t aabb_t;
typedef carve::geom::plane<3U> plane_t;
typedef carve::geom::vector<3U> vector_t;
typedef carve::geom::RTreeNode<3, carve::mesh::Face<3> *> face_rtree_t;

namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
			ref class XbimGeometryModel;
			public class XbimPolyhedronMeshStreamer
			{
			public: 
				XbimPolyhedronMeshStreamer(double precision);
				void BeginPolygon(GLenum type);
				void EndPolygon();
				void WriteTriangleIndex(size_t index);
				
				size_t WritePoint(IfcCartesianPoint^ p);
				carve::csg::CSG::meshset_t*  GetPolyhedron();
				carve::input::PolyhedronData * XbimPolyhedronMeshStreamer::GetPolyData();
			private:
				carve::input::PolyhedronData polyData;
				size_t _pointTally;
				size_t _fanStartIndex;
				size_t _previousToLastIndex;
				size_t _lastIndex;
				GLenum _meshType;
				std::vector<size_t> _indices;
				double _precision;
			};


			public ref class XbimPolyhedron : public XbimGeometryModel, public IXbimPolyhedron
			{
			private:
				static Object^ resourceLock = gcnew Object();
				
				carve::csg::CSG::meshset_t* _meshSet;
				~XbimPolyhedron()
				{
					InstanceCleanup();
				}

				!XbimPolyhedron()
				{
					InstanceCleanup();
				}

			protected:;
					  void DeletePolyhedron(void);
					  virtual void InstanceCleanup() override;
					  void AddFaces(std::vector<vertex_t>& vertices, std::vector<mesh_t*>& meshes, double precision, unsigned int rounding, IEnumerable<IfcFace^>^ faces, bool orientate);
			          void ResizeVertexStore(size_t newSize);
			
			public:
				XbimPolyhedron(void);
				//Constructs geometry based on  ascii data, if asTriangulation is true the triangulated mesh is created else a polyhedron mesh is created
				XbimPolyhedron(String^ strData, bool asTriangulation);
				//create and empty polyhedron
				XbimPolyhedron(int representationLabel, int styleLabel);
				XbimPolyhedron(carve::csg::CSG::meshset_t* mesh, int representationLabel, int styleLabel);
				XbimPolyhedron(XbimModelFactors^ modelFactors, IEnumerable<IfcFace^>^ faces, int representationLabel, int styleLabel, bool orientate);
				XbimPolyhedron(IfcConnectedFaceSet^ faces,double precision, unsigned int rounding, int representationLabel, int styleLabel, bool orientate);
				XbimPolyhedron(IfcOpenShell^ shell,double precision, unsigned int rounding, int representationLabel, int styleLabel, bool orientate);
				XbimPolyhedron(IfcClosedShell^ shell,double precision, unsigned int rounding, int representationLabel, int styleLabel, bool orientate);
				XbimPolyhedron(IfcShellBasedSurfaceModel^ sbsm, double precision, unsigned int rounding, int representationLabel, int styleLabel, bool orientate);
				XbimPolyhedron(IfcFaceBasedSurfaceModel^ fbsm, double precision, unsigned int rounding, int representationLabel, int styleLabel, bool orientate);
				XbimPolyhedron(IfcShell^ shell, double precision, unsigned int rounding, int representationLabel, int styleLabel, bool orientate);
				XbimPolyhedron(IfcFacetedBrep^ brep, double precision, unsigned int rounding, int representationLabel, int styleLabel, bool orientate);
				//properties
				virtual property double Volume 
				{
					double get() override;
				}
				virtual property
					int FaceCount 
				{
					int get();
				}

				virtual property int VertexCount 
				{
					int get();
				}

				//funtions
				virtual XbimGeometryModel^ CopyTo(IfcAxis2Placement^ placement) override;

				virtual XbimGeometryModel^ Cut(XbimGeometryModel^ shape,double deflection, double precision, double maxPrecision, unsigned int rounding) override;
				virtual XbimGeometryModel^ Union(XbimGeometryModel^ shape, double deflection, double precision, double maxPrecision, unsigned int rounding) override;
				virtual XbimGeometryModel^ Intersection(XbimGeometryModel^ shape, double deflection, double precision, double maxPrecision, unsigned int rounding) override;
				virtual XbimGeometryModel^ Combine(XbimGeometryModel^ shape, double deflection, double precision, double maxPrecision, unsigned int rounding) override;
				void AddFaces(IfcConnectedFaceSet^ faces, double precision, unsigned int rounding, bool orientate);
				void AddFaces(IfcFaceBasedSurfaceModel^ faceSets, double precision, unsigned int rounding, bool orientate);
				void AddFaces(IfcShellBasedSurfaceModel^ shellSets, double precision, unsigned int rounding, bool orientate);
				void AddFaces(IfcShell^ shell, double precision, unsigned int rounding, bool orientate);
				void AddFaces(IfcOpenShell^ shell, double precision, unsigned int rounding, bool orientate);
				void AddFaces(IfcClosedShell^ shell, double precision, unsigned int rounding, bool orientate);
				void AddFaces(IfcFacetedBrep^ brep, double precision, unsigned int rounding, bool orientate);

				virtual property meshset_t* MeshSet
				{
					meshset_t* get(){return _meshSet;}
				}
				virtual void TransformBy(XbimMatrix3D transform) override;
				virtual XbimRect3D GetBoundingBox() override;
				virtual property bool IsEmpty {bool get();}
				virtual property bool IsValid {bool get() override;}
				bool Intersects(XbimPolyhedron^ poly);
				virtual void ToSolid(double precision, double maxPrecision) override; 
				virtual IXbimGeometryModelGroup^ ToPolyHedronCollection(double deflection, double precision,double precisionMax, unsigned int rounding) override;
				virtual XbimTriangulatedModelCollection^ Mesh(double deflection) override;
				virtual XbimPolyhedron^ ToPolyHedron(double deflection, double precision,double precisionMax, unsigned int rounding) override ;
				virtual String^ WriteAsString(XbimModelFactors^ modelFactors) override;
				void WritePly(String^ fileName, bool ascii);
				void WriteObj(String^ fileName);
				void WriteVtk(String^ fileName);
				virtual bool Write(String^ fileName,XbimModelFactors^ modelFactors) override;

				virtual XbimMeshFragment MeshTo(IXbimMeshGeometry3D^ mesh3D, IfcProduct^ product, XbimMatrix3D transform, double deflection, short modelId) override;
				size_t RemoveFins();
				size_t EliminateShortEdges(double minLength,double EPSILON);
				size_t Simplify( double min_colinearity,
										  double min_delta_v,
										  double min_normal_angle,
										  double min_length,
										  double EPSILON);
				size_t Improve( double min_colinearity,
										  double min_delta_v,
										  double min_normal_angle,
										  double min_length,
										  double EPSILON);
				bool IsClosed();
			};
		}
	}

}