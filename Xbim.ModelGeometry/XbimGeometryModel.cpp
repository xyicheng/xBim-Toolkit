/* ==================================================================
TesselateStream:

TesselateStream helps creating the memory stream holding the triangulated mesh information;
it receives calls from teh OpenGL and Opencascade implementations of the meshing algorithms.

its functions can be called as follows: 
foreach face
	BeginFace(normal)
	foreach point
		WritePoint(x,y,z);
	foreach polygon using the points
		BeginPolygon(mode);
		foreach node (of the polygon)
			WritePointInt(int) or
			WritePointShort(int) or
			WritePointByte(int) (depending on the max size of int)

		EndPolygon (this fills some data left blank by BeginPolygon
	EndFace (this fills some data left blank by BeginFace)
*/

#include "StdAfx.h"
#include "XbimTriangularMeshStreamer.h"
#include "XbimGeometryModel.h"
#include "XbimLocation.h"
#include "XbimSolid.h"
#include "XbimGeomPrim.h"
#include "XbimFeaturedShape.h"
#include "XbimShell.h"
#include "XbimGeometryModelCollection.h"
#include "XbimFacetedShell.h"
#include "XbimMap.h"

#include <TopoDS_Shell.hxx>
#include <TopoDS_Solid.hxx>
#include <BRep_Builder.hxx>
#include <BRepAlgoAPI_Cut.hxx>
#include <BRepAlgoAPI_Fuse.hxx>
#include <BRepAlgoAPI_Common.hxx>
#include <TopoDS.hxx>
#include <ShapeFix_Shape.hxx> 
#include <ShapeFix_Wireframe.hxx> 
#include <BRepBuilderAPI_Sewing.hxx> 
#include <ShapeUpgrade_ShellSewing.hxx> 
#include <BRepMesh_IncrementalMesh.hxx>
#include <Poly_Array1OfTriangle.hxx>
#include <TColgp_Array1OfPnt.hxx>
#include <TShort_Array1OfShortReal.hxx>
#include <Poly_Triangulation.hxx>
#include <BRepBndLib.hxx>
#include <BRepBuilderAPI_Transform.hxx>
#include <GeomLProp_SLProps.hxx>
#include <BRepLib.hxx>
using namespace Xbim::Ifc::ProductExtension;
using namespace System::Linq;
using namespace Xbim::IO;

// This class helps creating the memory stream holding the mesh information
//
TesselateStream::TesselateStream( unsigned char* pDataStream, const TopTools_IndexedMapOfShape& points, unsigned short faceCount, int streamSize)
{
	_streamSize=streamSize;
	_pDataStream = pDataStream;
	_position=0;

	//write out number of points
	unsigned int * pPointCount = (unsigned int *)_pDataStream;
	*pPointCount = points.Extent();

	//move position on 
	_position+=sizeof(unsigned int);
	if(TesselateStream::UseDouble)
	{
		for(unsigned int i=1;i<=*pPointCount;i++)
		{
			const TopoDS_Vertex& vertex = TopoDS::Vertex(points.FindKey(i));
			gp_Pnt p = BRep_Tool::Pnt(vertex);
			double* pCord = (double *)(_pDataStream + _position);
			*pCord = p.X(); _position += sizeof(double);
			pCord = (double *)(_pDataStream + _position);
			*pCord = p.Y(); _position += sizeof(double);
			pCord = (double *)(_pDataStream + _position);
			*pCord = p.Z(); _position += sizeof(double);
		}
	}
	else
	{
		for(unsigned int i=1;i<=*pPointCount;i++)
		{
			const TopoDS_Vertex& vertex = TopoDS::Vertex(points.FindKey(i));
			gp_Pnt p = BRep_Tool::Pnt(vertex);
			float* pCord = (float *)(_pDataStream + _position);
			*pCord = (float)p.X(); _position += sizeof(float);
			pCord = (float*)(_pDataStream + _position);
			*pCord = (float)p.Y(); _position += sizeof(float);
			pCord = (float*)(_pDataStream + _position);
			*pCord = (float)p.Z(); _position += sizeof(float);
		}
	}
	unsigned short * pFaceCount = (unsigned short *)(_pDataStream + _position);
	*pFaceCount=faceCount;
	_position+=sizeof(faceCount);
}

// ==================================================================
// This class helps creating the memory stream holding the mesh information
//
TesselateStream::TesselateStream( unsigned char* pDataStream, unsigned short faceCount, unsigned int nodeCount, int streamSize)
{
	_streamSize=streamSize;
	_pDataStream = pDataStream;
	_position=0;
	//write out number of points
	unsigned int * pPointCount = (unsigned int *)_pDataStream;
	*pPointCount = nodeCount;
	_position+=sizeof(unsigned int);
	_pointPosition = _position;
	//move position on 
	if(UseDouble)
		_position+=nodeCount * 3 * sizeof(double);
	else
		_position+=nodeCount * 3 * sizeof(float);
	unsigned short * pFaceCount = (unsigned short *)(_pDataStream + _position);
	*pFaceCount=faceCount;
	_position+=sizeof(faceCount);
}

TesselateStream::TesselateStream( unsigned char* pDataStream,  int streamSize, int position)
{
	_streamSize=streamSize;
	_pDataStream = pDataStream;
	_position=position;
}

void TesselateStream::BeginFace(const gp_Dir& normal)
{
	//reset polygon count
	_polygonCount=0;
	//write out space for number of polygons
	_faceStart = _position;
	_position += sizeof(unsigned short);
	unsigned short * pCount = (unsigned short *)(_pDataStream + _position); _position += sizeof(unsigned short);
	*pCount=1;
	double* pCord = (double *)(_pDataStream + _position);
	*pCord = normal.X(); _position += sizeof(double);
	pCord = (double *)(_pDataStream + _position);
	*pCord = normal.Y(); _position += sizeof(double);
	pCord = (double *)(_pDataStream + _position);
	*pCord = normal.Z(); _position += sizeof(double);

}

