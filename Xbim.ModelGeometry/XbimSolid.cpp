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

#include <BRepAlgoAPI_Common.hxx>
#include <BRepAlgoAPI_Cut.hxx>
#include <BRepAlgoAPI_Fuse.hxx>
#include <Standard_PrimitiveTypes.hxx>
#include <BRepBuilderAPI_Transform.hxx>
#include <BRepBuilderAPI_FindPlane.hxx>
#include <BRepAlgo_Section.hxx>
#include <Geom_Plane.hxx>
#include <ShapeFix_Solid.hxx> 
#include <ShapeFix_Shell.hxx> 
#include <ShapeAnalysis_FreeBounds.hxx> 
#include <TopTools_HSequenceOfShape.hxx> 
#include <BRepBuilderAPI_Sewing.hxx> 
#include <ShapeUpgrade_ShellSewing.hxx> 
using namespace Xbim::XbimExtensions;
using namespace Xbim::Ifc::Extensions;
using namespace System::Windows::Media::Media3D;
using namespace System::Diagnostics;

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
			if(origin!=nullptr)
				temp.Move(XbimGeomPrim::ToLocation(origin));
			if(transform!=nullptr)
			{
				BRepBuilderAPI_Transform gTran(temp,XbimGeomPrim::ToTransform(transform));
				*nativeHandle =TopoDS::Solid( gTran.Shape());
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
				throw gcnew Exception("Error buiding solid from type " + type->Name);
			}

		};
		XbimSolid::XbimSolid(IfcCsgPrimitive3D^ repItem)
		{	
			throw gcnew Exception("Error. Solid of type IfcCsgPrimitive3D is not impemented yet");
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

		XbimTriangulatedModelStream^ XbimSolid::Mesh()
		{
			return Mesh(true, XbimGeometryModel::DefaultDeflection, Matrix3D::Identity);
		}

		XbimTriangulatedModelStream^ XbimSolid::Mesh(bool withNormals )
		{
			return Mesh(withNormals, XbimGeometryModel::DefaultDeflection, Matrix3D::Identity);
		}

		XbimTriangulatedModelStream^ XbimSolid::Mesh( bool withNormals, double deflection )
		{
			return XbimGeometryModel::Mesh(this,withNormals,deflection, Matrix3D::Identity);
			
		}

		XbimTriangulatedModelStream^ XbimSolid::Mesh(bool withNormals, double deflection, Matrix3D transform )
		{
			return XbimGeometryModel::Mesh(this,withNormals,deflection, transform);
			
		}

		//Solid operations
		IXbimGeometryModel^ XbimSolid::Cut(IXbimGeometryModel^ shape)
		{
			bool hasCurves =  _hasCurvedEdges || shape->HasCurvedEdges; //one has a curve the result will have one
			BRepAlgoAPI_Cut boolOp(*nativeHandle,*(shape->Handle));
		
			if(boolOp.ErrorStatus() == 0) //find the solid
			{ 
				const TopoDS_Shape & res = boolOp.Shape();
				if(res.ShapeType() == TopAbs_SOLID)
					return gcnew XbimSolid(TopoDS::Solid(res), hasCurves);
				else if(res.ShapeType() == TopAbs_SHELL)	
				{
					ShapeFix_Shell shellFix(TopoDS::Shell(res));
					shellFix.Perform();
					ShapeFix_Solid sfs;
					sfs.CreateOpenSolidMode() = Standard_True;
					return gcnew XbimSolid(sfs.SolidFromShell(shellFix.Shell()), hasCurves);
				}
				else if(res.ShapeType() == TopAbs_COMPOUND || res.ShapeType() == TopAbs_COMPSOLID)
					for (TopExp_Explorer solidEx(res,TopAbs_SOLID) ; solidEx.More(); solidEx.Next())  
						return gcnew XbimSolid(TopoDS::Solid(solidEx.Current()), hasCurves);
			}
			System::Diagnostics::Debug::WriteLine("Failed to form difference between two shapes");
			return nullptr;
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
			System::Diagnostics::Debug::WriteLine("Failed to form Union between two shapes");
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
			System::Diagnostics::Debug::WriteLine("Failed to form intersection between two shapes");
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
				throw(gcnew Exception("XbimSolid::CopyTo only supports IfcLocalPlacement type"));

		}
		//Static Builders



		TopoDS_Solid XbimSolid::Build(IfcFacetedBrep^ repItem, bool% hasCurves)
		{
			
			TopoDS_Shell shell = XbimShell::Build(repItem->Outer, hasCurves);
			/*ShapeFix_Shell fixer(shell);
			fixer.Perform();
			shell= fixer.Shell();
			GProp_GProps System;
			BRepGProp::VolumeProperties(shell, System, Standard_True);
			if(System.Mass() <0)
				shell.Reverse();*/
			BRep_Builder b;
			TopoDS_Solid solid;
			b.MakeSolid(solid);
			b.Add(solid, shell);
			return solid;

		}

		TopoDS_Solid XbimSolid::Build(IfcClosedShell^ cShell, bool% hasCurves)
		{
			TopoDS_Shell shell = XbimShell::Build(cShell, hasCurves);
			/*ShapeFix_Shell fixer(shell);
			fixer.Perform();
			shell= fixer.Shell();
			GProp_GProps System;
			BRepGProp::VolumeProperties(shell, System, Standard_True);
			if(System.Mass() <0)
				shell.Reverse();*/
			BRep_Builder b;
			TopoDS_Solid solid;
			b.MakeSolid(solid);
			b.Add(solid, shell);
			return solid;;

		}
		TopoDS_Solid XbimSolid::Build(IfcManifoldSolidBrep^ manifold, bool% hasCurves)
		{
			if(dynamic_cast<IfcFacetedBrep^>(manifold))
				return Build((IfcFacetedBrep^)manifold, hasCurves);
			throw gcnew Exception("Build::IfcManifoldSolidBrep subtype  is not implemented");
		}

		TopoDS_Solid XbimSolid::Build(IfcCsgSolid^ csgSolid, bool% hasCurves)
		{
			throw gcnew Exception("Build::IfcCsgSolid is not implemented");
		}

		TopoDS_Solid XbimSolid::Build(IfcSweptDiskSolid^ swdSolid, bool% hasCurves)
		{
			throw gcnew Exception("Build::IfcSweptDiskSolid is not implemented");
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
				throw(gcnew Exception(String::Format("XbimSolid. SweptAreaSolid of type {0} is not implemented",type->Name)));
			}
		}


		TopoDS_Solid XbimSolid::Build(IfcExtrudedAreaSolid^ repItem, bool% hasCurves)
		{

			TopoDS_Face face;
			if(dynamic_cast<IfcArbitraryClosedProfileDef^>(repItem->SweptArea)) 
				face =  XbimFace::Build((IfcArbitraryClosedProfileDef^)repItem->SweptArea,hasCurves);
			else if(dynamic_cast<IfcRectangleProfileDef^>(repItem->SweptArea))
				face = XbimFace::Build((IfcRectangleProfileDef^)repItem->SweptArea,hasCurves);	
			else if(dynamic_cast<IfcCircleProfileDef^>(repItem->SweptArea))
				face = XbimFace::Build((IfcCircleProfileDef^)repItem->SweptArea,hasCurves);	

			// AK: these are the ones that were giving errors
			else if(dynamic_cast<IfcLShapeProfileDef^>(repItem->SweptArea))
				face = XbimFace::Build((IfcLShapeProfileDef^)repItem->SweptArea,hasCurves);	
			else if(dynamic_cast<IfcUShapeProfileDef^>(repItem->SweptArea))
				face = XbimFace::Build((IfcUShapeProfileDef^)repItem->SweptArea,hasCurves);	
			else if(dynamic_cast<IfcIShapeProfileDef^>(repItem->SweptArea))
				face = XbimFace::Build((IfcIShapeProfileDef^)repItem->SweptArea,hasCurves);
			else
			{
				Type ^ type = repItem->SweptArea->GetType();
				throw(gcnew Exception(String::Format("XbimSolid. Could not BuildShape of type {0}. It is not implemented",type->Name)));
			}
			TopoDS_Solid solid = Build(face,repItem->ExtrudedDirection , repItem->Depth, hasCurves);
			solid.Move(XbimGeomPrim::ToLocation(repItem->Position));
			return  solid;
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
				throw(gcnew Exception(String::Format("XbimSolid. Could not BuildShape of type {0}. It is not implemented",type->Name)));
			}

			// Here we need to prepare the revolution.
			//
			TopoDS_Solid solid = Build(face,repItem->Axis, repItem->Angle, hasCurves);
			solid.Move(XbimGeomPrim::ToLocation(repItem->Position));
			return  solid;
		}

		TopoDS_Solid XbimSolid::Build(IfcSurfaceCurveSweptAreaSolid^ repItem, bool% hasCurves)
		{
			throw(gcnew Exception("XbimSolid. Support for SurfaceCurveSweptAreaSolid is not implemented"));
		}


		TopoDS_Solid XbimSolid::Build(IfcHalfSpaceSolid^ hs, bool% hasCurves)
		{
			if(dynamic_cast<IfcPolygonalBoundedHalfSpace^>(hs))
				return Build((IfcPolygonalBoundedHalfSpace^)hs, hasCurves);
			else if (dynamic_cast<IfcBoxedHalfSpace^>(hs))
				return Build((IfcBoxedHalfSpace^)hs, hasCurves);
			else //it is a simple Half space
			{
				IfcSurface^ surface = (IfcSurface^)hs->BaseSurface;
				TopoDS_Face face = XbimFace::Build(surface, hasCurves);
				IfcAxis2Placement3D^ axis3D = ((IPlacement3D^)surface)->Position;
				Vector3D zDir = Axis2Placement3DExtensions::ZAxisDirection(axis3D);
				gp_Vec zVec(zDir.X,zDir.Y,zDir.Z);
				gp_Pnt pnt(axis3D->Location->X,axis3D->Location->Y,axis3D->Location->Z);
				if(hs->AgreementFlag) zVec.Reverse();
				pnt.Translate(zVec);

				BRepPrimAPI_MakeHalfSpace halfSpaceBulder(face, pnt);

				return halfSpaceBulder.Solid();		
			}
		}

		TopoDS_Solid XbimSolid::Build(IfcPolygonalBoundedHalfSpace^ pbhs, bool% hasCurves)
		{
			
				//creates polygon and its plane normal direction
				gp_Ax3 ax3Polygon = XbimGeomPrim::ToAx3(pbhs->Position);
				gp_Dir normPolygon = ax3Polygon.Direction();	
				TopoDS_Wire wire =  XbimFaceBound::Build(pbhs->PolygonalBoundary, hasCurves); //get the polygon

				BRepBuilderAPI_MakeFace makeFace(wire, Standard_True);
				TopoDS_Face face = makeFace.Face();

				//move the face to create a very big prism, this is a work around as infinite half space don't work properly in open cascade
				gp_Trsf trsf;
				gp_Pnt origin = ax3Polygon.Location();
				origin.Translate(gp_Vec(normPolygon) * -1e8);
				ax3Polygon.SetLocation(origin);
				trsf.SetTransformation(ax3Polygon,gp_Ax3());	
				face.Move(TopLoc_Location(trsf));

				IfcSurface^ surface = pbhs->BaseSurface; //find the base surface
				TopoDS_Face cutFace = XbimFace::Build(surface, hasCurves);


				//BRepPrimAPI_MakePrism mpris(face, normPolygon, Standard_True); //create infinite extrusion
				BRepPrimAPI_MakePrism mpris(face,  gp_Vec(normPolygon) * 2e8); //create approx to infinite extrusion
				TopoDS_Solid prism = TopoDS::Solid(mpris.Shape());
				
					IfcAxis2Placement3D^ axis3D = ((IPlacement3D^)surface)->Position;
					Vector3D zDir = Axis2Placement3DExtensions::ZAxisDirection(axis3D);
					gp_Vec zVec(zDir.X,zDir.Y,zDir.Z);
					gp_Pnt pnt(axis3D->Location->X,axis3D->Location->Y,axis3D->Location->Z);
					if(pbhs->AgreementFlag) zVec.Reverse();
					pnt.Translate(zVec);
					BRepPrimAPI_MakeHalfSpace halfSpaceBulder(cutFace, pnt);

					TopoDS_Solid halfSpace = halfSpaceBulder.Solid();		

					//find common semi-infinite extrusion with half space
					BRepAlgoAPI_Common boolOp(prism, halfSpace);

					if(boolOp.ErrorStatus() == 0) //find the solid
					{ 

						const TopoDS_Shape & res = boolOp.Shape();
						for (TopExp_Explorer solidEx(res,TopAbs_SOLID) ; solidEx.More(); solidEx.Next())  
							return TopoDS::Solid(solidEx.Current());
					}
				
				System::Diagnostics::Debug::WriteLine("Failed create polygonally bounded half space, returning just half space");
				return prism; //just return the half space as the bound has failed		
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
			throw gcnew Exception("Only planar boxed half spaces are valid for building IfcBoxedHalfSpace");
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




