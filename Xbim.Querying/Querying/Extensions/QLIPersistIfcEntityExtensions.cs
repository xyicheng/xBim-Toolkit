using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.IO;
using Xbim.XbimExtensions.Interfaces;

namespace Xbim.Querying.Extensions
{
    public static class QLIPersistIfcEntityExtensions
    {
        // Depending on the property the return value can be an enumerable or a single item.
        // This does not depend from the property value but its definition in the schema
        //
        public static object GetPropertyByName(this IPersistIfcEntity entity, string propertyName)
        {
            object retval = null;
            IfcType ifcType = IfcMetaData.IfcType(entity);
            var prop = ifcType.IfcProperties.Where(x => x.Value.PropertyInfo.Name == propertyName).FirstOrDefault().Value;
            if (prop == null) // otherwise test inverses
            {
                prop = ifcType.IfcInverses.Where(x => x.PropertyInfo.Name == propertyName).FirstOrDefault();
            }
            // then populate the return value
            if (prop != null)
            {
                object propVal = prop.PropertyInfo.GetValue(entity, null);
                if (propVal != null)
                {
                    if (prop.IfcAttribute.IsEnumerable)
                    {
                        List<object> tretval = new List<object>();
                        IEnumerable<object> propCollection = propVal as IEnumerable<object>;
                        if (propCollection != null)
                        {
                            foreach (var item in propCollection)
                            {
                                IPersistIfcEntity pe = item as IPersistIfcEntity;
                                tretval.Add(pe);
                            }
                        }
                        retval = tretval;
                    }
                    else
                    {
                        retval = propVal;
                    }
                }
            }
            return retval;
        }
    }
}