void TesselateStream::EndFace()
{
	unsigned short * pPolyCount = (unsigned short *)(_pDataStream+_faceStart);
	*pPolyCount = _polygonCount;
}

void TesselateStream::BeginPolygon(GLenum type)
{
	_polygonCount++;
	_polygonStart =_position;
	unsigned char * pType = (_pDataStream+_polygonStart);
	*pType= (unsigned char)type;
	_position+= sizeof(unsigned char) + sizeof(unsigned short); //move on to leave space for number of points
	_indicesCount=0;
}

void TesselateStream::WritePoint(double x, double y, double z)
{
	if(UseDouble)
	{
		double* pCord = (double *)(_pDataStream + _pointPosition);
		*pCord = x; _pointPosition += sizeof(double);
		pCord = (double *)(_pDataStream + _pointPosition);
		*pCord = y; _pointPosition += sizeof(double);
		pCord = (double *)(_pDataStream + _pointPosition);
		*pCord = z; _pointPosition += sizeof(double);
	}
	else
	{
		float* pCord = (float *)(_pDataStream + _pointPosition);
		*pCord = (float)x; _pointPosition += sizeof(float);
		pCord = (float *)(_pDataStream + _pointPosition);
		*pCord = (float)y; _pointPosition += sizeof(float);
		pCord = (float *)(_pDataStream + _pointPosition);
		*pCord = (float)z; _pointPosition += sizeof(float);
	}
}

void TesselateStream::WritePointInt(unsigned int index)
{

	unsigned int * pIndex = (unsigned int*)(_pDataStream + _position);
	*pIndex = index;
	_position+=sizeof(unsigned int);
	_indicesCount++;
}

void TesselateStream::WritePointShort(unsigned int index)
{
	unsigned short * pIndex = (unsigned short*)(_pDataStream + _position);
	*pIndex = (unsigned short)index;
	_position+=sizeof(unsigned short);
	_indicesCount++;
}


void TesselateStream::WritePointByte(unsigned int index)
{
	unsigned char * pIndex = (unsigned char*)(_pDataStream + _position);
	*pIndex = (unsigned char)index;
	_position+=sizeof(unsigned char);
	_indicesCount++;
}
void TesselateStream::EndPolygon()
{
	unsigned short * pCount = (unsigned short *)(_pDataStream + _polygonStart + sizeof(unsigned char));
	*pCount = _indicesCount;
}




void CALLBACK BeginTessellate(GLenum type, void *pPolygonData)
{
	((TesselateStream*)pPolygonData)->BeginPolygon(type);
};
void CALLBACK EndTessellate(void *pVertexData)
{
	((TesselateStream*)pVertexData)->EndPolygon();
};

void CALLBACK TessellateError(GLenum err)
{
};

void CALLBACK AddVertexByte(void *pVertexData, void *pPolygonData)
{
	((TesselateStream*)pPolygonData)->WritePointByte((unsigned char)pVertexData);
};
void CALLBACK AddVertexShort(void *pVertexData, void *pPolygonData)
{
	((TesselateStream*)pPolygonData)->WritePointShort((unsigned short)pVertexData);
};
void CALLBACK AddVertexInt(void *pVertexData, void *pPolygonData)
{
	((TesselateStream*)pPolygonData)->WritePointInt((unsigned int)pVertexData);
};

gp_Dir GetNormal(const TopoDS_Face& face)
{
	// get bounds of face
	Standard_Real umin, umax, vmin, vmax;
	BRepTools::UVBounds(face, umin, umax, vmin, vmax);          // create surface
	Handle(Geom_Surface) surf=BRep_Tool::Surface(face);          // get surface properties
	GeomLProp_SLProps props(surf, umin, vmin, 1, 0.01);          // get surface normal
	gp_Dir norm = props.Normal();                         // check orientation
	if(face.Orientation()==TopAbs_REVERSED) 
		norm.Reverse();
	return norm;
}


namespace Xbim
{
	namespace ModelGeometry
	{
		XbimGeometryModel::XbimGeometryModel(void)
		{
		}

