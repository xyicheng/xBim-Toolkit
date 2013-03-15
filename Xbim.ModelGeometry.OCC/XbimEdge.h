#pragma once
#include <TopoDS_Edge.hxx>
#include <TopoDS_Vertex.hxx>

#include "XbimVertexPoint.h"

using namespace Xbim::XbimExtensions::Interfaces;
using namespace System::Collections::Generic;

namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
			public ref class XbimEdge 
			{
			private:
				TopoDS_Edge * nativeHandleEdge;

			public:
				XbimEdge(const TopoDS_Edge & edge, const TopoDS_Vertex & vertex, TopAbs_Orientation orientation );
				XbimEdge(XbimVertexPoint^ start, XbimVertexPoint^ end );
				XbimEdge(XbimEdge^ edge);
				~XbimEdge()
				{
					InstanceCleanup();
				}

				!XbimEdge()
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
							/*delete nativeHandleVertexStart;
							nativeHandleVertexStart=0;
							delete nativeHandleVertexEnd;
							nativeHandleVertexEnd=0;*/
							System::GC::SuppressFinalize(this);
						}
					}

				}

				virtual property XbimVertexPoint^ EdgeStart
				{
					XbimVertexPoint^ get();
				}

				bool IsStartVertex(XbimVertexPoint^ point);
				bool IsEndVertex(XbimVertexPoint^ point);
				void Reverse();
				virtual property XbimVertexPoint^ EdgeEnd
				{
					XbimVertexPoint^ get();
				}
				property TopoDS_Edge * Handle
				{
					TopoDS_Edge* get(){return nativeHandleEdge;};			
				}
			};
		}
	}
}

