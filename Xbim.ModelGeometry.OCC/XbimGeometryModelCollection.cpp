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
#include <BRepAlgoAPI_Fuse.hxx>
#include <BRepTools.hxx>
using namespace System::Linq;
using namespace Xbim::Common::Exceptions;

namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
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
			List<XbimTriangulatedModel^>^XbimGeometryModelCollection::Mesh( )
			{
				return Mesh(true, XbimGeometryModel::DefaultDeflection, Matrix3D::Identity);
			}

			List<XbimTriangulatedModel^>^XbimGeometryModelCollection::Mesh(bool withNormals )
			{
				return Mesh(withNormals, XbimGeometryModel::DefaultDeflection, Matrix3D::Identity);
			}
			List<XbimTriangulatedModel^>^XbimGeometryModelCollection::Mesh(bool withNormals, double deflection )
			{ 
				return Mesh(withNormals, deflection, Matrix3D::Identity);
			}

			List<XbimTriangulatedModel^>^ XbimGeometryModelCollection::Mesh(bool withNormals, double deflection, Matrix3D transform )
			{ 

				if(shapes->Count > 0) //we have children that need special materials etc
				{
					List<XbimTriangulatedModel^>^tm = gcnew List<XbimTriangulatedModel^>();
					for each(IXbimGeometryModel^ gm in shapes)
					{
						List<XbimTriangulatedModel^>^ mm = gm->Mesh(withNormals, deflection, transform);
						if(mm!=nullptr)
							tm->AddRange(mm);
					}
					return tm;
				}
				else
					return gcnew List<XbimTriangulatedModel^>();

			}

			void XbimGeometryModelCollection::Move(TopLoc_Location location)
			{	

				for each(IXbimGeometryModel^ shape in shapes)
					shape->Move(location);
				if (pCompound) //remove anyy cached compund data
				{
					delete pCompound;
					pCompound=0;
				}
			}

			IXbimGeometryModel^ XbimGeometryModelCollection::CopyTo(IfcObjectPlacement^ placement)
			{
				XbimGeometryModelCollection^ newColl = gcnew XbimGeometryModelCollection(_isMap, HasCurvedEdges);
				newColl->RepresentationLabel=RepresentationLabel;
				newColl->SurfaceStyleLabel=SurfaceStyleLabel;
				for each(IXbimGeometryModel^ shape in shapes)
				{
					newColl->Add(shape->CopyTo(placement));
				}
				return newColl;
			}
			///Every element should be a solid bedore this is called. returns a compound solid
			IXbimGeometryModel^ XbimGeometryModelCollection::Solidify()
			{
				IXbimGeometryModel^ a;
				for each(IXbimGeometryModel^ b in shapes)
				{
					if(a==nullptr) a=b;
					else
					{
						BRepAlgoAPI_Fuse fuse(*(a->Handle),*(b->Handle));
						if(fuse.IsDone() && !fuse.Shape().IsNull())
							a=gcnew XbimSolid(fuse.Shape());
					}
				}
				return a;

			}
		}
	}
}