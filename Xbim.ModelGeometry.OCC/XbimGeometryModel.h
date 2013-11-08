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

typedef void (__stdcall *GLUTessCallback)();	

void __stdcall XMS_AddVertexIndex(void * pVertexData, void * pPolygonData);
void __stdcall XMS_BeginTessellate(GLenum type, void *pPolygonData);
void __stdcall XMS_EndTessellate(void *pVertexData);
void __stdcall XMS_TessellateError(GLenum err);

void __stdcall CGS_BeginTessellate(GLenum type, void *pPolygonData);
void __stdcall CGS_EndTessellate(void *pPolygonData);
void __stdcall CGS_AddVertexIndex(void *pVertexData, void *pPolygonData);
#pragma managed
ref class XbimPolyhedron;


public ref class XbimGeometryModel abstract :  public IXbimGeometryModel, public IXbimGeometryModelGroup
{
	
private:
	ref struct enumerator : IEnumerator<IXbimGeometryModel^>
			{
				enumerator( IXbimGeometryModel^ gm )
				{
					colInst = gm;
				}

				virtual bool MoveNext() = IEnumerator<IXbimGeometryModel^>::MoveNext
				{
					if( currentIndex == 0 )
					{
						currentIndex++;
						return true;
					}
					return false;
				}

				property IXbimGeometryModel^ Current
				{
					virtual IXbimGeometryModel^ get() = IEnumerator<IXbimGeometryModel^>::Current::get
					{
						return colInst;
					}
				};
				// This is required as IEnumerator<T> also implements IEnumerator
				property Object^ Current2
				{
					virtual Object^ get() = System::Collections::IEnumerator::Current::get
					{
						return colInst;
					}
				};

				virtual void Reset() = IEnumerator<IXbimGeometryModel^>::Reset {currentIndex = -1;}
				~enumerator() {}

				IXbimGeometryModel^ colInst;
				int currentIndex;
			};

		public:		
			literal String^ PolyhedronFormat = "PLY";

			virtual IEnumerator<IXbimGeometryModel^>^ GetEnumerator()
			{
				return gcnew enumerator(this);
			}

			virtual System::Collections::IEnumerator^ GetEnumerator2() = System::Collections::IEnumerable::GetEnumerator
			{
				return gcnew enumerator(this);
			}

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

			virtual IXbimGeometryModelGroup^ AsPolyhedron(double deflection, double precision,double precisionMax);
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
			virtual  IXbimMeshGeometry3D^ TriangulatedMesh(double deflection);
			
#if USE_CARVE
			virtual IXbimGeometryModelGroup^ ToPolyHedronCollection(double deflection, double precision,double precisionMax) abstract ;
			virtual XbimPolyhedron^ ToPolyHedron(double deflection, double precision,double precisionMax) ;
			virtual String^ WriteAsString();
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
			
			virtual XbimRect3D GetAxisAlignedBoundingBox();
			
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
