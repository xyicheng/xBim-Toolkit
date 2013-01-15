using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.SharedBldgElements;

namespace Xbim.Xplorer
{
    class DefaultMaterials
    {
        private static Dictionary<Type, Material> _materialMap;

        static DefaultMaterials()
        {
            _materialMap = new Dictionary<Type, Material>();
            _materialMap.Add(typeof(IfcProduct), new Material(StripString(typeof(IfcProduct).ToString()), 0.98, 0.92, 0.74, 1, 0));
            _materialMap.Add(typeof(IfcWall), new Material(StripString(typeof(IfcWall).ToString()), 0.98, 0.92, 0.74, 1, 0));
            _materialMap.Add(typeof(IfcWallStandardCase), new Material(StripString(typeof(IfcWallStandardCase).ToString()), 0.98, 0.92, 0.74, 1, 0));
            _materialMap.Add(typeof(IfcRoof), new Material(StripString(typeof(IfcRoof).ToString()), 0.28, 0.24, 0.55, 1, 0));
            _materialMap.Add(typeof(IfcBeam), new Material(StripString(typeof(IfcBeam).ToString()), 0.0, 0.0, 0.55, 1, 0));
            _materialMap.Add(typeof(IfcColumn), new Material(StripString(typeof(IfcColumn).ToString()), 0.0, 0.0, 0.55, 1, 0));
            _materialMap.Add(typeof(IfcSlab), new Material(StripString(typeof(IfcSlab).ToString()), 0.47, 0.53, 0.60, 1, 0));
            _materialMap.Add(typeof(IfcWindow), new Material(StripString(typeof(IfcWindow).ToString()), 0.68, 0.85, 0.90, 0.2, 0, priority:2));
            _materialMap.Add(typeof(IfcCurtainWall), new Material(StripString(typeof(IfcCurtainWall).ToString()), 0.68, 0.85, 0.90, 0.4, 0, priority: 2));
            _materialMap.Add(typeof(IfcPlate), new Material(StripString(typeof(IfcPlate).ToString()), 0.68, 0.85, 0.90, 0.4, 0, priority: 2));
            _materialMap.Add(typeof(IfcDoor), new Material(StripString(typeof(IfcDoor).ToString()), 0.97, 0.19, 0, 1, 0));
            _materialMap.Add(typeof(IfcSpace), new Material(StripString(typeof(IfcSpace).ToString()), 0.68, 0.85, 0.90, 0.4, 0, priority: 2));
            _materialMap.Add(typeof(IfcMember), new Material(StripString(typeof(IfcMember).ToString()), 0.34, 0.34, 0.34, 1, 0));
            _materialMap.Add(typeof(IfcDistributionElement), new Material(StripString(typeof(IfcDistributionElement).ToString()), 0.0, 0.0, 0.55, 1, 0));
            _materialMap.Add(typeof(IfcElectricalElement), new Material(StripString(typeof(IfcElectricalElement).ToString()), 0.0, 0.9, 0.1, 1, 0));
            _materialMap.Add(typeof(IfcFurnishingElement), new Material(StripString(typeof(IfcFurnishingElement).ToString()), 1, 0, 0, 1, 0));
            _materialMap.Add(typeof(IfcOpeningElement), new Material(StripString(typeof(IfcOpeningElement).ToString()), 0.200000003, 0.200000003, 0.800000012, 0.2, 0, priority: 1));
            _materialMap.Add(typeof(IfcFeatureElementSubtraction), new Material(StripString(typeof(IfcFeatureElementSubtraction).ToString()), 1.0, 1.0, 1.0, 0.0, 0, priority: 1));
        }

        public static Material LookupMaterial(IfcProduct item)
        {
            Type type = item.GetType();
            Material material;

            while (type != null)
            {
                if (_materialMap.TryGetValue(type, out material))
                    return material;

                type = type.BaseType;
            }
            return null;
        }

        //hackiness to strip the base product details from the product types, to make it more human readable
        public static string StripString(string p)
        {
            p = p.Replace("Xbim.Ifc.SharedBldgElements.", String.Empty);
            p = p.Replace("Xbim.Ifc.Kernel.", String.Empty);
            p = p.Replace("Xbim.Ifc.ProductExtension.", String.Empty);
            return p;
        }
    }
}
