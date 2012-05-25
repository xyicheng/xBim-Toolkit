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
namespace Xbim
{
	namespace ModelGeometry
	{

		XbimFeaturedShape::XbimFeaturedShape(IXbimGeometryModel^ baseShape, IEnumerable<IXbimGeometryModel^>^ openings, IEnumerable<IXbimGeometryModel^>^ projections)
		{


			if(baseShape==nullptr)
			{
				System::Diagnostics::Debug::WriteLine("Undefined base shape passed to XbimFeaturedShape");
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
				List<IXbimGeometryModel^>^ nonSolidOpenings = gcnew List<IXbimGeometryModel^>();
				
				
				bool hasCompound = false;
				
				if(mOpenings->Count > 1)
				{
					Dictionary< XbimBoundingBox^, IXbimGeometryModel^>^ bbs = gcnew Dictionary<XbimBoundingBox^, IXbimGeometryModel^>();
					for each(IXbimGeometryModel^ opening in mOpenings) //temp disable quick cutting in favour of accuracy
					{
						bbs->Add( opening->GetBoundingBox(false), opening);
					}
					KeyValuePair<XbimBoundingBox^, IXbimGeometryModel^>^ kvp = Enumerable::FirstOrDefault(bbs);	
					XbimBoundingBox^ bb = kvp->Key;
					XbimBoundingBox^ basebb = mBaseShape->GetBoundingBox(false);
					IXbimGeometryModel^ opening = kvp->Value;
					while(bb !=nullptr)
					{
						bbs->Remove(bb);
						for each(XbimBoundingBox^ nb in bbs->Keys)
						{
							if(bb->Is2D() || !basebb->Intersects(bb) )// throw it away if it is 2D or does not intersect with the base shape
							{
								bb=nullptr;
								break;
							}

							if(bb->Intersects(nb))
							{	
								nonSolidOpenings->Add(opening); //intersects with next opening so do it separately
								bb=nullptr;
								break;

							}
						}
						if(bb!=nullptr)
						{
							b.Add(c,*(opening->Handle)); //no intersection so add to compound cutter
							hasCompound = true;
						}
						kvp = Enumerable::FirstOrDefault(bbs);	
						bb = kvp->Key;
						opening = kvp->Value;

					}				
				}
				else
				{
					b.Add(c,*(mOpenings[0]->Handle));
					hasCompound = true;
				}
				
				if(hasCompound ) //if we have a compund then cut it
				{
					
					BRepAlgoAPI_Cut boolOp(*(mResultShape->Handle),c);
					if(boolOp.ErrorStatus() == 0) //it worked so use the result or we didn't have any solids to cut
					{
						//see if we have a solid if so go with it


						//check if we have any shells and composites, these need to be done individually or they mess up the shape
						const TopoDS_Shape & shape = boolOp.Shape();

						if(shape.ShapeType() == TopAbs_SOLID)
							mResultShape = gcnew XbimSolid(TopoDS::Solid(shape), HasCurvedEdges);
						else if(shape.ShapeType() == TopAbs_SHELL)	
							mResultShape = gcnew XbimShell(TopoDS::Shell(shape), HasCurvedEdges);
						else if(shape.ShapeType() == TopAbs_COMPOUND)
						{	
							mResultShape = gcnew XbimSolid(shape, HasCurvedEdges);
							
						}
						else if(shape.ShapeType() == TopAbs_COMPSOLID)
							System::Diagnostics::Debug::WriteLine("Failed to form difference between two shapes, Compound Solids not supported");
						else
							System::Diagnostics::Debug::WriteLine("Failed to form difference between two shapes");
					}
					else //still failed stuff them all in and do one at a time
					{
						nonSolidOpenings->Clear();
						for each(IXbimGeometryModel^ opening in mOpenings) 
						{
							nonSolidOpenings->Add( opening);
						}
					}
				}
				if(nonSolidOpenings->Count > 0)
				{
					TopoDS_Shape shape2 = *(mResultShape->Handle);
					for each(IXbimGeometryModel^ opening in nonSolidOpenings)
					{
						
						//make sure we are cutting a solid as a hole
						BRepAlgoAPI_Cut boolOp(shape2,*(opening->Handle));
						if(boolOp.ErrorStatus() == 0) //it worked so use the result 
							shape2 = boolOp.Shape();
						else
							System::Diagnostics::Debug::WriteLine("Failed to cut opening, most likely overlapping openings detected");
						
					}
					if(shape2.ShapeType() == TopAbs_SOLID)
						mResultShape = gcnew XbimSolid(TopoDS::Solid(shape2), HasCurvedEdges);
					else if(shape2.ShapeType() == TopAbs_SHELL)	
						mResultShape = gcnew XbimShell(TopoDS::Shell(shape2), HasCurvedEdges);
					else if(shape2.ShapeType() == TopAbs_COMPOUND || shape2.ShapeType() == TopAbs_COMPSOLID)
					{
						
						for (TopExp_Explorer solidEx(shape2,TopAbs_SOLID) ; solidEx.More(); solidEx.Next())  
						{
							mResultShape = gcnew XbimSolid(TopoDS::Solid(solidEx.Current()), HasCurvedEdges);
							break;
						}
					}
					else
						System::Diagnostics::Debug::WriteLine("Failed to form difference between two shapes");
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
			System::Diagnostics::Debug::WriteLine("Failed to form difference between two shapes");
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
			System::Diagnostics::Debug::WriteLine("Failed to form union between two shapes");
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
			System::Diagnostics::Debug::WriteLine("Failed to form Intersection between two shapes");
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
					throw(gcnew Exception("XbimFeaturedShape::CopyTo has failed to move shape"));
			}
			else
				throw(gcnew Exception("XbimFeaturedShape::CopyTo only supports IfcLocalPlacement type"));
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