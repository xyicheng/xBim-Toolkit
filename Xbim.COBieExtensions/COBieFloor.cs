using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.Ifc.ProductExtension;

namespace Xbim.COBieExtensions
{
    public class COBieFloor
    {
        IfcBuildingStorey _ifcBuildingStorey;
        int _defaultLevel;
        public COBieFloor(IfcBuildingStorey storey, int defaultLevel)
        {
            _ifcBuildingStorey = storey;
            _defaultLevel = defaultLevel;
        }

        public string Name
        {
            get
            {
                if (_ifcBuildingStorey.Name.HasValue) return _ifcBuildingStorey.Name.Value;
                if (_ifcBuildingStorey.LongName.HasValue) return _ifcBuildingStorey.LongName.Value;
                if (_ifcBuildingStorey.Description.HasValue) return _ifcBuildingStorey.Description.Value;
                return "Level " + _defaultLevel.ToString();
            }
        }
    }
}
