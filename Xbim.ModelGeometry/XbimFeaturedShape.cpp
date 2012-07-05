#include "StdAfx.h"
#include "XbimFeaturedShape.h"
#include "XbimGeomPrim.h"
#include "XbimSolid.h"
#include "XbimShell.h"
#include "XbimGeometryModelCollection.h"

#include <BRepAlgoAPI_Cut.hxx>
#include <BRepAlgo_Cut.hxx>
#include <BRepAlgoAPI_Fuse.hxx>
#include <BRepAlgoAPI_Common.hxx>
#include <BRepMesh_IncrementalMesh.hxx>
#include <Poly_Array1OfTriangle.hxx>
#include <TColgp_Array1OfPnt.hxx>
#include <TShort_Array1OfShortReal.hxx>
#include <Poly_Triangulation.hxx>
#include <ShapeFix_Solid.hxx>
#include <ShapeFix_Shell.hxx> 
#include <ShapeFix_Shape.hxx> 
#include <ShapeFix_ShapeTolerance.hxx> 
#include <BRepBuilderAPI_Sewing.hxx> 
using namespace System::Linq;
using namespace Xbim::Common::Exceptions;

namespace Xbim
{
	namespace ModelGeometry
	{

		XbimFeaturedShape::XbimFeaturedShape(IXbimGeometryModel^ baseShape, IEnumerable<IXbimGeometryModel^>^ openings, IEnumerable<IXbimGeometryModel^>^ projections)
		{
			if(baseShape==nullptr)
			{
				Logger->Warn("Undefined base shape passed to XbimFeaturedShape");
				return;
			}
			mBaseShape = baseShape;
			mResultShape =  mBaseShape;
			if(projections!=nullptr && Enumerable::Count<IXbimGeometryModel^>(projections) > 0)
			{
				mProjections = gcnew List<IXbimGeometryModel^>(projections);
				for each(IXbimGeometryModel^ projection in mProjections)
					mResultShape = mResultShape->Union(projection);
			}
			if(openings!=nullptr && Enumerable::Count<IXbimGeometryModel^>(openings) > 0)
			{
				mOpenings = gcnew List<IXbimGeometryModel^>(openings);
				TopoDS_Compound c;
				BRep_Builder b;
				b.MakeCompound(c);
				for each(IXbimGeometryModel^ opening in mOpenings) //temp disable quick cutting in favour of accuracy
					b.Add(c,*(opening->Handle));
				BRepAlgoAPI_Cut boolOp(*(mResultShape->Handle),c);
				/*BRepTools::Write(c,"comp.txt");
				BRepTools::Write(*(mResultShape->Handle),"body.txt");*/
				if(boolOp.ErrorStatus() == 0) //it worked so use the result or we didn't have any solids to cut
				{
					//check if we have any shells and composites, these need to be done individually or they mess up the shape
					const TopoDS_Shape & shape = boolOp.Shape();
					//BRepTools::Write(shape,"res.txt");
					if(shape.ShapeType() == TopAbs_SOLID)
						mResultShape = gcnew XbimSolid(TopoDS::Solid(shape), HasCurvedEdges);
					else if(shape.ShapeType() == TopAbs_SHELL)	
						mResultShape = gcnew XbimShell(TopoDS::Shell(shape), HasCurvedEdges);
					else if(shape.ShapeType() == TopAbs_COMPOUND)
						mResultShape = gcnew XbimSolid(shape, HasCurvedEdges);
					else if(shape.ShapeType() == TopAbs_COMPSOLID)
						Logger->Warn("Failed to form difference between two shapes, Compound Solids not supported");
					else
						Logger->Warn("Failed to cut an opening in an element");
				}
				else //still failed stuff them all in and do one at a time
				{
					Logger->Warn("Failed to cut an opening in an element");
				}
			}
		}

