#include "StdAfx.h"
#include "XbimVertexPoint.h"
#include <gp_Pnt.hxx>
#include <BRep_Tool.hxx>
#include <BRep_Builder.hxx>

namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
		XbimVertexPoint::XbimVertexPoint(const TopoDS_Vertex & vertex)
		{
			pVertex = new TopoDS_Vertex();
			*pVertex = vertex;
		}

		XbimVertexPoint::XbimVertexPoint(double x, double y, double z)
		{
			pVertex = new TopoDS_Vertex();
			gp_XYZ pt(x,y,z);
			BRep_Builder b;
			b.MakeVertex(*pVertex , pt, _precision);
		}

		IfcCartesianPoint^ XbimVertexPoint::VertexGeometry::get() 
		{
			gp_Pnt p = BRep_Tool::Pnt(*pVertex);
			return  gcnew IfcCartesianPoint(p.X(),p.Y(),p.Z());

		}

		XbimPoint3D XbimVertexPoint::Point3D::get() 
		{
			gp_Pnt p = BRep_Tool::Pnt(*pVertex);
			return  XbimPoint3D(p.X(),p.Y(),p.Z());

		}
		}
	}
}