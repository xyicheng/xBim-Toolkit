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
			property Int64 RepresentationLabel{Int64 get();void set(Int64 value);}
			property TopoDS_Shape* Handle{ TopoDS_Shape* get();};
			IXbimGeometryModel^ Cut(IXbimGeometryModel^ shape);
			IXbimGeometryModel^ Union(IXbimGeometryModel^ shape);
			IXbimGeometryModel^ Intersection(IXbimGeometryModel^ shape);
			XbimTriangulatedModelCollection^ Mesh(bool withNormals, double deflection, Matrix3D transform );
			XbimTriangulatedModelCollection^ Mesh(bool withNormals, double deflection );
			XbimTriangulatedModelCollection^ Mesh(bool withNormals);
			XbimTriangulatedModelCollection^ Mesh();
			IXbimGeometryModel^ CopyTo(IfcObjectPlacement^ placement);
			property XbimLocation ^ Location {XbimLocation ^ get(); void set(XbimLocation ^ location);};
			XbimBoundingBox^ GetBoundingBox(bool precise);
			property double Volume {double get();};
			property bool HasCurvedEdges {bool get();};
			
		};
	}
}

