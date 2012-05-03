using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.DOM.PropertiesQuantities;

namespace Xbim.DOM.ExportHelpers
{
    public class ExportPropertiesHelper
    {
        public void Convert(IBimSingleProperties tProperties, IXbimSingleProperties xbimProperties)
        {
            IEnumerable<IBimPropertySingleValue> props = xbimProperties.FlatProperties;

            foreach (XbimPropertySingleValue prop in props)
            {
                try
                {
                    tProperties.SetProperty(prop);  //todo: revise this solution in comparison with the commented one
                }
                catch (Exception e)
                {
                    
                    throw new Exception("Error while converting property '"+prop.Name+"' in property set '"+prop.PsetName+"': " + e.Message);
                }

                //object value = prop.Value;
                //string pSetName = prop.PsetName;
                //string name = prop.Name;

                //switch (prop.Type)
                //{
                //    case XbimValueTypeEnum.INTEGER:
                //        tProperties.SetProperty_long(pSetName, name, value != null ? (long?)value : null);
                //        break;
                //    case XbimValueTypeEnum.REAL:
                //        tProperties.SetProperty_double(pSetName, name, value != null ? (double?)value : null);
                //        break;
                //    case XbimValueTypeEnum.BOOLEAN:
                //        tProperties.SetProperty_bool(pSetName, name, value != null ? (bool?)value : null);
                //        break;
                //    case XbimValueTypeEnum.STRING:
                //        tProperties.SetProperty_string(pSetName, name, value != null ? (string)value : null);
                //        break;
                //    default:
                //        break;
                //}
            }
        }
    }
}
