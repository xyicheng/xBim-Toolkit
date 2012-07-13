using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.SelectTypes;

namespace Xbim.DOM.PropertiesQuantities
{
    public class XbimPlateCommonProperties : XbimProperties
    {
        internal XbimPlateCommonProperties(IfcPlate plate) : base(plate, "Pset_PlateCommon") { }


        /// <summary>
        ///Reference ID for this specified type in this project (e.g. type 'A-1')
        /// </summary>
        public string Reference
        {
            get { IfcValue value = GetProperty("Reference"); if (value != null) return (IfcIdentifier)value; return null; }
            set { if (value != null) { IfcIdentifier val = (IfcIdentifier)value; SetProperty("Reference", val); } else { RemoveProperty("Reference"); } }
        }


        /// <summary>
        ///Indication whether the element is designed for use in the exterior (TRUE) or not (FALSE). If (TRUE) it is an external element and faces the outside of the building
        /// </summary>
        public bool? IsExternal
        {
            get { IfcValue value = GetProperty("IsExternal"); if (value != null) return (IfcBoolean)value; return null; }
            set { if (value != null) { IfcBoolean val = (IfcBoolean)value; SetProperty("IsExternal", val); } else { RemoveProperty("IsExternal"); } }
        }


        /// <summary>
        ///Indicates whether the object is intended to carry loads (TRUE) or not (FALSE)
        /// </summary>
        public bool? LoadBearing
        {
            get { IfcValue value = GetProperty("LoadBearing"); if (value != null) return (IfcBoolean)value; return null; }
            set { if (value != null) { IfcBoolean val = (IfcBoolean)value; SetProperty("LoadBearing", val); } else { RemoveProperty("LoadBearing"); } }
        }


        /// <summary>
        ///Acoustic rating for this object. It is giving according to the national building code.
        ///It indicates the sound transmission resistance of this object by an index ration (instead of providing full sound absorbtion values).
        /// </summary>
        public string AcousticRating
        {
            get { IfcValue value = GetProperty("AcousticRating"); if (value != null) return (IfcLabel)value; return null; }
            set { if (value != null) { IfcLabel val = (IfcLabel)value; SetProperty("AcousticRating", val); } else { RemoveProperty("AcousticRating"); } }
        }


        /// <summary>
        ///Fire rating for this object. It is given according to the national fire safety classification
        /// </summary>
        public string FireRating
        {
            get { IfcValue value = GetProperty("FireRating"); if (value != null) return (IfcLabel)value; return null; }
            set { if (value != null) { IfcLabel val = (IfcLabel)value; SetProperty("FireRating", val); } else { RemoveProperty("FireRating"); } }
        }


        /// <summary>
        ///Thermal transmittance coefficient (U-Value) of a material. It applies to the total door construction
        /// </summary>
        public double? ThermalTransmittance
        {
            get { IfcValue value = GetProperty("ThermalTransmittance"); if (value != null) return (IfcThermalTransmittanceMeasure )value; return null; }
            set { if (value != null) { IfcThermalTransmittanceMeasure  val = (IfcThermalTransmittanceMeasure )value; SetProperty("ThermalTransmittance", val); } else { RemoveProperty("ThermalTransmittance"); } }
        }
        
    }
}
