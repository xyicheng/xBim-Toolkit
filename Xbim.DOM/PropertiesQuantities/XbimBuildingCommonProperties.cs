using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.SelectTypes;
using Xbim.Ifc2x3.ProductExtension;

namespace Xbim.DOM.PropertiesQuantities
{
    public class XbimBuildingCommonProperties : XbimProperties
    {
        internal XbimBuildingCommonProperties(IfcBuilding building) : base(building, "Pset_BuildingCommon") { }


        /// <summary>
        ///A unique identifier assigned to a building. A temporary identifier is initially 
        ///assigned at the time of making a planning application. This temporary identifier is 
        ///changed to a permanent identifier when the building is registered into a statutory
        ///buildings and properties database.
        /// </summary>
        public string BuildingID
        {
            get { IfcValue value = GetProperty("BuildingID"); if (value != null) return (IfcIdentifier)value; return null; }
            set { if (value != null) { IfcIdentifier val = (IfcIdentifier)value; SetProperty("BuildingID", val); } else { RemoveProperty("BuildingID"); } }
        }

        /// <summary>
        ///Indicates whether the identity assigned to a building is permanent (= TRUE) or temporary (=FALSE)
        /// </summary>
        public bool? IsPermanentID
        {
            get { IfcValue value = GetProperty("IsPermanentID"); if (value != null) return (IfcBoolean)value; return null; }
            set { if (value != null) { IfcBoolean val = (IfcBoolean)value; SetProperty("IsPermanentID", val); } else { RemoveProperty("IsPermanentID"); } }
        }

        /// <summary>
        ///Main fire use for the building which is assigned from the fire use classification
        ///table as given by the relevant national building code.
        /// </summary>
        public string MainFireUse
        {
            get { IfcValue value = GetProperty("MainFireUse"); if (value != null) return (IfcLabel)value; return null; }
            set { if (value != null) { IfcLabel val = (IfcLabel)value; SetProperty("MainFireUse", val); } else { RemoveProperty("MainFireUse"); } }
        }

        /// <summary>
        ///Ancillary fire use for the building which is assigned from the fire use classification 
        ///table as given by the relevant national building code.
        /// </summary>
        public string AncillaryFireUse
        {
            get { IfcValue value = GetProperty("AncillaryFireUse"); if (value != null) return (IfcLabel)value; return null; }
            set { if (value != null) { IfcLabel val = (IfcLabel)value; SetProperty("AncillaryFireUse", val); } else { RemoveProperty("AncillaryFireUse"); } }
        }

        /// <summary>
        ///Indication whether this object is sprinkler protected (TRUE) or not (FALSE).
        /// </summary>
        public bool? SprinklerProtection
        {
            get { IfcValue value = GetProperty("SprinklerProtection"); if (value != null) return (IfcBoolean)value; return null; }
            set { if (value != null) { IfcBoolean val = (IfcBoolean)value; SetProperty("SprinklerProtection", val); } else { RemoveProperty("SprinklerProtection"); } }
        }

        /// <summary>
        ///Indication whether this object has an automatic sprinkler protection (TRUE) or not (FALSE). 
        ///It should only be given, if the property "SprinklerProtection" is set to TRUE.
        /// </summary>
        public bool? SprinklerProtectionAutomatic
        {
            get { IfcValue value = GetProperty("SprinklerProtectionAutomatic"); if (value != null) return (IfcBoolean)value; return null; }
            set { if (value != null) { IfcBoolean val = (IfcBoolean)value; SetProperty("SprinklerProtectionAutomatic", val); } else { RemoveProperty("SprinklerProtectionAutomatic"); } }
        }

        /// <summary>
        ///Occupancy type for this object. It is defined according to the presiding national building code.
        /// </summary>
        public string OccupancyType
        {
            get { IfcValue value = GetProperty("OccupancyType"); if (value != null) return (IfcLabel)value; return null; }
            set { if (value != null) { IfcLabel val = (IfcLabel)value; SetProperty("OccupancyType", val); } else { RemoveProperty("OccupancyType"); } }
        }

        /// <summary>
        ///Total planned area for the building Used for programming the building.
        /// </summary>
        public double? GrossPlannedArea
        {
            get { IfcValue value = GetProperty("GrossPlannedArea"); if (value != null) return (IfcAreaMeasure)value; return null; }
            set { if (value != null) { IfcAreaMeasure val = (IfcAreaMeasure)value; SetProperty("GrossPlannedArea", val); } else { RemoveProperty("GrossPlannedArea"); } }
        }

        /// <summary>
        ///Captures the number of storeys within a building for those cases where the IfcBuildingStorey 
        ///entity is not used. Note that if IfcBuilingStorey is asserted and the number of storeys in a
        ///building can be determined from it, then this approach should be used in preference to setting
        ///a property for the number of storeys.
        /// </summary>
        public int? NumberOfStoreys
        {
            get { IfcValue value = GetProperty("NumberOfStoreys"); if (value != null) return (int)((IfcInteger)value); return null; }
            set { if (value != null) { IfcInteger val = (IfcInteger)value; SetProperty("NumberOfStoreys", val); } else { RemoveProperty("NumberOfStoreys"); } }
        }

        /// <summary>
        ///Year of construction of this building, including expected year of completion.
        /// </summary>
        public string YearOfConstruction
        {
            get { IfcValue value = GetProperty("YearOfConstruction"); if (value != null) return (IfcLabel)value; return null; }
            set { if (value != null) { IfcLabel val = (IfcLabel)value; SetProperty("YearOfConstruction", val); } else { RemoveProperty("YearOfConstruction"); } }
        }

        /// <summary>
        ///This builing is listed as a historic building (TRUE), or not (FALSE), or unknown.
        /// </summary>
        public bool? IsLandmarked
        {
            get { IfcValue value = GetProperty("IsLandmarked"); if (value != null) return (IfcLogical)value; return null; }
            set { if (value != null) { IfcLogical val = (IfcLogical)value; SetProperty("IsLandmarked", val); } else { RemoveProperty("IsLandmarked"); } }
        }


    }
}
