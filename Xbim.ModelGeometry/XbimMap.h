#pragma once
#include "IXbimGeometryModel.h"
#include "XbimGeometryModel.h"

using namespace Xbim::Ifc2x3::GeometryResource;
using namespace Xbim::Ifc2x3::TopologyResource;
namespace Xbim
{
	namespace ModelGeometry
	{
		public ref class XbimMap : IXbimGeometryModel
		{
		private:
			IXbimGeometryModel^ _mappedItem;
			Matrix3D _transform;
			Int32 _representationLabel;
			Int32 _surfaceStyleLabel;
		public:
			XbimMap(IXbimGeometryModel^ item, IfcAxis2Placement^ origin, IfcCartesianTransformationOperator^ transform, Dictionary<int,Object^>^ maps);
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
						
			virtual property Int32 RepresentationLabel
			{
				Int32 get(){return _representationLabel; }
				void set(Int32 value){ _representationLabel=value; }
			}

			virtual property Int32 SurfaceStyleLabel
			{
				Int32 get(){return _surfaceStyleLabel; }
				void set(Int32 value){ _surfaceStyleLabel=value; }
			}

			property Matrix3D Transform
			{
				Matrix3D get()
				{
					return _transform;
				}
			}
			
			property IXbimGeometryModel^ MappedItem
			{
				IXbimGeometryModel^ get()
				{
					return _mappedItem;
				}
			}
			virtual List<XbimTriangulatedModel^>^Mesh(bool withNormals, double deflection, Matrix3D transform);
			virtual List<XbimTriangulatedModel^>^Mesh(bool withNormals, double deflection);
			virtual List<XbimTriangulatedModel^>^Mesh(bool withNormals);
			virtual List<XbimTriangulatedModel^>^Mesh();
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

