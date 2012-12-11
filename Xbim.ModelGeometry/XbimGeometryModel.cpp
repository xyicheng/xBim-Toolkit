#include "StdAfx.h"
#include "XbimGeometryModel.h"
#include "XbimTriangularMeshStreamer.h"
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
#using  <Xbim.IO.dll> as_friend
using namespace Xbim::IO;
using namespace Xbim::Ifc2x3::ProductExtension;
using namespace Xbim::Ifc2x3::SharedComponentElements;
using namespace System::Linq;
using namespace Xbim::Ifc2x3::PresentationAppearanceResource;
using namespace Xbim::Common::Exceptions;
class Message_ProgressIndicator {};


void CALLBACK XMS_BeginTessellate(GLenum type, void *pPolygonData)
{
	((XbimTriangularMeshStreamer*)pPolygonData)->BeginPolygon(type);
};
void CALLBACK XMS_EndTessellate(void *pVertexData)
{
	((XbimTriangularMeshStreamer*)pVertexData)->EndPolygon();
};
void CALLBACK XMS_TessellateError(GLenum err)
{
	// swallow the error.
};
void CALLBACK XMS_AddVertexIndex(void *pVertexData, void *pPolygonData)
{
	((XbimTriangularMeshStreamer*)pPolygonData)->WriteTriangleIndex((unsigned int)pVertexData);
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
		IXbimGeometryModel^ XbimGeometryModel::CreateFrom(IfcProduct^ product, IfcGeometricRepresentationContext^ repContext, Dictionary<IfcRepresentation^, IXbimGeometryModel^>^ maps, bool forceSolid, XbimLOD lod, bool occOut)
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
								BRepLib::Precision((Standard_Real)((IfcGeometricRepresentationContext^)(shape->ContextOfItems))->DefaultPrecision);

							//srl optimisation openings and projectionss cannot have openings or projection so don't check for them
							if(CutOpenings(product, lod) && !dynamic_cast<IfcFeatureElement^>(product ))
							{
								IfcElement^ element = (IfcElement^) product;
								List<IXbimGeometryModel^>^ projectionSolids = gcnew List<IXbimGeometryModel^>();
								List<IXbimGeometryModel^>^ openingSolids = gcnew List<IXbimGeometryModel^>();
								for each(IfcRelProjectsElement^ rel in element->HasProjections)
								{
									IfcFeatureElementAddition^ fe = rel->RelatedFeatureElement;
									if(fe->Representation!=nullptr)
									{
										IfcFeatureElementAddition^ fe = rel->RelatedFeatureElement;
										if(fe->Representation!=nullptr)
										{
											IXbimGeometryModel^ im = CreateFrom(fe,repContext, maps, true, lod, occOut);
											if(dynamic_cast<XbimGeometryModelCollection^>(im))
												im = ((XbimGeometryModelCollection^)im)->Solidify();
											if(!dynamic_cast<XbimSolid^>(im))
												throw gcnew XbimGeometryException("FeatureElementAdditions must be of type solid");

											im = im->CopyTo(fe->ObjectPlacement);
											projectionSolids->Add(im);
										}
									}
								}
								for each(IfcRelVoidsElement^ rel in element->HasOpenings)
								{
									IfcFeatureElementSubtraction^ fe = rel->RelatedOpeningElement;
									if(fe->Representation!=nullptr)
									{
										IXbimGeometryModel^ im = CreateFrom(fe, repContext, maps, true,lod, occOut);
										if(im!=nullptr && !im->Handle->IsNull())
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
								if(Enumerable::Any(openingSolids) || Enumerable::Any(projectionSolids))
								{

									IXbimGeometryModel^ baseShape = CreateFrom(shape, maps, true,lod, occOut);	

									IXbimGeometryModel^ fshape = gcnew XbimFeaturedShape(product, baseShape, openingSolids, projectionSolids);
#ifdef _DEBUG
									if(occOut)
									{
										char fname[512];
										sprintf(fname, "#%d",shape->EntityLabel);
										BRepTools::Write(*(fshape->Handle),fname );
									}
#endif

									return fshape;
								}
								else //we have no openings or projections
								{

									IXbimGeometryModel^ fshape = CreateFrom(shape, maps, forceSolid,lod, occOut);
#ifdef _DEBUG
									if(occOut)
									{

										char fname[512];
										sprintf(fname, "#%d",shape->EntityLabel);
										BRepTools::Write(*(fshape->Handle),fname );

									}
#endif
									return fshape;
								}
							}
							else
							{

								IXbimGeometryModel^ fshape = CreateFrom(shape, maps, forceSolid,lod, occOut);
#ifdef _DEBUG
								if(occOut)
								{
									char fname[512];
									sprintf(fname, "#%d",shape->EntityLabel);
									BRepTools::Write(*(fshape->Handle),fname );
								}
#endif
								return fshape;
							}
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

		IXbimGeometryModel^ XbimGeometryModel::CreateFrom(IfcProduct^ product, Dictionary<IfcRepresentation^, IXbimGeometryModel^>^ maps, bool forceSolid, XbimLOD lod, bool occOut)
		{
			return CreateFrom(product, nullptr, maps, forceSolid, lod, occOut);
		}

		IXbimGeometryModel^ XbimGeometryModel::CreateFrom(IfcProduct^ product, bool forceSolid, XbimLOD lod, bool occOut)
		{
			// HACK: Ideally we shouldn't need this try-catch handler. This just allows us to log the fault, and raise a managed exception, before the application terminates.
			// Upstream callers should ideally terminate the application ASAP.
			__try
			{
				return CreateFrom(product, nullptr, gcnew Dictionary<IfcRepresentation^, IXbimGeometryModel^>(), forceSolid,lod,occOut);
			}
			__except(GetExceptionCode() == EXCEPTION_ACCESS_VIOLATION)
			{
				Logger->Fatal("Access Violation in geometry engine. Thireturns may leave the application in an inconsistent state!");
				throw gcnew AccessViolationException(
					"A memory access violation occurred in the geometry engine. The application and geometry may be in an inconsistent state and the process should be terminated.");
			}
		}

		//IXbimGeometryModel^ XbimGeometryModel::Build(IfcBooleanResult^ repItem)
		//{
		//	IfcBooleanOperand^ fOp= repItem->FirstOperand;
		//	IfcBooleanOperand^ sOp= repItem->SecondOperand;
		//	IXbimGeometryModel^ shape1;
		//	IXbimGeometryModel^ shape2;
		//	System::Nullable<bool> _shape1IsSolid;
		//	if(dynamic_cast<IfcBooleanResult^>(fOp))
		//		shape1 = Build((IfcBooleanResult^)fOp);
		//	else if(dynamic_cast<IfcSolidModel^>(fOp))
		//		shape1 = gcnew XbimSolid((IfcSolidModel^)fOp);
		//	else if(dynamic_cast<IfcHalfSpaceSolid^>(fOp))
		//	{
		//		shape1 = gcnew XbimSolid((IfcHalfSpaceSolid^)fOp);
		//		if(dynamic_cast<IfcBoxedHalfSpace^>(fOp))
		//			_shape1IsSolid = false;
		//	}
		//	else if(dynamic_cast<IfcCsgPrimitive3D^>(fOp))
		//		shape1 = gcnew XbimSolid((IfcCsgPrimitive3D^)fOp);
		//	else
		//		throw(gcnew XbimException("XbimGeometryModel. Build(BooleanResult) FirstOperand must be a valid IfcBooleanOperand"));


		//	try
		//	{

		//		if(dynamic_cast<IfcBooleanResult^>(sOp))
		//			shape2 = Build((IfcBooleanResult^)sOp);
		//		else if(dynamic_cast<IfcSolidModel^>(sOp))
		//			shape2 = gcnew XbimSolid((IfcSolidModel^)sOp);
		//		else if(dynamic_cast<IfcHalfSpaceSolid^>(sOp))
		//		{
		//			shape2 = gcnew XbimSolid((IfcHalfSpaceSolid^)sOp);
		//			if(dynamic_cast<IfcBoxedHalfSpace^>(sOp))
		//				_shape1IsSolid = true;
		//		}
		//		else if(dynamic_cast<IfcCsgPrimitive3D^>(sOp))
		//			shape2 = gcnew XbimSolid((IfcCsgPrimitive3D^)sOp);
		//		else
		//			throw(gcnew XbimException("XbimGeometryModel. Build(BooleanResult) FirstOperand must be a valid IfcBooleanOperand"));

		//		//check if we have boxed half spaces then see if there is any intersect
		//		if(_shape1IsSolid.HasValue)
		//		{

		//			if(!shape1->GetBoundingBox(false)->Intersects(shape2->GetBoundingBox(false)))
		//			{
		//				if(_shape1IsSolid.Value == true) return shape1; else return shape2;
		//			}

		//		}

		//		if((*(shape2->Handle)).IsNull())
		//			return shape1; //nothing to subtract

		//		switch(repItem->Operator)
		//		{
		//		case IfcBooleanOperator::Union:
		//			return shape1->Union(shape2);	
		//		case IfcBooleanOperator::Intersection:
		//			return shape1->Intersection(shape2);
		//		case IfcBooleanOperator::Difference:
		//			return shape1->Cut(shape2);

		//		default:
		//			throw(gcnew InvalidOperationException("XbimGeometryModel. Build(BooleanClippingResult) Unsupported Operation"));
		//		}
		//	}
		//	catch(XbimGeometryException^ xbimE)
		//	{
		//		Logger->WarnFormat("Error performing boolean operation for entity #{0}={1}\n{2}\nA simplified version has been used",repItem->EntityLabel,repItem->GetType()->Name,xbimE->Message);
		//		return shape1;
		//	}
		//}


		/*
		Create a model geometry for a given shape
		*/
		IXbimGeometryModel^ XbimGeometryModel::CreateFrom(IfcRepresentation^ rep, Dictionary<IfcRepresentation^, IXbimGeometryModel^>^ maps, bool forceSolid, XbimLOD lod, bool occOut)
		{

			if(rep->Items->Count == 0) //we have nothing to do
				return nullptr;
			else if (rep->Items->Count == 1) //we have a single shape geometry
			{
				IfcRepresentationItem^ repItem = rep->Items->First;
				IXbimGeometryModel^ geom = CreateFrom(repItem,maps, forceSolid,lod,occOut);
				geom->RepresentationLabel = repItem->EntityLabel;
				IfcSurfaceStyle^ surfaceStyle = IfcRepresentationItemExtensions::SurfaceStyle(repItem);
				if(surfaceStyle!=nullptr) geom->SurfaceStyleLabel=Math::Abs(surfaceStyle->EntityLabel);
				return geom;
			}
			else // we have a compound shape
			{
				XbimGeometryModelCollection^ gms = gcnew XbimGeometryModelCollection();
				bool first = true;
				for each (IfcRepresentationItem^ repItem in rep->Items)
				{
					
					IXbimGeometryModel^ geom = CreateFrom(repItem,maps,false,lod,occOut); // we will make a solid when we have all the bits if necessary
					geom->RepresentationLabel = repItem->EntityLabel;
				    IfcSurfaceStyle^ surfaceStyle = IfcRepresentationItemExtensions::SurfaceStyle(repItem);
					if(surfaceStyle!=nullptr) geom->SurfaceStyleLabel=Math::Abs(surfaceStyle->EntityLabel);else  geom->SurfaceStyleLabel = 0;
					if(first)
					{
						first = false;
						gms->SurfaceStyleLabel=geom->SurfaceStyleLabel; //set collection same as first one for bounding boxes
					}
					if(!(dynamic_cast<XbimSolid^>(geom) && (*(geom->Handle)).IsNull())) 
					{
						gms->Add(geom); //don't add solids that are empty
#ifdef _DEBUG
						if(occOut)
						{
							char fname[512];
							sprintf(fname, "#%d",repItem->EntityLabel);
							BRepTools::Write(*(geom->Handle),fname );
						}
#endif
					}

				}
				return gms;
			}
		}

		IXbimGeometryModel^ XbimGeometryModel::CreateFrom(IfcRepresentation^ rep, bool forceSolid, XbimLOD lod, bool occOut)
		{
			return CreateFrom(rep, gcnew Dictionary<IfcRepresentation^, IXbimGeometryModel^>(), forceSolid,lod, occOut);
		}

		IXbimGeometryModel^ XbimGeometryModel::CreateFrom(IfcRepresentationItem^ repItem, bool forceSolid, XbimLOD lod, bool occOut)
		{
			return CreateFrom(repItem, gcnew Dictionary<IfcRepresentation^, IXbimGeometryModel^>(), forceSolid,lod, occOut);
		}

		IXbimGeometryModel^ XbimGeometryModel::CreateFrom(IfcRepresentationItem^ repItem, Dictionary<IfcRepresentation^, IXbimGeometryModel^>^ maps, bool forceSolid,XbimLOD lod, bool occOut)
		{
			if(!forceSolid && dynamic_cast<IfcFacetedBrep^>(repItem))
			{
				IXbimGeometryModel^ geom = gcnew XbimFacetedShell(((IfcFacetedBrep^)repItem)->Outer);
				geom->RepresentationLabel = repItem->EntityLabel;
				IfcSurfaceStyle^ surfaceStyle = IfcRepresentationItemExtensions::SurfaceStyle(repItem);
				if(surfaceStyle!=nullptr) geom->SurfaceStyleLabel=Math::Abs(surfaceStyle->EntityLabel);
				return geom;
			}
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
			else if(dynamic_cast<IfcBooleanResult^>(repItem)) 
				return gcnew XbimSolid((IfcBooleanResult^)repItem);
			else if(dynamic_cast<IfcMappedItem^>(repItem))
			{
				IfcMappedItem^ map = (IfcMappedItem^) repItem;
				IfcRepresentationMap^ repMap = map->MappingSource;
				IXbimGeometryModel^ mg;
				if(!maps->TryGetValue(repMap->MappedRepresentation, mg)) //look it up
				{
					mg =  CreateFrom(repMap->MappedRepresentation,maps, forceSolid,lod, occOut); //make the first one
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
				return nullptr; //this s not a solid object
				//IfcGeometricSet^ gset = (IfcGeometricSet^) repItem;
				//Logger->Warn(String::Format("Support for IfcGeometricSet #{0} has not been implemented", Math::Abs(gset->EntityLabel)));
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
				IXbimGeometryModel^ geom = gcnew XbimMap(item,origin,transform);
				geom->RepresentationLabel = item->RepresentationLabel;
				geom->SurfaceStyleLabel=item->SurfaceStyleLabel;
				return geom;
			}
			else if(dynamic_cast<XbimGeometryModelCollection^>(item))
			{
				XbimGeometryModelCollection^ mapColl = gcnew XbimGeometryModelCollection();
				mapColl->SurfaceStyleLabel=item->SurfaceStyleLabel;
				XbimGeometryModelCollection^ toMap = (XbimGeometryModelCollection^) item;
				for each(IXbimGeometryModel^ model in toMap)
					mapColl->Add(CreateMap(model,origin,transform, maps, forceSolid));
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
				{
					IXbimGeometryModel^ geom = gcnew XbimFacetedShell((IfcClosedShell^)(repItem->FbsmFaces->First));
					geom->RepresentationLabel = repItem->EntityLabel;
					IfcSurfaceStyle^ surfaceStyle = IfcRepresentationItemExtensions::SurfaceStyle(repItem);
					if(surfaceStyle!=nullptr) geom->SurfaceStyleLabel=Math::Abs(surfaceStyle->EntityLabel);
					return geom;
				}

			}

			else if(repItem->FbsmFaces->Count == 1) //its an open shell
			{
				if(forceSolid)
				{
					XbimSolid^ solid = gcnew XbimSolid(repItem->FbsmFaces->First);
					return solid;
				}
				else
				{
					IXbimGeometryModel^ geom = gcnew XbimFacetedShell(repItem->FbsmFaces->First);
					geom->RepresentationLabel = repItem->EntityLabel;
					IfcSurfaceStyle^ surfaceStyle = IfcRepresentationItemExtensions::SurfaceStyle(repItem);
					if(surfaceStyle!=nullptr) geom->SurfaceStyleLabel=Math::Abs(surfaceStyle->EntityLabel);
					return geom;
				}
					
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
							return  shell;
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
				return solid;
			}
			else if(repItem->SbsmBoundary->Count == 1 )
			{
				XbimFacetedShell^ shell =  gcnew XbimFacetedShell(repItem->SbsmBoundary->First);
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
					return gms;
				}
			}

		}

		IXbimGeometryModel^ XbimGeometryModel::Fix(IXbimGeometryModel^ shape)
		{


			ShapeUpgrade_ShellSewing ss;
			TopoDS_Shape res = ss.ApplySewing(*(shape->Handle), BRepLib::Precision()*10);
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

		long OpenCascadeShapeStreamerFeed(const TopoDS_Shape & shape, XbimTriangularMeshStreamer* tms)
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
						// note the negative values of the normals for reversed faces.
						// tms->info('R');

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
						// tms->info('N');
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

		void OpenGLShapeStreamerFeed(const TopoDS_Shape & shape, XbimTriangularMeshStreamer* tms)
		{
			GLUtesselator *ActiveTss = gluNewTess();

			gluTessCallback(ActiveTss, GLU_TESS_BEGIN_DATA,  (void (CALLBACK *)()) XMS_BeginTessellate);
			gluTessCallback(ActiveTss, GLU_TESS_END_DATA,  (void (CALLBACK *)()) XMS_EndTessellate);
			gluTessCallback(ActiveTss, GLU_TESS_ERROR,    (void (CALLBACK *)()) XMS_TessellateError);
			gluTessCallback(ActiveTss, GLU_TESS_VERTEX_DATA,  (void (CALLBACK *)()) XMS_AddVertexIndex);

			GLdouble glPt3D[3];
			// TesselateStream vertexData(pStream, points, faceCount, streamSize);
			for (TopExp_Explorer faceEx(shape,TopAbs_FACE) ; faceEx.More(); faceEx.Next()) 
			{
				tms->BeginFace(-1);
				const TopoDS_Face& face = TopoDS::Face(faceEx.Current());
				gp_Dir normal = GetNormal(face);
				tms->SetNormal(
					(float)normal.X(), 
					(float)normal.Y(), 
					(float)normal.Z()
					);
				// vertexData.BeginFace(normal);
				// gluTessBeginPolygon(tess, &vertexData);
				gluTessBeginPolygon(ActiveTss, tms);

				// go over each wire
				for (TopExp_Explorer wireEx(face,TopAbs_WIRE) ; wireEx.More(); wireEx.Next()) 
				{
					gluTessBeginContour(ActiveTss);
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
						void * pIndex = (void *)tms->WritePoint((float)p.X(), (float)p.Y(), (float)p.Z());
						gluTessVertex(ActiveTss, glPt3D, pIndex); 
					}
					gluTessEndContour(ActiveTss);
				}
				gluTessEndPolygon(ActiveTss);
				tms->EndFace();
			}
			gluDeleteTess(ActiveTss);
		}

#pragma managed


		List<XbimTriangulatedModel^>^XbimGeometryModel::Mesh(IXbimGeometryModel^ shape, bool withNormals, double deflection, Matrix3D transform )
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
				try
				{
					XbimTriangularMeshStreamer value(shape->RepresentationLabel, shape->SurfaceStyleLabel);
					XbimTriangularMeshStreamer* m = &value;
					//decide which meshing algorithm to use, Opencascade is slow but necessary to resolve curved edges
					if (hasCurvedEdges) 
					{
						// BRepMesh_IncrementalMesh calls BRepMesh_FastDiscret to create the mesh geometry.
						// todo: Bonghi: Question: is this ok to use the shape instead of transformedShape? I assume the transformed shape points to the shape.
						BRepMesh_IncrementalMesh incrementalMesh(*(shape->Handle), deflection); 
						OpenCascadeShapeStreamerFeed(transformedShape, m);
					}
					else
						OpenGLShapeStreamerFeed(transformedShape, m);
					unsigned int uiCalcSize = m->StreamSize();

					IntPtr BonghiUnManMem = Marshal::AllocHGlobal(uiCalcSize);
					unsigned char* BonghiUnManMemBuf = (unsigned char*)BonghiUnManMem.ToPointer();
					unsigned int controlSize = m->StreamTo(BonghiUnManMemBuf);

					if (uiCalcSize != controlSize)
					{
						int iError = 0;
						iError++;
					}

					array<unsigned char>^ BmanagedArray = gcnew array<unsigned char>(uiCalcSize);
					Marshal::Copy(BonghiUnManMem, BmanagedArray, 0, uiCalcSize);
					Marshal::FreeHGlobal(BonghiUnManMem);
					List<XbimTriangulatedModel^>^list = gcnew List<XbimTriangulatedModel^>();
					list->Add(gcnew XbimTriangulatedModel(BmanagedArray, shape->RepresentationLabel, shape->SurfaceStyleLabel) );
					return list;
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
			catch(...)
			{
				System::Diagnostics::Debug::WriteLine("Failed to Triangulate shape");
				return gcnew List<XbimTriangulatedModel^>();
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