		/* Creates a 3D Model geometry for the product based upon the first "Body" ShapeRepresentation that can be found in  Products.ProductRepresentation that is within the specified 
		GeometricRepresentationContext, if the Representation context is null the first  "Body" ShapeRepresentation
		is used. Returns null if their is no valid geometric definition
		*/
		IXbimGeometryModel^ XbimGeometryModel::CreateFrom(IfcProduct^ product, IfcGeometricRepresentationContext^ repContext, Dictionary<IfcRepresentation^, IXbimGeometryModel^>^ maps, bool forceSolid)
		{
			if(product->Representation == nullptr ||  product->Representation->Representations == nullptr) 
				return nullptr; //if it doesn't have one do nothing


			//we should cast the shape below to a ShapeRepresentation but using IfcRepresentation means this works for older IFC2x formats  and there is no data loss

			for each(IfcRepresentation^ shape in product->Representation->Representations)
			{
				if(repContext == nullptr || shape->ContextOfItems ==  repContext) 
				{
					if( !shape->RepresentationIdentifier.HasValue ||
																(String::Compare(shape->RepresentationIdentifier.Value, "body" , true)==0)||
																String::Compare(shape->RepresentationIdentifier.Value, "facetation" , true)==0)
																//we have a 3D geometry
					{
						if(dynamic_cast<IfcGeometricRepresentationContext^>(shape->ContextOfItems))
							BRepLib::Precision((Standard_Real)((IfcGeometricRepresentationContext^)(shape->ContextOfItems))->DefaultPrecision);
						if(dynamic_cast<IfcElement^>(product))
						{
							IfcElement^ element = (IfcElement^) product;
							//check if we have openings or projections
							IEnumerable<IfcRelProjectsElement^>^ projections = element->HasProjections;
							IEnumerable<IfcRelVoidsElement^>^ openings = element->HasOpenings;
							IfcRelVoidsElement^ voids = Enumerable::FirstOrDefault(openings);
							if( voids !=nullptr || Enumerable::FirstOrDefault(projections) !=nullptr )
							{
								List<IXbimGeometryModel^>^ projectionSolids = gcnew List<IXbimGeometryModel^>();
								List<IXbimGeometryModel^>^ openingSolids = gcnew List<IXbimGeometryModel^>();
								for each(IfcRelProjectsElement^ rel in projections)
								{
									IfcFeatureElementAddition^ fe = rel->RelatedFeatureElement;
									if(fe->Representation!=nullptr)
									{
										IXbimGeometryModel^ im = CreateFrom(fe,repContext, maps, true);
										if(!dynamic_cast<XbimSolid^>(im))
											throw gcnew Exception("FeatureElementAdditions must be of type solid");

										im = im->CopyTo(fe->ObjectPlacement);
										projectionSolids->Add(im);
									}
								}

								for each(IfcRelVoidsElement^ rel in openings)
								{
									IfcFeatureElementSubtraction^ fe = rel->RelatedOpeningElement;
									if(fe->Representation!=nullptr)
									{
										IXbimGeometryModel^ im = CreateFrom(fe, repContext, maps, true);
										if(im!=nullptr)
										{	
											if(product->ObjectPlacement != ((IfcLocalPlacement^)(fe->ObjectPlacement))->PlacementRelTo)
											{
												if(dynamic_cast<IfcLocalPlacement^>(product->ObjectPlacement))
												{	
													IfcLocalPlacement^ lp = (IfcLocalPlacement^)product->ObjectPlacement;							
													TopLoc_Location prodLoc = XbimGeomPrim::ToLocation(lp->RelativePlacement);
													prodLoc= prodLoc.Inverted();

													(*(im->Handle)).Move(prodLoc);	
												}
											}

											im = im->CopyTo(fe->ObjectPlacement);
											openingSolids->Add(im);
										}
									}
								}
								IXbimGeometryModel^ baseShape = CreateFrom(shape, maps, true);	
								
								IXbimGeometryModel^ fShape =  gcnew XbimFeaturedShape(baseShape, openingSolids, projectionSolids);

								return fShape;
							}
							else //we have no openings or projections
								return CreateFrom(shape, maps, forceSolid);
						}
						else
							return CreateFrom(shape, maps, forceSolid);
					}
				}
			}
			return nullptr;
		}
		IXbimGeometryModel^ XbimGeometryModel::CreateFrom(IfcProduct^ product, Dictionary<IfcRepresentation^, IXbimGeometryModel^>^ maps, bool forceSolid)
		{
			return CreateFrom(product, nullptr, maps, forceSolid);
		}
		IXbimGeometryModel^ XbimGeometryModel::CreateFrom(IfcProduct^ product, bool forceSolid)
		{
			return CreateFrom(product, nullptr, gcnew Dictionary<IfcRepresentation^, IXbimGeometryModel^>(), forceSolid);
		}

		IXbimGeometryModel^ XbimGeometryModel::Build(IfcBooleanResult^ repItem)
		{
			IfcBooleanOperand^ fOp= repItem->FirstOperand;
			IfcBooleanOperand^ sOp= repItem->SecondOperand;
			IXbimGeometryModel^ shape1;
			IXbimGeometryModel^ shape2;
			System::Nullable<bool> _shape1IsSolid;
			if(dynamic_cast<IfcBooleanResult^>(fOp))
				shape1 = Build((IfcBooleanResult^)fOp);
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
				throw(gcnew Exception("XbimGeometryModel. Build(BooleanResult) FirstOperand must be a valid IfcBooleanOperand"));
			if(dynamic_cast<IfcBooleanResult^>(sOp))
				shape2 = Build((IfcBooleanResult^)sOp);
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
				throw(gcnew Exception("XbimGeometryModel. Build(BooleanResult) FirstOperand must be a valid IfcBooleanOperand"));

			//check if we have boxed half spaces then see if there is any intersect
			if(_shape1IsSolid.HasValue)
			{
				if(!shape1->GetBoundingBox(false)->Intersects(shape2->GetBoundingBox(false)))
				{
					if(_shape1IsSolid.Value == true) return shape1; else return shape2;
				}
			}

			switch(repItem->Operator)
			{
			case IfcBooleanOperator::Union:
				return shape1->Union(shape2);	
			case IfcBooleanOperator::Intersection:
				return shape1->Intersection(shape2);
			case IfcBooleanOperator::Difference:
				return shape1->Cut(shape2);

			default:
				throw(gcnew Exception("XbimGeometryModel. Build(BooleanClippingResult) Unsupported Operation"));
			}	
		}


		/*
		Create a model geometry for a given shape
		*/
		IXbimGeometryModel^ XbimGeometryModel::CreateFrom(IfcRepresentation^ rep, Dictionary<IfcRepresentation^, IXbimGeometryModel^>^ maps, bool forceSolid)
		{

			if(rep->Items->Count == 0) //we have nothing to do
				return nullptr;
			else if (rep->Items->Count == 1) //we have a single shape geometry
			{
				return CreateFrom(rep->Items->First,maps, forceSolid);
			}
			else // we have a compound shape
			{
				XbimGeometryModelCollection^ gms = gcnew XbimGeometryModelCollection();
				for each (IfcRepresentationItem^ repItem in rep->Items)
				{
					gms->Add(CreateFrom(repItem,maps,forceSolid));
				}
				if(forceSolid)
					return gms->Solidify();
				else
					return gms;
			}
		}

		IXbimGeometryModel^ XbimGeometryModel::CreateFrom(IfcRepresentation^ rep, bool forceSolid)
		{
			return CreateFrom(rep, gcnew Dictionary<IfcRepresentation^, IXbimGeometryModel^>(), forceSolid);
		}

		IXbimGeometryModel^ XbimGeometryModel::CreateFrom(IfcRepresentationItem^ repItem, bool forceSolid)
		{
			return CreateFrom(repItem, gcnew Dictionary<IfcRepresentation^, IXbimGeometryModel^>(), forceSolid);
		}

