#pragma once

#include "XbimLocation.h"
#include "XbimBoundingBox.h"
#include "IXbimMeshGeometry.h"
#include "IXbimGeometryModel.h"
#include <TopoDS_Shape.hxx>
#include <TopoDS_Compound.hxx>
#include <TopTools_DataMapOfShapeInteger.hxx>
#include <TopTools_IndexedMapOfShape.hxx>
using namespace Xbim::Ifc2x3::GeometryResource;
using namespace Xbim::XbimExtensions::SelectTypes;
using namespace Xbim::Ifc2x3::GeometricModelResource;
using namespace Xbim::Ifc2x3::GeometricConstraintResource;
using namespace Xbim::Ifc2x3::RepresentationResource;
using namespace Xbim::Ifc2x3::Kernel;
using namespace System;
using namespace System::Collections::Generic;
using namespace Xbim::ModelGeometry::Scene;
using namespace Xbim::Common::Logging;
using namespace Xbim::Ifc2x3::SharedBldgElements;
using namespace System::Collections::Concurrent;
namespace Xbim
{
	namespace ModelGeometry
	{	

		namespace OCC
		{
#pragma unmanaged

void CALLBACK XMS_BeginTessellate(GLenum type, void *pPolygonData);
void CALLBACK XMS_EndTessellate(void *pVertexData);
void CALLBACK XMS_TessellateError(GLenum err);
void CALLBACK XMS_AddVertexIndex(void *pVertexData, void *pPolygonData);
		
#pragma managed

		
		public ref class XbimGeometryModel abstract 
		{
			
		private:
			static int _callStaticConstructor; //we need this to ensure the static constructor is called
			static ILogger^ Logger = LoggerFactory::GetLogger();
			
		public:
			
			static int DefaultDeflection = 4;
			static XbimGeometryModel(void)
			{
				Init();
				
			}
			static void Init()
			{
				Standard::SetReentrant(Standard_True);
				
			}
			
			virtual XbimTriangulatedModelStream^ Triangulate(IfcProduct^ product) abstract;

			
			XbimGeometryModel(void){};
			virtual List<XbimTriangulatedModel^>^Mesh(bool withNormals, double deflection, XbimMatrix3D transform) abstract;
			virtual List<XbimTriangulatedModel^>^Mesh(bool withNormals, double deflection) abstract;
			virtual List<XbimTriangulatedModel^>^Mesh(bool withNormals) abstract;
			virtual List<XbimTriangulatedModel^>^Mesh() abstract;
			property TopoDS_Shape* Handle{ virtual TopoDS_Shape* get() abstract;};
			virtual IXbimGeometryModel^ Cut(IXbimGeometryModel^ shape) abstract;
			virtual IXbimGeometryModel^ Union(IXbimGeometryModel^ shape)abstract;
			virtual IXbimGeometryModel^ Intersection(IXbimGeometryModel^ shape)abstract;
			virtual IXbimGeometryModel^ CopyTo(IfcObjectPlacement^ placement) abstract;
			property XbimLocation^ Location {virtual XbimLocation ^ get() abstract; virtual void set(XbimLocation ^ location) abstract;};

			static IXbimGeometryModel^ CreateFrom(IfcProduct^ product, IfcGeometricRepresentationContext^ repContext, ConcurrentDictionary<int, Object^>^ maps, bool forceSolid, XbimLOD lod, bool occOut);
			static IXbimGeometryModel^ CreateFrom(IfcProduct^ product, ConcurrentDictionary<int, Object^>^ maps, bool forceSolid, XbimLOD lod, bool occOut);
			static IXbimGeometryModel^ CreateFrom(IfcProduct^ product, bool forceSolid, XbimLOD lod, bool occOut);


			static IXbimGeometryModel^ CreateFrom(IfcRepresentationItem^ repItem, bool forceSolid, XbimLOD lod, bool occOut);
			static IXbimGeometryModel^ CreateFrom(IfcRepresentationItem^ repItem, ConcurrentDictionary<int, Object^>^ maps, bool forceSolid, XbimLOD lod, bool occOut);
			static IXbimGeometryModel^ CreateFrom(IfcRepresentation^ shape, ConcurrentDictionary<int, Object^>^ maps, bool forceSolid, XbimLOD lod, bool occOut);
			static IXbimGeometryModel^ CreateFrom(IfcRepresentation^ shape, bool forceSolid, XbimLOD lod, bool occOut);
			
			
			static IXbimGeometryModel^ Fix(IXbimGeometryModel^ shape);
			static List<XbimTriangulatedModel^>^Mesh(IXbimGeometryModel^ shape,  bool withNormals, double deflection, XbimMatrix3D transform );
			virtual XbimBoundingBox^ GetBoundingBox(bool precise) abstract;
			static XbimBoundingBox^ GetBoundingBox(IXbimGeometryModel^ shape, bool precise);
			property double Volume {virtual double get()abstract;};
			property bool HasCurvedEdges{virtual bool get() abstract;};
			
			static bool CutOpenings(IfcProduct^ product, XbimLOD lod);
		public:
			//Builds a TopoDS_Compound from a ShellBasedSurfaceModel
			static IXbimGeometryModel^ Build(IfcShellBasedSurfaceModel^ repItem, bool forceSolid);
			static IXbimGeometryModel^ Build(IfcFaceBasedSurfaceModel^ repItem, bool forceSolid);
			//static IXbimGeometryModel^ Build(IfcBooleanResult^ repItem);
			
			
		};
	}	
}
}
