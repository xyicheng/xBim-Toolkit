using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Xbim.Ifc2x3.PropertyResource;

namespace Xbim.WeXplorer.MVC4.Models
{
    public class XbimPropertyModel: IXbimJsonResult
    {
        public string Type
        {
            get { return _prop.GetType().Name; }
        }

        public uint? Label
        {
            get { return _prop.EntityLabel; }
        }

        public string Name 
        {
            get { return _prop.Name; }
        }

        public string Value
        {
            get { return _prop.NominalValue != null ? _prop.NominalValue.ToString() : ""; }
        }

        private IfcPropertySingleValue _prop;
        public XbimPropertyModel(IfcPropertySingleValue prop)
        {
            _prop = prop;
        }
    }
}