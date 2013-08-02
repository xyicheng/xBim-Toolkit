#include "StdAfx.h"
#include "XbimGeometryEngine.h"
#include "XbimFeaturedShape.h"
#include "XbimShell.h"
#include "XbimGeometryModelCollection.h"
#include "XbimFacetedShell.h"
#include "XbimMap.h"
#include <BRepBuilderAPI.hxx>
#include "XbimGeomPrim.h"
using namespace Xbim::Common::Logging;
using namespace Xbim::ModelGeometry::OCC;
using namespace System::Linq;
using namespace Xbim::Ifc2x3::ProductExtension;
using namespace Xbim::Ifc2x3::SharedComponentElements;
using namespace Xbim::Ifc2x3::PresentationAppearanceResource;
using namespace Xbim::IO;
namespace Xbim
{
	namespace ModelGeometry
	{

		/* Creates a 3D Model geometry for the product based upon the first "Body" ShapeRepresentation that can be found in  
		Products.ProductRepresentation that is within the specified GeometricRepresentationContext, if the Representation 
		context is null the first "Body" ShapeRepresentation is used. Returns null if their is no valid geometric definition
		*/
		IXbimGeometryModel^ XbimGeometryEngine::GetGeometry3D(IfcProduct^ product, ConcurrentDictionary<int, Object^>^ maps)
		{
			return CreateFrom(product,maps,false,XbimLOD::LOD_Unspecified,false);
		}

		IXbimGeometryModel^ XbimGeometryEngine::GetGeometry3D(IfcSolidModel^ solid, ConcurrentDictionary<int, Object^>^ maps)
		{
			return CreateFrom(solid,maps,false,XbimLOD::LOD_Unspecified,false);
		}
		IXbimGeometryModel^ XbimGeometryEngine::GetGeometry3D(IfcProduct^ product)
		{
			return CreateFrom(product,nullptr,false,XbimLOD::LOD_Unspecified,false);
		}

		IXbimGeometryModel^ XbimGeometryEngine::GetGeometry3D(IfcSolidModel^ solid)
		{
			return CreateFrom(solid,nullptr,false,XbimLOD::LOD_Unspecified,false);
		}

