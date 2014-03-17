#pragma once
#include "XbimGeometryModel.h"
#include "XbimGeometryModelCollection.h"
#include "XbimShell.h"
using namespace Xbim::Ifc2x3::GeometryResource;
using namespace Xbim::Ifc2x3::TopologyResource;
using namespace Xbim::Common::Logging;
using namespace  System::Threading;
namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
			//This class is a container for various shell meshes or polyhedron
			//it supports conversions between XbimPolyhedron, IfcShell and XbimShell
		public ref class XbimFacetedShell : public XbimGeometryModelCollection
		{
		private:
			IfcRepresentationItem^ _faceSet;	
			bool isSolid;

			void Build(IfcFaceBasedSurfaceModel^ repItem);
			void Build(IfcShellBasedSurfaceModel^ repItem);
			void Build(IfcRepresentationItem^ repItem);
			void Build(IfcConnectedFaceSet^ repItem);
			void Build(IfcClosedShell^ repItem);
			XbimTriangulatedModel^ TriangulateFaceSet(IEnumerable<IfcFace^>^ faces);
			
		public:
			void Build();
			XbimFacetedShell(bool isSolid,IfcRepresentationItem^ faceSet, bool hasCurves,int representationLabel, int surfaceStyleLabel );
			XbimFacetedShell(IfcShellBasedSurfaceModel^ sbms);
			XbimFacetedShell(IfcFaceBasedSurfaceModel^ fbms);
			XbimFacetedShell(IfcFacetedBrep^ brep);
			XbimFacetedShell(IfcConnectedFaceSet^ faceSet);
			virtual void ToSolid(double precision, double maxPrecision) override;
			XbimFacetedShell(IfcOpenShell^ shell);
			XbimFacetedShell(IfcClosedShell^ shell);
			XbimFacetedShell(IfcShell^ shell);
			IList<IfcFace^>^ Faces();

			virtual XbimGeometryModel^ CopyTo(IfcAxis2Placement^ placement) override;

			virtual property bool IsValid
			{
				bool get() override
				{
					return _faceSet!=nullptr;
				}
			}

			virtual void Move(TopLoc_Location location) override;

			virtual property double Volume
			{
				double get() override
				{
					double volume = 0;
					if(Handle!=nullptr) //we don't have a collection
						return XbimGeometryModel::Volume; //calculate the single shaep
					else
						return XbimGeometryModelCollection::Volume; //add up all the components
					
				}
			}
			
			virtual XbimRect3D GetBoundingBox() override
			{
				if(!_bounds.IsEmpty) return _bounds;
				if(Handle!=nullptr) //we don't have a collection
					return XbimGeometryModel::GetBoundingBox(); //calculate the single shaep
				else
					return XbimGeometryModelCollection::GetBoundingBox(); //add up all the components

			}

#if USE_CARVE
				virtual XbimPolyhedron^ ToPolyHedron(double deflection, double precision,double precisionMax) override;
				virtual IXbimGeometryModelGroup^ ToPolyHedronCollection(double deflection, double precision,double precisionMax) override;

#endif
				virtual String^ WriteAsString(XbimModelFactors^ modelFactors) override;
		private:
				String^ WriteAsString(XbimModelFactors^ modelFactors, IEnumerable<IfcFace^>^ faces);
		public:
				~XbimFacetedShell()
				{
					InstanceCleanup();
				}

				!XbimFacetedShell()
				{
					InstanceCleanup();
				}
				virtual void InstanceCleanup() override
				{   
					_faceSet=nullptr;			
					
				}

			virtual XbimTriangulatedModelCollection^ Mesh(double deflection) override;
			virtual XbimMeshFragment MeshTo(IXbimMeshGeometry3D^ mesh3D, IfcProduct^ product, XbimMatrix3D transform, double deflection, short modelId) override;
			
			virtual property TopoDS_Shape* Handle
			{
				TopoDS_Shape* get() override;
			}

			
		};
	}
}
}

