#include "StdAfx.h"
#include "XbimSolid.h"
#include "XbimShell.h"
#include "XbimGeometryModelCollection.h"
#include "XbimMeshGeometry.h"
#include "XbimGeomPrim.h"
#include <BRepAlgoAPI_Cut.hxx>
#include <BRepAlgoAPI_Common.hxx>
#include <BRepAlgoAPI_Fuse.hxx>
#include <BRepOffsetAPI_Sewing.hxx>
#include <BRepLib.hxx>
#include <ShapeUpgrade_ShellSewing.hxx>
using namespace System::Linq;
using namespace Xbim::Common::Exceptions;

namespace Xbim
{
	namespace ModelGeometry
	{
		/*Interfaces*/

		IXbimGeometryModel^ XbimGeometryModelCollection::Cut(IXbimGeometryModel^ shape)
		{
			throw gcnew XbimGeometryException("A cut operation has been applied to a collection of model object this is illegal according to schema");
			/*try
			{
				*/
			//	BRepAlgoAPI_Cut boolOp(*pCompound,*(shape->Handle));
			//	if(boolOp.ErrorStatus() == 0) //find the solid
			//	{ 
			//		const TopoDS_Shape & res = boolOp.Shape();
			//		if(res.ShapeType() == TopAbs_SOLID)
			//			return gcnew XbimSolid(TopoDS::Solid(res), HasCurvedEdges);
			//		else if(res.ShapeType() == TopAbs_SHELL)	
			//			return gcnew XbimShell(TopoDS::Shell(res), HasCurvedEdges);
			//		else if(res.ShapeType() == TopAbs_COMPOUND)
			//			return gcnew XbimGeometryModelCollection(TopoDS::Compound(res), HasCurvedEdges);
			//		else if(res.ShapeType() == TopAbs_COMPSOLID)
			//		{
			//			TopoDS_Compound cpd;
			//			BRep_Builder b;
			//			b.MakeCompound(cpd);
			//			b.Add(cpd, res);
			//			return gcnew XbimGeometryModelCollection(cpd, HasCurvedEdges);
			//		}
			//		else
			//		{
			//			System::Diagnostics::Debug::WriteLine("Failed to form difference between two shapes");
			//			return nullptr;
			//		}
			//	}
			//	else
			//	{

			//		System::Diagnostics::Debug::WriteLine("Failed to form difference between two shapes");
			//		return nullptr;

			//	}

			//}
			//catch (...)
			//{
			//	System::Diagnostics::Debug::WriteLine("Failed to form difference between two shapes");
			//	return nullptr;

			//}
		}
		IXbimGeometryModel^ XbimGeometryModelCollection::Union(IXbimGeometryModel^ shape)
		{
			throw gcnew XbimGeometryException("A cut operation has been applied to a collection of model object this is illegal according to schema");
			//BRepAlgoAPI_Fuse boolOp(*pCompound,*(shape->Handle));

			//if(boolOp.ErrorStatus() == 0) //find the solid
			//{ 
			//	const TopoDS_Shape & res = boolOp.Shape();
			//	if(res.ShapeType() == TopAbs_SOLID)
			//		return gcnew XbimSolid(TopoDS::Solid(res), HasCurvedEdges);
			//	else if(res.ShapeType() == TopAbs_SHELL)	
			//		return gcnew XbimShell(TopoDS::Shell(res), HasCurvedEdges);
			//	else if(res.ShapeType() == TopAbs_COMPOUND)
			//		return gcnew XbimGeometryModelCollection(TopoDS::Compound(res), HasCurvedEdges);
			//	else if(res.ShapeType() == TopAbs_COMPSOLID)
			//	{
			//		TopoDS_Compound cpd;
			//		BRep_Builder b;
			//		b.MakeCompound(cpd);
			//		b.Add(cpd, res);
			//		return gcnew XbimGeometryModelCollection(cpd, HasCurvedEdges);
			//	}
			//	else
			//	{
			//		System::Diagnostics::Debug::WriteLine("Failed to form union between two shapes");
			//		return nullptr;
			//	}
			//}
			//else
			//{
			//	System::Diagnostics::Debug::WriteLine("Failed to form union between two shapes");
			//	return nullptr;
			//}
		}
		IXbimGeometryModel^ XbimGeometryModelCollection::Intersection(IXbimGeometryModel^ shape)
		{
			throw gcnew XbimGeometryException("A cut operation has been applied to a collection of model object this is illegal according to schema");
			//BRepAlgoAPI_Common boolOp(*pCompound,*(shape->Handle));

			//if(boolOp.ErrorStatus() == 0) //find the solid
			//{ 
			//	const TopoDS_Shape & res = boolOp.Shape();
			//	if(res.ShapeType() == TopAbs_SOLID)
			//		return gcnew XbimSolid(TopoDS::Solid(res), HasCurvedEdges);
			//	else if(res.ShapeType() == TopAbs_SHELL)	
			//		return gcnew XbimShell(TopoDS::Shell(res), HasCurvedEdges);
			//	else if(res.ShapeType() == TopAbs_COMPOUND)
			//		return gcnew XbimGeometryModelCollection(TopoDS::Compound(res), HasCurvedEdges);
			//		
			//	else if(res.ShapeType() == TopAbs_COMPSOLID)
			//	{
			//		TopoDS_Compound cpd;
			//		BRep_Builder b;
			//		b.MakeCompound(cpd);
			//		b.Add(cpd, res);
			//		return gcnew XbimGeometryModelCollection(cpd, HasCurvedEdges);
			//	}
			//	else
			//	{
			//		System::Diagnostics::Debug::WriteLine("Failed to form union between two shapes");
			//		return nullptr;
			//	}
			//}
			//else
			//{
			//	System::Diagnostics::Debug::WriteLine("Failed to form union between two shapes");
			//	return nullptr;
			//}
		}
		XbimTriangulatedModelStream^ XbimGeometryModelCollection::Mesh( )
		{
			return Mesh(true, XbimGeometryModel::DefaultDeflection, Matrix3D::Identity);
		}