		IXbimGeometryModel^ XbimGeometryModel::CreateFrom(IfcRepresentationItem^ repItem, Dictionary<IfcRepresentation^, IXbimGeometryModel^>^ maps, bool forceSolid)
		{
			if(!forceSolid && dynamic_cast<IfcFacetedBrep^>(repItem))
				return gcnew XbimFacetedShell(((IfcFacetedBrep^)repItem)->Outer);
			else if(dynamic_cast<IfcSolidModel^>(repItem))
				return gcnew XbimSolid((IfcSolidModel^)repItem);
			else if(dynamic_cast<IfcHalfSpaceSolid^>(repItem))
				return gcnew XbimSolid((IfcHalfSpaceSolid^)repItem);
			else if(dynamic_cast<IfcCsgPrimitive3D^>(repItem))
				return gcnew XbimSolid((IfcCsgPrimitive3D^)repItem);
			else if(dynamic_cast<IfcFacetedBrep^>(repItem)) 
				return gcnew XbimSolid((IfcFacetedBrep^)repItem);
			else if(dynamic_cast<IfcShellBasedSurfaceModel^>(repItem)) 
				return Build((IfcShellBasedSurfaceModel^)repItem, forceSolid);
			else if(dynamic_cast<IfcFaceBasedSurfaceModel^>(repItem)) 
				return Build((IfcFaceBasedSurfaceModel^)repItem, forceSolid);
			else if(dynamic_cast<IfcBooleanClippingResult^>(repItem)) 
				return Build((IfcBooleanClippingResult^)repItem);
			else if(dynamic_cast<IfcMappedItem^>(repItem))
			{
				IfcMappedItem^ map = (IfcMappedItem^) repItem;
				IfcRepresentationMap^ repMap = map->MappingSource;
				IXbimGeometryModel^ mg;
				if(!maps->TryGetValue(repMap->MappedRepresentation, mg)) //look it up
				{
					mg =  CreateFrom(repMap->MappedRepresentation,maps, forceSolid); //make the first one
					maps->Add(repMap->MappedRepresentation, mg);
				}

				//need to transform all the geometries as below
				return CreateMap(mg, repMap->MappingOrigin, map->MappingTarget,maps, forceSolid);	

			}
			else
			{
				Type ^ type = repItem->GetType();
				throw(gcnew Exception(String::Format("XbimGeometryModel. Could not Build Geometry, type {0} is not implemented",type->Name)));
			}
		}

		IXbimGeometryModel^ XbimGeometryModel::CreateMap(IXbimGeometryModel^ item, IfcAxis2Placement^ origin, IfcCartesianTransformationOperator^ transform, Dictionary<IfcRepresentation^, IXbimGeometryModel^>^ maps, bool forceSolid)
		{
			if(dynamic_cast<XbimSolid^>(item))
			{
				return gcnew XbimSolid((XbimSolid^)item,origin,transform, item->HasCurvedEdges);
			}
			else if(dynamic_cast<XbimShell^>(item))
			{
				return gcnew XbimShell((XbimShell^)item,origin,transform, item->HasCurvedEdges);
			}
			else if(dynamic_cast<XbimFacetedShell^>(item))
			{
				return gcnew XbimMap(item,origin,transform);
			}
			else if(dynamic_cast<XbimGeometryModelCollection^>(item))
			{
				XbimGeometryModelCollection^ mapColl = gcnew XbimGeometryModelCollection();
				XbimGeometryModelCollection^ toMap = (XbimGeometryModelCollection^) item;
				for each(IXbimGeometryModel^ model in toMap)
					mapColl->Add(CreateMap(model,origin,transform, maps, forceSolid));
				if(forceSolid)
					return mapColl->Solidify();
				else
					return mapColl;
			}
			else
				throw(gcnew Exception("XbimGeometryModel.CreateMap Unsupported IXbimGeometryModel type"));
		}

		IXbimGeometryModel^ XbimGeometryModel::CreateMap(IXbimGeometryModel^ item, IfcAxis2Placement^ origin, Dictionary<IfcRepresentation^, IXbimGeometryModel^>^ maps, bool forceSolid)
		{
			return CreateMap(item, origin, nullptr, maps, forceSolid);
		}
		IXbimGeometryModel^ XbimGeometryModel::CreateMap(IXbimGeometryModel^ item, Dictionary<IfcRepresentation^, IXbimGeometryModel^>^ maps, bool forceSolid)
		{
			return CreateMap(item, nullptr, nullptr, maps, forceSolid);
		}


		IXbimGeometryModel^ XbimGeometryModel::Build(IfcFaceBasedSurfaceModel^ repItem, bool forceSolid)
		{

			if(repItem->FbsmFaces->Count == 0) return nullptr;
			else if(repItem->FbsmFaces->Count == 1 && dynamic_cast<IfcClosedShell^>(repItem->FbsmFaces->First))
			{
				//A Closed shell must be defined by IfcPolyLoop, therefore we can assume it comples as XbimFacetedShell
				if(forceSolid)
					return gcnew XbimSolid((IfcClosedShell^)(repItem->FbsmFaces->First));
				else
					return  gcnew XbimFacetedShell((IfcClosedShell^)(repItem->FbsmFaces->First));
			}

			else if(repItem->FbsmFaces->Count == 1) //its an open shell
			{
				if(forceSolid)
				{
					XbimShell^ shell = gcnew XbimShell(repItem->FbsmFaces->First);
					return  Fix(shell);
				}
				else
					return gcnew XbimFacetedShell(repItem->FbsmFaces->First);
			}

			else
			{
				XbimGeometryModelCollection^ gms = gcnew XbimGeometryModelCollection();	
				if(forceSolid)
				{
					for each(IfcConnectedFaceSet^ fbmsFaces in repItem->FbsmFaces)
					{
						if(dynamic_cast<IfcClosedShell^>(fbmsFaces)) 
							gms->Add(gcnew XbimSolid((IfcClosedShell^)fbmsFaces));
						else 
						{
							XbimShell^ shell = gcnew XbimShell(repItem->FbsmFaces->First);
							return  Fix(shell);
						}
					}
				}
				else
				{
					for each(IfcConnectedFaceSet^ fbmsFaces in repItem->FbsmFaces)
					{
						if(dynamic_cast<IfcClosedShell^>(fbmsFaces)) 
							gms->Add(gcnew XbimFacetedShell((IfcClosedShell^)fbmsFaces));
						else 
							gms->Add(gcnew XbimFacetedShell(fbmsFaces)); //just add the face set
					}
				}
				if( Enumerable::FirstOrDefault(gms) == nullptr)
					return nullptr;
				else
				{
					if(forceSolid)
						return gms->Solidify();
					else
						return gms;
				}
			}
		}

