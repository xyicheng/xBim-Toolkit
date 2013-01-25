using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc2x3.ProductExtension;

namespace Xbim.DOM.PropertiesQuantities
{
    public class XbimSpaceQuantities : XbimQuantities
    {
        internal XbimSpaceQuantities(IfcSpace space)
            : base(space, "SpaceQuantities")
        {
        }

        /// <summary>
        /// Floor Height (without flooring) to Ceiling height (without suspended ceiling) for this space 
        /// (measured from top of slab of this space to the bottom of slab of space above); the average 
        /// shall be taken if room shape is not prismatic.
        /// </summary>
        public double? NominalHeight { get { return GetElementQuantityAsDouble("NominalHeight"); } set { SetOrRemoveQuantity("NominalHeight", XbimQuantityTypeEnum.LENGTH, value); } }

        /// <summary>
        /// Clear Height between floor level (including finish) and ceiling level (including finish and sub construction) 
        /// of this space; the average shall be taken if room shape is not prismatic.
        /// </summary>
        public double? ClearHeight { get { return GetElementQuantityAsDouble("ClearHeight"); } set { SetOrRemoveQuantity("ClearHeight", XbimQuantityTypeEnum.LENGTH, value); } }

        /// <summary>
        /// Calculated gross perimeter at the floor level of this space. It all sides of the space, 
        /// including those parts of the perimeter that are created by virtual boundaries and openings. 
        /// The exact definition and calculation rules depend on the method of measurement used.
        /// </summary>
        public double? GrossPerimeter { get { return GetElementQuantityAsDouble("GrossPerimeter"); } set { SetOrRemoveQuantity("GrossPerimeter", XbimQuantityTypeEnum.LENGTH, value); } }

        /// <summary>
        /// Calculated net perimeter at the floor level of this space. It normally excludes those parts
        /// of the perimeter that are created by by virtual boundaries and openings. The exact definition 
        /// and calculation rules depend on the method of measurement used.
        /// </summary>
        public double? NetPerimeter { get { return GetElementQuantityAsDouble("NetPerimeter"); } set { SetOrRemoveQuantity("NetPerimeter", XbimQuantityTypeEnum.LENGTH, value); } }

        /// <summary>
        /// Calculated sum of all floor areas covered by the space. It normally includes the area covered 
        /// by elementsinside the space (columns, inner walls, etc.). The exact definition and calculation
        /// rules depend on the method of measurement used.	
        /// </summary>
        public double? GrossFloorArea
        {
            get
            { return GetElementQuantityAsDouble("GrossFloorArea"); }
            set
            { SetOrRemoveQuantity("GrossFloorArea", XbimQuantityTypeEnum.AREA, value); }
        }

        /// <summary>
        /// Calculated sum of all usable floor areas covered by the space. It normally excludes the area 
        /// covered by elements inside the space (columns, inner walls, etc.), floor openings, or other 
        /// protruding elements. Special rules apply for areas that have a low headroom. The exact definition
        /// and calculation rules depend on the method of measurement used.
        /// </summary>
        public double? NetFloorArea { get { return GetElementQuantityAsDouble("NetFloorArea"); } set { SetOrRemoveQuantity("NetFloorArea", XbimQuantityTypeEnum.AREA, value); } }

        /// <summary>
        /// Calculated sum of all ceiling areas of the space. It normally includes the area covered by 
        /// elementsinside the space (columns, inner walls, etc.). The ceiling area is the real (and not the projected) 
        /// area (e.g. in case of sloped ceilings). The exact definition and calculation rules depend on 
        /// the method of measurement used.
        /// </summary>
        public double? GrossCeilingArea { get { return GetElementQuantityAsDouble("GrossCeilingArea"); } set { SetOrRemoveQuantity("GrossCeilingArea", XbimQuantityTypeEnum.AREA, value); } }


        /// <summary>
        ///Calculated sum of all ceiling areas covered by the space. It normally excludes the area covered 
        ///by elements inside the space (columns, inner walls, etc.) or by ceiling openings. The ceiling area 
        ///is the real (and not the projected) area (e.g. in case of sloped ceilings). The exact definition and 
        ///calculation rules depend on the method of measurement used.
        /// </summary>
        public double? NetCeilingArea { get { return GetElementQuantityAsDouble("NetCeilingArea"); } set { SetOrRemoveQuantity("NetCeilingArea", XbimQuantityTypeEnum.AREA, value); } }


        /// <summary>
        ///Calculated sum of all wall areas bounded by the space. It normally includes the area covered by 
        ///elementsinside the wall area (doors, windows, other openings, etc.). The exact definition and 
        ///calculation rules depend on the method of measurement used
        /// </summary>
        public double? GrossWallArea { get { return GetElementQuantityAsDouble("GrossWallArea"); } set { SetOrRemoveQuantity("GrossWallArea", XbimQuantityTypeEnum.AREA, value); } }


        /// <summary>
        ///Calculated sum of all wall areas bounded by the space. It normally excludes the area coveredby 
        ///elements inside the wall area (doors, windows, other openings, etc.). Special rules apply for 
        ///areas that have a low headroom. The exact definition and calculation rules depend on the 
        ///method of measurement used.	
        /// </summary>
        public double? NetWallArea { get { return GetElementQuantityAsDouble("NetWallArea"); } set { SetOrRemoveQuantity("NetWallArea", XbimQuantityTypeEnum.AREA, value); } }


        /// <summary>
        ///Calculated gross volume of all areas enclosed by the space (normally including the volume 
        ///of construction elements inside the space). The exact definition and calculation rules 
        ///depend on the method of measurement used.	
        /// </summary>
        public double? GrossVolume { get { return GetElementQuantityAsDouble("GrossVolume"); } set { SetOrRemoveQuantity("GrossVolume", XbimQuantityTypeEnum.VOLUME, value); } }


        /// <summary>
        ///Calculated net volume of all areas enclosed by the space (normally excluding the volume 
        ///of construction elements inside the space). The exact definition and calculation rules depend 
        ///on the method of measurement used.
        /// </summary>
        public double? NetVolume { get { return GetElementQuantityAsDouble("NetVolume"); } set { SetOrRemoveQuantity("NetVolume", XbimQuantityTypeEnum.VOLUME, value); } }

    }
}
