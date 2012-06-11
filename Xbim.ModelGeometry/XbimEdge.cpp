#include "StdAfx.h"
#include "XbimEdge.h"
#include "XbimVertexPoint.h"
#include <TopExp_Explorer.hxx>
#include <TopoDS.hxx>
#include "BRepBuilderAPI_MakeEdge.hxx"
using namespace System;

namespace Xbim
{
	namespace ModelGeometry
	{
		XbimEdge::XbimEdge(const TopoDS_Edge & edge, const TopoDS_Vertex & vertex, TopAbs_Orientation orientation)
		{throw gcnew NotImplementedException("This constructor is not implemented");
			

		}
		XbimEdge::XbimEdge(XbimVertexPoint^ start, XbimVertexPoint^ end )
		{
			nativeHandleEdge = new TopoDS_Edge();
			BRepBuilderAPI_MakeEdge edgeMaker(*(start->Handle) , *(end->Handle));
			*nativeHandleEdge = edgeMaker.Edge();
			
		}

		XbimEdge::XbimEdge(XbimEdge^ edge )
		{
			nativeHandleEdge = new TopoDS_Edge();
			*nativeHandleEdge = *(edge->Handle); //get another reference to edge
		
		}


		bool XbimEdge::IsStartVertex(XbimVertexPoint^ point)
		{
			return point->Handle->IsSame(*(EdgeStart->Handle))==Standard_True;
		}
		bool XbimEdge::IsEndVertex(XbimVertexPoint^ point)
		{
			return point->Handle->IsSame(*(EdgeEnd->Handle))==Standard_True;
		}

		void  XbimEdge::Reverse()
		{
			nativeHandleEdge->Reverse();
		}

		XbimVertexPoint^ XbimEdge::EdgeStart::get()
		{
			TopExp_Explorer vertEx(*nativeHandleEdge,TopAbs_VERTEX);
			if(nativeHandleEdge->Orientation()==TopAbs_REVERSED)
				vertEx.Next();
			return gcnew XbimVertexPoint(TopoDS::Vertex(vertEx.Current()));

		}

		XbimVertexPoint^ XbimEdge::EdgeEnd::get()
		{

			TopExp_Explorer vertEx(*nativeHandleEdge,TopAbs_VERTEX);
			if(!nativeHandleEdge->Orientation()==TopAbs_REVERSED)
				vertEx.Next();
			return gcnew XbimVertexPoint(TopoDS::Vertex(vertEx.Current()));
		}
	}
}