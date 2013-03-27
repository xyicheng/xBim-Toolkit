#pragma once
#include <TopoDS_Face.hxx>
#include "XbimFaceBoundEnumerator.h"
#include <GeomLProp_SLProps.hxx>

using namespace Xbim::XbimExtensions::Interfaces;
using namespace System;
using namespace System::Collections::Generic;
using namespace Xbim::Ifc2x3::ProfileResource;
using namespace Xbim::Ifc2x3::GeometryResource;
using namespace Xbim::Ifc2x3::TopologyResource;

namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
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

				//calculates the topological normal of the face
				static gp_Vec TopoNormal(const TopoDS_Face & face);
				property XbimFaceOuterBound^ OuterBound
				{
					XbimFaceOuterBound^ get();
				}

				// IEnumerable Members

				virtual System::Collections::IEnumerator^ GetEnumerator2()  sealed = System::Collections::IEnumerable::GetEnumerator
				{
					return  gcnew XbimFaceBoundEnumerator(*nativeHandle);

				}


				// SRL: Builds a face from a IfcParameterizedProfileDef
				static TopoDS_Face Build(IfcParameterizedProfileDef ^ profile, bool% hasCurves);

				// AK: Builds a face from a IfcZShapeProfileDef
				static TopoDS_Face Build(IfcZShapeProfileDef ^ profile, bool% hasCurves);

				// AK: Builds a face from a IfcLShapeProfileDef
				static TopoDS_Face Build(IfcLShapeProfileDef ^ profile, bool% hasCurves);

				// AK: Builds a face from a IfcUShapeProfileDef
				static TopoDS_Face Build(IfcUShapeProfileDef ^ profile, bool% hasCurves);

				// AK: Builds a face from a IfcIShapeProfileDef
				static TopoDS_Face Build(IfcIShapeProfileDef ^ profile, bool% hasCurves);

				// SRL: Builds a face from a IfcTShapeProfileDef
				static TopoDS_Face Build(IfcTShapeProfileDef ^ profile, bool% hasCurves);

				// SRL: Builds a face from a IfcCShapeProfileDef
				static TopoDS_Face Build(IfcCShapeProfileDef ^ profile, bool% hasCurves);

				// SRL: Builds a face from a IfcCraneRailFShapeProfileDef
				static TopoDS_Face Build(IfcCraneRailFShapeProfileDef ^ profile, bool% hasCurves);

				// SRL: Builds a face from a IfcCraneRailAShapeProfileDef
				static TopoDS_Face Build(IfcCraneRailAShapeProfileDef ^ profile, bool% hasCurves);

				// SRL: Builds a face from a IfcEllipseProfileDef
				static TopoDS_Face Build(IfcEllipseProfileDef ^ profile, bool% hasCurves);
				//static builders
				//Builds a face from any supported ProfileDef
				static TopoDS_Face Build(IfcProfileDef ^ profile, bool% hasCurves);

				//Builds a face from a ArbitraryClosedProfileDef
				static TopoDS_Face Build(IfcArbitraryClosedProfileDef ^ profile, bool% hasCurves);

				//Builds a face from a IfcDerivedProfileDef
				static TopoDS_Face Build(IfcDerivedProfileDef ^ profile, bool% hasCurves);

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
}
