using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc2x3.MeasureResource;
using Xbim.Ifc2x3.QuantityResource;

namespace Xbim.Ifc2x3.Extensions
{
    public static class PhysicalSimpleQuantityExtensions
    {
        /// <summary>
        /// Returns the named unit for the specified quantity.
        /// 
        /// EFFICIENCY WARNING: *****************
        /// 
        /// this requires a number of Database calls. In case you have to run queries on multiple IfcPhysicalSimpleQuantity it's far better 
        /// to get the instance of IfcUnitAssignment from the model and use its GetUnitFor() method.
        /// </summary>
        /// <returns></returns>
        public static IfcNamedUnit GetResolvedUnit(this IfcPhysicalSimpleQuantity ifcPhysicalSimpleQuantity)
        {
            if (ifcPhysicalSimpleQuantity.Unit != null)
                return ifcPhysicalSimpleQuantity.Unit;
            IfcUnitAssignment modelUnits = ifcPhysicalSimpleQuantity.ModelOf.Instances.OfType<IfcUnitAssignment>().FirstOrDefault(); // not optional, should never return void in valid model
            return modelUnits.GetUnitFor(ifcPhysicalSimpleQuantity);
        }
    }
}
