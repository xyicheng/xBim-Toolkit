#pragma once
#include "XbimEdge.h"

#include <TopoDS_Edge.hxx>
#include <TopoDS_Vertex.hxx>
#include <TopAbs_Orientation.hxx>

using namespace Xbim::XbimExtensions::Interfaces;
using namespace System::Collections::Generic;

namespace Xbim
{
	namespace ModelGeometry
	{
		public ref class XbimOrientedEdge 
		{

		private:
			TopoDS_Edge * nativeHandleEdge;
			TopoDS_Vertex * nativeHandleVertex;
			TopAbs_Orientation orientation;

		public:

			XbimOrientedEdge(const TopoDS_Edge & edge, const TopoDS_Vertex & vertex, TopAbs_Orientation orient );

			~XbimOrientedEdge()
			{
				InstanceCleanup();
			}

			!XbimOrientedEdge()
			{
				InstanceCleanup();
			}
			void InstanceCleanup()
			{   
				int temp = System::Threading::Interlocked::Exchange((int)(void*)nativeHandleEdge, 0);
				if(temp!=0)
				{
					if (nativeHandleEdge)
					{
						delete nativeHandleEdge;
						nativeHandleEdge=0;
						delete nativeHandleVertex;
						nativeHandleVertex=0;
						System::GC::SuppressFinalize(this);
					}
				}

			}
			//interface

			virtual property XbimEdge^ EdgeElement 
			{
				XbimEdge^ get();
			}

			virtual property bool Orientation
			{
				bool get();
			}

		};
	}
}