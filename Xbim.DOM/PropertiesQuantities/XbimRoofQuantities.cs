using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.Extensions;

namespace Xbim.DOM.PropertiesQuantities
{
    public class XbimRoofQuantities :XbimQuantities
    {
        internal XbimRoofQuantities(IfcRoof roof) : base(roof, "RoofQuantities") { }

        /// <summary>
        ///Total (exposed to the outside) area of all roof slabs belonging to the roof. 
        ///The exact definition and calculation rules depend on the method of measurement used
        /// </summary>
        public double? TotalSurfaceArea { get { return GetElementQuantityAsDouble("TotalSurfaceArea"); } set { SetOrRemoveQuantity("TotalSurfaceArea", XbimQuantityTypeEnum.AREA, value); } }
        
    }
}
