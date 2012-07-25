using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.XbimExtensions.SelectTypes;

namespace Xbim.DOM.PropertiesQuantities
{
    public class XbimDoorCommonProperties : XbimProperties
    {
        internal XbimDoorCommonProperties(IfcDoor door) : base(door, "Pset_DoorCommon") { }

        /// <summary>
        ///Reference ID for this specified type in this project (e.g. type 'A-1')
        /// </summary>
        public string Reference
        {
            get { IfcValue value = GetProperty("Reference"); if (value != null) return (IfcIdentifier)value; return null; }
            set { if (value != null) { IfcIdentifier val = (IfcIdentifier)value; SetProperty("Reference", val); } else { RemoveProperty("Reference"); } }
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
        ///Acoustic rating for this object. It is giving according to the 
        ///national building code. It indicates the sound transmission resistance 
        ///of this object by an index ration (instead of providing full sound absorbtion values).
        /// </summary>
        public string AcousticRating
        {
            get { IfcValue value = GetProperty("AcousticRating"); if (value != null) return (IfcLabel)value; return null; }
            set { if (value != null) { IfcLabel val = (IfcLabel)value; SetProperty("AcousticRating", val); } else { RemoveProperty("AcousticRating"); } }
        }

        /// <summary>
        ///Index based rating system indicating security level. It is giving according to the national building code.
        /// </summary>
        public string SecurityRating
        {
            get { IfcValue value = GetProperty("SecurityRating"); if (value != null) return (IfcLabel)value; return null; }
            set { if (value != null) { IfcLabel val = (IfcLabel)value; SetProperty("SecurityRating", val); } else { RemoveProperty("SecurityRating"); } }
        }

        /// <summary>
        ///Indication whether the element is designed for use in the exterior (TRUE) or not (FALSE). If (TRUE) it is an external element and faces the outside of the building.
        /// </summary>
        public bool? IsExternal
        {
            get { IfcValue value = GetProperty("IsExternal"); if (value != null) return (IfcBoolean)value; return null; }
            set { if (value != null) { IfcBoolean val = (IfcBoolean)value; SetProperty("IsExternal", val); } else { RemoveProperty("IsExternal"); } }
        }

        /// <summary>
        ///Infiltration flowrate of outside air for the filler object based on the area of the filler object at a pressure level of 50 Pascals. It shall be used, if the length of all joints is unknown.
        /// </summary>
        public double? Infiltration
        {
            get { IfcValue value = GetProperty("Infiltration"); if (value != null) return (IfcVolumetricFlowRateMeasure )value; return null; }
            set { if (value != null) { IfcVolumetricFlowRateMeasure  val = (IfcVolumetricFlowRateMeasure )value; SetProperty("Infiltration", val); } else { RemoveProperty("Infiltration"); } }
        }

        /// <summary>
        ///Thermal transmittance coefficient (U-Value) of a material. It applies to the total door construction.
        /// </summary>
        public double? ThermalTransmittance
        {
            get { IfcValue value = GetProperty("ThermalTransmittance"); if (value != null) return (IfcThermalTransmittanceMeasure )value; return null; }
            set { if (value != null) { IfcThermalTransmittanceMeasure  val = (IfcThermalTransmittanceMeasure )value; SetProperty("ThermalTransmittance", val); } else { RemoveProperty("ThermalTransmittance"); } }
        }

        /// <summary>
        ///Fraction of the glazing area relative to the total area of the filling element. It shall be used, if the glazing area is not given separately for all panels within the filling element. 
        /// </summary>
        public double? GlazingAreaFraction
        {
            get { IfcValue value = GetProperty("GlazingAreaFraction"); if (value != null) return (IfcPositiveRatioMeasure)value; return null; }
            set { if (value != null) { IfcPositiveRatioMeasure val = (IfcPositiveRatioMeasure)value; SetProperty("GlazingAreaFraction", val); } else { RemoveProperty("GlazingAreaFraction"); } }
        }

        /// <summary>
        ///Indication that this object is designed to be accessible by the handicapped. It is giving according to the requirements of the national building code. 
        /// </summary>
        public bool? HandicapAccessible
        {
            get { IfcValue value = GetProperty("HandicapAccessible"); if (value != null) return (IfcBoolean)value; return null; }
            set { if (value != null) { IfcBoolean val = (IfcBoolean)value; SetProperty("HandicapAccessible", val); } else { RemoveProperty("HandicapAccessible"); } }
        }

        /// <summary>
        ///Indication whether this object is designed to serve as an exit in the case of fire (TRUE) or not (FALSE). Here it defines an exit door in accordance to the national building code.
        /// </summary>
        public bool? FireExit
        {
            get { IfcValue value = GetProperty("FireExit"); if (value != null) return (IfcBoolean)value; return null; }
            set { if (value != null) { IfcBoolean val = (IfcBoolean)value; SetProperty("FireExit", val); } else { RemoveProperty("FireExit"); } }
        }

        /// <summary>
        ///Indication whether this object is designed to close automatically after use (TRUE) or not (FALSE).
        /// </summary>
        public bool? SelfClosing
        {
            get { IfcValue value = GetProperty("SelfClosing"); if (value != null) return (IfcBoolean)value; return null; }
            set { if (value != null) { IfcBoolean val = (IfcBoolean)value; SetProperty("SelfClosing", val); } else { RemoveProperty("SelfClosing"); } }
        }

        /// <summary>
        ///Indication whether the object is designed to provide a smoke stop (TRUE) or not (FALSE).
        /// </summary>
        public bool? SmokeStop
        {
            get { IfcValue value = GetProperty("SmokeStop"); if (value != null) return (IfcBoolean)value; return null; }
            set { if (value != null) { IfcBoolean val = (IfcBoolean)value; SetProperty("SmokeStop", val); } else { RemoveProperty("SmokeStop"); } }
        }
    }
}
