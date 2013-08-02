#pragma once
#include "XbimGeometryModel.h"
#include "XbimGeometryModel.h"
#include "XbimShell.h"
using namespace Xbim::Ifc2x3::GeometryResource;
using namespace Xbim::Ifc2x3::TopologyResource;
using namespace Xbim::Common::Logging;
namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
		public ref class XbimFacetedShell : XbimGeometryModel
		{
		private:
			IfcConnectedFaceSet^ _faceSet;
			XbimShell^ _occShell;
			XbimRect3D _boundingBox;
			static ILogger^ Logger = LoggerFactory::GetLogger();
		public:

			XbimFacetedShell(IfcConnectedFaceSet^ faceSet);
			XbimFacetedShell(IfcOpenShell^ shell);
			XbimFacetedShell(IfcClosedShell^ shell);
			XbimFacetedShell(IfcShell^ shell);
			

				~XbimFacetedShell()
				{
					InstanceCleanup();
				}

				!XbimFacetedShell()
				{
					InstanceCleanup();
				}
				void InstanceCleanup()
				{   
					_faceSet=nullptr;
					_occShell=nullptr;

				}
			virtual XbimGeometryModel^ Cut(XbimGeometryModel^ shape) override;
			virtual XbimGeometryModel^ Union(XbimGeometryModel^ shape) override;
			virtual XbimGeometryModel^ Intersection(XbimGeometryModel^ shape) override;
			virtual XbimGeometryModel^ CopyTo(IfcObjectPlacement^ placement) override;
			virtual void Move(TopLoc_Location location) override;
			virtual property bool HasCurvedEdges
			{
				virtual bool get() override//this geometry never has curved edges
				{
					return false;
				}
			}
			virtual XbimRect3D GetBoundingBox()  override
			{
				return _boundingBox;
			};
			
			virtual List<XbimTriangulatedModel^>^Mesh(bool withNormals, double deflection) override;
			virtual property double Volume			{
				double get() override
				{
					throw gcnew NotImplementedException("Volume needs to be implemented");
				}
			}

			virtual property XbimLocation ^ Location  
			{
				XbimLocation ^ get() override
				{
					throw gcnew NotImplementedException("Location needs to be implemented");
				}
				void set(XbimLocation ^ location) override
				{
					throw gcnew NotImplementedException("Location needs to be implemented");
				}
			};

			virtual property TopoDS_Shape* Handle
			{
				TopoDS_Shape* get() override
				{
					if(_occShell==nullptr)
						_occShell = gcnew XbimShell(_faceSet);
					return _occShell->Handle;	
				};		
				
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

