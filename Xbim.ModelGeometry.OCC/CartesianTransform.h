#pragma once

using namespace System::Windows::Media::Media3D;
using namespace Xbim::Ifc2x3::GeometryResource;
using namespace Xbim::Ifc2x3::GeometricConstraintResource;
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
				static Matrix3D ConvertMatrix3D(IfcCartesianTransformationOperator3D ^ stepTransform);
				static Matrix3D ConvertMatrix3D(IfcObjectPlacement ^ placement);
			};
		}
	}
}
