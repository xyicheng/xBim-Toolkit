using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.COBieExtensions;
using Xbim.XbimExtensions;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.UtilityResource;
using Xbim.Ifc.SelectTypes;
using System.Reflection;

namespace Xbim.COBie
{
    [Serializable()]
    public class COBieFacilityRow : COBieRow
    {
        IfcProject _ifcProject;
        IfcSite _ifcSite;
        IfcBuilding _ifcBuilding;
        IModel _model;

        static COBieFacilityRow()
        {
            _columns = new Dictionary<int, COBieColumn>();
            //Properties = typeof(COBieFacility).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            Properties = typeof(COBieFacilityRow).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            // add column info 
            foreach (PropertyInfo propInfo in Properties)
            {
                object[] attrs = propInfo.GetCustomAttributes(typeof(COBieAttributes), true);
                if (attrs != null && attrs.Length > 0)
                {
                    _columns.Add(((COBieAttributes)attrs[0]).Order, new COBieColumn(((COBieAttributes)attrs[0]).ColumnName, ((COBieAttributes)attrs[0]).MaxLength, ((COBieAttributes)attrs[0]).AllowedType, ((COBieAttributes)attrs[0]).KeyType));
                }
            }

            
        }

        // TODO: Review this
        public void InitFacility(IModel model)
        {
            _model = model;
            _ifcProject = model.IfcProject;
            _ifcSite = model.InstancesOfType<IfcSite>().FirstOrDefault();
            _ifcBuilding = model.InstancesOfType<IfcBuilding>().FirstOrDefault();
            //if (_ifcProject != null)
            //    _ifcSite = _ifcProject.GetSites().FirstOrDefault();
            //else
            //    _ifcSite = model.InstancesOfType<IfcSite>().FirstOrDefault();
            //if (_ifcSite != null)
            //    _ifcBuilding = _ifcSite.GetBuildings().FirstOrDefault();
            //else
            //    _ifcBuilding = model.InstancesOfType<IfcBuilding>().FirstOrDefault();

           
        }

        




        [COBieAttributes(0, COBieKeyType.PrimaryKey, COBieAttributeState.Required, "Name", 255, COBieAllowedType.AlphaNumeric)]
        public string Name
        {
            get
            {
                return GetName();
            }
            set { }
        }

        [COBieAttributes(1, COBieKeyType.None, COBieAttributeState.Required, "CreatedBy", 255, COBieAllowedType.Email)]
        public string CreatedBy
        {
            get
            {
                return "";
            }
            set { }
        }

        [COBieAttributes(2, COBieKeyType.None, COBieAttributeState.Required, "CreatedOn", 19, COBieAllowedType.ISODate)]
        public DateTime CreatedOn
        {
            get
            {
                return DateTime.Now;
            }
            set { }
        }

        [COBieAttributes(3, COBieKeyType.None, COBieAttributeState.Required, "Category", 255, COBieAllowedType.Text)]
        public string Category
        {
            get
            {
                return GetCategory();
            }
            set { }
        }

        [COBieAttributes(4, COBieKeyType.None, COBieAttributeState.Required, "ProjectName", 255, COBieAllowedType.AlphaNumeric)]
        public string ProjectName
        {
            get
            {
                return GetProjectName();
            }
            set { }
        }

        [COBieAttributes(5, COBieKeyType.None, COBieAttributeState.Required, "SiteName", 255, COBieAllowedType.AlphaNumeric)]
        public string SiteName
        {
            get
            {
                return GetSiteName();
            }
            set { }
        }

        [COBieAttributes(6, COBieKeyType.None, COBieAttributeState.Required, "LinearUnits", 255, COBieAllowedType.Text)]
        public string LinearUnits
        {
            get
            {
                return GetLinearUnits();
            }
            set { }
        }

        [COBieAttributes(7, COBieKeyType.None, COBieAttributeState.Required, "AreaUnits", 255, COBieAllowedType.Text)]
        public string AreaUnits
        {
            get
            {
                return GetAreaUnits();
            }
            set { }
        }

        [COBieAttributes(8, COBieKeyType.None, COBieAttributeState.Required, "VolumeUnits", 255, COBieAllowedType.Text)]
        public string VolumeUnits
        {
            get
            {
                return GetVolumeUnits();
            }
            set { }
        }

        [COBieAttributes(9, COBieKeyType.None, COBieAttributeState.Required, "CurrencyUnit", 255, COBieAllowedType.Text)]
        public string CurrencyUnit
        {
            get
            {
                return "n/a";
            }
            set { }
        }

        [COBieAttributes(10, COBieKeyType.None, COBieAttributeState.Required, "AreaMeasurement", 255, COBieAllowedType.AlphaNumeric)]
        public string AreaMeasurement
        {
            get
            {
                return GetAreaMeasurement();
            }
            set { }
        }

