#include "StdAfx.h"
#include "XbimSolid.h"
#include "XbimShell.h"
#include "XbimGeometryModelCollection.h"
#include "XbimGeomPrim.h"
#include "XbimMeshedFaceEnumerator.h"
#include <TopoDS_Compound.hxx>
#include <TopoDS_Shell.hxx>
#include <TopExp_Explorer.hxx>
#include <BRepBuilderAPI_MakeSolid.hxx>
#include <BRepPrimAPI_MakeHalfSpace.hxx>
#include <BRepBuilderAPI_MakeFace.hxx>
#include <BRepPrimAPI_MakePrism.hxx>
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
#include <ShapeAnalysis_FreeBounds.hxx> 
#include <TopTools_HSequenceOfShape.hxx> 
#include <BRepBuilderAPI_Sewing.hxx> 
#include <ShapeUpgrade_ShellSewing.hxx> 
#include <BRepOffsetAPI_Sewing.hxx> 
#include <BRepLib.hxx>
#include <BRepAlgo_Cut.hxx>
#include <BRepCheck_Analyzer.hxx>
using namespace Xbim::XbimExtensions;
using namespace Xbim::Ifc2x3::Extensions;
using namespace System::Windows::Media::Media3D;
using namespace System::Diagnostics;
using namespace Xbim::Common::Exceptions;

namespace Xbim
{
	namespace ModelGeometry
	{
		//constructors

		XbimSolid::XbimSolid(const TopoDS_Solid&  solid )
		{
			nativeHandle = new TopoDS_Solid();
			*nativeHandle = solid;
		};



		XbimSolid::XbimSolid(const TopoDS_Shape&  shape )
		{

			TopoDS_Compound * pComp = new TopoDS_Compound();
			BRep_Builder b;
			b.MakeCompound(*pComp);
			b.Add(*pComp, shape);
			nativeHandle=pComp;
		};