		XbimTriangulatedModelStream^ XbimGeometryModelCollection::Mesh(bool withNormals )
		{
			return Mesh(withNormals, XbimGeometryModel::DefaultDeflection, Matrix3D::Identity);
		}
		XbimTriangulatedModelStream^ XbimGeometryModelCollection::Mesh(bool withNormals, double deflection )
		{ 
			return Mesh(withNormals, deflection, Matrix3D::Identity);
		}

		XbimTriangulatedModelStream^ XbimGeometryModelCollection::Mesh(bool withNormals, double deflection, Matrix3D transform )
		{ 
			
			if(shapes->Count > 0) //we have children that need special materials etc
			{
				XbimTriangulatedModelStream^ tm = gcnew XbimTriangulatedModelStream();
				for each(IXbimGeometryModel^ gm in shapes)
					tm->AddChild(gm->Mesh(withNormals, deflection, transform));
				return tm;
			}
			else
				return XbimTriangulatedModelStream::Empty;
			
		}

		IXbimGeometryModel^ XbimGeometryModelCollection::CopyTo(IfcObjectPlacement^ placement)
		{
			throw gcnew XbimGeometryException("A copyto operation has been applied to a collection of model object this is illegal according to schema");
			/*if(dynamic_cast<IfcLocalPlacement^>(placement))
			{
				TopoDS_Compound movedShape = *pCompound;
				IfcLocalPlacement^ lp = (IfcLocalPlacement^)placement;
				movedShape.Move(XbimGeomPrim::ToLocation(lp->RelativePlacement));
				return gcnew XbimGeometryModelCollection(movedShape, shapes, HasCurvedEdges);
			}
			else
				throw(gcnew InvalidOperationException("XbimSolid::CopyTo only supports IfcLocalPlacement type"));*/

		}
		///Every element should be a solid bedore this is called. returns a compound solid
		IXbimGeometryModel^ XbimGeometryModelCollection::Solidify()
		{
			BRep_Builder b;
			TopoDS_Compound compound;
			b.MakeCompound(compound);
			for each(IXbimGeometryModel^ shape in shapes)
				b.Add(compound, *(shape->Handle));
			return gcnew XbimSolid(compound);
		}
	}
}