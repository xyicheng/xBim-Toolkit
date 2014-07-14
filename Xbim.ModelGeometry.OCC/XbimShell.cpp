#include "StdAfx.h"
#include "XbimShell.h"
#include "XbimSolid.h"
#include "XbimGeometryModelCollection.h"
#include "XbimPolyhedron.h"
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
#include <BRepBuilderAPI_MakePolygon.hxx>
#include <BRepLib.hxx>
#include <BRepOffsetAPI_Sewing.hxx>
#include <BRepOffsetAPI_Sewing.hxx>
#include <TopExp.hxx>
#include <ShapeFix_FreeBounds.hxx>
#include <ShapeAnalysis_Shell.hxx>
#include <ShapeFix_EdgeConnect.hxx>

using namespace System::Linq;
using namespace System::Diagnostics;
using namespace Xbim::Common::Exceptions;
using namespace Xbim::Common;
using namespace Xbim::XbimExtensions;
using namespace Xbim::Ifc2x3::Extensions;

namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
			
			
			XbimShell::XbimShell(const TopoDS_Shape & shape, bool hasCurves,int representationLabel, int surfaceStyleLabel )
			{
				nativeHandle = new TopoDS_Shape();
				*nativeHandle = shape;
				_hasCurvedEdges = hasCurves;
				_representationLabel = representationLabel;
				_surfaceStyleLabel=surfaceStyleLabel;
			}



			XbimShell::XbimShell(XbimShell^ shell, IfcAxis2Placement^ origin, IfcCartesianTransformationOperator^ transform, bool hasCurves )
			{
				_hasCurvedEdges = hasCurves;
				_representationLabel = shell->RepresentationLabel;
				_surfaceStyleLabel = shell->SurfaceStyleLabel;
				TopoDS_Shape temp = *(shell->Handle);
				nativeHandle = new TopoDS_Shape();
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
						*nativeHandle =gTran.Shape();
					}
				}
				else
					*nativeHandle = temp;
			};



			/*Interfaces*/
			void XbimShell::ToSolid(double precision, double maxPrecision) 
			{
				if(isSolid) return;
				ShapeFix_Solid solidFixer;
				solidFixer.SetMinTolerance(precision);
				solidFixer.SetMaxTolerance(maxPrecision);
				solidFixer.SetPrecision(precision);
				solidFixer.CreateOpenSolidMode()= Standard_True;
				TopoDS_Compound solids;
				BRep_Builder builder;
				builder.MakeCompound(solids);
				for (TopExp_Explorer shellEx(*Handle,TopAbs_SHELL);shellEx.More();shellEx.Next()) //get each shell and make it a solid
				{
					TopoDS_Solid solid = solidFixer.SolidFromShell(TopoDS::Shell(shellEx.Current()));
					if(!solid.IsNull())
					{
						GProp_GProps System;
						BRepGProp::VolumeProperties(solid, System, Standard_False);
						double vol =  System.Mass();
						if(vol<0) 
							solid.Reverse();
						else if(vol==0)
						{
							Logger->WarnFormat("Attempt to close a Shell #{0}, it contains a solid with zero volume. Ignored",RepresentationLabel);
							continue;
						}
						builder.Add(solids,solid);
					}
				}
				if(BRepCheck_Analyzer(solids,Standard_True).IsValid() == Standard_False)
				{
					ShapeFix_Shape sfs(solids);
					sfs.SetPrecision(precision);
					sfs.SetMinTolerance(precision);
					sfs.SetMaxTolerance(maxPrecision);
					sfs.Perform();
					*nativeHandle = sfs.Shape();
				}
				else 
					*nativeHandle=solids;
				isSolid=true;
				
			}

			XbimGeometryModel^ XbimShell::CopyTo(IfcAxis2Placement^ placement)
			{
				TopoDS_Shape movedShape = *nativeHandle;
				movedShape.Move(XbimGeomPrim::ToLocation(placement));
				return gcnew XbimShell(movedShape, _hasCurvedEdges,RepresentationLabel, SurfaceStyleLabel);
			}



			//builds and sews a connected face set, does not orientate shells
			TopoDS_Shape XbimShell::Build(IfcConnectedFaceSet^ faceSet, bool% hasCurves)
			{
				IfcConnectedFaceSet^ facesetLocal = (IfcConnectedFaceSet^)faceSet->ModelOf->Instances[faceSet->EntityLabel]; //look up a local copy to stop the whole faceset being cached after the geometry has been loaded from the database
				if(facesetLocal->CfsFaces->Count == 0)
				{
					Logger->WarnFormat("IfcConnectedFaceSet #{0}, is empty",faceSet->EntityLabel);
					return TopoDS_Shape();
				}
				XbimModelFactors^ mf = ((IPersistIfcEntity^)faceSet)->ModelOf->ModelFactors;
				double tolerance = mf->Precision;
				double toleranceMax = mf->PrecisionMax;
				int maxSewFaces = mf->MaxBRepSewFaceCount;
				ShapeFix_ShapeTolerance fTol;		
				
				BRep_Builder builder;
				TopoDS_Shell shell;
				builder.MakeShell(shell); //create a shell to hold everything
				TopTools_DataMapOfIntegerShape vertexStore;
				TopTools_DataMapOfShapeListOfShape edgeMap;	
				for each (IfcFace^ fc in  facesetLocal->CfsFaces) //get all the vertices
				{
					for each (IfcFaceBound^ bound in fc->Bounds)
					{
						if(!dynamic_cast<IfcPolyLoop^>(bound->Bound) || ((IfcPolyLoop^)bound->Bound)->Polygon->Count < 3) continue;//skip non-polygonal faces
						IfcPolyLoop^polyLoop = (IfcPolyLoop^)bound->Bound;
						bool is3D = (polyLoop->Polygon[0]->Dim == 3);
						for each (IfcCartesianPoint^ p in polyLoop->Polygon) //add all the points into unique collection
						{
							if(!vertexStore.IsBound(p->EntityLabel))
							{
								TopoDS_Vertex v;
								builder.MakeVertex(v,gp_Pnt(p->X, p->Y, is3D ? p->Z : 0),tolerance);
								vertexStore.Bind(p->EntityLabel,v);
								
							}
						}
					}
				}
				for each ( IfcFace^ fc in  facesetLocal->CfsFaces)
				{
					IfcFaceBound^ outerBound = Enumerable::FirstOrDefault(Enumerable::OfType<IfcFaceOuterBound^>(fc->Bounds)); //get the outer bound
					if(outerBound == nullptr) outerBound = Enumerable::FirstOrDefault(fc->Bounds); //if one not defined explicitly use first found
					if(outerBound == nullptr || !dynamic_cast<IfcPolyLoop^>(outerBound->Bound)|| ((IfcPolyLoop^)(outerBound->Bound))->Polygon->Count<3) 
						continue; //invalid polygonal face

					TopoDS_Wire initialFaceLoop = BuildBound(outerBound,  vertexStore, edgeMap);

					if(!initialFaceLoop.IsNull())
					{
						//we have made the wire and the edges
						double currentFaceTolerance = tolerance;
TryBuildFace:
						BRepBuilderAPI_MakeFace faceBuilder(initialFaceLoop,Standard_True);
						BRepBuilderAPI_FaceError err = faceBuilder.Error();
						if ( err == BRepBuilderAPI_FaceDone )
						{		
							//now do holes if we have any
							if(fc->Bounds->Count>1)
							{
								//get the topological normal
								gp_Vec outerNormal = XbimFace::TopoNormal(faceBuilder.Face());

								for each (IfcFaceBound^ bound in fc->Bounds)
								{
									if(bound!=outerBound) //ignore the first one
									{
										TopoDS_Wire holeLoop = BuildBound(bound,  vertexStore, edgeMap);
										if(!holeLoop.IsNull())
										{
											gp_Vec holeNormal = XbimFaceBound::NewellsNormal(holeLoop);
											if ( outerNormal.Dot(holeNormal) > 0 ) //they should be in opposite directions, so reverse
												holeLoop.Reverse();
											double currentloopTolerance=tolerance;
TryBuildLoop:
											faceBuilder.Add(holeLoop);
											BRepBuilderAPI_FaceError loopErr=faceBuilder.Error();
											if(loopErr!=BRepBuilderAPI_FaceDone)
											{
												currentloopTolerance*=10; //try courser tolerance
												if(currentloopTolerance<=toleranceMax)
												{
													fTol.SetTolerance(holeLoop, currentloopTolerance , TopAbs_WIRE);
													goto TryBuildLoop;
												}
												String^ errMsg = XbimFace::GetBuildFaceErrorMessage(loopErr);
												Logger->WarnFormat("Invalid inner face bound, {0}. IfcFaceBound #(1) could not be added to face = #{2}. Inner Bound ignored",errMsg, bound->EntityLabel, fc->EntityLabel);
											}
										}
										else
											Logger->WarnFormat("Invalid inner face bound. IfcFaceBound #(1) could not be added to face = #{2}. Inner Bound ignored", bound->EntityLabel, fc->EntityLabel);
									}
								}

							}
							TopoDS_Face face = faceBuilder.Face();
							builder.Add(shell,face);
						}
						else
						{
							currentFaceTolerance*=10; //try courser tolerance
							if(currentFaceTolerance<=toleranceMax)
							{
								fTol.SetTolerance(initialFaceLoop, currentFaceTolerance , TopAbs_WIRE);
								goto TryBuildFace;
							}
							
							//XbimVector3D holeNormal = PolyLoopExtensions::NewellsNormal((IfcPolyLoop^)(outerBound->Bound));
							String^ errMsg = XbimFace::GetBuildFaceErrorMessage(err);
							Logger->WarnFormat("Invalid face, {0}. Found in IfcFace = #{1}, face discarded",errMsg, fc->EntityLabel);
						}
					}
					else
						Logger->WarnFormat("Invalid outer loop. IfcOuterBound #{0} could not build IfcFace = #{1}, face discarded",outerBound->EntityLabel, fc->EntityLabel);

				}
				//some objects have thousands of faces, accept as read if they have more than maxSewFaces, this is a perfromance issue as sewing is slow
				if(facesetLocal->CfsFaces->Count > maxSewFaces) return shell;

				//ensure all faces are correctly sewn and we have a valid BREP
				BRepBuilderAPI_Sewing  sfs(tolerance);
				sfs.SetMinTolerance(tolerance);
				sfs.SetMaxTolerance(toleranceMax);
				sfs.SetFloatingEdgesMode(Standard_True);
				sfs.Add(shell);
				sfs.Perform();
				TopoDS_Shape shape=sfs.SewedShape();
				
				if(BRepCheck_Analyzer(shape,Standard_False).IsValid() == Standard_False)
				{
					ShapeFix_Shape sfs(shape);
					sfs.SetPrecision(tolerance);
					sfs.SetMinTolerance(tolerance);
					sfs.SetMaxTolerance(toleranceMax);
					sfs.Perform();
					shape = sfs.Shape();
				}
				return shape;
			}

			

			TopoDS_Wire XbimShell::BuildBound(IfcFaceBound^ bound, TopTools_DataMapOfIntegerShape& vertexStore,TopTools_DataMapOfShapeListOfShape& edgeMap)
			{
				
				IfcPolyLoop^ polyLoop = (IfcPolyLoop^)bound->Bound;
				IList<IfcCartesianPoint^>^ boundToConvert;
				if(!bound->Orientation) //reverse the points if the sense is reversed
					boundToConvert = Enumerable::ToList(Enumerable::Reverse(polyLoop->Polygon)); 
				else 
					boundToConvert = polyLoop->Polygon;	
				double tolerance = bound->ModelOf->ModelFactors->Precision;
				//make the face loop edges
				int totalEdges=0;
				int lastPt=boundToConvert->Count;

				BRep_Builder wireMaker;
				TopoDS_Wire wire;
				wireMaker.MakeWire(wire);
				//BRepBuilderAPI_MakeWire wireMaker;
				for (int p=1; p<=lastPt; p++)
				{
					int p1;
					int p2;
				    if(p==lastPt)
					{
						p2 = boundToConvert[0]->EntityLabel;
						p1 = boundToConvert[p-1]->EntityLabel;	
					}
					else
					{
						p1= boundToConvert[p-1]->EntityLabel;
						p2 = boundToConvert[p]->EntityLabel;
					}
					
					bool builtEdge = false;
					Standard_Boolean v1Found=false;Standard_Boolean v2Found=false;
					const TopoDS_Vertex&v1 = TopoDS::Vertex(vertexStore.Find(p1));
					const TopoDS_Vertex&v2 = TopoDS::Vertex(vertexStore.Find(p2));
					
					if(edgeMap.IsBound(v2)) //we may have found an edge that starts at the end of this edge
					{
						for (TopTools_ListIteratorOfListOfShape it(edgeMap.Find(v2)); it.More(); it.Next())
						{
							const TopoDS_Edge & edge = TopoDS::Edge(it.Value());
							if(TopExp::LastVertex(edge, Standard_False).IsSame(v1)) //reuse this edge
							{
								//it is the reverse orientation so just use the found edge and reverse
								TopoDS_Edge edgeRev = edge;
								edgeRev.Reverse();
								wireMaker.Add(wire,edgeRev);
								builtEdge=true;
								totalEdges++;
								break;
							}
						}
					}
					if(!builtEdge) //see if the other way round has been stored
					{
						if(edgeMap.IsBound(v1)) //see if there is an edge that starts at the same place as this one
						{
							v1Found=true; //we have found one so need to change the collection
							for (TopTools_ListIteratorOfListOfShape it(edgeMap.Find(v1)); it.More(); it.Next())
							{
								const TopoDS_Edge & edge = TopoDS::Edge(it.Value());
								if(TopExp::LastVertex(edge, Standard_False).IsSame(v2)) //reuse this edge
								{
									//wireMaker.Add(edge);
									wireMaker.Add(wire,edge);
									builtEdge=true;
									totalEdges++;
									v1Found=false; //don't need to add edge twice
									break;
								}

							}
						}
						
					}
					if(!builtEdge) //OK we need to make one
					{

						BRepBuilderAPI_MakeEdge edgeMaker(v1,v2);	
						BRepBuilderAPI_EdgeError edgeErr = edgeMaker.Error();
						if(edgeErr!=BRepBuilderAPI_EdgeDone)
						{
							gp_Pnt pt1 =BRep_Tool::Pnt(v1);
							gp_Pnt pt2 =BRep_Tool::Pnt(v2);
							String^ errMsg = XbimEdge::GetBuildEdgeErrorMessage(edgeErr);
							Logger->WarnFormat("Invalid IfcEdge, {9}.\nFound in IfcPolyloop = #{0}. Start = #{7}({1}, {2}, {3}) End = #{8}({4}, {5}, {6}).\nEdge discarded",
								polyLoop->EntityLabel, pt1.X(),pt1.Y(),pt1.Z(),pt2.X(),pt2.Y(),pt2.Z(), p1, p2, errMsg);
						}
						else
						{
							TopoDS_Edge edge = edgeMaker.Edge();
							wireMaker.Add(wire,edge);
							totalEdges++;
							//just add it once to the map as we check twice
							if(v1Found)
								edgeMap.ChangeFind(v1).Append(edge);
							else //create a new list
							{
								TopTools_ListOfShape listEdge;
								listEdge.Append(edge);
								edgeMap.Bind(v1,listEdge); //always key on v1
							}
						}
					}
				}
				if(totalEdges<3)
				{
					Logger->WarnFormat("Invalid bound. IfcPolyloop = #{0} only has {1} edge(s), a minimum of 3 is required. Bound discarded",polyLoop->EntityLabel, totalEdges);
					return TopoDS_Wire();
				}
				wire.Closed(true);
				return wire;
			}

			IXbimGeometryModelGroup^ XbimShell::ToPolyHedronCollection(double deflection, double precision,double precisionMax, unsigned int rounding)
			{
				return ToPolyHedron(deflection,  precision, precisionMax, rounding);
			}
			
		}
	}
}
