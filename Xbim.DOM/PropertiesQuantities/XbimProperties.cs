using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xbim.Ifc.Kernel;
using Xbim.Ifc.Extensions;
using Xbim.Ifc.SelectTypes;

namespace Xbim.DOM.PropertiesQuantities
{
    public abstract class XbimProperties
    {
        private IfcObject _object;
        private string _psetName;

        internal XbimProperties(IfcObject ifcObject, string propertySetName)
        {
            _object = ifcObject;
            _psetName = propertySetName;
        }

        protected void RemoveProperty(string propertyName)
        {
            _object.RemovePropertySingleValue(_psetName, propertyName);
        }

        protected IfcValue GetProperty(string propertyName)
        {
            return _object.GetPropertySingleNominalValue(_psetName, propertyName);
        }

        protected void SetProperty(string propertyName, IfcValue value)
        {
            _object.SetPropertySingleValue(_psetName, propertyName, value);
        }
    }
}
