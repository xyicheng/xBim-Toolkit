using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions.SelectTypes;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.SharedBldgElements;

namespace Xbim.DOM.PropertiesQuantities
{
    public class XbimRoofCommonProperties: XbimProperties
    {
        internal XbimRoofCommonProperties(IfcRoof roof) : base(roof, "Pset_RoofCommon") { }

        /// <summary>
        ///Reference ID for this specified type in this project (e.g. type 'A-1')
        /// </summary>
        public string Reference
        {
            get { IfcValue value = GetProperty("Reference"); if (value != null) return (IfcIdentifier)value; return null; }
            set { if (value != null) { IfcIdentifier val = (IfcIdentifier)value; SetProperty("Reference", val); } else { RemoveProperty("Reference"); } }
        }


        /// <summary>
        ///Fire rating for this object. It is given according to the national fire safety classification.
        /// </summary>
        public string FireRating
        {
            get { IfcValue value = GetProperty("FireRating"); if (value != null) return (IfcLabel)value; return null; }
            set { if (value != null) { IfcLabel val = (IfcLabel)value; SetProperty("FireRating", val); } else { RemoveProperty("FireRating"); } }
        }

        /// <summary>
        ///Indication whether the element is designed for use in the exterior (TRUE) or not (FALSE). 
        ///If (TRUE) it is an external element and faces the outside of the building.
        /// </summary>
        public bool? IsExternal
        {
            get { IfcValue value = GetProperty("IsExternal"); if (value != null) return (IfcBoolean)value; return null; }
            set { if (value != null) { IfcBoolean val = (IfcBoolean)value; SetProperty("IsExternal", val); } else { RemoveProperty("IsExternal"); } }
        }


        /// <summary>
        ///Area of the roof projected onto a 2D horizontal plane
        /// </summary>
        public double? ProjectedArea
        {
            get { IfcValue value = GetProperty("ProjectedArea"); if (value != null) return (IfcAreaMeasure )value; return null; }
            set { if (value != null) { IfcAreaMeasure  val = (IfcAreaMeasure )value; SetProperty("ProjectedArea", val); } else { RemoveProperty("ProjectedArea"); } }
        }


        /// <summary>
        ///Total exposed area of the roof
        /// </summary>
        public double? TotalArea
        {
            get { IfcValue value = GetProperty("TotalArea"); if (value != null) return (IfcAreaMeasure )value; return null; }
            set { if (value != null) { IfcAreaMeasure  val = (IfcAreaMeasure )value; SetProperty("TotalArea", val); } else { RemoveProperty("TotalArea"); } }
        }
        
    }
}
