using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.SelectTypes;

namespace Xbim.DOM.PropertiesQuantities
{
    /// <summary>
    /// Definition from IAI: Properties common to the definition of all occurrences of IfcSite. Please note that several 
    /// site attributes are handled directly at the IfcSite instance, the site number (or short name) by IfcSite.Name, 
    /// the site name (or long name) by IfcSite.LongName, and the description (or comments) by IfcSite.Description. 
    /// The land title number is also given as an explicit attribute IfcSite.LandTitleNumber. Actual site quantities, 
    /// like site perimeter, site area and site volume are provided by IfcElementQuantities, and site classification 
    /// according to national building code by IfcClassificationReference. The global positioning of the site in terms 
    /// of Northing and Easting and height above sea level datum is given by IfcSite.RefLongitude, IfcSite.RefLatitude, 
    /// IfcSite.RefElevation and the postal address by IfcSite.SiteAddress.
    /// </summary>
    public class XbimSiteCommonProperties : XbimProperties
    {
        internal XbimSiteCommonProperties(IfcSite site) : base(site, "Pset_SiteCommon") { }

        /// <summary>
        ///The area of utilization expressed as a minimum value and a maximum value - according to local building codes.
        /// </summary>
        public double? BuildableArea
        {
            get { IfcValue value = GetProperty("BuildableArea"); if (value != null) return (IfcAreaMeasure)value; return null; }
            set { if (value != null) { IfcAreaMeasure val = (IfcAreaMeasure)value; SetProperty("BuildableArea", val); } else { RemoveProperty("BuildableArea"); } }
        }

        /// <summary>
        ///Total area of the site - masured according to local building codes.
        /// </summary>
        public double? TotalArea
        {
            get { IfcValue value = GetProperty("TotalArea"); if (value != null) return (IfcAreaMeasure)value; return null; }
            set { if (value != null) { IfcAreaMeasure val = (IfcAreaMeasure)value; SetProperty("TotalArea", val); } else { RemoveProperty("TotalArea"); } }
        }


        /// <summary>
        ///Calculated maximum height of buildings on this site - according to local building codes.
        /// </summary>
        public double? BuildingHeightLimit
        {
            get { IfcValue value = GetProperty("BuildingHeightLimit"); if (value != null) return (IfcPositiveLengthMeasure)value; return null; }
            set { if (value != null) { IfcPositiveLengthMeasure val = (IfcPositiveLengthMeasure)value; SetProperty("BuildingHeightLimit", val); } else { RemoveProperty("BuildingHeightLimit"); } }
        }

    }
}
