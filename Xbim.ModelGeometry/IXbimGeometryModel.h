#pragma once

#include "IXbimMeshGeometry.h"
#include <TopoDS_Shape.hxx>
#include "XbimLocation.h"
#include "XbimBoundingBox.h"
using namespace Xbim::Ifc2x3::GeometryResource;
using namespace Xbim::Ifc2x3::GeometricConstraintResource;
using namespace Xbim::XbimExtensions::SelectTypes;
using namespace Xbim::ModelGeometry::Scene;
using namespace System::Windows::Media::Media3D;
namespace Xbim
{
	namespace ModelGeometry
	{
		ref class XbimSolid;

		public interface class IXbimGeometryModel
		{
			property TopoDS_Shape* Handle{ TopoDS_Shape* get();};
			IXbimGeometryModel^ Cut(IXbimGeometryModel^ shape);
			IXbimGeometryModel^ Union(IXbimGeometryModel^ shape);
			IXbimGeometryModel^ Intersection(IXbimGeometryModel^ shape);
			XbimTriangulatedModelStream^ Mesh(bool withNormals, double deflection, Matrix3D transform );
			XbimTriangulatedModelStream^ Mesh(bool withNormals, double deflection );
			XbimTriangulatedModelStream^ Mesh(bool withNormals);
			XbimTriangulatedModelStream^ Mesh();
			IXbimGeometryModel^ CopyTo(IfcObjectPlacement^ placement);
			property XbimLocation ^ Location {XbimLocation ^ get(); void set(XbimLocation ^ location);};
			XbimBoundingBox^ GetBoundingBox(bool precise);
			property double Volume {double get();};
			property bool HasCurvedEdges {bool get();};
			
		};
	}
}

