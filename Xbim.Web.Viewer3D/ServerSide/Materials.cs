using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.SharedBldgServiceElements;

namespace Xbim.Web.Viewer3D.ServerSide
{
    public class Materials
    {
        private static Dictionary<Type, String> _materialMap;

        static Materials()
        {
            _materialMap = new Dictionary<Type, string>();
            _materialMap.Add(typeof(IfcProduct), "offwhite");
            _materialMap.Add(typeof(IfcWall), "offwhite");
            _materialMap.Add(typeof(IfcRoof), "darkslateblue");
            _materialMap.Add(typeof(IfcBeam), "darkblue");
            _materialMap.Add(typeof(IfcColumn), "darkblue");
            _materialMap.Add(typeof(IfcSlab), "lightgrey");
            _materialMap.Add(typeof(IfcWindow), "window");
            _materialMap.Add(typeof(IfcDoor), "darkred");
            _materialMap.Add(typeof(IfcSpace), "window");
            _materialMap.Add(typeof(IfcDistributionElement), "darkblue");
            _materialMap.Add(typeof(IfcElectricalElement), "green");
            
        }

        public static String LookupMaterial(IfcProduct item)
        {
            Type type = item.GetType();
            String material;

            while(type != null)
            {
                if(_materialMap.TryGetValue(type, out material))
                    return material;

                type = type.BaseType;
            }
            return null;
        }
    }
}