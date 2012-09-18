using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.XbimExtensions.Parser;

namespace Xbim.COBie
{
	/// <summary>
	/// Context for generating COBie data from one or more IFC models
	/// </summary>
	public class COBieContext : IDisposable
	{
        public COBieContext(ReportProgressDelegate progressHandler = null)
		{
            if (progressHandler != null)
            {
                _progress = progressHandler;
                this.ProgressStatus += progressHandler;
            }
			Models = new List<IModel>();
		}

		/// <summary>
		/// Collection of models to interrogate for data to populate the COBie worksheets
		/// </summary>
        /// <remarks>Due to be obsoleted. Will merge models explicitly</remarks>
		public ICollection<IModel> Models { get; set; }


        private IModel _model = null;
        /// <summary>
        /// Gets the model defined in this context to generate COBie data from
        /// </summary>
        public IModel Model
        {
            get
            {
                if (_model == null)
                {
                    _model = Models.First();
                }
                return _model;
            }
        }

		/// <summary>
		/// The pick list to use to cross-reference fields in the COBie worksheets
		/// </summary>
		public COBiePickList PickList { get; set; }

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

            ProgressStatus((int)percent, message);
        }

        public void Dispose()
        {
            if (_progress != null)
            {
                ProgressStatus -= _progress;
                _progress = null;
            }
        }
    }
}
