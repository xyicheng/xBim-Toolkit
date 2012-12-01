﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Xbim.Ifc.ProductExtension;
using Xbim.COBie.Rows;
using Xbim.XbimExtensions.Transactions;
using Xbim.Ifc.Extensions;

namespace Xbim.COBie.Serialisers.XbimSerialiser
{
    public class COBieXBimZone : COBieXBim
    {
        #region Properties
        public Dictionary<string, IfcSpace> Spaces { get; set; }
        #endregion

        public COBieXBimZone(COBieXBimContext xBimContext)
            : base(xBimContext)
        {
            Spaces = new Dictionary<string, IfcSpace>();
        }
        
        #region Methods
        // <summary>
        /// Create and setup objects held in the Zone COBieSheet
        /// </summary>
        /// <param name="cOBieSheet">COBieSheet of COBieZoneRow to read data from</param>
        public void SerialiseZone(COBieSheet<COBieZoneRow> cOBieSheet)
        {
            using (Transaction trans = Model.BeginTransaction("Add Zone"))
            {
                try
                {

                    ProgressIndicator.ReportMessage("Starting Zones...");
                    ProgressIndicator.Initialise("Creating Zones", cOBieSheet.RowCount);
                    for (int i = 0; i < cOBieSheet.RowCount; i++)
                    {
                        ProgressIndicator.IncrementAndUpdate();
                        COBieZoneRow row = cOBieSheet[i];
                        AddZone(row);
                    }

                    ProgressIndicator.Finalise();
                    trans.Commit();

                }
                catch (Exception)
                {
                    trans.Rollback();
                    //TODO: Catch with logger?
                    throw;
                }
            }
        }

        /// <summary>
        /// Add the data to the Zone object
        /// </summary>
        /// <param name="row">COBieZoneRow holding the data</param>
        private void AddZone(COBieZoneRow row)
        {
            IfcZone ifcZone = Model.New<IfcZone>();
            //Add Created By, Created On and ExtSystem to Owner History
            if ((ValidateString(row.CreatedBy)) && (Contacts.ContainsKey(row.CreatedBy)))
                SetNewOwnerHistory(ifcZone, row.ExtSystem, Contacts[row.CreatedBy], row.CreatedOn);
            else
                SetNewOwnerHistory(ifcZone, row.ExtSystem, Model.DefaultOwningUser, row.CreatedOn);

            //using statement will set the Model.OwnerHistoryAddObject to ifcZone.OwnerHistory as OwnerHistoryAddObject is used upon any property changes, 
            //then swaps the original OwnerHistoryAddObject back in the dispose, so set any properties within the using statement
            using (COBieXBimEditScope context = new COBieXBimEditScope(Model, ifcZone.OwnerHistory))
            {
                //Add Name
                if (ValidateString(row.Name)) ifcZone.Name = row.Name;

                //Add Category
                AddCategory(row.Category, ifcZone);

                //add space to the zone group
                AddSpaceToZone(row.SpaceNames, ifcZone);

                //Add GlobalId
                AddGlobalId(row.ExtIdentifier, ifcZone);

                //Add Description
                if (ValidateString(row.Description)) ifcZone.Description = row.Description;

            }
        }

        /// <summary>
        /// Add space to the building story(Floor)
        /// </summary>
        /// <param name="row"></param>
        /// <param name="ifcSpace"></param>
        private void AddSpaceToZone(string spaceName, IfcZone ifcZone)
        {
            if (ValidateString(spaceName))
            {
                IfcSpace space = null;
                if (Spaces.ContainsKey(spaceName))
                {
                    space = Spaces[spaceName];
                }
                else
                {
                    space = Model.InstancesOfType<IfcSpace>().Where(sp => sp.Name == spaceName).FirstOrDefault();
                    if (space != null)
                        Spaces.Add(spaceName, space);
                }
                if (space != null)
                    ifcZone.AddObjectToGroup(space);
                    
            }
        }

        #endregion
    }
}