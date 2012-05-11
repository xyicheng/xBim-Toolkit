using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.QuantityResource;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.SharedBldgElements;

namespace Xbim.Ifc.Extensions
{
    public static class SlabExtensions
    {
        /// <summary>
        /// Returns the Gross Footprint Area, if the element base quantity GrossFloorArea is defined
        /// </summary>
        /// <param name="buildingStorey"></param>
        /// <returns></returns>
        public static IfcAreaMeasure GrossFootprintArea(this IfcSlab slab)
        {
            IfcQuantityArea qArea = slab.GetQuantity<IfcQuantityArea>("BaseQuantities", "GrossFootprintArea");
            if (qArea == null) qArea = slab.GetQuantity<IfcQuantityArea>("GrossFootprintArea"); //just look for any area
            if (qArea == null) qArea = slab.GetQuantity<IfcQuantityArea>("CrossArea"); //just look for any area if revit has done it
            if (qArea != null) return qArea.AreaValue;
            //try none schema defined properties

            return null;
        }
    }
}
