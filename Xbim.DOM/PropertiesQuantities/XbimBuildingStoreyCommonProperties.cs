using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.SelectTypes;
using Xbim.Ifc.MeasureResource;

namespace Xbim.DOM.PropertiesQuantities
{
    public class XbimBuildingStoreyCommonProperties : XbimProperties
    {
        internal XbimBuildingStoreyCommonProperties(IfcBuildingStorey storey) : base(storey, "Pset_BuildingStoreyCommon") { }


        /// <summary>
        ///Indication whether this building storey is an entrance level to the building (TRUE), or (FALSE) if otherwise.
        /// </summary>
        public bool? EntranceLevel
        {
            get { IfcValue value = GetProperty("EntranceLevel"); if (value != null) return (IfcBoolean)value; return null; }
            set { if (value != null) { IfcBoolean val = (IfcBoolean)value; SetProperty("EntranceLevel", val); } else { RemoveProperty("EntranceLevel"); } }
        }


        /// <summary>
        ///Indication whether this building storey is fully above ground (TRUE), or below ground (FALSE), 
        ///or partially above and below ground (UNKNOWN) - as in sloped terrain.
        /// </summary>
        public bool? AboveGround
        {
            get { IfcValue value = GetProperty("AboveGround"); if (value != null) return (IfcLogical)value; return null; }
            set { if (value != null) { IfcLogical val = (IfcLogical)value; SetProperty("AboveGround", val); } else { RemoveProperty("AboveGround"); } }
        }


        /// <summary>
        ///Indication whether this object is sprinkler protected (true) or not (false).
        /// </summary>
        public bool? SprinklerProtection
        {
            get { IfcValue value = GetProperty("SprinklerProtection"); if (value != null) return (IfcBoolean)value; return null; }
            set { if (value != null) { IfcBoolean val = (IfcBoolean)value; SetProperty("SprinklerProtection", val); } else { RemoveProperty("SprinklerProtection"); } }
        }


        /// <summary>
        ///Indication whether this object has an automatic sprinkler protection (true) or not (false). 
        ///It should only be given, if the property "SprinklerProtection" is set to TRUE.
        /// </summary>
        public bool? SprinklerProtectionAutomatic
        {
            get { IfcValue value = GetProperty("SprinklerProtectionAutomatic"); if (value != null) return (IfcBoolean)value; return null; }
            set { if (value != null) { IfcBoolean val = (IfcBoolean)value; SetProperty("SprinklerProtectionAutomatic", val); } else { RemoveProperty("SprinklerProtectionAutomatic"); } }
        }


        /// <summary>
        ///Total planned area for the building storey. Used for programming the building storey.
        /// </summary>
        public double? GrossAreaPlanned
        {
            get { IfcValue value = GetProperty("GrossAreaPlanned"); if (value != null) return (IfcAreaMeasure)value; return null; }
            set { if (value != null) { IfcAreaMeasure val = (IfcAreaMeasure)value; SetProperty("GrossAreaPlanned", val); } else { RemoveProperty("GrossAreaPlanned"); } }
        }


        /// <summary>
        ///Total planned net area for the building storey. Used for programming the building storey.
        /// </summary> 
        public double? NetAreaPlanned
        {
            get { IfcValue value = GetProperty("NetAreaPlanned"); if (value != null) return (IfcAreaMeasure)value; return null; }
            set { if (value != null) { IfcAreaMeasure val = (IfcAreaMeasure)value; SetProperty("NetAreaPlanned", val); } else { RemoveProperty("NetAreaPlanned"); } }
        }


    }
}
