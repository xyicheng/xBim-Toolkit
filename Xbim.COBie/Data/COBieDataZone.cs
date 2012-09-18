using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.Rows;
using Xbim.Ifc.Extensions;
using Xbim.Ifc.ExternalReferenceResource;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.PropertyResource;
using Xbim.XbimExtensions;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the Zone tab.
    /// </summary>
    public class COBieDataZone : COBieData<COBieZoneRow>, IAttributeProvider
    {
        /// <summary>
        /// Data Zone constructor
        /// </summary>
        /// <param name="model">The context of the model being generated</param>
        public COBieDataZone(COBieContext context) : base(context)
        { }

        #region methods

        /// <summary>
        /// Fill sheet rows for Zone sheet
        /// </summary>
        /// <returns>COBieSheet<COBieZoneRow></returns>
        public override COBieSheet<COBieZoneRow> Fill()
        {
            ProgressIndicator.ReportMessage("Starting Zones...");

            //Create new sheet
            COBieSheet<COBieZoneRow> zones = new COBieSheet<COBieZoneRow>(Constants.WORKSHEET_ZONE);

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcZone> ifcZones = Model.InstancesOfType<IfcZone>();
            
            //list of attributes to exclude form attribute sheet
            List<string> excludePropertyValueNamesWildcard = new List<string> {  "Roomtag", "RoomTag", "Tag", "GSA BIM Area", "Length", "Width", "Height"};

            //Also check to see if we have any zones within the spaces
            IEnumerable<IfcSpace> ifcSpaces = Model.InstancesOfType<IfcSpace>();//.OrderBy(ifcSpace => ifcSpace.Name, new CompareIfcLabel());

            ProgressIndicator.Initialise("Creating Zones", ifcZones.Count() + ifcSpaces.Count());

            foreach (IfcZone zn in ifcZones)
            {
                ProgressIndicator.IncrementAndUpdate();
                // create zone for each space found
                IEnumerable<IfcSpace> spaces = (zn.IsGroupedBy == null) ? Enumerable.Empty<IfcSpace>() : zn.IsGroupedBy.RelatedObjects.OfType<IfcSpace>();
                foreach (IfcSpace sp in spaces)
                {
                    

                    COBieZoneRow zone = new COBieZoneRow(zones);

                    //IfcOwnerHistory ifcOwnerHistory = zn.OwnerHistory;

                    zone.Name = zn.Name.ToString();

                    zone.CreatedBy = GetTelecomEmailAddress(zn.OwnerHistory);
                    zone.CreatedOn = GetCreatedOnDateAsFmtString(zn.OwnerHistory);

                    zone.Category = GetCategory(zn);

                    zone.SpaceNames = sp.Name;

                    zone.ExtSystem = ifcApplication.ApplicationFullName;
                    zone.ExtObject = zn.GetType().Name;
                    zone.ExtIdentifier = zn.GlobalId;
                    zone.Description = (string.IsNullOrEmpty(zn.Description)) ? zn.Name.ToString() : zn.Description.ToString(); //if IsNullOrEmpty on Description then output Name
                    zones.Rows.Add(zone);
                    //----------fill in the attribute information for spaces-----------
                    //pass data from this sheet info as Dictionary
                    Dictionary<string, string> passedValues = new Dictionary<string, string>(){{"Sheet", "Zone"}, 
                                                                                          {"Name", zone.Name},
                                                                                          {"CreatedBy", zone.CreatedBy},
                                                                                          {"CreatedOn", zone.CreatedOn},
                                                                                          {"ExtSystem", zone.ExtSystem}
                                                                                          };//required property date <PropertySetName, PropertyName>

                    //add *ALL* the attributes to the passed attributes sheet except property names that match the passed List<string>
                    SetAttributeSheet(zn, passedValues, null, excludePropertyValueNamesWildcard, null, ref _attributes);
                    
                }

            }
            
            foreach (IfcSpace sp in ifcSpaces)
            {
                ProgressIndicator.IncrementAndUpdate();

                IEnumerable<IfcPropertySingleValue> spProperties = Enumerable.Empty<IfcPropertySingleValue>();
                foreach (IfcPropertySet pset in sp.GetAllPropertySets()) //was just looking at sp.GetPropertySet("PSet_Revit_Other") but 2012-08-07-COBieResponsibilityMatrix-v07.xlsx appears to want all
                {
                    spProperties = pset.HasProperties.Where<IfcPropertySingleValue>(p => p.Name.ToString().Contains("ZoneName"));
                    foreach (IfcPropertySingleValue spProp in spProperties)
                    {
                        COBieZoneRow zone = new COBieZoneRow(zones);
                        zone.Name = spProp.NominalValue.ToString();

                        zone.CreatedBy = GetTelecomEmailAddress(sp.OwnerHistory);
                        zone.CreatedOn = GetCreatedOnDateAsFmtString(sp.OwnerHistory);

                        zone.Category = spProp.Name;
                        zone.SpaceNames = sp.Name;

                        zone.ExtSystem = ifcApplication.ApplicationFullName;
                        zone.ExtObject = spProp.GetType().Name;
                        zone.ExtIdentifier = pset.GlobalId.ToString();
                            
                        zone.Description = (string.IsNullOrEmpty(spProp.NominalValue.ToString())) ? DEFAULT_STRING : spProp.NominalValue.ToString(); ;

                        zones.Rows.Add(zone);
                    }
                }

                //TODO: COBieResponsibilityMatrix-v07 states that Zone - "Repeated names consolidated", have to implemented as not clear on what is required
                //spProperties = spProperties.OrderBy(p => p.Name.ToString(), new CompareString()); //consolidate test, Concat as looping spaces then sort then dump to COBieZoneRow foreach

                
            }

            ProgressIndicator.Finalise();

            return zones;
        }
        #endregion

        COBieSheet<COBieAttributeRow> _attributes;

        public void InitialiseAttributes(ref COBieSheet<COBieAttributeRow> attributeSheet)
        {
            _attributes = attributeSheet;
        }
    }
}
