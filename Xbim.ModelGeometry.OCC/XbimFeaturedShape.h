#pragma once
#include "XbimGeometryModel.h"
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
		namespace OCC
		{
		public ref class XbimFeaturedShape :XbimGeometryModel
		{
		private:
			static ILogger^ Logger = LoggerFactory::GetLogger();
			bool _hasCurves;
			bool LowLevelCut(const TopoDS_Shape & from, const TopoDS_Shape & toCut, TopoDS_Shape & result);
		protected:
			XbimGeometryModel^ mResultShape;
			XbimGeometryModel^ mBaseShape;
			List<XbimGeometryModel^>^ mOpenings;
			List<XbimGeometryModel^>^ mProjections;
			XbimFeaturedShape(XbimFeaturedShape^ copy, IfcObjectPlacement^ location);
			bool DoCut(const TopoDS_Shape& shape);
			bool DoUnion(const TopoDS_Shape& shape);
		public:
			XbimFeaturedShape(IfcProduct^ product, XbimGeometryModel^ baseShape, IEnumerable<XbimGeometryModel^>^ openings, IEnumerable<XbimGeometryModel^>^ projections);
			
			virtual property TopoDS_Shape* Handle
			{
				TopoDS_Shape* get() override
				{
					if(mResultShape!=nullptr) return mResultShape->Handle; else return nullptr;
				};			
			}
			virtual property XbimLocation ^ Location 
			{
				XbimLocation ^ get() override
				{
					return mResultShape->Location;
				}
				void set(XbimLocation ^ location) override
				{
					mResultShape->Location = location;
				}
			};

			virtual property double Volume
			{
				double get() override
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
				virtual bool get() override
				{					
					return _hasCurves;
				}
			}
			

			virtual XbimGeometryModel^ Cut(XbimGeometryModel^ shape) override;
			virtual XbimGeometryModel^ Union(XbimGeometryModel^ shape) override;
			virtual XbimGeometryModel^ Intersection(XbimGeometryModel^ shape) override;
			virtual XbimGeometryModel^ CopyTo(IfcObjectPlacement^ placement) override;
			virtual void Move(TopLoc_Location location) override;

			~XbimFeaturedShape()
			{
				InstanceCleanup();
			}

			!XbimFeaturedShape()
			{
				InstanceCleanup();
			}
			void InstanceCleanup()
			{ 
				mResultShape=nullptr;
				mBaseShape=nullptr;
				mOpenings=nullptr;
				mProjections=nullptr;
			}
			virtual property XbimMatrix3D Transform
			{
				XbimMatrix3D get() override
				{
					return XbimMatrix3D::Identity;
				}
			}
		};
	}
}
}
