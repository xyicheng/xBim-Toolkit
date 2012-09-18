using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.Rows;
using Xbim.Ifc.ActorResource;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.UtilityResource;
using Xbim.XbimExtensions;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the Connection tab.
    /// </summary>
    public class COBieDataConnection : COBieData
    {
        /// <summary>
        /// Data Connection constructor
        /// </summary>
        /// <param name="model">IModel to read data from</param>
        public COBieDataConnection(IModel model)
        {
            Model = model;
        }

        #region Methods

        /// <summary>
        /// Fill sheet rows for Connection sheet
        /// </summary>
        /// <returns>COBieSheet<COBieConnectionRow></returns>
        public COBieSheet<COBieConnectionRow> Fill()
        {
            //Create new sheet
           COBieSheet<COBieConnectionRow> connections = new COBieSheet<COBieConnectionRow>(Constants.WORKSHEET_CONNECTION);

            // get all IfcBuildingStory objects from IFC file
            IEnumerable<IfcRelConnectsElements> ifcConnections = Model.InstancesOfType<IfcRelConnectsElements>();
            
            IfcRelConnectsPorts relCP = Model.InstancesOfType<IfcRelConnectsPorts>().FirstOrDefault();

            IfcProduct product = Model.InstancesOfType<IfcProduct>().FirstOrDefault();

            int ids = 0;
            foreach (IfcRelConnectsElements c in ifcConnections)
            {
                COBieConnectionRow conn = new COBieConnectionRow(connections);
                conn.Name = (string.IsNullOrEmpty(c.Name)) ? ids.ToString() : c.Name.ToString();
                conn.CreatedBy = GetTelecomEmailAddress(c.OwnerHistory);
                conn.CreatedOn = GetCreatedOnDateAsFmtString(c.OwnerHistory);
                conn.ConnectionType = c.Description;
                conn.SheetName = "";
                conn.RowName1 = (product == null) ? "" : product.Name.ToString();
                conn.RowName2 = (product == null) ? "" : product.Name.ToString();
                conn.RealizingElement = (relCP == null) ? "" : relCP.RealizingElement.Description.ToString();
                conn.PortName1 = (relCP == null) ? "" : relCP.RelatingPort.Description.ToString();
                conn.PortName2 = (relCP == null) ? "" : relCP.RelatedPort.Description.ToString();
                conn.ExtSystem = ifcApplication.ApplicationFullName;
                conn.ExtObject = "";
                conn.ExtIdentifier = c.GlobalId;
                conn.Description = (string.IsNullOrEmpty(c.Description)) ? DEFAULT_STRING : c.Description.ToString();

                connections.Rows.Add(conn);

                ids++;
            }

            return connections;
        }
        #endregion
    }
}
