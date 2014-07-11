#pragma once
#include "XbimGeometryModel.h"
#include "XbimGeometryModel.h"
#include "XbimGeometryModelCollection.h"
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
			
		protected:
			XbimGeometryModel^ mResultShape;
			XbimGeometryModel^ mBaseShape;
			List<XbimGeometryModel^>^ mOpenings;
			List<XbimGeometryModel^>^ mProjections;
			XbimFeaturedShape(XbimFeaturedShape^ copy, IfcAxis2Placement^ location);
			XbimFeaturedShape(XbimFeaturedShape^ copy, XbimMatrix3D t);
			XbimGeometryModelCollection^ PrepareFeatures(XbimGeometryModelCollection^ features, double precision, double precisionMax);
			XbimGeometryModel^ SubtractFrom(XbimGeometryModel^ base, XbimGeometryModelCollection^ openings, double deflection, double precision,double precisionMax, unsigned int rounding);
			
			/*bool DoCut(const TopoDS_Shape& shape);
			bool DoUnion(const TopoDS_Shape& shape);*/
		public:
#if USE_CARVE
			virtual XbimPolyhedron^ ToPolyHedron(double deflection, double precision, double precisionMax, unsigned int rounding) override;
			virtual IXbimGeometryModelGroup^ ToPolyHedronCollection(double deflection, double precision,double precisionMax, unsigned int rounding) override;
#endif
			virtual String^ WriteAsString(XbimModelFactors^ modelFactors) override;
			XbimFeaturedShape(IfcProduct^ product, XbimGeometryModel^ baseShape, XbimGeometryModelCollection^ openings, XbimGeometryModelCollection^ projections);
			virtual property bool IsValid
			{
				bool get() override
				{
					return mResultShape!=nullptr && mResultShape->IsValid;
				}
			}
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
			
			virtual XbimGeometryModel^ Cut(XbimGeometryModel^ shape, double deflection, double precision, double maxPrecision, unsigned int rounding) override;
			virtual XbimGeometryModel^ Union(XbimGeometryModel^ shape, double deflection, double precision, double maxPrecision, unsigned int rounding) override;
			virtual XbimGeometryModel^ Intersection(XbimGeometryModel^ shape, double deflection, double precision, double maxPrecision, unsigned int rounding) override;
			virtual XbimGeometryModel^ CopyTo(IfcAxis2Placement^ placement) override;
			virtual IXbimGeometryModel^ TransformBy(XbimMatrix3D transform) override;
			virtual void Move(TopLoc_Location location) override;

			~XbimFeaturedShape()
			{
				InstanceCleanup();
			}

			!XbimFeaturedShape()
			{
				InstanceCleanup();
			}
			virtual void InstanceCleanup() override
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
			virtual XbimTriangulatedModelCollection^ Mesh(double deflection) override {return mResultShape->Mesh(deflection);};
			virtual XbimMeshFragment MeshTo(IXbimMeshGeometry3D^ mesh3D, IfcProduct^ product, XbimMatrix3D transform, double deflection, short modelId) override;
			virtual XbimRect3D GetBoundingBox() override {return mResultShape->GetBoundingBox();};
			virtual void ToSolid(double precision, double maxPrecision) override; 
			
		};
	}
}
}
