#pragma once
#include <TopoDS_Face.hxx>
#include "XbimFaceBoundEnumerator.h"

using namespace Xbim::XbimExtensions::Interfaces;
using namespace System;
using namespace System::Collections::Generic;
using namespace Xbim::Ifc::ProfileResource;
using namespace Xbim::Ifc::GeometryResource;
using namespace Xbim::Ifc::TopologyResource;

namespace Xbim
{
	namespace ModelGeometry
	{
		public ref class XbimFace :  IEnumerable<XbimFaceBound^>
		{
		private:
			TopoDS_Face * nativeHandle;
		public:
			XbimFace(const TopoDS_Face & face);
			~XbimFace()
			{
				InstanceCleanup();
			}

			!XbimFace()
			{
				InstanceCleanup();
			}
			void InstanceCleanup()
			{   
				int temp = System::Threading::Interlocked::Exchange((int)(void*)nativeHandle, 0);
				if(temp!=0)
				{
					if (nativeHandle)
					{
						delete nativeHandle;
						nativeHandle=0;
						System::GC::SuppressFinalize(this);
					}
				}
			}
			void Print();
			/*Interface*/
			virtual property System::Collections::Generic::IEnumerable<XbimFaceBound^>^ Bounds
			{
				System::Collections::Generic::IEnumerable<XbimFaceBound^>^ get();
			}


			// IEnumerable<IIfcFace^> Members

			virtual System::Collections::Generic::IEnumerator<XbimFaceBound^>^ GetEnumerator()
			{

				return gcnew XbimFaceBoundEnumerator(*nativeHandle);
			}

			property XbimFaceOuterBound^ OuterBound
			{
				XbimFaceOuterBound^ get();
			}

			// IEnumerable Members

			virtual System::Collections::IEnumerator^ GetEnumerator2()  sealed = System::Collections::IEnumerable::GetEnumerator
			{
				return  gcnew XbimFaceBoundEnumerator(*nativeHandle);

			}

			//static builders
			//Builds a face from any supported ProfileDef
			static TopoDS_Face Build(IfcProfileDef ^ profile, bool% hasCurves);

			//Builds a face from a ArbitraryClosedProfileDef
			static TopoDS_Face Build(IfcArbitraryClosedProfileDef ^ profile, bool% hasCurves);

			//Builds a face from a ArbitraryProfileDefWithVoids
			static TopoDS_Face Build(IfcArbitraryProfileDefWithVoids ^ profile, bool% hasCurves);

			//Builds a face from a composite curve
			static TopoDS_Face Build(IfcCompositeCurve ^ cCurve, bool% hasCurves);

			//Builds a face from a CircleProfileDef
			static TopoDS_Face Build(IfcCircleProfileDef ^ profile, bool% hasCurves);

			//Builds a face from a Polyline
			static TopoDS_Face Build(IfcPolyline ^ pline, bool% hasCurves);

			//Builds a face from a PolyLoop
			static TopoDS_Face Build(IfcPolyLoop ^ loop, bool% hasCurves);

			//Builds a face from a PolyLoop
			static TopoDS_Face Build(IfcPolyLoop ^ loop, bool sense, bool% hasCurves);

			//Builds a face from a RectangleProfileDef
			static TopoDS_Face Build(IfcRectangleProfileDef ^ profile, bool% hasCurves);

			//Builds a face from a Surface
			static TopoDS_Face Build(IfcSurface ^ surface, bool% hasCurves);

			//Builds a face from a Plane
			static TopoDS_Face Build(IfcPlane ^ plane, bool% hasCurves);
		};
	}
}