namespace Xbim.SceneJSWebViewer
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Xbim.Ifc2x3.Kernel;
    using Xbim.Ifc2x3.SharedBldgElements;
    using Xbim.Ifc2x3.ProductExtension;
    using Xbim.IO;

    public class DefaultMaterials
    {
        private static Dictionary<Type, Material> _materialMap;

        static DefaultMaterials()
        {
            _materialMap = new Dictionary<Type, Material>();
            _materialMap.Add(typeof(IfcProduct), new Material(typeof(IfcProduct).Name, 0.98, 0.92, 0.74, 1, 0));
            _materialMap.Add(typeof(IfcWall), new Material(typeof(IfcWall).Name, 0.98, 0.92, 0.74, 1, 0));
            _materialMap.Add(typeof(IfcWallStandardCase), new Material(typeof(IfcWallStandardCase).Name, 0.98, 0.92, 0.74, 1, 0));
            _materialMap.Add(typeof(IfcRoof), new Material(typeof(IfcRoof).Name, 0.28, 0.24, 0.55, 1, 0));
            _materialMap.Add(typeof(IfcBeam), new Material(typeof(IfcBeam).Name, 0.0, 0.0, 0.55, 1, 0));
            _materialMap.Add(typeof(IfcColumn), new Material(typeof(IfcColumn).Name, 0.0, 0.0, 0.55, 1, 0));
            _materialMap.Add(typeof(IfcSlab), new Material(typeof(IfcSlab).Name, 0.47, 0.53, 0.60, 1, 0));
            _materialMap.Add(typeof(IfcWindow), new Material(typeof(IfcWindow).Name, 0.68, 0.85, 0.90, 0.2, 0));
            _materialMap.Add(typeof(IfcCurtainWall), new Material(typeof(IfcCurtainWall).Name, 0.68, 0.85, 0.90, 0.4, 0));
            _materialMap.Add(typeof(IfcPlate), new Material(typeof(IfcPlate).Name, 0.68, 0.85, 0.90, 0.4, 0));
            _materialMap.Add(typeof(IfcDoor), new Material(typeof(IfcDoor).Name, 0.97, 0.19, 0, 1, 0));
            _materialMap.Add(typeof(IfcSpace), new Material(typeof(IfcSpace).Name, 0.68, 0.85, 0.90, 0.4, 0));
            _materialMap.Add(typeof(IfcMember), new Material(typeof(IfcMember).Name, 0.34, 0.34, 0.34, 1, 0));
            _materialMap.Add(typeof(IfcDistributionElement), new Material(typeof(IfcDistributionElement).Name, 0.0, 0.0, 0.55, 1, 0));
            _materialMap.Add(typeof(IfcElectricalElement), new Material(typeof(IfcElectricalElement).Name, 0.0, 0.9, 0.1, 1, 0));
            _materialMap.Add(typeof(IfcFurnishingElement), new Material(typeof(IfcFurnishingElement).Name, 1, 0, 0, 1, 0));
            _materialMap.Add(typeof(IfcOpeningElement), new Material(typeof(IfcOpeningElement).Name, 0.200000003, 0.200000003, 0.800000012, 0.2, 0));
            _materialMap.Add(typeof(IfcFeatureElementSubtraction), new Material(typeof(IfcFeatureElementSubtraction).Name, 1.0, 1.0, 1.0, 0.0, 0));
        }


        public static Material LookupMaterial(short ifcType)
        {
            Type type = IfcMetaData.GetType(ifcType);
            string origName = type.Name;
            Material material;

            while (type != null)
            {
                if (_materialMap.TryGetValue(type, out material))
                {
                    return new Material(
                        origName,
                        material.Red,
                        material.Green,
                        material.Blue,
                        material.Alpha,
                        material.Emit
                        );
                }
                type = type.BaseType;
            }
            return null;
        }
    }
}