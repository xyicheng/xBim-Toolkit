﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.Rows;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.SharedBldgElements;
using Xbim.Ifc.SharedBldgServiceElements;
using Xbim.Ifc.Extensions;
using Xbim.XbimExtensions;
using Xbim.Ifc.PropertyResource;
using Xbim.Ifc.StructuralElementsDomain;
using Xbim.Ifc.SharedComponentElements;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the Component tab.
    /// </summary>
    public class COBieDataComponent : COBieData<COBieComponentRow>, IAttributeProvider
    {
        /// <summary>
        /// Data Component constructor
        /// </summary>
        /// <param name="model">The context of the model being generated</param>
        public COBieDataComponent(COBieContext context)
            : base(context)
        { }

        #region Methods

        /// <summary>
        /// Fill sheet rows for Component sheet
        /// </summary>
        /// <returns>COBieSheet<COBieComponentRow></returns>
        public override COBieSheet<COBieComponentRow> Fill()
        {
            ProgressIndicator.ReportMessage("Starting Components...");
            //Create new sheet
            COBieSheet<COBieComponentRow> components = new COBieSheet<COBieComponentRow>(Constants.WORKSHEET_COMPONENT);
         
            IEnumerable<IfcRelAggregates> relAggregates = Model.InstancesOfType<IfcRelAggregates>();
            IEnumerable<IfcRelContainedInSpatialStructure> relSpatial = Model.InstancesOfType<IfcRelContainedInSpatialStructure>();

            List<IfcObject> ifcElements = ((from x in relAggregates
                                            from y in x.RelatedObjects
                                            where !ComponentExcludeTypes.Contains(y.GetType())
                                            select y).Union(from x in relSpatial
                                                            from y in x.RelatedElements
                                                            where !ComponentExcludeTypes.Contains(y.GetType())
                                                            select y)).GroupBy(el => el.Name).Select(g => g.First()).OfType<IfcObject>().ToList(); //.Distinct().ToList();
            
            COBieDataPropertySetValues allPropertyValues = new COBieDataPropertySetValues(ifcElements); //properties helper class
            
            List<string> candidateProperties = new List<string> {  "SerialNumber",
                                                                   "InstallationDate",
                                                                   "WarrantyStartDate",
                                                                   "TagNumber",
                                                                   "BarCode",
                                                                   "AssetIdentifier"};
            List<string> excludePropertyValueNames = candidateProperties;
            excludePropertyValueNames.Add("Circuit NumberSystem Type");
            excludePropertyValueNames.Add("System Name");
            List<string> excludePropertyValueNamesWildcard = new List<string> {"Roomtag", "RoomTag", "Tag", "GSA BIM Area", "Length", "Width", "Height"};
            //set up filters on COBieDataPropertySetValues
            allPropertyValues.ExcludePropertyValueNames.AddRange(excludePropertyValueNames);
            allPropertyValues.FilterPropertyValueNames.AddRange(candidateProperties);
            allPropertyValues.ExcludePropertyValueNamesWildcard.AddRange(excludePropertyValueNamesWildcard);
            allPropertyValues.RowParameters["Sheet"] = "Component";


            ProgressIndicator.Initialise("Creating Components", ifcElements.Count());

            foreach (var obj in ifcElements)
            {
                ProgressIndicator.IncrementAndUpdate();

                COBieComponentRow component = new COBieComponentRow(components);

                IfcElement el = obj as IfcElement;
                if (el == null)
                    continue;
                component.Name = el.Name;
                component.CreatedBy = GetTelecomEmailAddress(el.OwnerHistory);
                component.CreatedOn = GetCreatedOnDateAsFmtString(el.OwnerHistory);
                
                component.TypeName = GetTypeName(el);
                component.Space = GetComponentRelatedSpace(el);
                component.Description = GetComponentDescription(el);
                component.ExtSystem = ifcApplication.ApplicationFullName;
                component.ExtObject = el.GetType().Name;
                component.ExtIdentifier = el.GlobalId;

                //set from PropertySingleValues filtered via candidateProperties
                allPropertyValues.SetFilteredPropertySingleValues(el); //set the internal filtered IfcPropertySingleValues List in allPropertyValues
                component.SerialNumber = allPropertyValues.GetFilteredPropertySingleValueValue("SerialNumber", false);
                component.InstallationDate = allPropertyValues.GetFilteredPropertySingleValueValue("InstallationDate", false);
                component.WarrantyStartDate = allPropertyValues.GetFilteredPropertySingleValueValue("WarrantyStartDate", false);
                component.TagNumber = allPropertyValues.GetFilteredPropertySingleValueValue("TagNumber", false);
                component.BarCode = allPropertyValues.GetFilteredPropertySingleValueValue("BarCode", false);
                component.AssetIdentifier = allPropertyValues.GetFilteredPropertySingleValueValue("AssetIdentifier", false);

                components.Rows.Add(component);

                //fill in the attribute information
                allPropertyValues.RowParameters["Name"] = component.Name;
                allPropertyValues.RowParameters["CreatedBy"] = component.CreatedBy;
                allPropertyValues.RowParameters["CreatedOn"] = component.CreatedOn;
                allPropertyValues.RowParameters["ExtSystem"] = component.ExtSystem;
                allPropertyValues.SetAttributesRows(el, ref _attributes); //fill attribute sheet rows
            }

            ProgressIndicator.Finalise();
            return components;
        }

        
        
        /// <summary>
        /// Get Space name which holds the passed in IfcElement
        /// </summary>
        /// <param name="el">Element to extract space name from</param>
        /// <returns>string</returns>
        internal string GetComponentRelatedSpace(IfcElement el)
        {
            if (el != null && el.ContainedInStructure.Count() > 0)
            {
                var owningSpace = el.ContainedInStructure.First().RelatingStructure;
                if (owningSpace.GetType() == typeof(IfcSpace))
                    return owningSpace.Name.ToString();
            }
            return Constants.DEFAULT_STRING;
        }

        /// <summary>
        /// Get Description for passed in IfcElement
        /// </summary>
        /// <param name="el">Element holding description</param>
        /// <returns>string</returns>
        internal string GetComponentDescription(IfcElement el)
        {
            if (el != null)
            {
                if (!string.IsNullOrEmpty(el.Description)) return el.Description;
                else if (!string.IsNullOrEmpty(el.Name)) return el.Name;
            }
            return Constants.DEFAULT_STRING;
        }
        #endregion

        COBieSheet<COBieAttributeRow> _attributes;

        public void InitialiseAttributes(ref COBieSheet<COBieAttributeRow> attributeSheet)
        {
            _attributes = attributeSheet;
        }
    }
}
