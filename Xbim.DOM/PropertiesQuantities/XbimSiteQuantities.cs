using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.Extensions;

namespace Xbim.DOM.PropertiesQuantities
{
    public class XbimSiteQuantities : XbimQuantities
    {
        internal XbimSiteQuantities(IfcSite site) : base(site, "SiteQuantities") { }

        /// <summary>
        ///Perimeter of the Site boundary. The exact definition and calculation rules depend on the method of measurement used.
        /// </summary>
        public double? NominalPerimeter { get { return GetElementQuantityAsDouble("NominalPerimeter"); } set { SetOrRemoveQuantity("NominalPerimeter", XbimQuantityTypeEnum.LENGTH, value); } }


        /// <summary>
        ///Area for this site (horizontal projections). The exact definition and calculation rules depend on the method of measurement used.
        /// </summary>
        public double? NominalArea { get { return GetElementQuantityAsDouble("NominalArea"); } set { SetOrRemoveQuantity("NominalArea", XbimQuantityTypeEnum.AREA, value); } }

    }
}
