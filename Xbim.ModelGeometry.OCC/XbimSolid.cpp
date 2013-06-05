#include "StdAfx.h"
#include "XbimSolid.h"
#include "XbimShell.h"
#include "XbimGeometryModelCollection.h"
#include "XbimGeomPrim.h"
#include <TopoDS_Compound.hxx>
#include <TopoDS_Shell.hxx>
#include <TopExp_Explorer.hxx>
#include <BRepBuilderAPI_MakeSolid.hxx>
#include <BRepPrimAPI_MakeHalfSpace.hxx>
#include <BRepBuilderAPI_MakeFace.hxx>
#include <BRepPrimAPI_MakePrism.hxx>
#include <BRepBuilderAPI.hxx>
#include <BRepBuilderAPI_MakeWire.hxx>
#include <BRepPrimAPI_MakeRevol.hxx>
#include <BRepOffsetAPI_MakePipeShell.hxx>
#include <BRepAlgoAPI_Common.hxx>
#include <BRepAlgoAPI_Cut.hxx>
#include <BRepAlgoAPI_Fuse.hxx>
#include <Standard_PrimitiveTypes.hxx>
#include <BRepBuilderAPI_Transform.hxx>
#include <BRepBuilderAPI_GTransform.hxx>
#include <BRepBuilderAPI_FindPlane.hxx>
#include <BRepAlgo_Section.hxx>
#include <Geom_Plane.hxx>
#include <ShapeFix_Solid.hxx> 
#include <ShapeFix_Shell.hxx> 
#include <ShapeFix_Shape.hxx> 
#include <ShapeAnalysis_FreeBounds.hxx> 
#include <TopTools_HSequenceOfShape.hxx> 
#include <BRepBuilderAPI_Sewing.hxx> 
#include <ShapeUpgrade_ShellSewing.hxx> 
#include <BRepOffsetAPI_Sewing.hxx> 
#include <BRepLib.hxx>
#include <BRepAlgo_Cut.hxx>
#include <BRepCheck_Analyzer.hxx>
#include <ShapeFix_ShapeTolerance.hxx>
#include <BOPTools_DSFiller.hxx>
#include <Geom_Curve.hxx>
#include <ShapeAnalysis_Shell.hxx>

