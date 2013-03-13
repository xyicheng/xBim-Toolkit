#pragma once

#include "IXbimMeshGeometry.h"
#include <TopoDS_Shape.hxx>
#include "XbimLocation.h"
#include "XbimBoundingBox.h"
#include <BRepBuilderApi.hxx>
using namespace Xbim::Ifc2x3::GeometryResource;
using namespace Xbim::Ifc2x3::GeometricConstraintResource;
using namespace Xbim::XbimExtensions::SelectTypes;
using namespace Xbim::ModelGeometry::Scene;
using namespace System::Windows::Media::Media3D;
using namespace System::Collections::Generic;
namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
			ref class XbimSolid;

			public interface class IXbimGeometryModel
			{
				property Int32 RepresentationLabel{Int32 get();void set(Int32 value);}
				property Int32 SurfaceStyleLabel{Int32 get();void set(Int32 value);}
				property TopoDS_Shape* Handle{ TopoDS_Shape* get();};
				IXbimGeometryModel^ Cut(IXbimGeometryModel^ shape);
				IXbimGeometryModel^ Union(IXbimGeometryModel^ shape);
				IXbimGeometryModel^ Intersection(IXbimGeometryModel^ shape);
				List<XbimTriangulatedModel^>^Mesh(bool withNormals, double deflection, Matrix3D transform );
				List<XbimTriangulatedModel^>^Mesh(bool withNormals, double deflection );
				List<XbimTriangulatedModel^>^Mesh(bool withNormals);
				List<XbimTriangulatedModel^>^Mesh();
				IXbimGeometryModel^ CopyTo(IfcObjectPlacement^ placement);
				void Move(TopLoc_Location location);
				property XbimLocation ^ Location {XbimLocation ^ get(); void set(XbimLocation ^ location);};
				XbimBoundingBox^ GetBoundingBox(bool precise);
				property double Volume {double get();};
				property bool HasCurvedEdges {bool get();};

			};
		}
	}
}

