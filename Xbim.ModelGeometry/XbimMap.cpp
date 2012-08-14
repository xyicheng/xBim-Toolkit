#include "StdAfx.h"
#include "XbimMap.h"
#include "XbimGeomPrim.h"
using namespace System::Collections::Generic;
using namespace Xbim::XbimExtensions;
using namespace Xbim::Ifc2x3::Extensions;
using namespace System::Windows::Media::Media3D;
using namespace System::Linq;
using namespace Xbim::Common::Exceptions;

namespace Xbim
{
	namespace ModelGeometry
	{


		XbimMap::XbimMap(IXbimGeometryModel^ item, IfcAxis2Placement^ origin, IfcCartesianTransformationOperator^ transform)
		{
			_mappedItem = item;
			 
			if(origin !=nullptr)
			{
				if(dynamic_cast<IfcAxis2Placement3D^>(origin))
					_transform = Axis2Placement3DExtensions::ToMatrix3D((IfcAxis2Placement3D^)origin);
				else if(dynamic_cast<IfcAxis2Placement2D^>(origin))
					_transform = Axis2Placement2DExtensions::ToMatrix3D((IfcAxis2Placement2D^)origin);
				else
					throw gcnew XbimGeometryException("Invalid IfcAxis2Placement argument");
					
			}
			if(transform!=nullptr)
				_transform= Matrix3D::Multiply(_transform, CartesianTransformationOperatorExtensions::ToMatrix3D(transform));

		}
		
		IXbimGeometryModel^ XbimMap::Cut(IXbimGeometryModel^ shape)
		{
			throw gcnew NotImplementedException("Cut needs to be implemented");
		}

		IXbimGeometryModel^ XbimMap::Union(IXbimGeometryModel^ shape)
		{
			throw gcnew NotImplementedException("Union needs to be implemented");
		}

		IXbimGeometryModel^ XbimMap::Intersection(IXbimGeometryModel^ shape)
		{
			throw gcnew NotImplementedException("Intersection needs to be implemented");
		}

		IXbimGeometryModel^ XbimMap::CopyTo(IfcObjectPlacement^ placement)
		{
			throw gcnew NotImplementedException("CopyTo needs to be implemented");
		}

		XbimTriangulatedModelCollection^ XbimMap::Mesh()
		{
			return Mesh(true, XbimGeometryModel::DefaultDeflection,_transform);
		}

		XbimTriangulatedModelCollection^ XbimMap::Mesh( bool withNormals )
		{
			return Mesh(withNormals, XbimGeometryModel::DefaultDeflection, _transform);
		}
		XbimTriangulatedModelCollection^ XbimMap::Mesh(bool withNormals, double deflection )
		{
			return _mappedItem->Mesh(withNormals, deflection, _transform);
		}

		XbimTriangulatedModelCollection^ XbimMap::Mesh(bool withNormals, double deflection, Matrix3D transform )
		{
			if(Matrix3D::Identity==transform)
				return _mappedItem->Mesh(withNormals, deflection, _transform);
			else
				return _mappedItem->Mesh(withNormals, deflection, Matrix3D::Multiply(_transform,transform));
		}
	}
}