		IXbimGeometryModel^ XbimGeometryModel::Build(IfcShellBasedSurfaceModel^ repItem, bool forceSolid)
		{
			if(repItem->SbsmBoundary->Count == 0) return nullptr;
			else if(repItem->SbsmBoundary->Count == 1 && dynamic_cast<IfcClosedShell^>(repItem->SbsmBoundary->First) ) 
			{
				//A Closed shell must be defined by IfcPolyLoop, therefore we can assume it comples as XbimFacetedShell

				XbimFacetedShell^ solid =  gcnew XbimFacetedShell((IfcClosedShell^)(repItem->SbsmBoundary->First));
				/*if( Enumerable::FirstOrDefault(solid->Faces) == nullptr)
				return nullptr;
				else*/
				return solid;
			}
			else if(repItem->SbsmBoundary->Count == 1 )
			{
				XbimFacetedShell^ shell =  gcnew XbimFacetedShell(repItem->SbsmBoundary->First);
				/*if( Enumerable::FirstOrDefault(shell->CfsFaces) == nullptr)
				return nullptr;
				else*/
				return shell;
			}
			else
			{
				XbimGeometryModelCollection^ gms = gcnew XbimGeometryModelCollection();		
				for each(IfcShell^ sbms in repItem->SbsmBoundary)
				{
					if(dynamic_cast<IfcClosedShell^>(sbms)) 
						gms->Add(gcnew XbimFacetedShell((IfcClosedShell^)sbms));
					else if(dynamic_cast<IfcOpenShell^>(sbms))
						gms->Add(gcnew XbimFacetedShell((IfcOpenShell^)sbms));
					else
					{
						Type ^ type = sbms->GetType();
						throw(gcnew Exception(String::Format("XbimGeometryModel:Build(IfcShellBasedSurfaceModel). Could not BuildShape of type {0}. It is not implemented",type->Name)));
					}
				}
				if( Enumerable::FirstOrDefault(gms) == nullptr)
					return nullptr;
				else
				{
					if(forceSolid)
						return gms->Solidify();
					else
						return gms;
				}
			}

		}

		IXbimGeometryModel^ XbimGeometryModel::Fix(IXbimGeometryModel^ shape)
		{

			ShapeUpgrade_ShellSewing ss;
			TopoDS_Shape res = ss.ApplySewing(*(shape->Handle));
			if(res.IsNull())
			{
				System::Diagnostics::Debug::WriteLine("Failed to fix shape, an empty solid has been found");
				return nullptr;
			}
			if(res.ShapeType() == TopAbs_COMPOUND)
			{
				BRep_Builder b;
				TopoDS_Shell shell;
				b.MakeShell(shell);
				for(TopExp_Explorer fExp(res, TopAbs_FACE); fExp.More(); fExp.Next())
				{
					b.Add(shell, TopoDS::Face(fExp.Current()));
				}
				ShapeFix_Shell shellFix(shell);
				shellFix.Perform();
				ShapeFix_Solid sfs;
				return  gcnew XbimSolid(sfs.SolidFromShell(shellFix.Shell()));				
			}
			else if(res.ShapeType() == TopAbs_SHELL) //make shells into solids
			{
				ShapeFix_Shell shellFix(TopoDS::Shell(res));
				shellFix.Perform();
				ShapeFix_Solid sfs;
				return gcnew XbimSolid(sfs.SolidFromShell(shellFix.Shell()));				
			}
			else if(res.ShapeType() == TopAbs_SOLID)
				return gcnew XbimSolid(TopoDS::Solid(res));
			else if(res.ShapeType() == TopAbs_COMPSOLID)
				System::Diagnostics::Debug::WriteLine("Failed to fix shape, Compound Solids not supported");
			return nullptr;
		}

		void XbimGeometryModel::Test(XbimTriangularMeshStreamer* v1)
		{
			v1->info(3);
		}

#pragma unmanaged

