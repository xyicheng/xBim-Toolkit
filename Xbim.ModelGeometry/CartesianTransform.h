#pragma once

using namespace System::Windows::Media::Media3D;
using namespace Xbim::Ifc::GeometryResource;
using namespace Xbim::Ifc::GeometricConstraintResource;
namespace Xbim
{	
	namespace ModelGeometry
	{
		public ref class CartesianTransform
		{
		public:

			// Builds a windows Matrix3D from a CartesianTransformationOperator3D
			static Matrix3D ConvertMatrix3D(IfcCartesianTransformationOperator3D ^ stepTransform);
			static Matrix3D ConvertMatrix3D(IfcObjectPlacement ^ placement);
		};
	}
}
