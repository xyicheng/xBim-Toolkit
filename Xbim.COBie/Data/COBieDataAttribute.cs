using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.COBie.Rows;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.GeometryResource;
using Xbim.Ifc.ExternalReferenceResource;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the Attribute tab.
    /// </summary>
    public class COBieDataAttribute : COBieData
    {

        /// <summary>
        /// Data Attribute constructor
        /// </summary>
        /// <param name="model">IModel to read data from</param>
        public COBieDataAttribute(IModel model)
        {
            Model = model;
        }

        #region Methods

        /// <summary>
        /// Fill sheet rows for Attribute sheet
        /// </summary>
        /// <returns> COBieSheet<COBieAttributeRow></returns>
        public COBieSheet<COBieAttributeRow> Fill()
        {
            //Create new sheet
            COBieSheet<COBieAttributeRow> attributes = new COBieSheet<COBieAttributeRow>(Constants.WORKSHEET_ATTRIBUTE);

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcObject> ifcObject = Model.InstancesOfType<IfcObject>();
            IfcBuildingStorey ifcBuildingStorey = Model.InstancesOfType<IfcBuildingStorey>().FirstOrDefault();
            //IfcTelecomAddress ifcTelecomAddres = Model.InstancesOfType<IfcTelecomAddress>().FirstOrDefault();
            IfcCartesianPoint ifcCartesianPoint = Model.InstancesOfType<IfcCartesianPoint>().FirstOrDefault();
            
           // IfcOwnerHistory ifcOwnerHistory = Model.InstancesOfType<IfcOwnerHistory>().FirstOrDefault();
            IfcProduct ifcProduct = Model.InstancesOfType<IfcProduct>().FirstOrDefault();
            IfcClassification ifcClassification = Model.InstancesOfType<IfcClassification>().FirstOrDefault();
            string applicationFullName = GetIfcApplication().ApplicationFullName;

            foreach (IfcObject obj in ifcObject)
            {
                COBieAttributeRow attribute = new COBieAttributeRow(attributes);
                attribute.Name = (ifcBuildingStorey == null || ifcBuildingStorey.Name.ToString() == "") ? "AttributeName" : ifcBuildingStorey.Name.ToString();

                attribute.CreatedBy = GetTelecomEmailAddress(obj.OwnerHistory);
                attribute.CreatedOn = GetCreatedOnDateAsFmtString(obj.OwnerHistory);

                IfcRelAssociatesClassification ifcRAC = obj.HasAssociations.OfType<IfcRelAssociatesClassification>().FirstOrDefault();
                if (ifcRAC != null)
                {
                    IfcClassificationReference ifcCR = (IfcClassificationReference)ifcRAC.RelatingClassification;
                    attribute.Category = ifcCR.Name;
                }
                attribute.Category = "";

                attribute.SheetName = "PickList.SheetType";
                attribute.RowName = DEFAULT_VAL;
                attribute.Value = "";
                attribute.Unit = "";
                attribute.ExtSystem = applicationFullName;
                attribute.ExtObject = "PickList.objType";
                attribute.ExtIdentifier = obj.GlobalId;
                attribute.Description = "";
                attribute.AllowedValues = "";

                attributes.Rows.Add(attribute);
            }

            return attributes;
        }
        #endregion
    }
}