        [COBieAttributes(11, COBieKeyType.None, COBieAttributeState.System, "ExternalSystem", 255, COBieAllowedType.AlphaNumeric)]
        public string ExternalSystem
        {
            get
            {
                return GetIfcApplication().ApplicationFullName;
            }
            set { }
        }

        [COBieAttributes(12, COBieKeyType.None, COBieAttributeState.System, "ExternalProjectObject", 255, COBieAllowedType.AlphaNumeric)]
        public string ExternalProjectObject
        {
            get
            {
                return "IfcProject";
            }
            set { }
        }

        [COBieAttributes(13, COBieKeyType.None, COBieAttributeState.System, "ExternalProjectIdentifier", 255, COBieAllowedType.AlphaNumeric)]
        public string ExternalProjectIdentifier
        {
            get
            {
                return _model.IfcProject.GlobalId;
            }
            set { }
        }

        [COBieAttributes(14, COBieKeyType.None, COBieAttributeState.System, "ExternalSiteObject", 255, COBieAllowedType.AlphaNumeric)]
        public string ExternalSiteObject
        {
            get
            {
                return "IfcSite";
            }
            set { }
        }

        [COBieAttributes(15, COBieKeyType.None, COBieAttributeState.System, "ExternalSiteIdentifier", 255, COBieAllowedType.AlphaNumeric)]
        public string ExternalSiteIdentifier
        {
            get
            {
                return _ifcSite.GlobalId;
            }
            set { }
        }

        [COBieAttributes(16, COBieKeyType.None, COBieAttributeState.System, "ExternalFacilityObject", 255, COBieAllowedType.AlphaNumeric)]
        public string ExternalFacilityObject
        {
            get
            {
                return "IfcBuilding";
            }
            set { }
        }

        [COBieAttributes(17, COBieKeyType.None, COBieAttributeState.System, "ExternalFacilityIdentifier", 255, COBieAllowedType.AlphaNumeric)]
        public string ExternalFacilityIdentifier
        {
            get
            {
                return _ifcBuilding.GlobalId;
            }
            set { }
        }

        [COBieAttributes(18, COBieKeyType.None, COBieAttributeState.As_Specified, "Description", 255, COBieAllowedType.AlphaNumeric)]
        public string Description
        {
            get
            {
                return GetDescription();
            }
            set { }
        }

        [COBieAttributes(19, COBieKeyType.None, COBieAttributeState.As_Specified, "ProjectDescription", 255, COBieAllowedType.AlphaNumeric)]
        public string ProjectDescription
        {
            get
            {
                return GetProjectDescription();
            }
            set { }
        }

        [COBieAttributes(20, COBieKeyType.None, COBieAttributeState.As_Specified, "SiteDescription", 255, COBieAllowedType.AlphaNumeric)]
        public string SiteDescription
        {
            get
            {
                return GetSiteDescription();
            }
            set { }
        }

        [COBieAttributes(21, COBieKeyType.None, COBieAttributeState.As_Specified, "Phase", 255, COBieAllowedType.AlphaNumeric)]
        public string Phase
        {
            get
            {
                return _model.IfcProject.Phase;
            }
            set { }
        }

        private string GetName()
        {            
            //if (_ifcBuilding != null)
            //{
            //    if (_ifcBuilding.Name.HasValue) return _ifcBuilding.Name.Value;
            //    if (_ifcBuilding.LongName.HasValue) return _ifcBuilding.LongName.Value;
            //    if (_ifcBuilding.Description.HasValue) return _ifcBuilding.Description.Value;
            //}
            //if (_ifcSite != null)
            //{
            //    if (_ifcSite.Name.HasValue) return _ifcSite.Name.Value;
            //    if (_ifcSite.LongName.HasValue) return _ifcSite.LongName.Value;
            //    if (_ifcSite.Description.HasValue) return _ifcSite.Description.Value;
            //}
            //if (_ifcProject != null)
            //{
            //    if (_ifcSite.Name.HasValue) return _ifcSite.Name.Value;
            //    if (_ifcSite.LongName.HasValue) return _ifcSite.LongName.Value;
            //    if (_ifcSite.Description.HasValue) return _ifcSite.Description.Value;
            //}
            return "n/a";
        }

        private string GetDescription()
        {
            if (_ifcBuilding != null)
            {
                if (!string.IsNullOrEmpty(_ifcBuilding.LongName)) return _ifcBuilding.LongName;
                else if (!string.IsNullOrEmpty(_ifcBuilding.Description)) return _ifcBuilding.Description;
                else if (!string.IsNullOrEmpty(_ifcBuilding.Name)) return _ifcBuilding.Name;
            }
            return "n/a";
        }