		long OpenCascadeStreamerFeed(const TopoDS_Shape & shape, XbimTriangularMeshStreamer* tms)
		{
			// vertexData receives the calls from the following code that put the information in the binary stream.
			//
			// XbimTriangularMeshStreamer tms;

			// triangle indices are 1 based; this converts them to 0 based them and deals with multiple triangles to be added for multiple calls to faces.
			//
			int tally = -1;	

			for (TopExp_Explorer faceEx(shape,TopAbs_FACE) ; faceEx.More(); faceEx.Next()) 
			{
				const TopoDS_Face& face = TopoDS::Face(faceEx.Current());
				TopAbs_Orientation orient = face.Orientation();
				TopLoc_Location loc;
				Handle (Poly_Triangulation) facing = BRep_Tool::Triangulation(face,loc);
				if(facing.IsNull())
				{
					continue;
				}	

				// computation of normals
				// the returing array is 3 times longer than point array and it's to be read in groups of 3.
				//
				Poly::ComputeNormals(facing);
				const TShort_Array1OfShortReal& normals =  facing->Normals();
				
				Standard_Integer nbNodes = facing->NbNodes();
				// tms.info('p', (int)nbNodes);
				Standard_Integer nbTriangles = facing->NbTriangles();

				const TColgp_Array1OfPnt& points = facing->Nodes();
				int nTally = 0;

				tms->BeginFace(nbNodes);

				for(Standard_Integer nd = 1 ; nd <= nbNodes ; nd++)
				{
					gp_XYZ p = points(nd).Coord();
					loc.Transformation().Transforms(p); // bonghi: question: to fix how mapped representation works, will we still have to apply the transform? 
					
					tms->WritePoint((float)p.X(), (float)p.Y(), (float)p.Z());
					nTally+=3;
				}

				const Poly_Array1OfTriangle& triangles = facing->Triangles();

				Standard_Integer n1, n2, n3;
				float nrmx, nrmy, nrmz;

				tms->BeginPolygon(GL_TRIANGLES);
				for(Standard_Integer tr = 1 ; tr <= nbTriangles ; tr++)
				{
					triangles(tr).Get(n1, n2, n3); // triangle indices are 1 based
					int iPointIndex;
					if(orient == TopAbs_REVERSED)
					{
						tms->info('R');
						// setnormal and point
						iPointIndex = 3 * n1 - 2; // n3
						nrmx = -(float)normals(iPointIndex++);
						nrmy = -(float)normals(iPointIndex++);
						nrmz = -(float)normals(iPointIndex++);
						tms->SetNormal(nrmx, nrmy, nrmz);
						tms->WriteTriangleIndex(n3);
						

						// setnormal and point
						iPointIndex = 3 * n2 - 2;
						nrmx = -(float)normals(iPointIndex++);
						nrmy = -(float)normals(iPointIndex++);
						nrmz = -(float)normals(iPointIndex++);
						tms->SetNormal(nrmx, nrmy, nrmz);
						tms->WriteTriangleIndex(n2);
						

						// setnormal and point
						iPointIndex = 3 * n3 - 2; // n1
						nrmx = -(float)normals(iPointIndex++);
						nrmy = -(float)normals(iPointIndex++);
						nrmz = -(float)normals(iPointIndex++);
						tms->SetNormal(nrmx, nrmy, nrmz);
						tms->WriteTriangleIndex(n1);
						
					}
					else
					{
						tms->info('N');
						// setnormal and point
						iPointIndex = 3 * n1 - 2;
						nrmx = (float)normals(iPointIndex++);
						nrmy = (float)normals(iPointIndex++);
						nrmz = (float)normals(iPointIndex++);
						tms->SetNormal(nrmx, nrmy, nrmz);
						tms->WriteTriangleIndex(n1);
						

						// setnormal and point
						iPointIndex = 3 * n2 - 2;
						nrmx = (float)normals(iPointIndex++);
						nrmy = (float)normals(iPointIndex++);
						nrmz = (float)normals(iPointIndex++);
						tms->SetNormal(nrmx, nrmy, nrmz);
						tms->WriteTriangleIndex(n2);
						

						// setnormal and point
						iPointIndex = 3 * n3 - 2;
						nrmx = (float)normals(iPointIndex++);
						nrmy = (float)normals(iPointIndex++);
						nrmz = (float)normals(iPointIndex++);
						tms->SetNormal(nrmx, nrmy, nrmz);
						tms->WriteTriangleIndex(n3);
					}
				}
				tally+=nbNodes; // bonghi: question: point coordinates might be duplicated with this method for different faces. Size optimisation could be possible at the cost of performance speed.

				tms->EndPolygon();
				tms->EndFace();
			}
			int iSize = tms->StreamSize();
			return 0;
		}

