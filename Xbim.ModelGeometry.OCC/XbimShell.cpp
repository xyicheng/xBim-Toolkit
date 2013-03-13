#include "StdAfx.h"
#include "XbimShell.h"
#include "XbimSolid.h"
#include "XbimGeometryModelCollection.h"
#include "XbimGeomPrim.h"
#include <TopoDS_Face.hxx>
#include <TopoDS_Solid.hxx>
#include <BRep_Builder.hxx>
#include <BRepBuilderAPI_MakeFace.hxx>
#include <BRepBuilderAPI_Transform.hxx>
#include <BRepBuilderAPI_GTransform.hxx>
#include <ShapeFix_Solid.hxx> 
#include <ShapeFix_Shell.hxx> 
#include <BRepTools.hxx> 
#include <ShapeFix_IntersectionTool.hxx> 
#include <ShapeBuild_ReShape.hxx> 
#include <ShapeFix_ShapeTolerance.hxx>
#include <BRepCheck_Analyzer.hxx> 
#include <Handle_BRepCheck_Result.hxx> 
#include <ShapeFix_Face.hxx> 
#include <ShapeExtend_Status.hxx> 
#include <BRepLib_FuseEdges.hxx> 
#include <BRepAlgoAPI_Cut.hxx> 
#include <BRepAlgoAPI_Fuse.hxx> 
#include <BRepAlgoAPI_Common.hxx> 
#include <BRep_Tool.hxx>
#include <ShapeFix_Shape.hxx>
#include <BRepBuilderAPI_FindPlane.hxx>
#include <BRepBuilderAPI_MakeWire.hxx>
#include <BRepBuilderAPI_MakeFace.hxx>
#include <BRepBuilderAPI_MakeEdge.hxx>
#include <GeomLProp_SLProps.hxx>
#include <Geom_Plane.hxx>
#include <ShapeFix_Face.hxx>
#include <BRepLib_FindSurface.hxx>
#include <BRepBuilderAPI_Sewing.hxx>
#include <ShapeUpgrade_ShellSewing.hxx>
#include <TopTools_Array1OfShape.hxx>
#include <TopTools_DataMapOfIntegerShape.hxx>
#include <BRepBuilderAPI_MakePolygon.hxx>
#include <BRepLib.hxx>
#include <BRepOffsetAPI_Sewing.hxx>
using namespace System::Linq;
using namespace System::Diagnostics;
using namespace System::Windows::Media::Media3D;
using namespace Xbim::Common::Exceptions;
using namespace Xbim::Common;
using namespace Xbim::XbimExtensions;
namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
			IEnumerable<XbimFace^>^ XbimShell::CfsFaces::get()
			{

				return this;
			}

			XbimShell::XbimShell(const TopoDS_Shape & shape)
			{
				pShell = new TopoDS_Shape();
				*pShell = shape;
			}

			XbimShell::XbimShell(const TopoDS_Shape & shape, bool hasCurves )
			{
				pShell = new TopoDS_Shape();
				*pShell = shape;
				_hasCurvedEdges = hasCurves;
			}

			List<XbimTriangulatedModel^>^XbimShell::Mesh()
			{
				return Mesh(true, XbimGeometryModel::DefaultDeflection, Matrix3D::Identity);
			}

			List<XbimTriangulatedModel^>^XbimShell::Mesh( bool withNormals )
			{
				return Mesh(withNormals, XbimGeometryModel::DefaultDeflection, Matrix3D::Identity);
			}

			List<XbimTriangulatedModel^>^XbimShell::Mesh(bool withNormals, double deflection )
			{

				return XbimGeometryModel::Mesh(this,withNormals,deflection, Matrix3D::Identity);

			}

			List<XbimTriangulatedModel^>^XbimShell::Mesh(bool withNormals, double deflection, Matrix3D transform )
			{
				return XbimGeometryModel::Mesh(this,withNormals,deflection, transform);

			}

			XbimShell::XbimShell(XbimShell^ shell, IfcAxis2Placement^ origin, IfcCartesianTransformationOperator^ transform, bool hasCurves )
			{
				_hasCurvedEdges = hasCurves;
				_representationLabel = shell->RepresentationLabel;
				_surfaceStyleLabel = shell->SurfaceStyleLabel;
				TopoDS_Shape temp = *(shell->Handle);
				pShell = new TopoDS_Shape();
				if(origin!=nullptr)
					temp.Move(XbimGeomPrim::ToLocation(origin));
				if(transform!=nullptr)
				{	
					if(dynamic_cast<IfcCartesianTransformationOperator3DnonUniform^>( transform))
					{
						BRepBuilderAPI_GTransform gTran(temp,XbimGeomPrim::ToTransform((IfcCartesianTransformationOperator3DnonUniform^)transform));
						*pShell = gTran.Shape();
					}
					else
					{
						BRepBuilderAPI_Transform gTran(temp,XbimGeomPrim::ToTransform(transform));
						*pShell =gTran.Shape();
					}
				}
				else
					*pShell = temp;
			};

			XbimShell::XbimShell(IfcConnectedFaceSet^ faceSet)
			{
				pShell = new TopoDS_Shell();
				*pShell = Build(faceSet, _hasCurvedEdges);

			}
			XbimShell::XbimShell(IfcClosedShell^ shell)
			{
				pShell = new TopoDS_Shell();
				*pShell = Build(shell, _hasCurvedEdges);
			}


			XbimShell::XbimShell(IfcOpenShell^ shell)
			{
				pShell = new TopoDS_Shell();
				*pShell = Build(shell, _hasCurvedEdges);
			}

			/*Interfaces*/


			IXbimGeometryModel^ XbimShell::Cut(IXbimGeometryModel^ shape)
			{
				throw gcnew XbimGeometryException("A cut operation has been applied to a shell (non-solid) object this is illegal according to schema");
			}

			IXbimGeometryModel^ XbimShell::Union(IXbimGeometryModel^ shape)
			{
				throw gcnew XbimGeometryException("A Union operation has been applied to a shell (non-solid) object this is illegal according to schema");
			}

			IXbimGeometryModel^ XbimShell::Intersection(IXbimGeometryModel^ shape)
			{
				throw gcnew XbimGeometryException("A Intersection operation has been applied to a shell (non-solid) object this is illegal according to schema");
			}
			IXbimGeometryModel^ XbimShell::CopyTo(IfcObjectPlacement^ placement)
			{
				if(dynamic_cast<IfcLocalPlacement^>(placement))
				{
					TopoDS_Shape movedShape = *pShell;
					IfcLocalPlacement^ lp = (IfcLocalPlacement^)placement;
					movedShape.Move(XbimGeomPrim::ToLocation(lp->RelativePlacement));
					return gcnew XbimShell(movedShape, _hasCurvedEdges);
				}
				else
					throw(gcnew NotSupportedException("XbimShell::CopyTo only supports IfcLocalPlacement type"));

			}

			void XbimShell::Move(TopLoc_Location location)
			{
				(*pShell).Move(location);		
			}

			TopoDS_Shape XbimShell::Build(IfcOpenShell^ shell, bool% hasCurves)
			{
				return Build((IfcConnectedFaceSet^)shell, hasCurves);
			}


			TopoDS_Shape XbimShell::Build(IfcClosedShell^ shell, bool% hasCurves)
			{
				TopoDS_Shape topoShell= Build((IfcConnectedFaceSet^)shell, hasCurves);
				topoShell.Closed(Standard_True);
				return topoShell;
			}

			TopoDS_Shape XbimShell::Build(IfcConnectedFaceSet^ faceSet, bool% hasCurves)
			{
				int facesAdded=0;
				XbimModelFactors^ mf = ((IPersistIfcEntity^)faceSet)->ModelOf->GetModelFactors;
				ShapeFix_ShapeTolerance fTol;
				BRepOffsetAPI_Sewing seamstress;
				double mm = mf->OneMilliMetre;
				seamstress.SetMinTolerance(mm/10);
				seamstress.SetMaxTolerance(mm);
				seamstress.SetTolerance(mm/10);

				for each ( IfcFace^ fc in  faceSet->CfsFaces)
				{
					IfcFaceBound^ outerBound = Enumerable::FirstOrDefault(Enumerable::OfType<IfcFaceOuterBound^>(fc->Bounds)); //get the outer bound
					if(outerBound == nullptr) outerBound = Enumerable::FirstOrDefault(fc->Bounds); //if one not defined explicitly use first found
					if(outerBound == nullptr ) break; //invalid face
					bool sense = outerBound->Orientation; //we are going to ignore this and calc the orientation as some BIM Ifc exporters do not respect

					if(dynamic_cast<IfcPolyLoop^>(outerBound->Bound))
					{
						IfcPolyLoop^ polyLoop=(IfcPolyLoop^)outerBound->Bound;
						bool hasCurves;

						TopoDS_Wire wire = XbimFaceBound::Build(polyLoop,hasCurves);

						if(wire.IsNull()) continue;
						fTol.SetTolerance(wire, mm/10 , TopAbs_WIRE);
						BRepBuilderAPI_MakeFace* faceBuilder=new BRepBuilderAPI_MakeFace(wire, Standard_True);
						BRepBuilderAPI_FaceError err = faceBuilder->Error();
						if ( err == BRepBuilderAPI_NotPlanar )
						{
							fTol.SetTolerance(wire, mm , TopAbs_WIRE); //drop the tolerance as some BIM tools have easy fitting planes
							delete faceBuilder;
							faceBuilder = new BRepBuilderAPI_MakeFace(wire);
							err = faceBuilder->Error();
						}
						if ( err != BRepBuilderAPI_FaceDone )
						{
							//bad face just ignore
							//Logger->WarnFormat("XbimShell::Build(ConnectedFaceSet) a legal face could not be built from the geometry Bound = #{0}, a valid plane could not be calculated",outerBound->EntityLabel);
							delete faceBuilder;
							continue;
						}
						gp_Vec nn =  XbimFaceBound::NewellsNormal(wire);
						if(fc->Bounds->Count>1) //do inner bounds
						{
							for each (IfcFaceBound^ innerBound in fc->Bounds)
							{
								if(innerBound!=outerBound) //already handled the outer bound
								{
									if(dynamic_cast<IfcPolyLoop^>(innerBound->Bound))
									{
										TopoDS_Wire innerWire = XbimFaceBound::Build((IfcPolyLoop^)(innerBound->Bound),hasCurves);
										if(!innerWire.IsNull()) 
										{
											gp_Vec inorm =  XbimFaceBound::NewellsNormal(innerWire);
											if ( inorm.Dot(nn) >= 0 ) //inner wire should be reverse of outer wire
											{
												TopAbs_Orientation o = innerWire.Orientation();
												innerWire.Orientation(o == TopAbs_FORWARD ? TopAbs_REVERSED : TopAbs_FORWARD);
											}
											faceBuilder->Add(innerWire);
										}
										else
											Logger->WarnFormat("XbimShell::Build(ConnectedFaceSet) an inner bound could not be built from the geometry Bound = #{0}, a valid wire could not be calculated",innerBound->Bound->EntityLabel);
									}
									else
									{
										Logger->WarnFormat("XbimShell::Build(ConnectedFaceSet) loops of type {0} are not implemented, Loop id = #{1}", 
											innerBound->Bound->GetType()->ToString(), innerBound->Bound->EntityLabel);
									}

								}
							}
						}
						if ( faceBuilder->IsDone() ) //managed to make face OK
						{
							//make sure the face is correctly oriented
							TopoDS_Face face = faceBuilder->Face();						
							gp_Vec tn = XbimFace::TopoNormal(face);
							if ( tn.Dot(nn) < 0 ) 
							{
								TopAbs_Orientation o = face.Orientation();
								face.Orientation(o == TopAbs_FORWARD ? TopAbs_REVERSED : TopAbs_FORWARD);
							}
							seamstress.Add(face);

							facesAdded++;
						}
						delete faceBuilder;
					}
					else
					{
						Logger->WarnFormat("XbimShell::Build(ConnectedFaceSet) loops of type {0} are not implemented, Loop id = #{1}", 
							outerBound->Bound->GetType()->ToString(), outerBound->Bound->EntityLabel);
					}

				}
				if(facesAdded>0)
				{
					seamstress.Perform();

					ShapeFix_Shape sfs(seamstress.SewedShape());
					sfs.SetPrecision(mm/10);
					sfs.SetMaxTolerance(mm);
					sfs.SetMinTolerance(mm/10);
					sfs.Perform();	
					//BRepTools::Write(sfs.Shape(),"s3");
					return sfs.Shape();
					//return seamstress.SewedShape();
				}
				else
				{
					Logger->WarnFormat("XbimShell::Build(ConnectedFaceSet) Could not build a valid shell from IfcConnectedFaceSet  #{1}", faceSet->EntityLabel);
					return TopoDS_Shell();
				}

			}
		}
	}
}