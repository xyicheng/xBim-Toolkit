#include "StdAfx.h"
#include "XbimOrientedEdge.h"
#include "XbimEdge.h"

namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
			XbimOrientedEdge::XbimOrientedEdge(const TopoDS_Edge & edge, const TopoDS_Vertex & vertex, TopAbs_Orientation orient)
			{
				nativeHandleEdge = new TopoDS_Edge();
				*nativeHandleEdge = edge;
				nativeHandleVertex = new TopoDS_Vertex();
				*nativeHandleVertex = vertex;
				orientation = orient;
			}

			/*Interface*/
			XbimEdge^ XbimOrientedEdge::EdgeElement::get()
			{
				return gcnew XbimEdge(*nativeHandleEdge, *nativeHandleVertex, orientation);
			}

			bool XbimOrientedEdge::Orientation::get()
			{
				return orientation == TopAbs_FORWARD; 
			}
		}
	}
}