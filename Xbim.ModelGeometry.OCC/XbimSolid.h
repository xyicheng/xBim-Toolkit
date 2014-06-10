#pragma once
#include "XbimGeometryModel.h"
#include "XbimFaceEnumerator.h"
#include "XbimShell.h"
#include "IXbimMeshGeometry.h"
#include "XbimGeometryModel.h"
#include <TopoDS_Solid.hxx>

using namespace System;
using namespace Xbim::XbimExtensions::Interfaces;
using namespace System::Collections::Generic;

using namespace Xbim::XbimExtensions::SelectTypes;
using namespace Xbim::Ifc2x3::GeometricModelResource;
using namespace Xbim::Ifc2x3::GeometricConstraintResource;
using namespace Xbim::Common::Logging;

namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{

			public ref class XbimSolid  : XbimGeometryModel
			{
			protected:			
						
			private:

			public:
				XbimSolid(){};
				XbimSolid(const TopoDS_Shape&  shape, bool hasCurves,int representationLabel, int surfaceStyleLabel );
				XbimSolid(XbimGeometryModel^ solid, XbimMatrix3D transform);
				XbimSolid(XbimGeometryModel^ solid, bool hasCurves);
				XbimSolid(XbimSolid^ solid, IfcAxis2Placement^ origin, IfcCartesianTransformationOperator^ transform, bool hasCurves);
				XbimSolid(IfcExtrudedAreaSolid^ repItem);
				XbimSolid(IfcRevolvedAreaSolid^ repItem);
				XbimSolid(IfcHalfSpaceSolid^ repItem);
				XbimSolid(IfcSolidModel^ repItem);				
				XbimSolid(IfcCsgPrimitive3D^ repItem);
				XbimSolid(IfcVertexPoint^ pt);
				XbimSolid(IfcEdge^ edge);
				~XbimSolid()
				{
					InstanceCleanup();
				};

				!XbimSolid()
				{
					InstanceCleanup();
				};
				

				//solid operations
				virtual XbimGeometryModel^ CopyTo(IfcAxis2Placement^ placement) override;
				virtual void ToSolid(double precision, double maxPrecision) override {}; //nothing to do
				virtual IXbimGeometryModelGroup^ ToPolyHedronCollection(double deflection, double precision,double precisionMax, unsigned int rounding) override;

				///static builders 
				
				static TopoDS_Solid Build(IfcSweptDiskSolid^ swdSolid, bool% hasCurves);
				static TopoDS_Solid Build(IfcSweptAreaSolid^ sweptAreaSolid, bool% hasCurves);
				static TopoDS_Solid Build(IfcExtrudedAreaSolid^ repItem, bool% hasCurves);
				static TopoDS_Solid Build(IfcRevolvedAreaSolid^ repItem, bool% hasCurves);
				static TopoDS_Solid Build(IfcSurfaceCurveSweptAreaSolid^ repItem, bool% hasCurves);
				static TopoDS_Solid Build(IfcBoxedHalfSpace^ bhs, bool% hasCurves);
				static TopoDS_Solid Build(IfcHalfSpaceSolid^ repItem, bool% hasCurves);
				static TopoDS_Solid Build(IfcPolygonalBoundedHalfSpace^ pbhs, bool% hasCurves);
			private:
				static TopoDS_Shell Build(const TopoDS_Wire & wire, IfcDirection^ dir, double depth, bool% hasCurves);
				static TopoDS_Solid Build(const TopoDS_Face & face, IfcDirection^ dir, double depth, bool% hasCurves);
				static TopoDS_Solid Build(const TopoDS_Wire & wire, gp_Dir dir, bool% hasCurves);
				static TopoDS_Solid Build(const TopoDS_Face & face, IfcAxis1Placement^ revolaxis, double angle, bool% hasCurves);
				static TopoDS_Solid MakeHalfSpace(IfcHalfSpaceSolid^ hs, bool% hasCurves, bool shift);
				//XbimGeometryModel interface

			};
		}
	}
}
