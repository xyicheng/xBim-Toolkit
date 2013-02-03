using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.PresentationAppearanceResource;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.SharedBldgServiceElements;

namespace Xbim.ModelGeometry.Scene
{

    public enum StandardColourMaps
    {
        /// <summary>
        /// Creates a colour map based on the IFC product types
        /// </summary>
        IfcProductTypeMap,
        /// <summary>
        /// Creates an empty colour map
        /// </summary>
        Empty
    }
    /// <summary>
    /// Provides a map for obtaining a colour for a keyed type, the colour is an ARGB value
    /// </summary>
    public class XbimColourMap : KeyedCollection<string, XbimColour>
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


        public XbimColourMap(StandardColourMaps initMap= StandardColourMaps.IfcProductTypeMap)
        {
            switch (initMap)
            {
                case StandardColourMaps.IfcProductTypeMap:
                    BuildIfcProductTypeMap();
                    break;
                default:
                    break;
            }
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
            Add(new XbimColour("Default", 0.98, 0.92, 0.74, 1));
            Add(new XbimColour(typeof(IfcWall).Name, 0.98, 0.92, 0.74, 1));
            Add(new XbimColour(typeof(IfcWallStandardCase).Name, 0.98, 0.92, 0.74, 1));
            Add(new XbimColour(typeof(IfcRoof).Name, 0.28, 0.24, 0.55, 1));
            Add(new XbimColour(typeof(IfcBeam).Name, 0.0, 0.0, 0.55, 1));
            Add(new XbimColour(typeof(IfcColumn).Name, 0.0, 0.0, 0.55, 1));
            Add(new XbimColour(typeof(IfcSlab).Name, 0.47, 0.53, 0.60, 1));
            Add(new XbimColour(typeof(IfcWindow).Name, 0.68, 0.85, 0.90, 0.2));
            Add(new XbimColour(typeof(IfcCurtainWall).Name, 0.68, 0.85, 0.90, 0.4));
            Add(new XbimColour(typeof(IfcPlate).Name, 0.68, 0.85, 0.90, 0.4));
            Add(new XbimColour(typeof(IfcDoor).Name, 0.97, 0.19, 0, 1));
            Add(new XbimColour(typeof(IfcSpace).Name, 0.68, 0.85, 0.90, 0.4));
            Add(new XbimColour(typeof(IfcMember).Name, 0.34, 0.34, 0.34, 1));
            Add(new XbimColour(typeof(IfcDistributionElement).Name, 0.0, 0.0, 0.55, 1));
            Add(new XbimColour(typeof(IfcElectricalElement).Name, 0.0, 0.9, 0.1, 1));
            Add(new XbimColour(typeof(IfcFurnishingElement).Name, 1, 0, 0, 1));
            Add(new XbimColour(typeof(IfcOpeningElement).Name, 0.200000003, 0.200000003, 0.800000012, 0.2));
            Add(new XbimColour(typeof(IfcFeatureElementSubtraction).Name, 1.0, 1.0, 1.0, 0.0));
            Add(new XbimColour(typeof(IfcFlowTerminal).Name, 0.95, 0.94, 0.74, 1));
            Add(new XbimColour(typeof(IfcFlowSegment).Name, 0.95, 0.94, 0.74, 1));
            Add(new XbimColour(typeof(IfcDistributionFlowElement).Name, 0.95, 0.94, 0.74, 1));
            Add(new XbimColour(typeof(IfcFlowFitting).Name, 0.95, 0.94, 0.74, 1));
            Add(new XbimColour(typeof(IfcRailing).Name, 0.95, 0.94, 0.74, 1));
        }



    }
}
