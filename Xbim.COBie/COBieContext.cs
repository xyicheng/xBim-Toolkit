using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;
using Xbim.Ifc.SharedBldgElements;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.SharedComponentElements;
using Xbim.Ifc.StructuralElementsDomain;
using Xbim.Ifc.SharedBldgServiceElements;
using Xbim.Ifc.HVACDomain;
using Xbim.Ifc.ElectricalDomain;

namespace Xbim.COBie
{
	/// <summary>
	/// Context for generating COBie data from one or more IFC models
	/// </summary>
	public class COBieContext : IDisposable
	{


        public Dictionary<long, string> EMails { get; private set; }
        public Dictionary<string, string> COBieGlobalValues { get; private set; }

        public COBieContext(ReportProgressDelegate progressHandler = null)
		{
            if (progressHandler != null)
            {
                _progress = progressHandler;
                this.ProgressStatus += progressHandler;
            }
			Models = new List<IModel>();

            EMails = new Dictionary<long, string>();
            COBieGlobalValues = new Dictionary<string, string>();
            COBieGlobalValues.Add("DEFAULTDATE", DateTime.Now.ToString(Constants.DATE_FORMAT));
		}

		/// <summary>
		/// Collection of models to interrogate for data to populate the COBie worksheets
		/// </summary>
        /// <remarks>Due to be obsoleted. Will merge models explicitly</remarks>
		public ICollection<IModel> Models { get; set; }


        private IModel _model = null;
        /// <summary>
        /// Gets the model defined in this context to generate COBie data from
        /// </summary>
        public IModel Model
        {
            get
            {
                if (_model == null)
                {
                    _model = Models.First();
                }
                return _model;
            }
        }

		/// <summary>
		/// The pick list to use to cross-reference fields in the COBie worksheets
		/// </summary>
		public COBiePickList PickList { get; set; }

        private ReportProgressDelegate _progress = null;


        public event ReportProgressDelegate ProgressStatus;

        /// <summary>
        /// Updates the delegates with the current percentage complete
        /// </summary>
        /// <param name="message"></param>
        /// <param name="total"></param>
        /// <param name="current"></param>
        public void UpdateStatus(string message, int total = 0, int current = 0)
        {
            decimal percent = 0;
            if (total != 0 && current > 0)
            {
                message = string.Format("{0} [{1}/{2}]", message, current, total);
                percent = (decimal)current / total * 100;
            }

            ProgressStatus((int)percent, message);
        }

        public void Dispose()
        {
            if (_progress != null)
            {
                ProgressStatus -= _progress;
                _progress = null;
            }
        }

        //================Global filter properties===============

        /// <summary>
        /// List of field names that are to be excluded from Attributes sheet with equal compare
        /// </summary>
        private List<string> _commonAttExcludesEq = new List<string>()
        {   "MethodOfMeasurement",  "Omniclass Number",     "Assembly Code",                "Assembly Description",     "Uniclass Description",     "Uniclass Code", 
            "Category Code",    "Category Description",     "Classification Description",   "Classification Code",      "Name",                     "Description", 
            "Hot Water Radius", "Host",                     "Limit Offset",                 "Recepticles",              "Mark",     "Workset",  "Keynote",  "VisibleOnPlan",
            "Edited by", "Elevation Base"
            
            //"Zone Base Offset", "Upper Limit",   "Line Pattern", "Symbol","Window Inset", "Radius", "Phase Created","Phase", //old ones might need to put back in
        };

        /// <summary>
        /// List of property names that are to be excluded from Attributes sheet with equal compare
        /// </summary>
        public List<string> CommonAttExcludesEq
        {
            get { return _commonAttExcludesEq; }
            private set { _commonAttExcludesEq = value; }
        }
        

        /// <summary>
        /// List of field names that are to be excluded from Attributes sheet with start with compare
        /// </summary>
        private List<string> _commonAttExcludesStartWith = new List<string>()
        {   "Omniclass Title",  "Half Oval",    "Level",    "Outside Diameter", "Outside Radius", "Moves With"
        };

        /// <summary>
        /// List of property names that are to be excluded from Attributes sheet with start with compare
        /// </summary>
        public List<string> CommonAttExcludesStartWith
        {
            get { return _commonAttExcludesStartWith; }
            private set { _commonAttExcludesStartWith = value; }
        }

