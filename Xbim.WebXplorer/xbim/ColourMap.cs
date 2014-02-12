using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Web;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.SharedBldgServiceElements;
using Xbim.ModelGeometry.Scene;

namespace Xbim.WebXplorer.xbim
{
    public class ColourMap : KeyedCollection<string, XbimColour>
    {
        protected override string GetKeyForItem(XbimColour item)
        {
            return item.Name;
        }

        public bool IsTransparent
        {
            get
            {
                return this.Any(c => c.IsTransparent);
            }
        }


        public ColourMap()
        {
            BuildIfcProductTypeMap();
        }

        new public XbimColour this[string key]
        {
            get
            {
                if (base.Contains(key))
                    return base[key];
                else if (base.Contains("Default"))
                    return base["Default"];
                else
                    return XbimColour.Default;
            }
        }


        private void BuildIfcProductTypeMap()
        {
            Clear();
            Add(new XbimColour("Default", 0.98, 0.92, 0.74, 1));
            Add(new XbimColour(typeof(IfcWall).Name, 0.98, 0.92, 0.74, 1));
            Add(new XbimColour(typeof(IfcWallStandardCase).Name, 0.98, 0.92, 0.74, 1));
            Add(new XbimColour(typeof(IfcRoof).Name, 0.28, 0.24, 0.55, 1));
            Add(new XbimColour(typeof(IfcBeam).Name, 0.0, 0.0, 0.55, 1));
            Add(new XbimColour(typeof(IfcBuildingElementProxy).Name, 0.95, 0.94, 0.74, 1));
            Add(new XbimColour(typeof(IfcColumn).Name, 0.0, 0.0, 0.55, 1));
            Add(new XbimColour(typeof(IfcSlab).Name, 0.47, 0.53, 0.60, 1));
            Add(new XbimColour(typeof(IfcWindow).Name, 0.68, 0.85, 0.90, 0.2));
            Add(new XbimColour(typeof(IfcCurtainWall).Name, 0.68, 0.85, 0.90, 0.4));
            Add(new XbimColour(typeof(IfcPlate).Name, 0.68, 0.85, 0.90, 0.4));
            Add(new XbimColour(typeof(IfcDoor).Name, 0.97, 0.19, 0, 1));
            Add(new XbimColour(typeof(IfcSpace).Name, 0.68, 0.85, 0.90, 0.6));
            Add(new XbimColour(typeof(IfcMember).Name, 0.34, 0.34, 0.34, 1));
            Add(new XbimColour(typeof(IfcEnergyConversionDevice).Name, 0.6, 0.0, 0.8, 1));
            Add(new XbimColour(typeof(IfcDistributionElement).Name, 0.0, 0.0, 0.55, 1));
            Add(new XbimColour(typeof(IfcElectricalElement).Name, 0.0, 0.9, 0.1, 1));
            Add(new XbimColour(typeof(IfcFurnishingElement).Name, 1, 0, 0, 1));
            Add(new XbimColour(typeof(IfcOpeningElement).Name, 0.200000003, 0.200000003, 0.800000012, 0.2));
            Add(new XbimColour(typeof(IfcFeatureElementSubtraction).Name, 1.0, 1.0, 1.0, 0.0));
            Add(new XbimColour(typeof(IfcFlowTerminal).Name, 1.0, 1.0, 0.2, 1));
            Add(new XbimColour(typeof(IfcFlowSegment).Name, 1.0, 0.6, 0.0, 1));
            Add(new XbimColour(typeof(IfcDistributionFlowElement).Name, 0.95, 0.94, 0.74, 1));
            Add(new XbimColour(typeof(IfcFlowFitting).Name, 1.0, 0.6, 0.0, 1));
            Add(new XbimColour(typeof(IfcRailing).Name, 0.95, 0.94, 0.74, 1));
            Add(new XbimColour(typeof(IfcFlowController).Name, 1.0, 0.0, 0.0, 1));
        }
    }
}