		XbimGeometryModel^ XbimGeometryEngine::CreateFrom(IfcProduct^ product, IfcGeometricRepresentationContext^ repContext, ConcurrentDictionary<int, Object^>^ maps, bool forceSolid, XbimLOD lod, bool occOut)
		{
			try
			{
				if(product->Representation == nullptr ||  product->Representation->Representations == nullptr 
					|| dynamic_cast<IfcTopologyRepresentation^>(product->Representation)) 
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
								BRepBuilderAPI::Precision((const Standard_Real)((IfcGeometricRepresentationContext^)(shape->ContextOfItems))->DefaultPrecision);

							//srl optimisation openings and projectionss cannot have openings or projection so don't check for them
							if(CutOpenings(product, lod) && !dynamic_cast<IfcFeatureElement^>(product ))
							{
								IfcElement^ element = (IfcElement^) product;
								List<XbimGeometryModel^>^ projectionSolids = gcnew List<XbimGeometryModel^>();
								List<XbimGeometryModel^>^ openingSolids = gcnew List<XbimGeometryModel^>();
								for each(IfcRelProjectsElement^ rel in element->HasProjections)
								{
									IfcFeatureElementAddition^ fe = rel->RelatedFeatureElement;
									if(fe->Representation!=nullptr)
									{
										IfcFeatureElementAddition^ fe = rel->RelatedFeatureElement;
										if(fe->Representation!=nullptr)
										{
											XbimGeometryModel^ im = CreateFrom(fe,repContext, maps, true, lod, occOut);
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
										XbimGeometryModel^ im = CreateFrom(fe, repContext, maps, true,lod, occOut);
										//BRepTools::Write(*(im->Handle),"f1" );
										if(im!=nullptr && !im->Handle->IsNull())
										{	
											im = im->CopyTo(fe->ObjectPlacement);
											//BRepTools::Write(*(im->Handle),"f2" );
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
													im->Move(prodLoc);
												}
											}
											//BRepTools::Write(*(im->Handle),"f3" );
											openingSolids->Add(im);
										}
									}

								}
								if(Enumerable::Any(openingSolids) || Enumerable::Any(projectionSolids))
								{

									XbimGeometryModel^ baseShape = CreateFrom(shape, maps, true,lod, occOut);	
									//BRepTools::Write(*(baseShape->Handle),"f4" );

									XbimGeometryModel^ fshape = gcnew XbimFeaturedShape(product, baseShape, openingSolids, projectionSolids);
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

									XbimGeometryModel^ fshape = CreateFrom(shape, maps, forceSolid,lod, occOut);
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

								XbimGeometryModel^ fshape = CreateFrom(shape, maps, forceSolid,lod, occOut);
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

		XbimGeometryModel^ XbimGeometryEngine::CreateFrom(IfcProduct^ product, ConcurrentDictionary<int, Object^>^ maps, bool forceSolid, XbimLOD lod, bool occOut)
		{
			return CreateFrom(product, nullptr, maps, forceSolid, lod, occOut);
		}

		XbimGeometryModel^ XbimGeometryEngine::CreateFrom(IfcProduct^ product, bool forceSolid, XbimLOD lod, bool occOut)
		{
			// HACK: Ideally we shouldn't need this try-catch handler. This just allows us to log the fault, and raise a managed exception, before the application terminates.
			// Upstream callers should ideally terminate the application ASAP.
			__try
			{
				return CreateFrom(product, nullptr, gcnew ConcurrentDictionary<int, Object^>(), forceSolid,lod,occOut);
			}
			__except(GetExceptionCode() == EXCEPTION_ACCESS_VIOLATION)
			{
				Logger->Fatal("Access Violation in geometry engine. Thireturns may leave the application in an inconsistent state!");
				throw gcnew AccessViolationException(
					"A memory access violation occurred in the geometry engine. The application and geometry may be in an inconsistent state and the process should be terminated.");
			}
		}

		/*
		Create a model geometry for a given shape
		*/
		XbimGeometryModel^ XbimGeometryEngine::CreateFrom(IfcRepresentation^ rep, ConcurrentDictionary<int, Object^>^ maps, bool forceSolid, XbimLOD lod, bool occOut)
		{

			if(rep->Items->Count == 0) //we have nothing to do
				return nullptr;
			else if (rep->Items->Count == 1) //we have a single shape geometry
			{
				IfcRepresentationItem^ repItem = rep->Items->First;
				XbimGeometryModel^ geom = CreateFrom(repItem,maps, forceSolid,lod,occOut);
				if(geom!=nullptr)
				{
					if(geom->RepresentationLabel==0) geom->RepresentationLabel = repItem->EntityLabel; //only set if we haven't further down
					IfcSurfaceStyle^ surfaceStyle = IfcRepresentationItemExtensions::SurfaceStyle(repItem);
					if(surfaceStyle!=nullptr) geom->SurfaceStyleLabel=Math::Abs(surfaceStyle->EntityLabel);
				}
				return geom;
			}
			else // we have a compound shape
			{
				XbimGeometryModelCollection^ gms = gcnew XbimGeometryModelCollection(false);
				gms->RepresentationLabel = rep->EntityLabel;
				bool first = true;
				for each (IfcRepresentationItem^ repItem in rep->Items)
				{

					XbimGeometryModel^ geom = CreateFrom(repItem,maps,false,lod,occOut); // we will make a solid when we have all the bits if necessary
					if(geom!=nullptr)
					{
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

				}
				return gms;
			}
		}

		XbimGeometryModel^ XbimGeometryEngine::CreateFrom(IfcRepresentation^ rep, bool forceSolid, XbimLOD lod, bool occOut)
		{
			return CreateFrom(rep, gcnew ConcurrentDictionary<int, Object^>(), forceSolid,lod, occOut);
		}

		XbimGeometryModel^ XbimGeometryEngine::CreateFrom(IfcRepresentationItem^ repItem, bool forceSolid, XbimLOD lod, bool occOut)
		{
			return CreateFrom(repItem, gcnew ConcurrentDictionary<int, Object^>(), forceSolid,lod, occOut);
		}

		XbimGeometryModel^ XbimGeometryEngine::CreateFrom(IfcRepresentationItem^ repItem, ConcurrentDictionary<int, Object^>^ maps, bool forceSolid,XbimLOD lod, bool occOut)
		{
			
			if(!forceSolid && dynamic_cast<IfcFacetedBrep^>(repItem))
			{
				XbimGeometryModel^ geom = gcnew XbimFacetedShell(((IfcFacetedBrep^)repItem)->Outer);
				geom->RepresentationLabel = repItem->EntityLabel;
				IfcSurfaceStyle^ surfaceStyle = IfcRepresentationItemExtensions::SurfaceStyle(repItem);
				if(surfaceStyle!=nullptr) geom->SurfaceStyleLabel=Math::Abs(surfaceStyle->EntityLabel);
				return geom;
			}
			else if(dynamic_cast<IfcSolidModel^>(repItem))
			{
				//look up if we have already created it
				//XbimGeometryModel^ solidModel;
				//if(!_solids->TryGetValue(Math::Abs(repItem->EntityLabel),solidModel))
				//{
				//	solidModel = gcnew XbimSolid((IfcSolidModel^)repItem);
				//	_solids->TryAdd(Math::Abs(repItem->EntityLabel),solidModel);
				//}
				//return solidModel;
				return gcnew XbimSolid((IfcSolidModel^)repItem);
			}
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
				XbimGeometryModel^ mg;
				Object ^ lookup;
				if(!maps->TryGetValue(Math::Abs(repMap->MappedRepresentation->EntityLabel), lookup)) //look it up
				{
					mg =  CreateFrom(repMap->MappedRepresentation,maps, forceSolid,lod, occOut); //make the first one
					if(mg!=nullptr)
					{
						mg->RepresentationLabel=repMap->MappedRepresentation->EntityLabel;
						IfcSurfaceStyle^ surfaceStyle = IfcRepresentationItemExtensions::SurfaceStyle(repItem);
						if(surfaceStyle!=nullptr) mg->SurfaceStyleLabel=Math::Abs(surfaceStyle->EntityLabel);
						maps->TryAdd(Math::Abs(repMap->MappedRepresentation->EntityLabel), mg);
					}
				}
				else
					mg= (XbimGeometryModel^)lookup;

				//need to transform all the geometries as below
				if(mg!=nullptr)
					return gcnew XbimMap(mg,repMap->MappingOrigin,map->MappingTarget, maps); 
				else
					return nullptr;

			} //the below items should build surfaces or topologies and need to be implemented
			else if(dynamic_cast<IfcCurveBoundedPlane^>(repItem))
			{
				return nullptr; //surface is not implmented yet
				//return gcnew XbimSolid((IfcVertexPoint^)repItem);
			}
			else if(dynamic_cast<IfcVertexPoint^>(repItem))
			{
				return nullptr; //topology is not implmented yet
				//return gcnew XbimSolid((IfcVertexPoint^)repItem);
			}
			else if(dynamic_cast<IfcEdge^>(repItem))
			{
				return nullptr; //topology is not implmented yet
				//return gcnew XbimSolid((IfcEdge^)repItem);
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

		XbimGeometryModel^ XbimGeometryEngine::Build(IfcFaceBasedSurfaceModel^ repItem, bool forceSolid)
		{

			if(repItem->FbsmFaces->Count == 0) return nullptr;
			else if(repItem->FbsmFaces->Count == 1 && dynamic_cast<IfcClosedShell^>(repItem->FbsmFaces->First))
			{
				//A Closed shell must be defined by IfcPolyLoop, therefore we can assume it comples as XbimFacetedShell
				if(forceSolid)
					return gcnew XbimSolid((IfcClosedShell^)(repItem->FbsmFaces->First));
				else
				{
					XbimGeometryModel^ geom = gcnew XbimFacetedShell((IfcClosedShell^)(repItem->FbsmFaces->First));
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
					XbimGeometryModel^ geom = gcnew XbimFacetedShell(repItem->FbsmFaces->First);
					geom->RepresentationLabel = repItem->EntityLabel;
					IfcSurfaceStyle^ surfaceStyle = IfcRepresentationItemExtensions::SurfaceStyle(repItem);
					if(surfaceStyle!=nullptr) geom->SurfaceStyleLabel=Math::Abs(surfaceStyle->EntityLabel);
					return geom;
				}

			}

			else
			{
				XbimGeometryModelCollection^ gms = gcnew XbimGeometryModelCollection(false);	
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



		XbimGeometryModel^ XbimGeometryEngine::Build(IfcShellBasedSurfaceModel^ repItem, bool forceSolid)
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
				XbimGeometryModelCollection^ gms = gcnew XbimGeometryModelCollection(false);		
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

		bool XbimGeometryEngine::CutOpenings(IfcProduct^ product, XbimLOD lod)
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



	}

}