		long OpenCascadeMesh(const TopoDS_Shape & shape, unsigned char* pStream, unsigned short faceCount, int nodeCount, int streamSize)
		{
			// vertexData receives the calls from the following code that put the information in the binary stream.
			//
			TesselateStream vertexData(pStream, faceCount, nodeCount, streamSize);
			XbimTriangularMeshStreamer tms;

			// triangle indices are 1 based; this converts them to 0 based them and deals with multiple triangles to be added for multiple calls to faces.
			//
			int tally = -1;	

			// writePoint is the pointer to the function used later to add an index entry for a polygon.
			// it's chosen depending on the size of the maximum value to be written (to save space in the stream)
			//
			void  (TesselateStream::*writePoint) (unsigned int);
			if(nodeCount<=0xFF) //we will use byte for indices
				writePoint = &TesselateStream::WritePointByte;
			else if(nodeCount<=0xFFFF) //use  unsigned short int for indices
				writePoint = &TesselateStream::WritePointShort;
			else //use unsigned int for indices
				writePoint = &TesselateStream::WritePointInt;


			for (TopExp_Explorer faceEx(shape,TopAbs_FACE) ; faceEx.More(); faceEx.Next()) 
			{
				const TopoDS_Face& face = TopoDS::Face(faceEx.Current());
				TopAbs_Orientation orient = face.Orientation();
				TopLoc_Location loc;
				Handle (Poly_Triangulation) facing = BRep_Tool::Triangulation(face,loc);
				if(facing.IsNull())
				{
					continue;
				}	

				// computation of normals
				// the returing array is 3 times longer than point array and it's to be read in groups of 3.
				//
				Poly::ComputeNormals(facing);
				const TShort_Array1OfShortReal& normals =  facing->Normals();
				// tms.info('n', (int)normals.Length());

				gp_Dir normal = GetNormal(face);
				vertexData.BeginFace(normal); //need to send array of normals
				
				/*
				float nx = (float)normal.X();
				float ny = (float)normal.Y();
				float nz = (float)normal.Z();
				tms.SetNormal(nx,ny,nz);*/

				/*for(Standard_Integer nd = 1 ; nd <= nbNodes ; nd++)
				{
				if(orient == TopAbs_REVERSED)
				receiver->AddNormal(-normals.Value(nTally+1),-normals.Value(nTally+2),-normals.Value(nTally+3));
				else
				receiver->AddNormal(normals.Value(nTally+1),normals.Value(nTally+2),normals.Value(nTally+3));
				nTally+=3;
				}*/

				Standard_Integer nbNodes = facing->NbNodes();
				// tms.info('p', (int)nbNodes);
				Standard_Integer nbTriangles = facing->NbTriangles();

				const TColgp_Array1OfPnt& points = facing->Nodes();
				int nTally = 0;

				tms.BeginFace(nbNodes);

				for(Standard_Integer nd = 1 ; nd <= nbNodes ; nd++)
				{
					gp_XYZ p = points(nd).Coord();
					loc.Transformation().Transforms(p); // bonghi: question: to fix how mapped representation works, will we still have to apply the transform? 
					vertexData.WritePoint(p.X(), p.Y(), p.Z());
					tms.WritePoint((float)p.X(), (float)p.Y(), (float)p.Z());
					nTally+=3;
				}

				const Poly_Array1OfTriangle& triangles = facing->Triangles();

				Standard_Integer n1, n2, n3;
				vertexData.BeginPolygon(GL_TRIANGLES);
				tms.BeginPolygon(GL_TRIANGLES);
				for(Standard_Integer tr = 1 ; tr <= nbTriangles ; tr++)
				{
					triangles(tr).Get(n1, n2, n3); // triangle indices are 1 based
					int iPointIndex;
					if(orient == TopAbs_REVERSED)
					{
						// tms.info('R');
						// setnormal and point
						iPointIndex = 3 * n3 - 2;
						// tms.info(iPointIndex);
						tms.SetNormal((float)normals(iPointIndex++), (float)normals(iPointIndex++), (float)normals(iPointIndex));
						tms.WriteTriangleIndex(n3+tally);
						(vertexData.*writePoint)(n3+tally);

						// setnormal and point
						iPointIndex = 3 * n2 - 2;
						// tms.info(iPointIndex);
						tms.SetNormal(normals(iPointIndex++),normals(iPointIndex++),normals(iPointIndex));
						tms.WriteTriangleIndex(n2+tally);
						(vertexData.*writePoint)(n2+tally);

						// setnormal and point
						iPointIndex = 3 * n1 - 2;
						//tms.info(iPointIndex);
						tms.SetNormal(normals(iPointIndex++),normals(iPointIndex++),normals(iPointIndex));
						tms.WriteTriangleIndex(n1+tally);
						(vertexData.*writePoint)(n1+tally);
					}
					else
					{
						// tms.info('N');
						// setnormal
						iPointIndex = 3 * n1 - 2;
						// tms.info(iPointIndex);
						tms.SetNormal(normals(iPointIndex++),normals(iPointIndex++),normals(iPointIndex));
						tms.WriteTriangleIndex(n1+tally);
						(vertexData.*writePoint)(n1+tally);

						// setnormal
						iPointIndex = 3 * n2 - 2;
						// tms.info(iPointIndex);
						tms.SetNormal(normals(iPointIndex++),normals(iPointIndex++),normals(iPointIndex));
						tms.WriteTriangleIndex(n2+tally);
						(vertexData.*writePoint)(n2+tally);

						// setnormal
						iPointIndex = 3 * n3 - 2;
						// tms.info(iPointIndex);
						tms.SetNormal(normals(iPointIndex++),normals(iPointIndex++),normals(iPointIndex));
						tms.WriteTriangleIndex(n3+tally);
						(vertexData.*writePoint)(n3+tally);
					}
				}
				tally+=nbNodes; // bonghi: question: point coordinates might be duplicated with this method for different faces. Size optimisation could be possible at the cost of performance speed.
				vertexData.EndPolygon();
				vertexData.EndFace();

				tms.EndPolygon();
				tms.EndFace();
			}
			int iSize = tms.StreamSize();
			return vertexData.Length();
		}

		long OpenGLMesh(const TopoDS_Shape & shape, const TopTools_IndexedMapOfShape& points, unsigned char* pStream, unsigned short faceCount, int streamSize)
		{

			GLUtesselator *tess = gluNewTess();

			gluTessCallback(tess, GLU_TESS_BEGIN_DATA,  (void (CALLBACK *)()) BeginTessellate);
			gluTessCallback(tess, GLU_TESS_END_DATA,  (void (CALLBACK *)()) EndTessellate);
			gluTessCallback(tess, GLU_TESS_ERROR,    (void (CALLBACK *)()) TessellateError);
			int vertexCount=points.Extent();
			if(vertexCount<=0xFF) //we will use byte for indices
				gluTessCallback(tess, GLU_TESS_VERTEX_DATA,  (void (CALLBACK *)()) AddVertexByte);
			else if(vertexCount<=0xFFFF) //use  unsigned short int for indices
				gluTessCallback(tess, GLU_TESS_VERTEX_DATA,  (void (CALLBACK *)()) AddVertexShort);
			else //use unsigned int for indices
				gluTessCallback(tess, GLU_TESS_VERTEX_DATA,  (void (CALLBACK *)()) AddVertexInt);

			GLdouble glPt3D[3];
			TesselateStream vertexData(pStream, points, faceCount, streamSize);
			for (TopExp_Explorer faceEx(shape,TopAbs_FACE) ; faceEx.More(); faceEx.Next()) 
			{
				const TopoDS_Face& face = TopoDS::Face(faceEx.Current());
				gp_Dir normal = GetNormal(face);
				vertexData.BeginFace(normal);
				gluTessBeginPolygon(tess, &vertexData);
				// go over each wire
				for (TopExp_Explorer wireEx(face,TopAbs_WIRE) ; wireEx.More(); wireEx.Next()) 
				{

					gluTessBeginContour(tess);
					const TopoDS_Wire& wire = TopoDS::Wire(wireEx.Current());

					BRepTools_WireExplorer wEx(wire);

					for(;wEx.More();wEx.Next())
					{
						const TopoDS_Edge& edge = wEx.Current();
						const TopoDS_Vertex& vertex=  wEx.CurrentVertex();
						gp_Pnt p = BRep_Tool::Pnt(vertex);
						glPt3D[0] = p.X();
						glPt3D[1] = p.Y();
						glPt3D[2] = p.Z();
						void * pIndex = (void *) (points.FindIndex(vertex) - 1);//convert to 0 based
						gluTessVertex(tess, glPt3D, pIndex); 
					}
					gluTessEndContour(tess);
				}
				gluTessEndPolygon(tess);
				vertexData.EndFace();
			}

			gluDeleteTess(tess);
			return vertexData.Length();
		}



#pragma managed

