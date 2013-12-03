#pragma once
#include "XbimGeometryModel.h"

using namespace Xbim::Common::Logging;
using namespace Xbim::IO;
using namespace Xbim::ModelGeometry::OCC;
using namespace Xbim::XbimExtensions;
namespace Xbim
{
	namespace ModelGeometry
	{
		public ref class XbimGeometryEngine : IXbimGeometryEngine
		{
		public:
			XbimGeometryEngine(void)
			{
				//_representationItemCache = gcnew ConcurrentDictionary<int, XbimGeometryModel^>();
			};
			virtual IXbimGeometryModelGroup^ GetGeometry3D(IfcProduct^ product);
			virtual IXbimGeometryModelGroup^ GetGeometry3D(IfcProduct^ product,  XbimGeometryType xbimGeometryType);
			virtual IXbimGeometryModelGroup^ GetGeometry3D(IfcSolidModel^ solid, XbimGeometryType geomType);
			virtual IXbimGeometryModelGroup^ GetGeometry3D(IfcRepresentation^ representation);
			virtual IXbimGeometryModelGroup^ GetGeometry3D(IfcRepresentationItem^ repItem);
			virtual IXbimGeometryModel^      GetGeometry3D(String^ data, XbimGeometryType xbimGeometryType);
			virtual void Init(XbimModel^ model)
			{
				_model = model;
				Standard::SetReentrant(Standard_True);
				maps = gcnew ConcurrentDictionary<int, Object^>();
				_deflection=model->ModelFactors->DeflectionTolerance;
				_precision = model->ModelFactors->Precision;
				_precisionMax = model->ModelFactors->PrecisionMax;
			}
			virtual property double Deflection
			{
				double get() {return _deflection;}
				void set(double def) {_deflection=def;}
			}
			virtual property double Precision
			{
				double get() {return _precision;}
				void set(double p) {_precision=p;}
			}
			virtual property double PrecisionMax
			{
				double get() {return _precisionMax;}
				void set(double p) {_precisionMax=p;}
			}

		private:
			static ILogger^ Logger = LoggerFactory::GetLogger();
			XbimModel^ _model;
			double _deflection;
			double _precisionMax;
			double _precision;
			ConcurrentDictionary<int, Object^>^ maps;
			XbimGeometryModel^ CreateFrom(IfcProduct^ product, IfcGeometricRepresentationContext^ repContext, ConcurrentDictionary<int, Object^>^ maps, bool forceSolid, XbimLOD lod, bool occOut);
			XbimGeometryModel^ CreateFrom(IfcProduct^ product, ConcurrentDictionary<int, Object^>^ maps, bool forceSolid, XbimLOD lod, bool occOut);
			XbimGeometryModel^ CreateFrom(IfcProduct^ product, bool forceSolid, XbimLOD lod, bool occOut);
			XbimGeometryModel^ CreateFrom(IfcRepresentationItem^ repItem, bool forceSolid, XbimLOD lod, bool occOut);
			XbimGeometryModel^ CreateFrom(IfcRepresentationItem^ repItem, ConcurrentDictionary<int, Object^>^ maps, bool forceSolid, XbimLOD lod, bool occOut);
			XbimGeometryModel^ CreateFrom(IfcRepresentation^ shape, ConcurrentDictionary<int, Object^>^ maps, bool forceSolid, XbimLOD lod, bool occOut);
			XbimGeometryModel^ CreateFrom(IfcRepresentation^ shape, bool forceSolid, XbimLOD lod, bool occOut);
			
			XbimGeometryModel^ Build(IfcBooleanResult^ repItem, ConcurrentDictionary<int, Object^>^ maps);
			XbimGeometryModel^ Build(IfcCsgSolid^ csgSolid, ConcurrentDictionary<int, Object^>^ maps);

			bool CutOpenings(IfcProduct^ product, XbimLOD lod);

		};
	}
}

