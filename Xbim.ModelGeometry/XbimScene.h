#pragma once
#include <TopoDS_Shape.hxx>
#include "XbimGeometryModel.h"
using namespace System;
using namespace System::IO;
using namespace System::Collections::Generic;
using namespace Xbim::ModelGeometry::Scene;
using namespace Xbim::Ifc::Kernel;
using namespace Xbim::XbimExtensions;
using namespace Xbim::Ifc::RepresentationResource;
namespace Xbim
{
	namespace ModelGeometry
	{	
		public delegate void ProcessModel(IModel^ model);

		public ref class XbimScene :  IXbimScene
		{
		private:
			TransformGraph^ _graph;
			Stream^ _sceneStream;
			String^ _sceneStreamFileName;
			Dictionary<IfcRepresentation^, IXbimGeometryModel^>^ _maps;
			void Initialise(void)
			{
				Standard::SetReentrant(Standard_True);
			}
		public:

			XbimScene(IModel^ model);
			XbimScene(String ^ ifcFileName, String ^ xbimFileName,String ^ xBimGeometryFileName, bool removeIfcGeoemtry);
			XbimScene(String ^ ifcFileName,String ^ xBimFileName,String ^ xBimGeometryFileName, bool removeIfcGeometry, ProcessModel ^ processingDelegate);
			!XbimScene();
			~XbimScene();
			virtual void Close();
			virtual bool ReOpen();
			XbimSceneStream^ AsSceneStream();
			virtual XbimTriangulatedModelStream^ Triangulate(TransformNode^ node);
			virtual property TransformGraph^ Graph
			{
				TransformGraph^ get()
				{
					return _graph;
				}
			}
		};
	}
}

