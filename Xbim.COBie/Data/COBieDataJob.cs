using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.Rows;
using Xbim.Ifc.ConstructionMgmtDomain;
using Xbim.Ifc.ExternalReferenceResource;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.ProcessExtensions;
using Xbim.XbimExtensions;
using Xbim.Ifc.PropertyResource;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.SelectTypes;

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the Job tab.
    /// </summary>
    public class COBieDataJob : COBieData<COBieJobRow>
    {
        /// <summary>
        /// Data Job constructor
        /// </summary>
        /// <param name="model">The context of the model being generated</param>
        public COBieDataJob(COBieContext context) : base(context)
        { }

        #region Methods

        /// <summary>
        /// Fill sheet rows for Job sheet
        /// </summary>
        /// <returns>COBieSheet<COBieJobRow></returns>
        public override COBieSheet<COBieJobRow> Fill()
        {
            ProgressIndicator.ReportMessage("Starting Jobs...");

            //create new sheet
            COBieSheet<COBieJobRow> jobs = new COBieSheet<COBieJobRow>(Constants.WORKSHEET_JOB);

            // get all IfcTask objects from IFC file
            IEnumerable<IfcTask> ifcTasks = Model.InstancesOfType<IfcTask>();

            COBieDataPropertySetValues allPropertyValues = new COBieDataPropertySetValues(ifcTasks.OfType<IfcObject>()); //properties helper class
            
            //IfcTypeObject typObj = Model.InstancesOfType<IfcTypeObject>().FirstOrDefault();
            IfcConstructionEquipmentResource cer = Model.InstancesOfType<IfcConstructionEquipmentResource>().FirstOrDefault();

            ProgressIndicator.Initialise("Creating Jobs", ifcTasks.Count());

            foreach (IfcTask ifcTask in ifcTasks)
            {
                ProgressIndicator.IncrementAndUpdate();

                if (ifcTask == null) continue;

                COBieJobRow job = new COBieJobRow(jobs);

                job.Name =  (string.IsNullOrEmpty(ifcTask.Name.ToString())) ? DEFAULT_STRING : ifcTask.Name.ToString();
                job.CreatedBy = GetTelecomEmailAddress(ifcTask.OwnerHistory);
                job.CreatedOn = GetCreatedOnDateAsFmtString(ifcTask.OwnerHistory);
                job.Category =  ifcTask.ObjectType.ToString(); 
                job.Status = (string.IsNullOrEmpty(ifcTask.Status.ToString())) ? DEFAULT_STRING : ifcTask.Status.ToString();

                job.TypeName = GetObjectType(ifcTask);
                job.Description = (string.IsNullOrEmpty(ifcTask.Description.ToString())) ? DEFAULT_STRING : ifcTask.Description.ToString();

                allPropertyValues.SetFilteredPropertySingleValues(ifcTask); //set properties values to this task
                IfcPropertySingleValue ifcPropertySingleValue = allPropertyValues.GetFilteredPropertySingleValue("TaskDuration");
                job.Duration = ((ifcPropertySingleValue != null) && (ifcPropertySingleValue.NominalValue != null)) ? ConvertNumberOrDefault(ifcPropertySingleValue.NominalValue.ToString()) : DEFAULT_NUMERIC;
                string unitName = GetUnit(ifcPropertySingleValue.Unit);
                job.DurationUnit = (string.IsNullOrEmpty(unitName)) ?  DEFAULT_STRING : unitName;

                ifcPropertySingleValue = allPropertyValues.GetFilteredPropertySingleValue("TaskStartDate");
                job.Start = ((ifcPropertySingleValue != null) && (ifcPropertySingleValue.NominalValue != null)) ? ifcPropertySingleValue.NominalValue.ToString() : new DateTime(1900, 12, 31, 23, 59, 59).ToString("yyyy-MM-dd HH:mm:ss");//default is 1900-12-31T23:59:59;
                unitName =  GetUnit(ifcPropertySingleValue.Unit);
                job.TaskStartUnit = (string.IsNullOrEmpty(unitName)) ? DEFAULT_STRING : unitName;

                ifcPropertySingleValue = allPropertyValues.GetFilteredPropertySingleValue("TaskInterval");
                job.Frequency = ((ifcPropertySingleValue != null) && (ifcPropertySingleValue.NominalValue != null)) ? ConvertNumberOrDefault(ifcPropertySingleValue.NominalValue.ToString()) : DEFAULT_NUMERIC;
                unitName = GetUnit(ifcPropertySingleValue.Unit);
                job.FrequencyUnit = (string.IsNullOrEmpty(unitName)) ? DEFAULT_STRING : unitName;

                job.ExtSystem = GetExternalSystem(ifcTask);
                job.ExtObject = ifcTask.GetType().Name;
                job.ExtIdentifier = ifcTask.GlobalId;

                job.TaskNumber = (string.IsNullOrEmpty(ifcTask.TaskId.ToString())) ? DEFAULT_STRING : ifcTask.TaskId.ToString();
                job.Priors =  GetPriors(ifcTask);
                job.ResourceNames = GetResources(ifcTask);

                jobs.Rows.Add(job);
            }

            ProgressIndicator.Finalise();
            return jobs;
        }

        /// <summary>
        /// Get the number of tasks before this task
        /// </summary>
        /// <param name="ifcTask">IfcTask object</param>
        /// <returns>string holding number of tasks that are before this task</returns>
        private string GetPriors(IfcTask ifcTask)
        {
            IEnumerable<IfcRelSequence> isSuccessorFrom = ifcTask.IsSuccessorFrom;
            //skip the first link to match example sheets count
            if (isSuccessorFrom.Count() == 1) isSuccessorFrom = isSuccessorFrom.First().RelatingProcess.IsSuccessorFrom; 

            int count = 0;
            //assume that the isSuccessorFrom list can only hold one IfcRelSequence.
            while (isSuccessorFrom.Count() == 1) //have a successor task so count one
            {
                count++;
                isSuccessorFrom = isSuccessorFrom.First().RelatingProcess.IsSuccessorFrom; //move up linked tasks to see if a successor to the successor
            }

            return count.ToString();
        }

        /// <summary>
        /// Get required resources for the task
        /// </summary>
        /// <param name="ifcTask">IfcTask object to get resources for</param>
        /// <returns>delimited string of the resources</returns>
        private string GetResources(IfcTask ifcTask)
        {
            IEnumerable<IfcConstructionEquipmentResource> ifcConstructionEquipmentResources = ifcTask.OperatesOn.SelectMany(ratp => ratp.RelatedObjects.OfType<IfcConstructionEquipmentResource>());
            List<string> strList = new List<string>();
            foreach (IfcConstructionEquipmentResource ifcConstructionEquipmentResource in ifcConstructionEquipmentResources)
            {
                if ((ifcConstructionEquipmentResource != null) && (!string.IsNullOrEmpty(ifcConstructionEquipmentResource.Name.ToString())))
                {
                    if (!strList.Contains(ifcConstructionEquipmentResource.Name.ToString()))
                        strList.Add(ifcConstructionEquipmentResource.Name.ToString());
                }
            }
            return (strList.Count > 0) ? string.Join(", ", strList) : DEFAULT_STRING;
        }
        /// <summary>
        /// Get the object IfcTypeObject name from the IfcTask object
        /// </summary>
        /// <param name="ifcTask">IfcTask object</param>
        /// <returns>string holding IfcTypeObject name</returns>
        private string GetObjectType(IfcTask ifcTask)
        {
            //first try
            IEnumerable<IfcTypeObject> ifcTypeObjects = ifcTask.OperatesOn.SelectMany(ratp => ratp.RelatedObjects.OfType<IfcTypeObject>());
            //second try on IsDefinedBy.OfType<IfcRelDefinesByType>
            if ((ifcTypeObjects == null) || (ifcTypeObjects.Count() == 0)) 
                ifcTypeObjects = ifcTask.IsDefinedBy.OfType<IfcRelDefinesByType>().Select(idb => (idb as IfcRelDefinesByType).RelatingType);

            //third try on IsDefinedBy.OfType<IfcRelDefinesByProperties> for DefinesType
            if ((ifcTypeObjects == null) || (ifcTypeObjects.Count() == 0))
                ifcTypeObjects = ifcTask.IsDefinedBy.OfType<IfcRelDefinesByProperties>().SelectMany(idb => (idb as IfcRelDefinesByProperties).RelatingPropertyDefinition.DefinesType);

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
                IEnumerable<IfcObject> ifcObjects = ifcTask.IsDefinedBy.OfType<IfcRelDefinesByProperties>().SelectMany(idb => idb.RelatedObjects);
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
