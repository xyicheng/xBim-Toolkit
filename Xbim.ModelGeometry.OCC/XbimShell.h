#pragma once
#include "XbimFaceEnumerator.h"
#include "XbimGeometryModel.h"
#include "XbimGeometryModel.h"
#include "XbimFace.h"
#include <TopoDS_Shell.hxx>
#include <TopTools_DataMapOfIntegerShape.hxx>
#include <TopTools_ListIteratorOfListOfShape.hxx>


using namespace Xbim::Ifc2x3::TopologyResource;
using namespace Xbim::Ifc2x3::GeometryResource;
using namespace Xbim::XbimExtensions::SelectTypes;
using namespace Xbim::XbimExtensions::Interfaces;
using namespace System::Collections::Generic;
using namespace System::IO;


namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
			public ref class XbimShell : XbimGeometryModel
			{
			private:
				bool isSolid;
				static TopoDS_Wire BuildBound(IfcFaceBound^ bound, TopTools_DataMapOfIntegerShape& vertexStore,TopTools_DataMapOfShapeListOfShape& edgeMap);
			
			public:
				
				XbimShell(const TopoDS_Shape & shell, bool hasCurves,int representationLabel, int surfaceStyleLabel );
				XbimShell(XbimShell^ shell, IfcAxis2Placement^ origin, IfcCartesianTransformationOperator^ transform, bool hasCurves );
				
				~XbimShell()
				{
					InstanceCleanup();
				}

				!XbimShell()
				{
					InstanceCleanup();
				}
				

				/*Interfaces*/
				virtual XbimGeometryModel^ CopyTo(IfcAxis2Placement^ placement) override;
				


				//Builds a TopoDS_Shell from an ConnectedFaceSet
				static TopoDS_Shape Build(IfcConnectedFaceSet^ faceSet, bool% hasCurves);

				virtual void ToSolid(double precision, double maxPrecision) override; 
				virtual IXbimGeometryModelGroup^ ToPolyHedronCollection(double deflection, double precision,double precisionMax) override;

			};
		}
	}
}