		IXbimGeometryModel^ XbimFeaturedShape::Cut(IXbimGeometryModel^ shape)
		{
			BRepAlgoAPI_Cut boolOp(*(mResultShape->Handle),*(shape->Handle));

			if(boolOp.ErrorStatus() == 0) //find the solid
			{ 
				const TopoDS_Shape & res = boolOp.Shape();
				if(res.ShapeType() == TopAbs_SOLID)
					return gcnew XbimSolid(TopoDS::Solid(res), HasCurvedEdges);
				else if(res.ShapeType() == TopAbs_SHELL)	
					return gcnew XbimShell(TopoDS::Shell(res), HasCurvedEdges);
				else if(res.ShapeType() == TopAbs_COMPOUND || res.ShapeType() == TopAbs_COMPSOLID)
					for (TopExp_Explorer solidEx(res,TopAbs_SOLID) ; solidEx.More(); solidEx.Next())  
						return gcnew XbimSolid(TopoDS::Solid(solidEx.Current()), HasCurvedEdges);
			}
			Logger->Warn("Failed to form difference between two shapes");
			return nullptr;
		}
		IXbimGeometryModel^ XbimFeaturedShape::Union(IXbimGeometryModel^ shape)
		{
			BRepAlgoAPI_Fuse boolOp(*(mResultShape->Handle),*(shape->Handle));

			if(boolOp.ErrorStatus() == 0) //find the solid
			{ 
				const TopoDS_Shape & res = boolOp.Shape();
				if(res.ShapeType() == TopAbs_SOLID)
					return gcnew XbimSolid(TopoDS::Solid(res), HasCurvedEdges);
				else if(res.ShapeType() == TopAbs_SHELL)	
					return gcnew XbimShell(TopoDS::Shell(res), HasCurvedEdges);
				else if(res.ShapeType() == TopAbs_COMPOUND || res.ShapeType() == TopAbs_COMPSOLID)
					for (TopExp_Explorer solidEx(res,TopAbs_SOLID) ; solidEx.More(); solidEx.Next())  
						return gcnew XbimSolid(TopoDS::Solid(solidEx.Current()), HasCurvedEdges);
			}
			Logger->Warn("Failed to form union between two shapes");
			return nullptr;
		}

		IXbimGeometryModel^ XbimFeaturedShape::Intersection(IXbimGeometryModel^ shape)
		{
			BRepAlgoAPI_Common boolOp(*(mResultShape->Handle),*(shape->Handle));

			if(boolOp.ErrorStatus() == 0) //find the solid
			{ 
				const TopoDS_Shape & res = boolOp.Shape();
				if(res.ShapeType() == TopAbs_SOLID)
					return gcnew XbimSolid(TopoDS::Solid(res), HasCurvedEdges);
				else if(res.ShapeType() == TopAbs_SHELL)	
					return gcnew XbimShell(TopoDS::Shell(res), HasCurvedEdges);
				else if(res.ShapeType() == TopAbs_COMPOUND || res.ShapeType() == TopAbs_COMPSOLID)
					for (TopExp_Explorer solidEx(res,TopAbs_SOLID) ; solidEx.More(); solidEx.Next())  
						return gcnew XbimSolid(TopoDS::Solid(solidEx.Current()), HasCurvedEdges);
			}
			Logger->Warn("Failed to form Intersection between two shapes");
			return nullptr;
		}

		XbimFeaturedShape::XbimFeaturedShape(XbimFeaturedShape^ copy, IfcObjectPlacement^ location)
		{
			if(dynamic_cast<IfcLocalPlacement^>(location))
			{
				TopoDS_Shape movedShape = *(copy->mResultShape->Handle);
				IfcLocalPlacement^ lp = (IfcLocalPlacement^)location;
				movedShape.Move(XbimGeomPrim::ToLocation(lp->RelativePlacement));

				if(movedShape.ShapeType() == TopAbs_SOLID)
					mResultShape = gcnew XbimSolid(TopoDS::Solid(movedShape), HasCurvedEdges);
				else if(movedShape.ShapeType() == TopAbs_COMPOUND || movedShape.ShapeType() == TopAbs_COMPSOLID)
				{
					for (TopExp_Explorer solidEx(movedShape,TopAbs_SOLID) ; solidEx.More(); solidEx.Next())  
					{
						mResultShape = gcnew XbimSolid(TopoDS::Solid(solidEx.Current()), HasCurvedEdges);
						break;
					}
				}
				mBaseShape = copy->mBaseShape;
				mOpenings = copy->mOpenings;
				mProjections = copy->mProjections;
				if(mResultShape == nullptr)
					throw(gcnew XbimGeometryException("XbimFeaturedShape::CopyTo has failed to move shape"));
			}
			else
				throw(gcnew NotImplementedException("XbimFeaturedShape::CopyTo only supports IfcLocalPlacement type"));
		}

		IXbimGeometryModel^ XbimFeaturedShape::CopyTo(IfcObjectPlacement^ placement)
		{
			return gcnew XbimFeaturedShape(this,placement);
		}

		XbimTriangulatedModelStream^ XbimFeaturedShape::Mesh()
		{
			return Mesh( true, XbimGeometryModel::DefaultDeflection);
		}

		XbimTriangulatedModelStream^ XbimFeaturedShape::Mesh(bool withNormals )
		{
			return Mesh(withNormals, XbimGeometryModel::DefaultDeflection);
		}

		XbimTriangulatedModelStream^ XbimFeaturedShape::Mesh(bool withNormals, double deflection )
		{
			return XbimGeometryModel::Mesh(mResultShape,withNormals,deflection, Matrix3D::Identity);
			
		}
		
		XbimTriangulatedModelStream^ XbimFeaturedShape::Mesh(bool withNormals, double deflection, Matrix3D transform )
		{
			return XbimGeometryModel::Mesh(mResultShape,withNormals,deflection, transform);
			
		}
	}
}