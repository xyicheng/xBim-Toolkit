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
			static void ConvertGeometry(IEnumerable<IfcProduct^>^ toConvert, ReportProgressDelegate^ progDelegate, bool oCCout );
		private:
			XbimLOD _lod;
			TransformGraph^ _graph;
			Stream^ _sceneStream;
			String^ _sceneStreamFileName;
			Dictionary<IfcRepresentation^, IXbimGeometryModel^>^ _maps;
			bool _occOut;
			static ILogger^ Logger = LoggerFactory::GetLogger();
			
			void Initialise(void)
			{
				Standard::SetReentrant(Standard_True);
			}
		public:
			XbimScene(XbimModel^ model);
			XbimScene(XbimModel^ model, IEnumerable<IfcProduct^>^ toDraw, bool oCCout );

			!XbimScene();
			~XbimScene();
			virtual void Close();

			
			
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

