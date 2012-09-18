using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.COBie.Rows;
using Xbim.Ifc.ConstructionMgmtDomain;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ExternalReferenceResource;
using Xbim.Ifc.Extensions;
using Xbim.Ifc.PropertyResource;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the Spare tab.
    /// </summary>
    public class COBieDataSpare : COBieData<COBieSpareRow>
    {

        /// <summary>
        /// Data Spare constructor
        /// </summary>
        /// <param name="model">The context of the model being generated</param>
        public COBieDataSpare(COBieContext context) : base(context)
        { }

        #region Methods

        /// <summary>
        /// Fill sheet rows for Spare sheet
        /// </summary>
        /// <returns>COBieSheet<COBieSpareRow></returns>
        public override COBieSheet<COBieSpareRow> Fill()
        {
            ProgressIndicator.ReportMessage("Starting Spares...");
            //Create new sheet
            COBieSheet<COBieSpareRow> spares = new COBieSheet<COBieSpareRow>(Constants.WORKSHEET_SPARE);
                        // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcConstructionProductResource> ifcConstructionProductResources = Model.InstancesOfType<IfcConstructionProductResource>();

            //IfcTypeObject typeObject = Model.InstancesOfType<IfcTypeObject>().FirstOrDefault();

            ProgressIndicator.Initialise("Creating Spares", ifcConstructionProductResources.Count());

            foreach (IfcConstructionProductResource ifcConstructionProductResource in ifcConstructionProductResources)
            {
                ProgressIndicator.IncrementAndUpdate();

                COBieSpareRow spare = new COBieSpareRow(spares);

                spare.Name = (string.IsNullOrEmpty(ifcConstructionProductResource.Name)) ? "" : ifcConstructionProductResource.Name.ToString();

                spare.CreatedBy = GetTelecomEmailAddress(ifcConstructionProductResource.OwnerHistory);
                spare.CreatedOn = GetCreatedOnDateAsFmtString(ifcConstructionProductResource.OwnerHistory);

                spare.Category = GetCategory(ifcConstructionProductResource);

                spare.TypeName = GetObjectType(ifcConstructionProductResource);
               
                spare.ExtSystem = ifcApplication.ApplicationFullName;
                spare.ExtObject = ifcConstructionProductResource.GetType().Name;
                spare.ExtIdentifier = ifcConstructionProductResource.GlobalId;
                spare.Description = (ifcConstructionProductResource == null) ? "" : ifcConstructionProductResource.Description.ToString();

                //get information from Pset_Spare_COBie property set 
                IfcPropertySet ifcPropertySet =  ifcConstructionProductResource.GetPropertySet("Pset_Spare_COBie");
                IfcPropertySingleValue ifcPropertySingleValue = ifcPropertySet.HasProperties.Where<IfcPropertySingleValue>(p => p.Name == "Suppliers").FirstOrDefault();
                spare.Suppliers = ((ifcPropertySingleValue != null) && (!string.IsNullOrEmpty(ifcPropertySingleValue.NominalValue.ToString()))) ? ifcPropertySingleValue.NominalValue.ToString() : DEFAULT_STRING;
                
                ifcPropertySingleValue = ifcPropertySet.HasProperties.Where<IfcPropertySingleValue>(p => p.Name == "SetNumber").FirstOrDefault(); 
                spare.SetNumber = ((ifcPropertySingleValue != null) && (!string.IsNullOrEmpty(ifcPropertySingleValue.NominalValue.ToString()))) ? ifcPropertySingleValue.NominalValue.ToString() : DEFAULT_STRING; ;
                
                ifcPropertySingleValue = ifcPropertySet.HasProperties.Where<IfcPropertySingleValue>(p => p.Name == "PartNumber").FirstOrDefault();
                spare.PartNumber = ((ifcPropertySingleValue != null) && (!string.IsNullOrEmpty(ifcPropertySingleValue.NominalValue.ToString()))) ? ifcPropertySingleValue.NominalValue.ToString() : DEFAULT_STRING; ;

                spares.Rows.Add(spare);
            }
            ProgressIndicator.Finalise();
            return spares;
        }

        /// <summary>
        /// Get the object IfcTypeObject name from the IfcConstructionProductResource object
        /// </summary>
        /// <param name="ifcConstructionProductResource">IfcConstructionProductResource object</param>
        /// <returns>string holding IfcTypeObject name</returns>
        private string GetObjectType(IfcConstructionProductResource ifcConstructionProductResource)
        {
            //first try on ResourceOf.RelatedObjects
            IEnumerable<IfcTypeObject> ifcTypeObjects = ifcConstructionProductResource.ResourceOf.SelectMany(ro => ro.RelatedObjects).OfType<IfcTypeObject>();
            
            //second try on IsDefinedBy.OfType<IfcRelDefinesByType>
            if ((ifcTypeObjects == null) || (ifcTypeObjects.Count() == 0))
                ifcTypeObjects  = ifcConstructionProductResource.IsDefinedBy.OfType<IfcRelDefinesByType>().Select(idb => (idb as IfcRelDefinesByType).RelatingType);
            
            //third try on IsDefinedBy.OfType<IfcRelDefinesByProperties> for DefinesType
            if ((ifcTypeObjects == null) || (ifcTypeObjects.Count() == 0))
                ifcTypeObjects = ifcConstructionProductResource.IsDefinedBy.OfType<IfcRelDefinesByProperties>().SelectMany(idb => (idb as IfcRelDefinesByProperties).RelatingPropertyDefinition.DefinesType);
            
            //convert to string and return if all ok
            if ((ifcTypeObjects != null) || (ifcTypeObjects.Count() > 0))
            {
                List<string> strList = new List<string>();
                foreach (IfcTypeObject ifcTypeItem in ifcTypeObjects)
                {
                    if ((ifcTypeItem != null) && (!string.IsNullOrEmpty(ifcTypeItem.Name.ToString())))
                    {
                        if (!strList.Contains(ifcTypeItem.Name.ToString()))
                            strList.Add(ifcTypeItem.Name.ToString());
                    }
                }
                return (strList.Count > 0) ? string.Join(" : ", strList) : DEFAULT_STRING;
            }


            //last try on IsDefinedBy.OfType<IfcRelDefinesByProperties> for IfcObject
            if ((ifcTypeObjects == null) || (ifcTypeObjects.Count() == 0))
            {
                IEnumerable<IfcObject> ifcObjects = ifcConstructionProductResource.IsDefinedBy.OfType<IfcRelDefinesByProperties>().SelectMany(idb => idb.RelatedObjects);
                List<string> strList = new List<string>();
                foreach (IfcObject ifcObject in ifcObjects)
                {
                    IEnumerable<IfcRelDefinesByType> ifcRelDefinesByTypes = ifcObject.IsDefinedBy.OfType<IfcRelDefinesByType>();
                    foreach (IfcRelDefinesByType ifcRelDefinesByType in ifcRelDefinesByTypes)
                    {
                        if ((ifcRelDefinesByType != null) &&
                            (ifcRelDefinesByType.RelatingType != null) &&
                            (!string.IsNullOrEmpty(ifcRelDefinesByType.RelatingType.Name.ToString()))
                            )
                        {
                            if (!strList.Contains(ifcRelDefinesByType.RelatingType.Name.ToString()))
                                strList.Add(ifcRelDefinesByType.RelatingType.Name.ToString());
                        }
                    }
                    return (strList.Count > 0) ? string.Join(" : ", strList) : DEFAULT_STRING;
                }
                return (strList.Count > 0) ? string.Join(" : ", strList) : DEFAULT_STRING;
            }

            return DEFAULT_STRING; //fail to get any types
        }
        #endregion
    }
}
