using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.Extensions;

namespace Xbim.DOM.PropertiesQuantities
{
    public class XbimBuildingQuantities : XbimQuantities
    {
        internal XbimBuildingQuantities(IfcBuilding building) : base(building, "BuildingQuantities") { }


        /// <summary>
        ///Calculated height of the building, measured from the level of terrain to the top
        ///part of the building. The exact definition and calculation rules depend on the
        ///method of measurement used.
        /// </summary>
        public double? NominalHeight { get { return GetElementQuantityAsDouble("NominalHeight"); } set { SetOrRemoveQuantity("NominalHeight", XbimQuantityTypeEnum.LENGTH, value); } }

        /// <summary>
        ///Calculated coverage of the site area that is occupied by the building (also referred to as footprint). 
        ///The exact definition and calculation rules depend on the method of measurement used.
        /// </summary>
        public double? NominalArea { get { return GetElementQuantityAsDouble("NominalArea"); } set { SetOrRemoveQuantity("NominalArea", XbimQuantityTypeEnum.AREA, value); } }

        /// <summary>
        ///Calculated sum of all areas covered by the building (normally including the area of construction elements).
        ///The exact definition and calculation rules depend on the method of measurement used.
        /// </summary>
        public double? GrossFloorArea { get { return GetElementQuantityAsDouble("GrossFloorArea"); } set { SetOrRemoveQuantity("GrossFloorArea", XbimQuantityTypeEnum.AREA, value); } }

        /// <summary>
        ///Calculated sum of all usable areas covered by the building (normally excluding the area 
        ///of construction elements). The exact definition and calculation rules depend on the 
        ///method of measurement used.
        /// </summary>
        public double? NetFloorArea { get { return GetElementQuantityAsDouble("NetFloorArea"); } set { SetOrRemoveQuantity("NetFloorArea", XbimQuantityTypeEnum.AREA, value); } }

        /// <summary>
        ///Calculated gross volume of all areas enclosed by the building (normally including the 
        ///area of construction elements). The exact definition and calculation rules depend on
        ///the method of measurement used.
        /// </summary>
        public double? GrossVolume { get { return GetElementQuantityAsDouble("GrossVolume"); } set { SetOrRemoveQuantity("GrossVolume", XbimQuantityTypeEnum.VOLUME, value); } }

        /// <summary>
        ///Calculated net volume of all areas enclosed by the building (normally excluding the area 
        ///of construction elements). The exact definition and calculation rules depend on the method
        ///of measurement used.
        /// </summary>
        public double? NetVolume { get { return GetElementQuantityAsDouble("NetVolume"); } set { SetOrRemoveQuantity("NetVolume", XbimQuantityTypeEnum.VOLUME, value); } }

    }
}
