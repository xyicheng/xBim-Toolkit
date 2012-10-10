using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Xbim.COBie
{
	/// <summary>
	/// Interface for a Type that exports COBie data into a particular format.
	/// </summary>
	public interface ICOBieFormatter
	{
		/// <summary>
		/// Exports the COBie data into a specialised format.
		/// </summary>
		/// <param name="data"></param>
		void Format(COBieBuilder data);
	}
}
