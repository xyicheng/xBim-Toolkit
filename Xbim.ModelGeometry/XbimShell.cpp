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

#include <BRepCheck_Analyzer.hxx> 
#include <Handle_BRepCheck_Result.hxx> 
#include <ShapeFix_Face.hxx> 
#include <ShapeExtend_Status.hxx> 
#include <BRepLib_FuseEdges.hxx> 
#include <BRepAlgoAPI_Cut.hxx> 
#include <BRepAlgoAPI_Fuse.hxx> 
#include <BRepAlgoAPI_Common.hxx> 
#include <BRep_Tool.hxx>

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
using namespace System::Linq;
using namespace System::Diagnostics;
using namespace System::Windows::Media::Media3D;
//#define XBIMTRACE 1
namespace Xbim
{
	namespace ModelGeometry
	{

		IEnumerable<XbimFace^>^ XbimShell::CfsFaces::get()
		{

			return this;
		}

		XbimShell::XbimShell(const TopoDS_Shell & shell)
		{
			pShell = new TopoDS_Shell();
			*pShell = shell;
		}
		
		XbimShell::XbimShell(const TopoDS_Shell & shell, bool hasCurves )
		{
			pShell = new TopoDS_Shell();
			*pShell = shell;
			_hasCurvedEdges = hasCurves;
		}

		XbimTriangulatedModelStream^ XbimShell::Mesh()
		{
			return Mesh(true, XbimGeometryModel::DefaultDeflection, Matrix3D::Identity);
		}

		XbimTriangulatedModelStream^ XbimShell::Mesh( bool withNormals )
		{
			return Mesh(withNormals, XbimGeometryModel::DefaultDeflection, Matrix3D::Identity);
		}

		XbimTriangulatedModelStream^ XbimShell::Mesh(bool withNormals, double deflection )
		{

			return XbimGeometryModel::Mesh(this,withNormals,deflection, Matrix3D::Identity);
			
		}

		XbimTriangulatedModelStream^ XbimShell::Mesh(bool withNormals, double deflection, Matrix3D transform )
		{
			return XbimGeometryModel::Mesh(this,withNormals,deflection, transform);
			
		}

