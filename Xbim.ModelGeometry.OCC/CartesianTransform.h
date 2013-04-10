#pragma once


using namespace Xbim::Ifc2x3::GeometryResource;
using namespace Xbim::Ifc2x3::GeometricConstraintResource;
using namespace Xbim::Common::Geometry;
namespace Xbim
{	
	namespace ModelGeometry
	{
		namespace OCC
		{
		public ref class CartesianTransform
		{
		public:

			// Builds a windows Matrix3D from a CartesianTransformationOperator3D
			static XbimMatrix3D ConvertMatrix3D(IfcCartesianTransformationOperator3D ^ stepTransform);
			static XbimMatrix3D ConvertMatrix3D(IfcObjectPlacement ^ placement);
		};
	}
}
}
