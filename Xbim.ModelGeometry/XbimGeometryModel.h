#pragma once
#include <list>
#include "XbimTriangularMeshStreamer.h"
#include "XbimLocation.h"
#include "XbimBoundingBox.h"
#include "IXbimMeshGeometry.h"
#include "IXbimGeometryModel.h"
#include <TopoDS_Shape.hxx>
#include <TopoDS_Compound.hxx>
#include <TopTools_DataMapOfShapeInteger.hxx>
#include <TopTools_IndexedMapOfShape.hxx>
using namespace Xbim::Ifc::GeometryResource;
using namespace Xbim::Ifc::SelectTypes;
using namespace Xbim::Ifc::GeometricModelResource;
using namespace Xbim::Ifc::GeometricConstraintResource;
using namespace Xbim::Ifc::RepresentationResource;
using namespace Xbim::Ifc::Kernel;
using namespace System;
using namespace System::Collections::Generic;
using namespace Xbim::ModelGeometry::Scene;

#pragma unmanaged

void CALLBACK XMS_BeginTessellate(GLenum type, void *pPolygonData);
void CALLBACK XMS_EndTessellate(void *pVertexData);
void CALLBACK XMS_TessellateError(GLenum err);
void CALLBACK XMS_AddVertexIndex(void *pVertexData, void *pPolygonData);
		
#pragma managed

namespace Xbim
{
	namespace ModelGeometry
	{	
		public ref class XbimGeometryModel abstract 
		{
			static private int _callStaticConstructor; //we need this to ensure the static constructor is called
		private:
			static void Test(XbimTriangularMeshStreamer* v1);
		public:
			
			static int DefaultDeflection = 4;
			static XbimGeometryModel(void)
			{
				Init();
			}
			static public void Init()
			{
				Standard::SetReentrant(Standard_True);
			}
			
			virtual XbimTriangulatedModelStream^ Triangulate(IfcProduct^ product) abstract;

			
			XbimGeometryModel(void);
			virtual XbimTriangulatedModelStream^ Mesh(bool withNormals, double deflection, Matrix3D transform) abstract;
			virtual XbimTriangulatedModelStream^ Mesh(bool withNormals, double deflection) abstract;
			virtual XbimTriangulatedModelStream^ Mesh(bool withNormals) abstract;
			virtual XbimTriangulatedModelStream^ Mesh() abstract;
			property TopoDS_Shape* Handle{ virtual TopoDS_Shape* get() abstract;};
			virtual IXbimGeometryModel^ Cut(IXbimGeometryModel^ shape) abstract;
			virtual IXbimGeometryModel^ Union(IXbimGeometryModel^ shape)abstract;
			virtual IXbimGeometryModel^ Intersection(IXbimGeometryModel^ shape)abstract;
			virtual IXbimGeometryModel^ CopyTo(IfcObjectPlacement^ placement) abstract;
			property XbimLocation^ Location {virtual XbimLocation ^ get() abstract; virtual void set(XbimLocation ^ location) abstract;};
			static IXbimGeometryModel^ CreateFrom(IfcRepresentationItem^ repItem, bool forceSolid);
			static IXbimGeometryModel^ CreateFrom(IfcRepresentationItem^ repItem, Dictionary<IfcRepresentation^, IXbimGeometryModel^>^ maps, bool forceSolid);
			static IXbimGeometryModel^ CreateFrom(IfcRepresentation^ shape, Dictionary<IfcRepresentation^, IXbimGeometryModel^>^ maps, bool forceSolid);
			static IXbimGeometryModel^ CreateFrom(IfcRepresentation^ shape, bool forceSolid);
			static IXbimGeometryModel^ CreateFrom(IfcProduct^ product, IfcGeometricRepresentationContext^ repContext, Dictionary<IfcRepresentation^, IXbimGeometryModel^>^ maps, bool forceSolid);
			static IXbimGeometryModel^ CreateFrom(IfcProduct^ product, Dictionary<IfcRepresentation^, IXbimGeometryModel^>^ maps, bool forceSolid);
			static IXbimGeometryModel^ CreateFrom(IfcProduct^ product, bool forceSolid);
			static IXbimGeometryModel^ CreateMap(IXbimGeometryModel^ item, IfcAxis2Placement^ origin, IfcCartesianTransformationOperator^ transform, Dictionary<IfcRepresentation^, IXbimGeometryModel^>^ maps, bool forceSolid);
			static IXbimGeometryModel^ CreateMap(IXbimGeometryModel^ item, IfcAxis2Placement^ origin, Dictionary<IfcRepresentation^, IXbimGeometryModel^>^ maps, bool forceSolid);
			static IXbimGeometryModel^ CreateMap(IXbimGeometryModel^ item, Dictionary<IfcRepresentation^, IXbimGeometryModel^>^ maps, bool forceSolid);
			static IXbimGeometryModel^ Fix(IXbimGeometryModel^ shape);
			static XbimTriangulatedModelStream^ Mesh(IXbimGeometryModel^ shape,  bool withNormals, double deflection, Matrix3D transform );
			virtual XbimBoundingBox^ GetBoundingBox(bool precise) abstract;
			static XbimBoundingBox^ GetBoundingBox(IXbimGeometryModel^ shape, bool precise);
			property double Volume {virtual double get()abstract;};
			property bool HasCurvedEdges{virtual bool get() abstract;};
			
			
		public:
			//Builds a TopoDS_Compound from a ShellBasedSurfaceModel
			static IXbimGeometryModel^ Build(IfcShellBasedSurfaceModel^ repItem, bool forceSolid);
			static IXbimGeometryModel^ Build(IfcFaceBasedSurfaceModel^ repItem, bool forceSolid);
			static IXbimGeometryModel^ Build(IfcBooleanResult^ repItem);
			
			
		};
	}	
}
