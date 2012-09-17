#include "StdAfx.h"
#include "XbimFeaturedShape.h"
#include "XbimGeomPrim.h"
#include "XbimSolid.h"
#include "XbimShell.h"
#include "XbimGeometryModelCollection.h"
#include "XbimBoundingBox.h"

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
#include <BRepBndLib.hxx> 
#include <BRepLib.hxx> 
#include <BRepCheck_Analyzer.hxx> 
using namespace System::Linq;
using namespace Xbim::Common::Exceptions;

namespace Xbim
{
	namespace ModelGeometry
	{
		bool LowLevelCut(const TopoDS_Shape & from, const TopoDS_Shape & toCut, TopoDS_Shape & result)
		{
			bool ok = false;

			try
			{
				if(toCut.IsNull() || from.IsNull()) 
					return false;
				BRepAlgoAPI_Cut boolOp(from,toCut);
				Standard_Integer err = boolOp.ErrorStatus();
				ok = (err==0);
				if(ok) result = boolOp.Shape();
				/*else
				{
					BRepAlgo_Cut boolOp2(from,toCut);
					ok = boolOp2.IsDone();
					if(ok) result = boolOp2.Shape();
				}*/

			}
			catch(... )
			{
			}
			return ok;
		}

		// cuts a shape from the result shape and updates thre result shape if it was successful
		bool XbimFeaturedShape::DoCut(const TopoDS_Shape& toCut, bool tryToRepair)
		{	
			TopoDS_Shape res;
			if(LowLevelCut(*(mResultShape->Handle),toCut,res))
			{		
				
				if( BRepCheck_Analyzer(res, Standard_False).IsValid() == 0) 
				{
					
					if(!tryToRepair) return false; //sometimes repairing can alter the tolerances of shapes and cause problems
					//try and fix it
					
					//BRepTools::Write(res,"r");
					ShapeFix_Shape fixer(res);
					fixer.SetPrecision(BRepLib::Precision());
					fixer.SetMinTolerance(BRepLib::Precision());
					fixer.SetMaxTolerance(BRepLib::Precision());
					fixer.Perform();

					if(BRepCheck_Analyzer(fixer.Shape(), Standard_False).IsValid() == 0) 
						return false;//messed up try individual cutting or throw an error
					else
						*(mResultShape->Handle) = fixer.Shape();
				}
				else
					*(mResultShape->Handle) = res;
				return true;
			}
			else
				return false;
		}

		// unions a shape from the result shape and updates thre result shape if it was successful
		bool XbimFeaturedShape::DoUnion(const TopoDS_Shape& toUnion)
		{
			BRepAlgoAPI_Fuse boolOp(*(mResultShape->Handle),toUnion);
			const TopoDS_Shape & shape = boolOp.Shape();
			//check if we have any shells and composites, these need to be done individually or they mess up the shape
			if(shape.ShapeType() == TopAbs_SOLID)
				mResultShape = gcnew XbimSolid(TopoDS::Solid(shape), HasCurvedEdges);
			else if(shape.ShapeType() == TopAbs_SHELL)	
				mResultShape = gcnew XbimShell(TopoDS::Shell(shape), HasCurvedEdges);
			else if(shape.ShapeType() == TopAbs_COMPOUND)
				mResultShape = gcnew XbimSolid(shape, HasCurvedEdges);
			else
				return false;
			return true;
		}

