#include "StdAfx.h"
#include "XbimGeometryModel.h"
#include "XbimLocation.h"
#include "XbimSolid.h"
#include "XbimGeomPrim.h"
#include "XbimFeaturedShape.h"
#include "XbimShell.h"
#include "XbimGeometryModelCollection.h"
#include "XbimFacetedShell.h"
#include "XbimMap.h"

#include <BRepTools.hxx>
#include <TopoDS_Shell.hxx>
#include <TopoDS_Solid.hxx>

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
using namespace Xbim::Ifc::SharedComponentElements;
using namespace System::Linq;
using namespace Xbim::IO;
using namespace Xbim::Common::Exceptions;
class Message_ProgressIndicator {};

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
void TesselateStream::BeginFace(const double x, const double y, const double z)
{
	//reset polygon count
	_polygonCount=0;
	//write out space for number of polygons
	_faceStart = _position;
	_position += sizeof(unsigned short);
	unsigned short * pCount = (unsigned short *)(_pDataStream + _position); _position += sizeof(unsigned short);
	*pCount=1;
	double* pCord = (double *)(_pDataStream + _position);
	*pCord = x; _position += sizeof(double);
	pCord = (double *)(_pDataStream + _position);
	*pCord = y; _position += sizeof(double);
	pCord = (double *)(_pDataStream + _position);
	*pCord = z; _position += sizeof(double);

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
	
		bool XbimGeometryModel::CutOpenings(IfcProduct^ product, XbimLOD lod)
		{
			if(dynamic_cast<IfcElement^>(product))
			{
				//add in additional types here that you don't want to cut
				if(dynamic_cast<IfcBeam^>(product) ||
					dynamic_cast<IfcColumn^>(product) ||
					dynamic_cast<IfcMember^>(product)||
					dynamic_cast<IfcElementAssembly^>(product)||
					dynamic_cast<IfcPlate^>(product))
				{
					return lod==XbimLOD::LOD400;
				}
				else
					return true;
			}
			else
				return false;
		}
		/* Creates a 3D Model geometry for the product based upon the first "Body" ShapeRepresentation that can be found in  
		Products.ProductRepresentation that is within the specified GeometricRepresentationContext, if the Representation 
		context is null the first "Body" ShapeRepresentation is used. Returns null if their is no valid geometric definition
		*/
		IXbimGeometryModel^ XbimGeometryModel::CreateFrom(IfcProduct^ product, IfcGeometricRepresentationContext^ repContext, Dictionary<IfcRepresentation^, IXbimGeometryModel^>^ maps, bool forceSolid, XbimLOD lod)
		{
			try
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
							BRepLib::Precision((Standard_Real)((IfcGeometricRepresentationContext^)(shape->ContextOfItems))->DefaultPrecision*100);
						
						if(CutOpenings(product, lod))
						{
							IfcElement^ element = (IfcElement^) product;
							//check if we have openings or projections
							IEnumerable<IfcRelProjectsElement^>^ projections = element->HasProjections;
							IEnumerable<IfcRelVoidsElement^>^ openings = element->HasOpenings;
							//srl optimisation openings and projects cannot have openings or projection so don't check for them
							if( !dynamic_cast<IfcFeatureElement^>(product ) && 
								( Enumerable::FirstOrDefault(openings) !=nullptr || Enumerable::FirstOrDefault(projections) !=nullptr ))
							{
								List<IXbimGeometryModel^>^ projectionSolids = gcnew List<IXbimGeometryModel^>();
								List<IXbimGeometryModel^>^ openingSolids = gcnew List<IXbimGeometryModel^>();
								for each(IfcRelProjectsElement^ rel in projections)
								{
									IfcFeatureElementAddition^ fe = rel->RelatedFeatureElement;
									if(fe->Representation!=nullptr)
									{
										IXbimGeometryModel^ im = CreateFrom(fe,repContext, maps, true, lod);
										if(!dynamic_cast<XbimSolid^>(im))
											throw gcnew XbimGeometryException("FeatureElementAdditions must be of type solid");

										im = im->CopyTo(fe->ObjectPlacement);
										projectionSolids->Add(im);
									}
								}

								for each(IfcRelVoidsElement^ rel in openings)
								{
									IfcFeatureElementSubtraction^ fe = rel->RelatedOpeningElement;
									if(fe->Representation!=nullptr)
									{
										IXbimGeometryModel^ im = CreateFrom(fe, repContext, maps, true,lod);
										if(im!=nullptr)
										{	
											im = im->CopyTo(fe->ObjectPlacement);
											//the rules say that 
											//The PlacementRelTo relationship of IfcLocalPlacement shall point (if given) 
											//to the local placement of the master IfcElement (its relevant subtypes), 
											//which is associated to the IfcFeatureElement by the appropriate relationship object
											if(product->ObjectPlacement != ((IfcLocalPlacement^)(fe->ObjectPlacement))->PlacementRelTo)
											{
												if(dynamic_cast<IfcLocalPlacement^>(product->ObjectPlacement))
												{	
													//we need to move the opening into the coordinate space of the product
													IfcLocalPlacement^ lp = (IfcLocalPlacement^)product->ObjectPlacement;							
													TopLoc_Location prodLoc = XbimGeomPrim::ToLocation(lp->RelativePlacement);
													prodLoc= prodLoc.Inverted();

													(*(im->Handle)).Move(prodLoc);	
												}
											}
											openingSolids->Add(im);
										}
									}
									
								}
								IXbimGeometryModel^ baseShape = CreateFrom(shape, maps, true,lod);	
								try
								{
									IXbimGeometryModel^ fShape =  gcnew XbimFeaturedShape(baseShape, openingSolids, projectionSolids);
									return fShape;
								}
								catch (XbimGeometryException^ xbimE)
								{
									Logger->WarnFormat("Failed create accurate geometry for entity #{0}={1}\n{2}A simplified representation for the shape has been used",product->EntityLabel,product->GetType()->Name,xbimE->Message);
									return baseShape;
								}
							}
							else //we have no openings or projections
								return CreateFrom(shape, maps, forceSolid,lod);
						}
						else
							return CreateFrom(shape, maps, forceSolid,lod);
					}
				}
			}
			}
			catch(XbimGeometryException^ xbimE)
			{
				Logger->ErrorFormat("Error creating geometry for entity #{0}={1}\n{2}\nThe geometry has been omitted",product->EntityLabel,product->GetType()->Name,xbimE->Message);
			}
			return nullptr;
		}

		IXbimGeometryModel^ XbimGeometryModel::CreateFrom(IfcProduct^ product, Dictionary<IfcRepresentation^, IXbimGeometryModel^>^ maps, bool forceSolid, XbimLOD lod)
		{
			return CreateFrom(product, nullptr, maps, forceSolid, lod);
		}

		IXbimGeometryModel^ XbimGeometryModel::CreateFrom(IfcProduct^ product, bool forceSolid, XbimLOD lod)
		{
			// HACK: Ideally we shouldn't need this try-catch handler. This just allows us to log the fault, and raise a managed exception, before the application terminates.
			// Upstream callers should ideally terminate the application ASAP.
			__try
			{
				return CreateFrom(product, nullptr, gcnew Dictionary<IfcRepresentation^, IXbimGeometryModel^>(), forceSolid,lod);
			}
			__except(GetExceptionCode() == EXCEPTION_ACCESS_VIOLATION)
			{
				Logger->Fatal("Access Violation in geometry engine. This may leave the application in an inconsistent state!");
				throw gcnew AccessViolationException(
					"A memory access violation occurred in the geometry engine. The application and geometry may be in an inconsistent state and the process should be terminated.");
			}
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
					throw(gcnew XbimException("XbimGeometryModel. Build(BooleanResult) FirstOperand must be a valid IfcBooleanOperand"));
			
				
			try
			{
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
					throw(gcnew XbimException("XbimGeometryModel. Build(BooleanResult) FirstOperand must be a valid IfcBooleanOperand"));

				//check if we have boxed half spaces then see if there is any intersect
				if(_shape1IsSolid.HasValue)
				{

					if(!shape1->GetBoundingBox(false)->Intersects(shape2->GetBoundingBox(false)))
					{
						if(_shape1IsSolid.Value == true) return shape1; else return shape2;
					}

				}

				if((*(shape2->Handle)).IsNull())
					return shape1; //nothing to subtract
				
				switch(repItem->Operator)
				{
				case IfcBooleanOperator::Union:
					return shape1->Union(shape2);	
				case IfcBooleanOperator::Intersection:
					return shape1->Intersection(shape2);
				case IfcBooleanOperator::Difference:
					return shape1->Cut(shape2);

				default:
					throw(gcnew InvalidOperationException("XbimGeometryModel. Build(BooleanClippingResult) Unsupported Operation"));
				}
			}
			catch(XbimGeometryException^ xbimE)
			{
				Logger->WarnFormat("Error performing boolean operation for entity #{0}={1}\n{2}\nA simplified version has been used",repItem->EntityLabel,repItem->GetType()->Name,xbimE->Message);
				return shape1;
			}
		}


		/*
		Create a model geometry for a given shape
		*/
		IXbimGeometryModel^ XbimGeometryModel::CreateFrom(IfcRepresentation^ rep, Dictionary<IfcRepresentation^, IXbimGeometryModel^>^ maps, bool forceSolid, XbimLOD lod)
		{

			if(rep->Items->Count == 0) //we have nothing to do
				return nullptr;
			else if (rep->Items->Count == 1) //we have a single shape geometry
			{
				return CreateFrom(rep->Items->First,maps, forceSolid,lod);
			}
			else // we have a compound shape
			{
				XbimGeometryModelCollection^ gms = gcnew XbimGeometryModelCollection();

				for each (IfcRepresentationItem^ repItem in rep->Items)
				{
					IXbimGeometryModel^ geom = CreateFrom(repItem,maps,false,lod); // we will make a solid when we have all the bits if necessary
					if(!(dynamic_cast<XbimSolid^>(geom) && (*(geom->Handle)).IsNull())) gms->Add(geom); //don't add solids that are empty
				}
				if(forceSolid)
					return gms->Solidify();
				else
					return gms;
			}
		}

		IXbimGeometryModel^ XbimGeometryModel::CreateFrom(IfcRepresentation^ rep, bool forceSolid, XbimLOD lod)
		{
			return CreateFrom(rep, gcnew Dictionary<IfcRepresentation^, IXbimGeometryModel^>(), forceSolid,lod);
		}

		IXbimGeometryModel^ XbimGeometryModel::CreateFrom(IfcRepresentationItem^ repItem, bool forceSolid, XbimLOD lod)
		{
			return CreateFrom(repItem, gcnew Dictionary<IfcRepresentation^, IXbimGeometryModel^>(), forceSolid,lod);
		}

		IXbimGeometryModel^ XbimGeometryModel::CreateFrom(IfcRepresentationItem^ repItem, Dictionary<IfcRepresentation^, IXbimGeometryModel^>^ maps, bool forceSolid,XbimLOD lod)
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
					mg =  CreateFrom(repMap->MappedRepresentation,maps, forceSolid,lod); //make the first one
					maps->Add(repMap->MappedRepresentation, mg);
				}

				//need to transform all the geometries as below
				if(mg!=nullptr)
					return CreateMap(mg, repMap->MappingOrigin, map->MappingTarget,maps, forceSolid);
				else
					return nullptr;

			}
			else if(dynamic_cast<IfcGeometricSet^>(repItem))
			{
				IfcGeometricSet^ gset = (IfcGeometricSet^) repItem;
				Logger->Warn(String::Format("Support for IfcGeometricSet #{0} has not been implemented", Math::Abs(gset->EntityLabel)));
			}
			else
			{
				Type ^ type = repItem->GetType();
				Logger->Warn(String::Format("XbimGeometryModel. Could not Build Geometry #{0}, type {1} is not implemented",Math::Abs(repItem->EntityLabel), type->Name));
			}
			return nullptr;
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
				throw(gcnew ArgumentOutOfRangeException("XbimGeometryModel.CreateMap Unsupported IXbimGeometryModel type"));
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
						throw(gcnew NotImplementedException(String::Format("XbimGeometryModel:Build(IfcShellBasedSurfaceModel). Could not BuildShape of type {0}. It is not implemented",type->Name)));
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
				Logger->Warn("Failed to fix shape, an empty solid has been found");
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
				Logger->Warn("Failed to fix shape, Compound Solids not supported");
			return nullptr;
		}

