using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.SharedBldgElements;
using Xbim.Ifc.Extensions;

namespace Xbim.DOM.PropertiesQuantities
{
    public class XbimPlateQuantities : XbimQuantities
    {
        internal XbimPlateQuantities(IfcPlate plate) : base(plate, "Platequantities") { }

        /// <summary>
        ///Total area of the cross section (or profile) of the plate (or its basic surface area). The exact definition and calculation rules depend on the method of measurement used
        /// </summary>
        public double? CrossSectionArea { get { return GetElementQuantityAsDouble("CrossSectionArea"); } set { SetOrRemoveQuantity("CrossSectionArea", XbimQuantityTypeEnum.AREA, value); } }


        /// <summary>
        ///Total gross volume of the plate, not taking into account possible processing features (cut-outs, etc.) or openings and recesses. The exact definition and calculation rules depend on the method of measurement used
        /// </summary>
        public double? GrossVolume { get { return GetElementQuantityAsDouble("GrossVolume"); } set { SetOrRemoveQuantity("GrossVolume", XbimQuantityTypeEnum.VOLUME, value); } }


        /// <summary>
        ///Total net volume of the plate, taking into account possible processing features (cut-outs, etc.) or openings and recesses. The exact definition and calculation rules depend on the method of measurement used
        /// </summary>
        public double? NetVolume { get { return GetElementQuantityAsDouble("NetVolume"); } set { SetOrRemoveQuantity("NetVolume", XbimQuantityTypeEnum.VOLUME, value); } }


        /// <summary>
        ///Total gross weight of the plate without add-on parts, not taking into account possible processing features (cut-outs, etc.) or openings and recesses
        /// </summary>
        public double? GrossWeight { get { return GetElementQuantityAsDouble("GrossWeight"); } set { SetOrRemoveQuantity("GrossWeight", XbimQuantityTypeEnum.WEIGHT, value); } }


        /// <summary>
        ///Total net weight of the plate without add-on parts, taking into account possible processing features (cut-outs, etc.) or openings and recesses
        /// </summary>
        public double? NetWeight { get { return GetElementQuantityAsDouble("NetWeight"); } set { SetOrRemoveQuantity("NetWeight", XbimQuantityTypeEnum.WEIGHT, value); } }
        
    }
}
