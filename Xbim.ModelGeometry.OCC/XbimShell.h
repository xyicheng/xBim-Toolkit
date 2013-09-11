#pragma once
#include "XbimFaceEnumerator.h"
#include "XbimGeometryModel.h"
#include "XbimGeometryModel.h"
#include "XbimFace.h"
#include <TopoDS_Shell.hxx>
#include <BRepGProp.hxx>
#include <GProp_GProps.hxx> 

using namespace Xbim::Ifc2x3::TopologyResource;
using namespace Xbim::Ifc2x3::GeometryResource;
using namespace Xbim::XbimExtensions::SelectTypes;
using namespace Xbim::XbimExtensions::Interfaces;
using namespace System::Collections::Generic;
using namespace System::IO;
using namespace Xbim::Common::Logging;

namespace Xbim
{
	namespace ModelGeometry
	{
		namespace OCC
		{
		public ref class XbimShell : XbimGeometryModel,  IEnumerable<XbimFace^>
		{
		private:
			TopoDS_Shape * pShell;
			bool _hasCurvedEdges;
			static ILogger^ Logger = LoggerFactory::GetLogger();
			
		public:
			XbimShell(IfcConnectedFaceSet^ faceSet);
			XbimShell(IfcClosedShell^ shell);
			XbimShell(IfcOpenShell^ shell);
		
			XbimShell(const TopoDS_Shape & shell);
			XbimShell(const TopoDS_Shape & shell, bool hasCurves );
			XbimShell(XbimShell^ shell, IfcAxis2Placement^ origin, IfcCartesianTransformationOperator^ transform, bool hasCurves );

			~XbimShell()
			{
				InstanceCleanup();
			}

			!XbimShell()
			{
				InstanceCleanup();
			}
			void InstanceCleanup()
			{   
				int temp = System::Threading::Interlocked::Exchange((int)(void*)pShell, 0);
				if(temp!=0)
				{
					if (pShell)
					{
						delete pShell;
						pShell=0;
						System::GC::SuppressFinalize(this);
					}
				}
			}

			virtual property XbimLocation ^ Location 
			{
				XbimLocation ^ get() override 
				{
					return gcnew XbimLocation(pShell->Location());
				}
				void set(XbimLocation ^ location) override 
				{
					pShell->Location(*(location->Handle));;
				}
			};
			
			virtual property double Volume
			{
				double get() override 
				{
					if(pShell!=nullptr)
					{
						GProp_GProps System;
						BRepGProp::VolumeProperties(*pShell, System, Standard_True);
						return System.Mass();
					}
					else
						return 0;
				}
			}
			virtual property bool HasCurvedEdges
			{
				virtual bool get() override 
				{
					return _hasCurvedEdges;
				}
			}
			virtual property XbimMatrix3D Transform
			{
				XbimMatrix3D get() override
				{
					return XbimMatrix3D::Identity;
				}
			}
			
			

			virtual property TopoDS_Shape* Handle
			{
				TopoDS_Shape* get() override
				{return pShell;};			
			}
			/*Interfaces*/

			virtual XbimGeometryModel^ Cut(XbimGeometryModel^ shape) override;
			virtual XbimGeometryModel^ Union(XbimGeometryModel^ shape) override;
			virtual XbimGeometryModel^ Intersection(XbimGeometryModel^ shape) override;
			virtual XbimGeometryModel^ CopyTo(IfcObjectPlacement^ placement) override;
			virtual void Move(TopLoc_Location location) override;
			// IEnumerable<IIfcFace^> Members
			virtual property IEnumerable<XbimFace^>^ CfsFaces
			{
				IEnumerable<XbimFace^>^ get();
			}

			virtual IEnumerator<XbimFace^>^ GetEnumerator()
			{

				return gcnew XbimFaceEnumerator(*(pShell));
			}
			virtual System::Collections::IEnumerator^ GetEnumerator2() sealed = System::Collections::IEnumerable::GetEnumerator
			{
				return gcnew XbimFaceEnumerator(*(pShell));
			}

			//Builds a TopoDS_Shell from an ClosedShell
			static TopoDS_Shape Build(IfcClosedShell^ shell, bool% hasCurves);
			
			//Builds a TopoDS_Shell from an Openshell
			static TopoDS_Shape Build(IfcOpenShell^ shell, bool% hasCurves);

			//Builds a TopoDS_Shell from an ConnectedFaceSet
			static TopoDS_Shape Build(IfcConnectedFaceSet^ faceSet, bool% hasCurves);

		
		};
		}
	}
}