#pragma unmanaged
#pragma unmanaged
		long OpenCascadeMesh(const TopoDS_Shape & shape, unsigned char* pStream, unsigned short faceCount, int nodeCount, int streamSize)
		{

			TesselateStream vertexData(pStream, faceCount, nodeCount, streamSize);

			int tally = -1;	
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

				/*Poly::ComputeNormals(facing);
				nTally = 0;
				const TShort_Array1OfShortReal& normals =  facing->Normals();*/

				gp_Dir normal = GetNormal(face);
				vertexData.BeginFace(normal); //need to send array of normals

				/*for(Standard_Integer nd = 1 ; nd <= nbNodes ; nd++)
				{
				if(orient == TopAbs_REVERSED)
				receiver->AddNormal(-normals.Value(nTally+1),-normals.Value(nTally+2),-normals.Value(nTally+3));
				else
				receiver->AddNormal(normals.Value(nTally+1),normals.Value(nTally+2),normals.Value(nTally+3));
				nTally+=3;
				}*/

				Standard_Integer nbNodes = facing->NbNodes();
				Standard_Integer nbTriangles = facing->NbTriangles();

				const TColgp_Array1OfPnt& points = facing->Nodes();
				int nTally = 0;

				for(Standard_Integer nd = 1 ; nd <= nbNodes ; nd++)
				{
					gp_XYZ p = points(nd).Coord();
					loc.Transformation().Transforms(p);
					vertexData.WritePoint(p.X(), p.Y(), p.Z());
					nTally+=3;
				}

				const Poly_Array1OfTriangle& triangles = facing->Triangles();

				Standard_Integer n1, n2, n3;
				vertexData.BeginPolygon(GL_TRIANGLES);
				for(Standard_Integer tr = 1 ; tr <= nbTriangles ; tr++)
				{
					triangles(tr).Get(n1, n2, n3);

					if(orient == TopAbs_REVERSED)
					{
						(vertexData.*writePoint)(n3+tally);
						(vertexData.*writePoint)(n2+tally);
						(vertexData.*writePoint)(n1+tally);
					}
					else
					{
						(vertexData.*writePoint)(n1+tally);
						(vertexData.*writePoint)(n2+tally);
						(vertexData.*writePoint)(n3+tally);
					}
				}
				tally+=nbNodes;
				vertexData.EndPolygon();
				vertexData.EndFace();
			}
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


		XbimTriangulatedModelStream^ XbimGeometryModel::Mesh(IXbimGeometryModel^ shape, bool withNormals, double deflection, Matrix3D transform )
		{

			//Build the Mesh
			try
			{
				bool hasCurvedEdges = shape->HasCurvedEdges;
				if(hasCurvedEdges) BRepMesh_IncrementalMesh incrementalMesh(*(shape->Handle), deflection);
				//size the job up
				//get all of the vertices in a map
				TopTools_IndexedMapOfShape points;
				unsigned short faceCount = 0;
				int maxVertexCount = 0;
				int triangleIndexCount = 0;
				
				TopoDS_Shape transformedShape;
				if(transform!=Matrix3D::Identity)
				{
					BRepBuilderAPI_Transform gTran(transformedShape,XbimGeomPrim::ToTransform(transform));
					transformedShape = gTran.Shape();
				}
				else
					transformedShape = *(shape->Handle);
				for (TopExp_Explorer faceEx(transformedShape,TopAbs_FACE) ; faceEx.More(); faceEx.Next()) 
				{
					faceCount++;
					if(hasCurvedEdges)
					{
						TopLoc_Location loc;
						Handle (Poly_Triangulation) facing = BRep_Tool::Triangulation(TopoDS::Face(faceEx.Current()),loc);
						
						if(facing.IsNull())
							continue;
						maxVertexCount+=facing->NbNodes();
						triangleIndexCount += facing->NbTriangles()*3;
					}
					else
					{
						for (TopExp_Explorer vEx(faceEx.Current(),TopAbs_VERTEX) ; vEx.More(); vEx.Next()) 
						{

							maxVertexCount++;
							points.Add(vEx.Current());

						}
					}
				}
				int vertexCount;
				if(hasCurvedEdges) vertexCount=maxVertexCount; else vertexCount= points.Extent();
				if(vertexCount==0) return XbimTriangulatedModelStream::Empty;
				int memSize =  sizeof(int) + (vertexCount * 3 *sizeof(double)); //number of points plus x,y,z of each point

				memSize += sizeof(unsigned int); //allow int for total number of faces
				int indexSize;
				if(vertexCount<=0xFF) //we will use byte for indices
					indexSize =sizeof(unsigned char) ;
				else if(vertexCount<=0xFFFF) 
					indexSize = sizeof(unsigned short); //use  unsigned short int for indices
				else
					indexSize = sizeof(unsigned int); //use unsigned int for indices
				memSize += faceCount * (sizeof(unsigned char)+(2*sizeof(unsigned short)) +sizeof(unsigned short)+ 3 * sizeof(double)); //allow space for the type of triangulation (1 byte plus number of indices - 2 bytes plus polygon count-2 bytes) + normal count + the normal
				if(hasCurvedEdges)
					memSize += triangleIndexCount * indexSize; //write out each indices
				else
					memSize += (maxVertexCount*indexSize) + (maxVertexCount); //assume worst case each face is made only of triangles, Max number of indices + Triangle Mode=1byte per triangle
				IntPtr vertexPtr = Marshal::AllocHGlobal(memSize);
				unsigned char* pointBuffer = (unsigned char*)vertexPtr.ToPointer();
				//decide which meshing algorithm to use, Opencascade is slow but necessary to resolve curved edges
				try
				{
					long streamLen;
					if(hasCurvedEdges)
						streamLen=OpenCascadeMesh(transformedShape, pointBuffer, faceCount, maxVertexCount,memSize);
					else
						streamLen=OpenGLMesh(transformedShape, points, pointBuffer, faceCount,memSize );

					array<unsigned char>^ managedArray = gcnew array<unsigned char>(streamLen);
					Marshal::Copy(vertexPtr, managedArray, 0, streamLen);
					return gcnew XbimTriangulatedModelStream(managedArray);
				}
				catch(...)
				{
					Logger->Error("Error processing geometry in XbimGeometryModel::Mesh");
					return XbimTriangulatedModelStream::Empty;
				}
				finally
				{
					Marshal::FreeHGlobal(vertexPtr);
				}
			}
			catch(...)
			{
				Logger->Error("Failed to Triangulate shape");
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