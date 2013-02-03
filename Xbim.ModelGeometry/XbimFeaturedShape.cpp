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
#include <Bnd_BoundSortBox.hxx> 
#include <TColStd_ListOfInteger.hxx> 
#include <Bnd_HArray1OfBox.hxx>
#include <TopTools_HArray1OfShape.hxx> 
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
		bool XbimFeaturedShape::DoCut(const TopoDS_Shape& toCut)
		{	
			TopoDS_Shape res;
			if(LowLevelCut(*(mResultShape->Handle),toCut,res))
			{		
				/*if( BRepCheck_Analyzer(res, Standard_False).IsValid() == Standard_True) 
				{*/
					*(mResultShape->Handle) = res;
					return true;
				//}
			}
			return false;
		}

		static int  CompareSize(KeyValuePair<double, IXbimGeometryModel^> x,KeyValuePair<double, IXbimGeometryModel^> y)
		{
			return x.Key.CompareTo(y.Key);
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
			_representationLabel= baseShape->RepresentationLabel;
			_surfaceStyleLabel=baseShape->SurfaceStyleLabel;
			_hasCurves = false;

			if(openings!=nullptr)	
			{
				//sort each opening in terms of the distance from the top left of the base bb and the bottom right of the opening
				mOpenings = gcnew List<IXbimGeometryModel^>();	
				for each (IXbimGeometryModel^ o in openings)
				{
					//expand collections to avoid clash
					if(dynamic_cast<XbimGeometryModelCollection^>(o))
					{
						for each (IXbimGeometryModel^ sub in (XbimGeometryModelCollection^)o)
						{
							if(sub->HasCurvedEdges) _hasCurves=true;
							mOpenings->Add(sub);
						}
					}
					else
					{
						if(o->HasCurvedEdges) _hasCurves=true;
						mOpenings->Add(o);
					}
				}
			}

			////check to see if the result will have curved edges
			if(projections!=nullptr)	
			{
				//sort each opening in terms of the distance from the top left of the base bb and the bottom right of the opening
				mProjections = gcnew List<IXbimGeometryModel^>();	
				for each (IXbimGeometryModel^ p in projections)
				{
					if(p->HasCurvedEdges) _hasCurves=true;
					mProjections->Add(p);
				}
			}

			//make sure result shape is consistent
			mResultShape =  gcnew XbimSolid( mBaseShape, _hasCurves);
			double tenthMM = product->ModelOf->GetModelFactors->OneMilliMetre/10; //work to an accuracy of 1/10 millimeter
			ShapeFix_ShapeTolerance fTol;


			if(mProjections->Count>0)
			{
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
						mResultShape =  gcnew XbimSolid( mBaseShape, _hasCurves);//go back to start
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
			//sort them and hit test them
			if(mOpenings->Count>0)
			{

				BRep_Builder b;
				List<IXbimGeometryModel^>^ unprocessed = gcnew List<IXbimGeometryModel^>(mOpenings);

				while(unprocessed->Count>0)
				{	
					List<IXbimGeometryModel^>^ toProcess = gcnew List<IXbimGeometryModel^>(unprocessed);
					TopoDS_Compound comp;
					b.MakeCompound(comp); //make a compound to hold all the cuts
					bool first = true;
					//make a compound of all the openings that do not intersect and process in batch 
					int total = toProcess->Count+1;
					Handle(Bnd_HArray1OfBox) HBnd = new  Bnd_HArray1OfBox(1,total);		
					int boxArraySize = 0;
					for each(IXbimGeometryModel^ opening in toProcess) // quick cutting 
					{	
						if(first)
						{
							Bnd_Box openingBB;
							BRepBndLib::Add(*(opening->Handle), openingBB);
							HBnd->SetValue(++boxArraySize,openingBB);
							b.Add(comp,*(opening->Handle));
							first=false;
							unprocessed->Remove(opening);
						}
						else
						{
							Bnd_Box openingBB;
							BRepBndLib::Add(*(opening->Handle), openingBB);
							int hit = 0;
							for (int i = 1; i <= boxArraySize; i++) //try and find a cut that intersects with this one
							{
								if(!openingBB.IsOut(HBnd->Value(i)))
								{
									hit=i;
									break;
								}
							}
							if(hit==0) //if no intersection process it first time
							{
								HBnd->SetValue(++boxArraySize,openingBB);
								b.Add(comp,*(opening->Handle));
								unprocessed->Remove(opening);
							}		
						}
					}

					try
					{

						//try with reasonably fine tolerances
						fTol.SetTolerance(*(mResultShape->Handle), tenthMM);
						fTol.LimitTolerance(*(mResultShape->Handle), tenthMM,tenthMM*10); //   1/10 mmm
						fTol.SetTolerance(comp, tenthMM);					//1mm
						fTol.LimitTolerance(comp, tenthMM,tenthMM*10);	
						
						/*BRepTools::Write(comp, "c");
						BRepTools::Write(*(mResultShape->Handle), "b");*/
						if(!DoCut(comp) ) //try the fast option first if it is not a shell, if more than one opening try slow
						{
							//try more relaxed tolerances
							fTol.LimitTolerance(*(mResultShape->Handle), tenthMM,tenthMM*50); //   1/2 mmm
						    fTol.LimitTolerance(comp, tenthMM,tenthMM*50);					//5mm
							if(!DoCut(comp) ) //try again
							{
								//now try individual cutting of shells to get what we can
								bool failed = false;
								//getting harder, the geometry is most likley badly defined try each of the sub solids
								for (TopExp_Explorer ex(comp,TopAbs_SOLID) ; ex.More(); ex.Next())  
								{
									try 
									{	
										if(!DoCut(ex.Current()))
											failed=true;
									} catch(...) {failed=true;}
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
			/*BRepTools::Write(*(mResultShape->Handle), "x");
			product->ModelOf->Close();*/
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
			_representationLabel = copy->RepresentationLabel;
			_surfaceStyleLabel = copy->SurfaceStyleLabel;
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
				_hasCurves = copy->HasCurvedEdges;
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

		void XbimFeaturedShape::Move(TopLoc_Location location)
		{
			mResultShape->Move(location);
		}


		List<XbimTriangulatedModel^>^XbimFeaturedShape::Mesh()
		{
			return Mesh( true, XbimGeometryModel::DefaultDeflection);
		}

		List<XbimTriangulatedModel^>^XbimFeaturedShape::Mesh(bool withNormals )
		{
			return Mesh(withNormals, XbimGeometryModel::DefaultDeflection);
		}

		List<XbimTriangulatedModel^>^XbimFeaturedShape::Mesh(bool withNormals, double deflection )
		{
			return XbimGeometryModel::Mesh(mResultShape,withNormals,deflection, XbimMatrix3D::Identity);
			
		}
		
		List<XbimTriangulatedModel^>^XbimFeaturedShape::Mesh(bool withNormals, double deflection, XbimMatrix3D transform )
		{
			return XbimGeometryModel::Mesh(mResultShape,withNormals,deflection, transform);
			
		}
	}
}