		int GetIndexSize(int NumPoints)
		{
			int indexSize;
			if(NumPoints<=0xFF) //we will use byte for indices
				indexSize =sizeof(unsigned char) ;
			else if(NumPoints<=0xFFFF) 
				indexSize = sizeof(unsigned short); //use  unsigned short int for indices
			else
				indexSize = sizeof(unsigned int); //use unsigned int for indices
			return indexSize;
		}

		XbimTriangulatedModelStream^ XbimGeometryModel::Mesh(IXbimGeometryModel^ shape, bool withNormals, double deflection, Matrix3D transform )
		{
			//Build the Mesh
			try
			{
				bool hasCurvedEdges = shape->HasCurvedEdges;

				// transformed shape is the shape placed according to the transform matrix
				TopoDS_Shape transformedShape;
				if(transform!=Matrix3D::Identity)
				{
					BRepBuilderAPI_Transform gTran(transformedShape,XbimGeomPrim::ToTransform(transform));
					transformedShape = gTran.Shape();
				}
				else
					transformedShape = *(shape->Handle);
				
				//decide which meshing algorithm to use, Opencascade is slow but necessary to resolve curved edges
				if (true) // hasCurvedEdges) 
				{
					// BRepMesh_IncrementalMesh calls BRepMesh_FastDiscret to create the mesh geometry.
					//
					// todo: Bonghi: Question: is this ok to use the shape instead of transformedShape? I assume the transformed shape points to the shape.
					BRepMesh_IncrementalMesh incrementalMesh(*(shape->Handle), deflection); 
					try
					{
						XbimTriangularMeshStreamer value;
						XbimTriangularMeshStreamer* m = &value;
						long streamLen2 = OpenCascadeStreamerFeed(transformedShape, m);
						int isssss = m->StreamSize();

						IntPtr BonghiUnManMem = Marshal::AllocHGlobal(isssss);
						unsigned char* BonghiUnManMemBuf = (unsigned char*)BonghiUnManMem.ToPointer();
						m->StreamTo(BonghiUnManMemBuf);

						array<unsigned char>^ BmanagedArray = gcnew array<unsigned char>(isssss);
						Marshal::Copy(BonghiUnManMem, BmanagedArray, 0, isssss);
						Marshal::FreeHGlobal(BonghiUnManMem);
						return gcnew XbimTriangulatedModelStream(BmanagedArray);
					}
					catch(...)
					{
						System::Diagnostics::Debug::WriteLine("Error processing geometry in XbimGeometryModel::Mesh");
					}
					finally
					{
						// Marshal::FreeHGlobal(vertexPtr);
					}
				}
				else // use opengl (faster than opencascade)
				{
					//size the job up
					//get all of the vertices in a map
					TopTools_IndexedMapOfShape points;
					unsigned short faceCount = 0;
					int maxVertexCount = 0;
					int triangleIndexCount = 0;
					
					for (TopExp_Explorer faceEx(transformedShape,TopAbs_FACE) ; faceEx.More(); faceEx.Next()) 
					{
						faceCount++;
						for (TopExp_Explorer vEx(faceEx.Current(),TopAbs_VERTEX) ; vEx.More(); vEx.Next()) 
						{
							maxVertexCount++;
							points.Add(vEx.Current());
						}
					}
					int vertexCount = points.Extent();
					if(vertexCount==0) 
						return XbimTriangulatedModelStream::Empty;
					int memSize =  sizeof(int) + (vertexCount * 3 *sizeof(double)); //number of points plus x,y,z of each point
					memSize += sizeof(unsigned int); //allow int for total number of faces
					
					int indexSize = GetIndexSize(vertexCount);
					
					memSize += faceCount * (sizeof(unsigned char)+(2*sizeof(unsigned short)) +sizeof(unsigned short)+ 3 * sizeof(double)); //allow space for the type of triangulation (1 byte plus number of indices - 2 bytes plus polygon count-2 bytes) + normal count + the normal
					memSize += (maxVertexCount*indexSize) + (maxVertexCount); //assume worst case each face is made only of triangles, Max number of indices + Triangle Mode=1byte per triangle
					IntPtr vertexPtr = Marshal::AllocHGlobal(memSize);
					unsigned char* pointBuffer = (unsigned char*)vertexPtr.ToPointer();

					try
					{
						long streamLen = OpenGLMesh(transformedShape, points, pointBuffer, faceCount,memSize );

						array<unsigned char>^ managedArray = gcnew array<unsigned char>(streamLen);
						Marshal::Copy(vertexPtr, managedArray, 0, streamLen);
						return gcnew XbimTriangulatedModelStream(managedArray);
					}
					catch(...)
					{
						System::Diagnostics::Debug::WriteLine("Error processing geometry in XbimGeometryModel::Mesh");
					}
					finally
					{
						Marshal::FreeHGlobal(vertexPtr);
					}
				}
			}
			catch(...)
			{
				System::Diagnostics::Debug::WriteLine("Failed to Triangulate shape");
				return XbimTriangulatedModelStream::Empty;
			}
		}	

		XbimBoundingBox^ XbimGeometryModel::GetBoundingBox(IXbimGeometryModel^ shape, bool precise)
		{
			Bnd_Box * pBox = new Bnd_Box();
			if(precise)
			{
				BRepBndLib::Add(*(shape->Handle), *pBox);
			}
			else
			{
				BRepBndLib::AddClose(*(shape->Handle), *pBox);
			}
			return gcnew XbimBoundingBox(pBox);
		};
	}
}