#pragma once
#include "IXbimGeometryModel.h"
#include "XbimGeometryModel.h"
#include "XbimSolid.h"
using namespace Xbim::Ifc2x3::GeometryResource;
using namespace Xbim::Ifc2x3::TopologyResource;
namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
		public ref class XbimMap : IXbimGeometryModel
		{
		private:
			IXbimGeometryModel^ _mappedItem;
			XbimMatrix3D _transform;
			Int32 _representationLabel;
			Int32 _surfaceStyleLabel;
		public:
				~XbimMap()
				{
					InstanceCleanup();
				}

				!XbimMap()
				{
					InstanceCleanup();
				}
				void InstanceCleanup()
				{   
					_mappedItem=nullptr;
				}
			XbimMap(IXbimGeometryModel^ item, IfcAxis2Placement^ origin, IfcCartesianTransformationOperator^ transform, ConcurrentDictionary<int,Object^>^ maps);
			virtual IXbimGeometryModel^ Cut(IXbimGeometryModel^ shape);
			virtual IXbimGeometryModel^ Union(IXbimGeometryModel^ shape);
			virtual IXbimGeometryModel^ Intersection(IXbimGeometryModel^ shape);
			virtual IXbimGeometryModel^ CopyTo(IfcObjectPlacement^ placement);
			virtual void Move(TopLoc_Location location);
			virtual property bool HasCurvedEdges
			{
				virtual bool get() //this geometry has the same curved edges as the object it maps
				{
					return _mappedItem->HasCurvedEdges;
				}
			}
			virtual XbimBoundingBox^ GetBoundingBox(bool precise)
			{
				return _mappedItem->GetBoundingBox(precise);
			};
						
			virtual property Int32 RepresentationLabel
			{
				Int32 get(){return _representationLabel; }
				void set(Int32 value){ _representationLabel=value; }
			}

			virtual property Int32 SurfaceStyleLabel
			{
				Int32 get(){return _surfaceStyleLabel; }
				void set(Int32 value){ _surfaceStyleLabel=value; }
			}

			property XbimMatrix3D Transform
			{
				XbimMatrix3D get()
				{
					return _transform;
				}
			}
			
			property IXbimGeometryModel^ MappedItem
			{
				IXbimGeometryModel^ get()
				{
					return _mappedItem;
				}
			}
			virtual List<XbimTriangulatedModel^>^Mesh(bool withNormals, double deflection, XbimMatrix3D transform);
			virtual List<XbimTriangulatedModel^>^Mesh(bool withNormals, double deflection);
			virtual List<XbimTriangulatedModel^>^Mesh(bool withNormals);
			virtual List<XbimTriangulatedModel^>^Mesh();
			virtual property double Volume
			{
				double get()
				{
					return _mappedItem->Volume;
				}
			}

			virtual property XbimLocation ^ Location 
			{
				XbimLocation ^ get()
				{
					throw gcnew NotImplementedException("Location needs to be implemented");
				}
				void set(XbimLocation ^ location)
				{
					throw gcnew NotImplementedException("Location needs to be implemented");
				}
			}

			virtual property TopoDS_Shape* Handle
			{
				TopoDS_Shape* get()
				{
					
					if(!_transform.IsIdentity) //see if we need to map
					{
						XbimSolid^ solid = gcnew XbimSolid(_mappedItem,_transform);
						_transform = XbimMatrix3D::Identity; //Matrix no longer should be applied it has been applied to the mapped geometry
						_mappedItem=solid;
					}
					return _mappedItem->Handle;
				}
			}

			
		};
	}
	}

}

