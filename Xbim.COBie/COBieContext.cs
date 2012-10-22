using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;
using Xbim.Ifc.SharedBldgElements;
using Xbim.Ifc.ProductExtension;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.SharedComponentElements;
using Xbim.Ifc.StructuralElementsDomain;
using Xbim.Ifc.SharedBldgServiceElements;
using Xbim.Ifc.HVACDomain;
using Xbim.Ifc.ElectricalDomain;
using Xbim.ModelGeometry.Scene;
using Xbim.COBie.Data;

namespace Xbim.COBie
{
	/// <summary>
	/// Context for generating COBie data from one or more IFC models
	/// </summary>
	public class COBieContext : IDisposable
	{

        //Worksheet Global
        public Dictionary<long, string> EMails { get; private set; } //contact list<EntityLable, emailaddress>
        public string TemplateFileName { get; set; } //template used by the workbook
        public string RunDate { get; set; } //Date the Workbook was created on
        
        private  GlobalUnits _workBookUnits;
        /// <summary>
        /// Global Units for the workbook
        /// </summary>
        public GlobalUnits WorkBookUnits
        {
            get
            {
                if (Model == null)
                {
                    throw new ArgumentException("COBieContext must contain a model before calling WorkBookUnits."); 
                   
                }
                if (_workBookUnits == null) //set up global units
                {
                    _workBookUnits = new GlobalUnits();
                    COBieData<COBieRow>.GetGlobalUnits(Model, _workBookUnits);
                }
                return _workBookUnits;
            }
           
        }
        
        public bool DepartmentsUsedAsZones { get; set; } //indicate if we have taken departments as Zones
        public FilterValues Exclude { get; set; } //filter values for attribute extraction in sheets

        public COBieContext()
        {
            RunDate = DateTime.Now.ToString(Constants.DATE_FORMAT);
            EMails = new Dictionary<long, string>();
            Scene = null;
            DepartmentsUsedAsZones = false;
            Exclude = new FilterValues();
        }

        public COBieContext(ReportProgressDelegate progressHandler = null) : this() 
		{
            if (progressHandler != null)
            {
                _progress = progressHandler;
                this.ProgressStatus += progressHandler;
            }
		}

		/// <summary>
        /// Gets the model defined in this context to generate COBie data from
        /// </summary>
        public IModel Model { get; set; }
       
        /// <summary>
        /// Scene object to get Geometry from
        /// </summary>
        public IXbimScene Scene  { get; set; }
       

        private ReportProgressDelegate _progress = null;

        public event ReportProgressDelegate ProgressStatus;

        /// <summary>
        /// Updates the delegates with the current percentage complete
        /// </summary>
        /// <param name="message"></param>
        /// <param name="total"></param>
        /// <param name="current"></param>
        public void UpdateStatus(string message, int total = 0, int current = 0)
        {
            decimal percent = 0;
            if (total != 0 && current > 0)
            {
                message = string.Format("{0} [{1}/{2}]", message, current, total);
                percent = (decimal)current / total * 100;
            }
            if(ProgressStatus != null)
                ProgressStatus((int)percent, message);
        }

        public void Dispose()
        {
            if (Scene != null)
            {
                Scene.Close();
                Scene = null;
            }

            if (_progress != null)
            {
                ProgressStatus -= _progress;
                _progress = null;
            }
        }
    }

    /// <summary>
    /// Global units
    /// </summary>
    public class GlobalUnits 
    {
        public string LengthUnit { get; set; }
        public string AreaUnit { get; set; }
        public string VolumeUnit { get; set; }
        public string MoneyUnit { get; set; }
    }
}