		XbimShell::XbimShell(XbimShell^ shell, IfcAxis2Placement^ origin, IfcCartesianTransformationOperator^ transform, bool hasCurves )
		{
			_hasCurvedEdges = hasCurves;
			TopoDS_Shell temp = *(((TopoDS_Shell*)shell->Handle));
			pShell = new TopoDS_Shell();
			if(origin!=nullptr)
				temp.Move(XbimGeomPrim::ToLocation(origin));
			if(transform!=nullptr)
			{	
				if(dynamic_cast<IfcCartesianTransformationOperator3DnonUniform^>( transform))
				{
					BRepBuilderAPI_GTransform gTran(temp,XbimGeomPrim::ToTransform((IfcCartesianTransformationOperator3DnonUniform^)transform));
					*pShell = TopoDS::Shell(gTran.Shape());
				}
				else
				{
					BRepBuilderAPI_Transform gTran(temp,XbimGeomPrim::ToTransform(transform));
					*pShell = TopoDS::Shell(gTran.Shape());
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

		/*XbimShell::XbimShell(IfcShell^ shell)
		{
			pShell = new TopoDS_Shell();
			if(dynamic_cast<IfcOpenShell^>(shell))
				*pShell = Build((IfcOpenShell^)shell, _hasCurvedEdges);
			else if(dynamic_cast<IfcClosedShell^>(shell))
				*pShell = Build((IfcClosedShell^)shell, _hasCurvedEdges);
			else
			{
				Type^ type = shell->GetType();
				throw gcnew Exception("Error buiding shell from type " + type->Name);
			}

		}*/

		XbimShell::XbimShell(IfcOpenShell^ shell)
		{
			pShell = new TopoDS_Shell();
			*pShell = Build(shell, _hasCurvedEdges);
		}

		/*Interfaces*/


		IXbimGeometryModel^ XbimShell::Cut(IXbimGeometryModel^ shape)
		{
			throw gcnew Exception("A cut operation has been applied to a shell (non-solid) object this is illegal according to schema");
			//bool hasCurves =  _hasCurvedEdges || shape->HasCurvedEdges; //one has a curve the result will have one
			//BRepAlgoAPI_Cut boolOp(*pShell,*(shape->Handle));

			//if(boolOp.ErrorStatus() == 0) //find the solid
			//{ 
			//	const TopoDS_Shape & res = boolOp.Shape();
			//	if(res.ShapeType() == TopAbs_SOLID)
			//		return gcnew XbimSolid(TopoDS::Solid(res), hasCurves);
			//	else if(res.ShapeType() == TopAbs_SHELL)	
			//		return gcnew XbimShell(TopoDS::Shell(res), hasCurves);
			//	else if(res.ShapeType() == TopAbs_COMPOUND)
			//		return gcnew XbimGeometryModelCollection(TopoDS::Compound(res), hasCurves);
			//	else if(res.ShapeType() == TopAbs_COMPSOLID)
			//	{
			//		TopoDS_Compound cpd;
			//		BRep_Builder b;
			//		b.MakeCompound(cpd);
			//		b.Add(cpd, res);
			//		return gcnew XbimGeometryModelCollection(cpd, hasCurves);
			//	}
			//	else
			//	{
			//		System::Diagnostics::Debug::WriteLine("Failed to form difference between two shapes");
			//		return nullptr;
			//	}
			//}
			//else
			//{
			//	System::Diagnostics::Debug::WriteLine("Failed to form difference between two shapes");
			//	return nullptr;
			//}
		}
		IXbimGeometryModel^ XbimShell::Union(IXbimGeometryModel^ shape)
		{
			throw gcnew Exception("A Union operation has been applied to a shell (non-solid) object this is illegal according to schema");
	  //  	bool hasCurves =  _hasCurvedEdges || shape->HasCurvedEdges; //one has a curve the result will have one
			//BRepAlgoAPI_Fuse boolOp(*pShell,*(shape->Handle));

			//if(boolOp.ErrorStatus() == 0) //find the solid
			//{ 
			//	const TopoDS_Shape & res = boolOp.Shape();
			//	if(res.ShapeType() == TopAbs_SOLID)
			//		return gcnew XbimSolid(TopoDS::Solid(res), hasCurves);
			//	else if(res.ShapeType() == TopAbs_SHELL)	
			//		return gcnew XbimShell(TopoDS::Shell(res), hasCurves);
			//	else if(res.ShapeType() == TopAbs_COMPOUND)
			//		return gcnew XbimGeometryModelCollection(TopoDS::Compound(res), hasCurves);
			//	else if(res.ShapeType() == TopAbs_COMPSOLID)
			//	{
			//		TopoDS_Compound cpd;
			//		BRep_Builder b;
			//		b.MakeCompound(cpd);
			//		b.Add(cpd, res);
			//		return gcnew XbimGeometryModelCollection(cpd, hasCurves);
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
		IXbimGeometryModel^ XbimShell::Intersection(IXbimGeometryModel^ shape)
		{
			throw gcnew Exception("A Intersection operation has been applied to a shell (non-solid) object this is illegal according to schema");
			//bool hasCurves =  _hasCurvedEdges || shape->HasCurvedEdges; //one has a curve the result will have one
			//BRepAlgoAPI_Common boolOp(*pShell,*(shape->Handle));

			//if(boolOp.ErrorStatus() == 0) //find the solid
			//{ 
			//	const TopoDS_Shape & res = boolOp.Shape();
			//	if(res.ShapeType() == TopAbs_SOLID)
			//		return gcnew XbimSolid(TopoDS::Solid(res), hasCurves);
			//	else if(res.ShapeType() == TopAbs_SHELL)	
			//		return gcnew XbimShell(TopoDS::Shell(res), hasCurves);
			//	else if(res.ShapeType() == TopAbs_COMPOUND)
			//		return gcnew XbimGeometryModelCollection(TopoDS::Compound(res), hasCurves);
			//	else if(res.ShapeType() == TopAbs_COMPSOLID)
			//	{
			//		TopoDS_Compound cpd;
			//		BRep_Builder b;
			//		b.MakeCompound(cpd);
			//		b.Add(cpd, res);
			//		return gcnew XbimGeometryModelCollection(cpd, hasCurves);
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
		IXbimGeometryModel^ XbimShell::CopyTo(IfcObjectPlacement^ placement)
		{
			if(dynamic_cast<IfcLocalPlacement^>(placement))
			{
				TopoDS_Shell movedShape = *pShell;
				IfcLocalPlacement^ lp = (IfcLocalPlacement^)placement;
				movedShape.Move(XbimGeomPrim::ToLocation(lp->RelativePlacement));
				return gcnew XbimShell(movedShape, _hasCurvedEdges);
			}
			else
				throw(gcnew Exception("XbimShell::CopyTo only supports IfcLocalPlacement type"));

		}
		TopoDS_Shell XbimShell::Build(IfcOpenShell^ shell, bool% hasCurves)
		{

			return Build((IfcConnectedFaceSet^)shell, hasCurves);
		}


		TopoDS_Shell XbimShell::Build(IfcClosedShell^ shell, bool% hasCurves)
		{
			TopoDS_Shell topoShell= Build((IfcConnectedFaceSet^)shell, hasCurves);
			topoShell.Closed(Standard_True);
			return topoShell;
		}

#pragma unmanaged

		TopoDS_Shell CreateShell(unsigned char *vertexBuffer, unsigned char *pointBuffer)
		{
			unsigned char * vBuff = vertexBuffer;
			unsigned char * pBuff = pointBuffer;

			int* pPointCount = (int *)pBuff;pBuff+=sizeof(int);
			int* pFaceCount = (int*)vBuff; vBuff+=sizeof(int);
			 
			BRep_Builder b;
			//create all the vertices
			TopTools_Array1OfShape points(0,*pPointCount);
			for(int i = 0; i < *pPointCount; i++)
			{
				unsigned char * pPoint =  pBuff + (sizeof(double) * 3 * i);

				double* pX = (double*)pPoint;pPoint += sizeof(double);
				double* pY = (double*)pPoint;pPoint += sizeof(double);
				double* pZ = (double*)pPoint;
				gp_XYZ pt(*pX,*pY,*pZ);
				TopoDS_Vertex vertex;
				b.MakeVertex(vertex , pt, Precision::Confusion());
				points.SetValue(i, vertex);
			}
			
			TopoDS_Shell shell;
			b.MakeShell(shell);
			
			for(int f = 0 ; f < *pFaceCount; f++)
			{
				int* pVertexCount = (int*)vBuff; vBuff+=sizeof(int);
				BRepBuilderAPI_MakePolygon makeWire;
				int* pFirstPointIdx = (int*)vBuff;

				for( int v=0; v < (*pVertexCount); v++)
				{
					int* pBeginPointIdx = (int*)vBuff; vBuff+=sizeof(int);
					makeWire.Add((const TopoDS_Vertex&) points.Value(*pBeginPointIdx));
				}
			
				makeWire.Close();
				const TopoDS_Wire& wire = makeWire.Wire();
				
				BRepBuilderAPI_FindPlane  FP(wire, 1e-3);
				if(!FP.Found())
				{
					//Debug::WriteLine(String::Format("XbimShell::Build(ConnectedFaceSet) could not find a plane for face id = #{0}",fc->EntityLabel));

					break;
				}
				gp_Pln pln = FP.Plane()->Pln();

				pln.SetAxis(pln.Axis().Reversed());
				BRepLib_MakeFace faceBlder(pln, wire, Standard_True);	
				int* pInnerBoundCount = (int*)vBuff; vBuff+=sizeof(int);
				for(int i = 0; i< *pInnerBoundCount;i++)
				{
					int* pInnerPointCount = (int*)vBuff; vBuff+=sizeof(int);
					BRepBuilderAPI_MakePolygon makeInnerWire;
					for(int p = 0; p< *pInnerPointCount;p++)
					{
						int* pBeginPointIdx = (int*)vBuff; vBuff+=sizeof(int);
						makeInnerWire.Add((const TopoDS_Vertex&) points.Value(*pBeginPointIdx));
					}
					makeInnerWire.Close();
					const TopoDS_Wire& innerWire = makeInnerWire.Wire();
					faceBlder.Add(innerWire);
				}
				b.Add(shell, faceBlder.Face());
			
		}
		return shell;
		}

#pragma managed


		TopoDS_Shell XbimShell::Build(IfcConnectedFaceSet^ faceSet, bool% hasCurves)
		{
			
			int pointCount = 0;
			int faceCount = faceSet->CfsFaces->Count;
			int boundCount = 0;
			for each ( IfcFace^ fc in  faceSet->CfsFaces)
			{
				for each (IfcFaceBound^ faceBound in fc->Bounds)
				{
					boundCount++;
					if(dynamic_cast<IfcPolyLoop^>(faceBound->Bound))
					{
						IfcPolyLoop^ polyLoop=(IfcPolyLoop^)faceBound->Bound;
						pointCount+=polyLoop->Polygon->Count;
					}
					else
						Debug::WriteLine(String::Format("XbimShell::Build(ConnectedFaceSet) loops of type {0} are not implemented, Loop id = #{1}", faceBound->Bound->GetType()->ToString(), faceBound->Bound->EntityLabel));
				}
			}
			int pSize = (pointCount*3*sizeof(double)) + sizeof(int);
			int vSize = (pointCount*sizeof(int)) + sizeof(int) + (faceCount*sizeof(int)) + (boundCount*sizeof(int))  ;
			IntPtr pointArray = Marshal::AllocHGlobal(pSize); 
			IntPtr vertexPtr = Marshal::AllocHGlobal(vSize);
			try
			{

				unsigned char* pointBuffer = (unsigned char*)pointArray.ToPointer();
				unsigned char* vertexBuffer = (unsigned char*)vertexPtr.ToPointer();
				{
					UnmanagedMemoryStream^ pointStream = gcnew UnmanagedMemoryStream(pointBuffer, pSize, pSize,  FileAccess::ReadWrite);
					UnmanagedMemoryStream^ vertexStream = gcnew UnmanagedMemoryStream(vertexBuffer, vSize, vSize, FileAccess::ReadWrite);
					faceCount = 0;
					{
						BinaryWriter^ pointWriter = gcnew BinaryWriter(pointStream);
						BinaryWriter^ vertexWriter = gcnew BinaryWriter(vertexStream);
						//create a map of all the points
						Dictionary<Point3D, int>^ points = gcnew Dictionary<Point3D, int>();
						pointWriter->Write((int) 0);
						vertexWriter->Write(faceCount);
						for each ( IfcFace^ fc in  faceSet->CfsFaces)
						{
							//get the outer bound, use the first one in the list if one is not defined
							IfcFaceBound^ outerBound = Enumerable::FirstOrDefault(fc->Bounds);
							List<IfcFaceBound^>^ innerBounds = gcnew List<IfcFaceBound^>();
							bool outerFound = false;				
							for each (IfcFaceBound^ bound in fc->Bounds)
							{		

								if(dynamic_cast<IfcFaceOuterBound^>(bound) ) //find the first and only outerbound
								{
									if(outerFound)
										Debug::WriteLine(String::Format("XbimShell::Build(ConnectedFaceSet) more than one outer bound has been found for a face = #{0}",fc->EntityLabel));
									else
									{
										outerBound = bound;
										outerFound=true;
									}
								}
								else
									if(outerBound != bound) innerBounds->Add(bound);
							}
							if(outerBound!=nullptr)
							{
								bool sense = outerBound->Orientation;
								//create the outer bound
								if(!MakeEdges(outerBound->Bound, points, pointWriter, vertexWriter, sense)) //if we cannot make the face and edges go to next one
									break;
								faceCount++;
								//if we have a  face add to the shell and process inner loops
								vertexWriter->Write(innerBounds->Count);
								if(innerBounds->Count > 0)
								{
									try
									{
										
										//create the inner bounds
										for each ( IfcFaceBound^ innerBound in  innerBounds)
										{
											bool innerSense = innerBound->Orientation;
											MakeEdges(innerBound->Bound, points, pointWriter, vertexWriter, innerSense);
										}
									}
									catch (...)
									{
										Debug::WriteLine(String::Format("XbimShell::Build(ConnectedFaceSet) an inner bound could not be added to a face id = #{0}",fc->EntityLabel));
									}
								}

							}
							else
								Debug::WriteLine(String::Format("XbimShell::Build(ConnectedFaceSet) a legal face could not be built from the geometry Bound = #{0}",outerBound->EntityLabel));

						}

						int pCount = points->Count;
						pointWriter->Seek(0,  SeekOrigin::Begin);
						pointWriter->Write(pCount);
						
						vertexWriter->Seek(0,  SeekOrigin::Begin);
						vertexWriter->Write(faceCount);
						pointStream->Flush();
						vertexStream->Flush();
						TopoDS_Shell shell = CreateShell(vertexBuffer, pointBuffer);

						return shell;
					}
					
				}
			}


			catch(Exception ^ e)
			{
				Debug::WriteLine(String::Format("XbimShell::Build(ConnectedFaceSet) general failure in ConnectedFace set = #{0}",faceSet->EntityLabel));
				System::Diagnostics::Debug::WriteLine(e->Message);
			}
			finally
			{
				Marshal::FreeHGlobal(pointArray);
				Marshal::FreeHGlobal(vertexPtr);
			}
			BRep_Builder b;
			TopoDS_Shell shell;
			b.MakeShell(shell);
			return shell;
		}



		bool XbimShell::MakeEdges(IfcLoop^ bound,Dictionary<Point3D, int>^ points, BinaryWriter^ pointWriter, BinaryWriter^ vertexWriter, bool sense ) 
		{

			if(dynamic_cast<IfcPolyLoop^>(bound))
			{
				IfcPolyLoop^ polyLoop=(IfcPolyLoop^)bound;
				if(polyLoop->Polygon->Count < 3) 
				{
					Debug::WriteLine(String::Format("XbimShell::Build(ConnectedFaceSet) loops with less than 3 points are illegal Loop id = #{0}",polyLoop->EntityLabel));
					
					return false;
				}
				//write a face

				
				IEnumerable<IfcCartesianPoint^>^ pts = polyLoop->Polygon;
				if(!sense)
					pts = Enumerable::Reverse(pts);
				//add all the points into shell point map
				vertexWriter->Write(polyLoop->Polygon->Count);
				for each(IfcCartesianPoint^ pt in pts)
				{
					Point3D% p3 = pt->WPoint3D();
					int pPos;
					if(!points->TryGetValue(p3, pPos))
					{
						pPos = (int)points->Count;
						
						points->Add(p3,pPos);
						pointWriter->Write(p3.X);
						pointWriter->Write(p3.Y);
						pointWriter->Write(p3.Z);
					}
					vertexWriter->Write(pPos);
				}
			}
			else //it is an invalid face so throw an error
			{
				Debug::WriteLine(String::Format("XbimShell::Build(ConnectedFaceSet) loops of type {0} are not implemented, Loop id = #{1}", bound->GetType()->ToString(), bound->EntityLabel));
				return false;
			}
			//if(edgeLoop->Count < 3) 
			//{
			//	Debug::WriteLine(String::Format("XbimShell::Build(ConnectedFaceSet) loops with less than 3 edges are illegal, Loop id = #{0}",  bound->EntityLabel));
			//	return false;
			//}
			//else
				return true;
		}

	}
	//
	//	TopoDS_Shell XbimShell::Build(IfcConnectedFaceSet^ faceSet, [OutAttribute] bool% isTesselated)
	//		{
	//			
	//			bool repairNeeded=false;
	//			isTesselated = true;
	//			BRep_Builder b;
	//			TopoDS_Shell shell;
	//			b.MakeShell(shell);
	//			if(faceSet->CfsFaces->Count < 800) return shell;
	//			//modified to resolve face direction problems and point uniqueness
	//
	//			//create a map of all the points
	//			Dictionary<Point3D, XbimVertexPoint^>^ points = gcnew Dictionary<Point3D, XbimVertexPoint^>();
	//			//create a map of all the edges
	//			Dictionary<XbimVertexPoint^, List<XbimEdge^>^>^ edges = gcnew Dictionary<XbimVertexPoint^, List<XbimEdge^>^>();
	//#if XBIMTRACE
	//			Debug::WriteLine(String::Format("Number of Faces = {0}",faceSet->CfsFaces->Count ));
	//			int pointTally = 0;
	//#endif			
	//			for each ( IfcFace^ fc in  faceSet->CfsFaces)
	//			{
	//				//get the outer bound, use the first one in the list if one is not defined
	//				IfcFaceBound^ outerBound = Enumerable::FirstOrDefault(fc->Bounds);
	//				List<IfcFaceBound^>^ innerBounds = gcnew List<IfcFaceBound^>();
	//				bool outerFound = false;
	//				
	//				for each (IfcFaceBound^ bound in fc->Bounds)
	//				{		
	//					
	//					if(dynamic_cast<IfcFaceOuterBound^>(bound) ) //find the first and only outerbound
	//					{
	//						if(outerFound)
	//							Debug::WriteLine(String::Format("XbimShell::Build(ConnectedFaceSet) more than one outer bound has been found for a face = #{0}",fc->EntityLabel));
	//						else
	//						{
	//							outerBound = bound;
	//							outerFound=true;
	//						}
	//					}
	//					else
	//						if(outerBound != bound) innerBounds->Add(bound);
	//				}
	//				if(outerBound!=nullptr)
	//				{
	//					bool sense = outerBound->Orientation;
	//					List<XbimEdge^>^ edgeLoop = gcnew List<XbimEdge^>();
	//					//create the outer bound
	//#if XBIMTRACE
	//								pointTally+=((IfcPolyLoop^)(outerBound->Bound))->Polygon->Count;
	//#endif
	//
	//					
	//					if(!MakeEdges(outerBound->Bound, points,edges, edgeLoop, sense,repairNeeded)) //if we cannot make the face and edges go to next one
	//						break;
	//					//create the face
	//					TopoDS_Wire wire;
	//					switch(edgeLoop->Count) // there will always be at least 3;
	//					{
	//					case 3:
	//						{
	//							BRepBuilderAPI_MakeWire makeWire(*(edgeLoop[0]->Handle),*(edgeLoop[1]->Handle),*(edgeLoop[2]->Handle));;
	//							wire = makeWire.Wire();
	//							break;
	//						}
	//					case 4:
	//						{
	//							BRepBuilderAPI_MakeWire makeWire(*(edgeLoop[0]->Handle),*(edgeLoop[1]->Handle),*(edgeLoop[2]->Handle),*(edgeLoop[3]->Handle));;
	//							wire = makeWire.Wire();
	//							break;
	//						}
	//					default:
	//						{
	//							isTesselated=false;
	//							BRepBuilderAPI_MakeWire makeWire;
	//							for each(XbimEdge^ edge in edgeLoop)
	//								makeWire.Add(*(edge->Handle));
	//							wire = makeWire.Wire();
	//							break;
	//						}
	//					}
	//					wire.Closed(true);
	//
	//					BRepBuilderAPI_FindPlane  FP(wire, 1e-3);
	//					
	//					if(!FP.Found())
	//					{
	//						Debug::WriteLine(String::Format("XbimShell::Build(ConnectedFaceSet) could not find a plane for face id = #{0}",fc->EntityLabel));
	//						break;
	//					}
	//					gp_Pln pln = FP.Plane()->Pln();
	//					pln.SetAxis(pln.Axis().Reversed());
	//					BRepLib_MakeFace faceBlder(pln, wire);	
	//					
	//					TopoDS_Face face = faceBlder.Face();
	//					
	//					
	//					//if we have a  face add to the shell and process inner loops
	//					if(!face.IsNull() )
	//					{
	//						if(innerBounds->Count > 0)
	//						{
	//							try
	//							{
	//
	//								//create the inner bounds
	//								for each ( IfcFaceBound^ bound in  innerBounds)
	//								{
	//#if XBIMTRACE
	//									pointTally+=((IfcPolyLoop^)(bound->Bound))->Polygon->Count;
	//#endif
	//									isTesselated=false; //has holes so not simple triangles or quads
	//									bool innerSense = bound->Orientation;
	//									List<XbimEdge^>^ innerLoop = gcnew List<XbimEdge^>();
	//									if(!MakeEdges(bound->Bound, points,edges, innerLoop,innerSense,repairNeeded)) //if we cannot make the edges go to next one
	//										break;
	//									TopoDS_Wire innerWire;
	//									switch(innerLoop->Count) // there will always be at least 3;
	//									{
	//									case 3:
	//										{
	//											BRepBuilderAPI_MakeWire makeInnerWire(*(innerLoop[0]->Handle),*(innerLoop[1]->Handle),*(innerLoop[2]->Handle));;
	//											innerWire = makeInnerWire.Wire();
	//											break;
	//										}
	//									case 4:
	//										{
	//											BRepBuilderAPI_MakeWire makeInnerWire(*(innerLoop[0]->Handle),*(innerLoop[1]->Handle),*(innerLoop[2]->Handle),*(innerLoop[3]->Handle));;
	//											innerWire = makeInnerWire.Wire();
	//											break;
	//										}
	//									default:
	//										{
	//											isTesselated=false;
	//											BRepBuilderAPI_MakeWire makeInnerWire;
	//											for each(XbimEdge^ edge in innerLoop)
	//												makeInnerWire.Add(*(edge->Handle));
	//											innerWire = makeInnerWire.Wire();
	//											break;
	//										}
	//									}
	//									innerWire.Closed(true);
	//									faceBlder.Add(innerWire);
	//
	//								}
	//							}
	//							catch (...)
	//							{
	//								Debug::WriteLine(String::Format("XbimShell::Build(ConnectedFaceSet) an inner bound could not be added to a face id = #{0}",fc->EntityLabel));
	//							}
	//						}
	//						if(faceBlder.IsDone())
	//						{
	//							b.Add(shell,faceBlder.Face());
	//						}
	//					}
	//					else
	//						Debug::WriteLine(String::Format("XbimShell::Build(ConnectedFaceSet) a legal face could not be built from the geometry Bound = #{0}",outerBound->EntityLabel));
	//				}
	//				else
	//					Debug::WriteLine(String::Format("XbimShell::Build(ConnectedFaceSet) an illegal face with 0 bounds has been found id = #{0}",fc->EntityLabel));
	//			}
	//#if XBIMTRACE
	//			int eCount = 0;
	//			for each(List<XbimEdge^>^ edgeList in edges->Values)
	//			{
	//				eCount+=edgeList->Count;
	//			}
	//			Debug::WriteLine(String::Format("Number of Points = {0}, Vertices = {1}, Edges = {2}",pointTally, points->Count, eCount / 2 ));
	//
	//#endif		
	//			
	//			ShapeFix_Shell fixer;
	//			fixer.FixFaceOrientation(shell);
	//			shell= fixer.Shell();
	//			GProp_GProps System;
	//			BRepGProp::VolumeProperties(shell, System, Standard_True);
	//			if(System.Mass() <0)
	//				shell.Reverse();
	//			return shell;
	//		}
	//
	//		bool XbimShell::MakeEdges(IfcLoop^ bound,Dictionary<Point3D, XbimVertexPoint^>^ points, Dictionary<XbimVertexPoint^, List<XbimEdge^>^>^ edges, List<XbimEdge^>^ edgeLoop, bool sense, bool& repairNeeded ) 
	//		{
	//
	//			if(dynamic_cast<IfcPolyLoop^>(bound))
	//			{
	//				IfcPolyLoop^ polyLoop=(IfcPolyLoop^)bound;
	//				
	//				if(polyLoop->Polygon->Count < 3) 
	//				{
	//					Debug::WriteLine(String::Format("XbimShell::Build(ConnectedFaceSet) loops with less than 3 points are illegal Loop id = #{0}",polyLoop->EntityLabel));
	//					return false;
	//				}
	//				List<IfcCartesianPoint^>^ pts = gcnew List<IfcCartesianPoint^>();
	//				pts->AddRange( polyLoop->Polygon);
	//				pts->Add(polyLoop->Polygon[0]);
	//				if(!sense)
	//					pts->Reverse();
	//				//add all the points into shell point map
	//				XbimVertexPoint^ lastPoint = nullptr;
	//				XbimVertexPoint^ firstPoint = lastPoint;
	//				
	//				for each(IfcCartesianPoint^ pt in pts)
	//				{
	//					Point3D% p3 = pt->WPoint3D();
	//					XbimVertexPoint ^ nextPoint;
	//					if(!points->TryGetValue(p3,nextPoint))
	//					{
	//						nextPoint = gcnew XbimVertexPoint(p3.X, p3.Y,p3.Z);
	//						points->Add(p3,nextPoint);
	//					}
	//					if(lastPoint!=nullptr) //if we are second point or more start making edges
	//					{
	//						List<XbimEdge^>^ edges1 = nullptr;
	//						List<XbimEdge^>^ edges2 = nullptr;
	//						XbimEdge^ theEdge = nullptr;
	//						bool e1Found = edges->TryGetValue(lastPoint,edges1);
	//						bool e2Found = edges->TryGetValue(nextPoint,edges2) ;
	//						if(e1Found && e2Found)
	//						{
	//							for each(XbimEdge^ edge in edges1)
	//							{
	//								if(edges2->Contains(edge)) //we have an edge that exists
	//								{
	//									theEdge = gcnew XbimEdge(edge);
	//								    if(edge->IsEndVertex(lastPoint)) //need to reverse the edge
	//										theEdge->Reverse();
	//									break;
	//									
	//								}
	//							}
	//						}
	//						if(theEdge == nullptr)
	//						{
	//							theEdge = gcnew XbimEdge(lastPoint,nextPoint );
	//							if(edges1==nullptr)
	//							{
	//								edges1 = gcnew List<XbimEdge^>();
	//								edges->Add(lastPoint,edges1);
	//							}
	//							if(edges2==nullptr)
	//							{
	//								edges2 = gcnew List<XbimEdge^>();
	//								edges->Add(nextPoint,edges2);
	//							}
	//							edges1->Add(theEdge);
	//							edges2->Add(theEdge);
	//						}
	//						edgeLoop->Add(theEdge);
	//					}
	//					lastPoint = nextPoint;
	//				}
	//			}
	//			else //it is an invalid face so throw an error
	//			{
	//				Debug::WriteLine(String::Format("XbimShell::Build(ConnectedFaceSet) loops of type {0} are not implemented, Loop id = #{1}", bound->GetType()->ToString(), bound->EntityLabel));
	//				return false;
	//			}
	//			if(edgeLoop->Count < 3) 
	//			{
	//				Debug::WriteLine(String::Format("XbimShell::Build(ConnectedFaceSet) loops with less than 3 edges are illegal, Loop id = #{0}",  bound->EntityLabel));
	//				return false;
	//			}
	//			else
	//				return true;
	//		}
	//
	//	}
}
