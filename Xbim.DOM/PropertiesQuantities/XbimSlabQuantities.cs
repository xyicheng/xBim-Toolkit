using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.Extensions;

namespace Xbim.DOM.PropertiesQuantities
{
    public class XbimSlabQuantities : XbimQuantities
    {
        internal XbimSlabQuantities(IfcSlab slab) : base(slab, "SlabQuantities") { }


        /// <summary>
        ///Total nominal (or average) width (or thickness) of the slab. 
        ///The exact definition and calculation rules depend on the method of measurement used
        /// </summary>
        public double? NominalWidth { get { return GetElementQuantityAsDouble("NominalWidth"); } set { SetOrRemoveQuantity("NominalWidth", XbimQuantityTypeEnum.LENGTH, value); } }


        /// <summary>
        ///Perimeter measured along the outer boundaries of the slab. 
        ///The exact definition and calculation rules depend on the method of measurement used
        /// </summary>
        public double? Perimeter { get { return GetElementQuantityAsDouble("Perimeter"); } set { SetOrRemoveQuantity("Perimeter", XbimQuantityTypeEnum.LENGTH, value); } }


        /// <summary>
        ///Total area of the extruded area of the slab. The exact definition 
        ///and calculation rules depend on the method of measurement used
        /// </summary>
        public double? GrossArea { get { return GetElementQuantityAsDouble("GrossArea"); } set { SetOrRemoveQuantity("GrossArea", XbimQuantityTypeEnum.AREA, value); } }


        /// <summary>
        ///Total area of the extruded area of the slab, taking into account 
        ///possible slab openings. The exact definition and calculation rules depend on the method of measurement used
        /// </summary>
        public double? NetArea { get { return GetElementQuantityAsDouble("NetArea"); } set { SetOrRemoveQuantity("NetArea", XbimQuantityTypeEnum.AREA, value); } }


        /// <summary>
        ///Total gross volume of the slab, not taking into account possible 
        ///openings and recesses. The exact definition and calculation rules depend on the method of measurement used
        /// </summary>
        public double? GrossVolume { get { return GetElementQuantityAsDouble("GrossVolume"); } set { SetOrRemoveQuantity("GrossVolume", XbimQuantityTypeEnum.VOLUME, value); } }


        /// <summary>
        ///Total net volume of the slab, taking into account possible openings
        ///and recesses. The exact definition and calculation rules depend on the method of measurement used
        /// </summary>
        public double? NetVolume { get { return GetElementQuantityAsDouble("NetVolume"); } set { SetOrRemoveQuantity("NetVolume", XbimQuantityTypeEnum.VOLUME, value); } }


        /// <summary>
        ///Total gross weight of the slab, not taking into account possible 
        ///openings and recesses or projections. The exact definition and calculation rules depend on the method of measurement used
        /// </summary>
        public double? GrossWeight { get { return GetElementQuantityAsDouble("GrossWeight"); } set { SetOrRemoveQuantity("GrossWeight", XbimQuantityTypeEnum.WEIGHT, value); } }


        /// <summary>
        ///Total net weight of the slab, taking into account possible openings and recesses or projections. 
        ///The exact definition and calculation rules depend on the method of measurement used
        /// </summary>
        public double? NetWeight { get { return GetElementQuantityAsDouble("NetWeight"); } set { SetOrRemoveQuantity("NetWeight", XbimQuantityTypeEnum.WEIGHT, value); } }
        
    }
}
