#pragma once
#include <TopoDS_Vertex.hxx>
using namespace Xbim::XbimExtensions::Interfaces;
using namespace Xbim::Ifc2x3::GeometryResource;
using namespace Xbim::Common::Geometry;

namespace Xbim
{
	namespace ModelGeometry
	{
		public ref class XbimVertexPoint 
		{

		private:
			TopoDS_Vertex * pVertex;
			static double _precision = 1.E-005;
		public:
			static property double Precision
			{
				double get(){return _precision;};
				void set(double value){ _precision = value;};
			}

			XbimVertexPoint(const TopoDS_Vertex & vertex);
			XbimVertexPoint(double x, double y, double z);
			~XbimVertexPoint()
			{
				InstanceCleanup();
			}

			!XbimVertexPoint()
			{
				InstanceCleanup();
			}
			void InstanceCleanup()
			{   
				int temp = System::Threading::Interlocked::Exchange((int)(void*)pVertex, 0);
				if(temp!=0)
				{
					if (pVertex)
					{
						delete pVertex;
						pVertex=0;
						System::GC::SuppressFinalize(this);
					}
				}
			}

			virtual property IfcCartesianPoint^ VertexGeometry
			{
				IfcCartesianPoint^ get();
			}
			virtual property XbimPoint3D Point3D
			{
				XbimPoint3D get();
			}

			property TopoDS_Vertex * Handle
			{
				TopoDS_Vertex* get(){return pVertex;};			
			}

		};

	}
}