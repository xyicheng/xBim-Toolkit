#pragma once

#include "XbimLocation.h"
#include "XbimBoundingBox.h"
#include "IXbimMeshGeometry.h"
#include "XbimGeometryModel.h"
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


public ref class XbimGeometryModel abstract : IXbimGeometryModel
		{
			
		private:
			static ILogger^ Logger = LoggerFactory::GetLogger();
		protected:
			Int32 _representationLabel;
			Int32 _surfaceStyleLabel;
			static Object^ resourceLock = gcnew Object();
		public:
			
			static int DefaultDeflection = 4;
			
			XbimGeometryModel(void){};
			virtual List<XbimTriangulatedModel^>^Mesh(bool withNormals, double deflection);
			virtual List<XbimTriangulatedModel^>^Mesh(bool withNormals);
			virtual List<XbimTriangulatedModel^>^Mesh();
			property TopoDS_Shape* Handle{ virtual TopoDS_Shape* get() abstract;};
			virtual XbimGeometryModel^ Cut(XbimGeometryModel^ shape) abstract;
			virtual XbimGeometryModel^ Union(XbimGeometryModel^ shape)abstract;
			virtual XbimGeometryModel^ Intersection(XbimGeometryModel^ shape)abstract;
			virtual XbimGeometryModel^ CopyTo(IfcObjectPlacement^ placement) abstract;
			virtual property XbimLocation^ Location 
			{
				XbimLocation ^ get() abstract; 
				void set(XbimLocation ^ location) abstract;
			};
		    virtual property Int32 RepresentationLabel
			{
				Int32 get()  
				{return _representationLabel; }
				void set(Int32 value)  
				{ _representationLabel=value; }
			}

			virtual property Int32 SurfaceStyleLabel
			{
				Int32 get()  
				{return _surfaceStyleLabel; }
				void set(Int32 value) 
				{ _surfaceStyleLabel=value; }
			}

			virtual property XbimMatrix3D Transform
			{
				XbimMatrix3D get() abstract;
			}
			virtual property bool IsMap
			{
				bool get() ;
			}
			virtual void Move(TopLoc_Location location) abstract;

			static XbimGeometryModel^ Fix(XbimGeometryModel^ shape);
			
			virtual XbimRect3D GetBoundingBox();
			virtual bool Intersects(XbimGeometryModel^ other);
			virtual property double Volume 
			{
				virtual double get() abstract;
			};
			property bool HasCurvedEdges{virtual bool get() abstract;};
			
		
			
		};
	}	
}
}
