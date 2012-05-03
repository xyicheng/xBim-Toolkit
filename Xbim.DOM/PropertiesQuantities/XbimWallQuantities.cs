using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.Extensions;
using Xbim.Ifc.SharedBldgElements;

namespace Xbim.DOM.PropertiesQuantities
{
    /// <summary>
    /// Quantities of the wall
    /// </summary>
    public class XbimWallQuantities : XbimQuantities
    {
        internal XbimWallQuantities(IfcWall wall) : base(wall, "WallQuantities") { }

        /// <summary>
        /// Total nominal (or average) length of the wall along the wall path. 
        /// The exact definition and calculation rules depend on the method of measurement used.
        /// </summary>
        public double? NominalLength { get { return GetElementQuantityAsDouble("NominalLength"); } set { SetOrRemoveQuantity("NominalLength", XbimQuantityTypeEnum.LENGTH, value); } }


        /// <summary>
        /// Total nominal (or average) width (or thickness) of the wall perpendicular to the wall path.
        /// The exact definition and calculation rules depend on the method of measurement used.
        /// </summary>
        public double? NominalWidth { get { return GetElementQuantityAsDouble("NominalWidth"); } set { SetOrRemoveQuantity("NominalWidth", XbimQuantityTypeEnum.LENGTH, value); } }

        /// <summary>
        /// Total nominal (or average) height of the wall along the wall path. 
        /// The exact definition and calculation rules depend on the method of measurement used.
        /// </summary>
        public double? NominalHeight { get { return GetElementQuantityAsDouble("NominalHeight"); } set { SetOrRemoveQuantity("NominalHeight", XbimQuantityTypeEnum.LENGTH, value); } }

        /// <summary>
        /// Area of the wall as viewed by a ground floor view, not taking any wall modifications (like recesses) into account. 
        /// It is also referred to as the foot print of the wall. The exact definition and calculation rules depend on the method of measurement used.
        /// </summary>
        public double? GrossFootprintArea { get { return GetElementQuantityAsDouble("GrossFootprintArea"); } set { SetOrRemoveQuantity("GrossFootprintArea", XbimQuantityTypeEnum.AREA, value); } }

        /// <summary>
        /// Area of the wall as viewed by a ground floor view, taking all wall modifications (like recesses) into account. 
        /// It is also referred to as the foot print of the wall. The exact definition and calculation rules depend on the method of measurement used.
        /// </summary>
        public double? NetFootprintArea { get { return GetElementQuantityAsDouble("NetFootprintArea"); } set { SetOrRemoveQuantity("NetFootprintArea", XbimQuantityTypeEnum.AREA, value); } }

        /// <summary>
        /// Area of the wall as viewed by an elevation view of the middle plane of the wall.  
        /// It does not take into account any wall modifications (such as openings). 
        /// The exact definition and calculation rules depend on the method of measurement used.
        /// </summary>
        public double? GrossSideArea { get { return GetElementQuantityAsDouble("GrossSideArea"); } set { SetOrRemoveQuantity("GrossSideArea", XbimQuantityTypeEnum.AREA, value); } }

        /// <summary>
        /// Area of the wall as viewed by an elevation view of the middle plane. It does take into account all wall modifications (such as openings).
        /// The exact definition and calculation rules depend on the method of measurement used.
        /// </summary>
        public double? NetSideArea { get { return GetElementQuantityAsDouble("NetSideArea"); } set { SetOrRemoveQuantity("NetSideArea", XbimQuantityTypeEnum.AREA, value); } }

        /// <summary>
        /// Area of the wall as viewed by an elevation view of the left side (when viewed along the wall path orientation). 
        /// It does not take into account any wall modifications (such as openings). The exact definition and calculation
        /// rules depend on the method of measurement used.
        /// </summary>
        public double? GrossSideAreaLeft { get { return GetElementQuantityAsDouble("GrossSideAreaLeft"); } set { SetOrRemoveQuantity("GrossSideAreaLeft", XbimQuantityTypeEnum.AREA, value); } }

        /// <summary>
        /// Area of the wall as viewed by an elevation view of the left side (when viewed along the wall path orientation).
        /// It does take into account all wall modifications (such as openings). The exact definition and calculation rules
        /// depend on the method of measurement used.
        /// </summary>
        public double? NetSideAreaLeft { get { return GetElementQuantityAsDouble("NetSideAreaLeft"); } set { SetOrRemoveQuantity("NetSideAreaLeft", XbimQuantityTypeEnum.AREA, value); } }

        /// <summary>
        /// Area of the wall as viewed by an elevation view of the right side (when viewed along the wall path orientation).
        /// It does not take into account any wall modifications (such as openings). The exact definition and calculation 
        /// rules depend on the method of measurement used.
        /// </summary>
        public double? GrossSideAreaRight { get { return GetElementQuantityAsDouble("GrossSideAreaRight"); } set { SetOrRemoveQuantity("GrossSideAreaRight", XbimQuantityTypeEnum.AREA, value); } }

        /// <summary>
        /// Area of the wall as viewed by an elevation view of the right side (when viewed along the wall path orientation).
        /// It does take into account all wall modifications (such as openings). The exact definition and calculation rules 
        /// depend on the method of measurement used.
        /// </summary>
        public double? NetSideAreaRight { get { return GetElementQuantityAsDouble("NetSideAreaRight"); } set { SetOrRemoveQuantity("NetSideAreaRight", XbimQuantityTypeEnum.AREA, value); } }


        /// <summary>
        /// Volume of the wall, without taking into account the openings and the connection geometry. The exact definition
        /// and calculation rules depend on the method of measurement used.
        /// </summary>
        public double? GrossVolume { get { return GetElementQuantityAsDouble("GrossVolume"); } set { SetOrRemoveQuantity("GrossVolume", XbimQuantityTypeEnum.VOLUME, value); } }

        /// <summary>
        /// Volume of the wall, after subtracting the openings and after considering the connection geometry. The exact
        /// definition and calculation rules depend on the method of measurement used.
        /// </summary>
        public double? NetVolume { get { return GetElementQuantityAsDouble("NetVolume"); } set { SetOrRemoveQuantity("NetVolume", XbimQuantityTypeEnum.VOLUME, value); } }

    }
}
