using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.COBie.Rows;
using Xbim.XbimExtensions.Transactions;
using Xbim.Ifc.ProcessExtensions;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.Extensions;
using Xbim.Ifc.MeasureResource;
using Xbim.Ifc.PropertyResource;
using Xbim.Ifc.ConstructionMgmtDomain;


namespace Xbim.COBie.Serialisers.XbimSerialiser
{
    public class COBieXBimJob : COBieXBim
    {
        #region Properties
        public IEnumerable<IfcTypeObject> IfcTypeObjects { get; private set; }
        public IEnumerable<IfcTask> IfcTasks { get; private set; }
        public IEnumerable<IfcConstructionEquipmentResource> IfcConstructionEquipmentResources { get; private set; }
        
        #endregion

        public COBieXBimJob(COBieXBimContext xBimContext)
            : base(xBimContext)
        {
        }

        /// <summary>
        /// Add the IfcTask to the Model object
        /// </summary>
        /// <param name="cOBieSheet">COBieSheet of COBieResourceRow to read data from</param>
        public void SerialiseJob(COBieSheet<COBieJobRow> cOBieSheet)
        {

            using (Transaction trans = Model.BeginTransaction("Add Job"))
            {
                try
                {
                    IfcTypeObjects = Model.InstancesOfType<IfcTypeObject>();
                    IfcConstructionEquipmentResources = Model.InstancesOfType<IfcConstructionEquipmentResource>();

                    ProgressIndicator.ReportMessage("Starting Jobs...");
                    ProgressIndicator.Initialise("Creating Jobs", (cOBieSheet.RowCount * 2));
                    for (int i = 0; i < cOBieSheet.RowCount; i++)
                    {
                        ProgressIndicator.IncrementAndUpdate();
                        COBieJobRow row = cOBieSheet[i];
                        AddJob(row);
                    }

                    //we need to assign IfcRelSequence relationships, but we need all tasks implemented, so loop rows again
                    IfcTasks = Model.InstancesOfType<IfcTask>(); //get new tasks
                    for (int i = 0; i < cOBieSheet.RowCount; i++)
                    {
                        ProgressIndicator.IncrementAndUpdate();
                        COBieJobRow row = cOBieSheet[i];
                        SetPriors(row);
                    }
                    ProgressIndicator.Finalise();
                    
                    trans.Commit();
                }
                catch (Exception)
                {
                    trans.Rollback();
                    throw;
                }
            }
        }

        

