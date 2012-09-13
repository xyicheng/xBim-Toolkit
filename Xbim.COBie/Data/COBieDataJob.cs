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

namespace Xbim.COBie.Data
{
    /// <summary>
    /// Class to input data into excel worksheets for the the Job tab.
    /// </summary>
    public class COBieDataJob : COBieData
    {
        /// <summary>
        /// Data Job constructor
        /// </summary>
        /// <param name="model">IModel to read data from</param>
        public COBieDataJob(IModel model)
        {
            Model = model;
        }

        #region Methods

        /// <summary>
        /// Fill sheet rows for Job sheet
        /// </summary>
        /// <returns>COBieSheet<COBieJobRow></returns>
        public COBieSheet<COBieJobRow> Fill()
        {
            //create new sheet
            COBieSheet<COBieJobRow> jobs = new COBieSheet<COBieJobRow>(Constants.WORKSHEET_JOB);

            // get all IfcTask objects from IFC file
            IEnumerable<IfcTask> ifcTasks = Model.InstancesOfType<IfcTask>();

            //IfcTypeObject typObj = Model.InstancesOfType<IfcTypeObject>().FirstOrDefault();
            IfcConstructionEquipmentResource cer = Model.InstancesOfType<IfcConstructionEquipmentResource>().FirstOrDefault();

            foreach (IfcTask task in ifcTasks)
            {
                if (task == null) continue;

                COBieJobRow job = new COBieJobRow(jobs);

                job.Name =  task.Name.ToString();

                job.CreatedBy = GetTelecomEmailAddress(task.OwnerHistory);
                job.CreatedOn = GetCreatedOnDateAsFmtString(task.OwnerHistory);

                job.Category = (task.ObjectType == null) ? DEFAULT_STRING : task.ObjectType.ToString(); ;

                job.Status = (task == null) ? "" : task.Status.ToString();

                job.TypeName = "";
                job.Description = (task == null) ? "" : task.Description.ToString();
                job.Duration = "";

                job.DurationUnit = "";
                job.TaskStartUnit = "";
                job.FrequencyUnit = "";
                //foreach (COBiePickListsRow plRow in pickLists.Rows)
                //{
                //    job.DurationUnit = (plRow == null) ? "" : plRow.DurationUnit + ",";
                //    job.TaskStartUnit = (plRow == null) ? "" : plRow.DurationUnit + ",";
                //    job.FrequencyUnit = (plRow == null) ? "" : plRow.DurationUnit + ",";
                //}
                //job.DurationUnit = job.DurationUnit.TrimEnd(',');
                //job.TaskStartUnit = job.TaskStartUnit.TrimEnd(',');
                //job.FrequencyUnit = job.FrequencyUnit.TrimEnd(',');

                job.Start = "";
                job.Frequency = "";

                job.ExtSystem = GetIfcApplication().ApplicationFullName;
                job.ExtObject = task.GetType().Name;
                job.ExtIdentifier = task.GlobalId;
                job.TaskNumber = (task == null) ? "" : task.GlobalId.ToString();
                job.Priors = (task == null) ? "" : task.Name.ToString();
                job.ResourceNames = (cer == null) ? "" : cer.Name.ToString();

                jobs.Rows.Add(job);
            }

            return jobs;
        }
        #endregion
    }
}
