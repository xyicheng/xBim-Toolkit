#pragma once
#include "XbimGeometryModel.h"
#include "XbimFaceEnumerator.h"
#include "XbimShell.h"
#include "IXbimMeshGeometry.h"
#include "XbimGeometryModel.h"
#include <TopoDS_Solid.hxx>
#include <BRepGProp.hxx>
#include <GProp_GProps.hxx> 
#include <BRep_Builder.hxx> 

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

			public ref class XbimSolid  : XbimGeometryModel,  IEnumerable<XbimFace^>
			{
			protected:
				TopoDS_Shape* nativeHandle;
				static ILogger^ Logger = LoggerFactory::GetLogger();			
			private:
				bool _hasCurvedEdges;

			public:
				XbimSolid(){};
				XbimSolid(const TopoDS_Solid&  solid);
				XbimSolid(const TopoDS_Solid&  solid, bool hasCurves);
				XbimSolid(const TopoDS_Shell&  shell);
				XbimSolid(const TopoDS_Shell&  shell, bool hasCurves);
				XbimSolid(const TopoDS_Shape&  shape);
				XbimSolid(const TopoDS_Shape&  shape, bool hasCurves);
				XbimSolid(XbimGeometryModel^ solid, XbimMatrix3D transform);
				XbimSolid(XbimGeometryModel^ solid, bool hasCurves);
				virtual property XbimLocation ^ Location 
				{
					XbimLocation ^ get() override
					{
						return gcnew XbimLocation(nativeHandle->Location());
					}
					void set(XbimLocation ^ location) override
					{
						nativeHandle->Location(*(location->Handle));
					}
				};

				virtual property bool HasCurvedEdges
				{
					virtual bool get() override
					{
						return _hasCurvedEdges;
					}
				};

				XbimSolid(XbimSolid^ solid, IfcAxis2Placement^ origin, IfcCartesianTransformationOperator^ transform, bool hasCurves);
				XbimSolid(IfcExtrudedAreaSolid^ repItem);

				XbimSolid(IfcRevolvedAreaSolid^ repItem);

				XbimSolid(IfcFacetedBrep^ repItem);

				XbimSolid(IfcHalfSpaceSolid^ repItem);

				XbimSolid(IfcSolidModel^ repItem);

				XbimSolid(IfcClosedShell^ repItem);

				XbimSolid(IfcConnectedFaceSet^ repItem);
				XbimSolid(IfcBooleanResult^ repItem);
				XbimSolid(IfcCsgPrimitive3D^ repItem);
				XbimSolid(IfcVertexPoint^ pt);
				XbimSolid(IfcEdge^ edge);
				void Print();
				~XbimSolid()
				{
					InstanceCleanup();
				};

				!XbimSolid()
				{
					InstanceCleanup();
				};
				void InstanceCleanup()
				{   
					IntPtr temp = System::Threading::Interlocked::Exchange(IntPtr(nativeHandle), IntPtr(0));
					if(temp!=IntPtr(0))
					{
						if (nativeHandle)
						{
							delete nativeHandle;
							nativeHandle=0;
							System::GC::SuppressFinalize(this);
						}
					}
				};

				virtual property TopoDS_Shape* Handle
				{
					TopoDS_Shape* get() override
					{return nativeHandle;};			
				};

				virtual property double Volume
				{
					double get() override
					{
						if(nativeHandle!=nullptr)
						{
							GProp_GProps System;
							BRepGProp::VolumeProperties(*nativeHandle, System);
							return System.Mass();
						}
						else
							return 0;
					}
				};

				virtual property XbimMatrix3D Transform
				{
					XbimMatrix3D get() override
					{
						return XbimMatrix3D::Identity;
					}
				};
				/*Interfaces*/


				virtual XbimGeometryModel^ Cut(XbimGeometryModel^ shape) override;
				virtual XbimGeometryModel^ Union(XbimGeometryModel^ shape) override;
				virtual XbimGeometryModel^ Intersection(XbimGeometryModel^ shape) override;
				// IEnumerable<IIfcFace^> Members
				virtual property IEnumerable<XbimFace^>^ Faces
				{
					IEnumerable<XbimFace^>^ get();
				};


				virtual IEnumerator<XbimFace^>^ GetEnumerator()
				{

					return gcnew XbimFaceEnumerator(*nativeHandle);
				};
				virtual System::Collections::IEnumerator^ GetEnumerator2() sealed = System::Collections::IEnumerable::GetEnumerator
				{
					return gcnew XbimFaceEnumerator(*nativeHandle);
				};
			
				//solid operations

				virtual XbimGeometryModel^ CopyTo(IfcObjectPlacement^ placement) override;
				virtual void Move(TopLoc_Location location) override;
				///static builders 

				static TopoDS_Shape Build(IfcCsgSolid^ csgSolid, bool% hasCurves);
				static TopoDS_Shape Build(IfcManifoldSolidBrep^ manifold, bool% hasCurves);

				static TopoDS_Solid Build(IfcSweptDiskSolid^ swdSolid, bool% hasCurves);

				static TopoDS_Solid Build(IfcSweptAreaSolid^ sweptAreaSolid, bool% hasCurves);
				static TopoDS_Solid Build(IfcExtrudedAreaSolid^ repItem, bool% hasCurves);
				static TopoDS_Solid Build(IfcRevolvedAreaSolid^ repItem, bool% hasCurves);
				static TopoDS_Solid Build(IfcSurfaceCurveSweptAreaSolid^ repItem, bool% hasCurves);
				static TopoDS_Shape Build(IfcFacetedBrep^ repItem, bool% hasCurves);
				static TopoDS_Solid Build(IfcBoxedHalfSpace^ bhs, bool% hasCurves);
				static TopoDS_Shape Build(IfcClosedShell^ cShell, bool% hasCurves);
				static TopoDS_Shape Build(IfcConnectedFaceSet^ cFaces, bool% hasCurves);
				static TopoDS_Solid Build(IfcHalfSpaceSolid^ repItem, bool% hasCurves);
				static TopoDS_Solid Build(IfcPolygonalBoundedHalfSpace^ pbhs, bool% hasCurves);
				static TopoDS_Shape Build(IfcBooleanResult^ repItem, bool% hasCurves);
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