        /// <summary>
        /// Add the data to the IfcTask object
        /// </summary>
        /// <param name="row">COBieJobRow holding the data</param>
        private void AddJob(COBieJobRow row)
        {
            IfcTask ifcTask = Model.New<IfcTask>();
            //Add Created By, Created On and ExtSystem to Owner History. 
            if ((ValidateString(row.CreatedBy)) && (Contacts.ContainsKey(row.CreatedBy)))
                SetNewOwnerHistory(ifcTask, row.ExtSystem, Contacts[row.CreatedBy], row.CreatedOn);
            else
                SetNewOwnerHistory(ifcTask, row.ExtSystem, Model.DefaultOwningUser, row.CreatedOn);

            //using statement will set the Model.OwnerHistoryAddObject to ifcConstructionEquipmentResource.OwnerHistory as OwnerHistoryAddObject is used upon any property changes, 
            //then swaps the original OwnerHistoryAddObject back in the dispose, so set any properties within the using statement
            using (COBieXBimEditScope context = new COBieXBimEditScope(Model, ifcTask.OwnerHistory))
            {
                //Add Name
                if (ValidateString(row.Name)) ifcTask.Name = row.Name;

                //Add Category
                if (ValidateString(row.Category)) ifcTask.ObjectType = row.Category;

                //AddStatus
                if (ValidateString(row.Status)) ifcTask.Status = row.Status;

                //Add Type Relationship
                if (ValidateString(row.TypeName))
                {
                    List<string> typeNames = SplitString(row.TypeName, ':');
                    IEnumerable<IfcTypeObject> ifcTypeObjects = IfcTypeObjects.Where(to => typeNames.Contains(to.Name.ToString().Trim()) );
                    SetRelAssignsToProcess(ifcTask, ifcTypeObjects);
                }
                //Add GlobalId
                AddGlobalId(row.ExtIdentifier, ifcTask);

                //add Description
                if (ValidateString(row.Description)) ifcTask.Description = row.Description;

               
                //Add Duration and duration Unit
                if (ValidateString(row.Duration))
                {
                    IfcPropertySingleValue ifcPropertySingleValue = AddPropertySingleValue(ifcTask, "Pset_Job_COBie", "Job Properties From COBie", "TaskDuration", "Task Duration", new IfcReal(row.Duration));
                    //DurationUnit
                    if (ValidateString(row.DurationUnit))
                        ifcPropertySingleValue.Unit = GetDurationUnit(row.DurationUnit);
                }

                //Add start time and start unit
                if (ValidateString(row.Start))
                {
                    IfcPropertySingleValue ifcPropertySingleValue = AddPropertySingleValue(ifcTask, "Pset_Job_COBie", null, "TaskStartDate", "Task Start Date", new IfcText(row.Start));
                    //TaskStartUnit
                    if (ValidateString(row.TaskStartUnit))
                        ifcPropertySingleValue.Unit = GetDurationUnit(row.TaskStartUnit);
                }

                //Add frequency and frequency unit
                if (ValidateString(row.Frequency))
                {
                    IfcPropertySingleValue ifcPropertySingleValue = AddPropertySingleValue(ifcTask, "Pset_Job_COBie", null, "TaskInterval", "Task Interval", new IfcReal(row.Frequency));
                    //TaskStartUnit
                    if (ValidateString(row.FrequencyUnit))
                        ifcPropertySingleValue.Unit = GetDurationUnit(row.FrequencyUnit);
                } 
                
                //Add Task ID
                if (ValidateString(row.TaskNumber)) ifcTask.TaskId = row.TaskNumber;

                //Add Priors, done in another loop see above

                //Add Resource names
                if (ValidateString(row.ResourceNames))
                {
                    List<string> Resources = row.ResourceNames.Split(',').ToList<string>(); //did dangerous using , as ',' as user can easily place out of sequence.
                    for (int i = 0; i < Resources.Count; i++)
                    {
                        Resources[i] = Resources[i].Trim();
                    }
                    IEnumerable<IfcConstructionEquipmentResource> ifcConstructionEquipmentResource = IfcConstructionEquipmentResources.Where(cer => Resources.Contains(cer.Name.ToString().Trim()));
                    if (ifcConstructionEquipmentResource != null) 
                        SetRelAssignsToProcess(ifcTask, ifcConstructionEquipmentResource);
                   
                }

            }
        }

        

        /// <summary>
        /// Create the relationships between the Process and the types it relates too
        /// </summary>
        /// <param name="processObj">IfcProcess Object</param>
        /// <param name="typeObjs">IEnumerable of IfcTypeObject, list of IfcTypeObjects </param>
        public void SetRelAssignsToProcess(IfcProcess processObj, IEnumerable<IfcObjectDefinition> typeObjs)
        {
            //find any existing relationships to this type
            IfcRelAssignsToProcess processRel = Model.InstancesWhere<IfcRelAssignsToProcess>(rd => rd.RelatingProcess == processObj).FirstOrDefault();
            if (processRel == null) //none defined create the relationship
            {
                processRel = Model.New<IfcRelAssignsToProcess>();
                processRel.RelatingProcess = processObj;


            }
            //add the type objects
            foreach (IfcObjectDefinition type in typeObjs)
            {
                processRel.RelatedObjects.Add_Reversible(type);
            }
        }

        /// <summary>
        /// set up IfcRelSequence for the task
        /// </summary>
        /// <param name="row">COBieJobRow holding the data</param>
        private void SetPriors(COBieJobRow row)
        {
            IEnumerable<IfcTask> ifcTaskFound = IfcTasks.Where(task => task.Name == row.Name && task.TaskId == row.TaskNumber);
            if (ifcTaskFound.Count() == 1) //should equal one
            {
                IfcTask ifcTask = ifcTaskFound.First();
                //find any existing relationships to this type
                IfcRelSequence relSequence = Model.InstancesWhere<IfcRelSequence>(rs => rs.RelatedProcess == ifcTask).FirstOrDefault();
                if (relSequence == null) //none defined create the relationship
                {
                    relSequence = Model.New<IfcRelSequence>();
                    relSequence.RelatedProcess = ifcTask;
                }

                ifcTaskFound = IfcTasks.Where(task => task.Name == row.Name && task.TaskId == row.Priors);
                if (ifcTaskFound.Count() == 1) //should equal one
                {
                    relSequence.RelatingProcess = ifcTaskFound.First();
                    return; //stop exception
                }
            }
            //throw new Exception("COBieXBimJob.SetPriors(): did not find a single task matching name and task number");
        }
        
    }
}
