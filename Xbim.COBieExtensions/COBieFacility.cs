using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.Kernel;
using Xbim.XbimExtensions;
using Xbim.Ifc.Extensions;
namespace Xbim.COBieExtensions
{
    public class COBieFacility
    {
        IfcProject _ifcProject;
        IfcSite _ifcSite;
        IfcBuilding _ifcBuilding;
        public COBieFacility(IModel model)
        {
            _ifcProject = model.IfcProject;
            if( _ifcProject!=null) 
                _ifcSite = _ifcProject.GetSites().FirstOrDefault();
            else 
                _ifcSite = model.InstancesOfType<IfcSite>().FirstOrDefault();
            if(_ifcSite!=null) 
                _ifcBuilding = _ifcSite.GetBuildings().FirstOrDefault();
            else 
                _ifcBuilding = model.InstancesOfType<IfcBuilding>().FirstOrDefault();
        }
        public string Name 
        { 
            get 
            {
                if (_ifcBuilding != null)
                {
                    if (_ifcBuilding.Name.HasValue) return _ifcBuilding.Name.Value;
                    if (_ifcBuilding.LongName.HasValue) return _ifcBuilding.LongName.Value;
                    if (_ifcBuilding.Description.HasValue) return _ifcBuilding.Description.Value;
                }
                if(_ifcSite!=null) 
                {
                    if (_ifcSite.Name.HasValue) return _ifcSite.Name.Value;
                    if (_ifcSite.LongName.HasValue) return _ifcSite.LongName.Value;
                    if (_ifcSite.Description.HasValue) return _ifcSite.Description.Value;
              
                }
                if(_ifcProject!=null) 
                {
                    if (_ifcSite.Name.HasValue) return _ifcSite.Name.Value;
                    if (_ifcSite.LongName.HasValue) return _ifcSite.LongName.Value;
                    if (_ifcSite.Description.HasValue) return _ifcSite.Description.Value;
              
                }
                return "Building Name";
            } 
        }
    }
}