		XbimSolid::XbimSolid(const TopoDS_Shape&  shape , bool hasCurves)
		{

			TopoDS_Compound * pComp = new TopoDS_Compound();
			BRep_Builder b;
			b.MakeCompound(*pComp);
			b.Add(*pComp, shape);
			nativeHandle=pComp;
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
					*nativeHandle =TopoDS::Solid( gTran.Shape());
					
				}
				else
				{
					BRepBuilderAPI_Transform gTran(temp,XbimGeomPrim::ToTransform(transform));
					*nativeHandle =TopoDS::Solid( gTran.Shape());
				}
			}
			else
				*nativeHandle = temp;
		};

		XbimSolid::XbimSolid(IfcExtrudedAreaSolid^ repItem)
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
		};


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

		System::Collections::Generic::IEnumerable<XbimMeshedFace^>^ XbimSolid::MeshedFaces::get()
		{

			return gcnew XbimMeshedFaceEnumerable(*nativeHandle);
		}

		XbimTriangulatedModelCollection^ XbimSolid::Mesh()
		{
			return Mesh(true, XbimGeometryModel::DefaultDeflection, Matrix3D::Identity);
		}

		XbimTriangulatedModelCollection^ XbimSolid::Mesh(bool withNormals )
		{
			return Mesh(withNormals, XbimGeometryModel::DefaultDeflection, Matrix3D::Identity);
		}

		XbimTriangulatedModelCollection^ XbimSolid::Mesh( bool withNormals, double deflection )
		{
			return XbimGeometryModel::Mesh(this,withNormals,deflection, Matrix3D::Identity);
			
		}

		XbimTriangulatedModelCollection^ XbimSolid::Mesh(bool withNormals, double deflection, Matrix3D transform )
		{
			return XbimGeometryModel::Mesh(this,withNormals,deflection, transform);
			
		}

		//Solid operations
		IXbimGeometryModel^ XbimSolid::Cut(IXbimGeometryModel^ shape)
		{
			bool hasCurves =  _hasCurvedEdges || shape->HasCurvedEdges; //one has a curve the result will have one
			try
			{

				BRepAlgoAPI_Cut boolOp(*nativeHandle,*(shape->Handle));
				if(boolOp.ErrorStatus() == 0)
				{
					TopoDS_Shape res = boolOp.Shape();
					if(res.IsNull()) return this; //nothing happened, stay as we were 
					if(res.ShapeType() == TopAbs_SOLID)
						return gcnew XbimSolid(TopoDS::Solid(res), hasCurves);
					if(res.ShapeType() == TopAbs_COMPOUND)
					{
						TopExp_Explorer compExp(res,TopAbs_SOLID);
						if(compExp.More())	return gcnew XbimSolid(TopoDS::Solid(TopoDS::Solid(compExp.Current())));//grab the first solid 
						compExp.Init(res,TopAbs_SHELL);
						if(compExp.More())	res = compExp.Current();//grab the first shell and solidify in next block
					}
					if(res.ShapeType() == TopAbs_SHELL)
					{
						try 
						{
							ShapeFix_Solid sf_solid;
							sf_solid.LimitTolerance(BRepLib::Precision());
							return gcnew XbimSolid(sf_solid.SolidFromShell(TopoDS::Shell(res)));
						} catch(...) 
						{
						}
					}
					XbimBoundingBox^ bb1 = GetBoundingBox(true);
					XbimBoundingBox^ bb2 = shape->GetBoundingBox(true);
					if(!bb1->Intersects(bb2)) //the two shapes never intersected
						return this; //just return what we had in the first place
					//totally invalid shape give up and throw an error
				}
			}
			catch(...) //some internal cascade failure
			{
			}
			throw gcnew XbimGeometryException("Failed to form difference between two shapes");
		}

		IXbimGeometryModel^ XbimSolid::Union(IXbimGeometryModel^ shape)
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
		IXbimGeometryModel^ XbimSolid::Intersection(IXbimGeometryModel^ shape)
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
		IXbimGeometryModel^ XbimSolid::CopyTo(IfcObjectPlacement^ placement)
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
		//Static Builders



		TopoDS_Shape XbimSolid::Build(IfcFacetedBrep^ repItem, bool% hasCurves)
		{
			
			return XbimSolid::Build(repItem->Outer, hasCurves);


		}

		TopoDS_Shape XbimSolid::Build(IfcClosedShell^ cShell, bool% hasCurves)
		{

			TopoDS_Shape shell = XbimShell::Build(cShell, hasCurves);
		
			BRepOffsetAPI_Sewing builder;
			builder.SetTolerance(BRepLib::Precision());
			builder.SetMaxTolerance(BRepLib::Precision());
			builder.SetMinTolerance(BRepLib::Precision());
			TopExp_Explorer exp(shell,TopAbs_FACE);
			if ( exp.More() )
			{
				for ( ; exp.More(); exp.Next() ) {
					TopoDS_Face face = TopoDS::Face(exp.Current());
					builder.Add(face);
				}
				builder.Perform();
				shell = builder.SewedShape();
				
				if(shell.ShapeType() == TopAbs_SOLID) return TopoDS::Solid(shell);
				if(shell.ShapeType() == TopAbs_COMPOUND)
				{
					TopExp_Explorer compExp(shell,TopAbs_SOLID);
					if(compExp.More())	return TopoDS::Solid(TopoDS::Solid(compExp.Current()));//grab the first solid 
					compExp.Init(shell,TopAbs_SHELL);
					if(compExp.More())	shell = compExp.Current();//grab the first shell and solidift in next block
				}
				if(shell.ShapeType() == TopAbs_SHELL)
				{
					try {
						ShapeFix_Solid sf_solid;
						sf_solid.LimitTolerance(BRepLib::Precision());
						return sf_solid.SolidFromShell(TopoDS::Shell(shell));
					} catch(...) 
					{
					}
				}
				return shell;
			}
			throw gcnew XbimException(String::Format("Failed to build a solid #{0} of type IfcClosedShell",cShell->GetType()->Name));
			
		}
		TopoDS_Shape XbimSolid::Build(IfcManifoldSolidBrep^ manifold, bool% hasCurves)
		{
			if(dynamic_cast<IfcFacetedBrep^>(manifold))
				return Build((IfcFacetedBrep^)manifold, hasCurves);
			throw gcnew NotImplementedException("Build::IfcManifoldSolidBrep subtype is not implemented");
		}

		TopoDS_Solid XbimSolid::Build(IfcCsgSolid^ csgSolid, bool% hasCurves)
		{
			throw gcnew NotImplementedException("Build::IfcCsgSolid is not implemented");
		}

		TopoDS_Solid XbimSolid::Build(IfcSweptDiskSolid^ swdSolid, bool% hasCurves)
		{
			throw gcnew NotImplementedException("Build::IfcSweptDiskSolid is not implemented");
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
			if(repItem->Depth<=0)
			{
				Logger->WarnFormat(String::Format("Invalid Solid Extrusion, Extrusion Depth must be >0, found in Entity #{0}=IfcExtrudedAreaSolid\nIt has been ignored",
					repItem->EntityLabel));
				return TopoDS_Solid();
			}
		
			TopoDS_Face face = XbimFace::Build(repItem->SweptArea,hasCurves);
			if(!face.IsNull())
			{
				TopoDS_Solid solid = Build(face,repItem->ExtrudedDirection , repItem->Depth, hasCurves);
				solid.Move(XbimGeomPrim::ToLocation(repItem->Position));
				return  solid;
			}
			else
			{
				Type ^ type = repItem->SweptArea->GetType();
				Logger->WarnFormat(String::Format("The face definition for {0} = #{1} is illegal and does not form a solid. It has been ignored",type->Name,
					repItem->SweptArea->EntityLabel));
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
			profile.Move(XbimGeomPrim::ToLocation(repItem->Position));
			TopoDS_Wire sweep = XbimFaceBound::Build(repItem->Directrix, hasCurves);

			BRepOffsetAPI_MakePipeShell pipeMaker(sweep);
			pipeMaker.Add(profile);
			if(dynamic_cast<IfcPlane^>(repItem->ReferenceSurface))
			{
				IfcPlane^ ifcPlane = (IfcPlane^)repItem->ReferenceSurface;
				gp_Ax3 ax3 = XbimGeomPrim::ToAx3(ifcPlane->Position);
				pipeMaker.SetMode(ax3.Direction());
			}
			else
				Logger->WarnFormat( "Entity #" + repItem->EntityLabel.ToString() + ", IfcSurfaceCurveSweptAreaSolid has a Non-Planar surface");
			pipeMaker.Build();
			if(pipeMaker.IsDone() && pipeMaker.MakeSolid())
				return TopoDS::Solid(pipeMaker.Shape());
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
				pln.SetLocation(pln.Location().Translated(direction * 10 * -BRepLib::Precision())); //shift a little to avoid covergent faces
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
			
			gp_Trsf toPos = XbimGeomPrim::ToTransform(pbhs->Position);
			face.Move(toPos);			
			TopoDS_Shape pris = BRepPrimAPI_MakePrism(face, gp_Vec(normPolygon)*2e7); //create infinite extrusion,  this is a work around as infinite half space don't work properly in open cascade
			//Move the prism so that it approximates to infinit in both directions
			gp_Trsf away; 
			away.SetTranslation(gp_Vec(normPolygon)*-1e7);
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
			throw gcnew XbimGeometryException("Failed create polygonally bounded half space");
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




