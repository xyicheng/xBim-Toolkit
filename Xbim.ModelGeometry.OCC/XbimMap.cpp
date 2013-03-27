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
		namespace OCC
		{
			XbimMap::XbimMap(IXbimGeometryModel^ item, IfcAxis2Placement^ origin, IfcCartesianTransformationOperator^ transform, ConcurrentDictionary<int,Object^>^ maps)
			{
				_mappedItem = item;
				_representationLabel = item->RepresentationLabel;
				_surfaceStyleLabel = item->SurfaceStyleLabel;


				if(origin !=nullptr)
				{
					if(dynamic_cast<IfcAxis2Placement3D^>(origin))
						_transform = Axis2Placement3DExtensions::ToMatrix3D((IfcAxis2Placement3D^)origin,maps);
					else if(dynamic_cast<IfcAxis2Placement2D^>(origin))
						_transform = Axis2Placement2DExtensions::ToMatrix3D((IfcAxis2Placement2D^)origin,maps);
					else
						throw gcnew XbimGeometryException("Invalid IfcAxis2Placement argument");

				}

				if(transform!=nullptr)
					_transform= Matrix3D::Multiply( CartesianTransformationOperatorExtensions::ToMatrix3D(transform, maps),_transform);

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

			void XbimMap::Move(TopLoc_Location location)
			{
				_mappedItem->Move(location);
			}

			List<XbimTriangulatedModel^>^XbimMap::Mesh()
			{
				return Mesh(true, XbimGeometryModel::DefaultDeflection,Matrix3D::Identity);
			}

			List<XbimTriangulatedModel^>^XbimMap::Mesh( bool withNormals )
			{
				return Mesh(withNormals, XbimGeometryModel::DefaultDeflection, Matrix3D::Identity);
			}
			List<XbimTriangulatedModel^>^XbimMap::Mesh(bool withNormals, double deflection )
			{
				return _mappedItem->Mesh(withNormals, deflection, Matrix3D::Identity);
			}

			List<XbimTriangulatedModel^>^XbimMap::Mesh(bool withNormals, double deflection, Matrix3D transform )
			{
				return _mappedItem->Mesh(withNormals, deflection, Matrix3D::Identity);
			}
		}
	}
}