#include <gp_Circ.hxx>
#include <gp_Elips.hxx>
#include <GC_MakeCircle.hxx>
#include <GC_MakeEllipse.hxx>
#include <GC_MakeLine.hxx>
#include <BRepBuilderAPI_MakeFace.hxx>
#include <BRepBuilderAPI_MakeEdge.hxx>
#include <BRepOffsetAPI_MakePipe.hxx>
#include <TColgp_Array1OfPnt.hxx>
//#include <BRepPrimAPI_MakeSphere.hxx>
//#include <BRepPrimAPI_MakeCylinder.hxx>
using namespace Xbim::XbimExtensions;
using namespace Xbim::Ifc2x3::Extensions;
using namespace System::Diagnostics;
using namespace Xbim::Common::Exceptions;
using namespace Xbim::Common;
namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
		//constructors

		XbimSolid::XbimSolid(const TopoDS_Solid&  solid )
		{
			nativeHandle = new TopoDS_Solid();
			*nativeHandle = solid;
		};



		XbimSolid::XbimSolid(const TopoDS_Shape&  shape )
		{

			if(shape.ShapeType() == TopAbs_SOLID)
				nativeHandle = new TopoDS_Solid();
			else if(shape.ShapeType() == TopAbs_COMPOUND)
				nativeHandle = new TopoDS_Compound();
			else
				throw gcnew XbimGeometryException("Attempt to build a solid from an unexpected shape type");
			*nativeHandle=shape;
		};

		XbimSolid::XbimSolid(const TopoDS_Shape&  shape , bool hasCurves)
		{
			if(shape.ShapeType() == TopAbs_SOLID)
				nativeHandle = new TopoDS_Solid();
			else if(shape.ShapeType() == TopAbs_COMPOUND)
				nativeHandle = new TopoDS_Compound();
			else
				throw gcnew XbimGeometryException("Attempt to build a solid from an unexpected shape type");
			*nativeHandle=shape;
			_hasCurvedEdges = hasCurves;
		};
		

		XbimSolid::XbimSolid(const TopoDS_Shell&  shell)
		{
			nativeHandle = new TopoDS_Solid();
			ShapeFix_Solid sfs;
			*nativeHandle = TopoDS::Solid(sfs.SolidFromShell(shell));
		};
		
		XbimSolid::XbimSolid(const TopoDS_Solid&  solid, bool hasCurves)
		{
			nativeHandle = new TopoDS_Solid();
			*nativeHandle = solid;
			_hasCurvedEdges = hasCurves;
		};
		XbimSolid::XbimSolid(const TopoDS_Shell&  shell, bool hasCurves)
		{
			nativeHandle = new TopoDS_Solid();
			ShapeFix_Solid sfs;
			*nativeHandle = TopoDS::Solid(sfs.SolidFromShell(shell));
			_hasCurvedEdges = hasCurves;
		};

		XbimSolid::XbimSolid(XbimSolid^ solid, IfcAxis2Placement^ origin, IfcCartesianTransformationOperator^ transform, bool hasCurves)
		{
			_representationLabel = solid->RepresentationLabel;
			_surfaceStyleLabel = solid->SurfaceStyleLabel;
			TopoDS_Solid temp = *(((TopoDS_Solid*)solid->Handle));
			nativeHandle = new TopoDS_Solid();
			_hasCurvedEdges = solid->HasCurvedEdges;
			if(origin!=nullptr)
				temp.Move(XbimGeomPrim::ToLocation(origin));
			if(transform!=nullptr)
			{
				if(dynamic_cast<IfcCartesianTransformationOperator3DnonUniform^>( transform))
				{
					BRepBuilderAPI_GTransform gTran(temp,XbimGeomPrim::ToTransform((IfcCartesianTransformationOperator3DnonUniform^)transform));
					*nativeHandle = gTran.Shape();
					
				}
				else
				{
					BRepBuilderAPI_Transform gTran(temp,XbimGeomPrim::ToTransform(transform));
					*nativeHandle = gTran.Shape();
				}
			}
			else
				*nativeHandle = temp;
		};

		XbimSolid::XbimSolid(XbimGeometryModel^ solid, XbimMatrix3D transform)
		{
			_representationLabel = solid->RepresentationLabel;
			_surfaceStyleLabel = solid->SurfaceStyleLabel;
			TopoDS_Solid temp = *(((TopoDS_Solid*)solid->Handle));
			nativeHandle = new TopoDS_Solid();
			_hasCurvedEdges = solid->HasCurvedEdges;
			if(!transform.IsIdentity)	
			{
				//assume worst case a non-uniform transformation and use BRepBuilderAPI_GTransform
				BRepBuilderAPI_GTransform gTran(temp,XbimGeomPrim::ToTransform(transform));
				*nativeHandle = gTran.Shape();	
			}
		};

		XbimSolid::XbimSolid(XbimGeometryModel^ solid, bool hasCurves)
		{
			_representationLabel = solid->RepresentationLabel;
			_surfaceStyleLabel = solid->SurfaceStyleLabel;	
			nativeHandle = new TopoDS_Solid();
			*nativeHandle = *(((TopoDS_Solid*)solid->Handle));
			_hasCurvedEdges = hasCurves;	
		};

		XbimSolid::XbimSolid(IfcExtrudedAreaSolid^ repItem)
		{

			nativeHandle = new TopoDS_Solid();
			*nativeHandle = Build(repItem, _hasCurvedEdges);
		};

		
		XbimSolid::XbimSolid(IfcVertexPoint^ pt)

		{
			/*nativeHandle = new TopoDS_Solid();
			_hasCurvedEdges = true;
			double diameter = pt->ModelOf->GetModelFactors->VertxPointDiameter;
			BRepPrimAPI_MakeSphere sphere(diameter/2);
			*nativeHandle = sphere.Solid();*/
		};

		XbimSolid::XbimSolid(IfcEdge^ edge)

		{
			/*nativeHandle = new TopoDS_Solid();
			_hasCurvedEdges = true;
			double diameter = edge->ModelOf->GetModelFactors->VertxPointDiameter/2;
			BRepPrimAPI_MakeCylinder edge(diameter/2,edge->End, edge->EdgeStart);
			*nativeHandle = sphere.Solid();*/
		};

		XbimSolid::XbimSolid(IfcBooleanResult^ repItem)
		{
			nativeHandle = new TopoDS_Solid();
			*nativeHandle = Build(repItem, _hasCurvedEdges);
		};

		XbimSolid::XbimSolid(IfcRevolvedAreaSolid^ repItem)
		{	
			nativeHandle = new TopoDS_Solid();
			*nativeHandle = Build(repItem,_hasCurvedEdges);
		};

		XbimSolid::XbimSolid(IfcFacetedBrep^ repItem)
		{	
			nativeHandle = new TopoDS_Solid();
			*nativeHandle = Build(repItem, _hasCurvedEdges);
		};
		
		XbimSolid::XbimSolid(IfcHalfSpaceSolid^ repItem)
		{	
			nativeHandle = new TopoDS_Solid();
			*nativeHandle = Build(repItem, _hasCurvedEdges);
		};

		XbimSolid::XbimSolid(IfcClosedShell^ repItem)
		{	
			nativeHandle = new TopoDS_Solid();
			*nativeHandle = Build(repItem, _hasCurvedEdges);
		}

		XbimSolid::XbimSolid(IfcConnectedFaceSet^ repItem)
		{	
			nativeHandle = new TopoDS_Solid();
			*nativeHandle = Build(repItem, _hasCurvedEdges);
		}

		XbimSolid::XbimSolid(IfcSolidModel^ repItem)
		{	
			nativeHandle = new TopoDS_Solid();
			if(dynamic_cast<IfcManifoldSolidBrep^>(repItem))
				*nativeHandle = Build((IfcManifoldSolidBrep^)repItem,_hasCurvedEdges);
			else if(dynamic_cast<IfcSweptAreaSolid^>(repItem))
				*nativeHandle = Build((IfcSweptAreaSolid^)repItem,_hasCurvedEdges);
			else if(dynamic_cast<IfcCsgSolid^>(repItem))
				*nativeHandle = Build((IfcCsgSolid^)repItem,_hasCurvedEdges);
			else if(dynamic_cast<IfcSweptDiskSolid^>(repItem))
				*nativeHandle = Build((IfcSweptDiskSolid^)repItem,_hasCurvedEdges);
			else
			{
				Type^ type = repItem->GetType();
				throw gcnew XbimGeometryException("Error buiding solid from type " + type->Name);
			}

		};
		XbimSolid::XbimSolid(IfcCsgPrimitive3D^ repItem)
		{	
			throw gcnew NotImplementedException("Solid of type IfcCsgPrimitive3D is not imlpemented yet");
		};
		/*Interfaces*/

		

		System::Collections::Generic::IEnumerable<XbimFace^>^ XbimSolid::Faces::get()
		{

			return this;
		}



		//Solid operations
		XbimGeometryModel^ XbimSolid::Cut(XbimGeometryModel^ shape)
		{
			bool hasCurves =  _hasCurvedEdges || shape->HasCurvedEdges; //one has a curve the result will have one
			try
			{		
				/*BRepTools::Write(*nativeHandle, "b");
				BRepTools::Write(*(shape->Handle), "c");*/
				BRepAlgoAPI_Cut boolOp(*nativeHandle,*(shape->Handle));
				if(boolOp.ErrorStatus() == 0)
				{
					if( BRepCheck_Analyzer(boolOp.Shape(), Standard_False).IsValid() == Standard_False) 
					{
						//try again slackening tolerances
						ShapeFix_ShapeTolerance fTol;
						double prec = BRepBuilderAPI::Precision();
						fTol.LimitTolerance(*nativeHandle, prec*10,prec*1000);
						fTol.LimitTolerance(*(shape->Handle),prec*10,prec*1000);
						BRepAlgoAPI_Cut boolOp2(*nativeHandle,*(shape->Handle));
						if(boolOp2.ErrorStatus() == 0  )
						{	
							if(BRepCheck_Analyzer(boolOp2.Shape(), Standard_False).IsValid() == Standard_True)
								return gcnew XbimSolid(boolOp2.Shape(), hasCurves);
							else //try and fix it
							{
								ShapeFix_Shape sfs;
								sfs.SetPrecision(prec);
								sfs.SetMaxTolerance( prec);
								sfs.SetMinTolerance(prec);
								//sfs.FixSolidMode()=1;
								sfs.Init(boolOp2.Shape());
								if(sfs.Perform())
									return gcnew XbimSolid(sfs.Shape(), hasCurves);
								//getting hard just take any solids
								BRep_Builder b;
								TopoDS_Compound c;
								b.MakeCompound(c);
								for (TopExp_Explorer solidEx(boolOp2.Shape(),TopAbs_SOLID) ; solidEx.More(); solidEx.Next())  
								{
									b.Add(c,solidEx.Current());		
								}
								return  gcnew XbimSolid(c, hasCurves);;
							}
						}
						else
						{
							//we have a messed up shape, best option is to ignore operation normally
							return this;//stick with what we started with
						}

					}
					else
					{
						return gcnew XbimSolid(boolOp.Shape(), hasCurves);
					}
				}
				else //try adjusting the tolerance to make it work
				{
					ShapeFix_ShapeTolerance fTol;
					double prec = BRepBuilderAPI::Precision();
					fTol.LimitTolerance(*nativeHandle, prec*100,prec*1000);
					fTol.LimitTolerance(*(shape->Handle),prec*100,prec*1000);
					BRepAlgoAPI_Cut boolOp2(*nativeHandle,*(shape->Handle));
					if(boolOp2.ErrorStatus() == 0  ) //let Brep error go
						return gcnew XbimSolid(boolOp2.Shape(), hasCurves);
				}
			}
			catch(...) //some internal cascade failure
			{
			}
			throw gcnew XbimGeometryException("Failed to form difference between two shapes");
		}

		XbimGeometryModel^ XbimSolid::Union(XbimGeometryModel^ shape)
		{
			bool hasCurves =  _hasCurvedEdges || shape->HasCurvedEdges; //one has a curve the result will have one
			BRepAlgoAPI_Fuse boolOp(*nativeHandle,*(shape->Handle));

			if(boolOp.ErrorStatus() == 0) //find the solid
			{ 
				const TopoDS_Shape & res = boolOp.Shape();
				if(res.ShapeType() == TopAbs_SOLID)
					return gcnew XbimSolid(TopoDS::Solid(res), hasCurves);
				else if(res.ShapeType() == TopAbs_SHELL)	
					return gcnew XbimShell(TopoDS::Shell(res), hasCurves);
				else if(res.ShapeType() == TopAbs_COMPOUND || res.ShapeType() == TopAbs_COMPSOLID)
					for (TopExp_Explorer solidEx(res,TopAbs_SOLID) ; solidEx.More(); solidEx.Next())  
						return gcnew XbimSolid(TopoDS::Solid(solidEx.Current()), hasCurves);
			}
			Logger->Warn("Failed to form Union between two shapes");
			return nullptr;
		}
		XbimGeometryModel^ XbimSolid::Intersection(XbimGeometryModel^ shape)
		{
			bool hasCurves =  _hasCurvedEdges || shape->HasCurvedEdges; //one has a curve the result will have one
			BRepAlgoAPI_Common boolOp(*nativeHandle,*(shape->Handle));

			if(boolOp.ErrorStatus() == 0) //find the solid
			{ 
				const TopoDS_Shape & res = boolOp.Shape();
				if(res.ShapeType() == TopAbs_SOLID)
					return gcnew XbimSolid(TopoDS::Solid(res), hasCurves);
				else if(res.ShapeType() == TopAbs_SHELL)	
					return gcnew XbimShell(TopoDS::Shell(res), hasCurves);
				else if(res.ShapeType() == TopAbs_COMPOUND || res.ShapeType() == TopAbs_COMPSOLID)
					for (TopExp_Explorer solidEx(res,TopAbs_SOLID) ; solidEx.More(); solidEx.Next())  
						return gcnew XbimSolid(TopoDS::Solid(solidEx.Current()), hasCurves);
			}
			Logger->Warn("Failed to form intersection between two shapes");
			return nullptr;
		}
		XbimGeometryModel^ XbimSolid::CopyTo(IfcObjectPlacement^ placement)
		{
			if(dynamic_cast<IfcLocalPlacement^>(placement))
			{
				TopoDS_Shape movedShape = *nativeHandle;
				IfcLocalPlacement^ lp = (IfcLocalPlacement^)placement;
				movedShape.Move(XbimGeomPrim::ToLocation(lp->RelativePlacement));
				return gcnew XbimSolid(movedShape, _hasCurvedEdges);
			}
			else
				throw(gcnew NotSupportedException("XbimSolid::CopyTo only supports IfcLocalPlacement type"));

		}

		void  XbimSolid::Move(TopLoc_Location location)
		{
			(*nativeHandle).Move(location);
		}

		//Static Builders



		TopoDS_Shape XbimSolid::Build(IfcFacetedBrep^ repItem, bool% hasCurves)
		{		
			return XbimSolid::Build(repItem->Outer, hasCurves);
		}

		TopoDS_Shape XbimSolid::Build(IfcConnectedFaceSet^ repItem, bool% hasCurves)
		{
			TopoDS_Shape shell = XbimShell::Build(repItem, hasCurves);
			if(shell.IsNull())
			{
				Logger->WarnFormat("Error performing solid operation for entity #{0}={1}\n{2}\nIt was null and has been ignored",repItem->EntityLabel,repItem->GetType()->Name);
				return TopoDS_Shape(); //failed return an empty shape, it is an invalid solid and will cause problems
			}
			else
			{
				if(shell.ShapeType() == TopAbs_SOLID)
					return TopoDS::Solid(shell); //our work is done
				else if(shell.ShapeType() == TopAbs_SHELL)
				{
					try 
					{
						ShapeFix_Solid sf_solid;
						double mm = repItem->ModelOf->GetModelFactors->OneMilliMetre;
						sf_solid.LimitTolerance(mm/10);
						sf_solid.SetPrecision(mm/10);
						//sf_solid.CreateOpenSolidMode () = true; //fix the shell if it is not a solid
						return sf_solid.SolidFromShell(TopoDS::Shell(shell));
					}
					catch(...) 
					{
						Logger->WarnFormat("Error performing solid operation for entity #{0}={1}\n{2}\nIt was an open shell and should have been a solid it has been ignored",repItem->EntityLabel,repItem->GetType()->Name);
						
					}
					return TopoDS_Shape(); //failed return an empty shape, it is an invalid solid and will cause problems
				}
				else if(shell.ShapeType() == TopAbs_COMPOUND) //grab every shell and try and solidify it
				{
					BRep_Builder b;
					TopoDS_Compound solids;
					b.MakeCompound(solids);
					ShapeFix_Solid sf_solid;
					double mm = repItem->ModelOf->GetModelFactors->OneMilliMetre;
					sf_solid.LimitTolerance(mm/10);
					sf_solid.SetPrecision(mm/10);
					//sf_solid.CreateOpenSolidMode () = true; //fix the shell if it is not a solid
					for (TopExp_Explorer ex(shell,TopAbs_SHELL) ; ex.More(); ex.Next())  //try and make any valid shell into a  solid, this is illegal geometry anyway,typically from Revit in 2013 and prior versions
					{
						try 
						{
							
							TopoDS_Solid solid =  sf_solid.SolidFromShell(TopoDS::Shell(ex.Current()));
							b.Add(solids,solid);
						}
						catch(...) //ignore if shell to solid failed
						{
							Logger->WarnFormat("Error performing solid operation for entity #{0}={1}\n{2}\nIt was an illegal shell and could not be made into a solid it has been ignored",repItem->EntityLabel,repItem->GetType()->Name);
						}
					}
					return solids; 
					
				}
				
			}
			return TopoDS_Shape(); //failed return an empty shape, it is an invalid solid and will cause problems
		}

		TopoDS_Shape XbimSolid::Build(IfcClosedShell^ cShell, bool% hasCurves)
		{
			return Build((IfcConnectedFaceSet^)cShell, hasCurves);
		}

		

		TopoDS_Shape XbimSolid::Build(IfcManifoldSolidBrep^ manifold, bool% hasCurves)
		{
			if(dynamic_cast<IfcFacetedBrep^>(manifold))
				return Build((IfcFacetedBrep^)manifold, hasCurves);
			throw gcnew NotImplementedException("Build::IfcManifoldSolidBrep subtype is not implemented");
		}

		TopoDS_Shape XbimSolid::Build(IfcCsgSolid^ csgSolid, bool% hasCurves)
		{
			if(dynamic_cast<IfcBooleanResult^>(csgSolid->TreeRootExpression)) 
			{
				IfcBooleanResult^ br = (IfcBooleanResult^)csgSolid->TreeRootExpression;
				return Build(br, hasCurves);
				
			}
			else
				throw gcnew NotImplementedException("Build::IfcCsgSolid is not implemented");
		}

		TopoDS_Shape XbimSolid::Build(IfcBooleanResult^ repItem, bool% hasCurves)
		{
			IfcBooleanOperand^ fOp= repItem->FirstOperand;
			IfcBooleanOperand^ sOp= repItem->SecondOperand;
			XbimGeometryModel^ shape1;
			XbimGeometryModel^ shape2;
			System::Nullable<bool> _shape1IsSolid;
			if(dynamic_cast<IfcBooleanResult^>(fOp))
				shape1 = gcnew XbimSolid((IfcBooleanResult^)fOp);
			else if(dynamic_cast<IfcSolidModel^>(fOp))
				shape1 = gcnew XbimSolid((IfcSolidModel^)fOp);
			else if(dynamic_cast<IfcHalfSpaceSolid^>(fOp))
			{
				shape1 = gcnew XbimSolid((IfcHalfSpaceSolid^)fOp);
				if(dynamic_cast<IfcBoxedHalfSpace^>(fOp))
					_shape1IsSolid = false;
			}
			else if(dynamic_cast<IfcCsgPrimitive3D^>(fOp))
				shape1 = gcnew XbimSolid((IfcCsgPrimitive3D^)fOp);
			else
				throw(gcnew XbimException("XbimGeometryModel. Build(BooleanResult) FirstOperand must be a valid IfcBooleanOperand"));


			try
			{

				if(dynamic_cast<IfcBooleanResult^>(sOp))
					shape2 = gcnew XbimSolid((IfcBooleanResult^)sOp);
				else if(dynamic_cast<IfcSolidModel^>(sOp))
					shape2 = gcnew XbimSolid((IfcSolidModel^)sOp);
				else if(dynamic_cast<IfcHalfSpaceSolid^>(sOp))
				{
					shape2 = gcnew XbimSolid((IfcHalfSpaceSolid^)sOp);
					if(dynamic_cast<IfcBoxedHalfSpace^>(sOp))
						_shape1IsSolid = true;
				}
				else if(dynamic_cast<IfcCsgPrimitive3D^>(sOp))
					shape2 = gcnew XbimSolid((IfcCsgPrimitive3D^)sOp);
				else
					throw(gcnew XbimException("XbimGeometryModel. Build(BooleanResult) FirstOperand must be a valid IfcBooleanOperand"));

				//check if we have boxed half spaces then see if there is any intersect
				if(_shape1IsSolid.HasValue)
				{

					if(!shape1->Intersects(shape2))
					{
						if(_shape1IsSolid.Value == true)
						{
							hasCurves = shape1->HasCurvedEdges;
							return TopoDS_Shape(*shape1->Handle); 
						}
						else
						{
							hasCurves = shape2->HasCurvedEdges;
							return TopoDS_Shape(*shape2->Handle);
						}
					}

				}

				if((*(shape2->Handle)).IsNull())
				{
					hasCurves = shape1->HasCurvedEdges;
					return TopoDS_Shape(*(shape1->Handle)); //nothing to subtract
				}
				switch(repItem->Operator)
				{
				case IfcBooleanOperator::Union:
					{	
						XbimGeometryModel^ m = shape1->Union(shape2);
						hasCurves = m->HasCurvedEdges;
						return TopoDS_Shape(*m->Handle);  
					}
				case IfcBooleanOperator::Intersection:
					{	
						XbimGeometryModel^ m = shape1->Intersection(shape2);
						hasCurves = m->HasCurvedEdges;
						return TopoDS_Shape(*m->Handle);  
					}
				case IfcBooleanOperator::Difference:
					{	
						XbimGeometryModel^ m = shape1->Cut(shape2);
						hasCurves = m->HasCurvedEdges;
						return TopoDS_Shape(*m->Handle);  
					}
				default:
					throw(gcnew InvalidOperationException("XbimGeometryModel. Build(BooleanClippingResult) Unsupported Operation"));
				}
			}
			catch(XbimGeometryException^ xbimE)
			{
				Logger->WarnFormat("Error performing boolean operation for entity #{0}={1}\n{2}\nA simplified version has been used",repItem->EntityLabel,repItem->GetType()->Name,xbimE->Message);
				return TopoDS_Shape(*shape1->Handle);  ;
			}
		}


		TopoDS_Solid XbimSolid::Build(IfcSweptDiskSolid^ swdSolid, bool% hasCurves)
		{
			
			//Build the directrix
			TopoDS_Wire sweep;
			bool isConic = (dynamic_cast<IfcConic^>(swdSolid->Directrix)!=nullptr);
			gp_Ax2 ax2(gp_Pnt(0.,0.,0.),gp_Dir(0.,0.,1.));
			XbimModelFactors^ mf = ((IPersistIfcEntity^)swdSolid)->ModelOf->GetModelFactors;

			double parameterFactor =  mf->LengthToMetresConversionFactor;
			Handle(Geom_Curve) curve;
			if(isConic)
			{
				//it could be based on a circle, ellipse or line
				if(dynamic_cast<IfcCircle^>(swdSolid->Directrix))
				{
					hasCurves=true;
					IfcCircle^ c = (IfcCircle^) swdSolid->Directrix;
					if(dynamic_cast<IfcAxis2Placement2D^>(c->Position))
					{
						IfcAxis2Placement2D^ ax2 = (IfcAxis2Placement2D^)c->Position;
						gp_Ax2 gpax2(gp_Pnt(ax2->Location->X, ax2->Location->Y,0), gp_Dir(0,0,1),gp_Dir(ax2->P[0]->X, ax2->P[0]->Y,0.));			
						gp_Circ gc(gpax2,c->Radius);
						curve = GC_MakeCircle(gc);
					}
					else if(dynamic_cast<IfcAxis2Placement3D^>(c->Position))
					{
						IfcAxis2Placement3D^ ax2 = (IfcAxis2Placement3D^)c->Position;
						gp_Ax3 	gpax3 = XbimGeomPrim::ToAx3(ax2);		
						gp_Circ gc(gpax3.Ax2(),c->Radius);	
						curve = GC_MakeCircle(gc);
					}	
					else
					{
						Type ^ type = c->Position->GetType();
						throw(gcnew NotImplementedException(String::Format("XbimFaceBound. Circle with Placement of type {0} is not implemented",type->Name)));	
					}
				}
				else if (dynamic_cast<IfcEllipse^>(swdSolid->Directrix))
				{
					hasCurves=true;
					IfcEllipse^ c = (IfcEllipse^) swdSolid->Directrix;

					if(dynamic_cast<IfcAxis2Placement2D^>(c->Position))
					{
						IfcAxis2Placement2D^ ax2 = (IfcAxis2Placement2D^)c->Position;
						double s1;
						double s2;
						if( c->SemiAxis1 > c->SemiAxis2)
						{
							s1=c->SemiAxis1;
							s2=c->SemiAxis2;
						}
						else //either same or two is larger than 1
						{
							s1=c->SemiAxis2;
							s2=c->SemiAxis1;
						}

						gp_Ax2 gpax2(gp_Pnt(ax2->Location->X, ax2->Location->Y,0), gp_Dir(0,0,1),gp_Dir(ax2->P[0]->X, ax2->P[0]->Y,0.));	

						gp_Elips gc(gpax2,s1, s2);
						curve = GC_MakeEllipse(gc);
					}
					else if(dynamic_cast<IfcAxis2Placement3D^>(c->Position))
					{
						Type ^ type = c->Position->GetType();
						throw(gcnew NotImplementedException(String::Format("XbimSolid. Ellipse with Placement of type {0} is not implemented",type->Name)));	
					}
					else
					{
						Type ^ type = c->Position->GetType();
						throw(gcnew NotImplementedException(String::Format("XbimSolid. Ellipse with Placement of type {0} is not implemented",type->Name)));	
					}
				}
				BRepBuilderAPI_MakeWire w;
				double flt1 = (double)(swdSolid->StartParam.Value) * parameterFactor;
				double flt2 = (double)(swdSolid->EndParam.Value) * parameterFactor;
				if ( isConic && Math::Abs(Math::IEEERemainder(flt2-flt1,(double)(Math::PI*2.0))-0.0f) < BRepBuilderAPI::Precision()) 
				{
					w.Add(BRepBuilderAPI_MakeEdge(curve));
				} 
				else 
				{
					BRepBuilderAPI_MakeEdge e (curve, flt1, flt2);
					w.Add(e.Edge());
				}
				sweep = w.Wire();
			}
			else if (dynamic_cast<IfcLine^>(swdSolid->Directrix))
			{
				IfcLine^ line = (IfcLine^)(swdSolid->Directrix);
				IfcCartesianPoint^ cp = line->Pnt;

				IfcVector^ dir = line->Dir;
				gp_Pnt pnt(cp->X,cp->Y,cp->Z);
				XbimVector3D v3d = dir->XbimVector3D();
				gp_Vec vec(v3d.X,v3d.Y,v3d.Z);
				curve = GC_MakeLine(pnt,vec);
				sweep = BRepBuilderAPI_MakeWire(BRepBuilderAPI_MakeEdge(GC_MakeLine(pnt,vec),0,dir->Magnitude));
			}
			else if(dynamic_cast<IfcCompositeCurve^>(swdSolid->Directrix))
			{
				sweep = XbimFaceBound::Build((IfcCompositeCurve^)(swdSolid->Directrix),hasCurves);
			}
			else
			{
				Type ^ type = swdSolid->Directrix->GetType();
				throw(gcnew NotImplementedException(String::Format("XbimSolid. CompositeCurveSegments with BasisCurve of type {0} is not implemented",type->Name)));	
			}

			//build the surface to sweep
			//make the outer wire
			gp_Circ outer(ax2,swdSolid->Radius);
			Handle(Geom_Circle) hOuter = GC_MakeCircle(outer);
			TopoDS_Edge outerEdge = BRepBuilderAPI_MakeEdge(hOuter);
			BRepBuilderAPI_MakeWire outerWire;
			outerWire.Add(outerEdge);
					
			//BRepTools::Write(faceBlder.Face(),"f");
			
			BRepOffsetAPI_MakePipeShell pipeMaker(sweep);
			pipeMaker.Add(outerWire.Wire(),Standard_True, Standard_True);
			pipeMaker.Build();
			if(pipeMaker.IsDone() && pipeMaker.MakeSolid())
			{ 
				TopoDS_Shape result = pipeMaker.Shape();
				
				//now add inner wire if it is defined
				/*if(swdSolid->InnerRadius.HasValue)
				{
				gp_Circ inner(ax2,swdSolid->InnerRadius.Value);
				Handle(Geom_Circle) hInner = GC_MakeCircle(inner);
				TopoDS_Edge innerEdge = BRepBuilderAPI_MakeEdge(hInner);
				BRepBuilderAPI_MakeWire innerWire;
				innerWire.Add(innerEdge);
				faceBlder.Add(innerWire);
				}*/
				return TopoDS::Solid(result);
			}
			else
			{
				Logger->WarnFormat( "Entity #" + swdSolid->EntityLabel.ToString() + ", IfcSweptDiskSolid could not be constructed ");
				return TopoDS_Solid();
			}
		}
		
		TopoDS_Solid XbimSolid::Build(IfcSweptAreaSolid^ sweptAreaSolid, bool% hasCurves)
		{
			if(dynamic_cast<IfcExtrudedAreaSolid^>(sweptAreaSolid))
				return Build((IfcExtrudedAreaSolid^)sweptAreaSolid, hasCurves);
			else if(dynamic_cast<IfcRevolvedAreaSolid^>(sweptAreaSolid))
				return Build((IfcRevolvedAreaSolid^)sweptAreaSolid, hasCurves);
			else if(dynamic_cast<IfcSurfaceCurveSweptAreaSolid^>(sweptAreaSolid))
				return Build((IfcSurfaceCurveSweptAreaSolid^)sweptAreaSolid, hasCurves);
			else
			{
				Type ^ type = sweptAreaSolid->GetType();
				throw(gcnew NotImplementedException(String::Format("XbimSolid. SweptAreaSolid of type {0} is not implemented",type->Name)));
			}
		}


		TopoDS_Solid XbimSolid::Build(IfcExtrudedAreaSolid^ repItem, bool% hasCurves)
		{
			TopoDS_Face face = XbimFace::Build(repItem->SweptArea,hasCurves);

			if(!face.IsNull() &&repItem->Depth<=0)
			{
				XbimModelFactors^ mf = ((IPersistIfcEntity^)repItem)->ModelOf->GetModelFactors;
				/*Logger->WarnFormat(String::Format("Invalid Solid Extrusion, Extrusion Depth must be >0, found in Entity #{0}=IfcExtrudedAreaSolid\nIt has been ignored",
					repItem->EntityLabel));*/
				//use a very thin 1mm extrusion
				TopoDS_Solid solid = Build(face,repItem->ExtrudedDirection , mf->OneMilliMetre/10, hasCurves);
				solid.Move(XbimGeomPrim::ToLocation(repItem->Position));
				return  solid;
			}
			if(!face.IsNull())
			{

				TopoDS_Solid solid = Build(face,repItem->ExtrudedDirection , repItem->Depth, hasCurves);
				solid.Move(XbimGeomPrim::ToLocation(repItem->Position));
				return  solid;
			}
			else
			{
				//a null face indicates an invalid solid, 
				return TopoDS_Solid();
			}
		}


		TopoDS_Solid XbimSolid::Build(IfcRevolvedAreaSolid^ repItem, bool% hasCurves)
		{
			// gettin the face right is necessary before the revolution can be performed.
			//
			TopoDS_Face face;
			if(dynamic_cast<IfcArbitraryClosedProfileDef^>(repItem->SweptArea)) 
				face =  XbimFace::Build((IfcArbitraryClosedProfileDef^)repItem->SweptArea, hasCurves);
			else if(dynamic_cast<IfcRectangleProfileDef^>(repItem->SweptArea))
				face = XbimFace::Build((IfcRectangleProfileDef^)repItem->SweptArea, hasCurves);	
			else if(dynamic_cast<IfcCircleProfileDef^>(repItem->SweptArea))
				face = XbimFace::Build((IfcCircleProfileDef^)repItem->SweptArea, hasCurves);	
			else
			{
				Type ^ type = repItem->SweptArea->GetType();
				throw(gcnew NotImplementedException(String::Format("XbimSolid. Could not BuildShape of type {0}. It is not implemented",type->Name)));
			}

			// Here we need to prepare the revolution.
			//
		
			TopoDS_Solid solid = Build(face,repItem->Axis, repItem->Angle, hasCurves);
			solid.Move(XbimGeomPrim::ToLocation(repItem->Position));
			return  solid;
		}

		TopoDS_Solid XbimSolid::Build(IfcSurfaceCurveSweptAreaSolid^ repItem, bool% hasCurves)
		{
			TopoDS_Wire profile;
			if(dynamic_cast<IfcArbitraryProfileDefWithVoids^>(repItem->SweptArea)) 
				profile =  XbimFaceBound::Build((IfcArbitraryProfileDefWithVoids^)repItem->SweptArea, hasCurves);
			else if(dynamic_cast<IfcArbitraryClosedProfileDef^>(repItem->SweptArea)) 
				profile =  XbimFaceBound::Build((IfcArbitraryClosedProfileDef^)repItem->SweptArea, hasCurves);
			else if(dynamic_cast<IfcRectangleProfileDef^>(repItem->SweptArea))
				profile = XbimFaceBound::Build((IfcRectangleProfileDef^)repItem->SweptArea, hasCurves);	
			else if(dynamic_cast<IfcCircleProfileDef^>(repItem->SweptArea))
				profile = XbimFaceBound::Build((IfcCircleProfileDef^)repItem->SweptArea, hasCurves);	
			else
			{
				Type ^ type = repItem->SweptArea->GetType();
				Logger->WarnFormat(String::Format("XbimSolid. Could not BuildShape of type {0}. It is not implemented",type->Name));
				return TopoDS_Solid();
			}
						//profile.Move(XbimGeomPrim::ToLocation(repItem->Position));
			TopoDS_Wire sweep = XbimFaceBound::Build(repItem->Directrix, hasCurves);
			BRepOffsetAPI_MakePipeShell pipeMaker(sweep);
			
			if(dynamic_cast<IfcPlane^>(repItem->ReferenceSurface))
			{
				IfcPlane^ ifcPlane = (IfcPlane^)repItem->ReferenceSurface;
				gp_Ax3 ax3 = XbimGeomPrim::ToAx3(ifcPlane->Position);
				pipeMaker.SetMode(ax3.Direction());
				//find the start position of the sweep
				BRepTools_WireExplorer wExp(sweep);
				Standard_Real start = 0;
				Standard_Real end = 1;
				Handle_Geom_Curve curve = BRep_Tool::Curve(wExp.Current(),start, end);
				gp_Pnt p1;
				gp_Vec tangent;
				curve->D1(0, p1, tangent);
				const TopoDS_Vertex firstPoint = wExp.CurrentVertex();
				gp_Ax3 toAx3(BRep_Tool::Pnt(firstPoint),tangent, ax3.Direction());	//rotate so normal of profile is tangental and X axis 
				gp_Trsf trsf;
				trsf.SetTransformation(toAx3,gp_Ax3());
				TopLoc_Location topLoc(trsf);			
				profile.Location(topLoc);
				pipeMaker.Add(profile,Standard_False, Standard_False);
			}
			else
			{
				Logger->WarnFormat( "Entity #" + repItem->EntityLabel.ToString() + ", IfcSurfaceCurveSweptAreaSolid has a Non-Planar surface");
				pipeMaker.SetMode(Standard_False); //use auto calculation of tangent and binormal
				pipeMaker.Add(profile,Standard_False, Standard_True);
			}

			pipeMaker.SetTransitionMode(BRepBuilderAPI_RightCorner);
			pipeMaker.Build();
			if(pipeMaker.IsDone() && pipeMaker.MakeSolid())
			{ 
				TopoDS_Shape result = pipeMaker.Shape();
				result.Move(XbimGeomPrim::ToLocation(repItem->Position));
				return TopoDS::Solid(result);
			}
			else
			{
				Logger->WarnFormat( "Entity #" + repItem->EntityLabel.ToString() + ", IfcSurfaceCurveSweptAreaSolid could not be constructed ");
				return TopoDS_Solid();
			}
		}

		TopoDS_Solid XbimSolid::MakeHalfSpace(IfcHalfSpaceSolid^ hs, bool% hasCurves, bool shift)
		{
			IfcSurface^ surface = (IfcSurface^)hs->BaseSurface;
			if(!dynamic_cast<IfcPlane^>(surface)) throw gcnew Exception("Non-Planar half spaces are not supported");
			IfcPlane^ ifcPlane = (IfcPlane^)surface;
			gp_Ax3 ax3 = XbimGeomPrim::ToAx3(ifcPlane->Position);
			gp_Pln pln(ax3);
			gp_Vec direction = hs->AgreementFlag ? -pln.Axis().Direction() : pln.Axis().Direction();
			const gp_Pnt pnt = pln.Location().Translated(direction );
			if(shift)
				pln.SetLocation(pln.Location().Translated(direction * 10 * -BRepBuilderAPI::Precision())); //shift a little to avoid covergent faces
			return BRepPrimAPI_MakeHalfSpace(BRepBuilderAPI_MakeFace(pln),pnt).Solid();
	
		}


		TopoDS_Solid XbimSolid::Build(IfcHalfSpaceSolid^ hs, bool% hasCurves)
		{
			if(dynamic_cast<IfcPolygonalBoundedHalfSpace^>(hs))
				return Build((IfcPolygonalBoundedHalfSpace^)hs, hasCurves);
			else if (dynamic_cast<IfcBoxedHalfSpace^>(hs))
				return Build((IfcBoxedHalfSpace^)hs, hasCurves);
			else //it is a simple Half space
			{
				return MakeHalfSpace(hs, hasCurves, false);
			}
		}

		TopoDS_Solid XbimSolid::Build(IfcPolygonalBoundedHalfSpace^ pbhs, bool% hasCurves)
		{
			
				//creates polygon and its plane normal direction
				gp_Ax3 ax3Polygon = XbimGeomPrim::ToAx3(pbhs->Position);
				gp_Dir normPolygon = ax3Polygon.Direction();	
				TopoDS_Wire wire =  XbimFaceBound::Build(pbhs->PolygonalBoundary, hasCurves); //get the polygon
				BRepBuilderAPI_MakeFace makeFace(wire);
				TopoDS_Face face = makeFace.Face();
				if(face.IsNull()) 
				{
					Logger->WarnFormat("The IfcPolygonalBoundedHalfSpace #{0} has an icorrectly defined PolygonalBoundary #{1}, it has been ignored",pbhs->EntityLabel,pbhs->PolygonalBoundary->EntityLabel);
					return TopoDS_Solid(); //the face is illegal
				}

				gp_Trsf toPos = XbimGeomPrim::ToTransform(pbhs->Position);
				face.Move(toPos);	
	
				TopoDS_Shape pris = BRepPrimAPI_MakePrism(face, gp_Vec(normPolygon)*2e6); //create infinite extrusion,  this is a work around as infinite half space don't work properly in open cascade
				//Move the prism so that it approximates to infinit in both directions
				gp_Trsf away; 
				away.SetTranslation(gp_Vec(normPolygon)*-1e6);
				pris.Move(away);

				TopoDS_Solid hs = MakeHalfSpace((IfcHalfSpaceSolid^)pbhs,hasCurves,false );//cast to build the half space

				BRepAlgoAPI_Common joiner(pris, hs);

				if(joiner.ErrorStatus() == 0) //find the solid and return it, else throw an exception
				{
					TopoDS_Shape result = joiner.Shape();
					if( BRepCheck_Analyzer(result).IsValid() == 0) //try and move half space in case it is co-planar with a face. This cause OpenCascade to delete the face and make an illegal solid
					{
						TopoDS_Solid hsMoved = MakeHalfSpace((IfcHalfSpaceSolid^)pbhs,hasCurves, true );//cast to build the half space

						BRepAlgoAPI_Common joiner2(pris, hsMoved);
						if(BRepCheck_Analyzer(joiner2.Shape()).IsValid() != 0)
							result = joiner2.Shape();
						else //these shapes have nothing in common, so just return an empty solid
							return TopoDS_Solid();
					}

					if(result.ShapeType() == TopAbs_SOLID) //if we have a solid just send it
					{
						return TopoDS::Solid(result);
					}

					for (TopExp_Explorer solidEx(result,TopAbs_SOLID) ; solidEx.More(); solidEx.Next())  
					{
						return TopoDS::Solid(solidEx.Current());
					}
				}
		
				throw gcnew XbimGeometryException("Failed to create polygonally bounded half space");
		
		}


		TopoDS_Solid XbimSolid::Build(IfcBoxedHalfSpace^ bhs, bool% hasCurves)
		{
			IfcSurface^ surface = (IfcSurface^)bhs->BaseSurface;
			if(dynamic_cast<IfcPlane^>(surface))
			{
				IfcPlane^ ifcPlane = (IfcPlane^) surface;
				gp_Ax3 ax3BaseSurface = XbimGeomPrim::ToAx3(ifcPlane->Position);
				gp_Pln plnBaseSurface(ax3BaseSurface);
				gp_Dir normBaseSurface = plnBaseSurface.Axis().Direction();   
				gp_Vec zVec(normBaseSurface);
				gp_Pnt pnt(ax3BaseSurface.Location());
				if(bhs->AgreementFlag) zVec.Reverse();
				pnt.Translate(zVec);
				TopoDS_Face faceBase = BRepBuilderAPI_MakeFace(plnBaseSurface);
				BRepPrimAPI_MakeHalfSpace halfSpaceBulder(faceBase, pnt);
				return halfSpaceBulder.Solid();
			}
			throw gcnew XbimGeometryException("Only planar boxed half spaces are valid for building IfcBoxedHalfSpace");
		}


		TopoDS_Solid XbimSolid::Build(const TopoDS_Wire & wire, gp_Dir dir, bool% hasCurves)
		{
			BRepBuilderAPI_MakeFace faceBlder(wire);
			BRepPrimAPI_MakePrism prism(faceBlder.Face() , dir);
			TopoDS_Solid solid = TopoDS::Solid(prism.Shape());
			return solid;
		}


		TopoDS_Shell XbimSolid::Build(const TopoDS_Wire & wire, IfcDirection^ dir, double depth, bool% hasCurves)
		{
			gp_Vec vec(dir->X,dir->Y,dir->Z );
			vec*= depth;
			BRepPrimAPI_MakePrism prism(wire , vec);
			TopoDS_Shell shell = TopoDS::Shell(prism.Shape());
			return shell;
		}

		TopoDS_Solid XbimSolid::Build(const TopoDS_Face & face, IfcDirection^ dir, double depth, bool% hasCurves)
		{
			// TODO: when depth is 0 this throws an exception
			//
			gp_Vec vec(dir->X,dir->Y,dir->Z );
			vec*= depth;
			BRepPrimAPI_MakePrism prism(face , vec);

			return TopoDS::Solid(prism.Shape());
		}

		TopoDS_Solid XbimSolid::Build(const TopoDS_Face & face, IfcAxis1Placement^ revolaxis, double angle, bool% hasCurves)
		{
			hasCurves=true;
			gp_Pnt Orig(
				revolaxis->Location->X,
				revolaxis->Location->Y,
				revolaxis->Location->Z
				);

			gp_Dir Vx(
				revolaxis->Axis->X,
				revolaxis->Axis->Y,
				revolaxis->Axis->Z
				);

			gp_Ax1 ax1(Orig,Vx);

			BRepPrimAPI_MakeRevol revol(face , ax1, angle);
			TopoDS_Solid solid =TopoDS::Solid(revol.Shape());
			return solid;
		}
		
		void XbimSolid::Print()
		{
			int c = 1;
			System::Diagnostics::Debug::WriteLine("Solid");
			for each(XbimFace^ face in this->Faces)
			{
				System::Diagnostics::Debug::WriteLine("Face " + c);
				face->Print();
			}
			System::Diagnostics::Debug::WriteLine("End Solid");
		}
		}
	}
}




