#pragma once
			
#include <BRepGProp.hxx>
#include <GProp_GProps.hxx> 
#include "XbimLocation.h"
#include "XbimBoundingBox.h"
#include "XbimGeomPrim.h"
#include "IXbimMeshGeometry.h"
#include <TopoDS_Shape.hxx>
#include <TopoDS_Compound.hxx>
#include <TopTools_DataMapOfShapeInteger.hxx>
#include <TopTools_IndexedMapOfShape.hxx>
#include <TopoDS.hxx>
#include <BRepTools.hxx>
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
using namespace Xbim::Common::Logging;


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
void CALLBACK CGS_BeginTessellate(GLenum type, void *pPolygonData);
void CALLBACK CGS_EndTessellate(void *pPolygonData);
void CALLBACK CGS_AddVertexIndex(void *pVertexData, void *pPolygonData);
#pragma managed

ref class XbimPolyhedron;
public ref class XbimGeometryModel abstract : IXbimGeometryModel
		{
			
		private:
		protected:
			TopoDS_Shape* nativeHandle;
			Int32 _representationLabel;
			Int32 _surfaceStyleLabel;	
			XbimRect3D _bounds;
			bool _hasCurvedEdges;
			static ILogger^ Logger = LoggerFactory::GetLogger();
			virtual void InstanceCleanup()
			{   
				IntPtr temp = System::Threading::Interlocked::Exchange(IntPtr(nativeHandle), IntPtr(0));
				if(temp!=IntPtr(0))
				{
					if (nativeHandle)
					{
						delete nativeHandle;
						nativeHandle=0;
						System::GC::SuppressFinalize(this);
					}
				}
			};
		public:

			XbimGeometryModel(){_bounds=XbimRect3D::Empty;};
			void Init(const TopoDS_Shape&  shape, bool hasCurves,int representationLabel, int surfaceStyleLabel );
			void Init(IfcRepresentationItem^ entity);
			virtual property bool IsValid
			{
				bool get() 
				{
					return nativeHandle!=nullptr && !nativeHandle->IsNull();
				}
			}
			virtual property bool HasCurvedEdges
			{
				virtual bool get() 
				{
					return _hasCurvedEdges;
				}
			};
			virtual property TopoDS_Shape* Handle
			{
				TopoDS_Shape* get() 
				{
					return nativeHandle;
				};			
			};
			virtual XbimTriangulatedModelCollection^ Mesh(double deflection);
			virtual XbimGeometryModel^ Cut(XbimGeometryModel^ shape, double precision, double maxPrecision);
			virtual XbimGeometryModel^ Union(XbimGeometryModel^ shape, double precision, double maxPrecision);
			virtual XbimGeometryModel^ Intersection(XbimGeometryModel^ shape, double precision, double maxPrecision);
			virtual XbimGeometryModel^ CopyTo(	IfcAxis2Placement^ placement) abstract;
			virtual void ToSolid(double precision, double maxPrecision) abstract;
#if USE_CARVE
			virtual XbimPolyhedron^ ToPolyHedron(double deflection, double precision,double precisionMax) ;
#endif
			virtual property XbimLocation ^ Location 
			{
				XbimLocation ^ get() 
				{
					return gcnew XbimLocation(Handle->Location());
				}
				void set(XbimLocation ^ location) 
				{
					Handle->Location(*(location->Handle));;
				}
			};

			virtual property Int32 RepresentationLabel
			{
				Int32 get()  
				{
					return _representationLabel;
				}
				void set(Int32 value)  
				{ 
					_representationLabel=value; 
				}
			}

			virtual property Int32 SurfaceStyleLabel
			{
				Int32 get()  
				{
					return _surfaceStyleLabel; 
				}
				void set(Int32 value) 
				{ 
					_surfaceStyleLabel=value;
				}
			}

			virtual property bool IsMap
			{
				bool get();
			}

			virtual void XbimGeometryModel::Move(TopLoc_Location location)
			{
				(*Handle).Move(location);	
			}

			virtual XbimRect3D GetBoundingBox();
			virtual bool Intersects(XbimGeometryModel^ other);

			virtual property double Volume
			{
				double get()  
				{
					if(nativeHandle!=nullptr)
					{
						GProp_GProps System;
						BRepGProp::VolumeProperties(*Handle, System, Standard_True);
						return System.Mass();
					}
					else
						return 0;
				}
			}
			virtual property XbimMatrix3D Transform
				{
					XbimMatrix3D get() 
					{
						return XbimGeomPrim::ToMatrix3D( Handle->Location());
						//return XbimMatrix3D::Identity;
					}
				}
		};
	}	
}
}
