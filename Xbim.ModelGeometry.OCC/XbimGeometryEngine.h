#pragma once
#include "XbimGeometryModel.h"

using namespace Xbim::Common::Logging;
using namespace Xbim::IO;
using namespace Xbim::ModelGeometry::OCC;
namespace Xbim
{
	namespace ModelGeometry
	{
		public ref class XbimGeometryEngine : IXbimGeometryEngine
		{

		public:
			XbimGeometryEngine(void)
			{
			};
			virtual IXbimGeometryModel^ GetGeometry3D(IfcProduct^ product, ConcurrentDictionary<int, Object^>^ maps);
			virtual IXbimGeometryModel^ GetGeometry3D(IfcSolidModel^ solid, ConcurrentDictionary<int, Object^>^ maps);
			virtual IXbimGeometryModel^ GetGeometry3D(IfcProduct^ product);
			virtual IXbimGeometryModel^ GetGeometry3D(IfcSolidModel^ solid);
			virtual void Init(XbimModel^ model)
			{
				_model = model;
				Standard::SetReentrant(Standard_True);
				//_solids = gcnew ConcurrentDictionary<int, XbimGeometryModel^>();
			}
		private:
			static ILogger^ Logger = LoggerFactory::GetLogger();
			//ConcurrentDictionary<int, XbimGeometryModel^>^ _solids;
			XbimModel^ _model;
			XbimGeometryModel^ CreateFrom(IfcProduct^ product, IfcGeometricRepresentationContext^ repContext, ConcurrentDictionary<int, Object^>^ maps, bool forceSolid, XbimLOD lod, bool occOut);
			XbimGeometryModel^ CreateFrom(IfcProduct^ product, ConcurrentDictionary<int, Object^>^ maps, bool forceSolid, XbimLOD lod, bool occOut);
			XbimGeometryModel^ CreateFrom(IfcProduct^ product, bool forceSolid, XbimLOD lod, bool occOut);
			XbimGeometryModel^ CreateFrom(IfcRepresentationItem^ repItem, bool forceSolid, XbimLOD lod, bool occOut);
			XbimGeometryModel^ CreateFrom(IfcRepresentationItem^ repItem, ConcurrentDictionary<int, Object^>^ maps, bool forceSolid, XbimLOD lod, bool occOut);
			XbimGeometryModel^ CreateFrom(IfcRepresentation^ shape, ConcurrentDictionary<int, Object^>^ maps, bool forceSolid, XbimLOD lod, bool occOut);
			XbimGeometryModel^ CreateFrom(IfcRepresentation^ shape, bool forceSolid, XbimLOD lod, bool occOut);
			//Builds a TopoDS_Compound from a ShellBasedSurfaceModel
			XbimGeometryModel^ Build(IfcShellBasedSurfaceModel^ repItem, bool forceSolid);
			XbimGeometryModel^ Build(IfcFaceBasedSurfaceModel^ repItem, bool forceSolid);
			bool CutOpenings(IfcProduct^ product, XbimLOD lod);
		};
	}
}