        /// <summary>
        /// List of property names that are to be excluded from Attributes sheet with contains with compare
        /// </summary>
        private List<string> _commonAttExcludesContains = new List<string>()
        {   "AssetAccountingType",  "GSA BIM Area",     "Height",   "Length",   "Size",     "Lighting Calculation Workplan",    "Offset",   "Omniclass"
        };

        /// <summary>
        ///  List of property names that are to be excluded from Attributes sheet with contains with compare
        /// </summary>
        public List<string> CommonAttExcludesContains
        {
            get { return _commonAttExcludesContains; }
            private set { _commonAttExcludesContains = value; }
        }

        //------------------Object Type Filters--------------------------------

        /// <summary>
        /// List of component class types to exclude from selection
        /// </summary>
        private List<Type> _componentExcludedTypes = new List<Type>{  typeof(IfcWall),
                                                        typeof(IfcAnnotation),
                                                        typeof(IfcWallStandardCase),
                                                        typeof(IfcSlab),
                                                        typeof(IfcBeam),
                                                        typeof(IfcSpace),
                                                        typeof(IfcBuildingStorey),
                                                        typeof(IfcBuilding),
                                                        typeof(IfcSite),
                                                        typeof(IfcProject),
                                                        typeof(IfcColumn),
                                                        typeof(IfcMember),
                                                        typeof(IfcPlate),
                                                        typeof(IfcRailing),
                                                        typeof(IfcStairFlight),
                                                        typeof(IfcCurtainWall),
                                                        typeof(IfcRampFlight),
                                                        typeof(IfcVirtualElement),
                                                        typeof(IfcFeatureElement),
                                                        typeof(IfcFastener),
                                                        typeof(IfcMechanicalFastener),
                                                        typeof(IfcElementAssembly),
                                                        typeof(IfcBuildingElementPart),
                                                        typeof(IfcReinforcingBar),
                                                        typeof(IfcReinforcingMesh),
                                                        typeof(IfcTendon),
                                                        typeof(IfcTendonAnchor),
                                                        typeof(IfcFooting),
                                                        typeof(IfcPile),
                                                        typeof(IfcRamp),
                                                        typeof(IfcRoof),
                                                        typeof(IfcStair),
                                                        typeof(IfcFlowFitting),
                                                        typeof(IfcFlowSegment),
                                                        typeof(IfcDistributionPort), 
                                                        typeof(IfcFeatureElementAddition), 
                                                        typeof(IfcProjectionElement), 
                                                        typeof(IfcCovering)
                                                        //typeof(IfcColumnStandardCase), //IFC2x Edition 4.
                                                        //typeof(IfcMemberStandardCase), //IFC2x Edition 4.
                                                        //typeof(IfcPlateStandardCase), //IFC2x Edition 4.
                                                        //typeof(IfcSlabElementedCase), //IFC2x Edition 4.
                                                        //typeof(IfcSlabElementedCase), //IFC2x Edition 4.
                                                        //typeof(IfcWallElementedCase), //IFC2x Edition 4.
                                                        //typeof(IfcCableCarrierSegment), //IFC2x Edition 4.
                                                        //typeof(IfcCableSegment), //IFC2x Edition 4.
                                                        //typeof(IfcDuctSegment), //IFC2x Edition 4.
                                                        //typeof(IfcPipeSegment), //IFC2x Edition 4.
                                                        };

        /// <summary>
        /// List of component class types to exclude from selection
        /// </summary>
        public List<Type> ComponentExcludeTypes
        {
            get { return _componentExcludedTypes; }
        }

