#pragma once
#include <TopoDS_Shape.hxx>
#include "XbimGeometryModel.h"
using namespace System;
using namespace System::IO;
using namespace System::Collections::Generic;
using namespace Xbim::ModelGeometry::Scene;
using namespace Xbim::Ifc2x3::Kernel;
using namespace Xbim::XbimExtensions;
using namespace Xbim::Common::Logging;
#using  <Xbim.IO.dll> as_friend
using namespace Xbim::IO;
namespace Xbim
{
	namespace ModelGeometry
	{	
		public delegate void ProcessModel(IModel^ model);

		public ref class XbimScene :  IXbimScene
		{

		public:
			static void ConvertGeometry(XbimModel^ model, IEnumerable<IfcProduct^>^ toConvert, ReportProgressDelegate^ progDelegate);
		private:
			XbimLOD _lod;
			TransformGraph^ _graph;
			Stream^ _sceneStream;
			String^ _sceneStreamFileName;
			Dictionary<IfcRepresentation^, IXbimGeometryModel^>^ _maps;
			static ILogger^ Logger = LoggerFactory::GetLogger();
			void ImportIfc(String ^ ifcFileName,String ^ xBimFileName,String ^ xBimGeometryFileName,  ProcessModel ^ processingDelegate);

			void Initialise(void)
			{
				Standard::SetReentrant(Standard_True);
			}
		public:
			XbimScene(IModel^ model);
			XbimScene(IModel^ model, IEnumerable<IfcProduct^>^ toDraw);
			XbimScene(String ^ ifcFileName, String ^ xbimFileName,String ^ xBimGeometryFileName);
			XbimScene(String ^ ifcFileName,String ^ xBimFileName,String ^ xBimGeometryFileName, ProcessModel ^ processingDelegate);
			!XbimScene();
			~XbimScene();
			virtual void Close();

			XbimSceneStream^ AsSceneStream();
			virtual XbimTriangulatedModelStream^ Triangulate(TransformNode^ node);
			virtual property TransformGraph^ Graph
			{
				TransformGraph^ get()
				{
					return _graph;
				}
			}
			virtual property XbimLOD LOD
			{
				XbimLOD get()
				{
					return _lod;
				}
				void set(XbimLOD lod)
				{
					_lod = lod;
				}
			}
		};
	}
}

