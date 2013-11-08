#include "StdAfx.h"
#include "XbimSolid.h"
#include "XbimShell.h"
#include "XbimCsg.h"
#include "XbimGeometryModelCollection.h"
#include "XbimFacetedShell.h"
#include "XbimGeomPrim.h"
#include <BRepAlgoAPI_Cut.hxx>
#include <BRepAlgoAPI_Common.hxx>
#include <BRepAlgoAPI_Fuse.hxx>
#include <BRepOffsetAPI_Sewing.hxx>
#include <BRepLib.hxx>
#include <ShapeUpgrade_ShellSewing.hxx>
#include <BRepAlgoAPI_Fuse.hxx>
#include <BRepTools.hxx>
#include <BRepMesh_IncrementalMesh.hxx>
#include <Poly_Triangulation.hxx>
#include <ShapeFix_Shape.hxx> 
#include <BRepCheck_Analyzer.hxx>
#include <ShapeFix_ShapeTolerance.hxx>
using namespace System::Linq;
using namespace Xbim::Common::Exceptions;

namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
			void XbimGeometryModelCollection::Init()
			{
				shapes = gcnew List<IXbimGeometryModel^>();
				_hasCurvedEdges = false;
			};

			XbimGeometryModelCollection::XbimGeometryModelCollection(const TopoDS_Shape&  shape , bool hasCurves,int representationLabel, int surfaceStyleLabel)
			{
				Init();
				Init(shape,hasCurves,representationLabel,surfaceStyleLabel);
			};

			XbimGeometryModelCollection::XbimGeometryModelCollection()
			{
				Init();
			};

			XbimGeometryModelCollection::XbimGeometryModelCollection(int representationLabel, int surfaceStyleLabel )
			{	
				Init();
				_representationLabel=representationLabel;
				_surfaceStyleLabel=surfaceStyleLabel;
			};

			XbimGeometryModelCollection::XbimGeometryModelCollection(bool hasCurvedEdges, int representationLabel, int surfaceStyleLabel)
			{
				Init();
				_representationLabel=representationLabel;
				_surfaceStyleLabel=surfaceStyleLabel;
				_hasCurvedEdges=hasCurvedEdges;
			}

			XbimGeometryModelCollection::XbimGeometryModelCollection(IfcRepresentationItem^ representationItem)
			{
				Init();
				_representationLabel=Math::Abs(representationItem->EntityLabel);
				IfcSurfaceStyle^ surfaceStyle = IfcRepresentationItemExtensions::SurfaceStyle(representationItem);
				if(surfaceStyle!=nullptr) _surfaceStyleLabel=Math::Abs(surfaceStyle->EntityLabel);
			};


			XbimGeometryModelCollection::XbimGeometryModelCollection(IfcRepresentation^ representation)
			{
				Init();
				_representationLabel=Math::Abs(representation->EntityLabel);
				//IfcSurfaceStyle^ surfaceStyle = IfcRepresentationItemExtensions::SurfaceStyle(representation);
				//if(surfaceStyle!=nullptr) _surfaceStyleLabel=Math::Abs(surfaceStyle->EntityLabel);
			};


			XbimTriangulatedModelCollection^ XbimGeometryModelCollection::Mesh(double deflection )
			{ 

				XbimTriangulatedModelCollection^ tm = gcnew XbimTriangulatedModelCollection();
				for each(XbimGeometryModel^ gm in shapes)
				{
					XbimTriangulatedModelCollection^ mm = gm->Mesh(deflection);
					if(mm!=nullptr)	tm->AddRange(mm);
				}
				return tm;
			}



			void XbimGeometryModelCollection::Move(TopLoc_Location location)
			{	

				for each(XbimGeometryModel^ shape in shapes)
					shape->Move(location);
				if (nativeHandle) //remove any cached compound data
				{
					delete nativeHandle;
					nativeHandle=0;
				}
			}

			XbimGeometryModel^ XbimGeometryModelCollection::CopyTo(IfcAxis2Placement^ placement)
			{
				XbimGeometryModelCollection^ newColl = gcnew XbimGeometryModelCollection(HasCurvedEdges,RepresentationLabel,SurfaceStyleLabel);
				for each(XbimGeometryModel^ shape in shapes)
				{
					newColl->Add(shape->CopyTo(placement));
				}
				return newColl;
			}

			void XbimGeometryModelCollection::ToSolid(double precision, double maxPrecision) 
			{				
				for each (XbimGeometryModel^ shape in this)
				{
				    shape->ToSolid(precision,maxPrecision);
				}
				
			}

			IXbimGeometryModelGroup^ XbimGeometryModelCollection::ToPolyHedronCollection(double deflection, double precision,double precisionMax)
			{
				XbimGeometryModelCollection^ polys = gcnew XbimGeometryModelCollection(this->_hasCurvedEdges,this->_representationLabel,this->_surfaceStyleLabel);
				for each(XbimGeometryModel^ shape in shapes)
				{
					polys->Add(shape->ToPolyHedron(deflection,  precision, precisionMax));
				}
				return polys;
			}
		}
	}
}