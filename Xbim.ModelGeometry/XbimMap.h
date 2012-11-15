#pragma once
#include "IXbimGeometryModel.h"
#include "XbimGeometryModel.h"

using namespace Xbim::Ifc::GeometryResource;
using namespace Xbim::Ifc::TopologyResource;
namespace Xbim
{
	namespace ModelGeometry
	{
		public ref class XbimMap : IXbimGeometryModel
		{
		private:
			IXbimGeometryModel^ _mappedItem;
			Matrix3D _transform;
		public:
			XbimMap(IXbimGeometryModel^ item, IfcAxis2Placement^ origin, IfcCartesianTransformationOperator^ transform);
			virtual IXbimGeometryModel^ Cut(IXbimGeometryModel^ shape);
			virtual IXbimGeometryModel^ Union(IXbimGeometryModel^ shape);
			virtual IXbimGeometryModel^ Intersection(IXbimGeometryModel^ shape);
			virtual IXbimGeometryModel^ CopyTo(IfcObjectPlacement^ placement);
			virtual property bool HasCurvedEdges
			{
				virtual bool get() //this geometry never has curved edges
				{
					return false;
				}
			}
			virtual XbimBoundingBox^ GetBoundingBox(bool precise)
			{
				return _mappedItem->GetBoundingBox(precise);
			};

			property Matrix3D Transform
			{
				Matrix3D get()
				{
					return _transform;
				}
			}
			virtual XbimTriangulatedModelStream^ Mesh(bool withNormals, double deflection, Matrix3D transform);
			virtual XbimTriangulatedModelStream^ Mesh(bool withNormals, double deflection);
			virtual XbimTriangulatedModelStream^ Mesh(bool withNormals);
			virtual XbimTriangulatedModelStream^ Mesh();
			virtual property double Volume
			{
				double get()
				{
					throw gcnew NotImplementedException("Volume needs to be implemented");
				}
			}

			virtual property XbimLocation ^ Location 
			{
				XbimLocation ^ get()
				{
					throw gcnew NotImplementedException("Location needs to be implemented");
				}
				void set(XbimLocation ^ location)
				{
					throw gcnew NotImplementedException("Location needs to be implemented");
				}
			}

			virtual property TopoDS_Shape* Handle
			{
				TopoDS_Shape* get()
				{
					throw gcnew NotImplementedException("Handle needs to be implemented");
				}

			}

			
		};
	}

}

