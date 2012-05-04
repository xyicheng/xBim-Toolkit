#pragma once

#include "XbimFaceBound.h"
#include "XbimEdgeLoop.h"
#include <TopoDS_Wire.hxx>
#include <TopoDS_Face.hxx>
#include <gp_Dir.hxx>

using namespace Xbim::Ifc::ProfileResource;
using namespace Xbim::Ifc::GeometryResource;
using namespace Xbim::Ifc::TopologyResource;
using namespace Xbim::XbimExtensions::Interfaces;
using namespace System::Collections::Generic;
using namespace Xbim::Ifc::MeasureResource;

namespace Xbim
{
	namespace ModelGeometry
	{
		public ref class XbimFaceBound
		{

		private:
			TopoDS_Wire * pWire;
			TopoDS_Face * pFace;
		public:
			XbimFaceBound(const TopoDS_Wire & wire, const TopoDS_Face & face);
			~XbimFaceBound()
			{
				InstanceCleanup();
			}

			!XbimFaceBound()
			{
				InstanceCleanup();
			}
			void InstanceCleanup()
			{   
				int temp = System::Threading::Interlocked::Exchange((int)(void*)pWire, 0);
				if(temp!=0)
				{
					if (pWire)
					{
						delete pWire;
						pWire=0;
						delete pFace;
						pFace=0;
						System::GC::SuppressFinalize(this);
					}
				}

			}
			/*Interface*/
			virtual property XbimEdgeLoop^ Bound
			{
				XbimEdgeLoop^ get();
			}
			virtual property bool Orientation
			{
				bool get();
			}

			void Print();
			//static methods
			//Builds a wire from a ArbitraryClosedProfileDef
			static TopoDS_Wire Build(IfcArbitraryClosedProfileDef ^ profile, bool% hasCurves);

			//Builds a wire from a RectangleProfileDef
			static TopoDS_Wire Build(IfcRectangleProfileDef ^ profile, bool% hasCurves);

			//Builds a wire from a CircleProfileDef
			static TopoDS_Wire Build(IfcCircleProfileDef ^ circProfile, bool% hasCurves);

			//Builds a wire from a composite curve
			static TopoDS_Wire Build(IfcCompositeCurve ^ cCurve, bool% hasCurves);

			//Builds a wire segment from a  curve
			static TopoDS_Wire Build(IfcCurve ^ curve, bool% hasCurves);

			//Builds a wire  from a  BoundedCurve
			static TopoDS_Wire Build(IfcBoundedCurve ^ bCurve, bool% hasCurves);

			//Builds a wire from a PolyLoop
			static TopoDS_Wire Build(IfcPolyLoop ^ loop, bool% hasCurves);

			//Builds a wire segment from a Polyline
			static TopoDS_Wire Build(IfcPolyline ^ pline, bool% hasCurves);

			//Builds a wire segment from a TrimmedCurve
			static TopoDS_Wire Build(IfcTrimmedCurve ^ tCurve, bool% hasCurves);


		};

	}
}