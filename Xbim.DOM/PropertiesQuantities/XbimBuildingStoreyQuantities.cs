using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.Extensions;

namespace Xbim.DOM.PropertiesQuantities
{
    public class XbimBuildingStoreyQuantities : XbimQuantities
    {
        internal XbimBuildingStoreyQuantities(IfcBuildingStorey storey) : base(storey, "StoreyQuantities") { }


        /// <summary>
        ///Standard height of this storey, from the bottom surface of the floor, 
        ///to the bottom surface of the floor or roof above. The exact definition and calculation 
        ///rules depend on the method of measurement used.	
        /// </summary>
        public double? NominalHeight { get { return GetElementQuantityAsDouble("NominalHeight"); } set { SetOrRemoveQuantity("NominalHeight", XbimQuantityTypeEnum.LENGTH, value); } }


        /// <summary>
        ///Calculated sum of all areas covered by the building storey (as horizontal 
        ///projections and normally including the area of construction elements. The exact 
        ///definition and calculation rules depend on the method of measurement used.
        /// </summary>
        public double? GrossFloorArea { get { return GetElementQuantityAsDouble("GrossFloorArea"); } set { SetOrRemoveQuantity("GrossFloorArea", XbimQuantityTypeEnum.AREA, value); } }


        /// <summary>
        ///Calculated sum of all usable areas covered by the building storey 
        ///(normally excluding the area of construction elements). The exact definition 
        ///and calculation rules depend on the method of measurement used.	
        /// </summary>
        public double? NetFloorArea { get { return GetElementQuantityAsDouble("NetFloorArea"); } set { SetOrRemoveQuantity("NetFloorArea", XbimQuantityTypeEnum.AREA, value); } }


        /// <summary>
        ///Calculated gross volume of all areas enclosed by the building storey 
        ///(normally including the area of construction elements). The exact definition 
        ///and calculation rules depend on the method of measurement used.
        /// </summary>
        public double? GrossVolume { get { return GetElementQuantityAsDouble("GrossVolume"); } set { SetOrRemoveQuantity("GrossVolume", XbimQuantityTypeEnum.VOLUME, value); } }


        /// <summary>
        ///Calculated net volume of all areas enclosed by the building storey 
        ///(normally excluding the area of construction elements). The exact definition and 
        ///calculation rules depend on the method of measurement used.
        /// </summary>
        public double? NetVolume { get { return GetElementQuantityAsDouble("NetVolume"); } set { SetOrRemoveQuantity("NetVolume", XbimQuantityTypeEnum.VOLUME, value); } }

    }
}
