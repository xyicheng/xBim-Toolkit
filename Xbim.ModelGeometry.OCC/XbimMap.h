#pragma once
#include "XbimGeometryModel.h"
#include "XbimGeometryModel.h"
#include "XbimSolid.h"
#include "BRepBuilderAPI_Transform.hxx"
#include "BRepBuilderAPI_GTransform.hxx"
#include "XbimGeomPrim.h"
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
			XbimTriangulatedModelCollection^ theMesh;
			IfcAxis2Placement^ _origin; 
			IfcCartesianTransformationOperator^ _cartTransform;
		public:
			XbimMap(XbimGeometryModel^ item, IfcAxis2Placement^ origin, IfcCartesianTransformationOperator^ transform, ConcurrentDictionary<unsigned int,Object^>^ maps);
			XbimMap(const TopoDS_Shape & shape,XbimMap^ copy);
#if USE_CARVE
			virtual XbimPolyhedron^ ToPolyHedron(double deflection, double precision, double precisionMax, unsigned int rounding) override;
			virtual IXbimGeometryModelGroup^ ToPolyHedronCollection(double deflection, double precision,double precisionMax, unsigned int rounding) override;
			virtual XbimMeshFragment MeshTo(IXbimMeshGeometry3D^ mesh3D, IfcProduct^ product, XbimMatrix3D transform, double deflection, short modelId) override;
#endif
			~XbimMap()
			{
				InstanceCleanup();
			}

			!XbimMap()
			{
				InstanceCleanup();
			}
			virtual void InstanceCleanup() override
			{   
				_mappedItem=nullptr;
				XbimGeometryModel::InstanceCleanup();
			}
			virtual XbimGeometryModel^ CopyTo(IfcAxis2Placement^ placement) override;
			virtual IXbimGeometryModel^ TransformBy(XbimMatrix3D transform) override;
			//virtual void Move(TopLoc_Location location) override;
			virtual property bool IsValid
			{
				bool get() override
				{
					return _mappedItem !=nullptr && _mappedItem->IsValid;
				}
			}

			virtual property XbimMatrix3D Transform
			{
				XbimMatrix3D get() override
				{
					return _transform;
				}
			}

			virtual property TopoDS_Shape* Handle
			{
				TopoDS_Shape* get() override
				{
					if(nativeHandle==nullptr)
					{
						TopoDS_Shape temp = *(_mappedItem->Handle);
						nativeHandle = new TopoDS_Shape();
						if(_origin!=nullptr)
							temp.Move(XbimGeomPrim::ToLocation(_origin));
						if(_cartTransform!=nullptr)
						{	
							if(dynamic_cast<IfcCartesianTransformationOperator3DnonUniform^>( _cartTransform))
							{
								BRepBuilderAPI_GTransform gTran(temp,XbimGeomPrim::ToTransform((IfcCartesianTransformationOperator3DnonUniform^)_cartTransform));
								*nativeHandle = gTran.Shape();
							}
							else
							{
								BRepBuilderAPI_Transform gTran(temp,XbimGeomPrim::ToTransform(_cartTransform));
								*nativeHandle =gTran.Shape();
							}
						}
						else
							*nativeHandle = temp;
					}
					return nativeHandle;
				}
			}

			property XbimGeometryModel^ MappedItem
			{
				XbimGeometryModel^ get()
				{
					return _mappedItem;
				}
			}

			virtual property double Volume
			{
				double get() override
				{
					return _mappedItem->Volume;
				}
			}

			virtual XbimTriangulatedModelCollection^ Mesh(double deflection) override;
			virtual void ToSolid(double precision, double maxPrecision) override; 
		};
	}
	}

}

