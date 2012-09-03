#pragma once
#include "XbimFaceEnumerator.h"
#include "IXbimGeometryModel.h"
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
		public ref class XbimShell : IXbimGeometryModel,  IEnumerable<XbimFace^>
		{
		private:
			TopoDS_Shape * pShell;
			bool _hasCurvedEdges;
			static ILogger^ Logger = LoggerFactory::GetLogger();
			Int64 _representationLabel;
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
				XbimLocation ^ get()
				{
					return gcnew XbimLocation(pShell->Location());
				}
				void set(XbimLocation ^ location)
				{
					pShell->Location(*(location->Handle));;
				}
			};
			
			virtual property double Volume
			{
				double get()
				{
					GProp_GProps System;
					BRepGProp::VolumeProperties(*pShell, System, Standard_True);
					return System.Mass();
				}
			}
			virtual property bool HasCurvedEdges
			{
				virtual bool get()
				{
					return _hasCurvedEdges;
				}
			}
			virtual XbimBoundingBox^ GetBoundingBox(bool precise)
			{
				return XbimGeometryModel::GetBoundingBox(this, precise);
			};
			
			virtual property Int64 RepresentationLabel
			{
				Int64 get(){return _representationLabel; }
				void set(Int64 value){ _representationLabel=value; }
			}
			virtual property TopoDS_Shape* Handle
			{
				TopoDS_Shape* get(){return pShell;};			
			}
			/*Interfaces*/

			virtual IXbimGeometryModel^ Cut(IXbimGeometryModel^ shape);
			virtual IXbimGeometryModel^ Union(IXbimGeometryModel^ shape);
			virtual IXbimGeometryModel^ Intersection(IXbimGeometryModel^ shape);
			virtual IXbimGeometryModel^ CopyTo(IfcObjectPlacement^ placement);

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

			virtual XbimTriangulatedModelCollection^ Mesh(bool withNormals, double deflection, Matrix3D transform);
			virtual XbimTriangulatedModelCollection^ Mesh(bool withNormals, double deflection);
			virtual XbimTriangulatedModelCollection^ Mesh(bool withNormals);
			virtual XbimTriangulatedModelCollection^ Mesh();

		
			//Builds a TopoDS_Shell from an ClosedShell
			static TopoDS_Shape Build(IfcClosedShell^ shell, bool% hasCurves);
			
			//Builds a TopoDS_Shell from an Openshell
			static TopoDS_Shape Build(IfcOpenShell^ shell, bool% hasCurves);

			//Builds a TopoDS_Shell from an ConnectedFaceSet
			static TopoDS_Shape Build(IfcConnectedFaceSet^ faceSet, bool% hasCurves);

		
		};

	}
}