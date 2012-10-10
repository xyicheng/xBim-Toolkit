using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.Rows;
using Xbim.Ifc2x3.Kernel;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.SharedBldgElements;
using Xbim.Ifc2x3.SharedBldgServiceElements;
using Xbim.Ifc2x3.Extensions;
using Xbim.XbimExtensions;
using Xbim.Ifc2x3.PropertyResource;
using Xbim.Ifc2x3.StructuralElementsDomain;
using Xbim.Ifc2x3.SharedComponentElements;

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

            IEnumerable<IfcObject> ifcElements = ((from x in relAggregates
                                            from y in x.RelatedObjects
                                                   where !Context.ComponentExcludeTypes.Contains(y.GetType())
                                            select y).Union(from x in relSpatial
                                                            from y in x.RelatedElements
                                                            where !Context.ComponentExcludeTypes.Contains(y.GetType())
                                                            select y)).GroupBy(el => el.Name).Select(g => g.First()).OfType<IfcObject>(); //.Distinct().ToList();
            
            COBieDataPropertySetValues allPropertyValues = new COBieDataPropertySetValues(ifcElements); //properties helper class
            COBieDataAttributeBuilder attributeBuilder = new COBieDataAttributeBuilder(Context, allPropertyValues);
            attributeBuilder.InitialiseAttributes(ref _attributes);
            //set up filters on COBieDataPropertySetValues for the SetAttributes only
            attributeBuilder.ExcludeAttributePropertyNames.AddRange(Context.ComponentAttExcludesEq); //we do not want listed properties for the attribute sheet so filter them out
            attributeBuilder.ExcludeAttributePropertyNamesWildcard.AddRange(Context.ComponentAttExcludesContains);//we do not want listed properties for the attribute sheet so filter them out
            attributeBuilder.RowParameters["Sheet"] = "Component";


            ProgressIndicator.Initialise("Creating Components", ifcElements.Count());

            foreach (var obj in ifcElements)
            {
                ProgressIndicator.IncrementAndUpdate();
                var xxx = obj.Decomposes.OfType<IfcRelAggregates>().Where(ra => Context.ComponentExcludeTypes.Contains(ra.RelatingObject.GetType()));
                if (xxx.Count() > 0) 
                    continue;

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
                component.ExtSystem = GetExternalSystem(el);
                component.ExtObject = el.GetType().Name;
                component.ExtIdentifier = el.GlobalId;

                //set from PropertySingleValues filtered via candidateProperties
                allPropertyValues.SetAllPropertySingleValues(el); //set the internal filtered IfcPropertySingleValues List in allPropertyValues
                component.SerialNumber = allPropertyValues.GetPropertySingleValueValue("SerialNumber", false);
                string installationDate = allPropertyValues.GetPropertySingleValueValue("InstallationDate", false);
                component.InstallationDate = ((installationDate == DEFAULT_STRING) || (!IsDate(installationDate))) ? GetCreatedOnDateAsFmtString(null) : installationDate;
                string warrantyStartDate = allPropertyValues.GetPropertySingleValueValue("WarrantyStartDate", false);
                component.WarrantyStartDate = ((warrantyStartDate == DEFAULT_STRING) || (!IsDate(warrantyStartDate))) ? GetCreatedOnDateAsFmtString(null) : warrantyStartDate;
                component.TagNumber = allPropertyValues.GetPropertySingleValueValue("TagNumber", false);
                component.BarCode = allPropertyValues.GetPropertySingleValueValue("BarCode", false);
                component.AssetIdentifier = allPropertyValues.GetPropertySingleValueValue("AssetIdentifier", false);
                
                components.Rows.Add(component);

                //fill in the attribute information
                attributeBuilder.RowParameters["Name"] = component.Name;
                attributeBuilder.RowParameters["CreatedBy"] = component.CreatedBy;
                attributeBuilder.RowParameters["CreatedOn"] = component.CreatedOn;
                attributeBuilder.RowParameters["ExtSystem"] = component.ExtSystem;
                attributeBuilder.PopulateAttributesRows(el); //fill attribute sheet rows
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
            string value = "";
            if (el != null && el.ContainedInStructure.Count() > 0)
            {
                var owningSpace = el.ContainedInStructure.Select(cis => cis.RelatingStructure).OfType<IfcSpace>().FirstOrDefault(); //only one or zero held in ContainedInStructure
                if ((owningSpace != null) && (owningSpace.Name != null)) value =  owningSpace.Name.ToString();  
            }
            return string.IsNullOrEmpty(value) ? Constants.DEFAULT_STRING : value;
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
            return DEFAULT_STRING;
        }
        #endregion

        COBieSheet<COBieAttributeRow> _attributes;

        public void InitialiseAttributes(ref COBieSheet<COBieAttributeRow> attributeSheet)
        {
            _attributes = attributeSheet;
        }
    }
}
