#include "StdAfx.h"
#include "XbimEdgeLoop.h"



namespace Xbim
{
	namespace ModelGeometry
	{
		XbimEdgeLoop::XbimEdgeLoop(const TopoDS_Wire & wire, const TopoDS_Face & face)
		{
			pWire = new TopoDS_Wire();
			*pWire = wire;
			pFace = new TopoDS_Face();
			*pFace = face;
		}

		/*Interface*/

		IEnumerable<XbimOrientedEdge^>^ XbimEdgeLoop::EdgeList::get()
		{
			return this;
		}

		void XbimEdgeLoop::Print()
		{
			
			int c = 1;
			
			for each(XbimOrientedEdge^ edge in this->EdgeList)
			{
				System::Diagnostics::Debug::WriteLine("Edge " + c);
				if( edge->Orientation)
					System::Diagnostics::Debug::WriteLine(System::String::Format("{0} to {1}", edge->EdgeElement->EdgeStart->VertexGeometry, edge->EdgeElement->EdgeEnd->VertexGeometry));
				else
					System::Diagnostics::Debug::WriteLine(System::String::Format("{1} to {0}", edge->EdgeElement->EdgeStart->VertexGeometry, edge->EdgeElement->EdgeEnd->VertexGeometry));
			}
			
			
		}
	}
}