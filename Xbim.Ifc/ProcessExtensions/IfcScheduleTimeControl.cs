using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.Kernel;
using Xbim.XbimExtensions.Parser;

namespace Xbim.Ifc.ProcessExtensions
{
    public class IfcScheduleTimeControl : IfcControl
    {
        #region ISupportIfcParser Members

        public override void IfcParse(int propIndex, IPropertyValue value)
        {

        }

        #endregion
    }
}