		XbimFeaturedShape::XbimFeaturedShape(IfcProduct^ product, IXbimGeometryModel^ baseShape, IEnumerable<IXbimGeometryModel^>^ openings, IEnumerable<IXbimGeometryModel^>^ projections)
		{
			if(baseShape==nullptr)
			{
				Logger->Warn("Undefined base shape passed to XbimFeaturedShape");
				return;
			}
			mBaseShape = baseShape;
			mResultShape =  mBaseShape;
			_representationLabel= baseShape->RepresentationLabel;

			if(projections!=nullptr && Enumerable::Count<IXbimGeometryModel^>(projections) > 0)
			{
				mProjections = gcnew List<IXbimGeometryModel^>(projections);
				TopoDS_Compound c;
				BRep_Builder b;
				b.MakeCompound(c);
				for each(IXbimGeometryModel^ projection in mProjections) // quick joinung 
					b.Add(c,*(projection->Handle));
				try
				{
					DoUnion(c);
					
				}
				catch(...)
				{
					try
					{
						mResultShape =  mBaseShape; //go back to start
						//try each cut separately
						for each(IXbimGeometryModel^ projection in mProjections) //one by one joinung for tricky geometries, opencascade is less likely to fail
						{
							DoUnion(*(projection->Handle));
						}
					}
					catch(...)
					{
						throw gcnew XbimGeometryException("XbimFeaturedShape Boolean Add Projections failed");
					}
				}
			}
			if(openings!=nullptr && Enumerable::Count<IXbimGeometryModel^>(openings) > 0)
			{
				mOpenings = gcnew List<IXbimGeometryModel^>(openings);
				
				BRep_Builder b;
				TopoDS_Shape c;
				if(mOpenings->Count>1)
				{
					TopoDS_Compound comp;
					b.MakeCompound(comp);
					for each(IXbimGeometryModel^ opening in mOpenings) // quick cutting 
						b.Add(comp,*(opening->Handle));
					c = comp;
				}
				else
					c =  *(mOpenings[0]->Handle);
				try
				{

					/*BRepTools::Write(c, "c");
					BRepTools::Write(*(mResultShape->Handle), "b");*/
					if(!DoCut(c,false) ) //try the fast option first if it is not a shell, if more than one opening try slow
					{
						//increase the tolerances and try again
						ShapeFix_ShapeTolerance fTol;
						double prec = Math::Max(1e-5,BRepLib::Precision()*1000 );
						//double prec = BRepLib::Precision();
						fTol.SetTolerance(*(mResultShape->Handle), prec);
						fTol.SetTolerance(c,prec);
						if(!DoCut(c,false) )
						{
							//try each cut separately
							bool failed = false;
							for each(IXbimGeometryModel^ opening in mOpenings) //one by one cutting for tricky geometries. opencascade is less likely to fail
							{
								//BRepTools::Write(*(opening->Handle),"h");
								if(mOpenings->Count==1 || !DoCut(*(opening->Handle),false)) //if only one opening just do sub parts, else try each opening before doing sub parts
								{
									//getting harder, the geometry is most likley badly defined try each of the sub shells
									for (TopExp_Explorer ex(*(opening->Handle),TopAbs_SHELL) ; ex.More(); ex.Next())  
									{
										try 
										{
											ShapeFix_Solid sf_solid;
											sf_solid.SetPrecision(BRepLib::Precision());
											sf_solid.LimitTolerance(BRepLib::Precision());
											TopoDS_Solid solid = sf_solid.SolidFromShell(TopoDS::Shell(ex.Current()));
											/*BRepTools::Write(solid,"s");*/
											if(!DoCut(solid,true))
												failed=true;
										} catch(...) {failed=true;}
									}
								}
							}
							if(failed)
								Logger->WarnFormat("Failed cut an opening in entity #{0}={1}\nA simplified representation for the shape has been used",product->EntityLabel,product->GetType()->Name);
						}
					}
				}
				catch(...)
				{
					Logger->ErrorFormat("Failed cut all openings in entity #{0}={1}\nA simplified representation for the shape has been used",product->EntityLabel,product->GetType()->Name);

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

		XbimTriangulatedModelCollection^ XbimFeaturedShape::Mesh()
		{
			return Mesh( true, XbimGeometryModel::DefaultDeflection);
		}

		XbimTriangulatedModelCollection^ XbimFeaturedShape::Mesh(bool withNormals )
		{
			return Mesh(withNormals, XbimGeometryModel::DefaultDeflection);
		}

		XbimTriangulatedModelCollection^ XbimFeaturedShape::Mesh(bool withNormals, double deflection )
		{
			return XbimGeometryModel::Mesh(mResultShape,withNormals,deflection, Matrix3D::Identity);
			
		}
		
		XbimTriangulatedModelCollection^ XbimFeaturedShape::Mesh(bool withNormals, double deflection, Matrix3D transform )
		{
			return XbimGeometryModel::Mesh(mResultShape,withNormals,deflection, transform);
			
		}
	}
}
