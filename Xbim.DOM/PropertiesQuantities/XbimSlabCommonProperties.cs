using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.SelectTypes;
using Xbim.Ifc2x3.MeasureResource;

namespace Xbim.DOM.PropertiesQuantities
{
    public class XbimSlabCommonProperties : XbimProperties
    {
        internal XbimSlabCommonProperties(IfcSlab slab) : base(slab, "Pset_SlabCommon") { }


        /// <summary>
        ///Acoustic rating for this object. It is giving according to the national building code. It indicates the sound transmission resistance of this object by an index ration (instead of providing full sound absorbtion values
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
        ///Indication whether the object is made from combustible material (TRUE) or not (FALSE).
        /// </summary>
        public bool? Combustible
        {
            get { IfcValue value = GetProperty("Combustible"); if (value != null) return (IfcBoolean)value; return null; }
            set { if (value != null) { IfcBoolean val = (IfcBoolean)value; SetProperty("Combustible", val); } else { RemoveProperty("Combustible"); } }
        }


        /// <summary>
        ///Indication on how the flames spread around the surface, It is given according to the national building code that governs the fire behaviour for materials
        /// </summary>
        public string SurfaceSpreadOfFlame
        {
            get { IfcValue value = GetProperty("SurfaceSpreadOfFlame"); if (value != null) return (IfcLabel)value; return null; }
            set { if (value != null) { IfcLabel val = (IfcLabel)value; SetProperty("SurfaceSpreadOfFlame", val); } else { RemoveProperty("SurfaceSpreadOfFlame"); } }
        }


        /// <summary>
        ///Thermal transmittance coefficient (U-Value) of a material. Here the total thermal transmittance coefficient through the slab (including all materials)
        /// </summary>
        public double? ThermalTransmittance
        {
            get { IfcValue value = GetProperty("ThermalTransmittance"); if (value != null) return (IfcThermalTransmittanceMeasure )value; return null; }
            set { if (value != null) { IfcThermalTransmittanceMeasure  val = (IfcThermalTransmittanceMeasure )value; SetProperty("ThermalTransmittance", val); } else { RemoveProperty("ThermalTransmittance"); } }
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
        ///Indication whether the object is designed to serve as a fire compartmentation (TRUE) or not (FALSE).
        /// </summary>
        public bool? Compartmentation
        {
            get { IfcValue value = GetProperty("Compartmentation"); if (value != null) return (IfcBoolean)value; return null; }
            set { if (value != null) { IfcBoolean val = (IfcBoolean)value; SetProperty("Compartmentation", val); } else { RemoveProperty("Compartmentation"); } }
        }


        /// <summary>
        ///Angle of the slab to the horizontal when used as a component for the roof (specified as 0 degrees or not asserted for cases where the slab is not used as a roof component).
        /// </summary>
        public double? PitchAngle
        {
            get { IfcValue value = GetProperty("PitchAngle"); if (value != null) return (IfcPlaneAngleMeasure )value; return null; }
            set { if (value != null) { IfcPlaneAngleMeasure  val = (IfcPlaneAngleMeasure )value; SetProperty("PitchAngle", val); } else { RemoveProperty("PitchAngle"); } }
        }
        
    }
}