        //TODO: after IfcRampType and IfcStairType are implemented then add to excludedTypes list
        private List<Type> _typeObjectExcludedTypes = new List<Type>{  typeof(IfcTypeProduct),
                                                            typeof(IfcElementType),
                                                            typeof(IfcBeamType),
                                                            typeof(IfcColumnType),
                                                            typeof(IfcCurtainWallType),
                                                            typeof(IfcMemberType),
                                                            typeof(IfcPlateType),
                                                            typeof(IfcRailingType),
                                                            typeof(IfcRampFlightType),
                                                            typeof(IfcSlabType),
                                                            typeof(IfcStairFlightType),
                                                            typeof(IfcWallType),
                                                            typeof(IfcDuctFittingType ),
                                                            typeof(IfcJunctionBoxType ),
                                                            typeof(IfcPipeFittingType),
                                                            typeof(IfcCableCarrierSegmentType),
                                                            typeof(IfcCableSegmentType),
                                                            typeof(IfcDuctSegmentType),
                                                            typeof(IfcPipeSegmentType),
                                                            typeof(IfcFastenerType),
                                                            typeof(IfcSpaceType),
                                                            //typeof(Xbim.Ifc.SharedBldgElements.IfcRampType), //IFC2x Edition 4.
                                                            //typeof(IfcStairType), //IFC2x Edition 4.
                                                             };
        /// <summary>
        /// List of type object class types to exclude from selection
        /// </summary>
        public List<Type> TypeObjectExcludeTypes
        {
            get { return _typeObjectExcludedTypes; }
        }




        //list or classes to exclude if the related object of the IfcRelAggregates is one of these types
        private List<Type> _assemblyExcludeTypes = new List<Type>{  typeof(IfcSite),
                                                        typeof(IfcProject),
                                                        typeof(IfcBuilding),
                                                        typeof(IfcBuildingStorey)
                                                        };

        /// <summary>
        /// List of type object class types to exclude from selection
        /// </summary>
        public List<Type> AssemblyExcludeTypes
        {
            get
            {
                List<Type> temp = new List<Type>();
                temp.AddRange(_assemblyExcludeTypes);
                //Assemblies should only be shown from the components and type sheets so add there exclusions 
                temp.AddRange(_typeObjectExcludedTypes);
                temp.AddRange(_componentExcludedTypes);
                return temp;
            }
        }

        /// <summary>
        /// List of property names that are to be excluded from the Attributes generated from the Component sheet with equal compare
        /// </summary>
        private List<string> _componentAttExcludesEq = new List<string>() 
        {   "Circuit NumberSystem Type", "System Name",  "AssetIdentifier", "BarCode", "TagNumber", "WarrantyStartDate", "InstallationDate", "SerialNumber"
        };

        /// <summary>
        /// List of property names that are to be excluded from the Attributes generated from the Component sheet with equal compare
        /// </summary>
        public List<string> ComponentAttExcludesEq
        {
            get { return _componentAttExcludesEq; }
        }

        /// <summary>
        /// List of property names that are to be excluded from the Attributes generated from the Component sheet with contains compare
        /// </summary>
        private List<string> _componentAttExcludesContains = new List<string>() { "Roomtag", "RoomTag", "GSA BIM Area", "Length", "Height", "Render Appearance", "Arrow at End" }; //"Tag",
        
        /// <summary>
        /// List of property names that are to be excluded from the Attributes generated from the Component sheet with contains compare
        /// </summary>
        public List<string> ComponentAttExcludesContains
        {
            get { return _componentAttExcludesContains; }
        }

        
        //Facility 
        private List<string> _facilityAttExcludesEq = new List<string> { "Phase" }; //excludePropertyValueNames

        /// <summary>
        /// List of property names that are to be excluded from the Attributes generated from the Facility sheet with equal compare
        /// </summary>
        public List<string> FacilityAttExcludesEq
        {
            get { return _facilityAttExcludesEq; }
        }

        private List<string> _facilityAttExcludesContains = new List<string> { "Roomtag", "RoomTag", "Tag", "GSA BIM Area", "Length", "Width", "Height" }; //excludePropertyValueNamesWildcard

        /// <summary>
        /// List of property names that are to be excluded from the Attributes generated from the Facility sheet with contains compare
        /// </summary>
        public List<string> FacilityAttExcludesContains
        {
            get { return _facilityAttExcludesContains; }
        }

        //Floor
        private List<string> _floorAttExcludesEq = new List<string> { "Name", "Line Weight", "Color", 
                                                          "Colour",   "Symbol at End 1 Default", 
                                                          "Symbol at End 2 Default", "Automatic Room Computation Height", "Elevation", "Storey Height" }; //excludePropertyValueNames

        /// <summary>
        /// List of property names that are to be excluded from the Attributes generated from the Floor sheet with equal compare
        /// </summary>
        public List<string> FloorAttExcludesEq
        {
            get { return _floorAttExcludesEq; }
        }

