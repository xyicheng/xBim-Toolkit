#pragma once
#include "IXbimGeometryModel.h"
#include "XbimGeometryModel.h"

#include <BRepGProp.hxx>
#include <GProp_GProps.hxx> 
using namespace Xbim::Ifc2x3::ProductExtension;
using namespace System::Collections::Generic;
using namespace Xbim::ModelGeometry::Scene;
using namespace Xbim::Common::Logging;
namespace Xbim
{
	namespace ModelGeometry
	{
		public ref class XbimFeaturedShape :IXbimGeometryModel
		{
		private:
			static ILogger^ Logger = LoggerFactory::GetLogger();
			Int32 _representationLabel;
			Int32 _surfaceStyleLabel;
		protected:
			IXbimGeometryModel^ mResultShape;
			IXbimGeometryModel^ mBaseShape;
			List<IXbimGeometryModel^>^ mOpenings;
			List<IXbimGeometryModel^>^ mProjections;
			XbimFeaturedShape(XbimFeaturedShape^ copy, IfcObjectPlacement^ location);
			bool DoCut(const TopoDS_Shape& shape, bool tryRepair);
			bool DoUnion(const TopoDS_Shape& shape);
		public:
			XbimFeaturedShape(IfcProduct^ product, IXbimGeometryModel^ baseShape, IEnumerable<IXbimGeometryModel^>^ openings, IEnumerable<IXbimGeometryModel^>^ projections);
			
			virtual property TopoDS_Shape* Handle
			{
				TopoDS_Shape* get(){if(mResultShape!=nullptr) return mResultShape->Handle; else return nullptr;};			
			}
			virtual property XbimLocation ^ Location 
			{
				XbimLocation ^ get()
				{
					return mResultShape->Location;
				}
				void set(XbimLocation ^ location)
				{
					mResultShape->Location = location;
				}
			};

			virtual property double Volume
			{
				double get()
				{
					if(mResultShape!=nullptr)
					{
						GProp_GProps System;
						BRepGProp::VolumeProperties(*(mResultShape->Handle), System, Standard_True);
						return System.Mass();
					}
					else
						return 0;
				}
			}
			virtual property bool HasCurvedEdges
			{
				virtual bool get()
				{
					bool hasCurves = false;
					if(mResultShape!=nullptr)
						hasCurves = mResultShape->HasCurvedEdges;
					if(mOpenings!=nullptr)
					{
						for each(IXbimGeometryModel^ opening in mOpenings)
							if(opening->HasCurvedEdges) hasCurves = true;
					}
					if(mProjections!=nullptr)
					{
						for each(IXbimGeometryModel^ projection in mProjections)
							if(projection->HasCurvedEdges) hasCurves = true;
					}
					return hasCurves;
				}
			}
			virtual XbimBoundingBox^ GetBoundingBox(bool precise)
			{
				return XbimGeometryModel::GetBoundingBox(mBaseShape, precise);
			};

			virtual IXbimGeometryModel^ Cut(IXbimGeometryModel^ shape);
			virtual IXbimGeometryModel^ Union(IXbimGeometryModel^ shape);
			virtual IXbimGeometryModel^ Intersection(IXbimGeometryModel^ shape);
			virtual IXbimGeometryModel^ CopyTo(IfcObjectPlacement^ placement);
			virtual XbimTriangulatedModelCollection^ Mesh(bool withNormals, double deflection, Matrix3D transform);
			virtual XbimTriangulatedModelCollection^ Mesh(bool withNormals, double deflection);
			virtual XbimTriangulatedModelCollection^ Mesh(bool withNormals);
			virtual XbimTriangulatedModelCollection^ Mesh();
			
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
		};
	}
}
