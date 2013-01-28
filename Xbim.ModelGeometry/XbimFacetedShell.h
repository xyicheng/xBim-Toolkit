#pragma once
#include "IXbimGeometryModel.h"
#include "XbimGeometryModel.h"
#include "XbimShell.h"
using namespace Xbim::Ifc2x3::GeometryResource;
using namespace Xbim::Ifc2x3::TopologyResource;
using namespace Xbim::Common::Logging;
namespace Xbim
{
	namespace ModelGeometry
	{
		public ref class XbimFacetedShell : IXbimGeometryModel
		{
		private:
			IfcConnectedFaceSet^ _faceSet;
			XbimShell^ _occShell;
			XbimBoundingBox^ _boundingBox;
			static ILogger^ Logger = LoggerFactory::GetLogger();
			Int32 _representationLabel;
			Int32 _surfaceStyleLabel;
		public:

			XbimFacetedShell(IfcConnectedFaceSet^ faceSet);
			XbimFacetedShell(IfcOpenShell^ shell);
			XbimFacetedShell(IfcClosedShell^ shell);
			XbimFacetedShell(IfcShell^ shell);
			

			virtual IXbimGeometryModel^ Cut(IXbimGeometryModel^ shape);
			virtual IXbimGeometryModel^ Union(IXbimGeometryModel^ shape);
			virtual IXbimGeometryModel^ Intersection(IXbimGeometryModel^ shape);
			virtual IXbimGeometryModel^ CopyTo(IfcObjectPlacement^ placement);
			virtual void Move(TopLoc_Location location);
			virtual property bool HasCurvedEdges
			{
				virtual bool get() //this geometry never has curved edges
				{
					return false;
				}
			}
			virtual XbimBoundingBox^ GetBoundingBox(bool precise)
			{
				return _boundingBox;
			};
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
			};

			virtual property TopoDS_Shape* Handle
			{
				TopoDS_Shape* get()
				{
					if(_occShell==nullptr)
						_occShell = gcnew XbimShell(_faceSet);
					return _occShell->Handle;	
				};		
				
			}

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