        private List<string> _floorAttExcludesContains = new List<string> { "Roomtag", "RoomTag", "Tag", "GSA BIM Area", "Length", "Width" }; //excludePropertyValueNamesWildcard

        /// <summary>
        /// List of property names that are to be excluded from the Attributes generated from the Floor sheet with contains compare
        /// </summary>
        public List<string> FloorAttExcludesContains
        {
            get { return _floorAttExcludesContains; }
        }

        //Space
        
        private List<string> _spaceAttExcludesEq = new List<string> { "Area", "Number", "UsableHeight", "RoomTag", "Room Tag" }; //excludePropertyValueNames

        /// <summary>
        /// List of property names that are to be excluded from the Attributes generated from the Space sheet with equal compare
        /// </summary>
        public List<string> SpaceAttExcludesEq
        {
            get { return _spaceAttExcludesEq; }
        }

        private List<string> _spaceAttExcludesContains = new List<string> { "ZoneName", "Category", "Length", "Width" }; //exclude part names //excludePropertyValueNamesWildcard

        /// <summary>
        /// List of property names that are to be excluded from the Attributes generated from the Space sheet with contains compare
        /// </summary>
        public List<string> SpaceAttExcludesContains
        {
            get { return _spaceAttExcludesContains; }
        }

        private List<string> _spaceAttExcludesPropertSetEq = new List<string>() { "BaseQuantities" }; //excludePropertSetNames

        /// <summary>
        /// List of property set names that are to be excluded from the Attributes generated from the Space sheet with equal compare
        /// </summary>
        public List<string> SpaceAttExcludesPropertSetEq
        {
            get { return _spaceAttExcludesPropertSetEq; }
        }

        //Space
       
        private List<string> _typeAttExcludesEq = new List<string>() 
        {   "SustainabilityPerformanceCodePerformance",     "AccessibilityPerformance",     "Features",     "Constituents",     "Material",     "Grade", 
            "Finish",   "Color",    "Size",     "Shape",    "ModelReference",   "NominalHeight",    "NominalWidth", "NominalLength",    "WarrantyName",
            "WarrantyDescription",  "DurationUnit",         "ServiceLifeType",  "ServiceLifeDuration",  "ExpectedLife",     "LifeCyclePhase",   "Cost",
            "ReplacementCost",  "WarrantyDurationUnit", "WarrantyDurationLabor",    "WarrantyGuarantorLabor",   "WarrantyDurationParts",    
            "WarrantyGuarantorParts",   "ModelLabel",   "ModelNumber",  "Manufacturer", "IsFixed",  "AssetType", "CodePerformance", "SustainabilityPerformance"
        
        };
 
        /// <summary>
        /// List of property names that are to be excluded from the Attributes generated from the Type sheet with equal compare
        /// </summary>
        public List<string> TypeAttExcludesEq
        {
            get { return _typeAttExcludesEq; }
        }

        
        private List<string> _typeAttExcludesContains = new List<string>() { "Roomtag", "RoomTag", "GSA BIM Area" }; //"Tag",

        /// <summary>
        /// List of property names that are to be excluded from the Attributes generated from the Type sheet with contains compare
        /// </summary>
        public List<string> TypeAttExcludesContains
        {
            get { return _typeAttExcludesContains; }
        }

        private List<string> _typeAttExcludesPropertSetEq = new List<string>() { "BaseQuantities" }; //excludePropertSetNames

        /// <summary>
        /// List of property set names that are to be excluded from the Attributes generated from the Space sheet with equal compare
        /// </summary>
        public List<string> TypeAttExcludesPropertSetEq
        {
            get { return _typeAttExcludesPropertSetEq; }
        }

        private List<string> _zoneAttExcludesContains = new List<string> { "Roomtag", "RoomTag", "Tag", "GSA BIM Area", "Length", "Width", "Height" }; //excludePropertyValueNamesWildcard

        /// <summary>
        /// List of property names that are to be excluded from the Attributes generated from the Zone sheet with contains compare
        /// </summary>
        public List<string> ZoneAttExcludesContains
        {
            get { return _zoneAttExcludesContains; }
        }
    }
}
