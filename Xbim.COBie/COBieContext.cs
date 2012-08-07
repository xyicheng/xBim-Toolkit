using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.XbimExtensions;

namespace Xbim.COBie
{
	/// <summary>
	/// Context for generating COBie data from one or more IFC models
	/// </summary>
	public class COBieContext
	{
		public COBieContext()
		{
			Models = new List<IModel>();
		}

		/// <summary>
		/// Collection of models to interrogate for data to populate the COBie worksheets
		/// </summary>
		public ICollection<IModel> Models { get; set; }

		/// <summary>
		/// The pick list to use to cross-reference fields in the COBie worksheets
		/// </summary>
		public COBiePickList PickList { get; set; }
	}
}
