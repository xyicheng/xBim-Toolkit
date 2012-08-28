using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.COBie.Rows;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ExternalReferenceResource;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the System tab.
    /// </summary>
    public class COBieDataSystem : COBieData
    {
        /// <summary>
        /// Data System constructor
        /// </summary>
        /// <param name="model">IModel to read data from</param>
        public COBieDataSystem(IModel model)
        {
            Model = model;
        }

        #region Methods

        /// <summary>
        /// Fill sheet rows for System sheet
        /// </summary>
        /// <returns>COBieSheet<COBieSystemRow></returns>
        public COBieSheet<COBieSystemRow> Fill()
        {
            //Create new sheet
            COBieSheet<COBieSystemRow> systems = new COBieSheet<COBieSystemRow>(Constants.WORKSHEET_SYSTEM);

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcSystem> ifcSystems = Model.InstancesOfType<IfcSystem>();

            
            foreach (IfcSystem s in ifcSystems)
            {
                IEnumerable<IfcProduct> ifcProducts = (s.IsGroupedBy == null) ? Enumerable.Empty<IfcProduct>() : s.IsGroupedBy.RelatedObjects.OfType<IfcProduct>();

                foreach (IfcProduct product in ifcProducts)
                {
                    COBieSystemRow sys = new COBieSystemRow(systems);

                    //IfcOwnerHistory ifcOwnerHistory = s.OwnerHistory;

                    sys.Name = s.Name;

                    sys.CreatedBy = GetTelecomEmailAddress(s.OwnerHistory);
                    sys.CreatedOn = GetCreatedOnDateAsFmtString(s.OwnerHistory);

                    IfcRelAssociatesClassification ifcRAC = s.HasAssociations.OfType<IfcRelAssociatesClassification>().FirstOrDefault();
                    if (ifcRAC != null)
                    {
                        IfcClassificationReference ifcCR = (IfcClassificationReference)ifcRAC.RelatingClassification;
                        sys.Category = ifcCR.Name;
                    }
                    else
                        sys.Category = "";

                    sys.ComponentName = product.Name;
                    sys.ExtSystem = GetIfcApplication().ApplicationFullName;
                    sys.ExtObject = "IfcSystem";
                    sys.ExtIdentifier = product.GlobalId;
                    sys.Description = GetSystemDescription(s);

                    systems.Rows.Add(sys);
                }

            }

            return systems;
        }

        private string GetSystemDescription(IfcSystem s)
        {
            if (s != null)
            {
                if (!string.IsNullOrEmpty(s.Description)) return s.Description;
                else if (!string.IsNullOrEmpty(s.Name)) return s.Name;
            }
            return DEFAULT_VAL;
        }
        #endregion
    }
}