        private string GetProjectDescription()
        {
            if (_ifcProject != null)
            {
                if (!string.IsNullOrEmpty(_ifcProject.LongName)) return _ifcProject.LongName;
                else if (!string.IsNullOrEmpty(_ifcProject.Description)) return _ifcProject.Description;
                else if (!string.IsNullOrEmpty(_ifcProject.Name)) return _ifcProject.Name;
            }
            return "Project Description";
        }

        private string GetSiteDescription()
        {
            if (_ifcSite != null)
            {
                if (!string.IsNullOrEmpty(_ifcSite.LongName)) return _ifcSite.LongName;
                else if (!string.IsNullOrEmpty(_ifcSite.Description)) return _ifcSite.Description;
                else if (!string.IsNullOrEmpty(_ifcSite.Name)) return _ifcSite.Name;
            }
            return "Site Description";
        }

        private string GetSiteName()
        {
            if (_ifcSite != null)
            {
                if (!string.IsNullOrEmpty(_ifcSite.Name)) return _ifcSite.Name;
                else if (!string.IsNullOrEmpty(_ifcSite.LongName)) return _ifcSite.LongName;
                else if (!string.IsNullOrEmpty(_ifcSite.GlobalId)) return _ifcSite.GlobalId;
            }
            return "Site Name";
        }

        private string GetProjectName()
        {
            if (_ifcProject != null)
            {
                if (!string.IsNullOrEmpty(_ifcProject.Name)) return _ifcProject.Name;
                else if (!string.IsNullOrEmpty(_ifcProject.LongName)) return _ifcProject.LongName;
                else if (!string.IsNullOrEmpty(_ifcProject.GlobalId)) return _ifcProject.GlobalId;
            }
            return "Site Name";
        }

        private IfcApplication GetIfcApplication()
        {
            IfcApplication app = _model.InstancesOfType<IfcApplication>().FirstOrDefault();
            return app;
        }

        private string GetLinearUnits()
        {
            IEnumerable<IfcUnitAssignment> unitAssignments = _model.InstancesOfType<IfcUnitAssignment>();
            foreach (IfcUnitAssignment ua in unitAssignments)
            {
                UnitSet us = ua.Units;
                foreach (IfcUnit u in us)
                {
                    if (u is IfcSIUnit)
                    {
                        if (((IfcSIUnit)u).UnitType == IfcUnitEnum.LENGTHUNIT)
                        {
                            if (((IfcSIUnit)u).Prefix.ToString().ToLower() == "milli") return "millimetres";
                            if (((IfcSIUnit)u).Name.ToString().ToLower() == "metre") return "metres";
                            if (((IfcSIUnit)u).Name.ToString().ToLower() == "inch") return "inches";
                        }
                    }                    
                }
            }   
            return "feet";
        }

        private string GetAreaUnits()
        {
            IEnumerable<IfcUnitAssignment> unitAssignments = _model.InstancesOfType<IfcUnitAssignment>();
            foreach (IfcUnitAssignment ua in unitAssignments)
            {                
                UnitSet us = ua.Units;
                foreach (IfcUnit u in us)
                {
                    if (u is IfcSIUnit)
                    {
                        if (((IfcSIUnit)u).UnitType == IfcUnitEnum.AREAUNIT)
                            if (((IfcSIUnit)u).Name.ToString().ToLower() == "square_metre") return "squaremetres";
                    }
                    else if (u is IfcConversionBasedUnit)
                    {
                        if (((IfcConversionBasedUnit)u).UnitType == IfcUnitEnum.AREAUNIT)
                            if (((IfcConversionBasedUnit)u).Name.ToString().ToLower() == "square_metre") return "squaremetres";
                    }
                }                
            }            
            
            return "squarefeet";
        }

        private string GetVolumeUnits()
        {
            IEnumerable<IfcUnitAssignment> unitAssignments = _model.InstancesOfType<IfcUnitAssignment>();
            foreach (IfcUnitAssignment ua in unitAssignments)
            {
                UnitSet us = ua.Units;
                foreach (IfcUnit u in us)
                {
                    if (u is IfcSIUnit)
                    {
                        if (((IfcSIUnit)u).UnitType == IfcUnitEnum.VOLUMEUNIT)
                            if (((IfcSIUnit)u).Name.ToString().ToLower() == "cubic_metre") return "cubicmetres";     
                    }      
                    else if (u is IfcConversionBasedUnit)
                    {
                        if (((IfcConversionBasedUnit)u).UnitType == IfcUnitEnum.VOLUMEUNIT)
                            if (((IfcConversionBasedUnit)u).Name.ToString().ToLower() == "cubic_metre") return "cubicmetres";     
                    }        
                }
            } 
            return "cubicfeet";
        }

        private string GetAreaMeasurement()
        {
            return "n/a";
        }

        private string GetCategory()
        {
            return "";
        }
    }

    
}
