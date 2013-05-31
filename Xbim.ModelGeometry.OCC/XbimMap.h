#pragma once
#include "XbimGeometryModel.h"
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
		public ref class XbimMap : XbimGeometryModel
		{
		private:
			XbimGeometryModel^ _mappedItem;
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
			XbimMap(XbimGeometryModel^ item, IfcAxis2Placement^ origin, IfcCartesianTransformationOperator^ transform, ConcurrentDictionary<int,Object^>^ maps);
			virtual XbimGeometryModel^ Cut(XbimGeometryModel^ shape) override;
			virtual XbimGeometryModel^ Union(XbimGeometryModel^ shape) override;
			virtual XbimGeometryModel^ Intersection(XbimGeometryModel^ shape) override;
			virtual XbimGeometryModel^ CopyTo(IfcObjectPlacement^ placement) override;
			virtual void Move(TopLoc_Location location) override;
			virtual property bool HasCurvedEdges
			{
				virtual bool get() override //this geometry has the same curved edges as the object it maps
				{
					return _mappedItem->HasCurvedEdges;
				}
			}
			

			virtual XbimRect3D GetBoundingBox()  override
			{
				return _mappedItem->GetBoundingBox();
			};


			virtual property XbimMatrix3D Transform
			{
				XbimMatrix3D get() override
				{
					return _transform;
				}
			}
			
			property XbimGeometryModel^ MappedItem
			{
				XbimGeometryModel^ get()
				{
					return _mappedItem;
				}
			}
			virtual List<XbimTriangulatedModel^>^Mesh(bool withNormals, double deflection) override;
				
			virtual property double Volume
			{
				double get() override
				{
					return _mappedItem->Volume;
				}
			}

			virtual property XbimLocation ^ Location 
			{
				XbimLocation ^ get() override
				{
					throw gcnew NotImplementedException("Location needs to be implemented");
				}
				void set(XbimLocation ^ location) override
				{
					throw gcnew NotImplementedException("Location needs to be implemented");
				}
			}

			virtual property TopoDS_Shape* Handle
			{
				TopoDS_Shape* get() override
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

